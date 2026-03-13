using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 손패 카드 클릭 시 카드 상세 정보를 표시하는 팝업.
/// HandSlotDrag.OnPointerClick에서 Instance.Show(card)를 호출합니다.
/// Inspector에서 참조가 할당되지 않으면 Awake()에서 UI를 자동 생성합니다.
/// </summary>
public class CardDetailPopup : MonoBehaviour
{
    public static CardDetailPopup Instance { get; private set; }

    [Header("UI 참조 (비워 두면 자동 생성)")]
    [SerializeField] private GameObject      panel;
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI effectDetailText;
    [SerializeField] private Image           rarityColorBar;
    [SerializeField] private Button          closeButton;

    private void Awake()
    {
        Instance = this;

        if (panel == null) BuildUI();

        if (panel     != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    private void Update()
    {
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }

    /// <summary>카드 상세 정보 팝업을 표시합니다. HandSlotDrag가 호출합니다.</summary>
    public void Show(CardData card)
    {
        if (panel == null || card == null) return;
        panel.SetActive(true);

        if (cardNameText     != null) cardNameText.text     = card.cardName;
        if (rarityText       != null) rarityText.text       = card.rarity.ToString();
        if (descriptionText  != null) descriptionText.text  = card.description;
        if (effectDetailText != null) effectDetailText.text = BuildEffectDetail(card);
        if (rarityColorBar   != null) rarityColorBar.color  = RarityToColor(card.rarity);
    }

    /// <summary>팝업을 닫습니다.</summary>
    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    // ── UI 자동 생성 ────────────────────────────────────────────────────────

    private void BuildUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
        Transform root = canvas != null ? canvas.transform : transform;

        // 기존 TMP에서 폰트를 가져와 팝업 텍스트에 사용합니다.
        var existingTmp = FindAnyObjectByType<TextMeshProUGUI>();
        var sharedFont  = existingTmp != null ? existingTmp.font : null;

        // ── 반투명 뒷배경 (클릭하면 닫힘) ─────────────────────────────
        var backdropGo  = MakeGO("Popup_Backdrop", root);
        Stretch(backdropGo.AddComponent<RectTransform>());
        var backdropImg = backdropGo.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.55f);
        var backdropBtn = backdropGo.AddComponent<Button>();
        backdropBtn.transition = Selectable.Transition.None;
        backdropBtn.onClick.AddListener(Hide);

        // ── 카드 패널 ────────────────────────────────────────────────────
        var panelGo  = MakeGO("Popup_Panel", backdropGo.transform);
        var panelRt  = panelGo.AddComponent<RectTransform>();
        panelRt.sizeDelta        = new Vector2(400f, 300f);
        panelRt.anchoredPosition = Vector2.zero;
        var panelImg = panelGo.AddComponent<Image>();
        panelImg.color = new Color(0.09f, 0.09f, 0.14f, 0.97f);

        var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding                = new RectOffset(22, 22, 20, 18);
        vlg.spacing                = 9f;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // ── 희귀도 컬러 바 ───────────────────────────────────────────────
        rarityColorBar = AddImage(panelGo.transform, "RarityBar", new Color(0.4f, 0.4f, 1f), 5f);

        // ── 카드 이름 ────────────────────────────────────────────────────
        cardNameText = AddTMP(panelGo.transform, "CardName", 22f, FontStyles.Bold,
                              Color.white, 32f, sharedFont);

        // ── 희귀도 ───────────────────────────────────────────────────────
        rarityText = AddTMP(panelGo.transform, "Rarity", 14f, FontStyles.Normal,
                            new Color(0.75f, 0.75f, 0.75f), 20f, sharedFont);

        // ── 구분선 ────────────────────────────────────────────────────────
        AddImage(panelGo.transform, "Divider", new Color(0.35f, 0.35f, 0.35f), 1f);

        // ── 설명 ─────────────────────────────────────────────────────────
        descriptionText = AddTMP(panelGo.transform, "Description", 15f, FontStyles.Normal,
                                 new Color(0.88f, 0.88f, 0.88f), 60f, sharedFont);
        descriptionText.enableWordWrapping = true;

        // ── 효과 상세 ────────────────────────────────────────────────────
        effectDetailText = AddTMP(panelGo.transform, "EffectDetail", 14f, FontStyles.Normal,
                                  new Color(0.95f, 0.95f, 0.95f), 80f, sharedFont);
        effectDetailText.enableWordWrapping = true;

        // ── 닫기 버튼 ────────────────────────────────────────────────────
        var closeBtnGo  = MakeGO("CloseButton", panelGo.transform);
        var closeBtnRt  = closeBtnGo.AddComponent<RectTransform>();
        closeBtnRt.sizeDelta = new Vector2(0f, 34f);
        var le = closeBtnGo.AddComponent<LayoutElement>();
        le.preferredHeight = 34f;
        var closeBtnImg = closeBtnGo.AddComponent<Image>();
        closeBtnImg.color = new Color(0.28f, 0.08f, 0.08f);
        closeButton = closeBtnGo.AddComponent<Button>();
        closeButton.targetGraphic = closeBtnImg;
        closeButton.onClick.AddListener(Hide);

        var closeLbl = AddTMP(closeBtnGo.transform, "Label", 14f, FontStyles.Bold,
                              Color.white, 34f, sharedFont);
        closeLbl.text                 = "닫기 (ESC)";
        closeLbl.horizontalAlignment  = HorizontalAlignmentOptions.Center;
        closeLbl.verticalAlignment    = VerticalAlignmentOptions.Middle;
        Stretch(closeLbl.rectTransform);

        panel = backdropGo;
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    private static GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Image AddImage(Transform parent, string name, Color color, float height)
    {
        var go  = MakeGO(name, parent);
        var rt  = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, height);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static TextMeshProUGUI AddTMP(Transform parent, string name, float size,
        FontStyles style, Color color, float height, TMP_FontAsset font)
    {
        var go  = MakeGO(name, parent);
        var rt  = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, height);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize   = size;
        tmp.fontStyle  = style;
        tmp.color      = color;
        tmp.richText   = true;
        if (font != null) tmp.font = font;
        return tmp;
    }

