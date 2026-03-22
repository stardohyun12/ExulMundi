Shader "Custom/CelShading3D"
{
    Properties
    {
        [Header(Base)]
        _BaseColor      ("Base Color",       Color)         = (0.8, 0.6, 0.4, 1)
        _ShadowColor    ("Shadow Color",     Color)         = (0.25, 0.22, 0.32, 1)
        _AmbientColor   ("Ambient Color",    Color)         = (0.1, 0.1, 0.15, 1)

        [Header(Cel Shading)]
        [Space(4)]
        _ShadowStep     ("Shadow Step",      Range(0, 1))   = 0.5
        _ShadowSmooth   ("Shadow Smooth",    Range(0, 0.2)) = 0.05

        [Header(Specular)]
        [Space(4)]
        _SpecularColor  ("Specular Color",   Color)         = (0.9, 0.9, 0.9, 1)
        _Glossiness     ("Glossiness",       Range(1, 512)) = 32
        _SpecularStep   ("Specular Step",    Range(0, 1))   = 0.7
        _SpecularSmooth ("Specular Smooth",  Range(0, 0.2)) = 0.05

        [Header(Rim)]
        [Space(4)]
        _RimColor       ("Rim Color",        Color)         = (0.7, 0.7, 1.0, 1)
        _RimAmount      ("Rim Amount",       Range(0, 1))   = 0.6
        _RimThreshold   ("Rim Threshold",    Range(0, 1))   = 0.1

        [Header(Outline)]
        [Space(4)]
        _OutlineColor   ("Outline Color",    Color)         = (0.05, 0.05, 0.1, 1)
        _OutlineWidth   ("Outline Width",    Range(0, 0.1)) = 0.04
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // ── Pass 1: Inverted Hull Outline ─────────────────────────────────────
        // Must be FIRST so it renders behind the lit surface.
        // "SRPDefaultUnlit" is the correct LightMode for URP to execute this pass.
        // Without a LightMode tag URP skips the pass entirely — this was the bug.
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   VertOutline
            #pragma fragment FragOutline
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;     half4  _ShadowColor;   half4  _AmbientColor;
                float  _ShadowStep;    float  _ShadowSmooth;
                half4  _SpecularColor; float  _Glossiness;
                float  _SpecularStep;  float  _SpecularSmooth;
                half4  _RimColor;      float  _RimAmount;      float  _RimThreshold;
                half4  _OutlineColor;  float  _OutlineWidth;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings   { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings VertOutline(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 posCS    = TransformObjectToHClip(IN.positionOS.xyz);
                float3 normalCS = mul((float3x3)UNITY_MATRIX_VP, TransformObjectToWorldNormal(IN.normalOS));
                posCS.xy       += normalize(normalCS.xy) * _OutlineWidth * posCS.w * 0.1;
                OUT.positionCS  = posCS;
                return OUT;
            }

            half4 FragOutline(Varyings IN) : SV_Target { return _OutlineColor; }
            ENDHLSL
        }

        // ── Pass 2: Cel Shading ───────────────────────────────────────────────
        Pass
        {
            Name "CelShading"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            // All shadow cascade variants — avoids atten=0 when keywords mismatch.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;     half4  _ShadowColor;   half4  _AmbientColor;
                float  _ShadowStep;    float  _ShadowSmooth;
                half4  _SpecularColor; float  _Glossiness;
                float  _SpecularStep;  float  _SpecularSmooth;
                half4  _RimColor;      float  _RimAmount;      float  _RimThreshold;
                half4  _OutlineColor;  float  _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                VertexPositionInputs pos    = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normal = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = normal.normalWS;
                OUT.viewDirWS  = GetWorldSpaceViewDir(pos.positionWS);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);

                // ── Main light ────────────────────────────────────────────────
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                    Light  mainLight   = GetMainLight(shadowCoord);
                #else
                    Light  mainLight   = GetMainLight();
                #endif

                float3 lightDir = normalize(mainLight.direction);

                // ── KEY FIX: NdotL drives the two-tone band, independent of atten ──
                // Bug: smoothstep(NdotL * atten) collapses to 0 when atten=0 (shadow
                // system not configured) → entire mesh becomes flat _ShadowColor.
                // Fix: NdotL purely controls which tone band. Shadow attenuation only
                // dims the lit tone, never forces pixels into the shadow color.
                float NdotL = dot(normalWS, lightDir);  // [-1, +1]

                // lightBand: 순수하게 NdotL 기반 2-tone 구분.
                // _ShadowStep=0 → 수직면이 정확히 경계. 빛 회전 시 경계선이 이동.
                float lightBand = smoothstep(
                    _ShadowStep - _ShadowSmooth,
                    _ShadowStep + _ShadowSmooth,
                    NdotL);

                // 실제 그림자(다른 오브젝트가 드리우는 그림자)는 별도로 처리.
                // smoothstep으로 경계를 sharp하게 snap → 셀 셰이딩 특유의 하드 그림자.
                float hardShadow   = smoothstep(0.0, 0.1, mainLight.shadowAttenuation);
                half3 litColor     = _BaseColor.rgb;
                half3 shadowedLit  = lerp(_ShadowColor.rgb, litColor, hardShadow);
                half3 diffuseColor = lerp(_ShadowColor.rgb, shadowedLit, lightBand);

                // ── Specular ──────────────────────────────────────────────────
                float3 halfVec      = normalize(lightDir + viewDirWS);
                float  specularRaw  = pow(max(0.0, dot(normalWS, halfVec)), _Glossiness);
                float  specularMask = smoothstep(
                    _SpecularStep - _SpecularSmooth,
                    _SpecularStep + _SpecularSmooth,
                    specularRaw * lightBand);
                half3 specularColor = specularMask * _SpecularColor.rgb * hardShadow;

                // ── Rim light ─────────────────────────────────────────────────
                float rimDot       = 1.0 - saturate(dot(viewDirWS, normalWS));
                float rimIntensity = smoothstep(
                    _RimAmount - 0.02,
                    _RimAmount + 0.02,
                    rimDot * saturate(NdotL));
                half3 rimColor = rimIntensity * _RimColor.rgb;

                // ── Ambient (SH) ──────────────────────────────────────────────
                half3 ambient = SampleSH(normalWS) * _AmbientColor.rgb;

                // ── Composite ─────────────────────────────────────────────────
                half3 color = (diffuseColor + specularColor) * mainLight.color
                            + rimColor
                            + ambient;
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // ── Pass 3: Shadow Caster ─────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;     half4  _ShadowColor;   half4  _AmbientColor;
                float  _ShadowStep;    float  _ShadowSmooth;
                half4  _SpecularColor; float  _Glossiness;
                float  _SpecularStep;  float  _SpecularSmooth;
                half4  _RimColor;      float  _RimAmount;      float  _RimThreshold;
                half4  _OutlineColor;  float  _OutlineWidth;
            CBUFFER_END

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // ── Pass 4: Depth Only ────────────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex   DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;     half4  _ShadowColor;   half4  _AmbientColor;
                float  _ShadowStep;    float  _ShadowSmooth;
                half4  _SpecularColor; float  _Glossiness;
                float  _SpecularStep;  float  _SpecularSmooth;
                half4  _RimColor;      float  _RimAmount;      float  _RimThreshold;
                half4  _OutlineColor;  float  _OutlineWidth;
            CBUFFER_END

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
}
