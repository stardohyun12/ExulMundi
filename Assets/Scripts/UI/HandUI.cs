using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 플레이어가 보유한 카드 목록을 화면 하단에 표시합니다.
/// 카드 슬롯은 드래그 앤 드롭으로 순서를 변경할 수 있습니다.
/// HandLayoutController와 연동해 Fan/Expanded 레이아웃을 지원합니다.
/// </summary>
public class HandUI : MonoBehaviour
{
    private const string SlotPrefabPath = "Prefabs/HandSlot";

    [Header("참조")]
    [SerializeField] private Transform           slotsParent;
    [SerializeField] private HandLayoutController layoutController;

    private GameObject                _slotPrefab;
    private WeaponManager             _weaponManager;
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
        _weaponManager = FindAnyObjectByType<WeaponManager>();

        if (CardInventory.Instance != null)
        {
            CardInventory.Instance.OnCardAdded   += OnCardChanged;
            CardInventory.Instance.OnCardRemoved += OnCardChanged;
            CardInventory.Instance.OnCardMoved   += Refresh;
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
            CardInventory.Instance.OnCardMoved   -= Refresh;
        }
    }

    private void OnCardChanged(CardData _) => Refresh();

    public void Refresh()
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

        // 무기 카드 인덱스 — 인접 슬롯 시각 표시에 사용
        int weaponIndex = -1;
        for (int i = 0; i < cards.Count; i++)
            if (cards[i].cardType == CardType.Weapon) { weaponIndex = i; break; }

        for (int i = 0; i < cards.Count; i++)
        {
            var card  = cards[i];
            var slot  = Instantiate(_slotPrefab, slotsParent);
            _slotObjects.Add(slot);

            SetChildText(slot, "CardName",    card.cardName);
            SetChildText(slot, "Rarity",      card.rarity.ToString());
            SetChildText(slot, "Description", card.description);

            // 희귀도 배경색
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

            // 드래그 컴포넌트 설정
            var drag = slot.GetComponent<HandSlotDrag>();
            if (drag == null) drag = slot.AddComponent<HandSlotDrag>();
            drag.Initialize(i, this);
            drag.SetCard(card);
            drag.SetTypeColor(card.cardType);

            // 무기 인접 슬롯 하이라이트
            bool isAdjacent = weaponIndex >= 0 &&
                              card.cardType == CardType.Accessory &&
                              (i == weaponIndex - 1 || i == weaponIndex + 1);
            drag.SetActiveHighlight(isAdjacent);

            // 게이지 바 + 팝 애니메이션 — 모든 슬롯에 HandSlotBehavior를 붙입니다.
            // isActiveSlot이 true인 슬롯만 게이지를 표시하고, false인 슬롯은 어두운 비활성 오버레이를 표시합니다.
            bool isActiveSlot = i == weaponIndex ||
                                (weaponIndex >= 0 && (i == weaponIndex - 1 || i == weaponIndex + 1));

            var behavior = slot.GetComponent<HandSlotBehavior>();
            if (behavior == null) behavior = slot.AddComponent<HandSlotBehavior>();
            behavior.Initialize(_weaponManager, isActiveSlot);
        }

        // 슬롯 생성 완료 후 HandLayoutController에 목록을 전달해 레이아웃을 재계산합니다.
        layoutController?.SetSlots(_slotObjects);
    }

    /// <summary>슬롯 드래그가 끝났을 때 CardInventory에 순서 변경을 요청합니다.</summary>
    public void OnSlotDropped(int fromIndex, int toIndex)
    {
        CardInventory.Instance?.MoveCard(fromIndex, toIndex);
        // MoveCard → OnCardMoved → Refresh 자동 호출
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
