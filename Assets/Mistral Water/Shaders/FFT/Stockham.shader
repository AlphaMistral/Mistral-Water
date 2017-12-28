Shader "Hidden/Mistral Water/Helper/Vertex/Stockham"
{
	Properties
	{
		_Input ("Input Sampler", 2D) = "black" {}
		_TransformSize ("Transform Size", Float) = 256
		_SubTransformSize ("Log Size", Float) = 8
	}

	SubShader
	{
		Pass
		{
			Cull Off
			ZWrite Off
			ZTest Off
			ColorMask RGBA

			CGPROGRAM

			#include "FFTCommon.cginc"

			#pragma vertex vert_quad
			#pragma fragment frag
			#pragma multi_compile _HORIZONTAL _VERTICAL

			uniform sampler2D _Input;
			uniform float _TransformSize;
			uniform float _SubTransformSize;

			float4 frag(FFTVertexOutput i) : SV_TARGET
			{
				float index;

				#ifdef _HORIZONTAL
					index = i.texcoord.x * _TransformSize - 0.5;
				#else
					index = i.texcoord.y * _TransformSize - 0.5;
				#endif

				float evenIndex = floor(index / _SubTransformSize) * (_SubTransformSize * 0.5) + fmod(index, _SubTransformSize * 0.5) + 0.5;

				#ifdef _HORIZONTAL
					float4 even = tex2D(_Input, float2((evenIndex), i.texcoord.y * _TransformSize) / _TransformSize);
					float4 odd  = tex2D(_Input, float2((evenIndex + _TransformSize * 0.5), i.texcoord.y * _TransformSize) / _TransformSize);
				#else
					float4 even = tex2D(_Input, float2(i.texcoord.x * _TransformSize, (evenIndex)) / _TransformSize);
					float4 odd  = tex2D(_Input, float2(i.texcoord.x * _TransformSize, (evenIndex + _TransformSize * 0.5)) / _TransformSize);
				#endif

				float twiddleV = GetTwiddle(index / _SubTransformSize);
				float2 twiddle = float2(cos(twiddleV), sin(twiddleV));
				float2 outputA = even.xy + MultComplex(twiddle, odd.xy);
				float2 outputB = even.zw + MultComplex(twiddle, odd.zw);

				return float4(outputA, outputB);
			}

			ENDCG
		}
	}
}
