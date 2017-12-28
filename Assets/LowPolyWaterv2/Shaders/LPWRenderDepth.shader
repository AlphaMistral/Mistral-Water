Shader "Hidden/LPWRenderDepth" {

    SubShader {
        Tags { "RenderType"="Opaque" }
        Pass {
            ZWrite On
            Lighting Off
            ColorMask 0

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata {
              float3 pos : POSITION;
            };

            struct v2f {
              float4 pos : SV_POSITION;
            };

            v2f vert (appdata IN) {
              v2f o;
              o.pos = UnityObjectToClipPos(IN.pos);
              return o;
            }

            fixed4 frag (v2f IN) : SV_Target {
              return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }

    SubShader {
        Tags { "RenderType"="Transparent" }
        
        Pass {
            ZWrite Off
            Lighting Off
            ColorMask 0
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            float4 vert () : SV_POSITION {
              return float4(0,0,0,0);
            }

            fixed4 frag () : SV_Target {
              return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }

    SubShader {
        Tags { "RenderType"="TransparentCutout" }
        
        Pass {
            ZWrite Off
            Lighting Off
            ColorMask 0
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            float4 vert () : SV_POSITION {
              return float4(0,0,0,0);
            }

            fixed4 frag () : SV_Target {
              return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}