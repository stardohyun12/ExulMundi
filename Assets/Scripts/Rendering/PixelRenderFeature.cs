using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// ── ARCHITECTURE NOTE ─────────────────────────────────────────────────────────
// camera.targetTexture approach was removed. Setting it caused "No cameras rendering"
// in the Game View because URP stopped writing to Display 1.
// Instead: two RenderGraph passes handle downscale → upscale within the normal pipeline.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// URP ScriptableRendererFeature — pixel-art rendering pipeline.
///
/// Pipeline (two RenderGraph passes per frame):
///   Pass 1 Downscale : activeColorTexture (full-res) → _LowResTemp (320×180, point filter)
///   Pass 2 Upscale   : _LowResTemp → backBufferColor (integer scale + PixelArtEffects)
///
/// PixelArtLit.shader provides flat 2-tone shading + quantized normals at full res.
/// Point-downscaling to 320×180 then locks in the blocky pixel-art look.
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

        [Header("Edge-Only Dithering")]
        [Range(0f, 1f)]      public float ditherStrength  = 0.55f;
        [Range(0.01f, 0.5f)] public float ditherEdgeWidth = 0.18f;

        [Header("Edge Detection")]
        [Range(0f, 1f)] public float edgeThreshold = 0.12f;
        public Color edgeColor = Color.black;

        [Header("Debug")]
        [Tooltip("RawLowRes: no effects. PixelGrid: red grid at pixel boundaries.")]
        public DebugMode debugMode = DebugMode.Off;
    }

    // ── Fields ────────────────────────────────────────────────────────────────

    public PixelSettings settings = new();

    private PixelArtPass _pass;
    private Material     _effectsMaterial;

    private static readonly int PixelTexelSizeId  = Shader.PropertyToID("_PixelTexelSize");
    private static readonly int DitherStrengthId  = Shader.PropertyToID("_DitherStrength");
    private static readonly int DitherEdgeWidthId = Shader.PropertyToID("_DitherEdgeWidth");
    private static readonly int EdgeThresholdId   = Shader.PropertyToID("_EdgeThreshold");
    private static readonly int EdgeColorId       = Shader.PropertyToID("_EdgeColor");

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

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_pass == null || _effectsMaterial == null) return;

        var cameraData = renderingData.cameraData;
        if (cameraData.cameraType != CameraType.Game)       return;
        if (cameraData.renderType != CameraRenderType.Base) return;

        int pixelW = Mathf.Max(1, settings.pixelWidth);
        int pixelH = Mathf.Max(1, settings.pixelHeight);

        // Sync material properties on main thread before RecordRenderGraph.
        _effectsMaterial.SetVector(PixelTexelSizeId,
            new Vector4(1f / pixelW, 1f / pixelH, pixelW, pixelH));
        _effectsMaterial.SetFloat(DitherStrengthId,  settings.ditherStrength);
        _effectsMaterial.SetFloat(DitherEdgeWidthId, settings.ditherEdgeWidth);
        _effectsMaterial.SetFloat(EdgeThresholdId,   settings.edgeThreshold);
        _effectsMaterial.SetColor(EdgeColorId,       settings.edgeColor);

        // Force AA off.
        var cam     = cameraData.camera;
        var urpData = cam.GetUniversalAdditionalCameraData();
        if (cam.allowMSAA) cam.allowMSAA = false;
        if (urpData != null && urpData.antialiasing != AntialiasingMode.None)
            urpData.antialiasing = AntialiasingMode.None;

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

        public PixelArtPass(PixelSettings settings, Material material)
        {
            _settings = settings;
            _material = material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData   = frameData.Get<UniversalCameraData>();

            TextureHandle src  = resourceData.activeColorTexture;
            TextureHandle dest = resourceData.backBufferColor;

            if (!src.IsValid() || !dest.IsValid())
            {
                Debug.LogWarning("[PixelRenderFeature] Invalid texture handles.");
                return;
            }

            int pixelW = Mathf.Max(1, _settings.pixelWidth);
            int pixelH = Mathf.Max(1, _settings.pixelHeight);

            // Temporary low-res texture — lives only for this frame.
            RenderTextureDescriptor lowResDesc = cameraData.cameraTargetDescriptor;
            lowResDesc.width           = pixelW;
            lowResDesc.height          = pixelH;
            lowResDesc.depthBufferBits = 0;
            lowResDesc.msaaSamples     = 1;

            TextureHandle lowRes = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, lowResDesc, "_PixelArtLowRes", false, FilterMode.Point);

            // Integer-scale viewport, letterboxed.
            int   screenW = Screen.width;
            int   screenH = Screen.height;
            int   scale   = Mathf.Min(Mathf.Max(1, screenW / pixelW),
                                      Mathf.Max(1, screenH / pixelH));
            float scaledW = pixelW * scale;
            float scaledH = pixelH * scale;

            var viewport = new Rect(
                Mathf.Floor((screenW - scaledW) * 0.5f),
                Mathf.Floor((screenH - scaledH) * 0.5f),
                scaledW, scaledH);

            int shaderPass = _settings.debugMode switch
            {
                DebugMode.RawLowRes => 1,
                DebugMode.PixelGrid => 2,
                _                  => 0,
            };

            // ── Pass 1: Downscale full-res → 320×180 (point, no blur) ──────────
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

            // ── Pass 2: Upscale 320×180 → screen with PixelArtEffects ─────────
            using (var builder = renderGraph.AddRasterRenderPass<UpPassData>(
                       "PixelArt Upscale + Effects", out var data))
            {
                data.source     = lowRes;
                data.material   = _material;
                data.viewport   = viewport;
                data.shaderPass = shaderPass;

                builder.UseTexture(lowRes);
                builder.SetRenderAttachment(dest, 0);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (UpPassData d, RasterGraphContext ctx) =>
                {
                    ctx.cmd.ClearRenderTarget(false, true, Color.black);
                    ctx.cmd.SetViewport(d.viewport);
                    Blitter.BlitTexture(ctx.cmd, d.source,
                        new Vector4(1, 1, 0, 0), d.material, d.shaderPass);
                });
            }
        }

        // ── Pass data ─────────────────────────────────────────────────────────

        private class DownPassData
        {
            public TextureHandle source;
        }

        private class UpPassData
        {
            public TextureHandle source;
            public Material      material;
            public Rect          viewport;
            public int           shaderPass;
        }
    }
}
