using UnityEngine;

/// <summary>근접 검. 주기적으로 주변 범위를 휩쓸어 모든 적에게 피해를 줍니다.</summary>
public class MeleeWeapon : WeaponBase
{
    public override WeaponType     WeaponType => WeaponType.Sword;
    public override WeaponCategory Category   => WeaponCategory.Melee;

    [Header("공격 설정")]
    [SerializeField] private float      attackInterval = 0.8f;
    [SerializeField] private float      slashRadius    = 1.8f;
    [SerializeField] private int        baseDamage     = 2;
    [SerializeField] private GameObject slashVfxPrefab;

    // Inspector 기본값 보존 (ResetStats용)
    private float _baseAttackInterval;
    private float _baseSlashRadius;
    private int   _baseBaseDamage;

    private float _timer;

    public override float AttackInterval
    {
        get => attackInterval;
        set => attackInterval = Mathf.Max(0.05f, value);
    }

    public float SlashRadius   { get => slashRadius; set => slashRadius = Mathf.Max(0.1f, value); }
    public int   BonusDamage   { get; set; }
    public float LifeStealRate { get; set; }
    /// <summary>카드에서 덮어쓸 수 있는 기본 공격력.</summary>
    public int   BaseDamage    { get => baseDamage;  set => baseDamage  = value; }

    /// <summary>공격 크기 = 슬래시 반경.</summary>
    public override float AttackSize
    {
        get => slashRadius;
        set => slashRadius = Mathf.Max(0.1f, value);
    }

    /// <summary>검은 항상 범위 내 전체 적을 타격하므로 개수 개념이 없습니다. 항상 1.</summary>
    public override int AttackCount
    {
        get => 1;
        set { } // 검은 개수 변경 없음
    }

    protected override void Awake()
    {
        _baseAttackInterval = attackInterval;
        _baseSlashRadius    = slashRadius;
        _baseBaseDamage     = baseDamage;
        base.Awake();
    }

    /// <summary>Inspector 기본값으로 모든 스탯을 초기화합니다.</summary>
    public override void ResetStats()
    {
        attackInterval = _baseAttackInterval;
        slashRadius    = _baseSlashRadius;
        baseDamage     = _baseBaseDamage;
        BonusDamage    = 0;
        LifeStealRate  = 0f;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= attackInterval)
        {
            _timer = 0f;
            Slash();
        }
    }

    private void Slash()
    {
        NotifyActivated();
        NotifyCooldownStarted(AttackInterval);

        int totalDamage = baseDamage + BonusDamage;
        var hits        = Physics2D.OverlapCircleAll(transform.position, slashRadius);

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<EnemyBase>(out var enemy)) continue;
            enemy.TakeDamage(totalDamage);

            if (LifeStealRate > 0f && _health != null)
                _health.Heal(Mathf.Max(1, Mathf.RoundToInt(totalDamage * LifeStealRate)));
        }

        if (slashVfxPrefab != null)
        {
            var vfx = Instantiate(slashVfxPrefab, transform.position, Quaternion.identity);
            if (vfx.TryGetComponent<SlashVFX>(out var slashVfx))
                slashVfx.Launch(FindNearestDirection(), slashRadius);
            else
                vfx.transform.localScale = Vector3.one * slashRadius;
        }
    }

    /// <summary>가장 가까운 적 방향을 반환합니다. 적이 없으면 위쪽을 반환합니다.</summary>
    private Vector2 FindNearestDirection()
    {
        var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        Transform nearest = null;
        float     minDist = float.MaxValue;

        foreach (var e in enemies)
        {
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < minDist) { minDist = d; nearest = e.transform; }
        }

        return nearest != null
            ? ((Vector2)(nearest.position - transform.position)).normalized
            : Vector2.up;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, slashRadius);
    }
}
