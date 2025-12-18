#ifndef GLOBAL_VARIABLES
#define GLOBAL_VARIABLES

#if defined(TEXTURE2D)
    TEXTURE2D_HALF(_POW_LUT);              SAMPLER(sampler_POW_LUT);
    TEXTURE2D_HALF(_SKY_GRADIENTS);        SAMPLER(sampler_SKY_GRADIENTS);
#endif

#if defined(TEXTURECUBE)
    TEXTURECUBE(_NIGHT_SKY); SAMPLER(sampler_NIGHT_SKY);
    TEXTURECUBE(_CLOUDS);    SAMPLER(sampler_CLOUDS);
#endif

uniform float3 _SUN_DIR;
uniform float3 _MOON_DIR;

uniform float  _SUN_RADIUS;
uniform float  _MOON_RADIUS;

uniform float2 _NIGHT_SKY_EXPOSURE;

#endif