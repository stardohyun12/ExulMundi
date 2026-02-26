using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

/// <summary>
/// 카드 보상 UI의 개별 선택지 슬롯.
/// 카드 정보를 표시하고 클릭 시 선택 콜백 호출.
/// </summary>
public class CardRewardSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI rarityText;
    public Image background;
    public Image rarityBorder;

    private static readonly Color CommonColor = new Color(0.75f, 0.75f, 0.75f);
    private static readonly Color RareColor   = new Color(0.35f, 0.6f,  1.0f);
    private static readonly Color LegendColor = new Color(1.0f,  0.8f,  0.15f);

    private static readonly Color BgNormal  = new Color(0.12f, 0.1f,  0.18f, 1f);
    private static readonly Color BgHover   = new Color(0.22f, 0.18f, 0.3f,  1f);

    private CardData _card;
    private Action<CardData> _onChosen;

    /// <summary>카드 데이터와 선택 콜백 세팅</summary>
    public void Setup(CardData card, Action<CardData> onChosen)
    {
        gameObject.SetActive(true);
        _card     = card;
        _onChosen = onChosen;

        if (cardNameText    != null) cardNameText.text    = card.cardName;
        if (descriptionText != null) descriptionText.text = card.description;
        if (rarityText      != null) rarityText.text      = GetRarityText(card.rarity);
        if (background      != null) background.color     = BgNormal;
        if (rarityBorder    != null) rarityBorder.color   = GetRarityColor(card.rarity);

        transform.localScale = Vector3.one;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_card == null) return;
        _onChosen?.Invoke(_card);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (background != null) background.color = BgHover;
        transform.localScale = Vector3.one * 1.06f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (background != null) background.color = BgNormal;
        transform.localScale = Vector3.one;
    }

    private static string GetRarityText(CardRarity rarity) => rarity switch
    {
        CardRarity.Common => "일반",
        CardRarity.Rare   => "희귀",
        CardRarity.Legend => "전설",
        _                 => ""
    };

    private static Color GetRarityColor(CardRarity rarity) => rarity switch
    {
        CardRarity.Common => CommonColor,
        CardRarity.Rare   => RareColor,
        CardRarity.Legend => LegendColor,
        _                 => CommonColor
    };
}
