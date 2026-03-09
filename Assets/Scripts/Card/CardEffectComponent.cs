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
}
