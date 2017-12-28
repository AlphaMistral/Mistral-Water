// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

#ifndef LPW_LITE_INCLUDED
#define LPW_LITE_INCLUDED

#include "UnityStandardUtils.cginc"
#include "UnityLightingCommon.cginc"

#define COMPUTESCREENPOS ComputeScreenPos
#if UNITY_VERSION < 540
	#define UNITY_VERTEX_INPUT_INSTANCE_ID
	#define UNITY_VERTEX_OUTPUT_STEREO
	#define UNITY_SETUP_INSTANCE_ID(v)
	#define UNITY_TRANSFER_INSTANCE_ID(v,o)
	#define UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o)
#endif

#if defined(LPWVERTEXLM) || (SHADER_TARGET < 30)
    #define LPW_AMBIENT(normal) UNITY_LIGHTMODEL_AMBIENT.rgb
    #define LIGHTCOLOR half3(1,1,1)
#else
    #define LPW_AMBIENT(normal) max(0.0, ShadeSH9(half4(normal, 1.0)))
    #define LIGHTCOLOR _LightColor0.rgb
#endif

struct appdata {
	float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f {
	float4 pos : SV_POSITION;
	UNITY_FOG_COORDS(0)
	#ifdef _SHADING_PIXELLIT
		float3 worldPos : TEXCOORD1;
		half3 worldNormal : TEXCOORD2;
	#else
		fixed4 vertexLight : TEXCOORD1;
	#endif
	#ifdef _EDGEBLEND_ON
		float4 screenPos : TEXCOORD3;
	#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

float _RSpeed_, _RHeight_, _Opacity, _TexSize_, _Speed_;
half _Gloss, _Specular, _Smoothness;
sampler2D _NoiseTex, _FresnelTex; 
fixed4 _Color;

#ifdef _EDGEBLEND_ON
	sampler2D_float _CameraDepthTexture;
	float _ShoreIntensity, _ShoreDistance;
	fixed4 _ShoreColor;
#endif

#if !defined(_WAVES_OFF) || (SHADER_TARGET < 30) || defined(SHADER_API_GLES)
	float _Height, _Length, _Stretch;
	float4 _Direction_; //cos, sin, cos*steepness, sin*steepness

	#if defined(_WAVES_HIGHQUALITY) || (SHADER_TARGET < 30) || defined(SHADER_API_GLES)
		float _Scale_;

		inline float hash( float n ){
		    return frac(sin(n)*43758.5453);
		}

		inline float noise( float2 x ){
			x /= _Scale_;
		    float2 p = floor(x);
		    float2 f = frac(x);
		    #if (SHADER_TARGET >= 30) 
				f = f*f*(3.0-2.0*f);//f = smoothstep(0.0, 1.0, f); 
			#endif
		    float n = p.x*57.0 + p.y;
		    return lerp(lerp( hash(n), hash(n+1.0),f.y), lerp( hash(n+57.0), hash(n+58.0),f.y),f.x) -0.5;
		}
	#else
		inline float noise(float2 uv){
		    return smoothstep(0,1,tex2Dlod(_NoiseTex, float4(uv/_TexSize_, 0,0)).a)-0.5;
		}
	#endif

	inline void gerstner(inout float3 p, float phase){
		float x = p.x*_Direction_.x - p.z*_Direction_.y;
		float z = p.z*_Direction_.x + p.x*_Direction_.y;
		float n = noise(float2(x/_Stretch, z/_Length + phase));
		p.y += _Height*n;
		p.xz -= n*_Direction_.wz;
	}
#endif

inline float ripple(float2 p, float phase){
	float2 uv = float2(p.x, phase+p.y);

	#if (SHADER_TARGET < 30) || defined(SHADER_API_GLES)
		return noise(uv)*_RHeight_;
	#else
    	return (tex2Dlod(_NoiseTex, float4(uv/_TexSize_, 0,0)).a-0.5)*_RHeight_;
    #endif
}

inline half4 lighting(half3 normal, half3 worldPos){
	float3 lightDir = _WorldSpaceLightPos0.xyz; // float, else you get artefacts with pixellit
	half3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));

	//diffuse
	half3 diff = _Color*max(0.0, dot (normal, lightDir));

	//ambient
	half3 ambient = LPW_AMBIENT(normal);

