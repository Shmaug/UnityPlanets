#define PI 3.14159265358979323846
#define SAMPLES 24
#define LSAMPLES 8
#define CLOUDSAMPLES 32
#define CLOUDLSAMPLES 8

#include "Lighting.cginc"

uniform float _Hr = .1332333; // 7994 / (6420e3 - 6360e3);
uniform float _Hm = .02; // 1200 / (6420e3 - 6360e3);
uniform float _InnerRadius;
uniform float _OuterRadius;
uniform float _CloudMin;
uniform float _CloudMax;
uniform float _CloudScale;
uniform float _CloudScroll;
uniform float _CloudSparse;
uniform float4 _CloudColor;
uniform sampler2D _NoiseTexture;

float2 RaySphere(in float3 v3CameraPos, in float3 v3Ray, in float radius) {
	float B = 2.0 * dot(v3CameraPos, v3Ray);
	float C = dot(v3CameraPos, v3CameraPos) - radius*radius;
	float fDet = sqrt(max(0.0, B*B - 4.0 * C));
	return float2(0.5 * (-B - fDet), 0.5 * (-B + fDet));
}

float noise(float3 x) {
	float3 p = floor(x);
	float3 f = frac(x);
	f = f*f*(3.0 - 2.0*f);

	float2 uv = (p.xy + float2(37.0, 17.0) * p.z) + f.xy;
	float4 coord = float4((uv + 0.5) / 256.0, 0, 0);
	coord.y = 1 - (coord.y % 1);
	float2 rg = tex2Dlod(_NoiseTexture, coord).yx;
	return lerp(rg.x, rg.y, f.z) * 2.0 - 1.0;
}
float fbm(float3 p) {
	float3 x = p + _Time.x * _CloudScroll;
	float sum = 0.0;
	sum += 0.50 * noise(x * 1.0);
	sum += 0.35 * noise(x * 2.0);
	sum += 0.15 * noise(x * 6.0);
	return sum;
}

float sampleCloudDensity(float3 p, float scale){
	float h = (length(p) - _CloudMin) * scale;
	return saturate(pow(max(fbm(p * _CloudScale) * saturate(25.0 * h) - h, 0.0) + .85, 100.0)) * _CloudColor.a;
}

float HenyeyGreenstein(float angle, float g){
	return (1 - g*g) / (pow(1 + g*g - 2.0 * g * angle, 1.5) * 4.0 * UNITY_PI);
}

float4 CloudScatter(in float3 ro, in float3 rd, in float depth, in float3 toLight, in float sunPower, out float cdepth){
	cdepth = depth;

	// calculate start and end points through cloud layer
	float h = length(ro);
    float start, end;

	float2 outer = RaySphere(ro, rd, _CloudMax);
	float2 inner = RaySphere(ro, rd, _CloudMin);

    if (h < _CloudMax && h > _CloudMin){ // inside the cloud layer
        start = 0;
		if (inner.x < 0 && inner.y < 0) // both intersections with inner radius are behind us (or non existant), exit point must be on outer radius
			end = max(outer.x, outer.y);
		else
			end = min(inner.x, inner.y);
	} else if (h < _CloudMin) { // below cloud layer
		start = max(inner.x, inner.y);
		end = max(outer.x, outer.y);
	} else if (h > _CloudMax){ // above cloud layer
		if (outer.x < 0 && outer.y < 0) return 0; // this pixel not in clouds
		start = min(outer.x, outer.y);
		end = min(inner.x, inner.y);
	}
	end = min(depth, end);
	if (end < start) return 0; // this pixel not in clouds

    float l = (end - start) / CLOUDSAMPLES;
    float t = start;

	float scale = 1.0 / (_CloudMax - _CloudMin);
	float sl = l * scale;

	const float silver_intensity = .2;
	const float silver_spread = 0.5;

	float angle = dot(rd, toLight);
	float hg1 = HenyeyGreenstein(angle, .60);
	float hg2 = HenyeyGreenstein(angle, .99 - silver_spread);
	float hg = max(hg1, silver_intensity * hg2);
	
	float4 tmp;
	float esum = 0.0;
	float den = 0.0;

    for (uint i = 0; i < CLOUDSAMPLES; i++) {
        float3 pos = ro + rd * t;
		float s = sampleCloudDensity(pos, scale);

		den += s;
		esum += exp(-s) * (1.0 - exp(-s * 2.0)) * saturate(1.0 - esum);

		if (esum > .5) cdepth = t;
		if (esum >= 1.0) break;

        t += l;
    }
	
	return float4(esum * _LightColor0.rgb * _CloudColor.rgb, esum);
}

