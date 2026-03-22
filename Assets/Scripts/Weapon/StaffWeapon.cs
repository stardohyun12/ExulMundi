using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마법 스태프. 플레이어 주위를 회전하는 오브(Orb)를 주기적으로 활성화합니다.
///
/// 주기 구조:
///   [활성 (activeDuration)]  → 오브가 밝게 빛나며 적에게 피해
///   [쿨타임 (cooldownDuration)] → 오브가 회색으로 비활성화, 카드 게이지 채움
///
/// AttackInterval 프로퍼티는 cooldownDuration에 매핑됩니다.
/// (AttackSpeedEffect 카드가 쿨타임을 단축하는 방식으로 동작)
/// </summary>
public class StaffWeapon : WeaponBase
{
    public override WeaponType     WeaponType => WeaponType.Staff;
    public override WeaponCategory Category   => WeaponCategory.Summon;

    [Header("오브 설정")]
    [SerializeField] private int   orbCount       = 3;
    [SerializeField] private float orbitRadius    = 2.0f;
    [SerializeField] private float rotationSpeed  = 180f;
    [SerializeField] private int   orbDamage      = 1;
    [SerializeField] private float damageCooldown = 0.5f;

    [Header("활성/쿨타임 주기")]
    [SerializeField] private float activeDuration   = 3f;
    [SerializeField] private float cooldownDuration = 2f;

    // ── 공개 스탯 ────────────────────────────────────────────────────────────

    public float LifeStealRate { get; set; }
    public int   BonusDamage   { get; set; }

    /// <summary>카드가 덮어쓸 수 있는 오브 기본 공격력.</summary>
    public int BaseDamage { get => orbDamage; set => orbDamage = value; }

    /// <summary>AttackInterval = cooldownDuration. AttackSpeedEffect가 쿨타임을 줄입니다.</summary>
    public override float AttackInterval
    {
        get => cooldownDuration;
        set => cooldownDuration = Mathf.Max(0.1f, value);
    }

    /// <summary>공격 크기 = 오브 스프라이트 스케일.</summary>
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

    public float OrbitRadius
    {
        get => orbitRadius;
        set { orbitRadius = Mathf.Max(0.3f, value); }
    }

    // ── Inspector 기본값 보존 ────────────────────────────────────────────────

    private int   _baseOrbCount;
    private float _baseOrbitRadius;
    private float _baseRotationSpeed;
    private int   _baseOrbDamage;
    private float _baseCooldownDuration;
    private float _baseActiveDuration;

    protected override void Awake()
    {
        _baseOrbCount         = orbCount;
        _baseOrbitRadius      = orbitRadius;
        _baseRotationSpeed    = rotationSpeed;
        _baseOrbDamage        = orbDamage;
        _baseCooldownDuration = cooldownDuration;
        _baseActiveDuration   = activeDuration;
        base.Awake();
    }

    public override void ResetStats()
    {
        orbCount         = _baseOrbCount;
        orbitRadius      = _baseOrbitRadius;
        rotationSpeed    = _baseRotationSpeed;
        orbDamage        = _baseOrbDamage;
        cooldownDuration = _baseCooldownDuration;
        activeDuration   = _baseActiveDuration;
        BonusDamage      = 0;
        LifeStealRate    = 0f;
    }

    // ── 상태 머신 ─────────────────────────────────────────────────────────────

    private enum Phase { Active, Cooldown }

    private static readonly Color ActiveTint   = Color.white;
    private static readonly Color CooldownTint = new(0.40f, 0.40f, 0.40f, 1f);

    private Phase _phase      = Phase.Cooldown;
    private float _phaseTimer = 0f;

    // ── 오브 ──────────────────────────────────────────────────────────────────

    private readonly List<GameObject>             _orbs      = new();
    private readonly List<float>                  _orbAngles = new();
    private readonly Dictionary<EnemyBase, float> _hitTimers = new();

    private float _currentAngle;

    // ── 생명 주기 ─────────────────────────────────────────────────────────────

    protected override void OnEnable()
    {
        base.OnEnable();
        _phaseTimer = 0f;
        RefreshOrbs();
        EnterCooldown();   // 첫 활성화는 쿨타임 이후에
    }

