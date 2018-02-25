#ifndef MISTRAL_WATER_COMMON_INCLUDED
#define MISTRAL_WATER_COMMON_INCLUDED

#include "Tessellation.cginc"
#include "MistralWaterLib.cginc"

VertexOutput vert(VertexInput v)
{
	VertexOutput o;

	UNITY_INITIALIZE_OUTPUT(VertexOutput, o);

	#if !_DISPLACEMENTMODE_OFF

		Displacement(v);

	#endif

	v.vertex.y += tex2Dlod(_Height, float4(v.texcoord, 0, 0)).r / 8;
	v.vertex.xz += tex2Dlod(_Anim, float4(v.texcoord, 0, 0)).rb / 8;

	o.pos = UnityObjectToClipPos(v.vertex);
	o.grabUV = ComputeGrabScreenPos(o.pos);
	o.screenPos = ComputeScreenPos(o.pos);
	COMPUTE_EYEDEPTH(o.screenPos.z);

	o.animUV = AnimateBump(v.texcoord);

	#if _FOAM_ON

		o.foamUV = TRANSFORM_TEX(v.texcoord, _FoamTex);

	#endif

	float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
	half3 worldNormal = UnityObjectToWorldNormal(tex2Dlod(_Bump, float4(v.texcoord, 0, 0)).rgb);
	half3 worldTangent = UnityObjectToWorldDir(v.tangent);
	half3 worldBitangent = cross(worldTangent, worldNormal);

	o.tSpace0 = half4(worldTangent.x, worldBitangent.x, worldNormal.x, worldPos.x);
	o.tSpace1 = half4(worldTangent.y, worldBitangent.y, worldNormal.y, worldPos.y);
	o.tSpace2 = half4(worldTangent.z, worldBitangent.z, worldNormal.z, worldPos.z);
	o.worldPos = worldPos;

	o.ambient = ShadeSH9(half4(worldNormal, 1));

	o.color = v.color / 1.2;

	//o.color = tex2Dlod(_White, float4(v.texcoord, 0, 0)).r;

	#ifdef UNITY_PASS_FORWARDBASE

		UNITY_TRANSFER_FOG(o, o.pos);

	#endif

	return o;
}

fixed4 frag(VertexOutput i) : SV_TARGET
{
	half4 normal1 = tex2D(_BumpTex, i.animUV.xy);
	half4 normal2 = tex2D(_BumpTex, i.animUV.zw);
	half3 normal = UnpackNormalBlend(normal1, normal2, _BumpScale);

	half3 worldNormal = normalize(half3(dot(i.tSpace0.xyz, normal), dot(i.tSpace1.xyz, normal), dot(i.tSpace2.xyz, normal)));
	half3 viewDirection = normalize(UnityWorldSpaceViewDir(i.worldPos));

	#ifdef CULL_FRONT

		worldViewDir = -worldViewDir;

	#endif

	#ifndef USING_DIRECTIONAL_LIGHT

		half3 lightDirection = normalize(UnityWorldSpaceLightDir(i.worldPos));

	#else

		half3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);

	#endif

	half2 offset = worldNormal.xz * _GrabTexture_TexelSize.xy * _Distortion;
	half4 screenUV = half4(offset * i.screenPos.z + i.screenPos.xy, i.screenPos.zw);

	#ifdef UNITY_Z_0_FAR_FROM_CLIPSPACE

		half4 grabUV = half4(offset * UNITY_Z_0_FAR_FROM_CLIPSPACE(i.grabUV.z) + i.grabUV.xy, i.grabUV.zw);

	#else

		half4 grabUV = half4(offset * i.grabUV.z + i.graUV.xy, i.grabUV.zw);

	#endif

	fixed4 refraction = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(grabUV));
	fixed4 cleanRefraction = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(i.grabUV));

	float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
	float waterDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(screenUV)));

	refraction.a = saturate(_DepthAmount / abs(waterDepth - screenUV.z));
	cleanRefraction.a = saturate(_DepthAmount / abs(sceneDepth - i.screenPos.z));

	#ifndef CULL_FRONT

		refraction = lerp(cleanRefraction, refraction, step(screenUV.z, waterDepth));

	#endif

	#ifndef CULL_FRONT

		#if _DEPTHFOG_ON

			fixed3 finalColor = lerp(_ShallowColor.rgb * refraction.rgb, _DeepColor.rgb, 1 - refraction.a);

		#else

			fixed3 finalColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, 1 - refraction.a) * refraction.rgb;

		#endif

	#else

		fixed3 finalColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, 0.5) * refraction.rgb;

	#endif

	#ifndef CULL_FRONT

		#if _REFLECTIONTYPE_CUBEMAP

			half3 worldReflect = reflect(-viewDirection, worldNormal);
			fixed3 cubeMap = texCUBE(_CubeMap, worldReflect).rgb * _CubeTint.rgb;

		#endif

		#if _REFLECTIONTYPE_REALTIME

			fixed3 rtReflection = tex2Dproj(_ReflectionTex, UNITY_PROJ_COORD(screenUV)) * _ReflectionStrength;

		#endif

		#if _REFLECTIONTYPE_REALTIME

			fixed3 finalReflection = rtReflection;

		#endif

		#if _REFLECTIONTYPE_CUBEMAP

			fixed3 finalReflection = cubeMap;

		#endif


	#endif

	#if _FOAM_ON

		float2 foamUV = i.foamUV;

		fixed foamTex = tex2D(_FoamTex, foamUV + normal.xy * 0.02).r;

		fixed3 foam = 1 - saturate(_FoamSize * (sceneDepth - screenUV.z));
		foam *= _FoamColor.rgb * foamTex * max(_LightColor0.rgb, i.ambient.rgb);

		finalColor += min(1, 2 * foam);

	#endif

	fixed NdotL = saturate(dot(worldNormal, lightDirection));

	#ifndef CULL_FRONT

		fixed NdotV = saturate(1 - dot(worldNormal, viewDirection));
		half fresnel = pow(NdotV, _FresnelAngle);
		finalColor = lerp(finalColor, finalReflection + finalColor, fresnel * _ReflectionStrength);

	#endif

	fixed3 specularColor = pow(saturate(dot(reflect(-lightDirection, worldNormal), viewDirection)), _Smoothness * 128) * _SpecularColor.rgb * _LightColor0.rgb;
	fixed3 color = finalColor * max(i.ambient, NdotL * _LightColor0.rgb) + specularColor;
	fixed alpha = saturate(_EdgeFade * (sceneDepth - screenUV.z)) * _ShallowColor.a;

	#ifndef UNITY_PASS_FORWARDADD

		color.rgb = lerp(cleanRefraction.rgb, color, alpha);
		UNITY_APPLY_FOG(i.fogCoord, color);

	#else

		color.rgb = diffuse * alpha * 2;

	#endif

	return fixed4(color + pow(saturate(i.color.rgb), 1.6), 1);
}

