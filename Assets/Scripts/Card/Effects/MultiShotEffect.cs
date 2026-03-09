using System.Collections.Generic;

/// <summary>
/// 멀티샷 카드 효과 (Gun 전용).
/// effectValue  = 추가 발사체 수
/// effectValue2 = 각도 오프셋 (기본 20도)
/// </summary>
public class MultiShotEffect : CardEffectComponent
{
    private readonly List<float> _registeredAngles = new();

    public override void Initialize(CardData card)
    {
        base.Initialize(card);
        var gun = Gun;
        if (gun == null) return;

        int   count = (int)card.effectValue;
        float angle = card.effectValue2 > 0f ? card.effectValue2 : 20f;

        for (int i = 1; i <= count; i++)
        {
            _registeredAngles.Add( angle * i);
            _registeredAngles.Add(-angle * i);
            gun.ExtraAngles.Add( angle * i);
            gun.ExtraAngles.Add(-angle * i);
        }
    }

    private void OnDestroy()
    {
        var gun = Gun;
        if (gun == null) return;
        foreach (float a in _registeredAngles) gun.ExtraAngles.Remove(a);
    }
}
