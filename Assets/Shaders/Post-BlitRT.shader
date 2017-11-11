Shader "Hidden/BlitRT"
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

			sampler2D _LastCameraDepthNormalsTexture;
			float4x4 _InvView;

			float4 frag (v2f_img i) : SV_Target
			{
				float3 normal;
				float depth;
				DecodeDepthNormal(tex2D(_LastCameraDepthNormalsTexture, i.uv), depth, normal);
				normal = mul((float3x3)_InvView, normal);

				return float4(normal * .5 + .5, 1);
			}
			ENDCG
		}
	}
}
