// Passes 0 and 1 draw the outlined renderers and the bodies that may stand in front of them into the outline mask (R = outlined, G = eye depth),
// depth-tested against each other; pass 2 lays the mask's outer rim over the frame wherever no nearer body covers it (InteractOutlineFeature).
Shader "Hidden/Game/InteractOutline"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    struct MaskVaryings
    {
        float4 positionCS : SV_POSITION;
        float depth : TEXCOORD0;
    };

    MaskVaryings MaskVert(float4 positionOS : POSITION)
    {
        MaskVaryings output;
        float3 positionWS = TransformObjectToWorld(positionOS.xyz);
        output.positionCS = TransformWorldToHClip(positionWS);
        output.depth = -TransformWorldToView(positionWS).z;

        return output;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off

        Pass
        {
            Name "Mask"
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex MaskVert
            #pragma fragment Frag

            half4 Frag(MaskVaryings input) : SV_Target
            {
                return half4(1, input.depth, 0, 0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Occluder"
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex MaskVert
            #pragma fragment Frag

            half4 Frag(MaskVaryings input) : SV_Target
            {
                return half4(0, input.depth, 0, 0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Rim"
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // A body this much nearer than the outlined thing hides its rim; one that touches it or stands behind does not.
            #define OCCLUDER_BIAS 0.02

            TEXTURE2D(_InteractOutlineMask);
            half4 _OutlineColor;
            float _OutlineWidth;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 here = SAMPLE_TEXTURE2D_LOD(_InteractOutlineMask, sampler_PointClamp, uv, 0).rg;

                if (here.r > 0.99)
                    discard;

                float width, height;
                _InteractOutlineMask.GetDimensions(width, height);
                float2 step = _OutlineWidth / float2(width, height);
                half rim = 0;
                float depth = 0;

                [unroll]
                for (int i = 0; i < 12; i++)
                {
                    float angle = i * (TWO_PI / 12.0);
                    float2 at = uv + float2(cos(angle), sin(angle)) * step;
                    rim = max(rim, SAMPLE_TEXTURE2D_LOD(_InteractOutlineMask, sampler_LinearClamp, at, 0).r);
                    float2 tap = SAMPLE_TEXTURE2D_LOD(_InteractOutlineMask, sampler_PointClamp, at, 0).rg;
                    depth = max(depth, tap.r * tap.g);
                }

                // G without R is a body: it covers the rim of whatever lies behind it. Where only the soft edge of the rim reaches, the depth
                // of the outlined thing is unknown and the body wins.
                if (here.g > 0 && (depth == 0 || here.g < depth - OCCLUDER_BIAS))
                    discard;

                return half4(_OutlineColor.rgb, _OutlineColor.a * rim);
            }
            ENDHLSL
        }
    }
}
