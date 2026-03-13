using UnityEngine;
using TMPro;

/// <summary>
/// 씬의 모든 TextMeshProUGUI에 지정한 폰트를 런타임에 일괄 적용합니다.
/// Canvas 루트에 붙이고 Inspector에서 font 필드를 연결하세요.
/// </summary>
public class TMPFontApplier : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;

    private void Awake()
    {
        if (font == null)
        {
            Debug.LogWarning("[TMPFontApplier] font가 연결되지 않았습니다.");
            return;
        }

        foreach (var tmp in FindObjectsByType<TextMeshProUGUI>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            tmp.font = font;
            tmp.extraPadding = true;
        }

        Debug.Log($"[TMPFontApplier] 폰트 일괄 적용: {font.name} (extraPadding ON)");
    }
}
