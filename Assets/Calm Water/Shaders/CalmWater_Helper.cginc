// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

#ifndef CALMWATER_HELPER_INCLUDED
#define CALMWATER_HELPER_INCLUDED

sampler2D _GrabTexture;
sampler2D_float _CameraDepthTexture; //Fix for depth precision
sampler2D _ReflectionTex;

sampler2D _MainTex;
sampler2D _BumpMap;
half4 _BumpMap_ST;

sampler2D _FoamTex;
half4 _FoamTex_ST;
samplerCUBE _Cube;

fixed4 _Color;

#ifndef LIGHTING_INCLUDED
fixed3 _SpecColor;
uniform fixed4 _LightColor0;
#endif

fixed4 _DepthColor;
fixed4 _CubeColor;
fixed3 _FoamColor;

half4 _GrabTexture_TexelSize;

float _EdgeFade;
float _BumpStrength;
float _SpeedX;
float _SpeedY;
float _Depth;
float _Distortion;
float _Reflection;
float _RimPower;
float _FoamSize;

float _Amplitude;
float _Frequency;
float _Speed;
uniform float _Steepness;
uniform float4 _WSpeed;
uniform float4 _WDirectionAB;
uniform float4 _WDirectionCD;

half _Smoothness;
float _Tess;

#ifndef LIGHTCOLOR
#define LIGHTCOLOR

#endif
float _Smoothing;


// =====================================
// Lighting
// =====================================

// NOTE: some intricacy in shader compiler on some GLES2.0 platforms (iOS) needs 'viewDir' & 'h'
// to be mediump instead of lowp, otherwise specular highlight becomes too bright.

// Lighting Terms ===============================================================================
half DiffuseTerm (half3 normalDir,half3 lightDir){
	return max (0, dot(normalDir,lightDir));
}

half NdotVTerm(half3 normalDir,half3 viewDir){
	return dot(normalDir,viewDir);
}

half SpecularTerm (half3 lightDir,half3 viewDir,half3 normalDir){
	return dot(reflect(-lightDir, normalDir), viewDir);
}

half3 SpecularColor (half gloss, half3 lightDir,half3 viewDir,half3 normalDir){
	float spec  = pow(max(0.0, SpecularTerm (lightDir,viewDir,normalDir) ), gloss * 128.0);
	return _LightColor0.rgb * spec * _SpecColor.rgb;
}

// Helpers ======================================================================================

inline float4 AnimateBump(float2 uv){

	float4 coords;

	coords.xy = TRANSFORM_TEX(uv,_BumpMap);
	coords.zw = TRANSFORM_TEX(uv,_BumpMap) * 0.5;

	coords.x += frac(_SpeedX * _Time.x);
	coords.y += frac(_SpeedY * _Time.x);
	coords.z -= frac(_SpeedX * 0.5 * _Time.x);
	coords.w -= frac(_SpeedY * 0.5 * _Time.x);

	return coords;
}

inline half3 SafeNormalize(half3 inVec)
{
	half dp3 = max(0.001f, dot(inVec, inVec));
	return inVec * rsqrt(dp3);
}


inline float4 OffsetUV(float4 uv, float2 offset){	
	#ifdef UNITY_Z_0_FAR_FROM_CLIPSPACE
		uv.xy = offset * UNITY_Z_0_FAR_FROM_CLIPSPACE(uv.z) + uv.xy;
	#else
		uv.xy = offset * uv.z + uv.xy;
	#endif

	return uv;
}

inline float4 OffsetDepth(float4 uv, float2 offset){	
	uv.xy = offset * uv.z + uv.xy;
	return uv;
}

inline float texDepth (sampler2D_float _Depth, float4 uv){
	return LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_Depth, UNITY_PROJ_COORD(uv)));
}

inline half3 WorldNormal(half3 t0,half3 t1, half3 t2, half3 bump){
	return normalize( half3( dot(t0, bump) , dot(t1, bump) , dot(t2, bump) ) );
}

