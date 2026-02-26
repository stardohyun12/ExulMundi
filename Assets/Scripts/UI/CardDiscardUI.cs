using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 카드 버리기 UI.
/// 카드 클릭 → 중앙 패널에 카드 정보 표시 → 버리기 버튼 → 회전+축소 애니메이션 → 버리기 완료
/// </summary>
public class CardDiscardUI : MonoBehaviour
{
    public static CardDiscardUI Instance { get; private set; }

    [Header("패널 (항상 활성화된 오브젝트와 분리)")]
    public GameObject panel;                // CardDiscardPanel (비활성 상태에서 시작)
    public RectTransform cardDisplayRoot;   // 중앙에 표시되는 카드 루트

    [Header("카드 정보 표시")]
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardDescText;

    [Header("버튼")]
    public Button discardButton;
    public Button cancelButton;

    [Header("애니메이션")]
    public float moveToCenter = 0.35f;
    public float discardDuration = 0.5f;
    public float rotateDegrees = 360f;

    private CardSlot _selectedSlot;
    private CardData _selectedCard;
    private bool _isAnimating = false;

    void Awake()
    {
        // CardDiscardUI는 항상 활성화된 오브젝트에 붙어야 Instance가 유지됨
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    void Start()
    {
        // panel은 별도 GameObject - 자기 자신이 아님
        if (panel != null) panel.SetActive(false);

        discardButton?.onClick.AddListener(OnDiscardClicked);
        cancelButton?.onClick.AddListener(OnCancelClicked);
    }

    /// <summary>
    /// 슬롯의 카드를 선택 → 중앙 패널에 표시
    /// </summary>
    public void ShowForDiscard(CardSlot slot)
    {
        if (_isAnimating) return;
        if (slot == null || slot.OccupantCard == null) return;

        _selectedSlot = slot;
        _selectedCard = slot.OccupantCard;

        if (cardNameText != null) cardNameText.text = _selectedCard.cardName;
        if (cardDescText != null)  cardDescText.text  = _selectedCard.description;

        panel?.SetActive(true);

        if (cardDisplayRoot != null)
        {
            cardDisplayRoot.localScale    = Vector3.one * 0.3f;
            cardDisplayRoot.localRotation = Quaternion.identity;
            StartCoroutine(AnimateCardIn());
        }
    }

    private IEnumerator AnimateCardIn()
    {
        float elapsed = 0f;
        while (elapsed < moveToCenter)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveToCenter);
            cardDisplayRoot.localScale = Vector3.Lerp(Vector3.one * 0.3f, Vector3.one, t);
            yield return null;
        }
        cardDisplayRoot.localScale = Vector3.one;
    }

    private void OnDiscardClicked()
    {
        if (_isAnimating || _selectedCard == null) return;
        StartCoroutine(DiscardAnimation());
    }

    private IEnumerator DiscardAnimation()
    {
        _isAnimating = true;
        discardButton.interactable = false;
        cancelButton.interactable  = false;

        float elapsed = 0f;
        while (elapsed < discardDuration)
        {
            elapsed += Time.deltaTime;
            float smooth = Mathf.SmoothStep(0f, 1f, elapsed / discardDuration);
            cardDisplayRoot.localScale    = Vector3.Lerp(Vector3.one, Vector3.zero, smooth);
            cardDisplayRoot.localRotation = Quaternion.Euler(0f, 0f, rotateDegrees * smooth);
            yield return null;
        }

        CompleteDiscard();
    }

    private void CompleteDiscard()
    {
        _isAnimating = false;

        if (PassiveCardManager.Instance != null)
        {
            PassiveCardManager.Instance.ownedCards.Remove(_selectedCard);
            // 슬롯 GameObject 자체를 삭제하고 간격 재조정
            PassiveCardManager.Instance.RemoveSlotAndCard(_selectedSlot);
        }

        Debug.Log($"[CardDiscardUI] 카드 버림: {_selectedCard.cardName}");

        panel?.SetActive(false);
        ResetCardDisplay();

        SimpleWorldManager.Instance?.OnDiscardComplete();
    }

    private void OnCancelClicked()
    {
        if (_isAnimating) return;
        panel?.SetActive(false);
        ResetCardDisplay();
        _selectedSlot = null;
        _selectedCard = null;
        SimpleWorldManager.Instance?.OnDiscardCancelled();
    }

    private void ResetCardDisplay()
    {
        if (cardDisplayRoot != null)
        {
            cardDisplayRoot.localScale    = Vector3.one;
            cardDisplayRoot.localRotation = Quaternion.identity;
        }
        if (discardButton != null) discardButton.interactable = true;
        if (cancelButton  != null) cancelButton.interactable  = true;
    }
}
