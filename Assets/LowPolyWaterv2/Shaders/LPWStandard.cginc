#ifndef LPW_STANDARD_INCLUDED
#define LPW_STANDARD_INCLUDED

// shadow helper functions and macros
#include "AutoLight.cginc"
#include "UnityCG.cginc"
#include "LPWCompatibility.cginc"

#if defined(WATER_REFLECTIVE) || defined(WATER_REFRACTIVE)
	#define WATER_REFL_OR_REFR
#endif

#if defined(_SHADING_PIXELLIT) || defined(WATER_REFL_OR_REFR)
	#define PASS_WORLDNORM
#endif

#if defined(_SHADING_PIXELLIT) || defined(WATER_REFL_OR_REFR) || defined(LPW_HQFOAM) || defined(LPW_SHADOWS)
    #define PASS_WORLDPOS
#endif

#if defined(LPW_HQFOAM) || defined(LPW_FOAM) || defined(_LIGHTABS_ON)
	#define LPW_DEPTH_EFFECT
	#if defined(LPW_HQFOAM) || defined(LPW_FOAM)
		#define LPW_ANY_FOAM
	#endif
#endif

#if defined(LPW_DEPTH_EFFECT) || defined(WATER_REFL_OR_REFR)
	#define PASS_SCREENPOS
#endif

#ifdef LPW_DISPLACE
	#define LPW_TIME _Time_
	float _Time_;
#else
	#define LPW_TIME _Time[1]
#endif

struct vertinput {
	float4 vertex : POSITION;
	#ifdef _CUSTOM_SHAPE
		float3 normal : NORMAL;
		float4 uv0 : TEXCOORD0;
		float2 uv1 : TEXCOORD1;
	#elif _USE_LOD
		float4 uv0 : TEXCOORD0;
	#else 
		float2 uv0 : TEXCOORD0;
	#endif
};


struct fraginput {
	float4 pos : SV_POSITION;
	UNITY_SHADOW_COORDS(0)
	UNITY_FOG_COORDS(1)
	#ifdef PASS_WORLDPOS
		float3 worldPos : TEXCOORD2;
	#endif
	#ifndef _SHADING_PIXELLIT
		half4 vertexLight0 : TEXCOORD3;
		half4 vertexLight1 : TEXCOORD4;
		#ifdef _POINTLIGHTS_ON
			half3 vertexLight2 : TEXCOORD7;
		#endif
	#else
	#endif
	#ifdef PASS_WORLDNORM
		half3 worldNormal : TEXCOORD5;
	#endif
	#ifdef PASS_SCREENPOS
		float4 screenPos : TEXCOORD6;
	#endif
	UNITY_VERTEX_OUTPUT_STEREO
};


#include "LPWNoise.cginc"
#include "LPWRipples.cginc"
#ifndef _WAVES_OFF
	#include "LPWWaves.cginc"
#endif
#ifdef LPW_DEPTH_EFFECT
	#include "LPWShoreBlend.cginc"
#endif
#include "LPWLighting.cginc"
#if defined(WATER_REFL_OR_REFR)
	#include "LPWReflection.cginc"
#endif
#define TOWORLD(p) mul(unity_ObjectToWorld, p)


fraginput vert (vertinput v) {
	fraginput o;
	UNITY_INITIALIZE_OUTPUT(fraginput,o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float4 p = v.vertex;
	float4 pos0 = TOWORLD(p);

	#ifdef _CUSTOM_SHAPE
		p.xyz -= float3(v.uv0.xy, v.uv1.x);
		float3 pos1 = TOWORLD(p).xyz;
		p.xyz = v.vertex.xyz;
		p.xyz -= float3(v.uv0.zw, v.uv1.y);
		float3 pos2 = TOWORLD(p).xyz;

		RipplesVertCustom(v.normal, pos0.xyz, pos1, pos2);
	#else
		#ifdef _USE_LOD
			float4 offs = v.uv0;
		#else
			float4 offs = float4(floor(v.uv0.xy),frac(v.uv0.xy)) * float4(1.0/10000.0, 1.0/10000.0, 10.0, 10.0) - 5.0;
			offs = offs.xzyw;
		#endif
		p.xz = v.vertex.xz-offs.xy;
		float3 pos1 = TOWORLD(p).xyz;
		p.xz = v.vertex.xz-offs.zw;
		float3 pos2 = TOWORLD(p).xyz;
		
		RipplesVert(pos0.xyz, pos1, pos2);
	#endif

	#if !defined(_WAVES_OFF)
		WavesVert(pos0.xyz, pos1, pos2);
	#endif

	half3 worldNormal = cross(pos1.xyz-pos0.xyz, pos2.xyz-pos0.xyz);
	worldNormal = normalize(worldNormal);

	#ifdef PASS_WORLDNORM
		o.worldNormal = worldNormal;
	#endif

	#ifdef PASS_WORLDPOS
		o.worldPos = pos0.xyz;
	#endif

	#if _SHADING_VERTEXLIT
		LPWLightingVert(worldNormal, pos0.xyz, o);
	#elif !defined(_SHADING_PIXELLIT) && !defined(LPW_NOLIGHT) // flat shading
		LPWLightingVert(worldNormal, (pos0.xyz+pos1+pos2)/3.0, o);
	#endif
 
	o.pos = mul(UNITY_MATRIX_VP, pos0);

	#if defined(LPW_DEPTH_EFFECT) 
		CALC_SCREENPOS(o.screenPos, o.pos, pos0);
	#elif defined(WATER_REFL_OR_REFR)
		o.screenPos = COMPUTESCREENPOS(o.pos);
	#endif

	v.vertex = mul(unity_WorldToObject, pos0);
	TRANSFER_SHADOW(o);
	
	UNITY_TRANSFER_FOG(o,o.pos); // pass fog coordinates to pixel shader
	return o;
}

fixed4 frag (fraginput i) : COLOR {
	UNITY_SETUP_INSTANCE_ID(i);

	LPWLightingInput li;
	UNITY_INITIALIZE_OUTPUT(LPWLightingInput, li);

	#ifdef LPW_SHADOWS
		li.atten = UNITY_SHADOW_ATTENUATION(i, i.worldPos);
	#endif

	#if defined(WATER_REFL_OR_REFR)
		LPWReflection(i.screenPos, i.worldNormal, i.worldPos, li);
	#endif

	#ifdef LPW_DEPTH_EFFECT
		float diff = ShoreBlend(i.screenPos
			#ifdef _LIGHTABS_ON
				, li.abso
			#endif
			);
	#endif

	#ifdef _SHADING_PIXELLIT
		LPWLightingPixel(i.worldNormal, i.worldPos, li);
	#else
		LPWUnpackVertLights(i, li);
	#endif

	fixed4 c = LPWLightingFrag(li);

	#ifdef LPW_ANY_FOAM
		#ifdef LPW_SHADOWS
			float atten = saturate((li.atten+_Shadow+0.1)*(dot(li.ambient,unity_ColorSpaceLuminance.rgb)*2+0.1));
		#endif
		Foam(diff, 
			#ifdef LPW_HQFOAM
				i.worldPos, 
			#endif
			#ifdef LPW_SHADOWS
				atten, 
			#endif
			c);
	#endif

	UNITY_APPLY_FOG(i.fogCoord, c.rgb); // apply fog
	return c;
}

#ifdef LPW_NOLIGHT
fixed4 frag_empty (fraginput i) : COLOR {
	return fixed4(0,0,0,0);
}
#endif

#endif // LPW_STANDARD_INCLUDED