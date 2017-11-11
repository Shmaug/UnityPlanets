Shader "Unlit/Skybox"
{
	Properties
	{
		_Color("Color", Color) = (0,0,0,1)
	}
		SubShader
	{
		Tags{ "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
		Cull Off ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			uniform float4 _Color;

			struct appdata {
				float4 vertex : POSITION;
			};
			struct v2f {
				float4 vertex : SV_POSITION;
				float3 view : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.view = mul((float3x3)unity_ObjectToWorld, v.vertex.xyz);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float4 color = _Color;

				return color;
			}
			ENDCG
		}
	}
}
