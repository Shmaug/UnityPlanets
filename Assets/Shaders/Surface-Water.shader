Shader "Custom/Water" {
	Properties{
		_BumpMap("Normals", 2D) = "bump" {}
		_BumpMap2("Normals 2", 2D) = "bump" {}

		_WaterColor("Water Color", Color) = (0.1,0.19,0.22,1)
		_ReflectionColor("Reflection Color", Color) = (1,1,1,1)

		_Distort("Distort", Float) = 0.15
		_DepthFade("Depth Fade", Float) = 0.15
		_DistFade("Distance Fade", Float) = 500

		_Speed("Wave Speed", Float) = .16
		_Frequency("Wave Frequency", Float) = .16
		_Frequency2("Wave Frequency 2", Float) = .16
	}
	SubShader {
		Tags{ "RenderType" = "Opaque" "Queue" = "Geometry+100" }

		Pass{
			CGPROGRAM
			#pragma target 5.0

			#pragma vertex vert
			#pragma fragment frag
    		
			#include "Lighting.cginc"

			#define NOCLOUDS
			#include "Scatter.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float4 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 texcoord : TEXCOORD0;
			};
			struct v2f {
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float3 tangent : TANGENT;
				float3 binormal : BINORMAL;

				float3 worldPos : TEXCOORD0;
				float2 wavePos : TEXCOORD1;
				float4 screenPos : TEXCOORD2;
			};

			uniform float _ScaleSpace;

			uniform float4 _BaseColor;
			uniform float4 _WaterColor;
			uniform float4 _ReflectionColor;

			uniform sampler2D _BumpMap;
			uniform sampler2D _BumpMap2;
			uniform float _DistFade;
			
			uniform float _Frequency;
			uniform float _Frequency2;
			uniform float _Distort;
			uniform float _DepthFade;
			uniform float _Speed;

			uniform float4 _PlanetAmbient;

			// atmosphere

			// #pragma instancing_options assumeuniformscaling
			UNITY_INSTANCING_CBUFFER_START(Props)
			UNITY_INSTANCING_CBUFFER_END
			
			float Fresnel(float cosA) {
				float t = 1.0 - cosA;
				t = t * t * t * t * t;
				return t;
			}

			v2f vert(appdata v) {
				v2f o;

				float3 normal = normalize(mul(v.normal, unity_WorldToObject).xyz);
				float3 tangent = normalize(mul((float3x3)unity_ObjectToWorld, v.tangent).xyz);
				float3 binormal = normalize(cross(tangent, normal));

				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

				o.vertex = UnityWorldToClipPos(worldPos);
				o.worldPos = worldPos;
				o.normal = normal;
				o.tangent = tangent;
				o.binormal = binormal;
				o.wavePos = float2(dot(worldPos, tangent), dot(worldPos, binormal));

				o.screenPos = ComputeScreenPos(o.vertex);

				return o;
			}
			float4 frag(v2f i) : SV_Target {
				float3 normal = normalize(i.normal);

				float3 view = i.worldPos - _WorldSpaceCameraPos;
				float depth = length(view);
				view /= depth;

				float3 color = 0;
				float3 light = 0;
				float fade = saturate(depth / _DistFade);

				if (_ScaleSpace < 1){
					float3 binormal = normalize(i.binormal);
					float3 tangent = normalize(i.tangent);
					float3x3 tbn = float3x3(tangent, binormal, normal);
					// wave normals
					float t = _Time.y * _Speed;
					float3 normal_tan = 
						UnpackNormal(tex2D(_BumpMap, i.wavePos.xy * _Frequency + t)) +
						UnpackNormal(tex2D(_BumpMap, i.wavePos.xy * _Frequency + float2(t * .25, -t))) +
						UnpackNormal(tex2D(_BumpMap2, i.wavePos.xy * _Frequency2 + float2(t * 1.1, t * .1))) * .1;
					normal_tan = normalize(normal_tan);
					normal_tan = normalize(lerp(normal_tan, float3(0,0,1), fade));

					normal = mul(normal_tan, tbn);

					float ndl = saturate(dot(_WorldSpaceLightPos0.xyz, normal));
					light = unity_AmbientSky + (_PlanetAmbient.rgb + _LightColor0.rgb) * ndl;

					// refraction color
					float3 refraction = _WaterColor.rgb * light;
					float3 reflection = _ReflectionColor.rgb * light;
					color = lerp(refraction, reflection, Fresnel(saturate(dot(normal, -view))));
				} else {
					fade = 1.0;
					float ndl = saturate(dot(_WorldSpaceLightPos0.xyz, normal));
					light = unity_AmbientSky + (_PlanetAmbient.rgb + _LightColor0.rgb) * ndl;
				}

				color = lerp(color, _WaterColor.rgb * light, fade);

				//float spec = dot(_WorldSpaceLightPos0.xyz, -reflect(view, normal));
				//if (spec > 0)
				//	color += _LightColor0.rgb * pow(spec, 100.0);

				return float4(color, 1.0);
			}
			ENDCG
		}
	
		Pass {
			Tags { "LightMode"="ShadowCaster" }
			Fog { Mode Off }
			ZWrite On
			Offset 1,1
			ZTest Less

			CGPROGRAM

			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag

			struct appdata{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};
			struct v2f {
				V2F_SHADOW_CASTER;
			};

			v2f vert(appdata v) {
				v2f o;
				TRANSFER_SHADOW_CASTER(o);
				return o;
			}

			float4 frag(v2f i) : SV_TARGET  {
				SHADOW_CASTER_FRAGMENT(i);
			}
			ENDCG
		}
	}
}
