/*
	This is the helper common methods and definitions used in FFT calculation. 
*/

#include "UnityCG.cginc"

static float PI = 3.1415926536;
static float EPSILON = 0.0001;
static float G = 9.81f;

struct FFTVertexInput
{
	float4 vertex : POSITION;
	float4 texcoord : TEXCOORD0;
};

struct FFTVertexOutput
{
	float4 pos : SV_POSITION;
	float4 texcoord : TEXCOORD0;
	float4 screenPos : TEXCOORD1;
};

FFTVertexOutput vert_quad(FFTVertexInput v)
{
	FFTVertexOutput o;

	o.pos = UnityObjectToClipPos(v.vertex);

	o.texcoord = v.texcoord;

	o.screenPos = ComputeScreenPos(o.pos);

	return o;
}

inline float UVRandom(float2 uv, float salt, float random)
{
	uv += float2(salt, random);
	return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

inline float2 MultComplex(float2 a, float2 b)
{
	return float2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
}

inline float2 MultByI(float2 a)
{
	return float2(-a.y, a.x);
}

inline float2 Conj(float2 a)
{
	return float2(a.x, -a.y);
}

inline float2 GetWave(float n, float m, float len, float res)
{
	//return PI * float2(2 * n - res, 2 * m - res) / len;
	n -= 0.5;
	m -= 0.5;
	n = ((n < res * 0.5) ? n : n - res);
	m = ((m < res * 0.5) ? m : m - res);
	return 2 * PI * float2(n, m) / len;

}

inline float Phillips(float n, float m, float amp, float2 wind, float res, float len)
{
	float2 k = GetWave(n, m, len, res);
	float klen = length(k);
	float klen2 = klen * klen;
	float klen4 = klen2 * klen2;
	if(klen < EPSILON)
		return 0;
	float kDotW = dot(normalize(k), normalize(wind));
	float kDotW2 = kDotW * kDotW;
	float wlen = length(wind);
	float l = wlen * wlen / G;
	float l2 = l * l;
	float damping = 0.01;
	float L2 = l2 * damping * damping;
	return amp * exp(-1 / (klen2 * l2)) / klen4 * kDotW2 * exp(-klen2 * L2);
}

inline float2 hTilde0(float2 uv, float r1, float r2, float phi)
{
	float2 r;
	float rand1 = UVRandom(uv, 10.612, r1);
	float rand2 = UVRandom(uv, 11.899, r2);
	rand1 = clamp(rand1, 0.01, 1);
	rand2 = clamp(rand2, 0.01, 1);
	float x = sqrt(-2 * log(rand1));
	float y = 2 * PI * rand2;
	r.x = x * cos(y);
	r.y = x * sin(y);
	return r * sqrt(phi / 2); 
}

inline float GetDispersion(float oldPhase, float newPhase)
{
	return fmod(oldPhase + newPhase, 2 * PI);
}

inline float CalcDispersion(float n, float m, float len, float dt, float res)
{
	float2 wave = GetWave(n, m, len, res);
	float w = 2 * PI / len;
	float wlen = length(wave);
	//Don't know what km(370) is  .... 
	return sqrt(G * length(wave) * (1 + wlen * wlen / 370 / 370)) * dt;
	//return floor(sqrt(G * length(wave) / w)) * w * dt;
}

inline float GetTwiddle(float ratio)
{
	return -2 * PI * ratio;
}
