using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 세계 이동 — 수직 스와이프로 패널 3개를 순환하여 무한 스크롤 구현.
/// 위로 스와이프 → 다음 스테이지, 아래로 스와이프 → 이전 스테이지.
/// </summary>
public class WorldScrollManager : MonoBehaviour
{
    public static WorldScrollManager Instance { get; private set; }

    [Header("패널 (Inspector에서 연결)")]
    public WorldPanel prevPanel;
    public WorldPanel currentPanel;
    public WorldPanel nextPanel;

    [Header("스와이프 감도")]
    [Tooltip("화면 높이의 몇 % 이상 드래그해야 전환되는지 (0.25 = 25%)")]
    [Range(0.1f, 0.5f)]
    public float snapThreshold = 0.25f;

    [Header("전환 애니메이션")]
    [Range(0.1f, 0.6f)]
    public float snapDuration = 0.28f;
    public AnimationCurve snapCurve;

    // ─── 런타임 ───────────────────────────────────────────────
    private RectTransform _prevRT, _currentRT, _nextRT;
    private float         _panelHeight;
    private Vector2       _dragStartPos;
    private float         _offset;
    private bool          _isDragging;
    private bool          _isAnimating;

    public int CurrentStage { get; private set; } = 1;

    // ══════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (snapCurve == null || snapCurve.keys.Length == 0)
            snapCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    void Start()
    {
        _prevRT    = prevPanel.GetComponent<RectTransform>();
        _currentRT = currentPanel.GetComponent<RectTransform>();
        _nextRT    = nextPanel.GetComponent<RectTransform>();

        // Canvas 좌표계 기준으로 패널 높이 결정 (anchoredPosition과 단위 일치)
        var canvas = GetComponentInParent<Canvas>();
        _panelHeight = canvas != null
            ? canvas.GetComponent<RectTransform>().rect.height
            : Screen.height;

        // 초기 배치: 위(이전) / 중앙(현재) / 아래(다음)
        SetY(_prevRT,     _panelHeight);
        SetY(_currentRT,  0f);
        SetY(_nextRT,    -_panelHeight);

        // 초기 내용 설정
        prevPanel.Setup(CurrentStage - 1);
        currentPanel.Setup(CurrentStage);
        nextPanel.Setup(CurrentStage + 1);
    }

    // ══════════════════════════════════════════════════════════

    void Update()
    {
        if (_isAnimating) return;
        HandleMouseInput();
        HandleTouchInput();
    }

    // ─── 입력 처리 ────────────────────────────────────────────

    void HandleMouseInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
            BeginDrag(mouse.position.ReadValue());
        else if (_isDragging && mouse.leftButton.isPressed)
            UpdateDrag(mouse.position.ReadValue());
        else if (_isDragging && mouse.leftButton.wasReleasedThisFrame)
            EndDrag();
    }

    void HandleTouchInput()
    {
        var ts = Touchscreen.current;
        if (ts == null) return;

        var touch = ts.primaryTouch;
        if (touch.press.wasPressedThisFrame)
            BeginDrag(touch.position.ReadValue());
        else if (_isDragging && touch.press.isPressed)
            UpdateDrag(touch.position.ReadValue());
        else if (_isDragging && touch.press.wasReleasedThisFrame)
            EndDrag();
    }

    void BeginDrag(Vector2 screenPos)
    {
        _isDragging   = true;
        _dragStartPos = screenPos;
    }

    void UpdateDrag(Vector2 screenPos)
    {
        _offset = screenPos.y - _dragStartPos.y;

        // 스테이지 1에서 아래로 드래그(이전으로) 차단
        if (CurrentStage <= 1)
            _offset = Mathf.Max(_offset, 0f);

        ApplyOffset(_offset);
    }

    void EndDrag()
    {
        _isDragging = false;
        float threshold = _panelHeight * snapThreshold;

        if (_offset > threshold)
            StartCoroutine(SnapTo(_panelHeight, CommitToNext));
        else if (_offset < -threshold && CurrentStage > 1)
            StartCoroutine(SnapTo(-_panelHeight, CommitToPrev));
        else
            StartCoroutine(SnapTo(0f, null)); // 원위치
    }

    // ─── 위치 적용 & 애니메이션 ───────────────────────────────

    void ApplyOffset(float off)
    {
        SetY(_prevRT,     _panelHeight + off);
        SetY(_currentRT,  0f           + off);
        SetY(_nextRT,    -_panelHeight + off);
    }

    IEnumerator SnapTo(float targetOffset, Action onComplete)
    {
        _isAnimating = true;
        float startOffset = _offset;
        float elapsed     = 0f;

        while (elapsed < snapDuration)
        {
            elapsed += Time.deltaTime;
            float t = snapCurve.Evaluate(Mathf.Clamp01(elapsed / snapDuration));
            ApplyOffset(Mathf.Lerp(startOffset, targetOffset, t));
            yield return null;
        }

        // 정확한 최종 위치 보정
        ApplyOffset(targetOffset);
        _offset = 0f;

        onComplete?.Invoke();
        _isAnimating = false;
    }

    // ─── 패널 순환 ────────────────────────────────────────────

    /// <summary>위로 스와이프 완료 → 다음 스테이지로</summary>
    void CommitToNext()
    {
        CurrentStage++;

        // 역할 교체: prev←current, current←next, next←prev(재활용)
        var (wc, wn, wp) = (currentPanel, nextPanel, prevPanel);
        var (rc, rn, rp) = (_currentRT,   _nextRT,   _prevRT);

        currentPanel = wn; _currentRT = rn;
        prevPanel    = wc; _prevRT    = rc;
        nextPanel    = wp; _nextRT    = rp;

        // 재배치 (offset은 이미 0)
        SetY(_prevRT,     _panelHeight);
        SetY(_currentRT,  0f);
        SetY(_nextRT,    -_panelHeight);

        // 재활용된 패널에 다음 스테이지 내용 설정
        nextPanel.Setup(CurrentStage + 1);

        Debug.Log($"[WorldScroll] Stage {CurrentStage} 진입");
    }

    /// <summary>아래로 스와이프 완료 → 이전 스테이지로</summary>
    void CommitToPrev()
    {
        CurrentStage--;

        // 역할 교체: prev←next(재활용), current←prev, next←current
        var (wc, wp, wn) = (currentPanel, prevPanel, nextPanel);
        var (rc, rp, rn) = (_currentRT,   _prevRT,   _nextRT);

        currentPanel = wp; _currentRT = rp;
        nextPanel    = wc; _nextRT    = rc;
        prevPanel    = wn; _prevRT    = rn;

        SetY(_prevRT,     _panelHeight);
        SetY(_currentRT,  0f);
        SetY(_nextRT,    -_panelHeight);

        prevPanel.Setup(CurrentStage - 1);

        Debug.Log($"[WorldScroll] Stage {CurrentStage} 복귀");
    }

    // ─── 헬퍼 ────────────────────────────────────────────────

    static void SetY(RectTransform rt, float y)
    {
        Vector2 p = rt.anchoredPosition;
        p.y = y;
        rt.anchoredPosition = p;
    }
}
