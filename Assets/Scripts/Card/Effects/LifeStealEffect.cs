/// <summary>
/// 흡혈 카드 효과. 무기 타입에 무관하게 공격 시 데미지의 일정 비율을 HP로 흡수합니다.
/// effectValue = 흡혈율 % (ex: 15 → 데미지의 15% 회복)
/// </summary>
public class LifeStealEffect : CardEffectComponent
{
    private float _rate;

    public override void Initialize(CardData card)
    {
        base.Initialize(card);
        _rate = card.effectValue / 100f;
        AddLifeSteal(_rate);
    }

    private void OnDestroy() => RemoveLifeSteal(_rate);
}
