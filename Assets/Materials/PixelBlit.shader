// PixelBlit.shader - Nearest-neighbor upscale for use with Blitter.BlitTexture().
// Samples _BlitTexture with sampler_point_clamp to guarantee no blur.
Shader "Custom/PixelBlit"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "PixelBlit"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            // Core.hlsl 은 GetFullScreenTriangleVertexPosition/TexCoord 를 포함합니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Blitter.BlitTexture() 가 자동으로 바인딩하는 소스 텍스처
            TEXTURE2D_X(_BlitTexture);

            // sampler_point_clamp : GPU 레벨에서 최근접 샘플링을 강제합니다.
            // bilinear 샘플러를 완전히 우회합니다.
            SAMPLER(sampler_point_clamp);

            // Blitter 가 설정하는 UV 스케일/오프셋 (일반적으로 (1,1,0,0))
            float4 _BlitScaleBias;

            struct Attributes { uint vertexID : SV_VertexID; };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                // URP 표준 전체화면 삼각형 — 플랫폼별 UV 플립을 자동 처리합니다.
                OUT.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
                OUT.texcoord   = GetFullScreenTriangleTexCoord(IN.vertexID)
                                 * _BlitScaleBias.xy + _BlitScaleBias.zw;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                // 최근접 샘플링 — 픽셀 경계가 뚜렷하게 유지됩니다.
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_point_clamp, IN.texcoord);
            }
            ENDHLSL
        }
    }
}
