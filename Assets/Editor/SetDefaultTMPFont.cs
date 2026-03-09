using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// NotoSansKR-Regular SDF를 TMP 기본 폰트로 설정합니다.
/// 에디터 시작 시 자동 실행되며, Tools → Set Korean TMP Font 로도 실행 가능합니다.
/// </summary>
[InitializeOnLoad]
public static class SetDefaultTMPFont
{
    private const string FontAssetPath =
        "Assets/Fonts/Noto_Sans_KR/static/NotoSansKR-Regular SDF.asset";

    // 에디터 시작 시 자동 실행
    static SetDefaultTMPFont() => Run();

    [MenuItem("Tools/Set Korean TMP Font")]
    public static void Run()
    {
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            Debug.LogError($"[SetDefaultTMPFont] 폰트 에셋을 찾을 수 없습니다: {FontAssetPath}");
            return;
        }

        var settings = TMP_Settings.instance;
        if (settings == null) return;

        var field = typeof(TMP_Settings).GetField(
            "m_defaultFontAsset",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        if (field == null) return;

        var current = field.GetValue(settings) as TMP_FontAsset;
        if (current == fontAsset) return; // 이미 설정됨 — 스킵

        field.SetValue(settings, fontAsset);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SetDefaultTMPFont] 기본 폰트 → '{fontAsset.name}'");
    }
}
