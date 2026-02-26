using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 패시브 카드 슬롯 하나.
/// 드래그로 카드를 배치받고, 슬롯에 있는 카드는 자동으로 패시브 효과를 줌.
/// 클릭하면 카드를 사용 (액티브 효과 발동 후 카드 소모)
/// Hover 시 위로 이동, 우클릭 시 확대
/// v2.0
/// </summary>
public class CardSlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("슬롯 UI")]
    public GameObject emptyState;           // 빈 슬롯 표시
    public GameObject cardVisual;           // 카드가 배치되면 표시할 비주얼
    public Image cardImage;                 // 카드 아트
    public TextMeshProUGUI cardNameText;    // 카드 이름 (TextMeshProUGUI)

    [Header("시너지 표시")]
    public Image synergyGlow;               // 시너지 활성화 시 빛나는 효과
    public Color synergyColor = Color.yellow;

    [Header("Hover 설정")]
    public float hoverMoveDistance = 20f;   // Hover 시 위로 올라가는 거리
    public float hoverAnimSpeed = 10f;      // 애니메이션 속도

    [Header("카드 확대 UI")]
    public CardDetailUI cardDetailUI;       // Inspector에서 연결

    public CardData OccupantCard { get; private set; }

    private RectTransform _rectTransform;
    private Vector2 _originalAnchoredPos;
    private Vector2 _targetAnchoredPos;
    private bool _isHovering;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        
        // TextMeshProUGUI 자동 찾기
        if (cardNameText == null)
        {
            cardNameText = GetComponentInChildren<TextMeshProUGUI>();
            if (cardNameText != null)
            {
                Debug.Log($"[CardSlot {name}] TextMeshProUGUI 자동 연결: {cardNameText.name}");
            }
            else
            {
                Debug.LogWarning($"[CardSlot {name}] TextMeshProUGUI를 찾을 수 없습니다!");
            }
        }
        
        // CardDetailUI 자동 찾기 (비활성화된 것도 포함)
        if (cardDetailUI == null)
        {
            cardDetailUI = FindObjectOfType<CardDetailUI>(true);
        }
    }

    void Start()
    {
        // Hover 시스템은 localScale로 변경 (위치 변경하지 않음)
        UpdateVisual();
    }

    void Update()
    {
        // Hover 애니메이션 - localScale 사용 (LayoutGroup과 호환)
        if (_rectTransform != null)
        {
            float targetScale = _isHovering ? 1.1f : 1.0f;
            _rectTransform.localScale = Vector3.Lerp(
                _rectTransform.localScale,
                Vector3.one * targetScale,
                Time.deltaTime * hoverAnimSpeed
            );
        }
    }

    // ═══════════════════════════════════════
    // IDropHandler
    // ═══════════════════════════════════════

    public void OnDrop(PointerEventData eventData)
    {
        // TODO: CardDragHandler 연동 구현
        Debug.Log($"카드 드롭 시도 (슬롯: {gameObject.name})");
    }

    // ═══════════════════════════════════════
    // IPointerClickHandler - 카드 사용
    // ═══════════════════════════════════════

    public void OnPointerClick(PointerEventData eventData)
    {
        // 빈 슬롯이면 무시
        if (OccupantCard == null) return;

        // 카드 선택 모드일 때 (SimpleWorldManager)
        if (SimpleWorldManager.Instance != null && SimpleWorldManager.Instance.IsSelectingCard)
        {
            SimpleWorldManager.Instance.OnCardSelected(this);
            return;
        }

        // 우클릭: 카드 확대
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ShowCardDetail();
            return;
        }

        // 좌클릭: 카드 사용
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 전투 중이 아니면 사용 불가
            if (BattleManager.Instance == null || !BattleManager.Instance.IsBattleActive)
            {
                Debug.Log("전투 중에만 카드를 사용할 수 있습니다.");
                return;
            }

            Debug.Log($"카드 사용: {OccupantCard.cardName}");
            UseCard();
        }
    }

    // ═══════════════════════════════════════
    // IPointerEnterHandler / IPointerExitHandler - Hover
    // ═══════════════════════════════════════

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (OccupantCard == null) return;
        _isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
    }

    /// <summary>
    /// 카드 사용 - 액티브 효과 발동 후 카드 소모
    /// </summary>
    private void UseCard()
    {
        Debug.Log("=== UseCard() 호출됨 ===");
        
        if (OccupantCard == null)
        {
            Debug.LogError("UseCard: OccupantCard is NULL!");
            return;
        }

        CardData cardToUse = OccupantCard;
        Debug.Log($">>> 카드 사용 시작: {cardToUse.cardName} <<<");

        // BattleManager 확인
        if (BattleManager.Instance == null)
        {
            Debug.LogError(">>> BattleManager.Instance is NULL!");
            return;
        }
        
        Debug.Log(">>> BattleManager OK");

        // HeroUnit 확인
        if (BattleManager.Instance.HeroUnit == null)
        {
            Debug.LogError(">>> HeroUnit is NULL!");
            return;
        }
        
        Debug.Log($">>> HeroUnit OK - ATK: {BattleManager.Instance.HeroUnit.EffectiveATK}");

        // Enemy 확인
        if (BattleManager.Instance.CurrentEnemy == null)
        {
            Debug.LogError(">>> CurrentEnemy is NULL!");
            return;
        }
        
        Debug.Log($">>> CurrentEnemy OK: {BattleManager.Instance.CurrentEnemy.name}");
        Debug.Log($">>> 카드 effectType: {cardToUse.effectType}, effectValue: {cardToUse.effectValue}");

        // 적용 전 HP
        int hpBefore = BattleManager.Instance.CurrentEnemy.CurrentHP;
        Debug.Log($">>> Dummy HP (사용 전): {hpBefore}");

        // 액티브 효과 적용!
        Debug.Log(">>> CardEffectApplier.Apply 호출!");
        CardEffectApplier.Apply(
            cardToUse, 
            BattleManager.Instance.HeroUnit, 
            BattleManager.Instance.CurrentEnemy
        );

        // 적용 후 HP
        int hpAfter = BattleManager.Instance.CurrentEnemy.CurrentHP;
        Debug.Log($">>> Dummy HP (사용 후): {hpAfter}");
        Debug.Log($">>> 실제 데미지: {hpBefore - hpAfter}");

        // 카드 소모 - 슬롯 GameObject 삭제 + 간격 재조정
        if (PassiveCardManager.Instance != null)
        {
            PassiveCardManager.Instance.ownedCards.Remove(cardToUse);
            PassiveCardManager.Instance.OnCardUsed(cardToUse);
            PassiveCardManager.Instance.RemoveSlotAndCard(this);
        }

        Debug.Log($">>> {cardToUse.cardName} 사용 완료!");
    }

    /// <summary>
    /// 카드 확대 UI 표시
    /// </summary>
    private void ShowCardDetail()
    {
        if (OccupantCard == null) return;

        // CardDetailUI 표시
        if (cardDetailUI != null)
        {
            cardDetailUI.ShowCard(OccupantCard);
        }
        else
        {
            Debug.LogError("CardDetailUI가 연결되지 않았습니다! Inspector에서 연결하거나 씬에 CardDetailUI를 추가하세요.");
        }
    }

    // ═══════════════════════════════════════
    // 슬롯 상태 변경
    // ═══════════════════════════════════════

    /// <summary>
    /// 슬롯에 카드 배치
    /// </summary>
    public void SetCard(CardData card)
    {
        if (card == null)
        {
            Debug.LogWarning("SetCard: card가 null입니다!");
            return;
        }

        // PassiveCardManager에 먼저 알림 (OccupantCard 설정 전에!)
        if (PassiveCardManager.Instance != null)
        {
            Debug.Log($"[CardSlot] PlaceCardInSlot 호출: {card.cardName}");
            PassiveCardManager.Instance.PlaceCardInSlot(card, this);
        }
        else
        {
            Debug.LogError($"[CardSlot] PassiveCardManager.Instance가 null입니다! 카드: {card.cardName}");
        }

        // 그 다음 슬롯에 설정
        OccupantCard = card;
        UpdateVisual();

        Debug.Log($"{card.cardName} 슬롯에 배치됨");
    }

    /// <summary>
    /// 슬롯 비우기
    /// </summary>
    public void Clear()
    {
        OccupantCard = null;
        UpdateVisual();
        SetSynergyActive(false);
    }

    /// <summary>
    /// 시너지 효과 표시 on/off
    /// </summary>
    public void SetSynergyActive(bool active)
    {
        if (synergyGlow != null)
        {
            synergyGlow.enabled = active;
            if (active)
                synergyGlow.color = synergyColor;
        }
    }

    // ═══════════════════════════════════════
    // 비주얼 갱신
    // ═══════════════════════════════════════

    private void UpdateVisual()
    {
        bool isEmpty = OccupantCard == null;

        if (emptyState != null)
            emptyState.SetActive(isEmpty);

        if (cardVisual != null)
            cardVisual.SetActive(!isEmpty);

        if (cardNameText != null)
        {
            if (isEmpty)
            {
                cardNameText.text = "빈 슬롯";
            }
            else
            {
                cardNameText.text = OccupantCard.cardName;
            }
        }

        if (!isEmpty && OccupantCard != null)
        {
            if (cardImage != null && OccupantCard.cardArt != null)
                cardImage.sprite = OccupantCard.cardArt;
        }
    }
}
