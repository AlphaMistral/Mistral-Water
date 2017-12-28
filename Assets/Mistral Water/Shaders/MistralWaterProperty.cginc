#ifndef MISTRAL_WATER_PROPERTY_INCLUDED
#define MISTRAL_WATER_PROPERTY_INCLUDED

struct VertexInput
{
	float4 vertex : POSITION;
	half3 normal : NORMAL;
	half4 tangent : TANGENT;
	half2 texcoord : TEXCOORD0;
	half4 color : COLOR;
};

struct VertexOutput
{
	float4 pos : SV_POSITION;
	fixed3 ambient : TEXCOORD9;

	half4 tSpace0 : TEXCOORD0;
	half4 tSpace1 : TEXCOORD1;
	half4 tSpace2 : TEXCOORD2;

	float3 worldPos : TEXCOORD3;
	half4 grabUV : TEXCOORD4;
	half4 screenPos : TEXCOORD5;
	half4 animUV : TEXCOORD6;
	half4 color : TEXCOORD10;

	#if _FOAM_ON

		half2 foamUV : TEXCOORD7;

	#endif

	#ifdef UNITY_PASS_FORWARDBASE

		UNITY_FOG_COORDS(8)

	#endif
};

#endif
