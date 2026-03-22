Shader "Custom/PixelArtSprite"
{
    // Pixel-art sprite shader.
    // - Alpha-border outline (1px black edge)
    // - Color palette quantization (optional)
    // - Stepped cel-shading lighting (optional)

    Properties
    {
        _MainTex ("Sprite Texture", 2D)    = "white" {}
        _Color   ("Tint",           Color) = (1,1,1,1)

        [Header(Outline)]
        [Space(4)]
        _OutlineColor     ("Outline Color",    Color)          = (0.05, 0.05, 0.08, 1)
        _OutlineThickness ("Outline Thickness (px)", Float)    = 1.0
        _AlphaClip        ("Alpha Clip",       Range(0, 1))    = 0.1

        [Header(Color Quantization)]
        [Space(4)]
        [Toggle(_COLOR_QUANT)] _ColorQuantOn ("Enable", Float)   = 0
        _ColorSteps ("Color Steps (per channel)", Float)         = 8.0

        [Header(Cel Shading)]
        [Space(4)]
        [Toggle(_CEL_SHADE)] _CelShadeOn ("Enable Cel Shading", Float) = 0
        _CelSteps ("Shading Steps", Range(1, 8))                = 3.0
        _AmbientMin ("Ambient Min", Range(0, 1))                = 0.3

        // Internal URP sprite renderer properties
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip         ("Flip",           Vector) = (1,1,1,1)
        [HideInInspector] _AlphaSrcBlend("AlphaSrcBlend",  Float) = 1
        [HideInInspector] _AlphaDstBlend("AlphaDstBlend",  Float) = 10
        [HideInInspector][Toggle(PIXELSNAP_ON)] _PixelSnap ("Pixel Snap", Float) = 0
        [HideInInspector][Toggle(_ALPHAPREMULTIPLY_ON)] _AlphaPremultiply("Premultiply Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "PreviewType"     = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "PixelArtSprite"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   2.0

            #pragma shader_feature_local _COLOR_QUANT
            #pragma shader_feature_local _CEL_SHADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ── 텍스처 / 샘플러 ──────────────────────────────────────────────
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // ── 상수 버퍼 ────────────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4  _MainTex_ST;
                float4  _MainTex_TexelSize; // (1/w, 1/h, w, h)
                half4   _Color;
                half4   _OutlineColor;
                float   _OutlineThickness;
                float   _AlphaClip;
                float   _ColorSteps;
                float   _CelSteps;
                float   _AmbientMin;
                half4   _RendererColor;
            CBUFFER_END

            // ── 버텍스 입/출력 ────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── 헬퍼 ─────────────────────────────────────────────────────────

            /// 포인트 샘플링으로 스프라이트 텍스처를 샘플링합니다.
            half4 SampleSprite(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            }

            // ── 버텍스 ────────────────────────────────────────────────────────
            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color * _Color * _RendererColor;
                return OUT;
            }

            // ── 프래그먼트 ────────────────────────────────────────────────────
            half4 Frag(Varyings IN) : SV_Target
            {
                half4 texCol = SampleSprite(IN.uv);
                half4 col    = texCol * IN.color;

                // ── 1. 외곽 테두리 ────────────────────────────────────────────
                //    이웃 4방향을 샘플링해 현재 픽셀이 알파 경계에 있는지 확인합니다.
                //    내 alpha > clip 이고 이웃 중 하나라도 alpha < clip → 테두리 픽셀
                float2 texel = _MainTex_TexelSize.xy * _OutlineThickness;

                float aR = SampleSprite(IN.uv + float2( texel.x,  0      )).a;
                float aL = SampleSprite(IN.uv + float2(-texel.x,  0      )).a;
                float aU = SampleSprite(IN.uv + float2( 0,        texel.y)).a;
                float aD = SampleSprite(IN.uv + float2( 0,       -texel.y)).a;

                bool isBorder = col.a > _AlphaClip &&
                                (aR < _AlphaClip || aL < _AlphaClip ||
                                 aU < _AlphaClip || aD < _AlphaClip);

                // ── 2. 색상 양자화 ────────────────────────────────────────────
                half3 result = col.rgb;
                #if _COLOR_QUANT
                    result = floor(result * _ColorSteps + 0.5h) / _ColorSteps;
                #endif

                // ── 3. 셀 쉐이딩 (조명을 계단식으로 처리) ─────────────────────
                #if _CEL_SHADE
                    Light mainLight = GetMainLight();
                    // 스프라이트는 2D → 법선은 카메라를 향한 World Forward 사용
                    half NdotL = saturate(dot(half3(0,0,-1), mainLight.direction));
                    // 계단식 처리
                    half stepped = floor(NdotL * _CelSteps + 0.5h) / _CelSteps;
                    half shade    = max(stepped, _AmbientMin);
                    result *= shade * half3(mainLight.color);
                #endif

                // ── 4. 테두리 색 덮어쓰기 ─────────────────────────────────────
                result = isBorder ? _OutlineColor.rgb : result;

                return half4(result, col.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/2D/Sprite-Lit-Default"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.SpriteUnlitShaderGUI"
}
