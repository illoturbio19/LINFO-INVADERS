Shader "LINFO/PixelBiomedicalGalaxy"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.015, 0.02, 0.055, 1)
        _BloodColor ("Blood Nebula", Color) = (0.55, 0.04, 0.09, 1)
        _CyanColor ("Immune Glow", Color) = (0.0, 0.85, 1.0, 1)
        _PurpleColor ("Tumor Depth", Color) = (0.36, 0.12, 0.62, 1)
        _PixelGrid ("Pixel Grid", Float) = 96
        _StarDensity ("Star Density", Range(0, 1)) = 0.14
        _VeinDensity ("Vein Density", Range(0, 1)) = 0.42
        _Speed ("Speed", Float) = 0.22
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "PixelBiomedicalGalaxyUnlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _BloodColor;
                half4 _CyanColor;
                half4 _PurpleColor;
                float _PixelGrid;
                float _StarDensity;
                float _VeinDensity;
                float _Speed;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y * _Speed;
                float grid = max(8.0, _PixelGrid);
                float2 uv = floor(input.uv * grid) / grid;
                float2 centered = uv * 2.0 - 1.0;

                float radius = length(centered);
                float swirlA = sin(radius * 10.0 + centered.x * 3.4 - centered.y * 2.2 - time * 1.8);
                float swirlB = sin(radius * 7.0 - centered.x * 1.8 + centered.y * 3.1 + time * 1.15);
                float spiral = (swirlA * 0.62 + swirlB * 0.38) * 0.5 + 0.5;
                float tissue = ValueNoise(uv * 5.5 + float2(time * 0.55, -time * 0.24));
                float marrow = ValueNoise(uv * 13.0 - float2(time * 0.15, time * 0.35));
                float veinLine = abs(sin((uv.x * 8.0 + tissue * 2.4 + time) + sin(uv.y * 9.0) * 0.5));
                float veins = smoothstep(0.035 + _VeinDensity * 0.05, 0.0, veinLine) * 0.32;

                half3 color = _BaseColor.rgb;
                color = lerp(color, _PurpleColor.rgb, tissue * 0.32);
                color = lerp(color, _BloodColor.rgb, spiral * marrow * 0.42);
                color += _BloodColor.rgb * veins;

                float2 starGridUv = input.uv * grid * 0.48;
                float2 starCell = floor(starGridUv);
                float2 starLocal = frac(starGridUv) - 0.5;
                float starShape = 1.0 - step(0.095, abs(starLocal.x)) * step(0.095, abs(starLocal.y));
                starShape *= step(max(abs(starLocal.x), abs(starLocal.y)), 0.22);
                float starHash = Hash21(starCell);
                float starPulse = sin(time * 8.0 + starHash * 6.28318) * 0.5 + 0.5;
                float stars = step(1.0 - _StarDensity * 0.055, starHash) * starShape * (0.25 + starPulse * 0.42);
                float immuneCells = step(0.996, Hash21(starCell + 71.2)) * starShape * (0.25 + starPulse * 0.45);

                color += _CyanColor.rgb * stars * 0.22;
                color += lerp(_CyanColor.rgb, half3(1.0, 0.95, 0.6), 0.35) * immuneCells * 0.28;

                float vignette = smoothstep(1.3, 0.22, length(centered));
                color *= 0.42 + vignette * 0.78;
                return half4(color * input.color.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
