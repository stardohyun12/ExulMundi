using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 손에 든 패시브 카드 관리자.
/// 카드를 손에 들고만 있으면 자동으로 전투에 패시브 효과를 줌.
/// 카드 시너지 계산 및 적용.
/// </summary>
public class PassiveCardManager : MonoBehaviour
{
    public static PassiveCardManager Instance { get; private set; }

    [Header("손 크기 제한")]
    [Tooltip("손에 들 수 있는 최대 카드 수")]
    public int maxHandSize = 10;

    [Header("슬롯 생성")]
    public GameObject cardSlotTemplate;     // 템플릿으로 사용할 슬롯 (씬에 있는 것)
    public Transform handArea;              // 슬롯이 생성될 부모 (HandArea)

    [Header("카드 슬롯 (동적 생성)")]
    public List<CardSlot> cardSlots = new List<CardSlot>();

    [Header("보유 카드 풀")]
    [Tooltip("플레이어가 획득한 모든 카드")]
    public List<CardData> ownedCards = new List<CardData>();

    [Header("UI 참조")]
    public Transform cardPoolParent;    // 보유 카드 목록 부모
    public GameObject cardPrefab;       // 카드 UI 프리팹
    public Transform dragLayer;         // 드래그 중 카드가 올라갈 레이어
    public GameObject handPanel;        // 손패 패널 (토글용)

    [Header("시너지 시스템")]
    public CardSynergySystem synergySystem;

    /// <summary>드래그 진행 중 여부 (스와이프 억제용)</summary>
    public bool IsDragging { get; set; }

