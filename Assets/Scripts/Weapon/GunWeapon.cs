using UnityEngine;
using System.Collections.Generic;

/// <summary>원거리 총기. 가장 가까운 적을 향해 자동 발사합니다.</summary>
public class GunWeapon : WeaponBase
{
    public override WeaponType     WeaponType => WeaponType.Gun;
    public override WeaponCategory Category   => WeaponCategory.Projectile;

    [Header("발사 설정")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float      attackInterval = 0.5f;
    [SerializeField] private float      bulletSpeed    = 12f;
    [SerializeField] private float      bulletSize     = 1f;

    // Inspector 기본값 보존 (ResetStats용)
    private float _baseAttackInterval;
    private float _baseBulletSpeed;
    private float _baseBulletSize;

    private float _timer;

    public override float AttackInterval
    {
        get => attackInterval;
        set => attackInterval = Mathf.Max(0.05f, value);
    }

    public float       BulletSpeed     { get => bulletSpeed;     set => bulletSpeed     = value; }
    public float       BulletSize      { get => bulletSize;      set => bulletSize      = value; }
    public List<float> ExtraAngles     { get; } = new();
    public bool        IsPiercing      { get; set; }
    public bool        IsExplosive     { get; set; }
    public float       ExplosionRadius { get; set; }
    public float       LifeStealRate   { get; set; }
    /// <summary>카드에서 지정한 기본 공격력. 0이면 Projectile 프리팹 기본값 사용.</summary>
    public int         BaseDamage      { get; set; }

    /// <summary>공격 크기 = 총알 크기.</summary>
    public override float AttackSize
    {
        get => bulletSize;
        set => bulletSize = Mathf.Max(0.1f, value);
    }

    /// <summary>공격 개수 = 동시 발사 방향 수 (1이면 단발, 2이면 양쪽 추가 등).</summary>
    public override int AttackCount
    {
        get => 1 + ExtraAngles.Count;
        set
        {
            int extra = Mathf.Max(0, value - 1);
            ExtraAngles.Clear();
            float step = extra > 0 ? 30f / extra : 30f;
            for (int i = 0; i < extra; i++)
            {
                float angle = (i % 2 == 0 ? 1 : -1) * step * ((i / 2) + 1);
                ExtraAngles.Add(angle);
            }
        }
    }

    protected override void Awake()
    {
        _baseAttackInterval = attackInterval;
        _baseBulletSpeed    = bulletSpeed;
        _baseBulletSize     = bulletSize;
        base.Awake();
    }

    /// <summary>Inspector 기본값으로 모든 스탯을 초기화합니다.</summary>
    public override void ResetStats()
    {
        attackInterval  = _baseAttackInterval;
        bulletSpeed     = _baseBulletSpeed;
        bulletSize      = _baseBulletSize;
        BaseDamage      = 0;
        ExtraAngles.Clear();
        IsPiercing      = false;
        IsExplosive     = false;
        ExplosionRadius = 0f;
        LifeStealRate   = 0f;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= attackInterval)
        {
            _timer = 0f;
            Shoot();
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null) return;

        Transform target  = FindNearestEnemy();
        Vector2   baseDir = target != null
            ? ((Vector2)(target.position - transform.position)).normalized
            : Vector2.up;

        SpawnBullet(baseDir);
        foreach (float angle in ExtraAngles)
            SpawnBullet(Quaternion.Euler(0, 0, angle) * baseDir);
    }

    /// <summary>발사체 1개를 생성합니다.</summary>
    public void SpawnBullet(Vector2 direction)
    {
        var go = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        go.transform.localScale = Vector3.one * bulletSize;

        if (go.TryGetComponent<Rigidbody2D>(out var rb))
            rb.linearVelocity = direction * bulletSpeed;

        if (go.TryGetComponent<Projectile>(out var proj))
        {
            if (BaseDamage > 0) proj.Damage = BaseDamage;
            proj.IsPiercing      = IsPiercing;
            proj.IsExplosive     = IsExplosive;
            proj.ExplosionRadius = ExplosionRadius;
            proj.LifeStealRate   = LifeStealRate;
            proj.ShooterHealth   = _health;
        }
    }

    private Transform FindNearestEnemy()
    {
        var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        Transform nearest = null;
        float     minDist = float.MaxValue;
        foreach (var e in enemies)
        {
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < minDist) { minDist = d; nearest = e.transform; }
        }
        return nearest;
    }
}
