using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 손패 패널을 관리합니다.
///
/// CardHouse 방식 참고 — 가상 원 중심을 화면 아래에 두고 카드를 원호 위에 배치합니다.
///   각 카드의 pivot = (0.5, 0) → 카드 하단 중앙이 원호 위의 점
///   x = sin(angle) × arcRadius
///   y = arcRadius × (cos(angle) − 1) + uplift
///   rotation = -angle  (왼쪽은 왼쪽으로, 오른쪽은 오른쪽으로)
///
/// Fan 모드    : 카드가 화면 하단에서 부채꼴로 펼쳐집니다.
/// Expanded 모드 : '손패 보기' 버튼으로 진입. 카드 일렬 + 게임 일시정지.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class HandManager : MonoBehaviour
{
    // ── 에디터 미리보기 (빌드에 영향 없음) ───────────────────────────────────────
#if UNITY_EDITOR
    [HideInInspector] public int editorPreviewCount = 5;
#endif

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("카드 프리팹 / 부모")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform  cardsParent;

    [Header("부채꼴 설정")]
    [Tooltip("최소 호 반지름(px). 계산 반지름이 이보다 작을 때 사용됩니다.")]
    [SerializeField] private float arcRadius     = 300f;
    [Tooltip("인접 카드 중심 간 수평 간격(px).")]
    [SerializeField] private float cardSpacing   = 140f;
    [Tooltip("카드가 3장 이상일 때 사용하는 최대 반각도(°).")]
    [SerializeField] private float maxHalfAngle       = 20f;
    [Tooltip("카드가 정확히 2장일 때 사용하는 최대 반각도(°). 좁게 설정하면 자연스럽게 모입니다.")]
    [SerializeField] private float maxHalfAngleTwoCards = 5.5f;
    [Tooltip("가장 낮은(끝) 카드의 Y 위치(px). 어떤 카드도 이 값보다 아래에 배치되지 않습니다.")]
    [SerializeField] private float cardUplift    = 80f;

    [Header("호버 / 선택")]
    [SerializeField] private float hoverRiseY    = 60f;
    [SerializeField] private float hoverScale    = 1.12f;
    [SerializeField] private float selectedRiseY = 130f;

    [Header("Expanded 모드")]
    [SerializeField] private GameObject dimBackground;
    [SerializeField] private Button     viewHandButton;
    [SerializeField] private float      flatSpacing    = 180f;
    [SerializeField] private float      expandedYRatio = 0.40f;
    [SerializeField] private float      slideDuration  = 0.28f;

    // ── 프로퍼티 ──────────────────────────────────────────────────────────────

    public float HoverRiseY    => hoverRiseY;
    public float HoverScale    => hoverScale;
    public float SelectedRiseY => selectedRiseY;
    public bool  IsExpanded    => _isExpanded;

    // ── 내부 상태 ─────────────────────────────────────────────────────────────

    private RectTransform           _panelRect;
    private bool                    _isExpanded;
    private Coroutine               _slideCoroutine;
    private readonly List<CardView> _cards = new();
    private CardView                _selectedCard;
    private WeaponManager           _weaponManager;

    // ── Unity 생명주기 ────────────────────────────────────────────────────────

    private void Awake()
    {
        _panelRect = GetComponent<RectTransform>();
    }

    /// <summary>Inspector 값이 바뀌면 Play 모드에서는 즉시 카드 배치를 갱신합니다.</summary>
    private void OnValidate()
    {
        if (Application.isPlaying && _panelRect != null && _cards.Count > 0)
            ArrangeHand();
    }

    private void Start()
    {
        _weaponManager = FindAnyObjectByType<WeaponManager>();

        viewHandButton?.onClick.AddListener(ToggleExpanded);
        dimBackground?.SetActive(false);

        // 패널을 화면 하단에 붙입니다 (anchoredPosition.y = 0).
        // 카드 위치는 cardUplift로 조절합니다.
        _panelRect.anchoredPosition = new Vector2(0f, 0f);

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

    public void PlayCardPop(int index) { }

    // ── Expanded 모드 ─────────────────────────────────────────────────────────

    /// <summary>Expanded 모드로 전환합니다.</summary>
    public void ExpandToView()
    {
        if (_isExpanded) return;
        _isExpanded    = true;
        Time.timeScale = 0f;
        dimBackground?.SetActive(true);
        ArrangeFlat();
        SlidePanel(TargetExpandedY(), animated: true);
    }

    /// <summary>Fan 모드로 복귀합니다.</summary>
    public void CollapseToFan()
    {
        if (!_isExpanded) return;
        _isExpanded    = false;
        Time.timeScale = 1f;
        dimBackground?.SetActive(false);
        ArrangeHand();
        SlidePanel(0f, animated: true);
    }

    // ── 호버 / 선택 이벤트 ────────────────────────────────────────────────────

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

    /// <summary>클릭 시 선택 토글.</summary>
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

    /// <summary>
    /// 카드 수에 따라 호의 곡률이 자동으로 조정되는 원호 배치.
    /// halfAngle = maxHalfAngle / count 이므로 카드가 많을수록 호가 수평에 가까워집니다.
    /// 반지름은 cardSpacing을 유지하도록 halfAngle에서 역산합니다.
    /// </summary>
    public void ArrangeHand()
    {
        int count = _cards.Count;
        if (count == 0) return;
        if (_isExpanded) { ArrangeFlat(); return; }

        // 2장일 때는 별도 반각도를 사용해 자연스러운 간격을 유지합니다.
        float activeMaxHalf = count == 2 ? maxHalfAngleTwoCards : maxHalfAngle;
        float halfAngle = count > 1 ? activeMaxHalf / (count + 1) : 0f;
        float halfRad   = halfAngle * Mathf.Deg2Rad;

        // 인접 카드 간 각도 간격에서 cardSpacing을 유지하는 반지름을 역산합니다.
        float anglePerCardRad = count > 1 ? halfRad * 2f / (count - 1) : 0f;
        float dynamicRadius   = (anglePerCardRad > 0.001f)
            ? cardSpacing / (2f * Mathf.Sin(anglePerCardRad * 0.5f))
            : arcRadius;
        dynamicRadius = Mathf.Max(dynamicRadius, arcRadius);

        // cardUplift = 끝(가장 낮은) 카드의 Y 위치. 중앙 카드는 yDrop만큼 더 위에 있습니다.
        float yDrop = dynamicRadius * (1f - Mathf.Cos(halfRad));

        for (int i = 0; i < count; i++)
        {
            float t     = count > 1 ? (float)i / (count - 1) : 0.5f;
            float angle = count > 1 ? Mathf.Lerp(-halfAngle, halfAngle, t) : 0f;
            float rad   = angle * Mathf.Deg2Rad;

            float x   = dynamicRadius * Mathf.Sin(rad);
            float y   = cardUplift + yDrop + dynamicRadius * (Mathf.Cos(rad) - 1f);
            float rot = -angle;

            _cards[i].SetBasePose(new Vector3(x, y, 0f), rot);
        }

        RefreshDepth();
    }

    private void ArrangeFlat()
    {
        int count = _cards.Count;
        if (count == 0) return;

        var   canvasRt    = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        float screenW     = canvasRt != null ? canvasRt.sizeDelta.x : 1920f;
        float cardWidth   = 160f;
        float usableWidth = screenW * 0.92f;

        float spacing = count > 1
            ? Mathf.Min(flatSpacing, (usableWidth - cardWidth) / (count - 1))
            : flatSpacing;

        float startX = -(count - 1) * spacing * 0.5f;
        for (int i = 0; i < count; i++)
            _cards[i].SetBasePose(new Vector3(startX + i * spacing, 0f, 0f), 0f);
    }

    public void RefreshDepth()
    {
        for (int i = 0; i < _cards.Count; i++)
            _cards[i].transform.SetSiblingIndex(i);
    }

    /// <summary>CardView가 드래그 앤 드롭에서 자신의 인덱스를 조회할 때 사용합니다.</summary>
    public int GetCardIndex(CardView cv) => _cards.IndexOf(cv);

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

    private float TargetExpandedY()
    {
        var   canvas  = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        float screenH = canvas != null ? canvas.sizeDelta.y : 1080f;
        return screenH * expandedYRatio;
    }
}
