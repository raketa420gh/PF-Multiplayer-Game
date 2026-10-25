// Planar mirror: shows the reflection camera's render at the screen position, flipped horizontally.
Shader "Game/Mirror"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.92, 0.94, 0.96, 1)
        [NoScaleOffset] _MirrorTex ("Mirror", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Mirror"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MirrorTex);
            SAMPLER(sampler_MirrorTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screen : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screen = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.screen.xy / input.screen.w;
                return SAMPLE_TEXTURE2D(_MirrorTex, sampler_MirrorTex, float2(1.0 - uv.x, uv.y)) * _Tint;
            }
            ENDHLSL
        }
    }
}
