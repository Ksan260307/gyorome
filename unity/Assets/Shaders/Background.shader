// ぎょろめ: 背景（上下のグラデーション + まん中がほんのり明るい + 四すみを少し暗く）
Shader "Gyorome/Background"
{
    Properties
    {
        _ColorTop ("Top", Color) = (1.0, 0.94, 0.78, 1)
        _ColorBottom ("Bottom", Color) = (1.0, 0.80, 0.54, 1)
        _Vignette ("Vignette", Range(0, 1)) = 0.35
        _Glow ("Center Glow", Range(0, 1)) = 0.18
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry-10" }
        Cull Off
        ZWrite On

        Pass
        {
            Name "Background"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorTop;
                half4 _ColorBottom;
                half _Vignette;
                half _Glow;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half t = smoothstep(0.0h, 1.0h, i.uv.y);
                half3 col = lerp(_ColorBottom.rgb, _ColorTop.rgb, t);
                float2 c = i.uv - 0.5;
                half d = length(c * float2(1.0, 1.0)) * 1.35h;
                col += _Glow * (1.0h - smoothstep(0.0h, 0.7h, d)) * half3(1, 1, 1) * 0.5h;
                col *= 1.0h - _Vignette * smoothstep(0.55h, 1.05h, d);
                return half4(col, 1.0h);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
