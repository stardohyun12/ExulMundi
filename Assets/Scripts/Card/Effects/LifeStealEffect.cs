/// <summary>
/// 흡혈 카드 효과. 무기 타입 무관하게 공격 시 HP를 흡수합니다.
/// effectValue = 흡혈율 % (ex: 20 → 데미지의 20% 회복)
/// </summary>
public class LifeStealEffect : CardEffectComponent
{
    private float _rate;

    public override void Initialize(CardData card)
    {
        base.Initialize(card);
        _rate = card.effectValue / 100f;
        if (Gun   != null) Gun.LifeStealRate   += _rate;
        if (Melee != null) Melee.LifeStealRate += _rate;
    }

    private void OnDestroy()
    {
        if (Gun   != null) Gun.LifeStealRate   -= _rate;
        if (Melee != null) Melee.LifeStealRate -= _rate;
    }
}
