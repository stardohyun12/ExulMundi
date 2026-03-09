/// <summary>
/// 무기 공격 방식 분류.
/// 카드 호환성 판단과 PassiveEffectApplier의 분기 기준으로 사용됩니다.
/// </summary>
public enum WeaponCategory
{
    /// <summary>발사체를 생성하는 무기. 예: 총, 활.</summary>
    Projectile,
    /// <summary>직접 범위를 휘두르는 무기. 예: 검, 도끼.</summary>
    Melee,
    /// <summary>소환물이 플레이어 주위에서 공격하는 무기. 예: 스태프 오브, 드론.</summary>
    Summon,
}

/// <summary>구체적인 무기 종류. WeaponManager가 어떤 컴포넌트를 붙일지 결정할 때 사용합니다.</summary>
public enum WeaponType
{
    Gun,
    Sword,
    Staff,
    Bow,
}
