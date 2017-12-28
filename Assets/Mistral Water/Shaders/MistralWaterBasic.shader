Shader "Mistral Water/Basic"
{
	Properties
	{
		_Anim ("Animation", 2D) = "black" {}
		_Bump ("BUBUBU", 2D) = "bump" {}
		_White ("WHWHWH", 2D) = "black" {}
		_ShallowColor ("Color in the Shallow Area", Color) = (1, 1, 1, 1)
		_DeepColor ("Color in the Deep Area", Color) = (0, 0, 0, 0)
		_DepthAmount ("The maximum of depth", Float) = 1

		[Toggle(_DEPTHFOG_ON)]
		_EnableFog ("Whether Enable Depth Fog", Float) = 0
		_EdgeFade ("Edge Fade", Float) = 1

		_SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
		_Smoothness ("Specular Smoothness", Range(0.01, 10)) = 0.5

		_BumpTex ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Normal Strength", Range(0, 2)) = 1
		_BumpSpeedX ("Normal Speed [X]", Float) = 0.5
		_BumpSpeedY ("Normal Speed [Y]", Float) = 0.5

		_Distortion ("Reflection & Refraction Distortion Scale", Range(0, 100)) = 50.0

		[KeywordEnum(RealTime, CubeMap)]
		_ReflectionType ("Reflection Type", Float) = 0

		_CubeTint ("Cube Color Tint", Color) = (1, 1, 1, 1)

		[NoScaleOffset]
		_CubeMap ("Cube Map", Cube) = "white" {}

		[NoScaleOffset]
		_ReflectionTex ("Reflection Map (No Editting!)", 2D) = "white" {}

		_ReflectionStrength ("Reflection Strength", Range(0, 1)) = 1
		_FresnelAngle ("Fresnel Angle", Range(1, 45)) = 5

		[Toggle(_FOAM_ON)]
		_EnableFoam ("Whether Enable Foam", Float) = 0
		_FoamColor ("Foam Color Tint", Color) = (1, 1, 1, 1)
		_FoamTex ("Foam Texture", 2D) = "white" {}
		_FoamSize ("Foam Size", Float) = 0.5

		[KeywordEnum(Off, Wave, Gerstner)]
		_DisplacementMode ("Displacement Mode", Float) = 0

		_Amplitude ("Amplitude", Float) = 0.05
		_Frequency ("Frequency", Float) = 1
		_Speed ("Wave Speed", Float) = 1
		_Steepness ("Wave Steepness", Float) = 1

		_WSpeed ("Wave Speed", Vector) = (1.2, 1.375, 1.1, 1.5)
		_WDirectionAB ("Wave1 Direction", Vector) = (0.3, 0.85, 0.85, 0.25)
		_WDirectionCD ("Wave2 Direction", Vector) = (0.1, 0.9, 0.5, 0.5)

		_Smoothing ("Normal Smoothing", Range(0, 1)) = 1
	}

	SubShader
	{
		Tags { "Queue" = "Transparent"  "RenderType" = "Transparent" }

		GrabPass
		{
			Tags { "LightMode" = "Always" }
		}

		Pass
		{
			Tags { "LightMode" = "ForwardBase" }
			ZWrite On
			ZTest LEqual
			Cull Back

			CGPROGRAM

			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			#include "MistralWaterCommon.cginc"

			#pragma target 3.0
			//#pragma hull hs_surf
			//#pragma domain ds_surf
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma shader_feature _REFLECTIONTYPE_CUBEMAP _REFLECTIONTYPE_REALTIME
			#pragma shader_feature __ _FOAM_ON
			#pragma shader_feature __ _DEPTHFOG_ON
			#pragma shader_feature __ _DISPLECEMENTMODE_OFF _DISPLACEMENTMODE_WAVE _DISPLACEMENTMODE_GERSTNER

			ENDCG
		}
	}
}
