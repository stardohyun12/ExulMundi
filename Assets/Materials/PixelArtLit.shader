Shader "Custom/PixelArtLit"
{
    // ─────────────────────────────────────────────────────────────────────────
    // PIXEL-RECONSTRUCTION OBJECT SHADER
    //
    // Philosophy: each output pixel must behave like a discrete sample.
    // This shader enforces that by:
    //   1. Quantizing normals to 4 directions → kills sub-pixel normal variation
    //   2. Strict 2-tone output: exactly SHADOW_COLOR or LIGHT_COLOR, nothing else
    //   3. No smooth interpolation at any stage
    //
    // PixelArtEffects post-pass then applies palette mapping and dithering on the
    // already-discrete 2-tone image rendered into the low-res RT.
    // ─────────────────────────────────────────────────────────────────────────

    Properties
    {
        [Header(Surface Colors)]
        // Only two colors exit this shader. Palette work is done in post-pass.
        _LightColor   ("Light Color",  Color) = (0.85, 0.68, 0.45, 1)
        _ShadowColor  ("Shadow Color", Color) = (0.14, 0.10, 0.22, 1)
        _AmbientColor ("Ambient",      Color) = (0.05, 0.04, 0.08, 1)

        [Space(8)]
        [Header(Light Split)]
        // step() threshold. NdotL >= this → LIGHT, else → SHADOW.
        _ShadowThresh ("Shadow Threshold", Range(0, 1)) = 0.4

        [Space(8)]
        [Header(Normal Quantization)]
        // How many directions to quantize normals to. 4 = blocky pixel art.
        // Higher values = smoother normals. Keep at 2~4 for pixel art.
        // Formula: normalize(round(normal * _NormalSteps) / _NormalSteps)
        _NormalSteps  ("Normal Quantize Steps", Range(1, 8)) = 2

        [Space(8)]
        [Header(Specular)]
        _SpecularStr   ("Specular Strength", Range(0, 1))  = 0.8
        _Glossiness    ("Glossiness",        Range(4, 512)) = 128
        _SpecularThresh("Specular Threshold",Range(0, 1))  = 0.92

        [Space(8)]
        [Header(Rim)]
        _RimStrength  ("Rim Strength",  Range(0, 1)) = 0.5
        _RimColor     ("Rim Color",     Color)       = (1.0, 0.90, 0.65, 1)
        _RimThreshold ("Rim Threshold", Range(0, 1)) = 0.55

        [Space(8)]
        [Header(Outline)]
        _OutlineColor ("Outline Color", Color)          = (0.02, 0.01, 0.05, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.02)) = 0.004
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
        }

        // ── Pass 1: ForwardLit ────────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LightColor;
                half4 _ShadowColor;
                half4 _AmbientColor;
                float _ShadowThresh;
                float _NormalSteps;
                float _SpecularStr;
                float _Glossiness;
                float _SpecularThresh;
                float _RimStrength;
                half4 _RimColor;
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
                float4 positionCS  : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── Normal Quantization ───────────────────────────────────────────
            // round(n * steps) / steps collapses the continuous normal sphere
            // into a small set of discrete directions.
            //
            // This is the core of "per-pixel lighting" behavior:
            // all fragments in the same low-res pixel that face roughly the same
            // direction will be snapped to the SAME quantized normal,
            // producing the same NdotL → same light/shadow output.
            //
            // Example with steps=2:
            //   normal (0.6, 0.7, 0.4) → round(0.6*2,0.7*2,0.4*2)/2
            //                          = round(1.2, 1.4, 0.8)/2
            //                          = (1, 1, 1)/2 = (0.5, 0.5, 0.5)
            //                          → normalize → (0.577, 0.577, 0.577)
            // All normals in that hemisphere snap to the same vector.
            float3 QuantizeNormal(float3 n, float steps)
            {
                float3 quantized = round(n * steps) / steps;
                float  len       = length(quantized);
                // Fallback to original normal if quantization collapses to zero.
                return len > 0.001 ? quantized / len : n;
            }

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs  = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS  = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = normInputs.normalWS;
                OUT.viewDirWS   = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.shadowCoord = GetShadowCoord(posInputs);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // Quantize normal: collapse continuous sphere into discrete face normals.
                // This kills sub-pixel normal variation — the biggest source of
                // "smooth 3D" appearance at low resolution.
                float3 normalWS  = QuantizeNormal(normalize(IN.normalWS), _NormalSteps);
                float3 viewDirWS = normalize(IN.viewDirWS);

                Light  mainLight = GetMainLight(IN.shadowCoord);
                float3 lightDir  = normalize(mainLight.direction);

                // Binary shadow attenuation: 0 or 1 only.
                float shadowAtten = step(0.5, mainLight.shadowAttenuation);
                float NdotL       = saturate(dot(normalWS, lightDir)) * shadowAtten;

                // STRICT 2-TONE: one step() call, output is 0 or 1, never 0.5.
                // Combined with quantized normals, this ensures large areas of
                // geometry produce identical flat color blocks — true pixel art form.
                float lit = step(_ShadowThresh, NdotL);

                // Two-tone diffuse. LIGHT_COLOR or SHADOW_COLOR only.
                half3 color = lerp(_ShadowColor.rgb, _LightColor.rgb, lit)
                            * half3(mainLight.color);

                // Specular: binary highlight dot. On or off, never partial.
                if (_SpecularStr > 0.001)
                {
                    float3 halfVec  = normalize(lightDir + viewDirWS);
                    float  NdotH    = saturate(dot(normalWS, halfVec));
                    float  specMask = step(_SpecularThresh, pow(NdotH, _Glossiness) * lit);
                    color += specMask * _SpecularStr * half3(mainLight.color);
                }

                // Additional lights: binary only.
                #ifdef _ADDITIONAL_LIGHTS
                {
                    uint count = GetAdditionalLightsCount();
                    for (uint i = 0u; i < count; ++i)
                    {
                        Light  addL      = GetAdditionalLight(i, IN.positionWS, half4(1,1,1,1));
                        float  addShadow = step(0.5, addL.shadowAttenuation);
                        float  addNdotL  = saturate(dot(normalWS, normalize(addL.direction)))
                                         * addShadow * addL.distanceAttenuation;
                        float  addLit    = step(_ShadowThresh, addNdotL);
                        color           += _LightColor.rgb * half3(addL.color) * addLit;
                    }
                }
                #endif

                // Rim: binary silhouette highlight.
                if (_RimStrength > 0.001)
                {
                    float rimVal  = 1.0 - saturate(dot(normalWS, viewDirWS));
                    float rimMask = step(_RimThreshold, rimVal) * lit;
                    color += _RimColor.rgb * rimMask * _RimStrength;
                }

                // Flat ambient: one constant value, no SampleSH gradient.
                color += _AmbientColor.rgb;

                return half4(saturate(color), 1.0);
            }
            ENDHLSL
        }

        // ── Pass 2: Outline ───────────────────────────────────────────────────
        Pass
        {
            Name "Outline"
            Cull Front

            HLSLPROGRAM
            #pragma vertex   VertOutline
            #pragma fragment FragOutline
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LightColor;
                half4 _ShadowColor;
                half4 _AmbientColor;
                float _ShadowThresh;
                float _NormalSteps;
                float _SpecularStr;
                float _Glossiness;
                float _SpecularThresh;
                float _RimStrength;
                half4 _RimColor;
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

                float4 posCS    = TransformObjectToHClip(IN.positionOS.xyz);
                float3 normalCS = mul((float3x3)UNITY_MATRIX_VP,
                                      TransformObjectToWorldNormal(IN.normalOS));
                float2 dir = length(normalCS.xy) > 0.0001
                             ? normalize(normalCS.xy)
                             : float2(1, 0);
                posCS.xy += dir * _OutlineWidth * posCS.w;

                OUT.positionCS = posCS;
                return OUT;
            }

            half4 FragOutline(Varyings IN) : SV_Target { return _OutlineColor; }
            ENDHLSL
        }

        // ── Pass 3: ShadowCaster ──────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LightColor;
                half4 _ShadowColor;
                half4 _AmbientColor;
                float _ShadowThresh;
                float _NormalSteps;
                float _SpecularStr;
                float _Glossiness;
                float _SpecularThresh;
                float _RimStrength;
                half4 _RimColor;
                float _RimThreshold;
                half4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings ShadowVert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 posWS    = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normWS   = TransformObjectToWorldNormal(IN.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDir = normalize(_LightPosition - posWS);
                #else
                float3 lightDir = _LightDirection;
                #endif
                float4 posCS = TransformWorldToHClip(ApplyShadowBias(posWS, normWS, lightDir));
                #if UNITY_REVERSED_Z
                posCS.z = min(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                posCS.z = max(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                Varyings OUT;
                OUT.positionCS = posCS;
                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // ── Pass 4: DepthNormals ──────────────────────────────────────────────
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex   DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LightColor;
                half4 _ShadowColor;
                half4 _AmbientColor;
                float _ShadowThresh;
                float _NormalSteps;
                float _SpecularStr;
                float _Glossiness;
                float _SpecularThresh;
                float _RimStrength;
                half4 _RimColor;
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
            };

            Varyings DepthNormalsVert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 DepthNormalsFrag(Varyings IN) : SV_Target
            {
                return half4(normalize(IN.normalWS) * 0.5 + 0.5, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
