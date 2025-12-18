#ifndef LIGHT_FUNCTIONS
#define LIGHT_FUNCTIONS

#ifndef GLOBAL_VARIABLES
    #include "Assets/Shaders/HLSL/GlobalVariables.hlsl"
#endif

#ifndef LIGHT_INPUTS
    #include "Assets/Shaders/HLSL/LightInputs.hlsl"
#endif

// Construct a rotation matrix that rotates around a particular axis by angle
// From: https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33
float3x3 AngleAxis3x3(float angle, float3 axis) {
    float c, s;
    sincos(angle, s, c);

    float t = 1 - c;
    float x = axis.x;
    float y = axis.y;
    float z = axis.z;

    return float3x3(
        t * x * x + c, t * x * y - s * z, t * x * z + s * y,
        t * x * y + s * z, t * y * y + c, t * y * z - s * x,
        t * x * z - s * y, t * y * z + s * x, t * z * z + c
        );
}

// Rotate the view direction, tilt with latitude, spin with time
float3 GetStarUVW(float3 viewDir, float latitude, float localSiderealTime) {
    // tilt = 0 at the north pole, where latitude = 90 degrees
    float tilt = PI * (latitude - 90) / 180;
    float3x3 tiltRotation = AngleAxis3x3(tilt, float3(1,0,0));

    // 0.75 is a texture offset for lST = 0 equals noon
    float spin = (0.75-localSiderealTime) * 2 * PI;
    float3x3 spinRotation = AngleAxis3x3(spin, float3(0, 1, 0));

    // The order of rotation is important
    float3x3 fullRotation = mul(spinRotation, tiltRotation);

    return mul(fullRotation,  viewDir);
}

half4 ComputeSkybox(float3 viewDirWS, out SkyData sd) {
    float sunViewDot      = dot(_SUN_DIR, viewDirWS);
    float sunZenithDot    = _SUN_DIR.y;
    float viewZenithDot   = viewDirWS.y;
    float moonViewDot     = dot(_MOON_DIR, viewDirWS);
    float sunMoonDot      = dot(_MOON_DIR, _SUN_DIR);
    float moonZenithDot   = _MOON_DIR.y;
    
    float sunViewDot01    = (sunViewDot + 1.0) * 0.5;
    float sunZenithDot01  = (sunZenithDot + 1.0) * 0.5;

    float moonStepRadius  = 1 - _MOON_RADIUS * _MOON_RADIUS;
    float sunStepRadius   = 1 - _SUN_RADIUS * _SUN_RADIUS;

    float nightSkyFactor  = (1 - sunViewDot01) * (saturate(-sunZenithDot));

    float solarEclipse01  = smoothstep(sunStepRadius, 1, sunMoonDot);
    float moonLight       = (1 - saturate(sunMoonDot)) * nightSkyFactor;
    
    sd.moon               = smoothstep(moonStepRadius, moonStepRadius + _MOON_RADIUS * 0.006 * (2 + solarEclipse01), moonViewDot);
    sd.sun                = smoothstep(sunStepRadius, sunStepRadius + _SUN_RADIUS * 0.03, sunViewDot) * (1 - sd.moon) * lerp(1, 8, solarEclipse01);

    sd.horizonHaze        = SAMPLE_TEXTURE2D(_POW_LUT, sampler_POW_LUT, float2(saturate(1 - viewZenithDot), 0.5)).r;
    sd.sunBloom           = SAMPLE_TEXTURE2D(_POW_LUT, sampler_POW_LUT, float2(saturate(sunViewDot), 0.5)).r;
    sd.moonBloom          = SAMPLE_TEXTURE2D(_POW_LUT, sampler_POW_LUT, float2(saturate(moonViewDot), 0.5)).r * moonLight;

    half4 nightSkyColor   = SAMPLE_TEXTURECUBE_BIAS(_NIGHT_SKY, sampler_NIGHT_SKY, viewDirWS, -1);
    float nightSkyExp     = smoothstep(_NIGHT_SKY_EXPOSURE.x, _NIGHT_SKY_EXPOSURE.y, SAMPLE_TEXTURE2D(_POW_LUT, sampler_POW_LUT, float2(nightSkyColor.a * 1.4, 0.5)).r);  
    nightSkyColor.rgb    *= nightSkyExp;
    nightSkyColor.rgb    *= half3(680, 630, 600);    

    half3 moonBloomColor  = half3(0.05, 0.1, 0.3) * sd.moonBloom * 1.5;

    float moonStep        = 1 - sd.moon; //1 - smoothstep(moonStepRadius, moonStepRadius + _MOON_RADIUS * 0.006, moonViewDot);
    float sunStep         = 1 - sd.sun;
    
    nightSkyColor.rgb    *= moonStep * nightSkyFactor;

    half3 sunZenithColor  = SAMPLE_TEXTURE2D(_SKY_GRADIENTS, sampler_SKY_GRADIENTS, float2(sunZenithDot01, 1)).rgb;
    half3 viewZenithColor = SAMPLE_TEXTURE2D(_SKY_GRADIENTS, sampler_SKY_GRADIENTS, float2(sunZenithDot01, 0.5)).rgb;
    half3 sunViewColor    = SAMPLE_TEXTURE2D(_SKY_GRADIENTS, sampler_SKY_GRADIENTS, float2(sunZenithDot01, 0)).rgb;
    half3 eclipseColor    = half3(1, 0.05, 0);
    
    sunViewColor          = lerp(sunViewColor, eclipseColor, solarEclipse01 * sd.sunBloom);
    half3 sunColor        = lerp(1, eclipseColor, solarEclipse01) * sd.sun;

    half3 skyColor = lerp(viewZenithColor, sunZenithColor + viewZenithColor * sd.horizonHaze + sunViewColor * sd.sunBloom + sunColor + nightSkyColor.rgb + moonBloomColor, 1 - sd.horizonHaze);
    skyColor *= lerp(1, 0.15, solarEclipse01 * (1 - nightSkyFactor));
    return half4(skyColor, 1.0);
}

#endif