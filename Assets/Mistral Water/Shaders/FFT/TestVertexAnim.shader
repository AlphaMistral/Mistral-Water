Shader "f"
{
	Properties
	{
		_Anim ("asdf", 2D) = "black" {}
		_Bump ("asdff", 2D)= "bump" {}
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM

			#include "UnityCG.cginc"

			#pragma target 4.0
			#pragma vertex vert
			#pragma fragment frag

			uniform sampler2D _Anim;
			uniform sampler2D _Bump;

			struct vin
			{
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 texcoord : TEXCOORD0;
			};

			v2f vert(vin v)
			{
				v2f o;

				v.vertex.y += tex2Dlod(_Anim, v.texcoord).g / 8;
				v.vertex.xz += tex2Dlod(_Anim, v.texcoord).rb / 8;

				//v.vertex.xy += tex2Dlod(_Anim, v.texcoord).rb / 1000;

				o.pos = UnityObjectToClipPos(v.vertex);

				o.texcoord = v.texcoord;

				return o;
			}

			fixed4 frag(v2f i) : COLOR
			{
				//return float4(tex2D(_Anim, i.texcoord).rgb / 8, 1);
				return 1;
			}

			ENDCG
		}
	}
}