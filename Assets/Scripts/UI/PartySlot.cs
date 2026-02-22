using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 파티 편성 슬롯 하나. 드래그로 동료 카드를 배치받음.
/// 한 번 배치된 카드는 되돌릴 수 없음 (하스스톤 방식).
/// </summary>
public class PartySlot : MonoBehaviour, IDropHandler
{
    [Header("슬롯 UI")]
    public GameObject emptyState;  // 빈 슬롯 표시 (예: "비어있음" 텍스트)

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

        // 슬롯이 차있으면 드롭 무시
        if (Occupant != null) return;

        Occupant = incoming;
        UpdateVisual();
        drag.ConfirmPlay(transform);
    }

    // --- 슬롯 상태 변경 ---

    public void Clear()
    {
        Occupant = null;
        UpdateVisual();
    }

    // --- 비주얼 갱신 ---

    private void UpdateVisual()
    {
        if (emptyState != null)
            emptyState.SetActive(Occupant == null);
    }
}
