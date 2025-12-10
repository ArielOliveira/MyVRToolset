Shader "Arielado/SkyboxBakeVisualizer" {
    Properties {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white"

        [NoScaleOffset] _SunZenithGrad ("Sun-Zenith gradient", 2D) = "white" {}
        [NoScaleOffset] _ViewZenithGrad ("View-Zenith gradient", 2D) = "white" {}
        [NoScaleOffset] _SunViewGrad ("Sun-View gradient", 2D) = "white" {}

        _SunRadius ("Sun Radius", Range(0.1, 1)) = 0.2
    }

    SubShader {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/HLSL/GlobalVariables.hlsl"
            #include "Assets/Shaders/HLSL/LightFunctions.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
            };

            TEXTURE2D_HALF(_BaseMap);            SAMPLER(sampler_BaseMap);
            TEXTURE2D_HALF(_SunZenithGrad);      SAMPLER(sampler_SunZenithGrad);
            TEXTURE2D_HALF(_ViewZenithGrad);     SAMPLER(sampler_ViewZenithGrad);
            TEXTURE2D_HALF(_SunViewGrad);        SAMPLER(sampler_SunViewGrad);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float _SunRadius;
            CBUFFER_END

            Varyings vert(Attributes IN) {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                float3 viewDirWS = normalize(IN.positionWS - GetCameraPositionWS());

                /*float sunViewDot = dot(_SUN_DIR, viewDirWS);
                float sunZenithDot = _SUN_DIR.y;
                float viewZenithDot = viewDirWS.y;

                float sunViewDot01 = (sunViewDot + 1.0) * 0.5;
                float sunZenithDot01 = (sunZenithDot + 1.0) * 0.5;
                
                half3 sunViewColor = SAMPLE_TEXTURE2D(_SunViewGrad, sampler_SunViewGrad, float2(sunZenithDot01, 0.5)).rgb;
                half3 sunZenithColor = SAMPLE_TEXTURE2D(_SunZenithGrad, sampler_SunZenithGrad, float2(sunZenithDot01, 0.5)).rgb;
                half3 viewZenithColor = SAMPLE_TEXTURE2D(_ViewZenithGrad, sampler_ViewZenithGrad, float2(sunZenithDot01, 0.5)).rgb;

                half horizonHaze = SAMPLE_TEXTURE2D(_POW_LUT, sampler_POW_LUT, float2(saturate(1 - viewZenithDot), 0.5)).r;
                half sunBloom = SAMPLE_TEXTURE2D(_POW_LUT, sampler_POW_LUT, float2(saturate(sunViewDot), 0.5)).r;

                float stepRadius = 1 - _SunRadius * _SunRadius;
                half sun = smoothstep(stepRadius, stepRadius + 0.015, sunViewDot);*/
                //float sun = step(stepRadius, sunViewDot);
                
                return ComputeSkybox(viewDirWS);
            }
            ENDHLSL
        }
    }
}
