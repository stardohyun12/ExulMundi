using UnityEngine;
using TMPro;

/// <summary>HP, 세계 이름, 보유 카드 수를 실시간으로 표시하는 HUD.</summary>
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI worldText;
    [SerializeField] private TextMeshProUGUI cardCountText;

    private PlayerHealth _health;

    private void Start()
    {
        _health = FindFirstObjectByType<PlayerHealth>();
        if (_health != null)
            _health.OnHealthChanged += UpdateHP;

        Refresh();
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnHealthChanged -= UpdateHP;
    }

    private void Update()
    {
        UpdateWorldText();
        UpdateCardCount();
    }

    private void Refresh()
    {
        if (_health != null) UpdateHP(_health.CurrentHP, _health.MaxHP);
        UpdateWorldText();
        UpdateCardCount();
    }

    private void UpdateHP(int current, int max)
    {
        if (hpText != null) hpText.text = $"HP  {current} / {max}";
    }

    private void UpdateWorldText()
    {
        string name = RunManager.Instance?.SelectedWorld?.worldName ?? "-";
        int    idx  = RunManager.Instance?.EncounterIndex ?? 0;
        if (worldText != null) worldText.text = $"{name}  {idx + 1}";
    }

    private void UpdateCardCount()
    {
        int count = CardInventory.Instance?.Cards.Count ?? 0;
        if (cardCountText != null) cardCountText.text = $"카드  {count}";
    }
}
