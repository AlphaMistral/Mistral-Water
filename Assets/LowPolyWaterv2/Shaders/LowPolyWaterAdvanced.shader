Shader "LowPolyWater/Advanced" {

CGINCLUDE
	// Uncomment this line to enable displacement / floating objects
	// #define LPW_DISPLACE
ENDCG	

Properties {
	// Lighting
	_Shadow("Shadow Bias", Range(0,1)) = 0.7
	_Color ("Color", Color) = (0,0.5,0.7)
	_Opacity ("Opacity", Range(0,1)) = 0.7
	_Specular ("Specular", Range(1,300)) = 70
	_SpecColor("Sun Color", Color) = (0.703,0.676,0.438,1)
	_Diffuse("Diffuse", Range(0,1)) = 0.5
	[Toggle] _PointLights("Enable Point Lights", Float) = 0
	[KeywordEnum(Flat, VertexLit, PixelLit)] _Shading("Shading", Float) = 0

	// Reflection
	[NoScaleOffset] _FresnelTex ("Fresnel (A) ", 2D) = "" {}
	_FresPower ("Fresnel Exponent", Range(0,2)) = 1.5
	_FresColor ("Fresnel Color", Color) = (0.305,0.371,0.395)
	_Reflection ("Reflection", Range(0,2)) = 1.2
	_Refraction ("Refractive Distortion", Float) = 2
	_NormalOffset ("Normal Offset", Range(0,5)) = 1
	[Toggle] _Distort("Enable Distortion", Float) = 0
	_Distortion ("Reflective Distortion", Float) = 1
	[NoScaleOffset] _BumpTex("Distortion Map", 2D) = "" {}
	_BumpScale ("Distortion Scale", Float) = 35
	_BumpSpeed ("Distortion Speed", Float) = 0.2

	// Waves
	[KeywordEnum(Off, LowQuality, HighQuality)]  _Waves("Enable Waves", Float) = 2
	_Length("Wave Length", Float) = 4
	_Stretch("Wave Stretch", Float) = 10
	_Speed("Wave Speed", Float) = 0.5
	_Height ("Wave Height", Float) = 0.5
	_Steepness ("Wave Steepness", Range(0,1)) = 0.2
	_Direction ("Wave Direction", Range(0,360)) = 180.0

	// Ripples
	_RSpeed("Ripple Speed", Float) = 1
	_RHeight ("Ripple Height", Float) = 0.25

	// Shore
	[Toggle] _EdgeBlend("Enable Foam", Float) = 0
	_ShoreColor("Foam Color", Color) = (1,1,1,1)
	_ShoreIntensity("Foam Intensity", Range(-1,1)) = 1
	_ShoreDistance("Foam Distance", Float) = 0.5
	[Toggle] _HQFoam("Enable HQ Foam", Float) = 0
	_FoamScale("Foam Scale", Float) = 20
	_FoamSpeed("Foam Speed", Float) = 0.3
	_FoamSpread("Foam Spread", Float) = 1
	[Toggle] _LightAbs("Enable Light Absorption", Float) = 0
	_Absorption ("Depth Transparency",Float) = 5
	_DeepColor ("Deep Water Color",Color) = (0,0.1,0.2,1)

	// Other
	_Scale("Global Scale", Float) = 1
	[NoScaleOffset] _NoiseTex("Noise Texture (A)", 2D) = "" {}
	[Toggle] _Cull ("Show Surface Underwater", Float) = 0
	[Toggle] _ZWrite("Write to Depth Buffer", Float) = 0

	// Hidden
	[HideInInspector] _TransformScale_ ("_TransformScale_", Float) = 1
	[HideInInspector] _Scale_ ("_Scale_", Float) = 1
	[HideInInspector] _BumpScale_ ("_BumpScale_", Float) = 1
	[HideInInspector] _Cull_ ("_Cull_", Float) = 2
	[HideInInspector] _Direction_ ("_Direction_", Vector) = (0,0,0,0)
	[HideInInspector] _RHeight_ ("_RHeight_", Float) = 0.2
	[HideInInspector] _RSpeed_ ("_RSpeed_", Float) = 0.2
	[HideInInspector] _TexSize_("_TexSize_", Float) = 64
	[HideInInspector] _Speed_("_Speed_", Float) = 0
	[HideInInspector] _Height_("_Height_", Float) = 0
	[HideInInspector] _ReflectionTex("_ReflectionTex", 2D) = "" {}
	[HideInInspector] _RefractionTex("_RefractionTex", 2D) = "" {}
	[HideInInspector] _Time_("_Time_", float) = 0
	[HideInInspector] _EnableShadows("_EnableShadows", float) = 0
	[HideInInspector] _Sun("_Sun", Vector) = (0,0,0,0)
	[HideInInspector] _SunColor("_SunColor", Color) = (1,1,1,1)
}

SubShader {
	Tags { "RenderType"="Transparent" "IgnoreProjector"="True" "Queue"="AlphaTest+51"}
	LOD 200
	ZWrite [_ZWrite]
	Cull [_Cull_]
	
	Pass {
		Tags { "LightMode" = "ForwardBase" }
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		#include "LPWStandard.cginc"

		#pragma target 3.0 
		#pragma vertex vert
		#pragma fragment frag

		#pragma shader_feature _ _SHADING_VERTEXLIT _SHADING_PIXELLIT
		#pragma shader_feature _ LPW_FOAM LPW_HQFOAM
		#pragma shader_feature _ _LIGHTABS_ON
		#pragma shader_feature _ _WAVES_OFF _WAVES_HIGHQUALITY
		#pragma shader_feature _ _CUSTOM_SHAPE _USE_LOD
		#pragma shader_feature _ _DISTORT_ON
		#pragma shader_feature _ WATER_REFRACTIVE 
		#pragma shader_feature _ WATER_REFLECTIVE 
		#pragma shader_feature _ _POINTLIGHTS_ON
		#pragma shader_feature _ LPW_SHADOWS
		
		#pragma multi_compile_fog

        #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
		ENDCG

	} // Pass

	Pass {
		Tags { "LightMode" = "Vertex" }
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		#define LPWVERTEXLM
		#include "LPWStandard.cginc"

		#pragma target 3.0 
		#pragma vertex vert
		#pragma fragment frag

		#pragma shader_feature _ _SHADING_VERTEXLIT _SHADING_PIXELLIT
		#pragma shader_feature _ LPW_FOAM LPW_HQFOAM
		#pragma shader_feature _ _LIGHTABS_ON
		#pragma shader_feature _ _WAVES_OFF _WAVES_HIGHQUALITY
		#pragma shader_feature _ _CUSTOM_SHAPE _USE_LOD
		#pragma shader_feature _ _DISTORT_ON
		#pragma shader_feature _ WATER_REFRACTIVE 
		#pragma shader_feature _ WATER_REFLECTIVE 
		#pragma shader_feature _ _POINTLIGHTS_ON
		#pragma shader_feature _ LPW_SHADOWS
		
		#pragma multi_compile_fog

        #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
		ENDCG

	} // Pass

    Pass {
		Tags { "LightMode" = "ShadowCaster" }

		CGPROGRAM
		#define LPW_NOLIGHT
		#include "LPWStandard.cginc"

		#pragma target 3.0 
		#pragma vertex vert
		#pragma fragment frag_empty

		#pragma shader_feature _ _WAVES_OFF _WAVES_HIGHQUALITY
		#pragma shader_feature _ _CUSTOM_SHAPE _USE_LOD
		ENDCG

	} // Pass
} // Subshader

SubShader {
	Tags { "RenderType"="Transparent" "IgnoreProjector"="True" "Queue"="AlphaTest+51"}
	LOD 200
	ZWrite [_ZWrite]
	Cull [_Cull_]
	
	Pass {
		Tags { "LightMode" = "ForwardBase" }
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		#define _WAVES_OFF
		#include "LPWStandard.cginc"

		#pragma target 2.0 
		#pragma vertex vert
		#pragma fragment frag

		#pragma shader_feature _ _SHADING_VERTEXLIT
		#pragma shader_feature _ _CUSTOM_SHAPE _USE_LOD
		#pragma shader_feature _ LPW_FOAM
		
		#pragma multi_compile_fog

		ENDCG

	} // Pass

	Pass {
		Tags { "LightMode" = "Vertex" }
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		#define LPWVERTEXLM
		#define _WAVES_OFF
		#include "LPWStandard.cginc"

		#pragma target 2.0 
		#pragma vertex vert
		#pragma fragment frag

		#pragma shader_feature _ _SHADING_VERTEXLIT
		#pragma shader_feature _ LPW_FOAM
		#pragma shader_feature _ _CUSTOM_SHAPE _USE_LOD
		
		#pragma multi_compile_fog
		ENDCG

	} // Pass
} // Subshader

Fallback "Mobile/VertexLit"
CustomEditor "LPWAsset.LPWShaderGUI"
} // Shader
