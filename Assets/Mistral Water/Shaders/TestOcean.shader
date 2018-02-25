Shader "Test/Ocean"
{
	Properties
	{
		_Anim ("fd", 2D) = "black" {}
		_Height ("gasf", 2D) = "black" {}
		_Bump ("hasdf",2D) = "bump" {}
		_White ("sdaf", 2D) = "black" {}
		_LightWrap ("asdf", Float) = 1 
		_Tint ("Tint", Color) = (0.5, 0.65, 0.75, 1)
		_SpecColor ("Specular Color", Color) = (1, 0.25, 0, 1)
		_Glossiness ("Glossiness", Float) = 64
		_RimColor ("Rim Color", Color) = (0, 0, 1, 1)
	}

	SubShader
	{
		Pass
		{
			Cull Back
			ZWrite On
			ZTest LEqual
			ColorMask RGB

			CGPROGRAM

			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag

			uniform sampler2D _Anim;
			uniform sampler2D _Height;
			uniform sampler2D _Bump;
			uniform sampler2D _White;

			uniform float4 _Tint;
			uniform float4 _SpecColor;
			uniform float _Glossiness;
			uniform float _LightWrap;
			uniform fixed4 _RimColor;

			uniform float4 _LightColor0;

			struct VertexInput
			{
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
			};

			struct VertexOutput
			{
				float4 pos : SV_POSITION;
				float3 normal : TEXCOORD1;
				float4 texcoord : TEXCOORD0;
				float4 color : TEXCOORD2;
				float3 lightDir : TEXCOORD3;
				float3 viewDir : TEXCOORD4;
			};

			VertexOutput vert(VertexInput v)
			{
				VertexOutput o;

				v.vertex.y += tex2Dlod(_Height, v.texcoord).r / 8;
				v.vertex.xz += tex2Dlod(_Anim, v.texcoord).rb / 8;

				o.pos = UnityObjectToClipPos(v.vertex);

				o.normal = UnityObjectToWorldNormal(tex2Dlod(_Bump, v.texcoord).rgb);

				o.color = tex2Dlod(_White, v.texcoord).r;

				o.lightDir = normalize(WorldSpaceLightDir(v.vertex));
				o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
				o.texcoord = v.texcoord;
				return o;
			}

			float4 frag(VertexOutput i) : COLOR
			{
				i.normal = tex2D(_Bump, i.texcoord).rgb;
				i.normal = normalize(i.normal);
				i.normal = UnityObjectToWorldNormal(i.normal);
				float4 diffuse = saturate(dot(i.normal, i.lightDir));
				diffuse = pow(saturate(diffuse * (1 - _LightWrap) + _LightWrap), 2 * _LightWrap + 1) * _Tint * _LightColor0;
				float3 H = normalize(i.viewDir + i.lightDir);
				float NdotH = saturate(dot(i.normal, H));
				float4 specular = _SpecColor * saturate(pow(NdotH, _Glossiness)) * _LightColor0;
				float4 rim = _RimColor * pow(max(0, 1 - dot(i.normal, i.viewDir)), 2);
				return diffuse + specular * 0 + pow(i.color / 2, 2) + rim;
			}

			ENDCG
		}
	}
}