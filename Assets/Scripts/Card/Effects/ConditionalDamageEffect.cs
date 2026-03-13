using UnityEngine;

/// <summary>
/// 조건부 공격력 보너스 효과. HP 조건 충족 시 모든 무기의 데미지를 증폭합니다.
/// effectValue  = HP% 임계값 (ex: 50 → 현재 HP가 최대의 50% 이하일 때 발동)
/// effectValue2 = 데미지 배율 (ex: 1.5 → 데미지 50% 증가)
/// PassiveEffectApplier의 ApplyAll() 또는 WeaponBase의 공격 직전 훅에서 호출합니다.
/// </summary>
public class ConditionalDamageEffect : CardEffectComponent
{
    private float _hpThresholdPercent;
    private float _damageMultiplier;

    public override void Initialize(CardData card)
    {
        base.Initialize(card);
        _hpThresholdPercent = card.effectValue;
        _damageMultiplier   = card.effectValue2;
    }

    /// <summary>현재 HP 비율이 임계값 이하이면 true를 반환합니다.</summary>
    public bool IsConditionMet()
    {
        if (_health == null || _health.MaxHP <= 0) return false;
        return (float)_health.CurrentHP / _health.MaxHP * 100f <= _hpThresholdPercent;
    }

    /// <summary>조건 충족 여부에 따른 데미지 배율을 반환합니다. 조건 미충족 시 1f.</summary>
    public float GetDamageMultiplier() => IsConditionMet() ? _damageMultiplier : 1f;

    /// <summary>
    /// 조건 충족 시 모든 무기 타입의 BaseDamage에 배율을 즉시 적용합니다.
    /// PassiveEffectApplier에서 매 사이클마다 ResetStats 이후 호출하세요.
    /// </summary>
    public void ApplyIfConditionMet()
    {
        if (!IsConditionMet()) return;
        ApplyDamageMultiplier(_damageMultiplier);
    }
}
