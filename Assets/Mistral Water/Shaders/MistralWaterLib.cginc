#ifndef MISTRAL_WATER_LIB_INCLUDED
// Upgrade NOTE: excluded shader from DX11 because it uses wrong array syntax (type[size] name)
#pragma exclude_renderers d3d11
#define MISTRAL_WATER_LIB_INCLUDED

#include "MistralWaterProperty.cginc"

#if _DISPLACEMENTMODE_FFT
	uniform sampler2D _Anim;
	uniform sampler2D _Bump;
	uniform sampler2D _Height;
	uniform sampler2D _White;
#endif

uniform sampler2D _GrabTexture;
uniform half4 _GrabTexture_TexelSize;
uniform sampler2D_float _CameraDepthTexture;

#if _REFLECTIONTYPE_REALTIME

	uniform sampler2D _ReflectionTex;

#endif

uniform sampler2D _BumpTex;
uniform half _BumpScale;
uniform half4 _BumpTex_ST;
uniform half _BumpSpeedX;
uniform half _BumpSpeedY;

#if _FOAM_ON

	uniform sampler2D _FoamTex;
	uniform half4 _FoamTex_ST;
	uniform fixed3 _FoamColor;
	uniform half _FoamSize;

#endif

uniform fixed4 _ShallowColor;
uniform fixed4 _DeepColor;
uniform half _DepthAmount;
uniform half _EdgeFade;

uniform fixed3 _SpecularColor;
uniform half _Smoothness;

uniform half _ReflectionStrength;
uniform half _FresnelAngle;
uniform samplerCUBE _CubeMap;
uniform fixed3 _CubeTint;

uniform half _Amplitude;
uniform half _Frequency;
uniform half _Speed;

#if _DISPLACEMENTMODE_GERSTNER

	uniform half _Steepness;
	uniform half4 _WSpeed;
	uniform half4 _WDirectionAB;
	uniform half4 _WDirectionCD;

#endif

uniform half _Smoothing;
uniform half _Distortion;

uniform fixed4 _LightColor0;

inline void Gerstner(out half3 offsets, out half3 normal, half3 vertex, half3 sVertex, 
					half amplitude, half frequency, half steepness,
					half4 speed, half4 directionAB, half4 directionCD)
{
	half3 offs;

	half4 AB = steepness * amplitude * directionAB;
	half4 CD = steepness * amplitude * directionCD;

	half4 dotABCD = frequency * half4(dot(directionAB.xy, sVertex.xz), dot(directionAB.zw, sVertex.xz), dot(directionCD.xy, sVertex.xz), dot(directionCD.zw, sVertex.xz));
	half4 t = _Time.yyyy * speed;

	half4 COS = cos(dotABCD + t);
	half4 SIN = sin(dotABCD + t);

	offs.x = dot(COS, half4(AB.xz, CD.xz));
	offs.z = dot(COS, half4(AB.yw, CD.yw));
	offs.y = dot(SIN, amplitude);

	offsets = offs;

	normal = half3(0, 2, 0);

	normal.x -= offs.x;
	normal.y -= offs.z;
	normal.xz *= _Smoothing;
	normal = normalize(normal);
	normal = half3(0, 1, 0);
}

