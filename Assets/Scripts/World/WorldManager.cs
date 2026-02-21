using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 세계 풀에서 랜덤으로 다음 세계를 선택하고 전환을 관리
/// </summary>
public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; }

    [Header("세계 데이터")]
    public WorldData[] worldPool;

    [Header("참조")]
    public Image backgroundImage;
    public WorldTransition worldTransition;
    public BattleManager battleManager;

    private WorldData currentWorld;
    private int worldIndex = -1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        TransitionToNextWorld();
    }

    /// <summary>
    /// 다음 세계로 전환 트리거
    /// </summary>
    public void TransitionToNextWorld()
    {
        WorldData next = PickNextWorld();
        worldTransition.PlayTransition(() => SetupWorld(next));
    }

    /// <summary>
    /// 도주 시 호출 — 패널티 선택 후 다음 세계로
    /// </summary>
    public void FleeFromBattle()
    {
        battleManager.EndBattle();
        PenaltyManager.Instance.ShowPenaltySelection(() => TransitionToNextWorld());
    }

    /// <summary>
    /// 전투 승리 시 호출
    /// </summary>
    public void OnBattleWon()
    {
        TransitionToNextWorld();
    }

    private WorldData PickNextWorld()
    {
        if (worldPool == null || worldPool.Length == 0) return null;

        // 같은 세계 연속 방지
        int next;
        do
        {
            next = Random.Range(0, worldPool.Length);
        } while (next == worldIndex && worldPool.Length > 1);

        worldIndex = next;
        return worldPool[next];
    }

    private void SetupWorld(WorldData world)
    {
        currentWorld = world;
        if (world == null) return;

        // 배경 설정
        if (backgroundImage != null && world.backgroundSprite != null)
            backgroundImage.sprite = world.backgroundSprite;

        // 전투 시작
        battleManager.StartBattle(world);
    }

    public WorldData GetCurrentWorld() => currentWorld;
}