float4 Scatter(in float3 ro, in float3 rd, in float depth, in float sunPower, in float3 toLight){
	const float g = .76; // mie g
	const float3 betaM = .021; // mie scattering constant
	const float3 betaR = float3(.38, 1.35, 3.31) * .1; // reyleigh scattering constant

	// calculate start and end points through atmosphere

	float2 outer = RaySphere(ro, rd, _OuterRadius);
	if (outer.x < 0 && outer.y < 0) return 0;
    
	float start;
    if (length(ro) < _OuterRadius) // inside the atmosphere
        start = 0;
    else
        start = min(outer.x, outer.y); // intersection with front of atmosphere
	
	float end = min(depth, max(outer.x, outer.y));

	if (length(ro + rd * end) < _InnerRadius) { // end point below innerRadius
		if (length(ro) < _InnerRadius) return 0; // ray completely under innerRadius

		float2 inner = RaySphere(ro, rd, _InnerRadius);
		end = min(inner.x, inner.y);
	}

	// sample clouds
	float4 cloud = 0.0;
	#ifndef NOCLOUDS
	if (_CloudColor.a > 0) cloud = CloudScatter(ro, rd, end, toLight, sunPower, end);
	#endif
	
	// atmopshere sampling setup
    float scale = 1 / (_OuterRadius - _InnerRadius);

    float l = (end - start) / SAMPLES;
    float t = start;

	// scattering setup

	float3 sumR, sumM;
	float opticalDepthR, opticalDepthM;

	float mu = saturate(dot(rd, toLight));
	float phaseM = 3.0f / (8.0f * PI) * ((1.0f - g * g) * (1.0f + mu * mu)) / ((2.0f + g*g) * pow(1.0f + g*g - 2.0f * g * mu, 1.5f));
	float phaseR = 3.0f / (16.0f * PI) * (1 + mu * mu);

    for (uint i = 0; i < SAMPLES; i++) {
        float3 pos = ro + rd * (t + l * .5f);

		float h = (length(pos) - _InnerRadius) * scale;
		float hr = exp(-h / _Hr) * l;
		float hm = exp(-h / _Hm) * l;
		opticalDepthR += hr;
		opticalDepthM += hm;
		
		float2 inner = RaySphere(pos, toLight, _InnerRadius);
		float2 tiLight = RaySphere(pos, toLight, _OuterRadius);
		
		float lLight = max(tiLight.x, tiLight.y);
		lLight /= LSAMPLES;
		float tLight = 0, curLight = 0;
		float opticalDepthLightR = 0, opticalDepthLightM = 0;
		
		float lh;
		for (uint j = 0; j < LSAMPLES; j++) {
			float3 posLight = pos + toLight * (tLight + lLight * .5f);
			lh = (length(posLight) - _InnerRadius) * scale;
			opticalDepthLightR += exp(-lh / _Hr) * lLight;
			opticalDepthLightM += exp(-lh / _Hm) * lLight;
			tLight += lLight;
		}

		float3 tau = betaR * (opticalDepthR + opticalDepthLightR) + betaM * 1.1f * (opticalDepthM + opticalDepthLightM);
		float3 atten = exp(-tau);
		sumR += atten * hr;
		sumM += atten * hm;

        t += l;
    }

	float3 color = sunPower * (sumR * betaR * phaseR + sumM * betaM * phaseM);
	float alpha = length(color);
	
	if (any(cloud)) {
		color += cloud.rgb * saturate(1.0 - color);
		alpha += cloud.a;
	}
	
	return float4(color, alpha);
}
