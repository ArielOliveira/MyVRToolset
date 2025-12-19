#ifndef LIGHT_FUNCTIONS
#define LIGHT_FUNCTIONS

#ifndef GLOBAL_VARIABLES
    #include "Assets/Shaders/HLSL/GlobalVariables.hlsl"
#endif

#ifndef LIGHT_INPUTS
    #include "Assets/Shaders/HLSL/LightInputs.hlsl"
#endif

#ifndef MATH
    #include "Assets/Shaders/HLSL/Math.hlsl"
#endif

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
    
    float moonSmoothness  = solarEclipse01 * 0.004;
    sd.moon               = aaStep(moonStepRadius + (moonSmoothness * 0.5), moonViewDot, moonSmoothness);
    sd.sun                = aaStep(sunStepRadius + 0.0025, sunViewDot, 0.005) * (1 - sd.moon) * (lerp(1, 8, solarEclipse01));    

    sd.horizonHaze        = SAMPLE_TEXTURE2D(_POW_LUT, sampler_POW_LUT, float2(saturate(1 - viewZenithDot), 0.5)).r;
    sd.sunBloom           = SAMPLE_TEXTURE2D(_POW_LUT, sampler_POW_LUT, float2(saturate(sunViewDot), 0.5)).r;
    sd.moonBloom          = SAMPLE_TEXTURE2D(_POW_LUT, sampler_POW_LUT, float2(saturate(moonViewDot), 0.5)).r * moonLight;

    half4 nightSkyColor   = SAMPLE_TEXTURECUBE_BIAS(_NIGHT_SKY, sampler_NIGHT_SKY, viewDirWS, -1);
    float nightSkyExp     = SAMPLE_TEXTURE2D(_POW_LUT, sampler_POW_LUT, float2(saturate(nightSkyColor.a * _NIGHT_SKY_EXPOSURE.y), 0.5)).r;
    
    nightSkyColor.rgb    *= half3(2080, 1830, 1700) * nightSkyExp;    

    half3 moonBloomColor  = half3(0.05, 0.1, 0.3) * sd.moonBloom * 1.5;

    float moonStep        = 1 - sd.moon;
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