    private void OnDisable() => ClearOrbs();

    private void Update()
    {
        _currentAngle += rotationSpeed * Time.deltaTime;
        if (_currentAngle >= 360f) _currentAngle -= 360f;

        UpdateOrbPositions();
        _phaseTimer += Time.deltaTime;

        switch (_phase)
        {
            case Phase.Active:
                CheckOrbCollisions();
                if (_phaseTimer >= activeDuration) EnterCooldown();
                break;

            case Phase.Cooldown:
                if (_phaseTimer >= cooldownDuration) EnterActive();
                break;
        }
    }

    // ── 상태 전환 ─────────────────────────────────────────────────────────────

    private void EnterCooldown()
    {
        _phase      = Phase.Cooldown;
        _phaseTimer = 0f;
        SetOrbTint(CooldownTint);
        NotifyCooldownStarted(cooldownDuration);
    }

    private void EnterActive()
    {
        _phase      = Phase.Active;
        _phaseTimer = 0f;
        SetOrbTint(ActiveTint);
        NotifyActivated();
        StartCoroutine(OrbPopAnimation());
    }

    // ── 오브 시각 효과 ────────────────────────────────────────────────────────

    private void SetOrbTint(Color color)
    {
        foreach (var orb in _orbs)
            if (orb != null && orb.TryGetComponent<SpriteRenderer>(out var sr))
                sr.color = color;
    }

    /// <summary>활성화 시 오브가 작게 시작해 튀어오르는 팝 애니메이션.</summary>
    private IEnumerator OrbPopAnimation()
    {
        float baseSize = _orbs.Count > 0 && _orbs[0] != null
            ? _orbs[0].transform.localScale.x : 0.35f;

        const float upTime   = 0.10f;
        const float downTime = 0.18f;
        const float peak     = 1.35f;

        // 0.1배 → peak 배
        float t = 0f;
        while (t < upTime)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(0.1f, peak, t / upTime) * baseSize;
            foreach (var orb in _orbs)
                if (orb != null) orb.transform.localScale = Vector3.one * s;
            yield return null;
        }

        // peak 배 → 1배
        t = 0f;
        while (t < downTime)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(peak, 1f, t / downTime) * baseSize;
            foreach (var orb in _orbs)
                if (orb != null) orb.transform.localScale = Vector3.one * s;
            yield return null;
        }

        foreach (var orb in _orbs)
            if (orb != null) orb.transform.localScale = Vector3.one * baseSize;
    }

    // ── 오브 생성/제거 ─────────────────────────────────────────────────────────

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
        var go = new GameObject("StaffOrb");
        go.transform.SetParent(null);

        var sr  = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1;

        var tex    = new Texture2D(16, 16);
        var pixels = new Color[16 * 16];
        var c      = new Color(0.6f, 0.3f, 1.0f, 1f);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
        tex.SetPixels(pixels);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        sr.color  = CooldownTint; // 시작은 쿨타임 색상

        go.transform.localScale = Vector3.one * 0.35f;
        return go;
    }

    // ── 오브 위치/충돌 ────────────────────────────────────────────────────────

    private void UpdateOrbPositions()
    {
        // XZ 평면에서 플레이어 주위를 궤도 회전 (탑다운 3D)
        Vector3 origin = transform.position;
        for (int i = 0; i < _orbs.Count; i++)
        {
            if (_orbs[i] == null) continue;
            float rad = (_currentAngle + _orbAngles[i]) * Mathf.Deg2Rad;
            _orbs[i].transform.position = origin + new Vector3(
                Mathf.Cos(rad) * orbitRadius,
                0f,
                Mathf.Sin(rad) * orbitRadius
            );
        }
    }

    private void CheckOrbCollisions()
    {
        float now      = Time.time;
        float hitRadius = 0.25f;
        int   totalDmg = orbDamage + BonusDamage;

        foreach (var orb in _orbs)
        {
            if (orb == null) continue;
            // 3D 구형 범위 충돌 감지
            var hits = Physics.OverlapSphere(orb.transform.position, hitRadius);
            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent<EnemyBase>(out var enemy)) continue;
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
