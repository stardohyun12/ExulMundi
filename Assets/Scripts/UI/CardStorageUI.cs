using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 보관함 UI — 카드 슬롯 목록을 표시하고 클릭으로 손패 회수를 지원합니다.
/// Toggle 버튼으로 패널을 열고 닫습니다.
/// </summary>
public class CardStorageUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Toggle     toggleButton;

    [Header("슬롯 컨테이너")]
    [SerializeField] private Transform  slotsContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("정보 텍스트")]
    [SerializeField] private TextMeshProUGUI slotCountText;

    private readonly List<CardStorageSlot> _slots = new();

    private void Awake()
    {
        panel?.SetActive(false);
        toggleButton?.onValueChanged.AddListener(on => panel?.SetActive(on));
    }

    private void OnEnable()
    {
        if (CardStorage.Instance != null)
            CardStorage.Instance.OnStorageChanged += Refresh;
    }

    private void OnDisable()
    {
        if (CardStorage.Instance != null)
            CardStorage.Instance.OnStorageChanged -= Refresh;
    }

    private void Start() => Refresh();

    /// <summary>보관함 내용을 새로 고칩니다.</summary>
    public void Refresh()
    {
        var storage = CardStorage.Instance;
        if (storage == null) return;

        foreach (var slot in _slots)
            if (slot != null) Destroy(slot.gameObject);
        _slots.Clear();

        for (int i = 0; i < storage.MaxSlots; i++)
        {
            var go   = slotPrefab != null
                       ? Instantiate(slotPrefab, slotsContainer)
                       : CreateDefaultSlot(i);
            var slot = go.GetComponent<CardStorageSlot>()
                       ?? go.AddComponent<CardStorageSlot>();

            var card = i < storage.StoredCards.Count ? storage.StoredCards[i] : null;
            slot.Setup(card, storage);
            _slots.Add(slot);
        }

        if (slotCountText != null)
            slotCountText.text = $"{storage.StoredCount} / {storage.MaxSlots}";
    }

    private GameObject CreateDefaultSlot(int index)
    {
        var go       = new GameObject($"StorageSlot_{index}");
        go.transform.SetParent(slotsContainer, false);
        var rt       = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 150f);
        var img      = go.AddComponent<Image>();
        img.color    = new Color(0.15f, 0.15f, 0.22f, 0.9f);
        return go;
    }
}

/// <summary>
/// 보관함 단일 슬롯 — 카드 정보를 표시하고 클릭 시 손패로 회수합니다.
/// </summary>
public class CardStorageSlot : MonoBehaviour, IPointerClickHandler
{
    private CardData    _card;
    private CardStorage _storage;
    private TextMeshProUGUI _nameText;
    private Image           _background;

    private static readonly Color EmptyColor = new(0.1f, 0.1f, 0.15f, 0.7f);

    /// <summary>CardStorageUI.Refresh()에서 호출됩니다.</summary>
    public void Setup(CardData card, CardStorage storage)
    {
        _card       = card;
        _storage    = storage;
        _background = GetComponent<Image>();
        _nameText   = GetComponentInChildren<TextMeshProUGUI>();

        if (card != null)
        {
            if (_background != null) _background.color = GetRarityColor(card.rarity);
            if (_nameText   != null) _nameText.text    = card.cardName;
        }
        else
        {
            if (_background != null) _background.color = EmptyColor;
            if (_nameText   != null) _nameText.text    = string.Empty;
        }
    }

    /// <summary>클릭 시 카드를 손패로 회수합니다.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_card == null || _storage == null) return;
        _storage.Retrieve(_card);
    }

    private static Color GetRarityColor(CardRarity rarity) => rarity switch
    {
        CardRarity.Common    => new Color(0.20f, 0.20f, 0.28f),
        CardRarity.Uncommon  => new Color(0.10f, 0.30f, 0.15f),
        CardRarity.Rare      => new Color(0.10f, 0.18f, 0.45f),
        CardRarity.Legendary => new Color(0.40f, 0.22f, 0.05f),
        _                    => EmptyColor
    };
}
