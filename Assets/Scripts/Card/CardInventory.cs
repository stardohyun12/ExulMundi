using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 플레이어가 보유한 카드 목록을 관리하고 효과를 Player에 적용/해제합니다.
/// </summary>
public class CardInventory : MonoBehaviour
{
    public static CardInventory Instance { get; private set; }

    private readonly List<CardData> _cards = new();
    private readonly Dictionary<CardData, CardEffectComponent> _effectMap = new();

    public IReadOnlyList<CardData> Cards => _cards;

    public event Action<CardData> OnCardAdded;
    public event Action<CardData> OnCardRemoved;
    /// <summary>카드 순서가 바뀔 때 발생합니다. PassiveEffectApplier와 HandUI가 구독합니다.</summary>
    public event Action           OnCardMoved;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>카드를 추가하고 효과를 적용합니다.</summary>
    public void AddCard(CardData card)
    {
        if (card == null) return;
        _cards.Add(card);
        ApplyEffect(card);
        OnCardAdded?.Invoke(card);
        Debug.Log($"[CardInventory] 카드 추가: {card.cardName}");
    }

    /// <summary>카드를 제거하고 효과를 해제합니다.</summary>
    public void RemoveCard(CardData card)
    {
        if (!_cards.Remove(card)) return;
        RemoveEffect(card);
        OnCardRemoved?.Invoke(card);
        Debug.Log($"[CardInventory] 카드 제거: {card.cardName}");
    }

    /// <summary>카드를 fromIndex에서 toIndex로 이동합니다. 순서 변경 후 OnCardMoved를 발생시킵니다.</summary>
    public void MoveCard(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;
        if (fromIndex < 0 || fromIndex >= _cards.Count) return;
        if (toIndex   < 0 || toIndex   >= _cards.Count) return;

        var card = _cards[fromIndex];
        _cards.RemoveAt(fromIndex);
        _cards.Insert(toIndex, card);

        OnCardMoved?.Invoke();
        Debug.Log($"[CardInventory] 카드 이동: {card.cardName} ({fromIndex} → {toIndex})");
    }

    private void ApplyEffect(CardData card)
    {
        // Weapon / Passive 카드는 PassiveEffectApplier가 처리합니다.
        if (card.cardType == CardType.Accessory || card.cardType == CardType.Weapon) return;

        if (string.IsNullOrEmpty(card.effectComponentType)) return;

        var type = Type.GetType(card.effectComponentType);
        if (type == null || !type.IsSubclassOf(typeof(CardEffectComponent)))
        {
            Debug.LogWarning($"[CardInventory] 효과 타입을 찾을 수 없습니다: {card.effectComponentType}");
            return;
        }

        var effect = gameObject.AddComponent(type) as CardEffectComponent;
        if (effect == null) return;

        effect.Initialize(card);
        _effectMap[card] = effect;
    }

    private void RemoveEffect(CardData card)
    {
        if (!_effectMap.TryGetValue(card, out var effect)) return;
        _effectMap.Remove(card);
        Destroy(effect);
    }
}
