using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>카드 보상 화면에서 카드 하나를 표시하는 슬롯.</summary>
public class CardRewardSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private Button          selectButton;

    private CardData         _card;
    private Action<CardData> _onSelect;

    /// <summary>슬롯을 카드 데이터로 초기화합니다.</summary>
    public void Setup(CardData card, Action<CardData> onSelect)
    {
        _card     = card;
        _onSelect = onSelect;

        if (cardNameText    != null) cardNameText.text    = card.cardName;
        if (descriptionText != null) descriptionText.text = card.description;
        if (rarityText      != null) rarityText.text      = card.rarity.ToString();

        selectButton?.onClick.RemoveAllListeners();
        selectButton?.onClick.AddListener(() => _onSelect?.Invoke(_card));
    }
}
