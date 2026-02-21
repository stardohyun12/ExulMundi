using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    public CompanionData companionData;

    [Header("UI 요소")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI atkText;
    public TextMeshProUGUI skillText;
    public Image cardImageDisplay;

    [Header("HP 바")]
    public Slider hpSlider;

    private CompanionUnit linkedUnit;

    void Start()
    {
        if (companionData != null)
            UpdateDisplay();
    }

    void OnDestroy()
    {
        if (linkedUnit != null)
            linkedUnit.OnHPChanged -= HandleHPChanged;
    }

    /// <summary>
    /// 외부에서 데이터 설정 (PartyManager 등)
    /// </summary>
    public void Setup(CompanionData data)
    {
        companionData = data;
        UpdateDisplay();
    }

    /// <summary>
    /// 전투 중 CompanionUnit에 연결 — HP바 실시간 갱신
    /// </summary>
    public void LinkUnit(CompanionUnit unit)
    {
        if (linkedUnit != null)
            linkedUnit.OnHPChanged -= HandleHPChanged;

        linkedUnit = unit;

        if (linkedUnit == null) return;

        linkedUnit.OnHPChanged += HandleHPChanged;

        if (hpSlider != null)
        {
            hpSlider.maxValue = linkedUnit.MaxHP;
            hpSlider.value = linkedUnit.CurrentHP;
        }
        if (hpText != null)
            hpText.text = $"HP: {linkedUnit.CurrentHP}/{linkedUnit.MaxHP}";
    }

    private void HandleHPChanged(CompanionUnit unit)
    {
        if (hpSlider != null)
            hpSlider.value = unit.CurrentHP;
        if (hpText != null)
            hpText.text = $"HP: {unit.CurrentHP}/{unit.MaxHP}";
    }

    public void UpdateDisplay()
    {
        if (companionData == null) return;

        if (nameText != null)
            nameText.text = companionData.companionName;
        if (hpText != null)
            hpText.text = $"HP: {companionData.maxHP}";
        if (atkText != null)
            atkText.text = $"ATK: {companionData.atk}";
        if (skillText != null && !string.IsNullOrEmpty(companionData.skillName))
            skillText.text = $"{companionData.skillName}: {companionData.skillDescription}";
        if (cardImageDisplay != null && companionData.cardImage != null)
            cardImageDisplay.sprite = companionData.cardImage;
        if (hpSlider != null)
        {
            hpSlider.maxValue = companionData.maxHP;
            hpSlider.value = companionData.maxHP;
        }
    }
}
