using UnityEngine;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("파티")]
    public List<CompanionUnit> companionUnits = new List<CompanionUnit>();

    [Header("적")]
    public List<EnemyUnit> enemyUnits = new List<EnemyUnit>();

    [Header("프리팹")]
    public GameObject companionUnitPrefab;
    public GameObject enemyUnitPrefab;

    [Header("스폰 위치")]
    public Transform companionSpawnArea;
    public Transform enemySpawnArea;

    [Header("UI")]
    public BattleUI battleUI;

    public bool IsBattleActive { get; private set; }

    public event System.Action OnBattleWon;
    public event System.Action OnBattleLost;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 세계 데이터 기반으로 전투 시작
    /// </summary>
    public void StartBattle(WorldData world)
    {
        ClearBattlefield();

        // 적 생성
        if (world.enemies != null)
        {
            for (int i = 0; i < world.enemies.Length; i++)
            {
                SpawnEnemy(world.enemies[i], i);
            }
        }

        // 동료 유닛 활성화 및 타겟 설정
        foreach (var companion in companionUnits)
        {
            companion.gameObject.SetActive(true);
            companion.ResetForBattle();
        }

        AssignTargets();
        IsBattleActive = true;

        // 전투 UI 초기화
        if (battleUI != null)
            battleUI.InitBattleUI();
    }

    /// <summary>
    /// 전투 종료 (도주 또는 결과 처리 시)
    /// </summary>
    public void EndBattle()
    {
        IsBattleActive = false;

        foreach (var companion in companionUnits)
            companion.StopAttacking();
        foreach (var enemy in enemyUnits)
            enemy.StopAttacking();
    }

    /// <summary>
    /// 적 사망 시 호출
    /// </summary>
    public void OnEnemyDied(EnemyUnit enemy)
    {
        enemyUnits.Remove(enemy);
        Destroy(enemy.gameObject);

        if (enemyUnits.Count == 0 && IsBattleActive)
        {
            // 적 전멸 — 승리
            IsBattleActive = false;
            Debug.Log("전투 승리!");
            OnBattleWon?.Invoke();
            WorldManager.Instance.OnBattleWon();
        }
        else
        {
            // 남은 적에게 타겟 재설정
            AssignTargets();
        }
    }

    /// <summary>
    /// 동료 사망 시 호출
    /// </summary>
    public void OnCompanionDied(CompanionUnit companion)
    {
        companionUnits.Remove(companion);
        companion.gameObject.SetActive(false);

        if (companionUnits.Count == 0 && IsBattleActive)
        {
            // 전멸 — 패배
            IsBattleActive = false;
            Debug.Log("전투 패배!");
            OnBattleLost?.Invoke();
        }
    }

    /// <summary>
    /// 파티에 동료 추가 (PartyManager에서 호출)
    /// </summary>
    public void AddCompanionToParty(CompanionData data)
    {
        if (companionUnitPrefab == null || companionSpawnArea == null) return;

        GameObject go = Instantiate(companionUnitPrefab, companionSpawnArea);
        CompanionUnit unit = go.GetComponent<CompanionUnit>();
        unit.Initialize(data);
        companionUnits.Add(unit);
    }

    private void SpawnEnemy(EnemyData data, int index)
    {
        if (enemyUnitPrefab == null || enemySpawnArea == null) return;

        GameObject go = Instantiate(enemyUnitPrefab, enemySpawnArea);
        EnemyUnit unit = go.GetComponent<EnemyUnit>();
        unit.Initialize(data);
        enemyUnits.Add(unit);
    }

    private void AssignTargets()
    {
        // 동료 → 첫 번째 살아있는 적 공격
        foreach (var companion in companionUnits)
        {
            if (enemyUnits.Count > 0)
                companion.SetTarget(enemyUnits[0]);
        }

        // 적 → 랜덤 동료 공격
        foreach (var enemy in enemyUnits)
        {
            if (companionUnits.Count > 0)
                enemy.SetTarget(companionUnits[Random.Range(0, companionUnits.Count)]);
        }
    }

    private void ClearBattlefield()
    {
        foreach (var enemy in enemyUnits)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }
        enemyUnits.Clear();
    }
}
