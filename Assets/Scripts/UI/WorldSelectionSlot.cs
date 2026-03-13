using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;

/// <summary>세계 선택 화면에서 세계 하나를 표시하는 슬롯. 카드 클릭으로 선택하며 호버 시 확대 효과를 제공합니다.</summary>
public class WorldSelectionSlot : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI worldNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI weaponTypeText;
    [SerializeField] private Image           backgroundImage;

    [Header("Hover 효과")]
    [SerializeField] private float hoverScale    = 1.07f;
    [SerializeField] private float scaleDuration = 0.15f;

    private static readonly Vector3 NormalScale = Vector3.one;

    private WorldDefinition         _world;
    private Action<WorldDefinition> _onSelect;
    private Coroutine               _scaleCoroutine;

    /// <summary>슬롯을 세계 데이터로 초기화합니다.</summary>
    public void Setup(WorldDefinition world, Action<WorldDefinition> onSelect)
    {
        _world    = world;
        _onSelect = onSelect;

        if (worldNameText   != null) worldNameText.text   = world.worldName;
        if (descriptionText != null) descriptionText.text = world.description;
        if (weaponTypeText  != null) weaponTypeText.text  = GetCategoryLabel(world.weaponCard);

        // themeColor를 어둡게 만들어 흰 텍스트와 충분한 대비를 확보합니다.
        if (backgroundImage != null)
        {
            Color c = world.themeColor;
            backgroundImage.color = new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f, 1f);
        }
    }

    // ── IPointerClickHandler ──────────────────────────────────────────────────

    public void OnPointerClick(PointerEventData eventData)
    {
        _onSelect?.Invoke(_world);
    }

    // ── IPointerEnterHandler / ExitHandler ───────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        ScaleTo(Vector3.one * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ScaleTo(NormalScale);
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────────────────

    private void ScaleTo(Vector3 target)
    {
        if (_scaleCoroutine != null)
            StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = StartCoroutine(ScaleCoroutine(target));
    }

    private IEnumerator ScaleCoroutine(Vector3 target)
    {
        Vector3 start   = transform.localScale;
        float   elapsed = 0f;

        while (elapsed < scaleDuration)
        {
            elapsed             += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(start, target, elapsed / scaleDuration);
            yield return null;
        }

        transform.localScale = target;
        _scaleCoroutine      = null;
    }

    private static string GetCategoryLabel(CardData weaponCard)
    {
        if (weaponCard == null) return "-";
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
