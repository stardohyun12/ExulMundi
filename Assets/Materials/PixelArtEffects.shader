// PixelArtEffects.shader
// ─────────────────────────────────────────────────────────────────────────────
// 역할: 저해상도 렌더 결과를 픽셀 아트로 변환하는 후처리.
//
// Pipeline (PixelRenderFeature가 전달):
//   _BlitTexture    = 저해상도 다운스케일된 씬 렌더 결과
//   _PixelTexelSize = (1/w, 1/h, w, h)
//
// Pass 0 — Pixel : UV 스냅 + 색 양자화 + Depth/Normal 기반 1px 아웃라인
// Pass 1 — Raw   : UV 스냅만 (디버그)
// Pass 2 — Grid  : 픽셀 경계 빨간 격자 (디버그)
//
// ── 아웃라인 구조 (최종) ─────────────────────────────────────────────────────
//
// [Depth — 8방향 Linear]
//   LinearEyeDepth(rawDepth) → 월드 단위 균일 depth (비선형 NDC 제거)
//   Cross(4) + Diagonal(4) 방향 샘플링
//   대각선은 sqrt(2) 거리이므로 1/sqrt(2) ≈ 0.707 가중치 적용
//   depthDiff = max(crossMax, diagMax * 0.707)
//   edgeDepth = saturate(depthDiff / threshold)
//
// [Normal — 8방향 + normalize]
//   SampleSceneNormals() 후 normalize() 재적용 (보간 오차 제거)
//   Cross(4) + Diagonal(4) 1-dot 최대값
//   smoothstep 노이즈 억제
//
// [합산]
//   edge = max(edgeDepth, normalDiff * weight) * edgeDepth
//   edgeDepth 게이트: depth 엣지 없는 픽셀은 Normal도 차단
//
// [이진화]
//   lerp(edge, floor(edge), binarize)
// ─────────────────────────────────────────────────────────────────────────────
Shader "Custom/PixelArtEffects"
{
    Properties
    {
        [HideInInspector] _PixelTexelSize          ("Pixel Texel Size",          Vector)     = (0.003125, 0.005556, 320, 180)
        [HideInInspector] _QuantizeSteps           ("Quantize Steps",            Float)      = 8

        // ── Depth 아웃라인 ──────────────────────────────────────────────────────
        [HideInInspector] _OutlineDepthThreshold   ("Depth Threshold (m)",       Float)      = 0.1
        [HideInInspector] _OutlineDepthMultiplier  ("Depth Multiplier",          Float)      = 1.0
        [HideInInspector] _OutlineDepthClamp       ("Depth Clamp (m)",           Float)      = 2.0
        [HideInInspector] _OutlineDiagWeight       ("Diagonal Weight",           Float)      = 0.707

        // ── Normal 아웃라인 ─────────────────────────────────────────────────────
        [HideInInspector] _OutlineNormalThreshold  ("Normal Threshold",          Float)      = 0.1
        [HideInInspector] _OutlineNormalMultiplier ("Normal Multiplier",         Float)      = 1.0
        [HideInInspector] _OutlineNormalEpsilon    ("Normal Smoothstep Epsilon", Float)      = 0.02
        [HideInInspector] _OutlineNormalWeight     ("Normal Weight",             Float)      = 0.5

        // ── 공통 ────────────────────────────────────────────────────────────────
        [HideInInspector] _OutlineColor            ("Outline Color",             Color)      = (0, 0, 0, 1)
        [HideInInspector] _OutlineBinarize         ("Binarize Edge",             Float)      = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

        TEXTURE2D_X(_BlitTexture);
        float4 _BlitScaleBias;

        float4 _PixelTexelSize;

        float  _QuantizeSteps;

        float  _OutlineDepthThreshold;
        float  _OutlineDepthMultiplier;
        float  _OutlineDepthClamp;
        float  _OutlineDiagWeight;

        float  _OutlineNormalThreshold;
        float  _OutlineNormalMultiplier;
        float  _OutlineNormalEpsilon;
        float  _OutlineNormalWeight;

        half4  _OutlineColor;
        float  _OutlineBinarize;

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
        float2 SnapUV(float2 uv)
        {
            float2 pixelPos = floor(uv * _PixelTexelSize.zw);
            return (pixelPos + 0.5) / _PixelTexelSize.zw;
        }

        // 색 양자화.
        half3 Quantize(half3 c, float steps)
        {
            return round(c * steps) / steps;
        }

        // Raw NDC depth 샘플. 이후 sky 마스킹과 LinearEyeDepth 변환에 함께 사용.
        float SampleRawDepth(float2 uv)
        {
            return SampleSceneDepth(uv);
        }

        // Raw NDC → 선형 Eye depth (월드 단위 미터).
        float ToLinearDepth(float rawDepth)
        {
            return LinearEyeDepth(rawDepth, _ZBufferParams);
        }

        // Far plane(스카이박스) 판정.
        // UNITY_REVERSED_Z(DX): far = 0에 수렴. OpenGL: far = 1에 수렴.
        // 스카이박스 픽셀은 LinearEyeDepth 시 수천 단위 → diff 폭발 원인.
        bool IsSky(float rawDepth)
        {
            #if defined(UNITY_REVERSED_Z)
                return rawDepth < 0.0001;
            #else
                return rawDepth > 0.9999;
            #endif
        }

        // Depth diff 헬퍼: 두 샘플 중 하나라도 sky면 0 반환 (마스킹).
        // LinearEyeDepth는 sky에서 수천 단위가 되어 edgeDepth=1을 유발하므로
        // sky 경계에서는 아웃라인을 내지 않는 게 올바른 동작.
        float SafeDepthDiff(float linA, float rawA, float linB, float rawB)
        {
            float mask = (IsSky(rawA) || IsSky(rawB)) ? 0.0 : 1.0;
            return abs(linA - linB) * mask;
        }

        // 노멀 샘플링: 길이가 0인 벡터(배경/sky 픽셀)에서 normalize()가 NaN을 반환하므로
        // length > 0.001인 경우에만 normalize, 아니면 (0,0,0) 반환.
        // NaN이 dot()로 전파되면 edge 전체가 NaN → 검정화면의 원인.
        float3 SampleNormal(float2 uv)
        {
            float3 n = SampleSceneNormals(uv);
            float  l = dot(n, n);          // length²
            return l > 0.001 ? n / sqrt(l) : float3(0, 0, 0);
        }

        ENDHLSL

        // ── Pass 0: 픽셀화 + 양자화 + 최종 아웃라인 ─────────────────────────
        Pass
        {
            Name "PixelArtUpscale"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment FragPixel

            half4 FragPixel(Varyings IN) : SV_Target
            {
                // ── 진단용 early-out ─────────────────────────────────────────
                // _BlitTexture 샘플 결과를 그대로 반환해 파이프라인이 살아있는지 확인.
                // 화면에 씬이 보이면 파이프라인 OK → 셰이더 로직 문제.
                // 여전히 검정이면 Downscale/Upscale 패스 자체가 작동 안 하는 것.
                return half4(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, IN.texcoord).rgb, 1.0);

                float2 uv    = SnapUV(IN.texcoord);
                float2 t     = _PixelTexelSize.xy;   // 저해상도 1픽셀 크기 (UV)
                float  steps = max(1.0, _QuantizeSteps);

                // ── 1. 색 샘플 + 양자화 ──────────────────────────────────────
                half3 c = Quantize(
                    SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rgb,
                    steps);

                // ── 2. Linear Depth 8방향 샘플링 (sky 마스킹 포함) ───────────
                // raw NDC depth를 먼저 샘플해 IsSky() 판정에 사용.
                // SafeDepthDiff(): 양쪽 중 하나라도 sky면 diff=0 → edgeDepth=0 보장.
                float rawC  = SampleRawDepth(uv);
                float rawN  = SampleRawDepth(uv + float2( 0,    t.y));
                float rawS  = SampleRawDepth(uv + float2( 0,   -t.y));
                float rawE  = SampleRawDepth(uv + float2( t.x,  0  ));
                float rawW  = SampleRawDepth(uv + float2(-t.x,  0  ));
                float rawNE = SampleRawDepth(uv + float2( t.x,  t.y));
                float rawNW = SampleRawDepth(uv + float2(-t.x,  t.y));
                float rawSE = SampleRawDepth(uv + float2( t.x, -t.y));
                float rawSW = SampleRawDepth(uv + float2(-t.x, -t.y));

                float dC  = ToLinearDepth(rawC);
                float dN  = ToLinearDepth(rawN);
                float dS  = ToLinearDepth(rawS);
                float dE  = ToLinearDepth(rawE);
                float dW  = ToLinearDepth(rawW);
                float dNE = ToLinearDepth(rawNE);
                float dNW = ToLinearDepth(rawNW);
                float dSE = ToLinearDepth(rawSE);
                float dSW = ToLinearDepth(rawSW);

                float crossMax = max(max(SafeDepthDiff(dC, rawC, dN,  rawN),
                                         SafeDepthDiff(dC, rawC, dS,  rawS)),
                                     max(SafeDepthDiff(dC, rawC, dE,  rawE),
                                         SafeDepthDiff(dC, rawC, dW,  rawW)));

                float diagMax  = max(max(SafeDepthDiff(dC, rawC, dNE, rawNE),
                                         SafeDepthDiff(dC, rawC, dNW, rawNW)),
                                     max(SafeDepthDiff(dC, rawC, dSE, rawSE),
                                         SafeDepthDiff(dC, rawC, dSW, rawSW)));

                float depthDiff = max(crossMax, diagMax * _OutlineDiagWeight)
                                  * _OutlineDepthMultiplier;

                depthDiff = min(depthDiff, _OutlineDepthClamp);

                float edgeDepth = saturate(depthDiff / max(_OutlineDepthThreshold, 0.0001));

                // ── 3. Normal 8방향 샘플링 ────────────────────────────────────
                float3 nC  = SampleNormal(uv);

                float3 nN  = SampleNormal(uv + float2( 0,    t.y));
                float3 nS  = SampleNormal(uv + float2( 0,   -t.y));
                float3 nE  = SampleNormal(uv + float2( t.x,  0  ));
                float3 nW  = SampleNormal(uv + float2(-t.x,  0  ));

                float3 nNE = SampleNormal(uv + float2( t.x,  t.y));
                float3 nNW = SampleNormal(uv + float2(-t.x,  t.y));
                float3 nSE = SampleNormal(uv + float2( t.x, -t.y));
                float3 nSW = SampleNormal(uv + float2(-t.x, -t.y));

                // 1 - dot: 두 노멀이 동일하면 0, 수직이면 1.
                // max(0, ...): 한쪽이 zero normal일 때 음수 방지.
                float crossNorm = max(max(max(0.0, 1.0 - dot(nC, nN)),  max(0.0, 1.0 - dot(nC, nS))),
                                      max(max(0.0, 1.0 - dot(nC, nE)),  max(0.0, 1.0 - dot(nC, nW))));

                float diagNorm  = max(max(max(0.0, 1.0 - dot(nC, nNE)), max(0.0, 1.0 - dot(nC, nNW))),
                                      max(max(0.0, 1.0 - dot(nC, nSE)), max(0.0, 1.0 - dot(nC, nSW))));

                // 대각선은 동일한 _OutlineDiagWeight로 약화 → 대각 노이즈 억제.
                float normalDiff = max(crossNorm, diagNorm * _OutlineDiagWeight)
                                   * _OutlineNormalMultiplier;

                // smoothstep: threshold 미만의 완만한 굴곡 노이즈 제거.
                normalDiff = smoothstep(
                    _OutlineNormalThreshold,
                    _OutlineNormalThreshold + max(_OutlineNormalEpsilon, 0.001),
                    normalDiff);

                // ── 4. 합산 + edgeDepth 게이트 ───────────────────────────────
                float edge = saturate(max(edgeDepth, normalDiff * _OutlineNormalWeight) * edgeDepth);

                // ── 5. 이진화 (선택) ──────────────────────────────────────────
                edge = lerp(edge, floor(edge), _OutlineBinarize);

                half3 color = lerp(c, _OutlineColor.rgb, edge);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // ── Pass 1: Debug Raw ─────────────────────────────────────────────────
        Pass
        {
            Name "DebugRawLowRes"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment FragRaw

            half4 FragRaw(Varyings IN) : SV_Target
            {
                float2 uv = SnapUV(IN.texcoord);
                return half4(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rgb, 1.0);
            }
            ENDHLSL
        }

        // ── Pass 2: Debug Grid ────────────────────────────────────────────────
        Pass
        {
            Name "DebugPixelGrid"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment FragGrid

            half4 FragGrid(Varyings IN) : SV_Target
            {
                float2 uv    = SnapUV(IN.texcoord);
                half3  color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rgb;
                float2 sub   = frac(IN.texcoord * _PixelTexelSize.zw);
                float2 fw    = fwidth(IN.texcoord * _PixelTexelSize.zw);
                float  grid  = saturate(step(sub.x, fw.x) + step(sub.y, fw.y));
                return half4(lerp(color, half3(1, 0, 0), grid * 0.7), 1.0);
            }
            ENDHLSL
        }
    }
}
