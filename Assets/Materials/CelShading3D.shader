Shader "Custom/CelShading3D"
{
    Properties
    {
        [Header(Base)]
        _BaseColor     ("Base Color",      Color)          = (0.8, 0.6, 0.4, 1)
        _ShadowColor   ("Shadow Color",    Color)          = (0.25, 0.22, 0.32, 1)
        _AmbientColor  ("Ambient Color",   Color)          = (0.1, 0.1, 0.15, 1)

        [Header(Cel Shading)]
        [Space(4)]
        _ShadowStep    ("Shadow Step",     Range(0, 1))    = 0.5
        _ShadowSmooth  ("Shadow Smooth",   Range(0, 0.1))  = 0.01

        [Header(Specular)]
        [Space(4)]
        _SpecularColor ("Specular Color",  Color)          = (0.9, 0.9, 0.9, 1)
        _Glossiness    ("Glossiness",      Range(1, 512))  = 32
        _SpecularStep  ("Specular Step",   Range(0, 1))    = 0.7
        _SpecularSmooth("Specular Smooth", Range(0, 0.1))  = 0.01

        [Header(Rim)]
        [Space(4)]
        _RimColor      ("Rim Color",       Color)          = (0.7, 0.7, 1.0, 1)
        _RimAmount     ("Rim Amount",      Range(0, 1))    = 0.6
        _RimThreshold  ("Rim Threshold",   Range(0, 1))    = 0.1

        [Header(Outline)]
        [Space(4)]
        _OutlineColor  ("Outline Color",   Color)          = (0.05, 0.05, 0.1, 1)
        _OutlineWidth  ("Outline Width",   Range(0, 0.1))  = 0.04
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // Pass 1: Cel Shading
        Pass
        {
            Name "CelShading"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _AmbientColor;
                float _ShadowStep;
                float _ShadowSmooth;
                half4 _SpecularColor;
                float _Glossiness;
                float _SpecularStep;
                float _SpecularSmooth;
                half4 _RimColor;
                float _RimAmount;
                float _RimThreshold;
                half4 _OutlineColor;
                float _OutlineWidth;
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

                VertexPositionInputs posInputs    = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS   = normalInputs.normalWS;
                OUT.viewDirWS  = GetWorldSpaceViewDir(posInputs.positionWS);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);

                // ── 메인 라이트 + 그림자 감쇠 (ref: roystan.net) ──────────
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                    Light  mainLight   = GetMainLight(shadowCoord);
                #else
                    Light mainLight = GetMainLight();
                #endif

                float3 lightDir = normalize(mainLight.direction);
                float  atten    = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                // ── Diffuse : NdotL × 그림자 감쇠 → smoothstep 양자화 ───
                float NdotL   = dot(normalWS, lightDir);
                float diffuse = smoothstep(
                    _ShadowStep - _ShadowSmooth,
                    _ShadowStep + _ShadowSmooth,
                    NdotL * atten
                );
                half3 diffuseColor = lerp(_ShadowColor.rgb, _BaseColor.rgb, diffuse);

                // ── Specular : Blinn-Phong → smoothstep 양자화 ────────────
                float3 halfVec           = normalize(lightDir + viewDirWS);
                float  NdotH             = dot(normalWS, halfVec);
                float  specularRaw       = pow(max(0.0, NdotH), _Glossiness);
                float  specularIntensity = smoothstep(
                    _SpecularStep - _SpecularSmooth,
                    _SpecularStep + _SpecularSmooth,
                    specularRaw * diffuse
                );
                half3 specularColor = specularIntensity * _SpecularColor.rgb;

                // ── Rim Light (ref: roystan.net) ───────────────────────────
                float rimDot       = 1.0 - dot(viewDirWS, normalWS);
                float rimIntensity = rimDot * pow(max(0.0, NdotL), _RimThreshold);
                rimIntensity       = smoothstep(
                    _RimAmount - 0.01,
                    _RimAmount + 0.01,
                    rimIntensity
                );
                half3 rimColor = rimIntensity * _RimColor.rgb;

                // ── Ambient : SH 샘플링 (ref: ronja-tutorials.com) ────────
                half3 ambient = SampleSH(normalWS) * _AmbientColor.rgb;

                // ── 최종 합산 ─────────────────────────────────────────────
                half3 color = (diffuseColor + specularColor + rimColor) * mainLight.color + ambient;
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // Pass 2: Inverted Hull Outline
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   VertOutline
            #pragma fragment FragOutline
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _AmbientColor;
                float _ShadowStep;
                float _ShadowSmooth;
                half4 _SpecularColor;
                float _Glossiness;
                float _SpecularStep;
                float _SpecularSmooth;
                half4 _RimColor;
                float _RimAmount;
                float _RimThreshold;
                half4 _OutlineColor;
                float _OutlineWidth;
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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings VertOutline(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // 클립 공간 팽창 → 카메라 거리와 무관하게 일정한 픽셀 두께
                float4 posCS    = TransformObjectToHClip(IN.positionOS.xyz);
                float3 normalCS = mul((float3x3)UNITY_MATRIX_VP,
                                     TransformObjectToWorldNormal(IN.normalOS));
                float2 offset   = normalize(normalCS.xy) * _OutlineWidth * posCS.w * 0.1;
                posCS.xy       += offset;

                OUT.positionCS = posCS;
                return OUT;
            }

            half4 FragOutline(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // Pass 3: Shadow Caster
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
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _AmbientColor;
                float _ShadowStep;
                float _ShadowSmooth;
                half4 _SpecularColor;
                float _Glossiness;
                float _SpecularStep;
                float _SpecularSmooth;
                half4 _RimColor;
                float _RimAmount;
                float _RimThreshold;
                half4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            // URP 17 — Shaders/ShadowCasterPass.hlsl
            ENDHLSL
        }
    }
}
