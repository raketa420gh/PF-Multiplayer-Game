// Pass 0 draws a renderer into the outline mask; pass 1 lays the mask's outer rim over the frame (InteractOutlineFeature).
Shader "Hidden/Game/InteractOutline"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Mask"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 Vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half4 Frag() : SV_Target
            {
                return 1;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Rim"
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_InteractOutlineMask);
            half4 _OutlineColor;
            float _OutlineWidth;

            half Mask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_LOD(_InteractOutlineMask, sampler_LinearClamp, uv, 0).r;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half inside = Mask(uv);

                if (inside > 0.99)
                    discard;

                float width, height;
                _InteractOutlineMask.GetDimensions(width, height);
                float2 step = _OutlineWidth / float2(width, height);
                half rim = 0;

                [unroll]
                for (int i = 0; i < 12; i++)
                {
                    float angle = i * (TWO_PI / 12.0);
                    rim = max(rim, Mask(uv + float2(cos(angle), sin(angle)) * step));
                }

                return half4(_OutlineColor.rgb, _OutlineColor.a * saturate(rim - inside));
            }
            ENDHLSL
        }
    }
}
