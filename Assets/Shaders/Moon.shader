Shader "Arielado/Moon" {
    Properties {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white"

        _BumpMap  ("Normal", 2D) = "bump" {}
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
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float4 tangentWS   : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _BumpMap_ST;
            CBUFFER_END

            Varyings vert(Attributes IN) {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS);
                OUT.normalWS = normalInputs.normalWS;
                OUT.tangentWS = float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w * GetOddNegativeScale());

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                float3 viewDirWS = normalize(IN.positionWS - GetCameraPositionWS());

                half3 normalMap = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv.xy * _BumpMap_ST.xy + _BumpMap_ST.zw));
                float sign = IN.tangentWS.w;
                float3 bitangent = sign * cross(IN.normalWS.xyz, IN.tangentWS.xyz);
                half3 normalWS = TransformTangentToWorld(normalMap, half3x3(IN.tangentWS.xyz, bitangent, IN.normalWS));
                normalWS = NormalizeNormalPerPixel(normalWS);

                float diffuse = dot(normalWS, _SUN_DIR);
                float diffuse01 = (diffuse + 1) * 0.5;

                half4 sky = ComputeSkybox(viewDirWS);

                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                return color * saturate(diffuse) + sky;
            }
            ENDHLSL
        }
    }
}