#ifdef UNITY_CAN_COMPILE_TESSELLATION
	struct TessVertex 
	{
		float4 vertex 	: INTERNALTESSPOS;
		float3 normal 	: NORMAL;
		float4 tangent 	: TANGENT;
		float2 texcoord : TEXCOORD0;
		float4 color : COLOR;
		#ifdef UNITY_PASS_FORWARDADD
		float2 texcoord1 : TEXCOORD1;
		#endif
		//float4 color 	: COLOR;
	};

	struct OutputPatchConstant 
	{
		float edge[3]         : SV_TessFactor;
		float inside          : SV_InsideTessFactor;
	};

	TessVertex tessvert (VertexInput v) 
	{
		TessVertex o;
		o.vertex 	= v.vertex;
		o.normal 	= v.normal;
		o.tangent 	= v.tangent;
		o.texcoord 	= v.texcoord;
		o.color = v.color;
		#ifdef UNITY_PASS_FORWARDADD
		o.texcoord1	= v.texcoord1;
		#endif
		//o.color 	= v.color;
		return o;
	}

    float4 Tessellation(TessVertex v, TessVertex v1, TessVertex v2)
    {
        return UnityEdgeLengthBasedTess(v.vertex, v1.vertex, v2.vertex, 32 - 1);
    }

    OutputPatchConstant hullconst (InputPatch<TessVertex,3> v) 
    {
        OutputPatchConstant o;
        float4 ts = Tessellation( v[0], v[1], v[2] );
        o.edge[0] = ts.x;
        o.edge[1] = ts.y;
        o.edge[2] = ts.z;
        o.inside = ts.w;
        return o;
    }

    [domain("tri")]
    [partitioning("fractional_odd")]
    [outputtopology("triangle_cw")]
    [patchconstantfunc("hullconst")]
    [outputcontrolpoints(3)]
    TessVertex hs_surf (InputPatch<TessVertex,3> v, uint id : SV_OutputControlPointID) 
    {
        return v[id];
    }

    [domain("tri")]
    VertexOutput ds_surf (OutputPatchConstant tessFactors, const OutputPatch<TessVertex,3> vi, float3 bary : SV_DomainLocation) 
    {
        VertexInput v = (VertexInput)0;

        v.vertex 	= vi[0].vertex*bary.x 	+ vi[1].vertex*bary.y 	+ vi[2].vertex*bary.z;
        v.texcoord 	= vi[0].texcoord*bary.x + vi[1].texcoord*bary.y + vi[2].texcoord*bary.z;
        #ifdef UNITY_PASS_FORWARDADD
        v.texcoord1 = vi[0].texcoord1*bary.x + vi[1].texcoord1*bary.y + vi[2].texcoord1*bary.z;
        #endif
        //v.color 	= vi[0].color*bary.x 	+ vi[1].color*bary.y 	+ vi[2].color*bary.z;
        v.tangent 	= vi[0].tangent*bary.x 	+ vi[1].tangent*bary.y 	+ vi[2].tangent*bary.z;
        v.normal 	= vi[0].normal*bary.x 	+ vi[1].normal*bary.y  	+ vi[2].normal*bary.z;
        v.color 	= vi[0].color*bary.x 	+ vi[1].color*bary.y  	+ vi[2].color*bary.z;

        VertexOutput o = vert(v);

        return o;
    }

#endif

#endif
