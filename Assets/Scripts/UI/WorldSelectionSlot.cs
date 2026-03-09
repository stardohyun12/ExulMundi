using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>세계 선택 화면에서 세계 하나를 표시하는 슬롯.</summary>
public class WorldSelectionSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI worldNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI weaponTypeText;
    [SerializeField] private Image           backgroundImage;
    [SerializeField] private Button          selectButton;

    private WorldDefinition         _world;
    private Action<WorldDefinition> _onSelect;

    /// <summary>슬롯을 세계 데이터로 초기화합니다.</summary>
    public void Setup(WorldDefinition world, Action<WorldDefinition> onSelect)
    {
        _world    = world;
        _onSelect = onSelect;

        if (worldNameText   != null) worldNameText.text   = world.worldName;
        if (descriptionText != null) descriptionText.text = world.description;
        if (weaponTypeText  != null) weaponTypeText.text  = GetCategoryLabel(world.weaponCard);
        if (backgroundImage != null) backgroundImage.color = world.themeColor;

        selectButton?.onClick.RemoveAllListeners();
        selectButton?.onClick.AddListener(() => _onSelect?.Invoke(_world));
    }

    private static string GetCategoryLabel(CardData weaponCard)
    {
        if (weaponCard == null) return "-";
        // WeaponType으로 카테고리를 역산해 표시
        return weaponCard.weaponType switch
        {
            WeaponType.Gun   => "투사체",
            WeaponType.Bow   => "투사체",
            WeaponType.Sword => "근접",
            WeaponType.Staff => "소환",
            _                => weaponCard.weaponType.ToString(),
        };
    }
}
