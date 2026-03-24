using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// WorldDefinition의 팔레트를 씬 전역 앰비언트 조명에 적용합니다.
/// 머티리얼별 색 재정의를 하지 않으므로 URP/Lit 등 표준 셰이더와 호환됩니다.
/// 세계 전환 시 <see cref="WorldDefinition.paletteFadeDuration"/> 동안 부드럽게 보간합니다.
/// </summary>
public class WorldPaletteController : MonoBehaviour
{
    // 현재 적용된 팔레트 (보간 시작점으로 사용)
    private Color _curAmbient;
    private Color _curOutline;

    private Coroutine _fadeCoroutine;

    // ── 생명주기 ──────────────────────────────────────────────────────────────

    private void OnEnable()  => RunManager.OnWorldChosen += OnWorldChosen;
    private void OnDisable() => RunManager.OnWorldChosen -= OnWorldChosen;

    private void Start()
    {
        var world = RunManager.Instance?.SelectedWorld;
        if (world != null) ApplyPaletteImmediate(world);
    }

    // ── 내부 ──────────────────────────────────────────────────────────────────

    private void OnWorldChosen(WorldDefinition world)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

        if (world.paletteFadeDuration <= 0f)
        {
            ApplyPaletteImmediate(world);
            return;
        }

        _fadeCoroutine = StartCoroutine(FadePalette(world));
    }

    private IEnumerator FadePalette(WorldDefinition world)
    {
        Color fromAmbient = _curAmbient;
        Color fromOutline = _curOutline;

        float elapsed  = 0f;
        float duration = world.paletteFadeDuration;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            ApplyToScene(
                Color.Lerp(fromAmbient, world.ambientColor, t),
                Color.Lerp(fromOutline, world.outlineColor, t)
            );
            yield return null;
        }

        ApplyPaletteImmediate(world);
        _fadeCoroutine = null;
    }

    private void ApplyPaletteImmediate(WorldDefinition world)
    {
        ApplyToScene(world.ambientColor, world.outlineColor);
    }

    /// <summary>
    /// 씬 전역 앰비언트 조명에 색을 적용합니다.
    /// URP/Lit의 앰비언트는 Unity RenderSettings로 제어합니다.
    /// </summary>
    private void ApplyToScene(Color ambient, Color outline)
    {
        _curAmbient = ambient;
        _curOutline = outline;

        // 전역 앰비언트 조명 변경 → 모든 URP/Lit 오브젝트에 자동 반영됩니다.
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;

        // 아웃라인 색은 추후 PixelRenderFeature 엣지 색상 연동 시 사용합니다.
    }
}
