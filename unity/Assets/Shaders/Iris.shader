// ぎょろめ: 黒目（ひとみ）。模様のない、まんまるの黒い円。
// メッシュの UV = (中心からの距離 0..1, 角度 0..1)。ひとみの外がわは白目と同じ色で描く。
// 大きさ _PupilSize、形 _PupilShape（まる・ねこ・ハート・星・ぐるぐる・×・輪）、色 _IrisColor（ふだんは黒。反応で金色や虹色に）
Shader "Gyorome/Iris"
{
    Properties
    {
        _IrisColor ("Pupil Color", Color) = (0.08, 0.07, 0.09, 1)
        _IrisInner ("Pupil Color (unused)", Color) = (0.08, 0.07, 0.09, 1)
        _PupilColor ("Pupil Color (fallback)", Color) = (0.08, 0.07, 0.09, 1)
        _ScleraColor ("Sclera Color", Color) = (1, 1, 1, 1)
        _PupilSize ("Pupil Size", Range(0.05, 1.0)) = 0.72
        _PupilShape ("Pupil Shape (0 round 1 slit 2 heart 3 star 4 spiral 5 cross 6 ring)", Float) = 0
        _Spin ("Spiral Spin", Float) = 0
        _Sparkle ("Sparkle", Range(0, 1)) = 0
        _ShadowColor ("Shadow Color", Color) = (0.78, 0.80, 0.88, 1)
        [Toggle] _Glint ("Glint (eye highlight)", Float) = 1
        _GlintSize ("Glint Size (cos)", Range(0.8, 0.999)) = 0.955
        _Glint2Size ("Glint 2 Size (cos)", Range(0.8, 0.999)) = 0.992
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+1" }
        LOD 100

        Pass
        {
            Name "ForwardIris"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            Offset -1, -1

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "GyoCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _IrisColor;
                half4 _IrisInner;
                half4 _PupilColor;
                half4 _ScleraColor;
                half _PupilSize;
                half _PupilShape;
                half _Spin;
                half _Sparkle;
                half4 _ShadowColor;
                half _Glint;
                half _GlintSize;
                half _Glint2Size;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD3;
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
                o.uv = v.uv;
                return o;
            }

            float dot2(float2 v) { return dot(v, v); }

            float sdHeart(float2 p)
            {
                p.x = abs(p.x);
                if (p.y + p.x > 1.0) return sqrt(dot2(p - float2(0.25, 0.75))) - sqrt(2.0) / 4.0;
                return sqrt(min(dot2(p - float2(0.00, 1.00)), dot2(p - 0.5 * max(p.x + p.y, 0.0)))) * sign(p.x - p.y);
            }

            float sdBox(float2 p, float2 b)
            {
                float2 d = abs(p) - b;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
            }

            // ひとみの形の符号つき距離（中が負）。p は半径 1 の座標、a は角度、s はひとみの大きさ
            float PupilDistance(float2 p, float a, float r, float s, int shape)
            {
                if (shape == 1) return (length(float2(p.x * 2.6, p.y)) - s) * 0.45;       // ねこ
                if (shape == 2) { float2 q = p / (s * 1.05) + float2(0.0, 0.55); return sdHeart(q) * s * 1.05; }   // ハート
                if (shape == 3) { float k = 0.5 + 0.5 * sin(5.0 * a); return r - s * (0.45 + 0.55 * k * k); }   // 星
                if (shape == 5)                                                             // ×
                {
                    float c = 0.70710678;
                    float2 q = float2(c * p.x - c * p.y, c * p.x + c * p.y);
                    return min(sdBox(q, float2(s, s * 0.26)), sdBox(q, float2(s * 0.26, s)));
                }
                if (shape == 6) return abs(r - s * 0.72) - s * 0.28;                       // 輪
                return r - s;                                                               // まる・ぐるぐる
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 N = normalize(i.normalWS);
                float r = saturate(i.uv.x);
                float a = i.uv.y * 6.2831853 + _Spin * 0.35;
                float2 p = r * float2(cos(a), sin(a));
                int shape = (int)(_PupilShape + 0.5);

                // ひとみの外がわ：白目と同じ
                half lit = GyoToonLit(N, 0.4h, 0.2h);
                Light light = GetMainLight();
                // 白目のメッシュの頂点カラー（#fbf9f6）とそろえる（ちがうと、黒目のまわりに明るい輪が見える）
                half3 scleraBase = GyoVertexColor(half3(0.984h, 0.976h, 0.965h));
                half3 white = scleraBase * _ScleraColor.rgb * lerp(_ShadowColor.rgb, half3(1, 1, 1), lit) * light.color;
                half fres = 1.0h - saturate(dot(N, normalize(_WorldSpaceCameraPos.xyz - i.positionWS)));
                white += 0.12h * smoothstep(0.7h, 1.0h, fres) * scleraBase;   // 白目と同じふちの光

                // ひとみ：まんまるの黒（ふちをなめらかに）
                float d = PupilDistance(p, a, r, _PupilSize, shape);
                float aa = max(fwidth(d), 0.003);
                half pupil = 1.0h - smoothstep(-aa, aa, d);
                half3 pupilCol = _IrisColor.rgb;
                if (shape == 4)
                {
                    float stripe = sin(a * 2.0 - r / max(_PupilSize, 0.05) * 9.0 + _Spin * 3.0);
                    pupilCol = lerp(_IrisColor.rgb, half3(0.92, 0.92, 0.95), smoothstep(-0.1, 0.1, stripe) * 0.9h);
                }
                half3 col = lerp(white, pupilCol, pupil);

                // きらめき（うれしい・みがいた）
                if (_Sparkle > 0.001h)
                {
                    float2 q = p - float2(-0.38, 0.40);
                    half pulse = 0.7h + 0.3h * sin(_Time.y * 6.0);
                    half star = max(exp2(-abs(q.x) * 48.0) * exp2(-abs(q.y) * 9.0), exp2(-abs(q.y) * 48.0) * exp2(-abs(q.x) * 9.0));
                    col = lerp(col, half3(1, 1, 1), saturate(star * 1.3h) * _Sparkle * pulse);
                }

                if (_Glint > 0.5h)
                {
                    half g = GyoGlint(N, _GlintSize, _Glint2Size);
                    col = lerp(col, half3(1, 1, 1), g);
                }
                return half4(col, 1.0h);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
