#ifndef MATH
#define MATH

//smooth version of step
float aaStep(float compValue, float gradient) {
    float halfChange = fwidth(gradient) * 0.5;
    //base the range of the inverse lerp on the change over one pixel
    float lowerEdge = compValue - halfChange;
    float upperEdge = compValue + halfChange;
    //do the inverse interpolation
    float stepped = (gradient - lowerEdge) / (upperEdge - lowerEdge);
    stepped = saturate(stepped);
    
    return stepped;
}

float aaStep(float compValue, float gradient, float softness) {
    float halfChange = max(fwidth(gradient), softness) * 0.5;
    //base the range of the inverse lerp on the change over one pixel
    float lowerEdge = compValue - halfChange;
    float upperEdge = compValue + halfChange;
    
    //do the inverse interpolation
    float stepped = (gradient - lowerEdge) / (upperEdge - lowerEdge);
    stepped = saturate(stepped);
    
    return stepped;
}

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

#endif