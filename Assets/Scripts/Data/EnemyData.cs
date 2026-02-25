using UnityEngine;

public enum EnemyBehaviorType
{
    SimpleAttacker, // 단순 반복 공격
    Enrager,        // HP 50% 이하 시 ATK 1.5배
    Defender,       // 주기적으로 방어막 생성 (DEF 일시 증가)
    Debuffer,       // 주인공 ATK 감소 공격
    Berserker,      // 공격 속도 빠르고 데미지 낮음
}

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Exul Mundi/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string enemyName;
    public Sprite sprite;

    [Header("스탯")]
    public int   maxHP    = 50;
    public int   atk      = 8;
    public int   def      = 0;
    public float atkSpeed = 1f; // 초당 공격 횟수

    [Header("행동 패턴")]
    public EnemyBehaviorType behaviorType = EnemyBehaviorType.SimpleAttacker;

    [Header("보상")]
    public int foodReward = 3;
    public int goldReward = 2;
}
