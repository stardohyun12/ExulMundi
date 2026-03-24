using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// ── ARCHITECTURE NOTE ─────────────────────────────────────────────────────────
// Unity 6 URP RenderGraph에서 cameraNormalsTexture는 URP 내장 기능(SSAO 등)이
// 요청해야만 생성된다. 외부 Feature의 UseTexture()만으로는 핸들이 생성되지 않음.
//
// 해결: NormalsPrepass를 직접 실행해 normalsRT를 채우고 글로벌 텍스처로 바인딩.
//   1. NormalsPrepass : DepthNormals LightMode 오브젝트 → normalsRT
//   2. Downscale      : activeColorTexture → LowResRT (320×180, Point filter)
//   3. Upscale        : LowResRT + normalsRT + CameraDepthTexture → 최종 컬러
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// URP ScriptableRendererFeature — pixel-art post-processing with outline.
/// </summary>
public class PixelRenderFeature : ScriptableRendererFeature
{
    // ── Public types ──────────────────────────────────────────────────────────

    public enum DebugMode
    {
        Off,
        RawLowRes,  // Point-upscale only — verify pixel blocks
        PixelGrid,  // Red grid at every low-res pixel boundary
    }

    [System.Serializable]
    public class PixelSettings
    {
        [Header("Internal Resolution")]
        [Tooltip("Scene is point-downscaled to this before upscaling with effects.")]
        [Min(1)] public int pixelWidth  = 320;
        [Min(1)] public int pixelHeight = 180;

        [Header("Soft Quantization")]
        [Tooltip("Levels per color channel. 8 = subtle banding. 4 = strong. 16 = nearly invisible.")]
        [Range(2f, 32f)] public float quantizeSteps = 8f;

        [Header("Outline — Depth  (외곽선 담당)")]
        [Tooltip("LinearEyeDepth 기준 threshold (단위: 미터). 이 값 이상 depth 차이에서 엣지 발생.")]
        [Range(0.01f, 1f)] public float outlineDepthThreshold = 0.1f;

        [Tooltip("depthDiff에 곱하는 배율.")]
        [Range(0.1f, 3f)] public float outlineDepthMultiplier = 1f;

        [Tooltip("depthDiff 최대값 (미터). 벽-바닥 경계 등 과도한 점프로 라인이 두꺼워지는 현상 방지.")]
        [Range(0.5f, 5f)] public float outlineDepthClamp = 2f;

        [Tooltip("대각선 방향 샘플 가중치. 0.707(1/√2)이 기하학적으로 정확한 값.")]
        [Range(0.3f, 1f)] public float outlineDiagWeight = 0.707f;

        [Header("Outline — Normal  (내부 모서리 보조)")]
        [Tooltip("Normal 차이의 smoothstep 시작점. 이 값 미만은 라인 없음.")]
        [Range(0f, 1f)] public float outlineNormalThreshold = 0.1f;

        [Tooltip("normalDiff에 곱하는 배율.")]
        [Range(0.5f, 2f)] public float outlineNormalMultiplier = 1f;

        [Tooltip("smoothstep 전환 폭. 클수록 경계가 부드러워져 작은 노이즈 억제.")]
        [Range(0.01f, 0.1f)] public float outlineNormalEpsilon = 0.02f;

        [Tooltip("Normal 채널 가중치. edgeDepth 게이트 구조상 내부 독립 기여 없음.")]
        [Range(0.3f, 1f)] public float outlineNormalWeight = 0.5f;

        [Header("Outline — Color")]
        public Color outlineColor = Color.black;

        [Header("Outline — Binarize")]
        [Tooltip("true: floor(edge) → 픽셀 퍼펙트 하드 1px. false: 연속값 블렌딩.")]
        public bool outlineBinarize = false;

        [Header("Debug")]
        [Tooltip("RawLowRes: no effects. PixelGrid: red grid at pixel boundaries.")]
        public DebugMode debugMode = DebugMode.Off;
    }

    // ── Fields ────────────────────────────────────────────────────────────────

    public PixelSettings settings = new();

    private PixelArtPass _pass;
    private Material     _effectsMaterial;

