/// <summary>
/// 폭발 탄환 카드 효과 (Gun 전용).
/// effectValue2 = 폭발 반경 (기본 2)
/// </summary>
public class ExplosiveShotEffect : CardEffectComponent
{
    public override void Initialize(CardData card)
    {
        base.Initialize(card);
        var gun = Gun;
        if (gun == null) return;
        gun.IsExplosive    = true;
        gun.ExplosionRadius = card.effectValue2 > 0f ? card.effectValue2 : 2f;
    }

    private void OnDestroy()
    {
        var gun = Gun;
        if (gun == null) return;
        gun.IsExplosive    = false;
        gun.ExplosionRadius = 0f;
    }
}
