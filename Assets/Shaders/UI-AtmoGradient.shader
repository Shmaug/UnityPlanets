Shader "Unlit/AtmoGradient"
{
	Properties
	{
		
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" }
		Blend One OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			uniform float _Hr = .1332333;
			uniform float _Hm = .02;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};
			struct v2f
			{
				float4 vertex : SV_POSITION;
				float u : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.u = v.uv.x;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float c = saturate(exp(-i.u / _Hr) + exp(-i.u / _Hm));
				return float4(c, c, c, 1);
			}
			ENDCG
		}
	}
}
