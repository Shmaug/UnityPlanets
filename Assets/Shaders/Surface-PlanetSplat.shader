Shader "Custom/PlanetSplat" {
	Properties{
		_Specular("Specular", Range(0,1)) = 0.5
		_Metallic("Metallic", Vector) = (0.4, 0.4, 0.4, 0.4)
		_Smoothness("Smoothness", Vector) = (0.4, 0.4, 0.4, 0.4)

		_TexScale("Scale", Float) = 1.0

		[NoScaleOffset]
		_Albedo0("Albedo0", 2D) = "white" {}
		[Normal]
		_Normal0("Normal0", 2D) = "bump" {}

		[NoScaleOffset]
		_Albedo1("Albedo1", 2D) = "white" {}
		[Normal]
		_Normal1("Normal1", 2D) = "bump" {}

		[NoScaleOffset]
		_Albedo2("Albedo2", 2D) = "white" {}
		[Normal]
		_Normal2("Normal2", 2D) = "bump" {}

		[NoScaleOffset]
		_Albedo3("Albedo3", 2D) = "white" {}
		[Normal]
		_Normal3("Normal3", 2D) = "bump" {}
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows vertex:vert
		#pragma target 5.0

		struct Input {
			float4 color : COLOR;
			float3 worldPos;
			float3 norm;
			float3 pos;
			float4 tex0;
		};

		#include "Lighting.cginc"

		uniform sampler2D _Albedo0;
		uniform sampler2D _Normal0;

		uniform sampler2D _Albedo1;
		uniform sampler2D _Normal1;

		uniform sampler2D _Albedo2;
		uniform sampler2D _Normal2;

		uniform sampler2D _Albedo3;
		uniform sampler2D _Normal3;

		uniform float _TexScale;
		uniform float4 _Smoothness;
		uniform float4 _Metallic;

		uniform float _ScaleSpace;
		uniform float4 _PlanetAmbient;

		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_CBUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_CBUFFER_END

		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.pos = v.vertex.xyz;
			o.norm = v.normal;
			o.tex0 = v.texcoord;
		}

		fixed4 triplanar(sampler2D samp, float3 blend, float3 pos) {
			return tex2D(samp, pos.yz) * blend.x + tex2D(samp, pos.xz) * blend.y + tex2D(samp, pos.xy) * blend.z;
		}
		void surf(Input i, inout SurfaceOutputStandard o) {
			float3 pos = i.worldPos * _TexScale * (i.tex0.w + _ScaleSpace * 1000);
			float3 blend = abs(i.norm) / dot(abs(i.norm), 1.0);

			float4 weights = i.color / dot(i.color, 1.0);

			float4 albedo = 0;
			float4 bump = 0;

			// triplanar
			
			albedo += triplanar(_Albedo0, blend, pos) * weights.x;
			albedo += triplanar(_Albedo1, blend, pos) * weights.y;
			albedo += triplanar(_Albedo2, blend, pos) * weights.z;
			albedo += triplanar(_Albedo3, blend, pos) * weights.w;

			bump += triplanar(_Normal0, blend, pos) * weights.x;
			bump += triplanar(_Normal1, blend, pos) * weights.y;
			bump += triplanar(_Normal2, blend, pos) * weights.z;
			bump += triplanar(_Normal3, blend, pos) * weights.w;

			o.Albedo = albedo.rgb;
			o.Normal = UnpackNormal(bump);
			o.Smoothness = dot(_Smoothness, weights) * albedo.a;
			o.Metallic = dot(_Metallic, weights);
		}
		ENDCG
	}
	FallBack "Diffuse"
}