    // 현재 손에 든 카드들
    private List<CardData> _currentHand = new List<CardData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        CreateSlotsForCards();
        AutoFillHand();
        RefreshCardPool();
        StartCoroutine(RebuildLayoutNextFrame());
    }

    private System.Collections.IEnumerator RebuildLayoutNextFrame()
    {
        yield return null;
        RefreshLayoutSpacing();
        Debug.Log("Layout 리빌드 완료!");
    }

    /// <summary>
    /// 보유 카드 개수만큼 슬롯 동적 생성
    /// </summary>
    private void CreateSlotsForCards()
    {
        if (cardSlotTemplate == null || handArea == null)
        {
            Debug.LogError("Template 또는 HandArea가 null!");
            return;
        }

        int cardCount = Mathf.Min(ownedCards.Count, maxHandSize);
        if (cardCount == 0) return;

        // 디버그: 현재 cardSlots 상태 출력
        Debug.Log($"[CreateSlotsForCards] cardSlots.Count = {cardSlots.Count}");
        for (int i = 0; i < cardSlots.Count; i++)
        {
            Debug.Log($"  cardSlots[{i}]: {(cardSlots[i] != null ? cardSlots[i].name : "null")}");
        }

        // 이미 슬롯이 정상 생성되어 있으면 재생성하지 않음
        if (cardSlots.Count >= cardCount && cardSlots.Count > 0 && cardSlots[0] != null)
        {
            Debug.Log($"슬롯이 이미 {cardSlots.Count}개 존재 - 재생성 생략");
            return;
        }

        Debug.Log($"슬롯 {cardCount}개 생성 시작");

        cardSlotTemplate.SetActive(false);
        cardSlots.Clear(); // 리스트 초기화

        // 기존 자식 모두 삭제 (템플릿 제외)
        List<GameObject> toDelete = new List<GameObject>();
        foreach (Transform child in handArea)
        {
            if (child.gameObject != cardSlotTemplate)
                toDelete.Add(child.gameObject);
        }
        foreach (var obj in toDelete)
            DestroyImmediate(obj);

        // 슬롯 생성
        for (int i = 0; i < cardCount; i++)
        {
            GameObject slot = Instantiate(cardSlotTemplate, handArea);
            slot.name = $"CardSlot{i + 1}";
            slot.SetActive(true);
            
            // LayoutElement 설정
            LayoutElement le = slot.GetComponent<LayoutElement>();
            if (le == null) le = slot.AddComponent<LayoutElement>();
            le.minWidth = 150;
            le.minHeight = 200;
            le.preferredWidth = 150;
            le.preferredHeight = 200;
            
            CardSlot cs = slot.GetComponent<CardSlot>();
            if (cs != null) cardSlots.Add(cs);
        }

        Debug.Log($"{cardSlots.Count}개 슬롯 생성 완료");
    }

    /// <summary>
    /// 시작 시 보유 카드를 자동으로 슬롯에 배치
    /// </summary>
    private void AutoFillHand()
    {
        Debug.Log($"[AutoFillHand] 시작 - 보유 카드: {ownedCards.Count}개, 슬롯: {cardSlots.Count}개");
        
        int slotIndex = 0;
        foreach (var card in ownedCards)
        {
            if (slotIndex >= cardSlots.Count) break;
            if (cardSlots[slotIndex] != null)
            {
                Debug.Log($"[AutoFillHand] {card.cardName} → cardSlots[{slotIndex}] (슬롯: {cardSlots[slotIndex].name})");
                cardSlots[slotIndex].SetCard(card);
                Debug.Log($"[AutoFillHand] {card.cardName}을(를) 슬롯 {slotIndex + 1}에 배치 완료");
            }
            slotIndex++;
        }
        
        Debug.Log($"[AutoFillHand] 완료 - 현재 손패: {_currentHand.Count}개");
    }

    // ═══════════════════════════════════════
    // 패널 토글
    // ═══════════════════════════════════════

    public void TogglePanel()
    {
        if (handPanel == null) return;
        handPanel.SetActive(!handPanel.activeSelf);
    }

    // ═══════════════════════════════════════
    // 카드 배치 / 제거
    // ═══════════════════════════════════════

    /// <summary>
    /// 카드를 특정 슬롯에 배치. 이미 다른 슬롯에 있으면 거기서 먼저 제거.
    /// </summary>
    public bool PlaceCardInSlot(CardData card, CardSlot targetSlot)
    {
        Debug.Log($"[PlaceCardInSlot] 호출됨 - 카드: {card?.cardName}, 슬롯: {targetSlot?.name}");

        if (card == null || targetSlot == null)
        {
            Debug.LogWarning($"[PlaceCardInSlot] card 또는 targetSlot이 null입니다!");
            return false;
        }

        if (targetSlot.OccupantCard == card)
        {
            Debug.Log($"[PlaceCardInSlot] 이미 같은 카드가 슬롯에 있습니다: {card.cardName}");
            return false;
        }

        // 동일 카드가 다른 슬롯에 이미 있으면 제거
        foreach (var slot in cardSlots)
        {
            if (slot != targetSlot && slot.OccupantCard == card)
                slot.Clear();
        }

        // 손에 추가
        if (!_currentHand.Contains(card))
        {
            _currentHand.Add(card);
            Debug.Log($"[PlaceCardInSlot] _currentHand에 추가: {card.cardName}, 현재 손 크기: {_currentHand.Count}");
        }
        else
        {
            Debug.Log($"[PlaceCardInSlot] 이미 _currentHand에 있음: {card.cardName}");
        }

        Debug.Log($"{card.cardName} → 슬롯 {cardSlots.IndexOf(targetSlot) + 1}에 배치");

        // 시너지 재계산
        RecalculateSynergies();

        return true;
    }

    /// <summary>
    /// 슬롯을 비움
    /// </summary>
    public void ClearSlot(CardSlot slot)
    {
        if (slot == null) return;

        if (slot.OccupantCard != null)
        {
            Debug.Log($"{slot.OccupantCard.cardName} 손에서 제거");
            _currentHand.Remove(slot.OccupantCard);
        }

        slot.Clear();
        RecalculateSynergies();
    }

    /// <summary>
    /// 슬롯 GameObject를 삭제하고 카드 개수에 맞춰 간격 재조정 (버리기용)
    /// </summary>
    public void RemoveSlotAndCard(CardSlot slot)
    {
        if (slot == null) return;

        CardData card = slot.OccupantCard;
        if (card != null) _currentHand.Remove(card);

        cardSlots.Remove(slot);
        Destroy(slot.gameObject);

        StartCoroutine(RefreshSpacingNextFrame());
        RecalculateSynergies();
    }

    private System.Collections.IEnumerator RefreshSpacingNextFrame()
    {
        yield return null;
        RefreshLayoutSpacing();
    }

    /// <summary>
    /// 남은 카드 개수에 따라 HorizontalLayoutGroup spacing 재계산
    /// </summary>
    public void RefreshLayoutSpacing()
    {
        if (handArea == null) return;

        var hlg = handArea.GetComponent<HorizontalLayoutGroup>();
        var rt  = handArea.GetComponent<RectTransform>();
        if (hlg == null || rt == null) return;

        int count = cardSlots.Count;
        if (count == 0) return;

        const float cardWidth = 150f;
        float padding     = hlg.padding.left + hlg.padding.right;
        float areaWidth   = rt.rect.width;
        float usedByCards = cardWidth * count;
        float freeSpace   = areaWidth - padding - usedByCards;

        hlg.spacing = count > 1
            ? Mathf.Clamp(freeSpace / (count - 1), 8f, 120f)
            : 0f;

        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    // ═══════════════════════════════════════
    // 손 조회
    // ═══════════════════════════════════════

    /// <summary>
    /// 현재 손에 든 카드 목록 반환
    /// </summary>
    public List<CardData> GetCurrentHand()
    {
        Debug.Log($"[PassiveCardManager] _currentHand 카드 수: {_currentHand.Count}");
        foreach (var card in _currentHand)
        {
            Debug.Log($"  - {card.cardName}");
        }
        return new List<CardData>(_currentHand);
    }

    /// <summary>
    /// 손이 가득 찼는지 확인
    /// </summary>
    public bool IsHandFull()
    {
        return _currentHand.Count >= maxHandSize;
    }

    /// <summary>
    /// 특정 카드가 손에 있는지 확인
    /// </summary>
    public bool HasCardInHand(CardData card)
    {
        return _currentHand.Contains(card);
    }

    // ═══════════════════════════════════════
    // 카드 획득 / 제거
    // ═══════════════════════════════════════

    /// <summary>
    /// 새 카드를 획득 — ownedCards에 추가하고 슬롯도 생성
    /// </summary>
    public void AcquireCard(CardData card)
    {
        if (card == null) return;

        ownedCards.Add(card);
        Debug.Log($"카드 획득: {card.cardName}");

        // 손에 자리가 있으면 바로 슬롯 생성 후 배치
        if (cardSlots.Count < maxHandSize)
            AddCardSlot(card);

        RefreshCardPool();
    }

    /// <summary>
    /// 슬롯 1개를 동적으로 추가하고 card를 배치
    /// </summary>
    private void AddCardSlot(CardData card)
    {
        if (cardSlotTemplate == null || handArea == null)
        {
            Debug.LogError("[PassiveCardManager] cardSlotTemplate 또는 handArea가 null입니다! Inspector에서 연결하세요.");
            return;
        }

        // 비활성 템플릿에서 복제
        bool wasActive = cardSlotTemplate.activeSelf;
        cardSlotTemplate.SetActive(false);

        GameObject slotGO = Instantiate(cardSlotTemplate, handArea);
        slotGO.name = $"CardSlot{cardSlots.Count + 1}";
        slotGO.SetActive(true);

        cardSlotTemplate.SetActive(wasActive);

        LayoutElement le = slotGO.GetComponent<LayoutElement>();
        if (le == null) le = slotGO.AddComponent<LayoutElement>();
        le.minWidth        = 150;
        le.minHeight       = 200;
        le.preferredWidth  = 150;
        le.preferredHeight = 200;

        CardSlot cs = slotGO.GetComponent<CardSlot>();
        if (cs != null)
        {
            cardSlots.Add(cs);

            // OccupantCard를 직접 설정하고 currentHand에 추가
            // (SetCard → PlaceCardInSlot을 거치면 중복 체크로 skip될 수 있음)
            cs.SetCard(card);

            if (!_currentHand.Contains(card))
                _currentHand.Add(card);
        }

        StartCoroutine(RefreshSpacingNextFrame());
        Debug.Log($"[PassiveCardManager] 슬롯 추가 완료: {card.cardName} (현재 손패 {_currentHand.Count}장)");
    }

    /// <summary>
    /// 랜덤으로 손에서 카드 1장 제거 (패널티용)
    /// </summary>
    public CardData RemoveRandomFromHand()
    {
        if (_currentHand.Count == 0) return null;

        int idx = Random.Range(0, _currentHand.Count);
        CardData removed = _currentHand[idx];

        // 해당 카드가 있는 슬롯 찾아서 비우기
        foreach (var slot in cardSlots)
        {
            if (slot.OccupantCard == removed)
            {
                slot.Clear();
                break;
            }
        }

        _currentHand.RemoveAt(idx);
        Debug.Log($"패널티: {removed.cardName} 손에서 제거됨");

        RecalculateSynergies();
        return removed;
    }

    // ═══════════════════════════════════════
    // 시너지 시스템
    // ═══════════════════════════════════════

    /// <summary>
    /// 현재 손에 든 카드들의 시너지를 재계산
    /// </summary>
    private void RecalculateSynergies()
    {
        if (synergySystem == null) return;

        synergySystem.CalculateSynergies(_currentHand);
    }

    /// <summary>
    /// 현재 활성화된 시너지 효과 목록 반환
    /// </summary>
    public List<SynergyEffect> GetActiveSynergies()
    {
        if (synergySystem == null) return new List<SynergyEffect>();
        return synergySystem.GetActiveSynergies();
    }

    // ═══════════════════════════════════════
    // 카드 사용
    // ═══════════════════════════════════════

    /// <summary>
    /// 카드 사용 시 호출 (액티브 효과 발동 후)
    /// </summary>
    public void OnCardUsed(CardData card)
    {
        if (card == null) return;

        // 손에서 제거
        _currentHand.Remove(card);

        Debug.Log($"{card.cardName} 사용됨 - 손에서 제거");

        // 시너지 재계산
        RecalculateSynergies();
    }

    // ═══════════════════════════════════════
    // 카드 풀 UI 갱신
    // ═══════════════════════════════════════

    private void RefreshCardPool()
    {
        if (cardPoolParent == null || cardPrefab == null) return;

        // 기존 카드 UI 제거
        foreach (Transform child in cardPoolParent)
            Destroy(child.gameObject);

        // 보유 카드 목록 생성
        foreach (var card in ownedCards)
        {
            GameObject cardGO = Instantiate(cardPrefab, cardPoolParent);

            // 레이아웃 설정
            var le = cardGO.GetComponent<LayoutElement>();
            if (le == null) le = cardGO.AddComponent<LayoutElement>();
            le.preferredWidth = 160f;
            le.minWidth = 130f;

            // 카드 표시
            CardDisplay display = cardGO.GetComponent<CardDisplay>();
            if (display != null)
                display.SetupCard(card);

            // 드래그 핸들러 설정 (필요시)
            // CardDragHandler는 프리팹에 이미 있어야 함
        }
    }
}
