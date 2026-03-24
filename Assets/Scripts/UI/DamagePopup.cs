using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 월드 좌표에서 데미지/회복 숫자 팝업을 Canvas에 스폰합니다.
/// 위로 떠오르며 페이드아웃됩니다. 데미지: 빨강, 회복: 초록.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private const float RiseSpeed    = 60f;
    private const float FadeDelay    = 0.3f;
    private const float FadeDuration = 0.4f;

    private static readonly Color ColorDamage = new(1.0f, 0.2f, 0.2f, 1f);
    private static readonly Color ColorHeal   = new(0.2f, 1.0f, 0.4f, 1f);

    public static DamagePopup Instance { get; private set; }

    [SerializeField] private Canvas    hudCanvas;
    [SerializeField] private GameObject popupPrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>월드 좌표에서 데미지 팝업을 생성합니다.</summary>
    public void ShowDamage(Vector3 worldPos, int amount)
        => Spawn(worldPos, $"-{amount}", ColorDamage);

    /// <summary>월드 좌표에서 회복 팝업을 생성합니다.</summary>
    public void ShowHeal(Vector3 worldPos, int amount)
        => Spawn(worldPos, $"+{amount}", ColorHeal);

    private void Spawn(Vector3 worldPos, string text, Color color)
    {
        if (hudCanvas == null)
        {
            hudCanvas = FindFirstObjectByType<Canvas>();
            if (hudCanvas == null) return;
        }

        var go  = popupPrefab != null
                  ? Instantiate(popupPrefab, hudCanvas.transform)
                  : CreateDefaultPopup(hudCanvas.transform);

        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text  = text;
            tmp.color = color;
        }

        // 월드 → Canvas 로컬 좌표 변환
        var rt        = go.GetComponent<RectTransform>();
        var screenPos = Camera.main.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            hudCanvas.GetComponent<RectTransform>(),
            screenPos,
            hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hudCanvas.worldCamera,
            out var localPoint);

        if (rt != null) rt.anchoredPosition = localPoint;

        StartCoroutine(AnimatePopup(go, tmp));
    }

    private IEnumerator AnimatePopup(GameObject go, TextMeshProUGUI tmp)
    {
        if (go == null) yield break;

        var rt     = go.GetComponent<RectTransform>();
        var startY = rt != null ? rt.anchoredPosition.y : 0f;
        float elapsed = 0f;

        // 상승
        while (elapsed < FadeDelay && go != null)
        {
            elapsed += Time.deltaTime;
            if (rt != null) rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, startY + elapsed * RiseSpeed);
            yield return null;
        }

        // 페이드아웃
        elapsed = 0f;
        while (elapsed < FadeDuration && go != null)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / FadeDuration);
            if (tmp != null)
            {
                var c = tmp.color;
                tmp.color = new Color(c.r, c.g, c.b, alpha);
            }
            if (rt != null) rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, startY + (FadeDelay + elapsed) * RiseSpeed);
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    private static GameObject CreateDefaultPopup(Transform parent)
    {
        var go         = new GameObject("DamagePopup");
        go.transform.SetParent(parent, false);
        var rt         = go.AddComponent<RectTransform>();
        rt.sizeDelta   = new Vector2(80f, 30f);
        var tmp        = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize   = 22f;
        tmp.fontStyle  = FontStyles.Bold;
        tmp.alignment  = TextAlignmentOptions.Center;
        go.AddComponent<LayoutElement>().ignoreLayout = true;
        return go;
    }
}
