Shader "Hidden/TextureCombine"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			#define R 1
			#define G 2
			#define B 4
			#define A 8
			
			uniform sampler2D _MainTex;

			uniform sampler2D _Tex0;
			uniform int _Tex0Invert;
			uniform int _Tex0r;
			uniform int _Tex0g;
			uniform int _Tex0b;
			uniform int _Tex0a;

			uniform sampler2D _Tex1;
			uniform int _Tex1Invert;
			uniform int _Tex1r;
			uniform int _Tex1g;
			uniform int _Tex1b;
			uniform int _Tex1a;

			void mix(int channel, float value, inout float4 col){
				if (channel & R) col.r += value;
				if (channel & G) col.g += value;
				if (channel & B) col.b += value;
				if (channel & A) col.a += value;
			}

			float4 frag (v2f_img i) : SV_Target
			{
				float4 c0 = tex2D(_Tex0, i.uv);
				float4 c1 = tex2D(_Tex1, i.uv);

				if (_Tex0Invert) c0 = 1.0 - c0;
				if (_Tex1Invert) c1 = 1.0 - c1;

				float4 col = 0;

				mix(_Tex0r, c0.r, col);
				mix(_Tex0g, c0.g, col);
				mix(_Tex0b, c0.b, col);
				mix(_Tex0a, c0.a, col);

				mix(_Tex1r, c1.r, col);
				mix(_Tex1g, c1.g, col);
				mix(_Tex1b, c1.b, col);
				mix(_Tex1a, c1.a, col);

				return col;
			}
			ENDCG
		}
	}
}
