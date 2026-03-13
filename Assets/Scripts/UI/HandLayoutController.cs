using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 손패 패널의 두 가지 상태를 관리합니다.
///
/// Fan 모드  : 패널이 화면 하단에 반쯤 숨겨지고 카드가 호(arc) 형태로 배치됩니다.
///             무기가 발사될 때 해당 카드 슬롯이 위로 튀어오르는 Pop 애니메이션을 재생합니다.
///
/// Expanded 모드 : '손패 보기' 버튼으로 진입합니다.
///                 패널이 화면 중앙으로 슬라이드 업되고 Time.timeScale = 0으로 일시정지됩니다.
///                 카드가 일렬로 펼쳐지며 드래그로 순서를 바꿀 수 있습니다.
///                 반투명 딤 배경이 활성화됩니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class HandLayoutController : MonoBehaviour
{
    // ── 상수 ─────────────────────────────────────────────────────────────────

    private const float FanPeekRatio    = 0.70f;  // 패널 30% 숨김 → 엣지 카드 하단 ~35% 화면 밖
    private const float FanSpread       = 5f;     // 1오프셋당 기울기(°) — 6장 양끝 ±12.5°, 5장 ±10°
    private const float CardSpacingPx   = 70f;    // 카드 중심 간 x 간격 — 130px 카드에서 60px 겹침(46%)
    private const float VerticalCurvePx = 65f;    // 중앙 카드 최대 y 오프셋(px) — 190px 카드 기준 34% 높이차
    private const float SlideDuration   = 0.28f;
    private const float FlatSpacing     = 180f;
    private const float ExpandedYRatio  = 0.30f;

    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("참조")]
    [SerializeField] private GameObject dimBackground;
    [SerializeField] private Button     viewHandButton;

    // ── 내부 상태 ─────────────────────────────────────────────────────────────

    private RectTransform              _panelRect;
    private float                      _panelHeight;
    private bool                       _isExpanded;
    private Coroutine                  _slideCoroutine;
    private readonly List<RectTransform> _slotRects = new();

    // ── Unity 생명주기 ────────────────────────────────────────────────────────

    private void Awake()
    {
        _panelRect   = GetComponent<RectTransform>();
        _panelHeight = _panelRect.sizeDelta.y;
    }

    private void Start()
    {
        viewHandButton?.onClick.AddListener(ToggleExpanded);
        dimBackground?.SetActive(false);
        // 시작 시 Fan 모드로 패널을 하단에 배치
        _panelRect.anchoredPosition = new Vector2(0f, TargetFanY());
    }

    // ── 공개 API ──────────────────────────────────────────────────────────────

    /// <summary>HandUI.Refresh() 완료 후 호출해 슬롯 목록을 갱신하고 레이아웃을 재계산합니다.</summary>
    public void SetSlots(List<GameObject> slotObjects)
    {
        _slotRects.Clear();
        foreach (var obj in slotObjects)
            if (obj != null && obj.TryGetComponent<RectTransform>(out var rt))
                _slotRects.Add(rt);

        if (_isExpanded) ApplyFlatLayout();
        else             ApplyFanLayout();
    }

    /// <summary>무기 발사 시 해당 인덱스의 카드 슬롯에 Pop 애니메이션을 재생합니다.</summary>
    public void PlayCardPop(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slotRects.Count) return;
        _slotRects[slotIndex].GetComponent<HandSlotBehavior>()?.TriggerPop();
    }

    // ── 상태 전환 ─────────────────────────────────────────────────────────────

    private void ToggleExpanded()
    {
        if (_isExpanded) CollapseToFan();
        else             ExpandToView();
    }

    /// <summary>Expanded 모드로 전환합니다. 게임이 일시정지됩니다.</summary>
    public void ExpandToView()
    {
        if (_isExpanded) return;
        _isExpanded    = true;
        Time.timeScale = 0f;
        dimBackground?.SetActive(true);
        ApplyFlatLayout();
        SlidePanel(TargetExpandedY(), animated: true);
    }

    /// <summary>Fan 모드로 복귀합니다. 게임이 재개됩니다.</summary>
    public void CollapseToFan()
    {
        if (!_isExpanded) return;
        _isExpanded    = false;
        Time.timeScale = 1f;
        dimBackground?.SetActive(false);
        ApplyFanLayout();
        SlidePanel(TargetFanY(), animated: true);
    }

    // ── 레이아웃 계산 ─────────────────────────────────────────────────────────

    private void ApplyFanLayout()
    {
        int count = _slotRects.Count;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            float offset        = i - (count - 1) * 0.5f;
            float normalizedPos = count > 1 ? 2f * i / (count - 1) - 1f : 0f;

            float x    = CardSpacingPx   * offset;
            float yArc = VerticalCurvePx * (1f - normalizedPos * normalizedPos);
            float tilt = FanSpread       * offset;

            var rt = _slotRects[i];

            // ── BOTTOM 피벗: 카드 하단이 화면 하단 근처에 고정되고 상단이 안쪽으로 펼쳐짐.
            //    이미지2처럼 "손으로 쥔" 느낌을 내려면 회전 기준점이 카드 하단이어야 합니다.
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);

            rt.anchoredPosition = new Vector2(x, yArc);
            rt.localRotation    = Quaternion.Euler(0f, 0f, tilt);
            rt.localScale       = Vector3.one;

            rt.GetComponent<HandSlotBehavior>()?.SetRestPosition(new Vector2(x, yArc));
        }
    }

    private void ApplyFlatLayout()
    {
        int   count      = _slotRects.Count;
        if (count == 0) return;

        float totalWidth = (count - 1) * FlatSpacing;
        float startX     = -totalWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var rt       = _slotRects[i];
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);

            rt.anchoredPosition = new Vector2(startX + i * FlatSpacing, 0f);
            rt.localRotation    = Quaternion.identity;
            rt.localScale       = Vector3.one;
        }
    }

    // ── 패널 슬라이드 ─────────────────────────────────────────────────────────

    private void SlidePanel(float targetY, bool animated)
    {
        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);

        if (animated) _slideCoroutine = StartCoroutine(SlidePanelCoroutine(targetY));
        else          _panelRect.anchoredPosition = new Vector2(0f, targetY);
    }

    private IEnumerator SlidePanelCoroutine(float targetY)
    {
        float startY  = _panelRect.anchoredPosition.y;
        float elapsed = 0f;

        while (elapsed < SlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / SlideDuration));
            _panelRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(startY, targetY, t));
            yield return null;
        }

        _panelRect.anchoredPosition = new Vector2(0f, targetY);
        _slideCoroutine = null;
    }

    // ── Y 위치 헬퍼 ───────────────────────────────────────────────────────────

    private float TargetFanY() => -_panelHeight * (1f - FanPeekRatio);

    private float TargetExpandedY()
    {
        var canvas = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        float screenH = canvas != null ? canvas.sizeDelta.y : 1080f;
        return screenH * ExpandedYRatio;
    }
}
