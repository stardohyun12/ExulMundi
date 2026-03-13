using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 손패 카드 한 장의 표시 및 인터랙션을 담당합니다.
///
/// 포즈 제어는 참조 코드 방식을 그대로 따릅니다.
///   targetPos / targetRot / targetScale → Update에서 localPosition / localEulerAngles / localScale 로 보간
///   SetHovered / SetSelected → 즉시 target을 갱신
///   SetBasePose → 선택 중이 아닐 때만 target을 갱신
///
/// 무기 슬롯(isActiveSlot = true)에만 쿨다운 게이지, Shake, 글로우가 활성화됩니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CardView : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    // ── 상수 ─────────────────────────────────────────────────────────────────

    private const float SPD          = 12f;   // 보간 속도 — 참조 코드 그대로
    private const float GlowBorderPx = 6f;

    private static readonly Color GlowColorOn  = new(1.0f, 0.78f, 0.20f, 0.90f);
    private static readonly Color GlowColorOff = new(1.0f, 0.78f, 0.20f, 0.00f);
    private static readonly Color GaugeColor   = new(0.25f, 0.25f, 0.25f, 0.55f);

    // ── 인스펙터 (프리팹에서 직접 연결하거나, 미연결 시 런타임 생성) ────────────

    [SerializeField] private Image cooldownBar;
    [SerializeField] private Image borderGlow;

    // ── 공개 참조 ─────────────────────────────────────────────────────────────

    [HideInInspector] public HandManager hand;

    // ── 포즈 상태 — 참조 코드 그대로 ─────────────────────────────────────────

    private Vector3 targetPos;
    private float   targetRot;
    private Vector3 targetScale = Vector3.one;
    private Vector3 basePos;
    private float   baseRot;
    private bool    isSelected;

    // ── 게임 참조 ─────────────────────────────────────────────────────────────

    private WeaponManager _weaponManager;
    private CardData      _card;
    private bool          _isActiveSlot;

    // ── UI 자식 참조 ──────────────────────────────────────────────────────────

    private Image           _background;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _rarityText;
    private TextMeshProUGUI _descText;

    // ── 쿨다운 ───────────────────────────────────────────────────────────────

    private float _cooldownDuration = 1f;
    private float _elapsed;
    private bool  _isFilling;

    // ── 글로우 ───────────────────────────────────────────────────────────────

    private Coroutine _glowCoroutine;

    // ── 초기화 ───────────────────────────────────────────────────────────────

    /// <summary>HandManager.Refresh()에서 슬롯 생성 직후 호출합니다.</summary>
    public void Setup(CardData data, HandManager manager, WeaponManager weaponManager, bool isActiveSlot)
    {
        hand           = manager;
        _weaponManager = weaponManager;
        _card          = data;
        _isActiveSlot  = isActiveSlot;

        // CENTER 피벗/앵커 → localPosition이 SlotsContainer 중심 기준으로 동작합니다.
        var rt       = GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(120f, 180f);

        BindChildUI();
        ApplyCardData(data);
        EnsureGlowBorder();
        EnsureGaugeBar();

        if (_isActiveSlot)
            StartCooldown(_weaponManager?.CurrentWeapon?.AttackInterval ?? 1f);
    }

    private void BindChildUI()
    {
        _background = GetComponent<Image>();
        _nameText   = FindChildTMP("CardName");
        _rarityText = FindChildTMP("Rarity");
        _descText   = FindChildTMP("Description");
    }

    private void ApplyCardData(CardData data)
    {
        if (_nameText   != null) _nameText.text  = data.cardName;
        if (_rarityText != null) _rarityText.text = data.rarity.ToString();
        if (_descText   != null) _descText.text   = data.description;

        if (_background == null) return;

        _background.color = data.rarity switch
        {
            CardRarity.Common    => new Color(0.20f, 0.20f, 0.28f),
            CardRarity.Uncommon  => new Color(0.10f, 0.30f, 0.15f),
            CardRarity.Rare      => new Color(0.10f, 0.18f, 0.45f),
            CardRarity.Legendary => new Color(0.40f, 0.22f, 0.05f),
            _                   => new Color(0.15f, 0.15f, 0.22f)
        };
        if (!_isActiveSlot)
            _background.color = Color.Lerp(_background.color, Color.black, 0.45f);
    }

    private void EnsureGlowBorder()
    {
        if (borderGlow == null)
        {
            var existing = transform.Find("GlowBorder");
            if (existing != null)
            {
                borderGlow = existing.GetComponent<Image>();
            }
            else
            {
                var go       = new GameObject("GlowBorder");
                go.transform.SetParent(transform, false);
                go.transform.SetAsFirstSibling();

                var rt       = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(-GlowBorderPx, -GlowBorderPx);
                rt.offsetMax = new Vector2( GlowBorderPx,  GlowBorderPx);

                go.AddComponent<LayoutElement>().ignoreLayout = true;
                borderGlow               = go.AddComponent<Image>();
                borderGlow.raycastTarget = false;
            }
        }
        borderGlow.color = GlowColorOff;
    }

    private void EnsureGaugeBar()
    {
        if (cooldownBar == null)
        {
            var existing = transform.Find("GaugeBar");
            if (existing != null)
            {
                cooldownBar = existing.GetComponent<Image>();
            }
            else
            {
                var go       = new GameObject("GaugeBar");
                go.transform.SetParent(transform, false);
                go.transform.SetAsFirstSibling();

                var rt       = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                go.AddComponent<LayoutElement>().ignoreLayout = true;
                cooldownBar               = go.AddComponent<Image>();
                cooldownBar.type          = Image.Type.Filled;
                cooldownBar.fillMethod    = Image.FillMethod.Horizontal;
                cooldownBar.fillOrigin    = (int)Image.OriginHorizontal.Left;
                cooldownBar.raycastTarget = false;
            }
        }
        cooldownBar.color      = GaugeColor;
        cooldownBar.fillAmount = 0f;
        cooldownBar.gameObject.SetActive(_isActiveSlot);
    }

    private TextMeshProUGUI FindChildTMP(string childName)
    {
        var t = transform.Find(childName);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    // ── 포즈 API — 참조 코드 그대로 ─────────────────────────────────────────

    /// <summary>HandManager.ArrangeHand()에서 기준 위치와 각도를 전달합니다.</summary>
    public void SetBasePose(Vector3 pos, float rot)
    {
        basePos = pos;
        baseRot = rot;
        if (!isSelected) { targetPos = pos; targetRot = rot; targetScale = Vector3.one; }
    }

    /// <summary>호버 상태를 설정합니다. 선택 중일 때는 무시됩니다.</summary>
    public void SetHovered(bool on)
    {
        if (isSelected) return;
        targetPos   = on ? basePos + new Vector3(0f, hand.HoverRiseY, 0f) : basePos;
        targetRot   = on ? 0f : baseRot;
        targetScale = on ? Vector3.one * hand.HoverScale : Vector3.one;
    }

    /// <summary>선택 상태를 설정합니다.</summary>
    public void SetSelected(bool on)
    {
        isSelected  = on;
        targetPos   = on ? basePos + new Vector3(0f, hand.SelectedRiseY, 0f) : basePos;
        targetRot   = on ? 0f : baseRot;
        targetScale = on ? Vector3.one * 1.25f : Vector3.one;
    }

    // ── Update — 참조 코드 그대로 ────────────────────────────────────────────

    private void Update()
    {
        transform.localPosition    = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * SPD);
        transform.localEulerAngles = new Vector3(0f, 0f,
            Mathf.LerpAngle(transform.localEulerAngles.z, targetRot, Time.deltaTime * SPD));
        transform.localScale       = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * SPD);

        if (_isFilling && _isActiveSlot)
        {
            _elapsed = Mathf.Min(_elapsed + Time.deltaTime, _cooldownDuration);
            UpdateCooldown(_elapsed / _cooldownDuration);
        }
    }

    /// <summary>
    /// 쿨다운 게이지를 갱신합니다 (t : 0 ~ 1).
    /// t == 1 이 되면 Shake 애니메이션을 실행합니다.
    /// </summary>
    public void UpdateCooldown(float t)
    {
        if (cooldownBar) cooldownBar.fillAmount = t;
        if (t >= 1f)
        {
            _isFilling = false;
            StartCoroutine(Shake());
        }
    }

    // ── Shake — 참조 코드 그대로 (selected 오프셋 보정 추가) ─────────────────

    private IEnumerator Shake()
    {
        Vector3 restPos = isSelected
            ? basePos + new Vector3(0f, hand.SelectedRiseY, 0f)
            : basePos;

        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            targetPos = restPos + new Vector3(Mathf.Sin(t * 80f) * 3f, 0f, 0f);
            yield return null;
        }
        targetPos = restPos;
    }

    // ── 쿨다운 / 글로우 내부 로직 ─────────────────────────────────────────────

    private void StartCooldown(float duration)
    {
        _cooldownDuration = Mathf.Max(0.05f, duration);
        _elapsed          = 0f;
        _isFilling        = true;
    }

    private void OnEnable()
    {
        WeaponBase.OnWeaponActivated       += HandleWeaponActivated;
        WeaponBase.OnWeaponCooldownStarted += HandleCooldownStarted;
    }

    private void OnDisable()
    {
        WeaponBase.OnWeaponActivated       -= HandleWeaponActivated;
        WeaponBase.OnWeaponCooldownStarted -= HandleCooldownStarted;
    }

    private void HandleWeaponActivated()
    {
        if (!_isActiveSlot) return;
        _isFilling = false;
        if (cooldownBar) cooldownBar.fillAmount = 0f;
        if (_glowCoroutine != null) StopCoroutine(_glowCoroutine);
        _glowCoroutine = StartCoroutine(GlowAnimation());
    }

    private void HandleCooldownStarted(float duration)
    {
        if (!_isActiveSlot) return;
        StartCooldown(duration);
    }

    private IEnumerator GlowAnimation()
    {
        if (borderGlow == null) yield break;

        const float inDur   = 0.06f;
        const float holdDur = 0.18f;
        const float outDur  = 0.40f;

        for (float t = 0f; t < inDur;   t += Time.unscaledDeltaTime)
        {
            borderGlow.color = Color.Lerp(GlowColorOff, GlowColorOn, t / inDur);
            yield return null;
        }
        borderGlow.color = GlowColorOn;

        for (float t = 0f; t < holdDur; t += Time.unscaledDeltaTime) yield return null;

        for (float t = 0f; t < outDur;  t += Time.unscaledDeltaTime)
        {
            borderGlow.color = Color.Lerp(GlowColorOn, GlowColorOff, t / outDur);
            yield return null;
        }
        borderGlow.color = GlowColorOff;
        _glowCoroutine   = null;
    }

    // ── 포인터 이벤트 — 참조 코드 그대로 ─────────────────────────────────────

    public void OnPointerEnter(PointerEventData e) => hand.OnCardHoverEnter(this);
    public void OnPointerExit (PointerEventData e) => hand.OnCardHoverExit(this);

    /// <summary>클릭 시 선택 토글 + 카드 상세 팝업을 표시합니다.</summary>
    public void OnPointerClick(PointerEventData e)
    {
        hand.OnCardClick(this);
        CardDetailPopup.Instance?.Show(_card);
    }
}
