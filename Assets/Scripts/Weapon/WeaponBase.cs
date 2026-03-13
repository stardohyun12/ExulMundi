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

    /// <summary>
    /// 무기/소환물이 활성화될 때 발생합니다 (쿨타임 종료, 첫 공격 등).
    /// HandSlotBehavior가 카드 팝 애니메이션과 게이지 초기화에 사용합니다.
    /// </summary>
    public static event System.Action OnWeaponActivated;

    /// <summary>
    /// 무기가 쿨타임에 진입할 때 발생합니다.
    /// float = 쿨타임 지속 시간 (초). HandSlotBehavior가 게이지 채움에 사용합니다.
    /// </summary>
    public static event System.Action<float> OnWeaponCooldownStarted;

    /// <summary>활성화 시 호출합니다. 카드 팝 애니메이션과 게이지 초기화를 유발합니다.</summary>
    protected void NotifyActivated() => OnWeaponActivated?.Invoke();

    /// <summary>쿨타임 시작 시 호출합니다. 카드 게이지 채움을 시작합니다.</summary>
    protected void NotifyCooldownStarted(float duration) => OnWeaponCooldownStarted?.Invoke(duration);

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
