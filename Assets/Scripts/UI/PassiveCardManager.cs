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
    public int maxHandSize = 5;

    [Header("카드 슬롯 (Inspector에서 연결)")]
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
        // 슬롯 수가 최대 손 크기와 맞는지 확인
        if (cardSlots.Count != maxHandSize)
        {
            Debug.LogWarning($"카드 슬롯 수({cardSlots.Count})와 최대 손 크기({maxHandSize})가 다릅니다!");
        }

        // 시작 시 보유 카드를 자동으로 손에 배치
        AutoFillHand();

        RefreshCardPool();
    }

    /// <summary>
    /// 시작 시 보유 카드를 자동으로 슬롯에 배치
    /// </summary>
    private void AutoFillHand()
    {
        int slotIndex = 0;
        foreach (var card in ownedCards)
        {
            if (slotIndex >= cardSlots.Count) break;
            if (cardSlots[slotIndex] != null)
            {
                cardSlots[slotIndex].SetCard(card);
                Debug.Log($"{card.cardName}을(를) 슬롯 {slotIndex + 1}에 자동 배치");
            }
            slotIndex++;
        }
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

        // 시너지 재계산
        RecalculateSynergies();
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
    /// 새 카드를 획득 (보유 카드 풀에 추가)
    /// </summary>
    public void AcquireCard(CardData card)
    {
        if (card == null) return;
        if (ownedCards.Contains(card))
        {
            Debug.LogWarning($"{card.cardName}은 이미 보유 중입니다.");
            return;
        }

        ownedCards.Add(card);
        Debug.Log($"카드 획득: {card.cardName}");
        RefreshCardPool();
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
