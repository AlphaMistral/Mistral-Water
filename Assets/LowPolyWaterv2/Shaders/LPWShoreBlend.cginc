#ifndef LPW_SHORE_BLEND_INCLUDED
#define LPW_SHORE_BLEND_INCLUDED

#include "UnityStandardUtils.cginc"

#if defined(LPW_SHADOWS) && defined(LPW_DEPTH_EFFECT) && (SHADER_TARGET >= 30) // both shadows and depth effect
    sampler2D_float _DepthTexture;
    #define DEPTHTEX _DepthTexture
#else
    sampler2D_float _CameraDepthTexture;
    #define DEPTHTEX _CameraDepthTexture
#endif

#define CALC_SCREENPOS(screenPos, clipPos, worldPos) \
    screenPos = COMPUTESCREENPOS(clipPos); \
    (screenPos).z = lerp(clipPos.w, mul(UNITY_MATRIX_V, worldPos).z, unity_OrthoParams.w);


float _ShoreIntensity;
float _ShoreDistance;
fixed4 _ShoreColor, _DeepColor;
#ifdef _LIGHTABS_ON
    float _Absorption;
#endif
#ifdef LPW_HQFOAM
    float _FoamSpread, _FoamScale, _FoamSpeed;
#endif


inline float ShoreBlend(float4 screenPos
    #ifdef _LIGHTABS_ON
        , out float abso
    #endif
    ){
    float sceneZ = SAMPLE_DEPTH_TEXTURE_PROJ(DEPTHTEX, UNITY_PROJ_COORD(screenPos));
    float perpectiveZ = LinearEyeDepth(sceneZ);
    #if defined(UNITY_REVERSED_Z)
        sceneZ = 1-sceneZ;
    #endif

    float orthoZ = sceneZ*(_ProjectionParams.y - _ProjectionParams.z) - _ProjectionParams.y;

    sceneZ = lerp(perpectiveZ, orthoZ, unity_OrthoParams.w);

    float diff = (1.0-unity_OrthoParams.w*2.0) * (sceneZ - screenPos.z);
    diff = lerp(10000.0, diff, diff > 0);

    #ifdef _LIGHTABS_ON
        abso = saturate(diff / _Absorption);
    #endif

    return diff;
}

inline void Foam(float diff, 
    #ifdef LPW_HQFOAM
        float3 worldPos, 
    #endif
    #ifdef LPW_SHADOWS
        float atten, 
    #endif
    inout half4 color){
    diff /= _ShoreDistance;

    #ifdef LPW_HQFOAM
        float2 uv = worldPos.xz /_FoamScale;
        float offs = _Time.x*_FoamSpeed;
        diff +=  (tex2D(_NoiseTex, uv + offs).a
                -tex2D(_NoiseTex, uv + float2(_FoamSpeed-offs, -offs)).a)
                *_FoamSpread;
    #endif

    diff = smoothstep(_ShoreIntensity, 1.0, diff);
    color.a = lerp(max(color.a, _ShoreColor.a), color.a, diff);


    half3 foamCol = lerp(color.rgb, _ShoreColor.rgb, _ShoreColor.a);
    #ifdef LPW_SHADOWS
        foamCol *= atten;
    #endif

    color.rgb = lerp(foamCol, color.rgb, diff);
}


#endif // LPW_SHORE_BLEND_INCLUDED