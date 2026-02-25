using UnityEngine;

public enum WorldType
{
    Normal,   // 일반 전투 (55%)
    Elite,    // 강한 적, 보상 증가 (15%)
    Shop,     // 카드 구매 (10%)
    Healing,  // 전투 없음, HP 회복 (8%)
    Mystery,  // 랜덤 이벤트 (7%)
    Boss,     // 보스 전투 (5%)
}

[CreateAssetMenu(fileName = "NewWorld", menuName = "Exul Mundi/World")]
public class WorldData : ScriptableObject
{
    [Header("기본 정보")]
    public string worldName;
    public Sprite backgroundSprite;

    [Header("타입 & 난이도")]
    public WorldType worldType = WorldType.Normal;
    [Range(1, 10)]
    public int difficulty = 1;

    [Header("적 구성")]
    public EnemyData[] enemies;

    /// <summary>이동 비용 — difficulty 기반 자동 계산</summary>
    public int MoveCost =>
        difficulty <= 3 ? 1 :
        difficulty <= 6 ? 2 : 3;
}
