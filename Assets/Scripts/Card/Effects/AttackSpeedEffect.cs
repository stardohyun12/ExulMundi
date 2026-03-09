/// <summary>
/// 공격 속도 증가 카드 효과.
/// effectValue = 감소율 % (ex: 30 → 30% 빠르게)
/// </summary>
public class AttackSpeedEffect : CardEffectComponent
{
    private float      _originalInterval;
    private WeaponBase _weapon;

    public override void Initialize(CardData card)
    {
        base.Initialize(card);
        _weapon = _weaponManager?.CurrentWeapon;
        if (_weapon == null) return;
        _originalInterval      = _weapon.AttackInterval;
        _weapon.AttackInterval *= 1f - card.effectValue / 100f;
    }

    private void OnDestroy()
    {
        if (_weapon != null) _weapon.AttackInterval = _originalInterval;
    }
}