    private static readonly int PixelTexelSizeId           = Shader.PropertyToID("_PixelTexelSize");
    private static readonly int QuantizeStepsId             = Shader.PropertyToID("_QuantizeSteps");
    private static readonly int OutlineDepthThresholdId     = Shader.PropertyToID("_OutlineDepthThreshold");
    private static readonly int OutlineDepthMultiplierId    = Shader.PropertyToID("_OutlineDepthMultiplier");
    private static readonly int OutlineDepthClampId         = Shader.PropertyToID("_OutlineDepthClamp");
    private static readonly int OutlineDiagWeightId         = Shader.PropertyToID("_OutlineDiagWeight");
    private static readonly int OutlineNormalThresholdId    = Shader.PropertyToID("_OutlineNormalThreshold");
    private static readonly int OutlineNormalMultiplierId   = Shader.PropertyToID("_OutlineNormalMultiplier");
    private static readonly int OutlineNormalEpsilonId      = Shader.PropertyToID("_OutlineNormalEpsilon");
    private static readonly int OutlineNormalWeightId       = Shader.PropertyToID("_OutlineNormalWeight");
    private static readonly int OutlineColorId              = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineBinarizeId           = Shader.PropertyToID("_OutlineBinarize");

    // ── ScriptableRendererFeature API ─────────────────────────────────────────