    // ── 효과 설명 문자열 ────────────────────────────────────────────────────

    private static string BuildEffectDetail(CardData card) => card.effectComponentType switch
    {
        "LifeStealEffect"         => $"공격 시 데미지의 <color=#FF8888>{card.effectValue:0}%</color>를 HP로 흡수",
        "AttackSpeedEffect"       => $"공격 속도 <color=#FFD700>{(1f - card.effectValue) * 100f:0}%</color> 증가",
        "BonusDamageEffect"       => $"공격력 <color=#FFD700>{card.effectValue:0}%</color> 증가",
        "MultiShotEffect"         => $"좌우 <color=#87CEEB>{card.effectValue:0}도</color> 방향으로 추가 발사",
        "PiercingEffect"          => "<color=#87CEEB>관통</color> — 탄환이 적을 통과합니다",
        "ExplosiveEffect"         => $"착탄 시 반경 <color=#FFA500>{card.effectValue:0.0}m</color> 폭발",
        "SlashRadiusEffect"       => $"공격 범위 <color=#87CEEB>{card.effectValue:0.0}배</color> 확대",
        "OrbitRadiusEffect"       => $"오브 궤도 반경 <color=#87CEEB>{card.effectValue:0.0}배</color> 확대",
        "AttackCountEffect"       => $"오브/탄환 수 <color=#FFD700>+{card.effectValue:0}</color>",
        "ConditionalDamageEffect" => $"HP <color=#FF8888>{card.effectValue:0}%</color> 이하 시 공격력 <color=#FFD700>×{card.effectValue2:0.0}</color>",
        "BulletSpeedEffect"       => $"탄환 속도 <color=#87CEEB>{card.effectValue:0.0}배</color>",
        _                        => string.Empty
    };

    private static Color RarityToColor(CardRarity rarity) => rarity switch
    {
        CardRarity.Common    => new Color(0.70f, 0.70f, 0.70f),
        CardRarity.Uncommon  => new Color(0.20f, 0.70f, 0.30f),
        CardRarity.Rare      => new Color(0.20f, 0.40f, 0.90f),
        CardRarity.Legendary => new Color(0.90f, 0.60f, 0.10f),
        _                   => Color.white
    };
}
