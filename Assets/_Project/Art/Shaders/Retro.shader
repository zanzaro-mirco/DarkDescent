// Il filtro retrò (D2 della M11): a tutto schermo, dopo il post-processing e prima della UI.
// Ogni pixel va al colore più vicino della palette di 32, nello spazio sRGB, dopo uno spostamento
// del dithering ordinato di Bayer 4 x 4 legato ai pixel dello schermo: il motivo resta fermo quando
// la camera si muove o gira. Forza, dithering e palette sono proprietà globali, scritte da RetroFilter.
Shader "DarkDescent/Retro"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "Retro"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define PALETTE_SIZE 32

            // la distanza tra due colori vicini della palette, circa: il dithering sposta di metà
            #define DITHER_SPREAD 0.12

            float _RetroStrength;
            float _RetroDither;
            float4 _RetroPalette[PALETTE_SIZE];

            static const float Bayer[16] =
            {
                0.0 / 16.0,  8.0 / 16.0,  2.0 / 16.0, 10.0 / 16.0,
                12.0 / 16.0, 4.0 / 16.0, 14.0 / 16.0,  6.0 / 16.0,
                3.0 / 16.0, 11.0 / 16.0,  1.0 / 16.0,  9.0 / 16.0,
                15.0 / 16.0, 7.0 / 16.0, 13.0 / 16.0,  5.0 / 16.0
            };

            half3 Nearest(half3 color)
            {
                half3 best = _RetroPalette[0].rgb;
                float bestDistance = 1e9;
                [unroll]
                for (int i = 0; i < PALETTE_SIZE; i++)
                {
                    half3 delta = color - _RetroPalette[i].rgb;
                    // il verde pesa di più, il blu di meno: come RetroPalette.Distance
                    float distance = dot(delta * delta, float3(0.3, 0.59, 0.11));
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = _RetroPalette[i].rgb;
                    }
                }

                return best;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord).rgb;
                if (_RetroStrength <= 0.0)
                {
                    return half4(color, 1.0);
                }

                half3 srgb = LinearToSRGB(saturate(color));
                uint2 pixel = (uint2)input.positionCS.xy % 4u;
                float threshold = Bayer[pixel.y * 4u + pixel.x] - 0.5;
                half3 quantized = Nearest(saturate(srgb + threshold * DITHER_SPREAD * _RetroDither));
                return half4(SRGBToLinear(lerp(srgb, quantized, _RetroStrength)), 1.0);
            }
            ENDHLSL
        }
    }
}
