using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 보유 동료 목록과 파티 슬롯 관리.
/// 카드 드래그 앤 드롭으로 파티 편성. 항상 접근 가능한 토글 패널.
/// </summary>
public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }

    [Header("보유 동료")]
    public List<CompanionData> ownedCompanions = new List<CompanionData>();

    [Header("파티 슬롯 (Inspector에서 4개 연결)")]
    public List<PartySlot> slots = new List<PartySlot>();
    public int maxPartySize = 4;

    [Header("UI 참조")]
    public Transform cardListParent;   // 보유 동료 카드 목록 부모
    public GameObject cardPrefab;      // CardDisplay + CardDragHandler + CardHover 프리팹
    public Transform DragLayer;        // 드래그 중 카드가 올라갈 최상단 레이어
    public GameObject partyPanel;      // 파티 편성 패널 (토글 대상)

    /// <summary>드래그 진행 중 여부 (CardSwipe 스와이프 억제용)</summary>
    public bool IsDragging { get; set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // HorizontalLayoutGroup이 카드 너비를 기준으로 배치하도록 설정
        if (cardListParent != null)
        {
            var hlg = cardListParent.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.childControlWidth = true;
                hlg.childForceExpandWidth = false;
                hlg.childControlHeight = false;
            }
        }

        RefreshCardList();
    }

    // --- 패널 토글 ---

    public void TogglePanel()
    {
        if (partyPanel == null) return;
        partyPanel.SetActive(!partyPanel.activeSelf);
    }

    // --- 슬롯 배치 ---

    /// <summary>
    /// 동료를 특정 슬롯에 배치. 이미 다른 슬롯에 있으면 거기서 먼저 제거.
    /// </summary>
    public bool PlaceInSlot(CompanionData companion, PartySlot targetSlot)
    {
        if (companion == null || targetSlot == null) return false;
        if (targetSlot.Occupant == companion) return false;

        // 동일 동료가 다른 슬롯에 이미 있으면 제거
        foreach (var slot in slots)
        {
            if (slot != targetSlot && slot.Occupant == companion)
                slot.Clear();
        }

        Debug.Log($"{companion.companionName} → 슬롯 {slots.IndexOf(targetSlot) + 1}에 배치");
        return true;
    }

    /// <summary>
    /// 슬롯을 비움
    /// </summary>
    public void ClearSlot(PartySlot slot)
    {
        if (slot == null) return;
        if (slot.Occupant != null)
            Debug.Log($"{slot.Occupant.companionName} 파티에서 제거");
        slot.Clear();
    }

    // --- 파티 조회 ---

    /// <summary>
    /// 현재 편성된 동료 목록 반환
    /// </summary>
    public List<CompanionData> GetPartyData()
    {
        var party = new List<CompanionData>();
        foreach (var slot in slots)
        {
            if (slot != null && slot.Occupant != null)
                party.Add(slot.Occupant);
        }
        return party;
    }

    /// <summary>
    /// 파티를 BattleManager에 등록
    /// </summary>
    public void DeployParty()
    {
        if (BattleManager.Instance == null) return;
        foreach (var companion in GetPartyData())
            BattleManager.Instance.AddCompanionToParty(companion);
    }

    /// <summary>
    /// 랜덤 동료 1명을 파티에서 제거 (패널티용)
    /// </summary>
    public CompanionData RemoveRandomFromParty()
    {
        var occupiedSlots = slots.FindAll(s => s != null && s.Occupant != null);
        if (occupiedSlots.Count == 0) return null;

        int idx = Random.Range(0, occupiedSlots.Count);
        CompanionData removed = occupiedSlots[idx].Occupant;
        occupiedSlots[idx].Clear();
        Debug.Log($"패널티: {removed.companionName} 이탈");
        return removed;
    }

    // --- 카드 플레이 처리 ---

    /// <summary>
    /// 카드가 필드에 배치된 후 손 목록에서 제거
    /// </summary>
    public void OnCardPlayed(CompanionData data)
    {
        if (data == null) return;
        ownedCompanions.Remove(data);
        Debug.Log($"{data.companionName} 필드에 배치됨 → 손에서 제거");
    }

    // --- 카드 목록 갱신 ---

    private void RefreshCardList()
    {
        if (cardListParent == null || cardPrefab == null) return;

        foreach (Transform child in cardListParent)
            Destroy(child.gameObject);

        foreach (var companion in ownedCompanions)
        {
            GameObject card = Instantiate(cardPrefab, cardListParent);

            // HorizontalLayoutGroup에 카드 선호 크기를 알려줘 겹침 방지
            var le = card.AddComponent<LayoutElement>();
            le.preferredWidth = 160f;
            le.minWidth = 130f;

            CardDisplay display = card.GetComponent<CardDisplay>();
            if (display != null)
                display.Setup(companion);
            // CardDragHandler는 프리팹에 이미 붙어있어야 함
        }
    }
}
