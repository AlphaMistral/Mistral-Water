Shader "LowPolyWater/Lite" {
	Properties {
		// Lighting
		_Color ("Color", Color) = (0,0.5,0.7)
		_Opacity ("Opacity", Range(0,1)) = 0.7
		_Gloss ("Specular Gloss", Range(0,1)) = 0.6
		_Specular ("Specular", Range(0.03,3)) = 0.6
		_SpecColor("Sun Color", Color) = (1,1,1,1)
		_Smoothness("Smoothness", Range(0,1)) = 1
		[NoScaleOffset] _FresnelTex ("Fresnel (A) ", 2D) = "" { }
		[KeywordEnum(Flat, VertexLit, PixelLit)] _Shading("Shading", Float) = 0

		// Waves
		[KeywordEnum(Off, LowQuality, HighQuality)]  _Waves("Enable Waves", Float) = 0
		_Length("Wave Length", Float) = 3.3
		_Stretch("Wave Stretch", Float) = 10
		_Speed("Wave Speed", Float) = 0.5
		_Height ("Wave Height", Float) = 1
		_Steepness ("Wave Steepness", Range(0,1)) = 0.5
		_Direction ("Wave Direction", Range(0,360)) = 180.0

		//Ripples
		_RSpeed("Ripple Speed", Float) = 1
		_RHeight ("Ripple Height", Float) = 0.2

		//Shore
		[Toggle] _EdgeBlend("Enable Shore", Float) = 0
		_ShoreColor("Shore Color", Color) = (1,1,1,1)
		_ShoreIntensity("Shore Intensity", Range(-1,1)) = 0
		_ShoreDistance("Shore Distance", Float) = 1

		//Other
		[NoScaleOffset] _NoiseTex("Noise Texture (A)", 2D) = "white" {}
		[Toggle] _ZWrite ("Write To Depth Buffer", Float) = 0
		
		[HideInInspector] _Direction_ ("_Direction_", Vector) = (0,0,0,0)
		[HideInInspector] _Scale_("_Scale_", Float) = 1
		[HideInInspector] _RHeight_ ("_RHeight_", Float) = 0.2
		[HideInInspector] _RSpeed_ ("_RSpeed_", Float) = 0.2
		[HideInInspector] _TexSize_("_TexSize_", Float) = 64
		[HideInInspector] _Speed_("_Speed_", Float) = 0
		[HideInInspector] _Height_("_Height_", Float) = 0
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent-200"}
		LOD 200
		ZWrite [_ZWrite]

        Pass {
			Name "FORWARD"
			Tags { "LightMode" = "ForwardBase" }

			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma target 3.0 
			#pragma vertex vert
			#pragma fragment frag

			#pragma shader_feature _ _SHADING_VERTEXLIT _SHADING_PIXELLIT
			#pragma shader_feature _EDGEBLEND_ON
			#pragma shader_feature _ _WAVES_OFF _WAVES_HIGHQUALITY
			#pragma multi_compile_fog

			#include "LPWLite.cginc"

			ENDCG
        } // Pass
		
		Pass {
            Tags {"LightMode" = "Vertex"}

			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma target 3.0 
			#pragma vertex vert
			#pragma fragment frag

			#pragma shader_feature _ _SHADING_VERTEXLIT _SHADING_PIXELLIT
			#pragma shader_feature _EDGEBLEND_ON
			#pragma shader_feature _ _WAVES_OFF _WAVES_HIGHQUALITY
			#pragma multi_compile_fog

			#define LPWVERTEXLM
			#include "LPWLite.cginc"

			ENDCG

		} // Pass
	} // Subshader

	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent-200"}
		LOD 200
		ZWrite [_ZWrite]

        Pass {
			Name "FORWARD"
			Tags { "LightMode" = "ForwardBase" }

			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma target 2.0 
			#pragma vertex vert
			#pragma fragment frag

			#pragma shader_feature _ _SHADING_VERTEXLIT
			#pragma shader_feature _ _EDGEBLEND_ON
			#pragma multi_compile_fog

			#define _WAVES_OFF
			#include "LPWLite.cginc"

			ENDCG
        } // Pass
		
		Pass {
            Tags {"LightMode" = "Vertex"}

			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma target 2.0 
			#pragma vertex vert
			#pragma fragment frag

			#pragma shader_feature _ _SHADING_VERTEXLIT
			#pragma shader_feature _ _EDGEBLEND_ON
			#pragma multi_compile_fog

			#define LPWVERTEXLM
			#define _WAVES_OFF
			#include "LPWLite.cginc"

			ENDCG

		} // Pass
	} // Subshader

	Fallback "Legacy Shaders/VertexLit"
	CustomEditor "LPWAsset.LPWShaderGUI"
} // Shader
