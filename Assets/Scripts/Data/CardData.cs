using UnityEngine;

public enum CardCategory { Offense, Defense, Utility, Special }
public enum CardRarity   { Common, Rare, Legend }

public enum CardEffectType
{
    // === 공격형 (액티브) ===
    InstantDamage,      // 적에게 즉시 데미지 (effectValue = ATK 배율%)
    MultihitDamage,     // 다단 히트 (effectValue = 1회 배율%, hitCount = 횟수)
    PierceDamage,       // DEF 무시 데미지 (effectValue = ATK 배율%)
    ExecuteDamage,      // 적 현재 HP의 effectValue% 즉시 제거
    ConditionDamage,    // 내 HP 50% 이하일 때 effectValue2% 추가 데미지

    // === 방어형 (액티브) ===
    InstantHeal,        // 즉시 HP effectValue 회복
    DEFBuff,            // DEF +effectValue, effectValue2초 지속
    BlockNextHit,       // 다음 공격 1회 완전 무효화
    HealToThreshold,    // HP가 최대치의 effectValue% 미만이면 그만큼 회복
    ThornsBuff,         // 받는 데미지의 effectValue% 반사, effectValue2초 지속
    InvincibleBurst,    // effectValue초간 HP 1 이하로 안 내려감

    // === 유틸형 (액티브) ===
    DrawCard,           // 카드 effectValue장 추가 드로우
    EnergyRegenBoost,   // 에너지 회복 속도 effectValue배, effectValue2초 지속
    AtkSpeedBoost,      // 공격 속도 effectValue배, effectValue2초 지속
    WeaknessExpose,     // 다음 카드 효과 2배 (effectValue 미사용)
    RefreshHand,        // 핸드 전체 버리고 동수 새로 드로우
    EnergyBurst,        // 에너지 전부 소모 → 소모량 * effectValue 데미지

    // === 특수형 (액티브) ===
    BloodPact,          // ATK 영구 +effectValue, 최대HP 영구 -effectValue2
    Gamble,             // 50% 확률: HP effectValue% 회복 OR 데미지
    BurnHand,           // 핸드 모두 버리고 장당 ATK*effectValue% 데미지
    UltraFocus,         // effectValue초간 에너지 회복속도 10배 (카드 연속 사용)
}

public enum PassiveEffectType
{
    None,               // 패시브 효과 없음 (액티브 카드)
    
    // === 스탯 증가 ===
    ATKBonus,           // 공격력 +effectValue%
    DEFBonus,           // 방어력 +effectValue
    MaxHPBonus,         // 최대 HP +effectValue%
    AttackSpeedBonus,   // 공격 속도 +effectValue%
    
    // === 특수 효과 ===
    LifeSteal,          // 생명력 흡수 effectValue%
    LowHPATKBonus,      // HP 50% 이하일 때 공격력 +effectValue%
    LowHPAtkSpeedBonus, // HP 50% 이하일 때 공격 속도 +effectValue%
    CriticalChance,     // 치명타 확률 +effectValue%
    DamageReflect,      // 받는 데미지의 effectValue% 반사
}

/// <summary>
/// 전투 중 플레이어가 사용하는 카드 데이터 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "Exul Mundi/Card")]
public class CardData : ScriptableObject
{
    [Header("카드 정보")]
    public string cardName;
    [TextArea] public string description;
    public Sprite cardArt;
    public CardCategory category;
    public CardRarity rarity;

    [Header("액티브 효과 (카드 사용 시 1회 발동 후 소모)")]
    public CardEffectType effectType;
    public float effectValue;   // 주 효과 수치
    public float effectValue2;  // 보조 수치 (지속시간, 2차 효과 등)
    public int hitCount = 1;    // MultihitDamage 전용
    
    [Header("패시브 효과 (손에 들고 있기만 해도 발동)")]
    public PassiveEffectType passiveEffectType = PassiveEffectType.None;
    public float passiveValue;      // 패시브 효과 수치
    public float passiveValue2;     // 패시브 보조 수치
}
