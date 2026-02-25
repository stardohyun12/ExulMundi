using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 세계(스테이지) 전환 총괄.
/// 식량 소비, 난이도 관리, 보상 처리.
/// </summary>
public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; }

    [Header("세계 데이터")]
    public WorldData[] worldPool;

    [Header("참조")]
    public Image          backgroundImage;
    public WorldTransition worldTransition;
    public BattleManager  battleManager;

    [Header("식량 시스템")]
    public int  startingFood = 15;
    public TextMeshProUGUI foodText;

    [Header("보상 UI (승리 후)")]
    public GameObject rewardPanel;

    // ───────────────────────────────────────
    // 런타임 상태
    // ───────────────────────────────────────

    private WorldData _currentWorld;
    private int       _worldIndex      = -1;
    private int       _currentFood;
    private int       _difficultyBonus = 0; // 도주 패널티 누적

    public int  CurrentFood       => _currentFood;
    public int  DifficultyBonus   => _difficultyBonus;

    // ═══════════════════════════════════════
    // 초기화
    // ═══════════════════════════════════════

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        _currentFood = startingFood;
        UpdateFoodUI();

        if (rewardPanel != null) rewardPanel.SetActive(false);

        TransitionToNextWorld();
    }

    // ═══════════════════════════════════════
    // 세계 전환
    // ═══════════════════════════════════════

    /// <summary>다음 세계로 이동 (식량 소비)</summary>
    public void TransitionToNextWorld()
    {
        WorldData next = PickNextWorld();

        // 식량 소비
        if (next != null)
        {
            int cost = next.MoveCost;
            if (_currentFood < cost)
            {
                Debug.Log("식량 부족! 이동 불가.");
                // TODO: 식량 부족 UI 표시
                return;
            }
            _currentFood -= cost;
            UpdateFoodUI();
        }

        worldTransition.PlayTransition(() => SetupWorld(next));
    }

    /// <summary>도주 시 — 패널티 선택 후 이동</summary>
    public void FleeFromBattle()
    {
        battleManager.EndBattle();
        PenaltyManager.Instance.ShowPenaltySelection(() => TransitionToNextWorld());
    }

    /// <summary>전투 승리 시 — 보상 후 이동</summary>
    public void OnBattleWon()
    {
        // 식량 보상 추가
        if (_currentWorld != null)
        {
            int foodGain = _currentWorld.worldType == WorldType.Elite ? 7 : 3;
            _currentFood += foodGain;
            UpdateFoodUI();
            Debug.Log($"식량 +{foodGain} (현재: {_currentFood})");
        }

        // 보상 패널 표시 (임시: 3초 후 자동 이동)
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
            Invoke(nameof(HideRewardAndTransition), 3f);
        }
        else
        {
            TransitionToNextWorld();
        }
    }

    private void HideRewardAndTransition()
    {
        if (rewardPanel != null) rewardPanel.SetActive(false);
        TransitionToNextWorld();
    }

    // ═══════════════════════════════════════
    // 난이도 / 패널티
    // ═══════════════════════════════════════

    /// <summary>도주 패널티 — 다음 세계 난이도 보너스 누적</summary>
    public void IncreaseDifficulty(int amount)
    {
        _difficultyBonus += amount;
        Debug.Log($"난이도 누적 증가: +{amount} (총 보너스: {_difficultyBonus})");
    }

    // ═══════════════════════════════════════
    // 내부 유틸
    // ═══════════════════════════════════════

    private WorldData PickNextWorld()
    {
        if (worldPool == null || worldPool.Length == 0) return null;

        int next;
        do { next = Random.Range(0, worldPool.Length); }
        while (next == _worldIndex && worldPool.Length > 1);

        _worldIndex = next;
        return worldPool[next];
    }

    private void SetupWorld(WorldData world)
    {
        _currentWorld = world;
        if (world == null) return;

        if (backgroundImage != null && world.backgroundSprite != null)
            backgroundImage.sprite = world.backgroundSprite;

        Debug.Log($"세계 진입: {world.worldName} (타입: {world.worldType})");

        switch (world.worldType)
        {
            case WorldType.Normal:
            case WorldType.Elite:
            case WorldType.Boss:
                battleManager.StartBattle(world);
                break;

            case WorldType.Healing:
                // 전투 없이 HP 회복
                battleManager.HeroUnit?.Heal(Mathf.RoundToInt(
                    battleManager.HeroUnit.MaxHP * 0.3f));
                Debug.Log("회복 스테이지: HP 30% 회복");
                Invoke(nameof(TransitionToNextWorld), 2f);
                break;

            case WorldType.Shop:
                // TODO: 상점 UI 오픈
                Debug.Log("상점 스테이지 (미구현)");
                Invoke(nameof(TransitionToNextWorld), 1f);
                break;

            case WorldType.Mystery:
                // TODO: 랜덤 이벤트
                Debug.Log("미스터리 스테이지 (미구현)");
                Invoke(nameof(TransitionToNextWorld), 1f);
                break;
        }
    }

    private void UpdateFoodUI()
    {
        if (foodText != null) foodText.text = $"식량: {_currentFood}";
    }

    public WorldData GetCurrentWorld() => _currentWorld;
}
