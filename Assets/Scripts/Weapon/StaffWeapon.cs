using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 마법 스태프. 플레이어 주위를 회전하는 오브(Orb)를 유지하고,
/// 오브가 적에 닿으면 피해를 줍니다. 오브 개수·회전 반경·속도를 카드로 강화할 수 있습니다.
/// </summary>
public class StaffWeapon : WeaponBase
{
    public override WeaponType     WeaponType => WeaponType.Staff;
    public override WeaponCategory Category   => WeaponCategory.Summon;

    [Header("오브 설정")]
    [SerializeField] private int   orbCount        = 3;
    [SerializeField] private float orbitRadius     = 2.0f;
    [SerializeField] private float rotationSpeed   = 180f;  // 초당 각도
    [SerializeField] private int   orbDamage       = 1;
    [SerializeField] private float damageCooldown  = 0.5f;  // 오브당 적 1마리 피해 쿨타임
    [SerializeField] private float attackInterval  = 0.5f;  // WeaponBase 인터페이스용 (오브 회전 속도로 대응)

    public float LifeStealRate { get; set; }
    public int   BonusDamage   { get; set; }
    /// <summary>카드에서 덮어쓸 수 있는 오브 기본 공격력.</summary>
    public int   BaseDamage    { get => orbDamage;  set => orbDamage  = value; }

    /// <summary>공격 크기 = 오브의 물리적 크기.</summary>
    public override float AttackSize
    {
        get => _orbs.Count > 0 ? _orbs[0].transform.localScale.x : 0.35f;
        set
        {
            float s = Mathf.Max(0.05f, value);
            foreach (var orb in _orbs)
                if (orb != null) orb.transform.localScale = Vector3.one * s;
        }
    }

    /// <summary>공격 개수 = 오브 개수.</summary>
    public override int AttackCount
    {
        get => orbCount;
        set { orbCount = Mathf.Max(1, value); RefreshOrbs(); }
    }

    // Inspector 기본값 보존 (ResetStats용)
    private int   _baseOrbCount;
    private float _baseOrbitRadius;
    private float _baseRotationSpeed;
    private int   _baseOrbDamage;
    private float _baseAttackInterval;

    protected override void Awake()
    {
        _baseOrbCount       = orbCount;
        _baseOrbitRadius    = orbitRadius;
        _baseRotationSpeed  = rotationSpeed;
        _baseOrbDamage      = orbDamage;
        _baseAttackInterval = attackInterval;
        base.Awake();
    }

    /// <summary>Inspector 기본값으로 모든 스탯을 초기화합니다.</summary>
    public override void ResetStats()
    {
        orbCount       = _baseOrbCount;
        orbitRadius    = _baseOrbitRadius;
        rotationSpeed  = _baseRotationSpeed;
        orbDamage      = _baseOrbDamage;
        attackInterval = _baseAttackInterval;
        BonusDamage    = 0;
        LifeStealRate  = 0f;
    }

    public override float AttackInterval
    {
        get => attackInterval;
        set
        {
            attackInterval = Mathf.Max(0.05f, value);
            // attackInterval이 짧아질수록 회전이 빨라진다
            rotationSpeed = Mathf.Clamp(180f / attackInterval * 0.5f, 60f, 720f);
        }
    }

    public float OrbitRadius
    {
        get => orbitRadius;
        set { orbitRadius = Mathf.Max(0.3f, value); }
    }

    // 오브 인스턴스와 각도
    private readonly List<GameObject>            _orbs        = new();
    private readonly List<float>                 _orbAngles   = new();
    // 오브별 적별 마지막 피해 시간
    private readonly Dictionary<EnemyBase, float> _hitTimers  = new();

    private float _currentAngle;

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshOrbs();
    }

    private void OnDisable() => ClearOrbs();

    private void Update()
    {
        _currentAngle += rotationSpeed * Time.deltaTime;
        if (_currentAngle >= 360f) _currentAngle -= 360f;

        UpdateOrbPositions();
        CheckOrbCollisions();
    }

    // ── 오브 생성/제거 ─────────────────────────────────

    private void RefreshOrbs()
    {
        ClearOrbs();

        float step = 360f / Mathf.Max(1, orbCount);
        for (int i = 0; i < orbCount; i++)
        {
            _orbAngles.Add(step * i);
            _orbs.Add(CreateOrb());
        }
    }

    private void ClearOrbs()
    {
        foreach (var orb in _orbs)
            if (orb != null) Destroy(orb);
        _orbs.Clear();
        _orbAngles.Clear();
        _hitTimers.Clear();
    }

    private GameObject CreateOrb()
    {
        // 단색 사각형 오브 (FallbackSprite와 동일한 방식)
        var go = new GameObject("StaffOrb");
        go.transform.SetParent(null);  // 월드 공간에 독립 배치

        var sr   = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1;
        var tex  = new Texture2D(16, 16);
        var pixels = new Color[16 * 16];
        var c    = new Color(0.6f, 0.3f, 1.0f, 1f);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
        tex.SetPixels(pixels);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        sr.color  = Color.white;

        go.transform.localScale = Vector3.one * 0.35f;
        return go;
    }

    // ── 오브 위치 갱신 ────────────────────────────────

    private void UpdateOrbPositions()
    {
        Vector2 origin = transform.position;
        for (int i = 0; i < _orbs.Count; i++)
        {
            if (_orbs[i] == null) continue;
            float rad = (_currentAngle + _orbAngles[i]) * Mathf.Deg2Rad;
            _orbs[i].transform.position = origin + new Vector2(
                Mathf.Cos(rad) * orbitRadius,
                Mathf.Sin(rad) * orbitRadius
            );
        }
    }

    // ── 충돌 감지 (OverlapCircle) ──────────────────────

    private void CheckOrbCollisions()
    {
        float now        = Time.time;
        float orbHitRadius = 0.25f;
        int   totalDmg   = orbDamage + BonusDamage;

        foreach (var orb in _orbs)
        {
            if (orb == null) continue;
            var hits = Physics2D.OverlapCircleAll(orb.transform.position, orbHitRadius);
            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent<EnemyBase>(out var enemy)) continue;

                // 오브당·적당 쿨타임 체크
                if (_hitTimers.TryGetValue(enemy, out float last) && now - last < damageCooldown)
                    continue;

                _hitTimers[enemy] = now;
                enemy.TakeDamage(totalDmg);

                if (LifeStealRate > 0f && _health != null)
                    _health.Heal(Mathf.Max(1, Mathf.RoundToInt(totalDmg * LifeStealRate)));
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.3f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, orbitRadius);
    }
}
