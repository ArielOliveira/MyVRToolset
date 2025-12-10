#ifndef GLOBAL_VARIABLES
#define GLOBAL_VARIABLES

#if defined(TEXTURE2D)
    TEXTURE2D_HALF(_POW_LUT);              SAMPLER(sampler_POW_LUT);

    TEXTURE2D_HALF(_SUN_ZENITH_GRAD);      SAMPLER(sampler_SUN_ZENITH_GRAD);
    TEXTURE2D_HALF(_VIEW_ZENITH_GRAD);     SAMPLER(sampler_VIEW_ZENITH_GRAD);
    TEXTURE2D_HALF(_SUN_VIEW_GRAD);        SAMPLER(sampler_SUN_VIEW_GRAD);
#endif

uniform float3 _SUN_DIR;
uniform float3 _MOON_DIR;

uniform float  _SUN_RADIUS;
uniform float  _MOON_RADIUS;

#endif