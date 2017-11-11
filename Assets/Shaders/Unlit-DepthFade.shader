Shader "Unlit/DepthFade"
{
	Properties
	{
		_Color ("Color", Color) = (0,0,0,1)
		_Depth ("Depth", Float) = 2
		_Falloff("Falloff", Float) = 1
		[MaterialToggle] _UseAmbient ("Use Ambient Light", Float) = 0
		
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "RenderQueue"="Transparent+100" }
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _CameraDepthTexture;
			
			float4 _Color;
			float _Depth;
			float _UseAmbient;
			float _Falloff;

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
				float4 screenPos : TEXCOORD2;
				UNITY_FOG_COORDS(3)
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata_base v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.worldNormal = mul(v.normal, unity_WorldToObject);
				return o;
			}

			float CalcDepth(v2f i) {
				float depth = LinearEyeDepth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)).r);

				float3 ray = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);

				return (depth - i.screenPos.w) * dot(ray, normalize(i.worldNormal));
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float d = CalcDepth(i);
				
				float4 col = float4(_Color.rgb, pow(saturate(d / _Depth), _Falloff));

				if (_UseAmbient) col.rgb *= UNITY_LIGHTMODEL_AMBIENT;

				return col;
			}
			ENDCG
		}
	}
}
