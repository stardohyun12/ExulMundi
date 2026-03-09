using UnityEngine;

/// <summary>모든 무기의 추상 기반. WeaponManager가 활성/비활성을 제어합니다.</summary>
public abstract class WeaponBase : MonoBehaviour
{
    public abstract WeaponType     WeaponType     { get; }
    /// <summary>공격 방식 분류. 카드 호환성과 PassiveEffectApplier 분기에 사용합니다.</summary>
    public abstract WeaponCategory Category       { get; }
    public abstract float          AttackInterval { get; set; }
    /// <summary>공격 크기. Gun=총알 크기, Sword=슬래시 반경, Staff=오브 크기.</summary>
    public abstract float          AttackSize     { get; set; }
    /// <summary>공격 개수. Gun=추가 발사 수, Staff=오브 개수. Sword는 항상 1.</summary>
    public abstract int            AttackCount    { get; set; }

    /// <summary>카드 효과 재적용 전에 모든 스탯을 Inspector 기본값으로 되돌립니다.</summary>
    public abstract void ResetStats();

    protected PlayerHealth _health;

    protected virtual void Awake()
    {
        enabled = false; // WeaponManager가 명시적으로 켠다
    }

    protected virtual void OnEnable()
    {
        if (_health == null)
            _health = GetComponent<PlayerHealth>();
    }
}
