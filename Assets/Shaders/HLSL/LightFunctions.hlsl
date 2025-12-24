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
    float sunMoonDot      = dot(_MOON_DIR, _SUN_DIR);
    float moonViewDot     = dot(_MOON_DIR, viewDirWS);
    
    float sunZenithDot    = _SUN_DIR.y;
    float viewZenithDot   = viewDirWS.y;
    float moonZenithDot   = _MOON_DIR.y;
    
    float sunViewDot01    = (sunViewDot + 1.0) * 0.5;
    float sunZenithDot01  = (sunZenithDot + 1.0) * 0.5;

    float moonStepRadius  = 1 - _MOON_RADIUS * _MOON_RADIUS;
    float sunStepRadius   = 1 - _SUN_RADIUS * _SUN_RADIUS;

    float nightSkyFactor  = (1 - sunViewDot01) * (saturate(-sunZenithDot));

    float solarEclipse01  = smoothstep(sunStepRadius, 1, sunMoonDot);
    sd.lunarEclipse       = 1 - smoothstep(sunStepRadius - 0.01, sunStepRadius, -sunViewDot);
    float moonLight       = (1 - saturate(sunMoonDot)) * nightSkyFactor;
    moonLight            *= 1 - smoothstep(sunStepRadius - 0.01, sunStepRadius, -sunMoonDot);
    
    float moonSmoothness  = (moonStepRadius * solarEclipse01 * 0.003);

    sd.moon               = smoothstep(moonStepRadius - moonSmoothness - 0.001, moonStepRadius, moonViewDot - (moonSmoothness * 0.97) - 0.00097);
    sd.sun                = smoothstep(sunStepRadius - 0.01, sunStepRadius, sunViewDot - 0.0097) * (1 - sd.moon) * (lerp(1, 8, solarEclipse01));    

    half4 nightSkyColor   = SAMPLE_TEXTURECUBE_BIAS(_NIGHT_SKY, sampler_NIGHT_SKY, viewDirWS, -1);

    half2 sunMoonBloom    = SAMPLE_TEXTURE2D(_POW_LUT, sampler_POW_LUT, float2(saturate(sunViewDot), saturate(moonViewDot))).zw;
    half2 hazeAndNight    = SAMPLE_TEXTURE2D(_POW_LUT, sampler_POW_LUT, float2(saturate(1 - viewZenithDot), saturate(nightSkyColor.a * _NIGHT_SKY_EXPOSURE.y * 6))).xw;

    sd.sunBloom           = sunMoonBloom.x;
    sd.moonBloom          = sunMoonBloom.y * moonLight;
    sd.horizonHaze        = hazeAndNight.x;
    
    float nightSkyExp     = hazeAndNight.y;
    
    nightSkyColor.rgb    *= half3(1080, 830, 700) * nightSkyExp;    

    half3 moonBloomColor  = half3(0.05, 0.1, 0.3) * sd.moonBloom * 1.5;

    float moonStep        = 1 - sd.moon;
    float sunStep         = 1 - sd.sun;
    
    nightSkyColor.rgb    *= moonStep * nightSkyFactor;

    half3 sunZenithColor  = SAMPLE_TEXTURE2D(_SKY_GRADIENTS, sampler_SKY_GRADIENTS, float2(sunZenithDot01, 1)).rgb;
    half3 viewZenithColor = SAMPLE_TEXTURE2D(_SKY_GRADIENTS, sampler_SKY_GRADIENTS, float2(sunZenithDot01, 0.5)).rgb;
    half3 sunViewColor    = SAMPLE_TEXTURE2D(_SKY_GRADIENTS, sampler_SKY_GRADIENTS, float2(sunZenithDot01, 0)).rgb;

    half3 eclipseColor    = half3(0.8, 0.12, 0);
    
    sunViewColor          = lerp(sunViewColor, eclipseColor, solarEclipse01 * sd.sunBloom);
    half3 sunColor        = lerp(1, eclipseColor, solarEclipse01) * sd.sun;

    half3 skyColor = lerp(viewZenithColor, sunZenithColor + viewZenithColor * sd.horizonHaze + sunViewColor * sd.sunBloom + sunColor + nightSkyColor.rgb + moonBloomColor, 1 - sd.horizonHaze);

    skyColor *= lerp(1, 0.15, solarEclipse01 * (1 - nightSkyFactor));
    return half4(skyColor, 1.0);
}

#endif