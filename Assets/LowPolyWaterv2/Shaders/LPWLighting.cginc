#ifndef LPW_LIGHTING_INCLUDED
#define LPW_LIGHTING_INCLUDED

#include "UnityLightingCommon.cginc"
#include "UnityStandardUtils.cginc"

#if defined(LPWVERTEXLM) || (SHADER_TARGET < 30)
    #define LPW_AMBIENT(normal) UNITY_LIGHTMODEL_AMBIENT.rgb
    #define LIGHTCOLOR half3(1,1,1)
#else
    #define LPW_AMBIENT(normal) max(0.0, ShadeSH9(half4(normal, 1.0)))
    #define LIGHTCOLOR _SunColor.rgb // _LightColor0.rgb
#endif

#define LPW_ALPHA(fres, spec) saturate(_Opacity + (fres) + (spec))


half _Opacity, _Specular, _Smoothness;
sampler2D _FresnelTex; 
fixed4 _Color, _SunColor;
half3 _Sun;
half _Shadow;
half _FresPower;
half _Diffuse;
fixed3 _FresColor;
#if defined(WATER_REFLECTIVE)
    half _Reflection;
#endif

struct LPWLightingInput{
    half3 diff;
    half3 ambient;
    half fresPow;
    half specPow;
    #ifdef LPW_SHADOWS
        half atten;
    #endif
    #if defined(WATER_REFLECTIVE)
        half3 refl;
    #endif
    #if defined(WATER_REFRACTIVE)
        half3 refr;
    #endif
    #if defined(_LIGHTABS_ON)
        float abso; // light absorption
    #endif
    #if defined(_POINTLIGHTS_ON)
        half3 pointLights;
    #endif
};


inline half3 LPWLightingBase(half3 normal, float3 worldPos, out half3 ambient, out half fresPow, out half specPow, out half3 diff){
    half3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));

    ambient = LPW_AMBIENT(normal);

    // Fresnel
    half dn = saturate(dot( worldViewDir, normal ));
    #if (SHADER_TARGET < 30) || defined(SHADER_API_GLES)
        fresPow = 1.0-dn;
        fresPow *= fresPow;
    #else
        fresPow = tex2Dlod(_FresnelTex, half4(dn,0.5,0,0) ).a;
        fresPow = pow(fresPow, _FresPower);
    #endif

    // Specular
    half3 h = normalize (_Sun + worldViewDir);
    half nh = max (0.0, dot (normal, h));
    specPow = pow (nh, _Specular);

    // Diffuse
    half nl = max(0.0, dot(normal, _Sun));
    diff = lerp(0.5, nl, _Diffuse)*LIGHTCOLOR+ambient; 

    return worldViewDir;
}


inline fixed4 LPWLightingFrag(LPWLightingInput i){
    #ifdef LPW_SHADOWS
        i.specPow *= step(1, i.atten); // 1 >= atten
    #endif

    half3 spec = _SpecColor.rgb * i.specPow;
    #ifdef _POINTLIGHTS_ON
        spec += i.pointLights;
    #endif

    half3 watCol = _Color.rgb;
    half opacity = _Opacity;

    #if defined(_LIGHTABS_ON)
        half4 deepCol = _DeepColor;
        half4 watColOpacity = lerp(half4(watCol, opacity), deepCol, i.abso);
        watCol = watColOpacity.rgb;
        opacity = watColOpacity.a;
    #endif

    #if defined(WATER_REFLECTIVE)
        half3 fres = _FresColor*i.ambient + lerp(i.ambient, i.refl.rgb, saturate(_Reflection)) ;
        watCol =  lerp(watCol, i.refl.rgb, saturate(_Reflection-1.0));
    #else
        half3 fres = i.ambient + _FresColor;
    #endif

    half3 diff = watCol*i.diff;
    half3 col = lerp(diff, fres, i.fresPow);

    #ifdef LPW_SHADOWS
        col *= saturate(i.atten+_Shadow);
    #endif

    half alpha = saturate(opacity + i.fresPow + i.specPow);

    col += spec;

    #if defined(WATER_REFRACTIVE)
        half3 refr = lerp(i.refr, diff, _Opacity);
        return fixed4(lerp(refr, col, alpha), 1);
    #else
        return fixed4(col, alpha);
    #endif
}

inline half3 PointLight(int index, float3 worldPos, half3 worldViewDir, half3 normal){
        float3 lightPosition = float3(unity_4LightPosX0[index], unity_4LightPosY0[index], unity_4LightPosZ0[index]);
        float3 lightDir = lightPosition - worldPos;  
        float dist = dot(lightDir,lightDir);
        lightDir *= rsqrt(dist); // == normalize

        float attenuation = 1.0 / (1.0 + unity_4LightAtten0[index] * dist);
        half3 h = normalize (lightDir + worldViewDir);
        half nh = max (0.0, dot (normal, h));
        half specPow = pow(nh, _Specular*0.5);

        float3 diff = max(0.0, dot(normal, lightDir));         

        return unity_LightColor[index].rgb * (saturate(attenuation*10.0)*specPow + attenuation*diff)*0.5;
}

inline half3 PointLightsSpec(float3 worldPos, half3 worldViewDir, half3 normal){
    half3 light = PointLight(0, worldPos, worldViewDir, normal);
    light += PointLight(1, worldPos, worldViewDir, normal);
    light += PointLight(2, worldPos, worldViewDir, normal);
    light += PointLight(3, worldPos, worldViewDir, normal);
    return light;
}


#if _SHADING_PIXELLIT

	inline void LPWLightingPixel(half3 normal, float3 worldPos, inout LPWLightingInput i){
        half3 worldViewDir = LPWLightingBase(normal, worldPos, i.ambient, i.fresPow, i.specPow, i.diff);
        #ifdef _POINTLIGHTS_ON
            i.pointLights = PointLightsSpec(worldPos, worldViewDir, normal);
        #endif
	}

#else // not _SHADING_PIXELLIT

	inline void LPWLightingVert(half3 normal, float3 worldPos, inout fraginput o){
        half3 worldViewDir = LPWLightingBase(normal, worldPos, 
            o.vertexLight1.xyz, //ambient
            o.vertexLight0.w, //fresPow
            o.vertexLight1.w, //specPow
            o.vertexLight0.xyz //diff
        );

        #if defined(_POINTLIGHTS_ON)
            o.vertexLight2 = PointLightsSpec(worldPos, worldViewDir, normal);
        #endif
	}

	inline void LPWUnpackVertLights(fraginput i, inout LPWLightingInput li){
		li.diff = i.vertexLight0.rgb;
		li.fresPow = i.vertexLight0.a;
		li.ambient = i.vertexLight1.rgb;
		li.specPow = i.vertexLight1.a;
        #if defined(_POINTLIGHTS_ON)
            li.pointLights = i.vertexLight2;
        #endif
	}

#endif //_SHADING_PIXELLIT

#endif // LPW_LIGHTING_INCLUDED