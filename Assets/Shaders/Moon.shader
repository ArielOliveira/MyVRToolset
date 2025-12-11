Shader "Arielado/Moon" {
    Properties {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", Cube) = "white"
        
        [NoScaleOffset] _BumpMap  ("Normal", Cube) = "bump" {}
    }

    SubShader {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/HLSL/LightFunctions.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float4 tangentOS  : TANGENT;
                float3 normalOS   : NORMAL;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 positionOS  : TEXCOORD0;
                float3 normalOS    : TEXCOORD1;
                float4 tangentOS   : TEXCOORD2;
            };

            TEXTURECUBE(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURECUBE(_BumpMap); SAMPLER(sampler_BumpMap);
            

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert(Attributes IN) {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                
                OUT.positionOS = IN.positionOS;
                OUT.normalOS = IN.normalOS;
                OUT.tangentOS = IN.tangentOS;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                float3 positionWS = TransformObjectToWorld(IN.positionOS);
                float3 viewDirWS = normalize(positionWS - GetCameraPositionWS());

                half3 normalMap = UnpackNormal(SAMPLE_TEXTURECUBE(_BumpMap, sampler_BumpMap, IN.normalOS));
                half3 normalWS = TransformTangentToWorld(normalMap, half3x3(-normalInputs.tangentWS.xyz, normalInputs.bitangentWS, normalInputs.normalWS));

                float diffuse = dot(normalWS, _SUN_DIR);
                float diffuse01 = (diffuse + 1) * 0.5;
                
                half sun = (half)0;
                half moon = (half)0;
                half4 sky = ComputeSkybox(viewDirWS, sun, moon);

                half4 color = SAMPLE_TEXTURECUBE(_BaseMap, sampler_BaseMap, IN.normalOS) * _BaseColor;
                return color * saturate(diffuse) + sky;
            }
            ENDHLSL
        }
    }
}
