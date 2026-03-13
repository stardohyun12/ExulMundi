using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HandSlot에 붙어 슬롯의 활성/비활성 상태와 무기 쿨타임을 시각화합니다.
///
/// 활성 슬롯 (무기 카드 및 인접 ±1 액세서리):
///   OnWeaponActivated        → 게이지 초기화(0), 팝 애니메이션 재생
///   OnWeaponCooldownStarted  → 게이지 채움 시작 (duration 동안 0→1)
///
/// 비활성 슬롯 (인접하지 않은 액세서리 카드):
///   슬롯 전체를 반투명 검정 오버레이로 덮어 비활성 상태임을 표시합니다.
///   게이지 없음.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class HandSlotBehavior : MonoBehaviour
{
    private static readonly Color InactiveOverlayColor = new(0f, 0f, 0f, 0.60f);
    private static readonly Color GaugeColor           = new(0.25f, 0.25f, 0.25f, 0.55f);

    // 글로우 테두리 색상 (황금빛)
    private static readonly Color GlowColorOn  = new(1.0f, 0.78f, 0.20f, 0.90f);
    private static readonly Color GlowColorOff = new(1.0f, 0.78f, 0.20f, 0.00f);
    private const float GlowBorderPx = 6f; // 카드 바깥으로 확장되는 글로우 두께(px)

    private Image         _gauge;
    private Image         _inactiveOverlay;
    private Image         _glowBorder;
    private WeaponManager _weaponManager;
    private bool          _isActive = true;

    private float     _cooldownDuration = 1f;
    private float     _elapsed          = 0f;
    private bool      _isFilling        = false;
    private Coroutine _popCoroutine;
    private Coroutine _glowCoroutine;
    private bool      _isPopped         = false;
    private float     _popHoldTimer     = 0f;
    private Vector2   _restPosition;          // ApplyFanLayout이 설정한 카드의 기준 위치

    // ── 초기화 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// HandUI.Refresh()에서 슬롯 생성 후 호출합니다.
    /// isActive가 false이면 회색 비활성 오버레이를 씌우고 게이지는 숨깁니다.
    /// </summary>
    public void Initialize(WeaponManager weaponManager, bool isActive)
    {
        _weaponManager = weaponManager;
        _isActive      = isActive;

        EnsureGlowBorder();
        EnsureInactiveOverlay();
        EnsureGauge();
        ApplyActiveState();

        if (!_isActive) return;

        float duration = _weaponManager?.CurrentWeapon?.AttackInterval ?? 1f;
        OnCooldownStarted(duration);
    }

    private void EnsureGlowBorder()
    {
        var existing = transform.Find("GlowBorder");
        if (existing != null)
        {
            _glowBorder = existing.GetComponent<Image>();
            return;
        }

        var go = new GameObject("GlowBorder");
        go.transform.SetParent(transform, false);
        // GaugeBar보다 앞(인덱스 1)에, 카드 배경보다 뒤에 위치해 테두리만 노출됩니다.
        go.transform.SetAsFirstSibling();

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-GlowBorderPx, -GlowBorderPx);
        rt.offsetMax = new Vector2(GlowBorderPx, GlowBorderPx);

        var le = go.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        _glowBorder               = go.AddComponent<Image>();
        _glowBorder.color         = GlowColorOff;
        _glowBorder.raycastTarget = false;
    }

    private void EnsureInactiveOverlay()
    {
        var existing = transform.Find("InactiveOverlay");
        if (existing != null)
        {
            _inactiveOverlay = existing.GetComponent<Image>();
            return;
        }

        var go = new GameObject("InactiveOverlay");
        go.transform.SetParent(transform, false);
        go.transform.SetAsLastSibling(); // 모든 자식 위에 그려져야 함

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var le = go.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        _inactiveOverlay               = go.AddComponent<Image>();
        _inactiveOverlay.color         = InactiveOverlayColor;
        _inactiveOverlay.raycastTarget = false;
        _inactiveOverlay.enabled       = false;
    }

    private void EnsureGauge()
    {
        var existing = transform.Find("GaugeBar");
        if (existing != null)
        {
            _gauge = existing.GetComponent<Image>();
            return;
        }

        var go = new GameObject("GaugeBar");
        go.transform.SetParent(transform, false);
        go.transform.SetAsFirstSibling();

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var le = go.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        _gauge               = go.AddComponent<Image>();
        _gauge.color         = GaugeColor;
        _gauge.type          = Image.Type.Filled;
        _gauge.fillMethod    = Image.FillMethod.Horizontal;
        _gauge.fillOrigin    = (int)Image.OriginHorizontal.Left;
        _gauge.fillAmount    = 0f;
        _gauge.raycastTarget = false;
    }

    /// <summary>_isActive 상태에 따라 오버레이와 게이지 가시성을 반영합니다.</summary>
    private void ApplyActiveState()
    {
        if (_inactiveOverlay != null)
            _inactiveOverlay.enabled = !_isActive;

        if (_gauge != null)
            _gauge.gameObject.SetActive(_isActive);
    }

    /// <summary>HandLayoutController.ApplyFanLayout()이 카드 위치 확정 후 호출합니다.
    /// PopAnimation이 항상 이 위치로 복귀하므로 drift가 발생하지 않습니다.</summary>
    public void SetRestPosition(Vector2 pos)
    {
        _restPosition = pos;
        // 팝 애니메이션이 없을 때는 즉시 위치를 동기화합니다.
        if (_popCoroutine == null)
        {
            var rt = GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = _restPosition;
        }
    }

    private void OnEnable()
    {
        WeaponBase.OnWeaponActivated       += OnActivated;
        WeaponBase.OnWeaponCooldownStarted += OnCooldownStarted;
    }

    private void OnDisable()
    {
        WeaponBase.OnWeaponActivated       -= OnActivated;
        WeaponBase.OnWeaponCooldownStarted -= OnCooldownStarted;
    }

    // ── 이벤트 핸들러 ────────────────────────────────────────────────────────

    /// <summary>무기/소환물 활성화 → 게이지 초기화 + 팝 애니메이션 (활성 슬롯만).</summary>
    private void OnActivated()
    {
        if (!_isActive) return;

        _isFilling = false;
        if (_gauge != null) _gauge.fillAmount = 0f;

        // 직접 PopAnimation()을 시작하지 않고 TriggerPop()을 사용합니다.
        // TriggerPop은 _isPopped 상태를 확인해 연사 시 위치 drift를 방지합니다.
        TriggerPop();
    }

    /// <summary>쿨타임 시작 → 게이지 채움 시작 (활성 슬롯만).</summary>
    private void OnCooldownStarted(float duration)
    {
        if (!_isActive) return;

        _cooldownDuration = Mathf.Max(0.05f, duration);
        _elapsed          = 0f;
        _isFilling        = true;
    }

    // ── 매 프레임 게이지 갱신 ────────────────────────────────────────────────

    private void Update()
    {
        if (_gauge == null || !_isFilling || !_isActive) return;

        _elapsed          = Mathf.Min(_elapsed + Time.deltaTime, _cooldownDuration);
        _gauge.fillAmount = _elapsed / _cooldownDuration;

        if (_elapsed >= _cooldownDuration)
            _isFilling = false;
    }

    // ── 팝 애니메이션 ────────────────────────────────────────────────────────

    private const float PopHoldDuration = 0.12f;
    private const float PeakOffsetY     = 3f;    // 아주 미세하게 올라옴 (px)
    private const float PeakScale       = 1.01f; // 스케일 변화 최소화

    /// <summary>외부(HandLayoutController)에서 무기 발사 시 직접 호출합니다.
    /// 연속 호출 시 카드가 내려오지 않고 hold 시간만 연장됩니다.</summary>
    public void TriggerPop()
    {
        // 글로우는 연속 발사마다 항상 재시작해 빛남이 보이게 합니다.
        if (_glowCoroutine != null) StopCoroutine(_glowCoroutine);
        _glowCoroutine = StartCoroutine(GlowAnimation());

        if (_isPopped)
        {
            _popHoldTimer = PopHoldDuration;
            return;
        }
        if (_popCoroutine != null) StopCoroutine(_popCoroutine);
        _popCoroutine = StartCoroutine(PopAnimation());
    }

    private IEnumerator PopAnimation()
    {
        const float upDuration   = 0.07f;
        const float downDuration = 0.12f;

        var rt = GetComponent<RectTransform>();

        // 현재 anchoredPosition 대신 _restPosition 기준으로 움직입니다.
        // 연사로 인한 drift를 방지합니다.
        Vector2 originalPos = _restPosition;
        Vector2 peakPos     = originalPos + new Vector2(0f, PeakOffsetY);

        // ── 올라가기 ─────────────────────────────────────────────────────────
        float t = 0f;
        while (t < upDuration)
        {
            t += Time.unscaledDeltaTime;
            float p              = Mathf.Clamp01(t / upDuration);
            rt.anchoredPosition  = Vector2.Lerp(originalPos, peakPos, p);
            transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(PeakScale, PeakScale, 1f), p);
            yield return null;
        }
        rt.anchoredPosition  = peakPos;
        transform.localScale = new Vector3(PeakScale, PeakScale, 1f);

        // ── 홀드 (연속 발동 시 TriggerPop이 타이머를 연장) ───────────────────
        _isPopped     = true;
        _popHoldTimer = PopHoldDuration;
        while (_popHoldTimer > 0f)
        {
            _popHoldTimer -= Time.unscaledDeltaTime;
            yield return null;
        }
        _isPopped = false;

        // ── 내려오기 ─────────────────────────────────────────────────────────
        t = 0f;
        while (t < downDuration)
        {
            t += Time.unscaledDeltaTime;
            float p              = Mathf.Clamp01(t / downDuration);
            rt.anchoredPosition  = Vector2.Lerp(peakPos, originalPos, p);
            transform.localScale = Vector3.Lerp(new Vector3(PeakScale, PeakScale, 1f), Vector3.one, p);
            yield return null;
        }

        rt.anchoredPosition  = originalPos;
        transform.localScale = Vector3.one;
        _popCoroutine        = null;
    }

    // ── 글로우 테두리 애니메이션 ──────────────────────────────────────────────

    private IEnumerator GlowAnimation()
    {
        if (_glowBorder == null) yield break;

        const float inDuration   = 0.06f;
        const float holdDuration = 0.18f;
        const float outDuration  = 0.40f;

        // 점등
        float t = 0f;
        while (t < inDuration)
        {
            t += Time.unscaledDeltaTime;
            _glowBorder.color = Color.Lerp(GlowColorOff, GlowColorOn, Mathf.Clamp01(t / inDuration));
            yield return null;
        }
        _glowBorder.color = GlowColorOn;

        // 유지
        float hold = 0f;
        while (hold < holdDuration)
        {
            hold += Time.unscaledDeltaTime;
            yield return null;
        }

        // 서서히 소등
        t = 0f;
        while (t < outDuration)
        {
            t += Time.unscaledDeltaTime;
            _glowBorder.color = Color.Lerp(GlowColorOn, GlowColorOff, Mathf.Clamp01(t / outDuration));
            yield return null;
        }
        _glowBorder.color = GlowColorOff;
        _glowCoroutine    = null;
    }
}
