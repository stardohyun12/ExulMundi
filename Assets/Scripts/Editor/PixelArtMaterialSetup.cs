#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 3D 오브젝트 머티리얼을 Custom/CelShading3D 셰이더로 전환합니다.
/// - 조명/그림자: Directional Light에 smoothstep으로 자연스럽게 반응
/// - 셀 쉐이딩: _ShadowStep/_ShadowSmooth로 경계 조절
/// - 앰비언트: SampleSH → WorldPaletteController의 RenderSettings.ambientLight 자동 반영
/// Tools > Setup Pixel Art Materials 메뉴에서 실행하세요.
/// </summary>
public static class PixelArtMaterialSetup
{
    private const string ShaderName = "Custom/CelShading3D";

    [MenuItem("Tools/Setup Pixel Art Materials")]
    public static void SetupMaterials()
    {
        var shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"[PixelArtMaterialSetup] '{ShaderName}' 셰이더를 찾을 수 없습니다.");
            return;
        }

        // baseColor   : 오브젝트 고유 색
        // shadowColor : 그림자 영역 색 (어둡고 약간 채도 낮게)
        Apply("Assets/Materials/Player_Cel.mat", shader,
            baseColor:   new Color(0.35f, 0.60f, 0.95f, 1f),
            shadowColor: new Color(0.12f, 0.20f, 0.50f, 1f));

        Apply("Assets/Materials/Ground_Cel.mat", shader,
            baseColor:   new Color(0.40f, 0.62f, 0.25f, 1f),
            shadowColor: new Color(0.15f, 0.28f, 0.10f, 1f));

        Apply("Assets/Materials/Enemy_Cel.mat", shader,
            baseColor:   new Color(0.90f, 0.30f, 0.25f, 1f),
            shadowColor: new Color(0.40f, 0.10f, 0.08f, 1f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PixelArtMaterialSetup] 완료: 3개 머티리얼 → CelShading3D");
    }

    /// <summary>머티리얼을 CelShading3D로 전환하고 색 속성을 설정합니다.</summary>
    private static void Apply(string path, Shader shader, Color baseColor, Color shadowColor)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { Debug.LogWarning($"[PixelArtMaterialSetup] 없음: {path}"); return; }

        mat.shader = shader;

        // 오브젝트 고유 색과 그림자 색.
        mat.SetColor("_BaseColor",   baseColor);
        mat.SetColor("_ShadowColor", shadowColor);

        // PixelArtLit과 동일한 어두운 앰비언트 상수로 고정.
        // SampleSH × 이 값 = 거의 검정 → 스카이박스 색감 영향 제거.
        // WorldPaletteController가 씬 전역 앰비언트를 교체할 때 이 값이 가중치 역할.
        mat.SetColor("_AmbientColor", new Color(0.05f, 0.04f, 0.08f, 1f));

        // 셀 쉐이딩 경계: _ShadowStep=0 → 수직면이 정확히 명암 경계.
        // _ShadowSmooth: 약간의 smoothstep 폭. 너무 크면 그라디언트처럼 보임.
        mat.SetFloat("_ShadowStep",   0.0f);
        mat.SetFloat("_ShadowSmooth", 0.05f);

        // 스페큘러 비활성화.
        mat.SetFloat("_SpecularStep",   1.0f);
        mat.SetFloat("_SpecularSmooth", 0.0f);

        // 림 라이트 비활성화.
        mat.SetFloat("_RimAmount", 0.0f);

        // 아웃라인: CelShading3D의 Cull Front 패스가 담당.
        mat.SetColor("_OutlineColor", new Color(0.02f, 0.01f, 0.05f, 1f));
        mat.SetFloat("_OutlineWidth", 0.03f);

        EditorUtility.SetDirty(mat);
        Debug.Log($"[PixelArtMaterialSetup] 적용: {path}");
    }
}
#endif
