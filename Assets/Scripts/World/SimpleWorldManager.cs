using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 간단한 세계 관리자 - 배경색으로 세계 구분
/// </summary>
public class SimpleWorldManager : MonoBehaviour
{
    public static SimpleWorldManager Instance { get; private set; }

    [Header("세계 배경색")]
    public Color[] worldColors = new Color[]
    {
        new Color(0.2f, 0.8f, 0.3f), // 숲 - 녹색
        new Color(0.9f, 0.3f, 0.1f), // 화산 - 빨강
        new Color(0.3f, 0.7f, 0.9f), // 얼음 - 파랑
        new Color(0.6f, 0.4f, 0.8f), // 유적 - 보라
        new Color(0.2f, 0.2f, 0.3f), // 어둠 - 검정
        new Color(1.0f, 0.9f, 0.5f)  // 천상 - 노랑
    };

    [Header("참조")]
    public Camera mainCamera;
    public BattleManager battleManager;
    public TestBattleStarter testBattleStarter;

    [Header("UI")]
    public TextMeshProUGUI worldInfoText;
    public Button skipWorldButton;
    public GameObject cardSelectPanel;
    public TextMeshProUGUI selectPromptText;
    public CardDiscardUI cardDiscardUI;

    [Header("카드 보상")]
    public CardData[] cardPool;             // 보상으로 제시될 카드 풀
    public CardRewardUI cardRewardUI;       // 보상 UI 직접 참조

    private int _currentWorldIndex = 0;
    private int _worldLevel = 1;
    private bool _isSelectingCard = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (skipWorldButton != null)
        {
            skipWorldButton.onClick.AddListener(SkipWorld);
        }

        if (cardSelectPanel != null)
        {
            cardSelectPanel.SetActive(false);
        }

        LoadNextWorld();
    }

    public void LoadNextWorld()
    {
        _currentWorldIndex = Random.Range(0, worldColors.Length);
        
        if (mainCamera != null)
            mainCamera.backgroundColor = worldColors[_currentWorldIndex];

        if (worldInfoText != null)
            worldInfoText.text = $"{GetWorldName(_currentWorldIndex)} Lv.{_worldLevel}";

        // 세계 이동 시 누적 데미지 초기화
        DamageTrackerUI.Instance?.ResetTotal();

        RespawnEnemy();

        Debug.Log($"세계 이동: {GetWorldName(_currentWorldIndex)} Lv.{_worldLevel}");
    }

    private void RespawnEnemy()
    {
        if (battleManager == null || testBattleStarter == null)
        {
            Debug.LogError("[SimpleWorldManager] battleManager 또는 testBattleStarter가 null입니다!");
            return;
        }

        var dummyData = testBattleStarter.dummyData;
        if (dummyData == null)
        {
            Debug.LogError("[SimpleWorldManager] dummyData가 null입니다!");
            return;
        }

        int scaledHP = Mathf.RoundToInt(
            dummyData.maxHP * Mathf.Pow(1.5f, _worldLevel - 1));

        // BattleManager를 통해 새 적을 스폰하고 전투 재시작
        battleManager.SpawnAndStartBattle(dummyData, scaledHP);
    }

    public void OnEnemyDefeated()
    {
        Debug.Log("적 처치! 카드 보상 선택...");
        _worldLevel++;
        Invoke(nameof(ShowCardReward), 1.0f);
    }

    private void ShowCardReward()
    {
        if (cardRewardUI != null && cardPool != null && cardPool.Length > 0)
            cardRewardUI.Show(cardPool, LoadNextWorld);
        else
            LoadNextWorld();
    }

    public void SkipWorld()
    {
        // 카드가 있으면 항상 카드 버리기 선택
        if (PassiveCardManager.Instance != null)
        {
            var hand = PassiveCardManager.Instance.GetCurrentHand();
            if (hand.Count > 0)
            {
                ShowCardSelectPanel();
                return;
            }
        }

        // 카드가 없으면 HP 지불
        PayWithHP();
        LoadNextWorld();
    }

    private void ShowCardSelectPanel()
    {
        _isSelectingCard = true;

        if (cardSelectPanel != null)
        {
            cardSelectPanel.SetActive(true);
        }

        if (selectPromptText != null)
        {
            selectPromptText.text = "버릴 카드를 선택하세요";
        }
    }

    /// <summary>
    /// CardSlot 클릭 → CardDiscardUI에 위임
    /// </summary>
    public void OnCardSelected(CardSlot slot)
    {
        if (!_isSelectingCard) return;
        if (slot == null || slot.OccupantCard == null) return;

        cardSelectPanel?.SetActive(false);

        if (cardDiscardUI != null)
        {
            cardDiscardUI.ShowForDiscard(slot);
        }
        else
        {
            Debug.LogError("[SimpleWorldManager] cardDiscardUI가 연결되지 않았습니다! Inspector에서 연결하세요.");
            PerformDirectDiscard(slot);
        }
    }

    /// <summary>
    /// CardDiscardUI에서 버리기 완료 시 호출
    /// </summary>
    public void OnDiscardComplete()
    {
        _isSelectingCard = false;
        LoadNextWorld();
    }

    /// <summary>
    /// CardDiscardUI에서 취소 시 호출 - 카드 선택 패널 복귀
    /// </summary>
    public void OnDiscardCancelled()
    {
        // 선택 모드 유지하고 패널 다시 표시
        if (cardSelectPanel != null)
        {
            cardSelectPanel.SetActive(true);
        }
    }

    private void PerformDirectDiscard(CardSlot slot)
    {
        CardData card = slot.OccupantCard;
        slot.Clear();
        if (PassiveCardManager.Instance != null)
        {
            PassiveCardManager.Instance.ownedCards.Remove(card);
            PassiveCardManager.Instance.OnCardUsed(card);
        }
        _isSelectingCard = false;
        cardSelectPanel?.SetActive(false);
        LoadNextWorld();
    }

    /// <summary>
    /// 카드 선택 취소 - HP로 대신 지불 (CancelButton에서 호출)
    /// </summary>
    public void CancelCardSelect()
    {
        _isSelectingCard = false;
        cardSelectPanel?.SetActive(false);
        PayWithHP();
        LoadNextWorld();
    }

    public bool IsSelectingCard => _isSelectingCard;

    private void PayWithHP()
    {
        if (battleManager != null && battleManager.HeroUnit != null)
        {
            int damage = Mathf.RoundToInt(battleManager.HeroUnit.MaxHP * 0.2f);
            battleManager.HeroUnit.TakeDamage(damage);
            Debug.Log($"HP 지불: {damage} 데미지");
        }
    }

    private string GetWorldName(int index)
    {
        string[] names = { "숲의 세계", "화산 지대", "얼음 산맥", "고대 유적", "어둠의 성채", "천상의 정원" };
        return names[index];
    }
}
