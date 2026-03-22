// PixelArtEffects.shader
// ─────────────────────────────────────────────────────────────────────────────
// POST-PROCESS PASS — operates on the 320×180 low-res RT rendered by PixelArtLit.
//
// Three shader passes (selected by PixelRenderFeature based on DebugMode):
//
//   Pass 0 — Full pixel-art effects (normal output)
//     1. Pixel-centered UV snap     → each upscaled pixel reads exactly its center
//     2. Luma-directed palette snap → shadow sub-palette / light sub-palette
//     3. Edge-constrained dithering → Bayer pattern only at the boundary
//     4. Sobel edge overlay         → hard 1px outlines
//
//   Pass 1 — Debug: Raw low-res
//     Pure point-sampled upscale, zero effects.
//     USE THIS FIRST. If the Game view shows visible pixel blocks, the low-res RT
//     is working correctly. If it looks smooth, camera.targetTexture is not applied.
//
//   Pass 2 — Debug: Pixel grid
//     Point-sampled color + red grid lines at every low-res pixel boundary.
//     Confirms that each on-screen block maps to exactly one low-res pixel.
// ─────────────────────────────────────────────────────────────────────────────
Shader "Custom/PixelArtEffects"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }

        // ── Shared code (all passes) ──────────────────────────────────────────
        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D_X(_BlitTexture);
        float4 _BlitScaleBias;

        // Set by PixelRenderFeature: x=1/w, y=1/h, z=w, w=h (low-res RT dimensions)
        float4 _PixelTexelSize;

        float _DitherStrength;
        float _DitherEdgeWidth;
        float _EdgeThreshold;
        half4 _EdgeColor;

        // ── Vertex (shared by all passes) ─────────────────────────────────────
        struct Attributes { uint vertexID : SV_VertexID; };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes IN)
        {
            Varyings OUT;
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
            OUT.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
            OUT.texcoord   = GetFullScreenTriangleTexCoord(IN.vertexID)
                             * _BlitScaleBias.xy + _BlitScaleBias.zw;
            return OUT;
        }

        // ── Pixel-centered UV snap ────────────────────────────────────────────
        // Each fragment in a given on-screen block maps to one low-res pixel.
        // Adding 0.5 samples the CENTER of that pixel, not its edge.
        // Without this snap, upscaling at non-integer positions introduces bilinear blur.
        float2 SnapUV(float2 texcoord, out float2 pixelPos)
        {
            pixelPos = floor(texcoord * _PixelTexelSize.zw);
            return (pixelPos + 0.5) / _PixelTexelSize.zw;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        float Luma(half3 c) { return dot(c, half3(0.2126, 0.7152, 0.0722)); }

        // Bayer 4×4. pixelPos MUST be integer grid coordinates (from SnapUV).
        // Using continuous UV here would make the pattern swim with the camera.
        float Bayer4(float2 pixelPos)
        {
            static const float m[16] =
            {
                 0,  8,  2, 10,
                12,  4, 14,  6,
                 3, 11,  1,  9,
                15,  7, 13,  5
            };
            uint x = (uint)pixelPos.x % 4;
            uint y = (uint)pixelPos.y % 4;
            return m[y * 4 + x] / 16.0;
        }

        // Sobel 3×3 on the point-sampled low-res buffer.
        float SobelEdge(float2 uv)
        {
            float2 d  = _PixelTexelSize.xy;
            float  tl = Luma(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2(-d.x,  d.y)).rgb);
            float  tc = Luma(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2( 0.0,  d.y)).rgb);
            float  tr = Luma(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2( d.x,  d.y)).rgb);
            float  ml = Luma(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2(-d.x,  0.0)).rgb);
            float  mr = Luma(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2( d.x,  0.0)).rgb);
            float  bl = Luma(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2(-d.x, -d.y)).rgb);
            float  bc = Luma(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2( 0.0, -d.y)).rgb);
            float  br = Luma(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2( d.x, -d.y)).rgb);
            float  gx = (-tl - 2.0*ml - bl) + (tr + 2.0*mr + br);
            float  gy = ( tl + 2.0*tc + tr) - (bl + 2.0*bc + br);
            return saturate(sqrt(gx*gx + gy*gy));
        }

        // ── Palette (luma-directed sub-range) ─────────────────────────────────
        // Two separate palettes keyed by luma, not by RGB distance.
        // This prevents a shadow pixel from being snapped to a light palette color.
        //
        // When the dither offset pushes luma across the 0.5 threshold, PaletteSnap
        // selects from the OTHER sub-palette, creating the checkerboard hatch pattern.
        //
        // Colors are in LINEAR space. Values sourced from Endesga-32 earthy tones.
        #define SHADOW_COUNT 3
        #define LIGHT_COUNT  3

        static const half3 SHADOW_PALETTE[SHADOW_COUNT] =
        {
            half3(0.03, 0.02, 0.06),   // deep shadow / near-black
            half3(0.14, 0.10, 0.22),   // standard shadow
            half3(0.30, 0.18, 0.18)    // bright shadow / boundary tone (reached by dither)
        };

        static const half3 LIGHT_PALETTE[LIGHT_COUNT] =
        {
            half3(0.42, 0.28, 0.15),   // dim lit / boundary tone (reached by dither)
            half3(0.72, 0.52, 0.28),   // standard lit
            half3(0.90, 0.76, 0.48)    // highlight / specular
        };

        float ColorDistSq(half3 a, half3 b) { half3 d = a - b; return dot(d, d); }

        half3 PaletteSnap(half3 color, float luma)
        {
            float bestDist = 1e9;
            half3 bestCol;

            if (luma < 0.5)
            {
                bestCol = SHADOW_PALETTE[0];
                for (int i = 0; i < SHADOW_COUNT; ++i)
                {
                    float d = ColorDistSq(color, SHADOW_PALETTE[i]);
                    if (d < bestDist) { bestDist = d; bestCol = SHADOW_PALETTE[i]; }
                }
            }
            else
            {
                bestCol = LIGHT_PALETTE[0];
                for (int j = 0; j < LIGHT_COUNT; ++j)
                {
                    float d = ColorDistSq(color, LIGHT_PALETTE[j]);
                    if (d < bestDist) { bestDist = d; bestCol = LIGHT_PALETTE[j]; }
                }
            }
            return bestCol;
        }

        ENDHLSL

        // ── Pass 0: Full pixel-art effects ────────────────────────────────────
        Pass
        {
            Name "PixelArtEffects"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment FragEffects

            half4 FragEffects(Varyings IN) : SV_Target
            {
                // 1. Pixel-centered UV snap.
                float2 pixelPos;
                float2 snappedUV = SnapUV(IN.texcoord, pixelPos);

                // 2. Point sample — zero interpolation.
                half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, snappedUV).rgb;
                float luma  = Luma(color);

                // 3. Sobel edge on raw 2-tone input, before dither modifies luma.
                float edge = SobelEdge(snappedUV);

                // 4. Edge-constrained Bayer dithering.
                //    edgeFactor = 1 at luma=0.5 (boundary), 0 in solid shadow/light.
                //    The dither offset nudges luma across 0.5, causing PaletteSnap to select
                //    from the OTHER sub-palette in a Bayer-stable pattern.
                float edgeFactor   = saturate(1.0 - abs(luma - 0.5) / max(_DitherEdgeWidth, 0.01));
                float bayer        = Bayer4(pixelPos) - 0.5;   // [-0.5, +0.5)
                float ditherOffset = bayer * _DitherStrength * edgeFactor;
                float ditherLuma   = luma + ditherOffset;
                half3 ditherColor  = saturate(color + ditherOffset);

                // 5. Luma-directed palette snap.
                color = PaletteSnap(ditherColor, ditherLuma);

                // 6. Hard 1px Sobel outline. step() — no anti-aliasing.
                color = lerp(color, _EdgeColor.rgb, step(_EdgeThreshold, edge) * _EdgeColor.a);

                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // ── Pass 1: Debug — raw low-res ───────────────────────────────────────
        // Zero effects. Only pixel-centered point sampling.
        // Enable this first to confirm the RT is truly 320×180.
        // If the Game view shows hard pixel blocks, the pipeline is correct.
        // If it looks smooth or high-res, camera.targetTexture is not being applied.
        Pass
        {
            Name "DebugRawLowRes"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment FragDebugRaw

            half4 FragDebugRaw(Varyings IN) : SV_Target
            {
                float2 pixelPos;
                float2 snappedUV = SnapUV(IN.texcoord, pixelPos);
                half3  color     = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, snappedUV).rgb;
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // ── Pass 2: Debug — pixel grid ────────────────────────────────────────
        // Point-sampled color + red grid lines at each low-res pixel boundary.
        // The grid lines are 1 screen-pixel wide, drawn using fwidth() for
        // screen-space derivative — scale-independent at any upscale factor.
        Pass
        {
            Name "DebugPixelGrid"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment FragDebugGrid

            half4 FragDebugGrid(Varyings IN) : SV_Target
            {
                float2 pixelPos;
                float2 snappedUV = SnapUV(IN.texcoord, pixelPos);
                half3  color     = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, snappedUV).rgb;

                // Fractional position [0,1) within each low-res pixel in screen space.
                float2 subPixel = frac(IN.texcoord * _PixelTexelSize.zw);

                // fwidth gives the per-screen-pixel step size in low-res pixel coords.
                // Comparing subPixel to fwidth draws exactly 1 screen-pixel wide lines
                // at the leading edge of each low-res pixel — scale-invariant.
                float2 fw      = fwidth(IN.texcoord * _PixelTexelSize.zw);
                float  gridX   = step(subPixel.x, fw.x);
                float  gridY   = step(subPixel.y, fw.y);
                float  grid    = saturate(gridX + gridY);

                // Blend red grid lines over the color at 70% opacity.
                color = lerp(color, half3(1.0, 0.0, 0.0), grid * 0.7);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
