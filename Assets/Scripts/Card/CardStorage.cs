using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 플레이어 보관함 카드 목록을 관리합니다.
/// 기본 3슬롯이며, 전투 클리어마다 ExpandSlot()이 호출되어 +1 확장됩니다.
/// </summary>
public class CardStorage : MonoBehaviour
{
    private const int DefaultMaxSlots = 3;

    public static CardStorage Instance { get; private set; }

    public int MaxSlots    { get; private set; } = DefaultMaxSlots;
    public int StoredCount => _storedCards.Count;
    public IReadOnlyList<CardData> StoredCards => _storedCards;

    /// <summary>보관함 내용이 변경될 때 발생합니다.</summary>
    public event Action OnStorageChanged;

    private readonly List<CardData> _storedCards = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        var em = FindFirstObjectByType<EncounterManager>();
        if (em != null) em.OnEncounterComplete += ExpandSlot;
    }

    private void OnDestroy()
    {
        var em = FindFirstObjectByType<EncounterManager>();
        if (em != null) em.OnEncounterComplete -= ExpandSlot;
    }

    /// <summary>손패에서 보관함으로 카드를 이동합니다.</summary>
    public bool Store(CardData card)
    {
        if (card == null) return false;
        if (_storedCards.Count >= MaxSlots)
        {
            Debug.LogWarning($"[CardStorage] 보관함이 가득 찼습니다 ({MaxSlots}슬롯).");
            return false;
        }
        CardInventory.Instance?.RemoveCard(card);
        _storedCards.Add(card);
        OnStorageChanged?.Invoke();
        Debug.Log($"[CardStorage] 보관 → {card.cardName} ({_storedCards.Count}/{MaxSlots})");
        return true;
    }

    /// <summary>보관함에서 손패로 카드를 꺼냅니다.</summary>
    public bool Retrieve(CardData card)
    {
        if (!_storedCards.Remove(card)) return false;
        CardInventory.Instance?.AddCard(card);
        OnStorageChanged?.Invoke();
        Debug.Log($"[CardStorage] 회수 → {card.cardName}");
        return true;
    }

    /// <summary>전투 클리어 시 최대 슬롯 수를 1 증가시킵니다.</summary>
    public void ExpandSlot()
    {
        MaxSlots++;
        OnStorageChanged?.Invoke();
        Debug.Log($"[CardStorage] 슬롯 확장 → 최대 {MaxSlots}슬롯");
    }
}
