Shader "Hidden/Mistral Water/Helper/Map/Ocean Normal"
{
	Properties
	{
		_Resolution ("Resolution", Float) = 512
		_Length ("Sea Width", Float) = 512
		_Displacement ("Displacement", 2D) = "black" {}
		_Bump ("_Bump", 2D) = "bump" {}
	}

	SubShader
	{
		Pass
		{
			Cull Off
			ZWrite Off
			ZTest Off
			ColorMask R

			CGPROGRAM

			#include "FFTCommon.cginc"

			#pragma vertex vert_quad
			#pragma fragment frag

			uniform float _Resolution;
			uniform float _Length;
			uniform sampler2D _Displacement;
			uniform float4 _Displacement_TexelSize;
			uniform sampler2D _Bump;

			float4 frag(FFTVertexOutput i) : SV_TARGET
			{
				float texelSize = 1 / _Length;
				float2 dDdy = -0.5 * (tex2D(_Displacement, i.texcoord + float4(0, -texelSize, 0, 0)).rb - tex2D(_Displacement, i.texcoord + float4(0, texelSize, 0, 0)).rb) / 8;
				float2 dDdx = -0.5 * (tex2D(_Displacement, i.texcoord + float4(-texelSize, 0, 0, 0)).rb - tex2D(_Displacement, i.texcoord + float4(texelSize, 0, 0, 0)).rb) / 8;
				float2 noise = 0.3 * tex2D(_Bump, i.texcoord).xz;
				float jacobian = (1 + dDdx.x) * (1 + dDdy.y) - dDdx.y * dDdy.x;
				float turb = max(0, 1 - jacobian + length(noise));
				float xx = 1 + 3 * smoothstep(1.2, 1.8, turb);
				xx = min(turb, 1);
				xx = smoothstep(0, 1, turb);
				return float4(xx, xx, xx, 1);
			}

			ENDCG
		}
	}
}