using UnityEngine;

/// <summary>
/// t3ssel8r 방식의 절차적 애니메이션 컴포넌트.
/// 키프레임 애니메이션 없이 수학 함수만으로 생동감을 부여합니다.
///
/// ■ 대시 (탄성 함수)
///   대시 방향으로 늘어나고(stretch), 반대 방향으로 눌렸다가(squash)
///   감쇠 진동으로 복귀합니다.
///
/// ■ 피격 (스프링-댐퍼)
///   피해를 입으면 오브젝트가 랜덤 방향으로 흔들리다 복귀합니다.
///   스프링 방정식: x'' = -k·x − b·x' 을 수치 적분합니다.
///
/// Player에 붙이고 <see cref="animationRoot"/>에 시각적 루트를 지정합니다.
/// Rigidbody가 있는 루트가 아닌 자식 Mesh 오브젝트를 지정하면 물리와 분리됩니다.
/// </summary>
public class ProceduralAnimator : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("타겟")]
    [Tooltip("스케일·위치 애니메이션을 적용할 시각적 루트. 없으면 transform 사용.")]
    [SerializeField] private Transform animationRoot;

    [Header("대시 탄성")]
    [Tooltip("대시 방향 늘어남 비율. 1 = 원래 크기.")]
    [SerializeField] private float dashStretch       = 1.35f;
    [Tooltip("대시 수직 눌림 비율.")]
    [SerializeField] private float dashSquash        = 0.70f;
    [Tooltip("대시 스쿼시→스트레치 전환 속도.")]
    [SerializeField] private float dashSquashTime    = 0.06f;
    [Tooltip("원래 크기로 복귀하는 스프링 강도.")]
    [SerializeField] private float dashSpringK       = 220f;
    [Tooltip("복귀 스프링 감쇠 계수. 1 = 임계 감쇠(진동 없음), <1 = 진동.")]
    [SerializeField] private float dashSpringDamping = 0.35f;

    [Header("피격 스프링")]
    [Tooltip("피격 시 오프셋 최대 크기 (월드 단위).")]
    [SerializeField] private float hitKickStrength = 0.18f;
    [Tooltip("피격 스프링 강도.")]
    [SerializeField] private float hitSpringK      = 180f;
    [Tooltip("피격 스프링 감쇠.")]
    [SerializeField] private float hitDamping      = 8f;

    // ── 스프링 상태 ───────────────────────────────────────────────────────────

    // Scale spring (x=축방향 스케일, y=수직 스케일): 목표값은 항상 1
    private float _scaleAlongAxis = 1f;   // 대시 방향 or X 스케일
    private float _scalePerp      = 1f;   // 수직 스케일
    private float _scaleVelAlong  = 0f;
    private float _scaleVelPerp   = 0f;

    // Position spring (피격 흔들림)
    private Vector3 _posOffset  = Vector3.zero;
    private Vector3 _posVelocity = Vector3.zero;

    // 대시 방향 (XZ)
    private Vector3 _dashDir   = Vector3.forward;
    private float   _dashTimer = 0f;     // 스쿼시 지속 타이머

    // ── 캐시 ──────────────────────────────────────────────────────────────────

    private Transform         _root;
    private PlayerController3D _controller;
    private PlayerHealth       _health;

    // ── 생명주기 ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        _root       = animationRoot != null ? animationRoot : transform;
        _controller = GetComponent<PlayerController3D>();
        _health     = GetComponent<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (_controller != null) _controller.OnDashStarted  += OnDash;
        if (_health     != null) _health.OnHealthChanged     += OnHit;
    }

    private void OnDisable()
    {
        if (_controller != null) _controller.OnDashStarted  -= OnDash;
        if (_health     != null) _health.OnHealthChanged     -= OnHit;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // ── 대시 탄성 ────────────────────────────────────────────────────────
        if (_dashTimer > 0f)
        {
            // 스쿼시 단계: 짧고 납작하게
            _dashTimer -= dt;
            _scaleAlongAxis = dashSquash;
            _scalePerp      = dashStretch * 0.5f + 1f; // 약간만 부풀림
        }
        else
        {
            // 스트레치→복귀: 대시 방향으로 늘어나다 스프링으로 돌아옴
            SpringStep(ref _scaleAlongAxis, ref _scaleVelAlong,
                       target: 1f, k: dashSpringK, damping: dashSpringDamping, dt);
            SpringStep(ref _scalePerp, ref _scaleVelPerp,
                       target: 1f, k: dashSpringK, damping: dashSpringDamping, dt);
        }

        // 대시 방향 좌표계로 스케일 변환
        Vector3 scale = DirectionalScale(_dashDir, _scaleAlongAxis, _scalePerp);
        _root.localScale = scale;

        // ── 피격 스프링 ──────────────────────────────────────────────────────
        // x'' = -k*x - b*x'   →  velocity += (-k*offset - b*vel)*dt
        // animationRoot가 명시적으로 지정된 경우에만 위치 오프셋을 적용합니다.
        // 루트 transform에 localPosition을 덮어쓰면 Rigidbody 위치가 초기화되므로 금지합니다.
        if (animationRoot != null)
        {
            Vector3 springForce = -hitSpringK * _posOffset - hitDamping * _posVelocity;
            _posVelocity += springForce * dt;
            _posOffset   += _posVelocity * dt;

            _root.localPosition = _posOffset;
        }
    }

    // ── 이벤트 핸들러 ──────────────────────────────────────────────────────────

    private void OnDash(Vector3 direction)
    {
        // XZ 평면 방향만 사용
        _dashDir = direction.sqrMagnitude > 0.001f
            ? new Vector3(direction.x, 0f, direction.z).normalized
            : Vector3.forward;

        // 스쿼시 단계 시작 → 그 후 스프링으로 스트레치→복귀
        _dashTimer    = dashSquashTime;
        _scaleAlongAxis = dashSquash;
        _scalePerp    = 1f / dashSquash;          // 부피 보존
        _scaleVelAlong = (dashStretch - dashSquash) / dashSquashTime;
        _scaleVelPerp  = 0f;
    }

    private void OnHit(int currentHP, int maxHP)
    {
        // 피격 방향은 랜덤 XZ (피격자 없는 경우 완전 랜덤)
        Vector2 rand2D  = Random.insideUnitCircle.normalized;
        Vector3 kickDir = new Vector3(rand2D.x, 0f, rand2D.y);
        _posVelocity   += kickDir * hitKickStrength * 60f; // 임펄스
    }

    // ── 유틸 ──────────────────────────────────────────────────────────────────

    /// <summary>스프링-댐퍼 1축 수치 적분 (Explicit Euler).</summary>
    private static void SpringStep(ref float value, ref float velocity,
                                    float target, float k, float damping, float dt)
    {
        float displacement  = value - target;
        float criticalDamp  = 2f * Mathf.Sqrt(k);       // 임계 감쇠 계수
        float dampForce     = damping * criticalDamp * velocity;
        float springForce   = -k * displacement;
        float accel         = springForce - dampForce;

        velocity += accel * dt;
        value    += velocity * dt;
    }

    /// <summary>
    /// XZ 대시 방향 기준으로 along(앞뒤) / perp(좌우·상하) 스케일을 월드 로컬 스케일로 변환합니다.
    /// Y(높이)는 항상 perp 스케일을 사용합니다.
    /// </summary>
    private static Vector3 DirectionalScale(Vector3 dir, float along, float perp)
    {
        // dir이 (1,0,0)에 가까우면 x=along, z=perp
        // dir이 (0,0,1)에 가까우면 x=perp,  z=along
        float ax = Mathf.Abs(dir.x);
        float az = Mathf.Abs(dir.z);

        float sx = Mathf.Lerp(perp, along, ax);
        float sz = Mathf.Lerp(perp, along, az);
        return new Vector3(sx, perp, sz);
    }
}