inline void GerstnerLevelOne(out half3 offsets, out half3 normal, half3 vertex, half3 sVertex, 
							half amplitude, half frequency, half steepness, half speed,
							half4 directionAB, half4 directionCD)
{
	const float amps[5] = {0.7, 0.6, 0.6, 0.7, 0.9};
	const float steeps[5] = {0.95, 0.615, 0.821, 0.462, 0.611};
	const float speeds[5] = {-2.112, 0.6124, -0.878, -3.6234, 1};
	const half2 dir[5] = {half2(1, -0.2), half2(-0.9, 1), half2(0.2, 0.2), half2(-1.0, 0.77), half2(0.99, -1.145)};
	const float fs[5] = {0.954, 1.52, 0.44, 0.21, 0.8};
	half3 offs = 0;

	for(int i = 0;i < 5;i++)
	{
		offs.x += steepness * amplitude * steeps[i] * amps[i] * dir[i].x * cos(frequency * fs[i] * dot(sVertex.xz, dir[i]) + speeds[i] * frequency * fs[i] * _Time.y);
		offs.z += steepness * amplitude * steeps[i] * amps[i] * dir[i].y * cos(frequency * fs[i] * dot(sVertex.xz, dir[i]) + speeds[i] * frequency * fs[i] * _Time.y);
		offs.y += amplitude * amps[i] * sin(frequency * fs[i] * dot(sVertex.xz, dir[i].xy) + speeds[i] * frequency * fs[i] * _Time.y);
	}

	offsets = offs;

	normal = half3(0, 1, 0);
	//normal.x -= directionAB.x * frequency * amplitude * cos(frequency * dot(directionAB.xy, offs.xz) + speed * frequency * _Time.y);
	//normal.z -= directionAB.y * frequency * amplitude * cos(frequency * dot(directionAB.xy, offs.xz) + speed * frequency * _Time.y);
	//normal.y += 1 - offs.y;
}

inline void Wave(out half3 offsets, out half3 normal, half3 vertex, half4 sVertex, half amplitude, half frequency, half s)
{
	float4 v0 = sVertex;
	float4 v1 = v0 + float4(0.05,0,0,0);
	float4 v2 = v0 + float4(0,0,0.05,0);

	float speed = s * _Time.y;
	amplitude *= 0.01;

	v0.y += sin ( speed + (v0.x * frequency )) * amplitude;
	v1.y += sin ( speed + (v1.x * frequency )) * amplitude;
	v2.y += sin ( speed + (v2.x * frequency )) * amplitude;

	v0.y -= cos ( speed + (v0.z * frequency )) * amplitude;
	v1.y -= cos ( speed + (v1.z * frequency )) * amplitude;
	v2.y -= cos ( speed + (v2.z * frequency )) * amplitude;

	v1.y -= (v1.y - v0.y) * (1 - _Smoothing);
	v2.y -= (v2.y - v0.y) * (1 - _Smoothing);

	float3 vna = cross(v2 - v0,v1 - v0);

	float4 vn = mul(float4x4(unity_WorldToObject), float4(vna, 0) );
	normal = normalize(vn).xyz;
	offsets = mul(float4x4(unity_WorldToObject), v0).xyz;
}

inline void Displacement(inout VertexInput v)
{
	half4 worldPos = mul(unity_ObjectToWorld, v.vertex);
	half3 offsets;
	half3 normal;

	#if _DISPLACEMENTMODE_WAVE

	Wave(offsets, normal, v.vertex.xyz, worldPos, _Amplitude, _Frequency, _Speed);
	v.vertex.y += offsets.y;
	v.normal = normal;

	#endif

	#if _DISPLACEMENTMODE_GERSTNER

	half3 squashedVertex = worldPos.xzz;
	Gerstner(offsets, normal, v.vertex.xyz, squashedVertex, 
		_Amplitude * 0.01, _Frequency, _Steepness,
		_WSpeed, _WDirectionAB, _WDirectionCD
	);

	v.vertex.xyz += offsets;
	v.normal = normal;

	#endif
}

inline half4 AnimateBump(half2 uv)
{
	half4 coords;

	coords.xy = TRANSFORM_TEX(uv, _BumpTex);
	coords.zw = TRANSFORM_TEX(uv, _BumpTex) * 0.5;

	coords.x += frac(_BumpSpeedX * _Time.x);
	coords.y += frac(_BumpSpeedY * _Time.x);
	coords.z -= frac(_BumpSpeedX * 0.5 * _Time.x);
	coords.w -= frac(_BumpSpeedY * 0.5 * _Time.x);

	return coords;
}

inline half3 UnpackNormalBlend(half4 normal1, half4 normal2, half scale)
{
	half3 normal = normalize((normal1.xyz * 2 - 1) + (normal2.xyz * 2 - 1));
	normal.xy *= scale;
	normal.xy = (normal1.wy * 2 - 1) + (normal2.wy * 2 - 1);
	normal.xy *= scale;
	normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
	return normalize(normal);
}

#endif
