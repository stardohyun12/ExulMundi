using UnityEngine;

/// <summary>
/// 모든 카드 효과 컴포넌트의 추상 기반.
/// CardInventory가 카드 추가/제거 시 동적으로 Add/Destroy합니다.
/// </summary>
public abstract class CardEffectComponent : MonoBehaviour
{
    protected CardData         _card;
    protected WeaponManager    _weaponManager;
    protected PlayerController _controller;
    protected PlayerHealth     _health;

    /// <summary>카드가 추가될 때 CardInventory가 호출합니다.</summary>
    public virtual void Initialize(CardData card)
    {
        _card          = card;
        _weaponManager = GetComponent<WeaponManager>();
        _controller    = GetComponent<PlayerController>();
        _health        = GetComponent<PlayerHealth>();
    }

    protected GunWeapon   Gun   => _weaponManager?.CurrentWeapon as GunWeapon;
    protected MeleeWeapon Melee => _weaponManager?.CurrentWeapon as MeleeWeapon;
    protected StaffWeapon Staff => _weaponManager?.CurrentWeapon as StaffWeapon;

    /// <summary>
    /// 현재 무기의 데미지에 배율을 곱합니다. 무기 타입에 무관하게 동작합니다.
    /// </summary>
    protected void ApplyDamageMultiplier(float multiplier)
    {
        if (Gun   != null) Gun.BaseDamage   = Mathf.Max(1, Mathf.RoundToInt(Gun.BaseDamage   * multiplier));
        if (Melee != null) Melee.BaseDamage = Mathf.Max(1, Mathf.RoundToInt(Melee.BaseDamage * multiplier));
        if (Staff != null) Staff.BaseDamage = Mathf.Max(1, Mathf.RoundToInt(Staff.BaseDamage * multiplier));
    }

    /// <summary>
    /// 현재 무기에 흡혈률을 더합니다. 무기 타입에 무관하게 동작합니다.
    /// </summary>
    protected void AddLifeSteal(float rate)
    {
        if (Gun   != null) Gun.LifeStealRate   += rate;
        if (Melee != null) Melee.LifeStealRate += rate;
        if (Staff != null) Staff.LifeStealRate += rate;
    }

    /// <summary>
    /// 현재 무기에서 흡혈률을 뺍니다. 무기 타입에 무관하게 동작합니다.
    /// </summary>
    protected void RemoveLifeSteal(float rate)
    {
        if (Gun   != null) Gun.LifeStealRate   -= rate;
        if (Melee != null) Melee.LifeStealRate -= rate;
        if (Staff != null) Staff.LifeStealRate -= rate;
    }
}
