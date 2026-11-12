// ぎょろめ: 白目・まぶた・しずく用のトゥーンシェーダー（頂点カラー × 色、やわらかい 2 階調、ふちの光、つや）
Shader "Gyorome/Toon"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0.78, 0.80, 0.88, 1)
        _ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.42
        _ShadowSoftness ("Shadow Softness", Range(0.001, 0.5)) = 0.18
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.15
        _RimWidth ("Rim Width", Range(0, 1)) = 0.3
        [Toggle] _Glint ("Glint (eye highlight)", Float) = 1
        _GlintSize ("Glint Size (cos)", Range(0.8, 0.999)) = 0.955
        _Glint2Size ("Glint 2 Size (cos)", Range(0.8, 0.999)) = 0.992
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardToon"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "GyoCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _ShadowColor;
                half _ShadowThreshold;
                half _ShadowSoftness;
                half4 _RimColor;
                half _RimStrength;
                half _RimWidth;
                half _Glint;
                half _GlintSize;
                half _Glint2Size;
                half _Cull;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half4 color : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 N = normalize(i.normalWS) * IS_FRONT_VFACE(face, 1.0, -1.0);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                half3 albedo = GyoVertexColor(i.color.rgb) * _Color.rgb;
                half lit = GyoToonLit(N, _ShadowThreshold, _ShadowSoftness);
                Light light = GetMainLight();
                half3 col = albedo * lerp(_ShadowColor.rgb, half3(1, 1, 1), lit) * light.color;
                half fres = 1.0h - saturate(dot(N, V));
                col += _RimColor.rgb * _RimStrength * smoothstep(1.0h - _RimWidth, 1.0h, fres) * albedo;
                if (_Glint > 0.5h)
                {
                    half g = GyoGlint(N, _GlintSize, _Glint2Size);
                    col = lerp(col, half3(1, 1, 1), g);
                }
                return half4(col, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack Off
}
