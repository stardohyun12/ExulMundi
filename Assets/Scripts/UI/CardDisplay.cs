using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 카드 UI 렌더링.
/// SetupCard(CardData) — 전투 스킬 카드
/// Setup(CompanionData) — 기존 동료 카드 (하위 호환)
/// </summary>
public class CardDisplay : MonoBehaviour
{
    // 기존 하위 호환용
    [HideInInspector] public CompanionData companionData;

    [Header("공통 UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;      // 카드 카드에서는 에너지 비용 표시
    public TextMeshProUGUI atkText;     // 카드 카드에서는 카테고리 표시
    public TextMeshProUGUI skillText;   // 효과 설명
    public Image           cardImageDisplay;

    [Header("HP 바 (동료 카드 전용)")]
    public Slider hpSlider;

    [Header("등급 표시 (테두리 이미지)")]
    public Image rarityBorder;

    // 등급별 색상
    private static readonly Color ColCommon = Color.white;
    private static readonly Color ColRare   = new Color(0.3f, 0.6f, 1f);
    private static readonly Color ColLegend = new Color(1f, 0.8f, 0.1f);

    // ══════════════════════════════════════
    // 전투 카드 설정
    // ══════════════════════════════════════

    public void SetupCard(CardData data)
    {
        if (data == null) return;

        if (nameText  != null) nameText.text  = data.cardName;
        if (hpText    != null) hpText.text    = data.category.ToString();
        if (atkText   != null) atkText.text   = data.rarity.ToString();
        if (skillText != null) skillText.text = data.description;

        if (cardImageDisplay != null && data.cardArt != null)
            cardImageDisplay.sprite = data.cardArt;

        // HP 바는 전투 카드에서 숨김
        if (hpSlider != null) hpSlider.gameObject.SetActive(false);

        // 등급 색상
        if (rarityBorder != null)
        {
            rarityBorder.color = data.rarity switch
            {
                CardRarity.Common => ColCommon,
                CardRarity.Rare   => ColRare,
                CardRarity.Legend => ColLegend,
                _                 => ColCommon,
            };
        }
    }

    // ══════════════════════════════════════
    // 동료 카드 설정 (하위 호환)
    // ══════════════════════════════════════

    public void Setup(CompanionData data)
    {
        companionData = data;
        UpdateDisplay();
    }

    void Start()
    {
        if (companionData != null) UpdateDisplay();
    }

    void OnDestroy()
    {
        if (_linkedUnit != null)
            _linkedUnit.OnHPChanged -= HandleHPChanged;
    }

    // CompanionUnit HP 실시간 연동 (동료 카드 전용)
    private CompanionUnit _linkedUnit;

    public void LinkUnit(CompanionUnit unit)
    {
        if (_linkedUnit != null)
            _linkedUnit.OnHPChanged -= HandleHPChanged;

        _linkedUnit = unit;
        if (_linkedUnit == null) return;

        _linkedUnit.OnHPChanged += HandleHPChanged;

        if (hpSlider != null)
        {
            hpSlider.maxValue = _linkedUnit.MaxHP;
            hpSlider.value    = _linkedUnit.CurrentHP;
        }
        if (hpText != null)
            hpText.text = $"HP: {_linkedUnit.CurrentHP}/{_linkedUnit.MaxHP}";
    }

    private void HandleHPChanged(CompanionUnit unit)
    {
        if (hpSlider != null) hpSlider.value = unit.CurrentHP;
        if (hpText   != null) hpText.text    = $"HP: {unit.CurrentHP}/{unit.MaxHP}";
    }

    public void UpdateDisplay()
    {
        if (companionData == null) return;

        if (nameText  != null) nameText.text  = companionData.companionName;
        if (hpText    != null) hpText.text    = $"HP: {companionData.maxHP}";
        if (atkText   != null) atkText.text   = $"ATK: {companionData.atk}";
        if (skillText != null && !string.IsNullOrEmpty(companionData.skillName))
            skillText.text = $"{companionData.skillName}: {companionData.skillDescription}";
        if (cardImageDisplay != null && companionData.cardImage != null)
            cardImageDisplay.sprite = companionData.cardImage;
        if (hpSlider != null)
        {
            hpSlider.gameObject.SetActive(true);
            hpSlider.maxValue = companionData.maxHP;
            hpSlider.value    = companionData.maxHP;
        }
    }
}
