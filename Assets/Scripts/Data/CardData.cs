using UnityEngine;

public enum CardRarity { Common, Uncommon, Rare, Legendary }

/// <summary>
/// 카드 종류.
/// Passive = 0 (기본값) — 무기 스탯을 강화하는 패시브 효과 카드.
/// Weapon  = 1           — 런 시작 시 자동으로 무기를 결정하는 카드. 손패에 표시되지만 효과는 항상 적용됨.
/// </summary>
public enum CardType { Passive = 0, Weapon = 1 }

/// <summary>카드 하나를 정의하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "Exul Mundi/Card Data")]
public class CardData : ScriptableObject
{
    [Header("기본 정보")]
    public string    cardName;
    [TextArea]
    public string    description;
    public Sprite    artwork;
    public CardRarity rarity;

    [Header("카드 종류")]
    [Tooltip("Weapon: 런 시작 시 무기를 결정합니다. 무기는 항상 사용되며 손패에 표시됩니다.\nPassive: 손패에 있는 동안 무기 스탯을 강화합니다.")]
    public CardType  cardType;

    [Header("무기 카드 전용")]
    [Tooltip("cardType == Weapon일 때 장착할 무기 타입")]
    public WeaponType weaponType;

    [Header("무기 기본 스탯 (Weapon 카드 전용)")]
    [Tooltip("기본 공격력. 0 = 무기 스크립트 기본값 유지.")]
    public int   baseDamage;
    [Tooltip("기본 공격 간격(초). 0 = 기본값 유지.")]
    public float baseAttackInterval;
    [Tooltip("사거리 / 슬래시 반경 / 궤도 반경. 0 = 기본값 유지.")]
    public float baseRange;
    [Tooltip("발사체 속도 (Gun 전용). 0 = 기본값 유지.")]
    public float baseProjectileSpeed;
    [Tooltip("공격 크기. Gun=총알 크기, Sword=슬래시 반경, Staff=오브 크기. 0 = 기본값 유지.")]
    public float baseAttackSize;
    [Tooltip("공격 개수. Gun=추가 발사 수, Staff=오브 개수. 0 = 기본값 유지.")]
    public int   baseAttackCount;

    [Header("패시브 효과 (Passive 카드 전용)")]
    [Tooltip("PassiveEffectApplier에서 처리하는 효과 식별자. ex: AttackSpeedEffect")]
    public string effectComponentType;
    public float  effectValue;
    public float  effectValue2;

    [Header("시너지 태그")]
    public string[] synergyTags;

    [Header("무기 호환성")]
    [Tooltip("비워두면 모든 카테고리에 적용됩니다.")]
    public WeaponCategory[] compatibleCategories;

    /// <summary>지정 무기 카테고리와 호환되는지 확인합니다.</summary>
    public bool IsCompatible(WeaponCategory category)
    {
        if (compatibleCategories == null || compatibleCategories.Length == 0) return true;
        foreach (var c in compatibleCategories)
            if (c == category) return true;
        return false;
    }
}
