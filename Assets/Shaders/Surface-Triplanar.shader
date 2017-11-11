Shader "Custom/Triplanar" {
	Properties{
		_Scale ("Scale", Float) = .5
		_Color ("Color", Color) = (1,1,1,1)
		[NoScaleOffset]
		_MainTex ("Albedo (RGB) Smoothness (A)", 2D) = "white" {}
		_Smoothness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.5
		[Normal]
		[NoScaleOffset]
		_NormalTex ("Normal", 2D) = "bump" {}

		[Space]
		_Scale2 ("Scale 2", Float) = .1
		_ScaleBlend ("Scale Blend", Range(0, 1)) = .5
		_BlendScale("Blend Scale", Float) = .5
		_Warp("Warp", Float) = .2
		_WarpScale("Warp Scale", Float) = 1.0
		[Toggle(TEXBLEND)] _TexBlend("Blend Separate Texture", Float) = 0
		[NoScaleOffset]
		_BlendTex ("Albedo (RGB) Smoothness (A)", 2D) = "white" {}
		[NoScaleOffset]
		[Normal]
		_BlendTexNormal ("Normal", 2D) = "bump" {}
		[NoScaleOffset]
		_NoiseTexture ("rgba noise", 2D) = "black" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows vertex:vert
		#pragma target 5.0
		#pragma multi_compile ___ TEXBLEND

		float4 _Color;
		float _Scale;
		float _Scale2;
		float _ScaleBlend;
		float _BlendScale;
		sampler2D _MainTex;
		sampler2D _NormalTex;
		sampler2D _NoiseTexture;
		float _Smoothness;
		float _Metallic;

		float _Warp;
		float _WarpScale;
		sampler2D _BlendTex;
		sampler2D _BlendTexNormal;

		struct Input {
			float2 uv_MainTex;
			float3 norm;
			float3 pos;
		};

		float noise(float3 x) {
			float3 p = floor(x);
			float3 f = frac(x);
			f = f*f*(3.0-2.0*f);
			float2 uv = (p.xy+float2(37.0,17.0)*p.z) + f.xy;
			uv = (uv + .5) / 256.0;
			uv.y *= -1.0;
			float2 rg = tex2D(_NoiseTexture, uv).yx;
			return lerp(rg.x, rg.y, f.z);
		}

		void vert(inout appdata_full v, out Input data){
			UNITY_INITIALIZE_OUTPUT(Input, data);
			data.norm = v.normal;
			data.pos = v.vertex.xyz;
		}

		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_CBUFFER_START(Props)
		UNITY_INSTANCING_CBUFFER_END

		fixed4 triplanar(sampler2D samp, float3 blend, float3 pos){
			return tex2D(samp, pos.yz) * blend.x + tex2D(samp, pos.xz) * blend.y + tex2D(samp, pos.xy) * blend.z;
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float3 worldScale = float3(
				length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x)), // scale x axis
				length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y)), // scale y axis
				length(float3(unity_ObjectToWorld[0].z, unity_ObjectToWorld[1].z, unity_ObjectToWorld[2].z))  // scale z axis
				);
			float3 pos = IN.pos * worldScale * _Scale;
			float3 blend = abs(IN.norm) / dot(abs(IN.norm), 1.0);

			float4 c = triplanar(_MainTex, blend, pos);
			float4 n = triplanar(_NormalTex, blend, pos);

			if (_ScaleBlend > 0){
				float3 pos2 = IN.pos * worldScale * _Scale2;
				float w = 0;
				if (_Warp > 0) w = noise(pos * _BlendScale * _WarpScale) * _Warp;
				float sblend = _ScaleBlend * (saturate(noise(pos * _BlendScale + w) * 4 - 1));

				#ifdef TEXBLEND
				sampler2D sc = _BlendTex;
				sampler2D sn = _BlendTexNormal;
				#else
				sampler2D sc = _MainTex;
				sampler2D sn = _NormalTex;
				#endif

				c = c * (1.0 - sblend) + triplanar(sc, blend, pos2) * sblend;
				n = n * (1.0 - sblend) + triplanar(sn, blend, pos2) * sblend;
			}

			o.Albedo = c.rgb * _Color.rgb;
			o.Alpha = c.a;
			o.Smoothness = _Smoothness * c.a;
			o.Metallic = _Metallic;
			o.Normal = UnpackNormal(n);
		}
		ENDCG
	}
	FallBack "Diffuse"
}
