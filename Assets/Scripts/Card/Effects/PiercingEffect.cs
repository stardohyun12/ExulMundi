/// <summary>관통 탄환 카드 효과 (Gun 전용).</summary>
public class PiercingEffect : CardEffectComponent
{
    public override void Initialize(CardData card)
    {
        base.Initialize(card);
        if (Gun != null) Gun.IsPiercing = true;
    }

    private void OnDestroy()
    {
        if (Gun != null) Gun.IsPiercing = false;
    }
}
