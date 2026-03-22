Shader "Custom/PixelArtEffect"
{
    Properties
    {
        // Auto-assigned by FullScreenPassRendererFeature.
        [HideInInspector] _BlitTexture ("Blit Texture", 2D) = "white" {}

        [Header(Pixelation)]
        [Space(4)]
        _PixelSize       ("Pixel Size (px)", Float)        = 4.0

        [Header(Outline)]
        [Space(4)]
        _OutlineColor     ("Outline Color",    Color)          = (0.05, 0.05, 0.05, 1)
        _OutlineThreshold ("Edge Threshold",   Range(0.01, 1)) = 0.12
        _ColorEdgeWeight  ("Color Edge Weight",Float)          = 4.0

        [Header(Color Quantization)]
        [Space(4)]
        [Toggle(_COLOR_QUANT)] _ColorQuantEnabled ("Enable Color Quantization", Float) = 0
        _ColorSteps      ("Color Steps (per channel)", Float) = 8.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        ZTest  Always
        Blend  Off
        Cull   Off

        Pass
        {
            Name "PixelArtEffect"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   3.0
            #pragma shader_feature_local _COLOR_QUANT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // ── 파라미터 ─────────────────────────────────────────────────────
            float _PixelSize;
            half4 _OutlineColor;
            float _OutlineThreshold;
            float _ColorEdgeWeight;
            float _ColorSteps;

            // ── 헬퍼 ─────────────────────────────────────────────────────────

            /// UV를 _PixelSize px 격자에 스냅합니다.
            float2 SnapUV(float2 uv, float2 screenSize)
            {
                return floor(uv * screenSize / _PixelSize) * _PixelSize / screenSize;
            }

            /// RGB → 상대 휘도 (ITU-R BT.601)
            half Lum(half3 c)
            {
                return dot(c, half3(0.299h, 0.587h, 0.114h));
            }

            // ※ sampler_PointClamp : URP Core가 선언하는 내장 포인트 샘플러.
            //    bilinear를 쓰면 스냅 후에도 픽셀이 번지므로 반드시 포인트를 사용합니다.
            half4 SamplePx(float2 uv, float2 ss)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, SnapUV(uv, ss));
            }

            // ── 프래그먼트 셰이더 ─────────────────────────────────────────────

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 ss = _ScreenParams.xy;

                // 픽셀 한 칸 (UV 공간)
                float2 st = _PixelSize / ss;

                // ── 1. 픽셀화 ─────────────────────────────────────────────────
                half4 col = SamplePx(uv, ss);

                // ── 2. 8방향 이웃 샘플링 ──────────────────────────────────────
                half4 pR  = SamplePx(uv + float2( st.x,  0    ), ss);
                half4 pL  = SamplePx(uv + float2(-st.x,  0    ), ss);
                half4 pU  = SamplePx(uv + float2( 0,     st.y ), ss);
                half4 pD  = SamplePx(uv + float2( 0,    -st.y ), ss);
                half4 pRU = SamplePx(uv + float2( st.x,  st.y ), ss);
                half4 pLU = SamplePx(uv + float2(-st.x,  st.y ), ss);
                half4 pRD = SamplePx(uv + float2( st.x, -st.y ), ss);
                half4 pLD = SamplePx(uv + float2(-st.x, -st.y ), ss);

                // ── 3. Sobel 필터로 엣지 강도 계산 (8방향 → 더 선명한 테두리) ─
                half lumR  = Lum(pR.rgb);  half lumL  = Lum(pL.rgb);
                half lumU  = Lum(pU.rgb);  half lumD  = Lum(pD.rgb);
                half lumRU = Lum(pRU.rgb); half lumLU = Lum(pLU.rgb);
                half lumRD = Lum(pRD.rgb); half lumLD = Lum(pLD.rgb);

                half gx = -lumLD - 2*lumL - lumLU + lumRD + 2*lumR + lumRU;
                half gy = -lumLD - 2*lumD - lumRD + lumLU + 2*lumU + lumRU;
                half edge = saturate(sqrt(gx*gx + gy*gy) * _ColorEdgeWeight);

                // 픽셀아트 특유의 딱 떨어지는 하드 엣지 (그라디언트 없음)
                edge = step(_OutlineThreshold, edge);

                // ── 4. 테두리 색 합성 ─────────────────────────────────────────
                half3 result = lerp(col.rgb, _OutlineColor.rgb, edge * saturate(col.a * 8));

                // ── 5. 색상 양자화 (선택) ──────────────────────────────────────
                #if _COLOR_QUANT
                    result = floor(result * _ColorSteps + 0.5h) / _ColorSteps;
                #endif

                return half4(result, col.a);
            }
            ENDHLSL
        }
    }
}