	//fresnel
	half dn = max (0.0, dot( worldViewDir, normal ));
	#if (SHADER_TARGET < 30) || defined(SHADER_API_GLES)
		half fresPower = 1-dn;
		fresPower *= fresPower;
	#else
		half fresPower = tex2Dlod(_FresnelTex, half4(dn,dn,0,0) ).a;
	#endif
	half3 fres = ambient * fresPower;
	fres = lerp(diff, fres, _Smoothness);

	//specular
	half3 h = normalize (lightDir + worldViewDir);
	half nh = max (0.0, dot (normal, h));
	half specPower = pow (nh, _Specular*128.0) * _Gloss;
	half3 spec = _SpecColor.rgb*specPower;

	return fixed4(
		/*rgb:  */_Color*ambient + LIGHTCOLOR * (fres + spec),
		/*alpha:*/_Opacity*(1.0+0.2*fresPower+specPower));
}

v2f vert (appdata v) {
  	UNITY_SETUP_INSTANCE_ID(v);
	v2f o;
	UNITY_INITIALIZE_OUTPUT(v2f,o);
		UNITY_TRANSFER_INSTANCE_ID(v,o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float4 pos0 = mul(unity_ObjectToWorld, v.vertex);

	// decode positions
	float4 offs = float4(floor(v.uv),frac(v.uv)) * float4(1.0/10000.0, 1.0/10000.0, 10.0, 10.0) - 5.0;
	float4 p = v.vertex;
	p.xz -= offs.xz;
	float3 pos1 = mul(unity_ObjectToWorld, p).xyz;
	p.xz = v.vertex.xz-offs.yw;
	float3 pos2 = mul(unity_ObjectToWorld, p).xyz;

	// ripples
	float phase = _Time[1]*_RSpeed_;
	pos0.y += ripple(pos0.xz, phase);
	pos1.y += ripple(pos1.xz, phase);
	pos2.y += ripple(pos2.xz, phase);

	// waves
	#ifndef _WAVES_OFF
		phase = _Time[1]*_Speed_;
		gerstner(pos0.xyz, phase);
		gerstner(pos1, phase);
		gerstner(pos2, phase);
	#endif

	half3 worldNormal = cross(pos1-pos0.xyz, pos2-pos0.xyz);
	worldNormal = normalize(worldNormal);

	#ifdef _SHADING_PIXELLIT
		o.worldNormal = worldNormal;
		o.worldPos = pos0.xyz;
	#elif _SHADING_VERTEXLIT
		o.vertexLight = lighting(worldNormal, pos0.xyz);
	#else // flat shading
		o.vertexLight = lighting(worldNormal, (pos0.xyz+pos1+pos2)/3.0);
	#endif

	o.pos = mul(UNITY_MATRIX_VP, pos0);

	#ifdef _EDGEBLEND_ON
		o.screenPos = COMPUTESCREENPOS(o.pos);
		o.screenPos.z = lerp(o.pos.w, mul(UNITY_MATRIX_V, pos0).z, unity_OrthoParams.w);
	#endif

	UNITY_TRANSFER_FOG(o,o.pos); // pass fog coordinates to pixel shader
	return o;
}


fixed4 frag (v2f i) : COLOR {
	UNITY_SETUP_INSTANCE_ID(i);

	#ifdef _SHADING_PIXELLIT
		fixed4 c = lighting(i.worldNormal, i.worldPos);
	#else
		fixed4 c = i.vertexLight;
	#endif

	#ifdef _EDGEBLEND_ON
		float sceneZ = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos));
		float perpectiveZ = LinearEyeDepth(sceneZ);
		#if defined(UNITY_REVERSED_Z)
			sceneZ = 1-sceneZ;
		#endif
		float orthoZ = sceneZ*(_ProjectionParams.y - _ProjectionParams.z) - _ProjectionParams.y;

		sceneZ = lerp(perpectiveZ, orthoZ, unity_OrthoParams.w);

		float diff = abs(sceneZ - i.screenPos.z)/_ShoreDistance;
		diff = smoothstep(_ShoreIntensity , 1 , diff);
        c = lerp(lerp(c, _ShoreColor, _ShoreColor.a), c, diff);
	#endif

	UNITY_APPLY_FOG(i.fogCoord, c); // apply fog
	return c;
}

#endif // LPW_LITE_INCLUDED