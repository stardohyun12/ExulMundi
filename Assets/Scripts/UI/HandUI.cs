using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 플레이어가 보유한 카드 목록을 화면 하단에 표시합니다.
/// CardInventory의 OnCardAdded/OnCardRemoved 이벤트를 구독해 자동 갱신합니다.
/// </summary>
public class HandUI : MonoBehaviour
{
    private const string SlotPrefabPath = "Prefabs/HandSlot";

    [Header("참조")]
    [SerializeField] private Transform slotsParent;

    private GameObject                _slotPrefab;
    private readonly List<GameObject> _slotObjects = new();

    private void Awake()
    {
        _slotPrefab = Resources.Load<GameObject>(SlotPrefabPath);
        if (_slotPrefab == null)
            Debug.LogError("[HandUI] HandSlot 프리팹을 Resources에서 찾지 못했습니다. " +
                           "Assets/Resources/Prefabs/HandSlot.prefab 경로를 확인하세요.");
    }

    private void Start()
    {
        if (CardInventory.Instance != null)
        {
            CardInventory.Instance.OnCardAdded   += OnCardChanged;
            CardInventory.Instance.OnCardRemoved += OnCardChanged;
        }
        else
        {
            Debug.LogWarning("[HandUI] Start — CardInventory.Instance가 null입니다.");
        }
        Refresh();
    }

    private void OnDestroy()
    {
        if (CardInventory.Instance != null)
        {
            CardInventory.Instance.OnCardAdded   -= OnCardChanged;
            CardInventory.Instance.OnCardRemoved -= OnCardChanged;
        }
    }

    private void OnCardChanged(CardData _) => Refresh();

    private void Refresh()
    {
        foreach (var obj in _slotObjects)
            if (obj != null) Destroy(obj);
        _slotObjects.Clear();

        if (_slotPrefab == null)
        {
            Debug.LogError("[HandUI] Refresh 실패 — _slotPrefab이 null입니다.");
            return;
        }
        if (CardInventory.Instance == null) return;

        var cards = CardInventory.Instance.Cards;
        Debug.Log($"[HandUI] Refresh — 카드 {cards.Count}장");

        foreach (var card in cards)
        {
            var slot = Instantiate(_slotPrefab, slotsParent);
            _slotObjects.Add(slot);

            SetChildText(slot, "CardName",    card.cardName);
            SetChildText(slot, "Rarity",      card.rarity.ToString());
            SetChildText(slot, "Description", card.description);

            var img = slot.GetComponent<Image>();
            if (img != null)
            {
                img.color = card.rarity switch
                {
                    CardRarity.Common    => new Color(0.20f, 0.20f, 0.28f),
                    CardRarity.Uncommon  => new Color(0.10f, 0.30f, 0.15f),
                    CardRarity.Rare      => new Color(0.10f, 0.18f, 0.45f),
                    CardRarity.Legendary => new Color(0.40f, 0.22f, 0.05f),
                    _                   => new Color(0.15f, 0.15f, 0.22f)
                };
            }
        }
    }

    private static void SetChildText(GameObject slot, string childName, string value)
    {
        var t = slot.transform.Find(childName);
        if (t == null)
        {
            Debug.LogWarning($"[HandUI] '{childName}' 자식을 찾을 수 없습니다.");
            return;
        }
        var tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = value;
    }
}