    public override void Create()
    {
        var shader = Shader.Find("Custom/PixelArtEffects");
        if (shader == null)
        {
            Debug.LogError("[PixelRenderFeature] Shader 'Custom/PixelArtEffects' not found.");
            return;
        }

        _effectsMaterial = CoreUtils.CreateEngineMaterial(shader);

        _pass = new PixelArtPass(settings, _effectsMaterial)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer,
                                          ref RenderingData renderingData)
    {
        if (_pass == null || _effectsMaterial == null) return;

        var cameraData = renderingData.cameraData;
        if (cameraData.cameraType != CameraType.Game)       return;
        if (cameraData.renderType != CameraRenderType.Base) return;

        int pixelW = Mathf.Max(1, settings.pixelWidth);
        int pixelH = Mathf.Max(1, settings.pixelHeight);

        _effectsMaterial.SetVector(PixelTexelSizeId,
            new Vector4(1f / pixelW, 1f / pixelH, pixelW, pixelH));
        _effectsMaterial.SetFloat(QuantizeStepsId,            settings.quantizeSteps);
        _effectsMaterial.SetFloat(OutlineDepthThresholdId,    settings.outlineDepthThreshold);
        _effectsMaterial.SetFloat(OutlineDepthMultiplierId,   settings.outlineDepthMultiplier);
        _effectsMaterial.SetFloat(OutlineDepthClampId,        settings.outlineDepthClamp);
        _effectsMaterial.SetFloat(OutlineDiagWeightId,        settings.outlineDiagWeight);
        _effectsMaterial.SetFloat(OutlineNormalThresholdId,   settings.outlineNormalThreshold);
        _effectsMaterial.SetFloat(OutlineNormalMultiplierId,  settings.outlineNormalMultiplier);
        _effectsMaterial.SetFloat(OutlineNormalEpsilonId,     settings.outlineNormalEpsilon);
        _effectsMaterial.SetFloat(OutlineNormalWeightId,      settings.outlineNormalWeight);
        _effectsMaterial.SetColor(OutlineColorId,             settings.outlineColor);
        _effectsMaterial.SetFloat(OutlineBinarizeId,          settings.outlineBinarize ? 1f : 0f);

        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_effectsMaterial);
    }

    // ── RenderGraph pass ──────────────────────────────────────────────────────

    private class PixelArtPass : ScriptableRenderPass
    {
        private readonly PixelSettings _settings;
        private readonly Material      _material;

        private static readonly List<ShaderTagId> DepthNormalsTags = new List<ShaderTagId>
        {
            new ShaderTagId("DepthNormals"),
            new ShaderTagId("DepthNormalsOnly"),
        };
        private static readonly int NormalsTexId = Shader.PropertyToID("_CameraNormalsTexture");

        public PixelArtPass(PixelSettings settings, Material material)
        {
            _settings = settings;
            _material = material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData  = frameData.Get<UniversalResourceData>();
            var cameraData    = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData     = frameData.Get<UniversalLightData>();

            TextureHandle src = resourceData.activeColorTexture;
            if (!src.IsValid()) return;

            var desc   = cameraData.cameraTargetDescriptor;
            int fullW  = desc.width;
            int fullH  = desc.height;
            int pixelW = Mathf.Max(1, _settings.pixelWidth);
            int pixelH = Mathf.Max(1, _settings.pixelHeight);

            int shaderPass = _settings.debugMode switch
            {
                DebugMode.RawLowRes => 1,
                DebugMode.PixelGrid => 2,
                _                  => 0,
            };

            // ── Normals RT ───────────────────────────────────────────────────
            // URP cameraNormalsTexture를 기다리지 않고 직접 생성.
            var normalsDesc = new TextureDesc(fullW, fullH)
            {
                colorFormat     = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                depthBufferBits = DepthBits.None,
                msaaSamples     = MSAASamples.None,
                filterMode      = FilterMode.Point,
                clearBuffer     = true,
                clearColor      = new Color(0.5f, 0.5f, 1f, 1f),
                name            = "PixelNormalsRT"
            };
            TextureHandle normalsRT = renderGraph.CreateTexture(normalsDesc);

            // ── Pass 1: NormalsPrepass (RasterPass) ───────────────────────────
            // DepthNormals / DepthNormalsOnly LightMode를 가진 오브젝트를 normalsRT에 렌더링.
            // activeDepthTexture를 depth attachment로 공유 → 별도 depth RT 불필요.
            // SetGlobalTextureAfterPass: 이 패스 완료 후 _CameraNormalsTexture 글로벌 바인딩.
            using (var builder = renderGraph.AddRasterRenderPass<NormalsPassData>(
                       "PixelArt NormalsPrepass", out var data))
            {
                builder.SetRenderAttachment(normalsRT, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);

                var sortFlags    = cameraData.defaultOpaqueSortFlags;
                var drawSettings = RenderingUtils.CreateDrawingSettings(
                    DepthNormalsTags, renderingData, cameraData, lightData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;

                var filterSettings = new FilteringSettings(RenderQueueRange.opaque);

                data.rendererList = renderGraph.CreateRendererList(
                    new RendererListParams(renderingData.cullResults, drawSettings, filterSettings));

                builder.UseRendererList(data.rendererList);

                // 패스 완료 후 normalsRT를 _CameraNormalsTexture 슬롯에 글로벌 바인딩
                builder.SetGlobalTextureAfterPass(normalsRT, NormalsTexId);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (NormalsPassData d, RasterGraphContext ctx) =>
                {
                    ctx.cmd.DrawRendererList(d.rendererList);
                });
            }

            // ── Pass 2: Downscale ─────────────────────────────────────────────
            TextureHandle lowRes = renderGraph.CreateTexture(new TextureDesc(pixelW, pixelH)
            {
                colorFormat     = desc.graphicsFormat,
                depthBufferBits = DepthBits.None,
                msaaSamples     = MSAASamples.None,
                filterMode      = FilterMode.Point,
                clearBuffer     = true,
                clearColor      = Color.black,
                name            = "LowResPixelRT"
            });

            using (var builder = renderGraph.AddRasterRenderPass<DownPassData>(
                       "PixelArt Downscale", out var data))
            {
                data.source = src;
                builder.UseTexture(src);
                builder.SetRenderAttachment(lowRes, 0);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (DownPassData d, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1, 1, 0, 0), 0, false);
                });
            }

            // ── Pass 3: Upscale + Outline ─────────────────────────────────────
            // normalsRT는 SetGlobalTextureAfterPass로 _CameraNormalsTexture에 바인딩됨.
            // UseTexture(normalsRT): 이 패스가 normalsRT를 읽는다고 RenderGraph에 선언.
            TextureHandle depthTex = resourceData.cameraDepthTexture;

            using (var builder = renderGraph.AddRasterRenderPass<UpPassData>(
                       "PixelArt Upscale", out var data))
            {
                data.source     = lowRes;
                data.material   = _material;
                data.shaderPass = shaderPass;

                builder.UseTexture(lowRes);
                builder.UseTexture(normalsRT);
                if (depthTex.IsValid()) builder.UseTexture(depthTex);

                builder.SetRenderAttachment(src, 0);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (UpPassData d, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, d.source,
                        new Vector4(1, 1, 0, 0), d.material, d.shaderPass);
                });
            }
        }

        // ── Pass 데이터 클래스 ────────────────────────────────────────────────

        private class NormalsPassData
        {
            public RendererListHandle rendererList;
        }

        private class DownPassData
        {
            public TextureHandle source;
        }

        private class UpPassData
        {
            public TextureHandle source;
            public Material      material;
            public int           shaderPass;
        }
    }
}
