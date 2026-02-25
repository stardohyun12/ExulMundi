using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 전투 흐름 총괄 관리자.
/// 주인공 1명 vs 적 자동 전투. 플레이어는 카드로 개입.
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    // ───────────────────────────────────────
    // Inspector
    // ───────────────────────────────────────

    [Header("주인공")]
    public HeroData     heroData;
    public HeroUnit     HeroUnit;       // 씬에 배치된 HeroUnit 오브젝트

    [Header("적")]
    public List<EnemyUnit> enemyUnits = new();
    public GameObject      enemyUnitPrefab;
    public Transform       enemySpawnArea;

    [Header("에너지 / 핸드")]
    public EnergySystem energySystem;
    public HandManager  handManager;

    [Header("UI")]
    public BattleUI battleUI;

    // ───────────────────────────────────────
    // 런타임 상태
    // ───────────────────────────────────────

    public bool      IsBattleActive { get; private set; }

    /// <summary>현재 살아있는 적 (카드 효과 타겟)</summary>
    public EnemyUnit CurrentEnemy => enemyUnits.Count > 0 ? enemyUnits[0] : null;

    public event System.Action OnBattleWon;
    public event System.Action OnBattleLost;

    // ───────────────────────────────────────
    // 초기화
    // ───────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // HeroUnit 사망 이벤트 구독
        if (HeroUnit != null)
            HeroUnit.OnHeroDied += OnHeroDied;
    }

    void OnDestroy()
    {
        if (HeroUnit != null)
            HeroUnit.OnHeroDied -= OnHeroDied;
    }

    // ═══════════════════════════════════════
    // 전투 시작 / 종료
    // ═══════════════════════════════════════

    /// <summary>WorldData 기반으로 전투 시작</summary>
    public void StartBattle(WorldData world)
    {
        ClearEnemies();

        // 주인공 준비
        if (HeroUnit != null)
        {
            if (heroData != null && !HeroUnit.IsAlive)
                HeroUnit.Initialize(heroData);
            else
                HeroUnit.ResetForBattle();
        }

        // 적 스폰 (난이도 배율 적용)
        float scaleMult = 1f + (world.difficulty - 1) * 0.15f;
        if (world.enemies != null)
            foreach (var data in world.enemies)
                SpawnEnemy(data, scaleMult);

        // 타겟 할당
        AssignTargets();

        // 에너지 시작
        energySystem?.StartEnergyRegen();

        // 핸드 구성 (HeroData의 시작 덱 사용)
        if (heroData != null && heroData.startingDeck != null)
            handManager?.SetupAndDraw(heroData.startingDeck);

        IsBattleActive = true;

        if (battleUI != null)
            battleUI.InitBattleUI();

        Debug.Log($"전투 시작 — 세계: {world.worldName} (난이도 {world.difficulty})");
    }

    public void EndBattle()
    {
        IsBattleActive = false;

        HeroUnit?.StopAttacking();
        foreach (var e in enemyUnits)
            e.StopAttacking();

        energySystem?.StopEnergyRegen();
    }

    // ═══════════════════════════════════════
    // 전투 이벤트
    // ═══════════════════════════════════════

    /// <summary>적 사망 시 BattleManager로 전달</summary>
    public void OnEnemyDied(EnemyUnit enemy)
    {
        enemyUnits.Remove(enemy);
        Destroy(enemy.gameObject);

        if (enemyUnits.Count == 0 && IsBattleActive)
        {
            IsBattleActive = false;
            Debug.Log("전투 승리!");
            energySystem?.StopEnergyRegen();
            OnBattleWon?.Invoke();
            // WorldManager.Instance?.OnBattleWon(); // 구버전 - 사용 안함
        }
        else
        {
            AssignTargets();
        }
    }

    /// <summary>주인공 사망 시 호출 (HeroUnit.OnHeroDied 이벤트에서)</summary>
    public void OnHeroDied(HeroUnit hero)
    {
        if (!IsBattleActive) return;
        IsBattleActive = false;
        Debug.Log("전투 패배!");
        energySystem?.StopEnergyRegen();
        OnBattleLost?.Invoke();
    }

    // ═══════════════════════════════════════
    // 내부 유틸
    // ═══════════════════════════════════════

    private void SpawnEnemy(EnemyData data, float scaleMult)
    {
        if (enemyUnitPrefab == null || enemySpawnArea == null) return;

        GameObject go   = Instantiate(enemyUnitPrefab, enemySpawnArea);
        EnemyUnit  unit = go.GetComponent<EnemyUnit>();

        // 난이도 배율 적용을 위한 임시 래퍼 (ScriptableObject 직접 수정 안 함)
        var scaledData = ScriptableObject.CreateInstance<EnemyData>();
        scaledData.enemyName   = data.enemyName;
        scaledData.sprite      = data.sprite;
        scaledData.maxHP       = Mathf.RoundToInt(data.maxHP  * scaleMult);
        scaledData.atk         = Mathf.RoundToInt(data.atk    * scaleMult);
        scaledData.def         = data.def;
        scaledData.atkSpeed    = data.atkSpeed;
        scaledData.behaviorType = data.behaviorType;
        scaledData.foodReward  = data.foodReward;
        scaledData.goldReward  = data.goldReward;

        unit.Initialize(scaledData);
        enemyUnits.Add(unit);
    }

    private void AssignTargets()
    {
        // 주인공 → 첫 번째 적
        if (HeroUnit != null && enemyUnits.Count > 0)
            HeroUnit.SetTarget(enemyUnits[0]);

        // 모든 적 → 주인공
        foreach (var e in enemyUnits)
            if (HeroUnit != null)
                e.SetTarget(HeroUnit);
    }

    private void ClearEnemies()
    {
        foreach (var e in enemyUnits)
            if (e != null) Destroy(e.gameObject);
        enemyUnits.Clear();
    }

    // ═══════════════════════════════════════
    // 하위 호환 (CompanionUnit 등 구형 참조 대비 — 제거 예정)
    // ═══════════════════════════════════════

    /// <summary>[Deprecated] CompanionUnit.TakeDamage에서 호출 — 현재 무시</summary>
    public void OnCompanionDied(CompanionUnit companion) { }

    /// <summary>도주 패널티 — HP 감소</summary>
    public void ApplyHPPenalty(int amount) => HeroUnit?.ApplyHPPenalty(amount);

    /// <summary>도주 패널티 — 난이도 증가</summary>
    public void ApplyDifficultyPenalty(int amount)
    {
        // 구버전 WorldManager - 사용 안함
        // WorldManager.Instance?.IncreaseDifficulty(amount);
        Debug.Log($"난이도 증가 (미구현): +{amount}");
    }
}
