Shader "Hidden/Mistral Water/Helper/Vertex/Dispersion"
{
	Properties
	{
		_Length ("Wave Length", Float) = 256
		_Resolution ("Ocean Resolution", int) = 256
		_DeltaTime ("Delta Time", Float) = 0.016
		_Phase ("Last Phase", 2D) = "black" {}
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

			uniform float _DeltaTime;
			uniform float _Length;
			uniform int _Resolution;
			uniform sampler2D _Phase;

			float4 frag(FFTVertexOutput i) : SV_TARGET
			{
				float n = (i.texcoord.x * _Resolution);
				float m = (i.texcoord.y * _Resolution);

				float deltaPhase = CalcDispersion(n, m, _Length, _DeltaTime, _Resolution);
				float phase = tex2D(_Phase, i.texcoord).r;

				return float4(GetDispersion(phase, deltaPhase), 0, 0, 0);
			}

			ENDCG
		}
	}
}
