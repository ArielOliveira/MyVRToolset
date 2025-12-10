#ifndef LIGHT_FUNTIONS
#define LIGHT_FUNTIONS

#ifndef GLOBAL_VARIABLES
    #include "Assets/Shaders/HLSL/GlobalVariables.hlsl"
#endif

half4 ComputeSkybox(float3 viewDirWS) {
    float sunViewDot      = dot(_SUN_DIR, viewDirWS);
    float sunZenithDot    = _SUN_DIR.y;
    float viewZenithDot   = viewDirWS.y;

    float sunViewDot01    = (sunViewDot + 1.0) * 0.5;
    float sunZenithDot01  = (sunZenithDot + 1.0) * 0.5;

    half3 sunViewColor    = SAMPLE_TEXTURE2D(_SUN_VIEW_GRAD, sampler_SUN_VIEW_GRAD, float2(sunZenithDot01, 0.5)).rgb;
    half3 sunZenithColor  = SAMPLE_TEXTURE2D(_SUN_ZENITH_GRAD, sampler_SUN_ZENITH_GRAD, float2(sunZenithDot01, 0.5)).rgb;
    half3 viewZenithColor = SAMPLE_TEXTURE2D(_VIEW_ZENITH_GRAD, sampler_VIEW_ZENITH_GRAD, float2(sunZenithDot01, 0.5)).rgb;

    half horizonHaze      = SAMPLE_TEXTURE2D(_POW_LUT, sampler_POW_LUT, float2(saturate(1 - viewZenithDot), 0.5)).r;
    half sunBloom         = SAMPLE_TEXTURE2D(_POW_LUT, sampler_POW_LUT, float2(saturate(sunViewDot), 0.5)).r;

    float stepRadius      = 1 - _SUN_RADIUS * _SUN_RADIUS;
    half sun              = smoothstep(stepRadius, stepRadius + 0.015, sunViewDot);

    return half4(sunZenithColor + (viewZenithColor * horizonHaze) + (sunViewColor * sunBloom) + sun, 1.0);
}

#endif