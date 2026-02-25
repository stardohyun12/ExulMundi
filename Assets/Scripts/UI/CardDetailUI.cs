using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 카드 확대 UI - 우클릭 시 카드를 크게 보여줌
/// </summary>
public class CardDetailUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI 참조")]
    public GameObject detailPanel;
    public Text cardNameText;
    public Text descriptionText;
    public Text categoryText;
    public Text rarityText;
    public Image cardImage;

    private CardData _currentCard;

    void Start()
    {
        // 시작 시 숨김
        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    void Update()
    {
        // ESC 키로 닫기 (새로운 Input System 사용)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (detailPanel != null && detailPanel.activeSelf)
            {
                HideCard();
            }
        }
    }

    /// <summary>
    /// 카드 상세 정보 표시
    /// </summary>
    public void ShowCard(CardData card)
    {
        if (card == null) return;

        _currentCard = card;

        // UI 업데이트
        if (cardNameText != null)
            cardNameText.text = card.cardName;

        if (descriptionText != null)
            descriptionText.text = card.description;

        if (categoryText != null)
            categoryText.text = $"분류: {card.category}";

        if (rarityText != null)
            rarityText.text = $"등급: {card.rarity}";

        if (cardImage != null && card.cardArt != null)
            cardImage.sprite = card.cardArt;

        // 패널 표시
        if (detailPanel != null)
            detailPanel.SetActive(true);

        Debug.Log($"카드 확대: {card.cardName}");
    }

    /// <summary>
    /// 카드 상세 정보 숨김
    /// </summary>
    public void HideCard()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);

        _currentCard = null;
    }

    /// <summary>
    /// 클릭 시 닫기
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        HideCard();
    }
}
