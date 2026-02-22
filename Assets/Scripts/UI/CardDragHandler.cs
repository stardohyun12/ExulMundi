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
    private bool isPlayed;

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
        if (isPlayed || PartyManager.Instance == null) return;

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
        if (isPlayed || rootCanvas == null) return;

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
        if (PartyManager.Instance != null)
            PartyManager.Instance.IsDragging = false;

        // 이미 슬롯에 배치됐으면 복귀 처리 불필요
        if (isPlayed) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        CardHover hover = GetComponent<CardHover>();
        if (hover != null) hover.enabled = true;

        // 슬롯에 배치되지 않았으면 원래 위치로 복귀
        ReturnToOrigin();
    }

    /// <summary>
    /// 슬롯에 카드를 영구 배치. 이후 드래그 불가 (하스스톤 방식).
    /// </summary>
    public void ConfirmPlay(Transform slotTransform)
    {
        isPlayed = true;

        // 슬롯의 자식으로 reparent
        transform.SetParent(slotTransform, false);

        // 슬롯에 꽉 차도록 stretch
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        CardHover hover = GetComponent<CardHover>();
        if (hover != null) hover.enabled = true;

        // 이후 드래그 불가
        this.enabled = false;

        PartyManager.Instance.OnCardPlayed(CompanionData);
    }

    /// <summary>
    /// 슬롯 배치 실패 시 원래 위치로 복귀
    /// </summary>
    public void ReturnToOrigin()
    {
        if (originalParent == null) return;
        transform.SetParent(originalParent, true);
        transform.SetSiblingIndex(originalSiblingIndex);
    }
}
