#ifndef LPW_REFLECTION_INCLUDED
#define LPW_REFLECTION_INCLUDED


sampler2D _ReflectionTex, _RefractionTex, _BumpTex;
float _Distortion, _Refraction, _BumpScale_, _BumpSpeed, _NormalOffset;


inline void LPWReflection(float4 screenPos, half3 worldNormal, float3 worldPos, inout LPWLightingInput li){

	#ifdef _DISTORT_ON
		float2 offFactor = lerp(
			float2(1.0, 							screenPos.w / 10.0), 
			float2(1.0/unity_OrthoParams.x,			1.0) , 
			unity_OrthoParams.w);
		float2 bumpUV = worldPos.xz/ _BumpScale_;
		half2 bump1 = UnpackNormal(tex2D( _BumpTex, bumpUV.xy+_BumpSpeed*_Time.xx  )).xy;
		half2 bump2 = UnpackNormal(tex2D( _BumpTex, bumpUV.xy+_BumpSpeed*float2(1-_Time[0], -_Time[0]) )).xy;
		half2 bump = (bump1 + bump2) * 0.5 * offFactor.y;
	#else
		float offFactor = lerp(_NormalOffset, _NormalOffset/unity_OrthoParams.x, unity_OrthoParams.w);
	#endif
	
	#if defined(WATER_REFLECTIVE)
		float4 uv1 = screenPos;
		#ifdef _DISTORT_ON
			uv1.xy += (worldNormal.xz*_NormalOffset + bump * _Distortion) * offFactor.x;
		#else
			uv1.xy += worldNormal.xz * offFactor.x;
		#endif
		li.refl = tex2Dproj( _ReflectionTex, UNITY_PROJ_COORD(uv1) ).rgb;
	#endif

	#if defined(WATER_REFRACTIVE)
		float4 uv2 = screenPos;
		#ifdef _DISTORT_ON
			uv2.xy -= (worldNormal.xz*_NormalOffset + bump * _Refraction) * offFactor.x;
		#else
			uv2.xy -= worldNormal.xz * offFactor.x;
		#endif
		li.refr = tex2Dproj( _RefractionTex, UNITY_PROJ_COORD(uv2) ).rgb;
	#endif
}


#endif // LPW_REFLECTION_INCLUDED