// PixelArtEffects.shader
// ─────────────────────────────────────────────────────────────────────────────
// 역할: 해상도 픽셀화 전담. 색/아웃라인/조명은 CelShading3D가 담당.
//
// Pass 0 — Pixel  : UV 스냅 + 포인트 샘플 (업스케일 블러 제거만)
// Pass 1 — Raw    : 동일 (디버그용 별칭)
// Pass 2 — Grid   : 포인트 샘플 + 저해상도 픽셀 경계 빨간 격자
// ─────────────────────────────────────────────────────────────────────────────
Shader "Custom/PixelArtEffects"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D_X(_BlitTexture);
        float4 _BlitScaleBias;

        // x=1/w, y=1/h, z=w, w=h  (저해상도 RT 크기)
        float4 _PixelTexelSize;

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

        // UV를 저해상도 픽셀 중앙으로 스냅.
        // 업스케일 시 각 화면 픽셀이 동일한 텍셀을 읽도록 보장 (블러 방지).
        float2 SnapUV(float2 uv, out float2 pixelPos)
        {
            pixelPos = floor(uv * _PixelTexelSize.zw);
            return (pixelPos + 0.5) / _PixelTexelSize.zw;
        }

        ENDHLSL

        // ── Pass 0: 픽셀화 (UV 스냅 + 포인트 샘플만) ─────────────────────────
        Pass
        {
            Name "PixelArtUpscale"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment FragPixel

            half4 FragPixel(Varyings IN) : SV_Target
            {
                float2 pixelPos;
                float2 uv = SnapUV(IN.texcoord, pixelPos);
                return half4(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rgb, 1.0);
            }
            ENDHLSL
        }

        // ── Pass 1: Debug Raw (Pass 0와 동일) ────────────────────────────────
        Pass
        {
            Name "DebugRawLowRes"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment FragRaw

            half4 FragRaw(Varyings IN) : SV_Target
            {
                float2 pixelPos;
                float2 uv = SnapUV(IN.texcoord, pixelPos);
                return half4(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rgb, 1.0);
            }
            ENDHLSL
        }

        // ── Pass 2: Debug Grid (저해상도 픽셀 경계 시각화) ───────────────────
        Pass
        {
            Name "DebugPixelGrid"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment FragGrid

            half4 FragGrid(Varyings IN) : SV_Target
            {
                float2 pixelPos;
                float2 uv    = SnapUV(IN.texcoord, pixelPos);
                half3  color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rgb;

                float2 sub  = frac(IN.texcoord * _PixelTexelSize.zw);
                float2 fw   = fwidth(IN.texcoord * _PixelTexelSize.zw);
                float  grid = saturate(step(sub.x, fw.x) + step(sub.y, fw.y));

                color = lerp(color, half3(1.0, 0.0, 0.0), grid * 0.7);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
