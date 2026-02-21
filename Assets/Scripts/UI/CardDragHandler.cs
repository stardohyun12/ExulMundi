using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 동료 카드에 부착하여 드래그 앤 드롭으로 파티 슬롯에 배치.
/// CardDisplay, CanvasGroup과 함께 사용.
/// </summary>
[RequireComponent(typeof(CardDisplay))]
[RequireComponent(typeof(CanvasGroup))]
public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CardDisplay cardDisplay;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;

    private Transform originalParent;
    private int originalSiblingIndex;

    public CompanionData CompanionData => cardDisplay != null ? cardDisplay.companionData : null;

    void Awake()
    {
        cardDisplay = GetComponent<CardDisplay>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // 루트 캔버스 탐색
        Canvas c = GetComponentInParent<Canvas>();
        while (c != null && !c.isRootCanvas)
            c = c.transform.parent?.GetComponentInParent<Canvas>();
        rootCanvas = c;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (PartyManager.Instance == null) return;

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // 드래그 레이어로 이동 → 모든 UI 위에 표시
        transform.SetParent(PartyManager.Instance.DragLayer, true);
        transform.SetAsLastSibling();

        canvasGroup.alpha = 0.75f;
        canvasGroup.blocksRaycasts = false;

        // CardHover와 충돌 방지
        CardHover hover = GetComponent<CardHover>();
        if (hover != null) hover.enabled = false;

        PartyManager.Instance.IsDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rootCanvas == null) return;

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 worldPoint
        );
        rectTransform.position = worldPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        CardHover hover = GetComponent<CardHover>();
        if (hover != null) hover.enabled = true;

        if (PartyManager.Instance != null)
            PartyManager.Instance.IsDragging = false;

        // 슬롯에 배치되지 않았으면 원래 위치로 복귀
        if (PartyManager.Instance != null && transform.parent == PartyManager.Instance.DragLayer)
            ReturnToOrigin();
    }

    /// <summary>
    /// PartySlot이 드롭 성공 후 카드를 원래 목록으로 돌려보낼 때 호출
    /// </summary>
    public void ReturnToOrigin()
    {
        if (originalParent == null) return;
        transform.SetParent(originalParent, true);
        transform.SetSiblingIndex(originalSiblingIndex);
    }
}
