/*
	This Shader helps render the initial spectrum, namely the htilde and htilde_conj in Tessendorf's paper. 
	The two complex numbers, four values would be output into the render target. 
	First the Phillips Spectrum is calculated. 
	Later it will be applied to the htilde and its conjugate. 
*/
Shader "Hidden/Mistral Water/Helper/Vertex/Initial Spectrum"
{
	Properties
	{
		_RandomSeed1 ("Random Seed 1", Float) = 1.5122
		_RandomSeed2 ("Random Seed 2", Float) = 6.1152
		_Amplitude ("Phillips Ampitude", Float) = 1
		_Length ("Wave Length", Float) = 256
		_Resolution ("Ocean Resolution", int) = 256
		_Wind ("Wind Direction (XY)", Vector) = (1, 1, 0, 0)
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

			uniform float _RandomSeed1;
			uniform float _RandomSeed2;
			uniform float _Amplitude;
			uniform float _Length;
			uniform int _Resolution;
			uniform float2 _Wind;

			float4 frag(FFTVertexOutput i) : SV_TARGET
			{
				float n = (i.texcoord.x * _Resolution);
				float m = (i.texcoord.y * _Resolution);

				float phi1 = Phillips(n, m, _Amplitude, _Wind, _Resolution, _Length);
				float phi2 = Phillips(_Resolution - n, _Resolution - m, _Amplitude, _Wind, _Resolution, _Length);

				float2 h0 = hTilde0(i.texcoord.xy, _RandomSeed1 / 2, _RandomSeed2 * 2, phi1);
				float2 h0conj = Conj(hTilde0(i.texcoord.xy, _RandomSeed1, _RandomSeed2, phi2));

				return float4(h0, h0conj);
			}

			ENDCG
		}
	}
}
