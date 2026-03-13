using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// HandSlot 프리팹에 붙어 드래그 앤 드롭으로 카드 순서를 변경합니다.
/// 짧은 클릭은 CardDetailPopup을 엽니다.
/// HandUI.Refresh()에서 Initialize()로 초기화합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class HandSlotDrag : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    private static readonly Color HighlightColor  = new(1f,  0.95f, 0.2f,  1f);
    private static readonly Color WeaponColor     = new(1f,  0.75f, 0.1f,  1f);
    private static readonly Color AccessoryColor  = new(0.6f, 0.7f, 0.85f, 1f);

    private int             _index;
    private HandUI          _handUI;
    private CardData        _card;
    private RectTransform   _rectTransform;
    private CanvasGroup     _canvasGroup;
    private Canvas          _canvas;
    private Transform       _originalParent;
    private Vector2         _originalPosition;
    private Image           _highlightBorder;
    private Color           _typeColor;

    /// <summary>슬롯 인덱스, 소속 HandUI, 카드 데이터를 설정합니다.</summary>
    public void Initialize(int index, HandUI handUI)
    {
        _index         = index;
        _handUI        = handUI;
        _rectTransform = GetComponent<RectTransform>();
        _canvas        = GetComponentInParent<Canvas>();

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        var borderTransform = transform.Find("ActiveBorder");
        if (borderTransform != null)
            _highlightBorder = borderTransform.GetComponent<Image>();
    }

    /// <summary>클릭 시 팝업에 표시할 카드 데이터를 설정합니다.</summary>
    public void SetCard(CardData card) => _card = card;

    /// <summary>카드 타입에 따른 기본 테두리 색상을 설정합니다.</summary>
    public void SetTypeColor(CardType cardType)
    {
        if (_highlightBorder == null) return;
        _typeColor               = cardType == CardType.Weapon ? WeaponColor : AccessoryColor;
        _highlightBorder.color   = _typeColor;
        _highlightBorder.enabled = true;
    }

    /// <summary>무기 카드 인접 여부에 따라 하이라이트 색상으로 전환합니다.</summary>
    public void SetActiveHighlight(bool active)
    {
        if (_highlightBorder == null) return;
        _highlightBorder.color = active ? HighlightColor : _typeColor;
    }

    // ── IPointerClickHandler ───────────────────────────────────────────────

    /// <summary>
    /// 드래그 없이 클릭했을 때 카드 상세 팝업을 엽니다.
    /// Unity EventSystem은 드래그 발생 시 OnPointerClick을 호출하지 않으므로 별도 플래그 불필요.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        CardDetailPopup.Instance?.Show(_card);
    }

    // ── IBeginDragHandler ──────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalParent   = transform.parent;
        _originalPosition = _rectTransform.anchoredPosition;

        // Canvas 기준 월드 위치를 먼저 기록한 뒤 부모를 변경합니다.
        // 부모 변경 후에도 화면 위치가 유지되도록 worldPositionStays=true를 사용합니다.
        transform.SetParent(_canvas.transform, worldPositionStays: true);
        transform.SetAsLastSibling();

        _canvasGroup.alpha          = 0.7f;
        _canvasGroup.blocksRaycasts = false;
    }

    // ── IDragHandler ───────────────────────────────────────────────────────

    public void OnDrag(PointerEventData eventData)
    {
        _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
    }

    // ── IEndDragHandler ────────────────────────────────────────────────────

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.alpha          = 1f;
        _canvasGroup.blocksRaycasts = true;

        // 원래 부모로 돌아갈 때 worldPositionStays=true로 복귀하면
        // anchoredPosition이 의도치 않게 바뀌므로, 복귀 후 원래 위치를 직접 복원합니다.
        transform.SetParent(_originalParent, worldPositionStays: false);
        _rectTransform.anchoredPosition = _originalPosition;
    }

    // ── IDropHandler ───────────────────────────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        var dragged = eventData.pointerDrag.GetComponent<HandSlotDrag>();
        if (dragged == null || dragged == this) return;

        _handUI.OnSlotDropped(dragged._index, _index);
    }
}
