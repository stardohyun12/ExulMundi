using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 인카운터(적 웨이브)를 관리합니다.
/// dummyMode = true 이면 허수아비를 killsToComplete 마리 처치 시 완료 처리합니다.
/// killsToComplete = 0 이면 무한 리스폰합니다.
/// </summary>
public class EncounterManager : MonoBehaviour
{
    [Header("더미 모드 (프로토타입)")]
    [Tooltip("체크 시 허수아비를 스폰합니다.")]
    [SerializeField] private bool       dummyMode       = true;
    [SerializeField] private GameObject dummyPrefab;
    [SerializeField] private Vector2    dummySpawnPos   = Vector2.zero;
    [Tooltip("이 수만큼 처치하면 인카운터 완료. 0이면 무한 리스폰.")]
    [SerializeField] private int        killsToComplete = 3;

    [Header("인카운터 풀")]
    [SerializeField] private EncounterData[] encounterPool;

    [Header("스폰 반경")]
    [SerializeField] private float spawnRadius = 8f;

    public Action OnEncounterComplete;

    private readonly List<EnemyBase> _activeEnemies = new();
    private bool  _dummyRunning;
    private int   _killCount;
    private float _currentDifficultyMult = 1f;

    /// <summary>세계 선택 시 RunManager가 풀을 교체합니다.</summary>
    public void SetEncounterPool(EncounterData[] pool) => encounterPool = pool;

    /// <summary>인카운터를 시작합니다.</summary>
    public void StartEncounter(float difficultyMult = 1f)
    {
        _killCount             = 0;
        _currentDifficultyMult = difficultyMult;

        if (dummyMode)
        {
            _dummyRunning = true;
            SpawnDummy(difficultyMult);
            return;
        }

        if (encounterPool == null || encounterPool.Length == 0)
        {
            Debug.LogWarning("[EncounterManager] encounterPool이 비어있습니다.");
            OnEncounterComplete?.Invoke();
            return;
        }

        var data = encounterPool[UnityEngine.Random.Range(0, encounterPool.Length)];
        StartCoroutine(RunEncounter(data, difficultyMult));
    }

    /// <summary>현재 인카운터를 강제로 종료합니다.</summary>
    public void ForceEnd()
    {
        _dummyRunning = false;
        StopAllCoroutines();
        foreach (var e in _activeEnemies)
            if (e != null) Destroy(e.gameObject);
        _activeEnemies.Clear();
    }

    // ── 더미 모드 ─────────────────────────────────────

    private void SpawnDummy(float difficultyMult = 1f)
    {
        if (dummyPrefab == null)
        {
            Debug.LogWarning("[EncounterManager] dummyPrefab이 비어있습니다.");
            return;
        }

        var go    = Instantiate(dummyPrefab, dummySpawnPos, Quaternion.identity);
        var enemy = go.GetComponent<EnemyBase>();
        if (enemy == null) return;

        enemy.ApplyDifficultyScale(difficultyMult);
        enemy.OnDied += OnDummyDied;
        _activeEnemies.Add(enemy);
    }

    private void OnDummyDied(EnemyBase enemy)
    {
        _activeEnemies.Remove(enemy);
        if (!_dummyRunning) return;

        _killCount++;
        Debug.Log($"[EncounterManager] 더미 처치 {_killCount}/{killsToComplete}");

        // killsToComplete > 0 이고 목표 달성 시 인카운터 완료
        if (killsToComplete > 0 && _killCount >= killsToComplete)
        {
            _dummyRunning = false;
            Debug.Log("[EncounterManager] 더미 인카운터 완료!");
            OnEncounterComplete?.Invoke();
            return;
        }

        // 목표 미달 or 무한 모드 → 리스폰
        StartCoroutine(RespawnDummyAfterDelay());
    }

    private IEnumerator RespawnDummyAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        if (_dummyRunning) SpawnDummy(_currentDifficultyMult);
    }

    // ── 일반 웨이브 모드 ──────────────────────────────

    private IEnumerator RunEncounter(EncounterData data, float difficultyMult)
    {
        foreach (var wave in data.waves)
        {
            yield return new WaitForSeconds(wave.delay);
            SpawnWave(wave, difficultyMult);

            if (wave.isBoss)
                yield return new WaitUntil(() => _activeEnemies.Count == 0);
        }

        yield return new WaitUntil(() => _activeEnemies.Count == 0);
        OnEncounterComplete?.Invoke();
    }

    private void SpawnWave(EnemyWave wave, float difficultyMult)
    {
        if (wave.enemyPrefabs == null || wave.enemyPrefabs.Length == 0) return;

        for (int i = 0; i < wave.count; i++)
        {
            var prefab = wave.enemyPrefabs[UnityEngine.Random.Range(0, wave.enemyPrefabs.Length)];
            if (prefab == null) continue;

            Vector2 pos = (Vector2)transform.position
                + UnityEngine.Random.insideUnitCircle.normalized * spawnRadius;
            var go    = Instantiate(prefab, pos, Quaternion.identity);
            var enemy = go.GetComponent<EnemyBase>();

            if (enemy == null) continue;
            enemy.ApplyDifficultyScale(difficultyMult);
            enemy.OnDied += OnEnemyDied;
            _activeEnemies.Add(enemy);
        }
    }

    private void OnEnemyDied(EnemyBase enemy) => _activeEnemies.Remove(enemy);

    // ── 중첩 직렬화 클래스 ─────────────────────────────

    [Serializable]
    public class EncounterData
    {
        public string      encounterName;
        public EnemyWave[] waves;
    }

    [Serializable]
    public class EnemyWave
    {
        public GameObject[] enemyPrefabs;
        public int          count  = 3;
        public float        delay  = 0f;
        public bool         isBoss = false;
    }
}
