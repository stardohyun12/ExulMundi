using UnityEngine;

/// <summary>
/// LineRenderer로 슬래시 호(arc)를 그립니다. 스프라이트 없이 동작합니다.
/// MeleeWeapon이 Instantiate 후 Launch()로 방향과 반경을 전달합니다.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class SlashVFX : MonoBehaviour
{
    private const float Lifetime    = 0.25f;
    private const int   ArcPoints   = 24;
    private const float ArcDegrees  = 140f;

    private static readonly Color SlashColor = new(0.8f, 0.95f, 1f, 1f);

    private LineRenderer _lr;
    private float        _elapsed;
    private float        _radius;

    private void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.positionCount    = ArcPoints;
        _lr.useWorldSpace    = false;
        _lr.loop             = false;
        _lr.numCapVertices   = 4;
        _lr.numCornerVertices = 4;
        _lr.startColor       = SlashColor;
        _lr.endColor         = new Color(SlashColor.r, SlashColor.g, SlashColor.b, 0f);
    }

    /// <summary>슬래시 방향과 반경을 설정하고 VFX를 시작합니다.</summary>
    public void Launch(Vector2 direction, float radius)
    {
        _radius = radius;

        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // 호 포인트 계산 — 로컬 공간 기준 (방향이 +X축)
        for (int i = 0; i < ArcPoints; i++)
        {
            float t   = (float)i / (ArcPoints - 1);
            float deg = Mathf.Lerp(-ArcDegrees * 0.5f, ArcDegrees * 0.5f, t);
            float rad = deg * Mathf.Deg2Rad;
            _lr.SetPosition(i, new Vector3(Mathf.Cos(rad) * radius,
                                           Mathf.Sin(rad) * radius, 0f));
        }
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / Lifetime);

        // 폭: 빠르게 두꺼워졌다가 서서히 얇아짐
        float width;
        if (t < 0.25f)
            width = Mathf.Lerp(0f, 1f, t / 0.25f);
        else
            width = Mathf.Lerp(1f, 0f, (t - 0.25f) / 0.75f);

        float w = width * radius * 0.35f;
        _lr.startWidth = w;
        _lr.endWidth   = w * 0.2f;

        // 알파 페이드
        var startC = SlashColor;
        startC.a       = Mathf.Lerp(1f, 0f, t);
        _lr.startColor = startC;
        var endC = startC;
        endC.a       = 0f;
        _lr.endColor = endC;

        if (_elapsed >= Lifetime)
            Destroy(gameObject);
    }

    private float radius => _radius > 0f ? _radius : 1f;
}
