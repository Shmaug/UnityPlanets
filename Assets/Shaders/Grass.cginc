#ifndef GRASSCGINC
#define GRASSCGINC

#include "UnityCG.cginc"

#ifndef SHADOWCASTER
#include "Lighting.cginc"
#include "AutoLight.cginc"
#endif

float _Size;
float _Spread;
float _yOffset;
float _DrawDistance;
float _DistFalloff;

uint _TextureCount;

float4 _Wind;
float _WindScale;
float _WindSpeed;
float _WindAmount;

#define BLADECOUNT 1

struct v2g {
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float4 tex0 : TEXCOORD0;
#ifndef SHADOWCASTER
	float3 vlight : TEXCOORD1;
#endif
};
struct g2f {
#ifndef SHADOWCASTER
	float4 pos : SV_POSITION;
	float3 normal : NORMAL;
	float4 tex0 : TEXCOORD1;
	float3 worldPos : TEXCOORD0;
	float3 vlight : TEXCOORD2;
	LIGHTING_COORDS(3, 4)
#else
	V2F_SHADOW_CASTER;
	float4 tex0 : TEXCOORD1;
#endif
};

float3 GetWind(float3 wpos) {
	wpos *= _WindScale;

	float t = _Time.y * _WindSpeed * _Wind.w + dot(wpos.xz, _Wind.xz) * .25;
	float sinx = sin(t);
	float sinx5 = sin(t + 5);
	float f = 2 * sinx * sinx * sinx5 * sinx5;

	return _Wind.xyz * f * _WindAmount;
}
float3x3 axisangle(float3 axis, float angle)
{
	float s = sin(angle);
	float c = cos(angle);
	float oc = 1.0 - c;

	return transpose(float3x3(
		oc * axis.x * axis.x + c, oc * axis.x * axis.y - axis.z * s, oc * axis.z * axis.x + axis.y * s,
		oc * axis.x * axis.y + axis.z * s, oc * axis.y * axis.y + c, oc * axis.y * axis.z - axis.x * s,
		oc * axis.z * axis.x - axis.y * s, oc * axis.y * axis.z + axis.x * s, oc * axis.z * axis.z + c ));
}

g2f setvert(float3 vertex, float3 normal, float4 tex0) {
	g2f o = (g2f)0;
	v2g v = (v2g)0;
	v.vertex = float4(vertex, 1.0);
	v.normal = normal;

	o.tex0 = tex0;

	// shadowcaster g2f
	#ifdef SHADOWCASTER
	#ifdef SHADOWS_CUBE
	o.vec = mul(unity_ObjectToWorld, v.vertex).xyz - _LightPositionRange.xyz;
	o.pos = UnityObjectToClipPos(v.vertex);
	#else
	o.pos = UnityObjectToClipPos(v.vertex.xyz);
    o.pos = UnityApplyLinearShadowBias(o.pos);
	#endif
	#else

	// regular v2f
	o.pos = UnityObjectToClipPos(vertex);
	o.normal = mul((float3x3)unity_ObjectToWorld, normal);
	o.worldPos = mul(unity_ObjectToWorld, vertex).xyz;

	TRANSFER_VERTEX_TO_FRAGMENT(o);

	#endif

	return o;
}

bool InFrustum(float3 wpos, float dist)
{
	float4 pos = float4(wpos, 1.0);

	return
		dot(pos, unity_CameraWorldClipPlanes[0]) > -dist &&
		dot(pos, unity_CameraWorldClipPlanes[1]) > -dist &&
		dot(pos, unity_CameraWorldClipPlanes[2]) > -dist &&
		dot(pos, unity_CameraWorldClipPlanes[3]) > -dist;
}
bool InView(float3 wpos, float4 tex0) {
	float d = length(_WorldSpaceCameraPos.xyz - wpos);
	d += _DistFalloff * tex0.x;
	return (_DrawDistance <= 0 || d < _DrawDistance) && InFrustum(wpos, _Spread + 2 * _Size);
}

[maxvertexcount(4)]
void geom(point v2g p[1], inout TriangleStream<g2f> triStream) {
	v2g i = p[0];

	float3 worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;

	if (!InView(worldPos, i.tex0))
		return;

	const float randomnums[32] = { 0.35825f, 0.33648f, 0.03036f, 0.52093f, 0.16593f, 0.85862f, 0.71356f, 0.37319f, 0.16887f, 0.36572f, 0.87636f, 0.07129f, 0.97062f, 0.07100f, 0.69601f, 0.09673f, 0.87167f, 0.53476f, 0.88892f, 0.01361f, 0.43281f, 0.31642f, 0.96874f, 0.77504f, 0.28773f, 0.75383f, 0.09934f, 0.56009f, 0.62574f, 0.97024f, 0.38942f, 0.16712f };

	float3 normal = normalize(i.normal);

	float3 up = normal;

	float size = _Size * i.tex0.z;

	up *= size * (.8 + .4 * randomnums[(int)(i.tex0.x * 32)]);

	float3 w = GetWind(worldPos);
	float3 o = -normal * _yOffset;
	
	for (uint b = 0; b < BLADECOUNT; b++) {
		uint lookup = (b * 3 + 32 * i.tex0.x) % 32;
		
		float3x3 m =
			axisangle(normalize(cross(normal, float3(0, 1, 0))), acos(normal.y))
			* axisangle(normal, i.tex0.x * UNITY_PI + randomnums[lookup] * UNITY_TWO_PI);
		float3 right = normalize(mul(m, float3(1, 0, 0)));
		right *= .5f * size;

		float u = floor(i.tex0.w * _TextureCount) / _TextureCount;
		float2 uvbox = float2(u, u + 1.0 / _TextureCount);

		float3 p = mul(m, normalize(float3(randomnums[(lookup + 2) % 32], randomnums[(lookup + 1) % 32], 0))) * _Spread * i.tex0.x;

		float we = w - normal * abs(dot(normal, w));
		
		g2f v0 = setvert(i.vertex.xyz + o + p + right, normal, float4(uvbox.y, 0, 0, i.tex0.y));
		g2f v1 = setvert(i.vertex.xyz + o + p + right + up + we, normal, float4(uvbox.y, 1, 0, i.tex0.y));
		g2f v2 = setvert(i.vertex.xyz + o + p - right, normal, float4(uvbox.x, 0, 0, i.tex0.y));
		g2f v3 = setvert(i.vertex.xyz + o + p - right + up + we, normal, float4(uvbox.x, 1, 0, i.tex0.y));

		#ifndef SHADOWCASTER
		v0.vlight = i.vlight;
		v1.vlight = i.vlight;
		v2.vlight = i.vlight;
		v3.vlight = i.vlight;
		#endif

		triStream.Append(v0);
		triStream.Append(v1);
		triStream.Append(v2);
		triStream.Append(v3);

		triStream.RestartStrip();
	}
}

#endif