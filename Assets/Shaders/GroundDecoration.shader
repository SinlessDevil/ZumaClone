Shader "Zuma/Ground Decoration"
{
    Properties
    {
        _Mask("Engraving Mask", 2D) = "black" {}
        _Depth("Engraving Strength", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-50" "RenderType" = "Transparent" }

        Pass
        {
            Blend DstColor Zero, Zero One
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Mask);
            SAMPLER(sampler_Mask);

            CBUFFER_START(UnityPerMaterial)
                half _Depth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half stroke = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, input.uv).r;
                half rim = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, input.uv + float2(0.0035, -0.0035)).r;
                half carved = saturate(stroke * 2.4h - 0.035h);
                half bevel = saturate(rim * 1.6h - carved - 0.035h);
                half shade = 1.0h + _Depth * (bevel * 0.48h - carved);
                return half4(shade, shade, shade, 1.0h);
            }
            ENDHLSL
        }
    }
}