//==========================================================================================================
// Rim
//==========================================================================================================
inline fixed RimLight (half3 vDir,fixed3 n,fixed rimPower){
	return pow(1.0 - saturate(dot(SafeNormalize(vDir),n)),rimPower);
}
//==========================================================================================================
// UnpackNormals blend and scale
//==========================================================================================================
half3 UnpackNormalBlend ( half4 n1, half4 n2, half scale){
#if defined(UNITY_NO_DXT5nm)
	half3 normal = normalize((n1.xyz * 2 - 1) + (n2.xyz * 2 - 1));
	#if (SHADER_TARGET >= 30)
	normal.xy *= scale;
	#endif
	return normal;
#else
	half3 normal;
	normal.xy = (n1.wy * 2 - 1) + (n2.wy * 2 - 1);
	#if (SHADER_TARGET >= 30)
		normal.xy *= scale;
	#endif
	normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
	return normalize(normal);
#endif
}

// =======================================================
// Displacement
// =======================================================

void Wave (out half3 offs, out half3 nrml, half3 vtx, half4 tileableVtx,half amplitude ,half frequency,half s){

	float4 v0 = tileableVtx;
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

	float3 vna = cross(v2-v0,v1-v0);

	float4 vn 	= mul(float4x4(unity_WorldToObject), float4(vna,0) );
	nrml 		= normalize (vn).xyz;
	offs 		= mul(float4x4(unity_WorldToObject),v0).xyz;
}

half3 GerstnerNormal (half2 xzVtx, half4 amp, half4 freq, half4 speed, half4 dirAB, half4 dirCD) 
{
	half3 nrml = half3(0,2.0,0);

	half4 AB = freq.xxyy * amp.xxyy * dirAB.xyzw;
	half4 CD = freq.zzww * amp.zzww * dirCD.xyzw;
	
	half4 dotABCD = freq.xyzw * half4(dot(dirAB.xy, xzVtx), dot(dirAB.zw, xzVtx), dot(dirCD.xy, xzVtx), dot(dirCD.zw, xzVtx));
	half4 TIME = _Time.yyyy * speed;
	
	half4 COS = cos (dotABCD + TIME);
	
	nrml.x -= dot(COS, half4(AB.xz, CD.xz));
	nrml.z -= dot(COS, half4(AB.yw, CD.yw));
	
	nrml.xz *= _Smoothing;
	nrml = normalize (nrml);
	nrml = half3(0, 1, 0);
	return nrml;			
}	

half3 GerstnerOffset (half2 xzVtx, half steepness, half4 amp, half4 freq, half4 speed, half4 dirAB, half4 dirCD) 
{
	half3 offsets;
	
	half4 AB = steepness * amp.xxyy * dirAB.xyzw;
	half4 CD = steepness * amp.zzww * dirCD.xyzw;
	
	half4 dotABCD = freq.xyzw * half4(dot(dirAB.xy, xzVtx), dot(dirAB.zw, xzVtx), dot(dirCD.xy, xzVtx), dot(dirCD.zw, xzVtx));
	half4 TIME = _Time.yyyy * speed;
	
	half4 COS = cos (dotABCD + TIME);
	half4 SIN = sin (dotABCD + TIME);
	
	offsets.x = dot(COS, half4(AB.xz, CD.xz));
	offsets.z = dot(COS, half4(AB.yw, CD.yw));
	offsets.y = dot(SIN, amp);

	return offsets;			
}	

void Gerstner (	out half3 offs, out half3 nrml,
				 half3 vtx, half3 tileableVtx, 
				 half4 amplitude, half4 frequency, half4 steepness, 
				 half4 speed, half4 directionAB, half4 directionCD) 
{

		offs = GerstnerOffset(tileableVtx.xz, steepness, amplitude, frequency, speed, directionAB, directionCD);
		nrml = GerstnerNormal(tileableVtx.xz + offs.xz, amplitude, frequency, speed, directionAB, directionCD);							
}

#endif
