using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 파티 편성 슬롯 하나. 드래그로 동료 카드를 배치받음.
/// 클릭으로 슬롯을 비울 수 있음.
/// </summary>
public class PartySlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Header("슬롯 UI")]
    public GameObject emptyState;        // 빈 슬롯 표시 (예: "비어있음" 텍스트)
    public GameObject occupiedState;     // 배치된 동료 표시 영역
    public TextMeshProUGUI companionNameText;
    public Image companionPortrait;

    [Header("배경 색상")]
    public Image backgroundImage;
    public Color emptyColor = new Color(0.25f, 0.25f, 0.25f, 0.85f);
    public Color occupiedColor = new Color(0.15f, 0.45f, 0.15f, 0.90f);

    public CompanionData Occupant { get; private set; }

    void Start()
    {
        UpdateVisual();
    }

    // --- IDropHandler ---

    public void OnDrop(PointerEventData eventData)
    {
        CardDragHandler drag = eventData.pointerDrag?.GetComponent<CardDragHandler>();
        if (drag == null) return;

        CompanionData incoming = drag.CompanionData;
        if (incoming == null) return;

        bool success = PartyManager.Instance.PlaceInSlot(incoming, this);
        if (success)
            drag.ReturnToOrigin();
    }

    // --- IPointerClickHandler ---

    public void OnPointerClick(PointerEventData eventData)
    {
        // 드래그 직후에도 Click이 발생할 수 있으므로 드래그 중이면 무시
        if (PartyManager.Instance != null && PartyManager.Instance.IsDragging) return;

        if (Occupant != null)
            PartyManager.Instance.ClearSlot(this);
    }

    // --- 슬롯 상태 변경 ---

    public void SetOccupant(CompanionData companion)
    {
        Occupant = companion;
        UpdateVisual();
    }

    public void Clear()
    {
        Occupant = null;
        UpdateVisual();
    }

    // --- 비주얼 갱신 ---

    private void UpdateVisual()
    {
        bool occupied = Occupant != null;

        if (emptyState != null) emptyState.SetActive(!occupied);
        if (occupiedState != null) occupiedState.SetActive(occupied);

        if (backgroundImage != null)
            backgroundImage.color = occupied ? occupiedColor : emptyColor;

        if (occupied)
        {
            if (companionNameText != null)
                companionNameText.text = Occupant.companionName;
            if (companionPortrait != null && Occupant.cardImage != null)
                companionPortrait.sprite = Occupant.cardImage;
        }
    }
}
