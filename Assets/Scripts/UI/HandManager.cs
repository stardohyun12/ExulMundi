using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 손패 패널을 관리합니다.
///
/// 진원(circular arc) 알고리즘으로 카드를 배치합니다.
///   x = sin(θ) × fanRadius
///   y = (cos(θ) − 1) × fanRadius + handYOffset
///
/// Fan 모드  : 패널 하단 30%를 화면 밖으로 숨깁니다. 카드가 호 형태로 배치됩니다.
/// Expanded 모드 : '손패 보기' 버튼으로 진입합니다. 카드 일렬 배치 + 게임 일시정지.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class HandManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("카드 프리팹 / 부모")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform  cardsParent;

    [Header("부채꼴 설정")]
    [SerializeField] private float fanRadius    = 800f;  // 원호 반지름 (클수록 평평)
    [SerializeField] private float handYOffset  = 80f;   // 중앙 카드 Y 오프셋 (양수 = 위로)
    [SerializeField] private float anglePerCard = 10f;   // 인접 카드 사이 고정 각도(°)

    [Header("호버 / 선택")]
    [SerializeField] private float hoverRiseY    = 50f;
    [SerializeField] private float hoverScale    = 1.12f;
    [SerializeField] private float selectedRiseY = 100f;

    [Header("Expanded 모드")]
    [SerializeField] private GameObject dimBackground;
    [SerializeField] private Button     viewHandButton;
    [SerializeField] private float      flatSpacing    = 180f;
    [SerializeField] private float      expandedYRatio = 0.30f;
    [SerializeField] private float      slideDuration  = 0.28f;

    // ── 프로퍼티 (CardView에서 참조) ──────────────────────────────────────────

    public float HoverRiseY    => hoverRiseY;
    public float HoverScale    => hoverScale;
    public float SelectedRiseY => selectedRiseY;
    public bool  IsExpanded    => _isExpanded;

    // ── 내부 상태 ─────────────────────────────────────────────────────────────

    private RectTransform            _panelRect;
    private float                    _panelHeight;
    private bool                     _isExpanded;
    private Coroutine                _slideCoroutine;
    private readonly List<CardView>  _cards = new();
    private CardView                 _selectedCard;
    private WeaponManager            _weaponManager;

    // ── Unity 생명주기 ────────────────────────────────────────────────────────

    private void Awake()
    {
        _panelRect   = GetComponent<RectTransform>();
        _panelHeight = _panelRect.sizeDelta.y;
    }

    private void Start()
    {
        _weaponManager = FindAnyObjectByType<WeaponManager>();

        viewHandButton?.onClick.AddListener(ToggleExpanded);
        dimBackground?.SetActive(false);
        _panelRect.anchoredPosition = new Vector2(0f, TargetFanY());

        if (CardInventory.Instance != null)
        {
            CardInventory.Instance.OnCardAdded   += OnCardDataChanged;
            CardInventory.Instance.OnCardRemoved += OnCardDataChanged;
            CardInventory.Instance.OnCardMoved   += Refresh;
        }
        else
        {
            Debug.LogWarning("[HandManager] CardInventory.Instance가 null입니다.");
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (CardInventory.Instance != null)
        {
            CardInventory.Instance.OnCardAdded   -= OnCardDataChanged;
            CardInventory.Instance.OnCardRemoved -= OnCardDataChanged;
            CardInventory.Instance.OnCardMoved   -= Refresh;
        }
    }

    // ── 공개 API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 무기 발사 시 해당 인덱스 카드에 글로우 애니메이션을 재생합니다.
    /// 쿨다운 게이지 만충 시 Shake는 CardView 내부에서 자동으로 실행됩니다.
    /// </summary>
    public void PlayCardPop(int index) { /* Shake는 CardView 내부 UpdateCooldown에서 자동 실행됩니다. */ }

    // ── Expanded 모드 ─────────────────────────────────────────────────────────

    /// <summary>Expanded 모드로 전환합니다. 게임이 일시정지됩니다.</summary>
    public void ExpandToView()
    {
        if (_isExpanded) return;
        _isExpanded    = true;
        Time.timeScale = 0f;
        dimBackground?.SetActive(true);
        ArrangeFlat();
        SlidePanel(TargetExpandedY(), animated: true);
    }

    /// <summary>Fan 모드로 복귀합니다. 게임이 재개됩니다.</summary>
    public void CollapseToFan()
    {
        if (!_isExpanded) return;
        _isExpanded    = false;
        Time.timeScale = 1f;
        dimBackground?.SetActive(false);
        ArrangeHand();
        SlidePanel(TargetFanY(), animated: true);
    }

    // ── 호버 / 선택 이벤트 (CardView에서 호출) ────────────────────────────────

    public void OnCardHoverEnter(CardView cv)
    {
        if (_selectedCard != null || _isExpanded) return;
        cv.SetHovered(true);
        cv.transform.SetAsLastSibling();
    }

    public void OnCardHoverExit(CardView cv)
    {
        if (_selectedCard != null || _isExpanded) return;
        cv.SetHovered(false);
        RefreshDepth();
    }

    /// <summary>클릭 시 선택 토글. 이미 선택된 카드면 해제합니다.</summary>
    public void OnCardClick(CardView cv)
    {
        if (_selectedCard == cv)
        {
            _selectedCard.SetSelected(false);
            _selectedCard = null;
            RefreshDepth();
        }
        else
        {
            if (_selectedCard != null) _selectedCard.SetSelected(false);
            _selectedCard = cv;
            cv.SetSelected(true);
            cv.transform.SetAsLastSibling();
        }
    }

    // ── 카드 생성 / 배치 ──────────────────────────────────────────────────────

    private void OnCardDataChanged(CardData _) => Refresh();

    private void Refresh()
    {
        foreach (var c in _cards)
            if (c != null) Destroy(c.gameObject);
        _cards.Clear();
        _selectedCard = null;

        if (cardPrefab == null)
        {
            Debug.LogError("[HandManager] cardPrefab이 설정되지 않았습니다.");
            return;
        }
        if (CardInventory.Instance == null) return;

        var cards       = CardInventory.Instance.Cards;
        int weaponIndex = -1;
        for (int i = 0; i < cards.Count; i++)
            if (cards[i].cardType == CardType.Weapon) { weaponIndex = i; break; }

        for (int i = 0; i < cards.Count; i++)
        {
            var go = Instantiate(cardPrefab, cardsParent);
            var cv = go.GetComponentInChildren<CardView>();
            if (cv == null)
            {
                Debug.LogError("[HandManager] cardPrefab에 CardView 컴포넌트가 없습니다.");
                Destroy(go);
                continue;
            }

            bool isActiveSlot = i == weaponIndex ||
                                (weaponIndex >= 0 && (i == weaponIndex - 1 || i == weaponIndex + 1));
            cv.Setup(cards[i], this, _weaponManager, isActiveSlot);
            _cards.Add(cv);
        }

        ArrangeHand();
    }

    private void ArrangeHand()
    {
        int count = _cards.Count;
        if (count == 0) return;

        if (_isExpanded) { ArrangeFlat(); return; }

        // 인접 카드 간 각도를 고정합니다.
        // 중앙 인덱스((count-1)/2)가 항상 각도 0(수직)에 위치합니다.
        int centerIdx = (count - 1) / 2;

        for (int i = 0; i < count; i++)
        {
            float angle = (i - centerIdx) * anglePerCard;
            _cards[i].SetBasePose(FanPosition(angle), -angle);
        }

        RefreshDepth();
    }

    // 진원 호 위의 좌표 (SlotsContainer 중심 기준, localPosition용 Vector3)
    private Vector3 FanPosition(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(
            Mathf.Sin(rad) * fanRadius,
            (Mathf.Cos(rad) - 1f) * fanRadius + handYOffset,
            0f
        );
    }

    private void ArrangeFlat()
    {
        int   count  = _cards.Count;
        float startX = -(count - 1) * flatSpacing * 0.5f;
        for (int i = 0; i < count; i++)
            _cards[i].SetBasePose(new Vector3(startX + i * flatSpacing, 0f, 0f), 0f);
    }

    public void RefreshDepth()
    {
        for (int i = 0; i < _cards.Count; i++)
            _cards[i].transform.SetSiblingIndex(i);
    }

    // ── 패널 슬라이드 ─────────────────────────────────────────────────────────

    private void ToggleExpanded()
    {
        if (_isExpanded) CollapseToFan();
        else             ExpandToView();
    }

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
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / slideDuration));
            _panelRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(startY, targetY, t));
            yield return null;
        }
        _panelRect.anchoredPosition = new Vector2(0f, targetY);
        _slideCoroutine = null;
    }

    // ── Y 위치 헬퍼 ───────────────────────────────────────────────────────────

    // 패널 높이의 30%를 화면 하단으로 숨겨 카드가 화면 밖에서 살짝 올라오는 효과
    private float TargetFanY() => -_panelHeight * 0.30f;

    private float TargetExpandedY()
    {
        var canvas = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        float screenH = canvas != null ? canvas.sizeDelta.y : 1080f;
        return screenH * expandedYRatio;
    }
}
