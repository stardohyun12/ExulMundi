using UnityEngine;

/// <summary>
/// 범용 데미지 증가 효과. 무기 타입에 무관하게 현재 BaseDamage에 배율을 곱합니다.
/// effectValue = 증가율 % (ex: 20 → 데미지 +20%)
/// </summary>
public class BonusDamageEffect : CardEffectComponent
{
    private float _multiplier;

    // ResetStats 이후 시점에서 BaseDamage를 읽어야 하므로
    // PassiveEffectApplier가 ResetStats → Initialize 순서를 보장해야 합니다.
    public override void Initialize(CardData card)
    {
        base.Initialize(card);
        _multiplier = 1f + card.effectValue / 100f;
        ApplyDamageMultiplier(_multiplier);
    }

    // OnDestroy 시점엔 WeaponManager.ResetStats()가 이미 호출되므로
    // 별도 복구가 필요 없습니다. (CardInventory가 Remove 전 ResetStats를 호출하는 구조)
}
