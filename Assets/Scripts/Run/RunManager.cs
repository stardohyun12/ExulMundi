using UnityEngine;
using System.Collections;

/// <summary>
/// 런 전체 흐름을 관리합니다.
/// 시작 시 세계(무기 타입) 1회 선택 → 전투 → 카드 보상 → 반복.
/// </summary>
public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("참조")]
    [SerializeField] private EncounterManager encounterManager;
    [SerializeField] private CardRewardUI     cardRewardUI;
    [SerializeField] private EscapeUI         escapeUI;
    [SerializeField] private WorldSelectionUI worldSelectionUI;
    [SerializeField] private GameOverUI       gameOverUI;

    [Header("세계 풀 (시작 시 선택)")]
    [SerializeField] private WorldDefinition[] worldPool;

    [Header("기본 카드 풀 (세계 카드풀 없을 때 사용)")]
    [SerializeField] private CardData[] fallbackCardPool;

    [Header("난이도")]
    [SerializeField] private float difficultyPerEncounter = 1.15f;

    private int             _encounterIndex = 0;
    private float           _difficultyMult = 1f;
    private WorldDefinition _selectedWorld;

    public int             EncounterIndex => _encounterIndex;
    public float           DifficultyMult => _difficultyMult;
    public WorldDefinition SelectedWorld  => _selectedWorld;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        PlayerHealth.OnPlayerDied += OnPlayerDied;

        // 게임 시작 시 단 1회 세계 선택
        if (worldSelectionUI != null && worldPool != null && worldPool.Length > 0)
            worldSelectionUI.Show(worldPool, OnWorldSelected);
        else
            BeginEncounter();
    }

    private void OnDestroy() => PlayerHealth.OnPlayerDied -= OnPlayerDied;

    // ── 세계 선택 (1회) ─────────────────────────────────

    private void OnWorldSelected(WorldDefinition world)
    {
        _selectedWorld = world;

        // 무기 카드를 손패에 추가 → PassiveEffectApplier가 무기를 장착합니다.
        if (world.weaponCard != null)
        {
            CardInventory.Instance?.AddCard(world.weaponCard);
        }
        else
        {
            Debug.LogWarning($"[RunManager] '{world.worldName}'에 weaponCard가 없습니다. 무기가 장착되지 않습니다.");
        }

        Debug.Log($"[RunManager] 세계: {world.worldName} | 무기 카드: {world.weaponCard?.cardName ?? "없음"}");
        BeginEncounter();
    }

    // ── 인카운터 ─────────────────────────────────────────

    private void BeginEncounter()
    {
        encounterManager.OnEncounterComplete = OnEncounterComplete;
        encounterManager.StartEncounter(_difficultyMult);
        escapeUI?.Show();
    }

    private void OnEncounterComplete()
    {
        escapeUI?.Hide();
        StartCoroutine(ShowRewardThenNext());
    }

    private IEnumerator ShowRewardThenNext()
    {
        yield return new WaitForSeconds(0.4f);

        CardData[] pool = (_selectedWorld?.cardPool?.Length > 0)
            ? _selectedWorld.cardPool
            : fallbackCardPool;

        cardRewardUI.Show(pool, OnRewardChosen);
    }

    private void OnRewardChosen(CardData card)
    {
        if (card != null) CardInventory.Instance?.AddCard(card);
        AdvanceEncounter();
    }

    private void AdvanceEncounter()
    {
        _encounterIndex++;
        _difficultyMult *= difficultyPerEncounter;
        Debug.Log($"[RunManager] 인카운터 {_encounterIndex} | 난이도 x{_difficultyMult:F2}");
        BeginEncounter();
    }

    // ── 탈출 ─────────────────────────────────────────────

    /// <summary>HP 1을 소모해 현재 인카운터에서 탈출합니다.</summary>
    public void EscapeWithHP()
    {
        FindFirstObjectByType<PlayerHealth>()?.TakeDamage(1);
        ForcedEscape();
    }

    /// <summary>카드 1장을 제거해 현재 인카운터에서 탈출합니다.</summary>
    public void EscapeWithCard(CardData card)
    {
        CardInventory.Instance?.RemoveCard(card);
        ForcedEscape();
    }

    private void ForcedEscape()
    {
        encounterManager.ForceEnd();
        escapeUI?.Hide();
        OnRewardChosen(null); // 보상 없이 다음으로
    }

    // ── 게임 오버 ─────────────────────────────────────────

    private void OnPlayerDied()
    {
        encounterManager.ForceEnd();
        escapeUI?.Hide();
        Debug.Log("[RunManager] 플레이어 사망 — 게임 오버");
        gameOverUI?.Show(_encounterIndex);
    }
}
