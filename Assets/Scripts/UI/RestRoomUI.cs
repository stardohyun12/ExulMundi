using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 휴식 공간 UI — 회복 / 합성 / 교체 / 건너뛰기 버튼과 현재 HP를 표시합니다.
/// RestRoomManager.Enter()에서 Show()가 호출됩니다.
/// </summary>
public class RestRoomUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject panel;

    [Header("버튼")]
    [SerializeField] private Button healButton;
    [SerializeField] private Button synthesizeButton;
    [SerializeField] private Button replaceButton;
    [SerializeField] private Button skipButton;

    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI titleText;

    private RestRoomManager _manager;

    private void Awake()
    {
        panel?.SetActive(false);
        healButton?.onClick.AddListener(OnHealClicked);
        synthesizeButton?.onClick.AddListener(OnSynthesizeClicked);
        replaceButton?.onClick.AddListener(OnReplaceClicked);
        skipButton?.onClick.AddListener(OnSkipClicked);
    }

    private void OnEnable()
    {
        var ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null) ph.OnHealthChanged += RefreshHP;
    }

    private void OnDisable()
    {
        var ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null) ph.OnHealthChanged -= RefreshHP;
    }

    /// <summary>RestRoomManager가 호출합니다.</summary>
    public void Show(RestRoomManager manager)
    {
        _manager = manager;
        panel?.SetActive(true);
        if (titleText != null) titleText.text = "휴식 공간";

        var ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null) RefreshHP(ph.CurrentHP, ph.MaxHP);

        // 합성 버튼: 카드가 2장 미만이면 비활성화
        if (synthesizeButton != null)
            synthesizeButton.interactable = CardInventory.Instance?.Cards.Count >= 2;
    }

    /// <summary>RestRoomManager.Complete()에서 호출됩니다.</summary>
    public void Hide() => panel?.SetActive(false);

    private void RefreshHP(int current, int max)
    {
        if (hpText != null) hpText.text = $"HP : {current} / {max}";
    }

    private void OnHealClicked()      => _manager?.OnHealChosen();
    private void OnSkipClicked()      => _manager?.Skip();

    private void OnSynthesizeClicked()
    {
        var inv = CardInventory.Instance;
        if (inv == null || inv.Cards.Count < 2)
        {
            Debug.LogWarning("[RestRoomUI] 합성에 필요한 카드가 부족합니다 (최소 2장 필요).");
            return;
        }
        _manager?.OnSynthesizeChosen(inv.Cards[0], inv.Cards[1], null);
    }

    private void OnReplaceClicked()
    {
        var world = RunManager.Instance?.SelectedWorld;
        var pool  = world?.cardPool;
        if (pool == null || pool.Length == 0)
        {
            Debug.LogWarning("[RestRoomUI] 교체에 사용할 카드 풀이 없습니다.");
            return;
        }
        _manager?.OnReplaceChosen(pool, _ => { });
    }
}
