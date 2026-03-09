using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// 씬과 프리팹의 모든 TMP 텍스트를 NotoSansKR-Regular SDF로 일괄 교체합니다.
/// Tools → Replace All TMP Fonts 로 실행하세요.
/// </summary>
public static class ReplaceTMPFont
{
    private const string KoreanFontPath =
        "Assets/Fonts/Noto_Sans_KR/static/NotoSansKR-Regular SDF.asset";

    [MenuItem("Tools/Replace All TMP Fonts")]
    public static void Run()
    {
        var newFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        if (newFont == null)
        {
            Debug.LogError($"[ReplaceTMPFont] 폰트를 찾을 수 없습니다: {KoreanFontPath}");
            return;
        }

        int count = 0;

        // ── 1. TMP_Settings 기본 폰트 교체 ──────────────────
        var settings = TMP_Settings.instance;
        if (settings != null)
        {
            var settingsField = typeof(TMP_Settings).GetField(
                "m_defaultFontAsset",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            settingsField?.SetValue(settings, newFont);
            EditorUtility.SetDirty(settings);
        }

        // ── 2. 열린 씬의 모든 TMP 텍스트 교체 ───────────────
        var allTMPs = Object.FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var tmp in allTMPs)
        {
            if (tmp.font == newFont) continue;
            Undo.RecordObject(tmp, "Replace TMP Font");
            tmp.font = newFont;
            EditorUtility.SetDirty(tmp);
            count++;
        }

        // ── 3. 프리팹 교체 ───────────────────────────────────
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        foreach (var guid in prefabGuids)
        {
            string path   = AssetDatabase.GUIDToAssetPath(guid);
            var    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // missing script 제거
            bool removedMissing = false;
            foreach (var child in prefab.GetComponentsInChildren<Transform>(true))
            {
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
                if (removed > 0)
                {
                    removedMissing = true;
                    Debug.Log($"[ReplaceTMPFont] '{path}' 에서 missing script {removed}개 제거");
                }
            }

            var tmps = prefab.GetComponentsInChildren<TMP_Text>(true);
            bool changed = removedMissing;
            foreach (var tmp in tmps)
            {
                if (tmp.font == newFont) continue;
                tmp.font = newFont;
                changed  = true;
                count++;
            }

            if (changed)
            {
                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
            }
        }

        // ── 4. 저장 ──────────────────────────────────────────
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        Debug.Log($"[ReplaceTMPFont] 완료. {count}개의 TMP 텍스트를 '{newFont.name}'으로 교체했습니다.");
    }
}
