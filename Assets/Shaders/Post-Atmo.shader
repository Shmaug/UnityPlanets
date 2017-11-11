Shader "Hidden/PostAtmo"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Far ("Far", Float) = 1.0
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
			#include "Scatter.cginc"
			
			uniform sampler2D _MainTex;
			uniform sampler2D _CameraDepthTexture;
			uniform sampler2D _NoiseTex;
			uniform float4 _MainTex_TexelSize;

			uniform float4x4 _ClipToWorld;
			uniform float3 _CameraPos;
			uniform float3 _SunDir;
			uniform float _SunPower;
			uniform float _Far;
			uniform float _SkyScatter;
			
			// Calculates the Mie phase function
			half getMiePhase(half eyeCos, half eyeCos2, float g) {
				const float _SunSize = .02;

				float g2 = g * g;
				half temp = 1.0 + g2 - 2.0 * g * eyeCos;
				temp = pow(temp, pow(_SunSize, 0.65) * 10);
				temp = max(temp, 1.0e-4);
				temp = 1.5 * ((1.0 - g2) / (2.0 + g2)) * (1.0 + eyeCos2) / temp;
				return temp;
			}

			float4 frag (v2f_img i) : SV_Target
			{
				// calculate world space ray
                float3 cameraRightW = mul((float3x3)unity_CameraToWorld, float3(1, 0, 0));
                float3 cameraUpW    = mul((float3x3)unity_CameraToWorld, float3(0, 1, 0));
                float3 cameraFwdW   = mul((float3x3)unity_CameraToWorld, float3(0, 0, 1));
				
                float scrW = _ScreenParams.x;
                float scrH = _ScreenParams.y;
				float h = 2.0f / unity_CameraProjection._m11;
				float w = h * scrW / scrH;
                float3 rd = cameraFwdW + (i.uv.x - 0.5f) * w * cameraRightW + (i.uv.y - 0.5f) * h * cameraUpW;
				rd = normalize(rd);
				
				// sample screen
				float4 col = tex2D(_MainTex, i.uv);
				float z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
				float depth = _Far * Linear01Depth(z);

				float3 sundir = -normalize(_SunDir);
				float3 suncol = 0;

				if (depth >= _Far) {
					if (!_SkyScatter) return col;

					float sun = dot(rd, sundir);
					if (sun > 0) suncol = _LightColor0 * getMiePhase(sun, sun*sun, .9995);
				}

				// atmospheric scattering
				float4 atmo = Scatter(_CameraPos, rd, depth, _SunPower, sundir);

				col.rgb = col.rgb * saturate(1 - atmo.a) + atmo.rgb;
				
				col.rgb += suncol;

				return col;
			}
			ENDCG
		}
	}
}
