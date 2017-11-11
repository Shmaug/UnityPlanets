Shader "Custom/Grass" {
	Properties{
		[NoScaleOffset]
		_MainTex("Texture", 2D) = "white" {}
		_TextureCount("Texture Count", Int) = 1
		_Color1("Color 1", Color) = (1,1,1,1)
		_Color2("Color 2", Color) = (1,1,1,1)

		_Specular("Specular Color (RGB)", Color) = (.1,.1,.1,1)
		_Smoothness("Smoothness", Range(0,1)) = 0

		_Size("Size", Float) = 1.0
        _Cutoff("Alpha Cutoff", Range(0,1)) = .75

		_Spread("Spread", Range(0,10)) = .5
		_yOffset("Y Offset", Float) = .1
		_WindScale("Wind Scale", Float) = 1
		_WindSpeed("Wind Speed", Float) = 1
		_WindAmount("Wind Amount", Float) = .5
		_Wind("Wind", Vector) = (.1, .2, .1, 1.0)

		_DrawDistance("Draw Distance", Float) = 100
		_DistFalloff("Draw Distance Falloff", Float) = 10
	}
	SubShader{
		Tags { "RenderType"="CustomGrass" "RenderQueue"="Geometry" }

		// Forward passes
		Pass{
			Tags {"LightMode" = "ForwardBase"}
			Cull Off
			ZWrite On

			CGPROGRAM
			#pragma target 5.0

			#pragma multi_compile_fwdbase
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom

			#include "Grass.cginc"

			sampler2D _MainTex;
			float4 _Color1;
			float4 _Color2;
            float _Cutoff;

			v2g vert(appdata_base v) {
				v2g o;
				
				o.vertex = v.vertex;
				o.normal = v.normal;

				o.tex0 = v.texcoord;
				o.vlight = 0;

				#ifdef VERTEXLIGHT_ON
				float4 col = lerp(_Color1, _Color2, v.texcoord.z);
				float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
				float3 wnorm = mul((float3x3)unity_ObjectToWorld, v.normal);

				for (int index = 0; index < 4; index++)
				{
					float3 lightPosition = float3(unity_4LightPosX0[index], unity_4LightPosY0[index], unity_4LightPosZ0[index]);

					float3 vertexToLightSource = lightPosition.xyz - wpos;
					float3 lightDirection = normalize(vertexToLightSource);
					float squaredDistance = dot(vertexToLightSource, vertexToLightSource);
					float attenuation = 1.0 / (1.0 + unity_4LightAtten0[index] * squaredDistance);
					float3 diffuseReflection = attenuation * unity_LightColor[index].rgb * col.rgb * max(0.0, dot(wnorm, lightDirection));

					o.vlight += diffuseReflection;
				}
				#endif

				return o;
			}
			float4 frag(g2f i) : SV_Target {
				float4 col = tex2D(_MainTex, i.tex0.xy) * lerp(_Color1, _Color2, i.tex0.w);
				clip(col.a - _Cutoff);

				float3 normal = normalize(i.normal);

				float3 lightdir;
				float atten;
				if (_WorldSpaceLightPos0.w == 0) {
					atten = 1.0;
					lightdir = normalize(_WorldSpaceLightPos0.xyz);
				} else {
					float3 v2l = _WorldSpaceLightPos0.xyz - i.worldPos;
					float dist = length(v2l);

					atten = 1.0 / (1.0 + _LightColor0.w * dist * dist);

					lightdir = v2l / dist;
				}

				float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb;
				float3 diffuse = atten * max(0, dot(normal, lightdir)) * _LightColor0.rgb;

                float a = SHADOW_ATTENUATION(i);
                diffuse *= a;
				col.rgb = (i.vlight + ambient + diffuse) * col.rgb;

				return float4(col.rgb, 1.0);
			}

			ENDCG
		}
		Pass{
			Tags { "LightMode" = "ForwardAdd" }
			Fog { Color (0,0,0,0) }
			Blend One One
			Cull Off

			CGPROGRAM
			#pragma target 5.0

			#pragma multi_compile_fwdadd_fullshadows
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom

			#include "Grass.cginc"
			
			sampler2D _MainTex;
			float4 _Color1;
			float4 _Color2;
            float _Cutoff;
			sampler2D _LightTextureB0;

			v2g vert(appdata_base v) {
				v2g o;
				
				o.vertex = v.vertex;
				o.normal = v.normal;

				o.tex0 = v.texcoord;
				o.vlight = 0;
				return o;
			}
			float4 frag(g2f i) : SV_Target {
				float4 col = tex2D(_MainTex, i.tex0.xy) * lerp(_Color1, _Color2, i.tex0.w);
				clip(col.a - _Cutoff);

				float3 normal = normalize(i.normal);

				float3 lightdir;
				float atten;
				if (_WorldSpaceLightPos0.w == 0) {
					atten = 1.0;
					lightdir = normalize(_WorldSpaceLightPos0.xyz);
				} else {
					float3 v2l = _WorldSpaceLightPos0.xyz - i.worldPos;
					float dist = length(v2l);
					lightdir = v2l / dist;

					atten = 1.0 / (1.0 + _LightColor0.w * dist * dist);
				}
                
				float3 diffuse = col.rgb * atten * max(0, dot(normal, lightdir)) * _LightColor0.rgb;

                float a = LIGHT_ATTENUATION(i);
				diffuse *= a;

				return float4(diffuse, 1.0);
			}

			ENDCG
		}
		
		// Deferred pass
		Pass{
			Tags { "LightMode" = "Deferred" }
			Cull Off

			CGPROGRAM
			#pragma target 5.0

			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom
			
            #pragma exclude_renderers nomrt
            #pragma multi_compile_prepassfinal
            #pragma multi_compile_instancing

			#include "Grass.cginc"
			#include "UnityStandardCore.cginc"
			#pragma multi_compile ___ UNITY_HDR_ON

			//sampler2D _MainTex;
            //float _Cutoff;
			float4 _Color1;
			float4 _Color2;

            float3 _Specular;
            float _Smoothness;

			v2g vert(appdata_base v) {
				v2g o;
				
				o.vertex = v.vertex;
				o.normal = v.normal;

				o.tex0 = v.texcoord;
				o.vlight = 0;

				return o;
			}


			void frag(g2f i,
				out half4 outAlbedo   : SV_Target0, // albedo (rgb) occlusion (a)
				out half4 outSurface  : SV_Target1, // specular (rgb), smoothness (a)
				out half4 outNormal   : SV_Target2, // normal (xyz)
				out half4 outEmission : SV_Target3) {

				float4 col = tex2D(_MainTex, i.tex0.xy) * lerp(_Color1, _Color2, i.tex0.w);
				//col = float4(i.tex0.xy, 0, 1);
				clip(col.a - _Cutoff);

				UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);
				
				half3 normalWorld = normalize(i.normal);
				half3 eyeVec = normalize(i.worldPos - _WorldSpaceCameraPos);
				half3 specColor = _Specular.rgb;
				half smoothness = _Smoothness;
				half oneMinusReflectivity;
				half3 diffColor = EnergyConservationBetweenDiffuseAndSpecular(col.rgb, specColor, oneMinusReflectivity);

				half alpha = 1.0;
				diffColor = PreMultiplyAlpha(diffColor, 1.0, oneMinusReflectivity, alpha);

				half occlusion = 1.0;
				
				UnityLight light;
				light.dir = half3(0, 1, 0);
				light.ndotl = 0;
				light.color = 0;

				UnityIndirect indirect;
				indirect.diffuse = 0;
				indirect.specular = 0;

				#if UNITY_SHOULD_SAMPLE_SH
				indirect.diffuse = ShadeSHPerPixel(normalWorld, 0, i.worldPos);
				#endif
				
				half3 emissive = UNITY_BRDF_PBS(diffColor, specColor, oneMinusReflectivity, smoothness, normalWorld, -eyeVec, light, indirect).rgb;

				#ifndef UNITY_HDR_ON
				emissive = exp2(-emissive);
				#endif

				outAlbedo = half4(diffColor, 1.0);
				outSurface = half4(specColor, smoothness);
				outNormal = half4(normalWorld * .5f + .5f, 1.0);
				outEmission = half4(emissive, 1.0);
			}

			ENDCG
		}
		
		// Shadowcaster
		Pass {
			Tags { "LightMode" = "ShadowCaster" }
			Fog { Mode Off }
			ZWrite On ZTest Less Cull Off
			Offset 1,1

			CGPROGRAM
			#pragma multi_compile_shadowcaster
			#define SHADOWCASTER
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom

			#include "Grass.cginc"
			
			sampler2D _MainTex;
			float4 _Color1;
			float4 _Color2;
            float _Cutoff;

			v2g vert(appdata_base v) {
				v2g o;
				
				o.vertex = v.vertex;
				o.normal = v.normal;
				o.tex0 = v.texcoord;

				return o;
			}
			float4 frag(g2f i) : SV_Target {
				float4 col = tex2D(_MainTex, i.tex0.xy) * lerp(_Color1, _Color2, i.tex0.w);
				clip(col.a - .5);

				SHADOW_CASTER_FRAGMENT(i)
			}

			ENDCG
		}
	}
}
