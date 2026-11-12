#ifndef GYOROME_COMMON_INCLUDED
#define GYOROME_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// 頂点カラーは sRGB で入っている（FBX）。リニア色空間ならリニアにする
half3 GyoVertexColor(half3 c)
{
#if defined(UNITY_COLORSPACE_GAMMA)
    return c;
#else
    return SRGBToLinear(c);
#endif
}

// 2 階調のやわらかいトゥーン：0 = かげ、1 = ひなた
half GyoToonLit(float3 N, half threshold, half softness)
{
    Light light = GetMainLight();
    half ndl = dot(N, light.direction) * 0.5h + 0.5h;
    return smoothstep(threshold - softness, threshold + softness, ndl);
}

// 目のハイライト（つや）：カメラから見た向きで決めるので、目玉が回っても同じ所に出る（本物の目の映りこみと同じ）
// 大きいのが左上、小さいのが右下
half GyoGlint(float3 normalWS, half size1, half size2)
{
    float3 nVS = normalize(TransformWorldToViewDir(normalWS));
    float3 g1 = normalize(float3(-0.42, 0.55, 0.72));
    float3 g2 = normalize(float3(0.46, -0.40, 0.79));
    half a = smoothstep(size1, size1 + 0.012h, dot(nVS, g1));
    half b = smoothstep(size2, size2 + 0.006h, dot(nVS, g2)) * 0.85h;
    return saturate(a + b);
}

#endif
