Shader "Hidden/VolumetricCloudSystem/VolumetricCloud"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor("Base Color",Color)=(1.0, 0.0, 0.0, 0.3)
        _DensityMultiplier("Density Multiplier", Range(0.0,2.0)) = 0.1
        _MaxOpacityDistance("Max Opacity Distance",Range(0.0,100))=5
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 100
        ZWrite Off
        Cull Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "VolumetricCloudPass"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "../Includes/RaymarchMath.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _DensityMultiplier;
                float _MaxOpacityDistance;
            CBUFFER_END

            float3 _CloudBoundsMin;
            float3 _CloudBoundsMax;
            float4x4 _CameraInvVP;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewVector : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
                OUT.uv = GetFullScreenTriangleTexCoord(IN.vertexID);

                #if UNITY_REVERSED_Z
                float depth = 0.0;
                #else
                float depth = 1.0;
                #endif

                float4 ndc = float4(OUT.uv * 2.0 - 1.0, depth, 1.0);
                float4 worldPos = mul(_CameraInvVP, ndc);
                worldPos /= worldPos.w;

                OUT.viewVector = worldPos.xyz - _WorldSpaceCameraPos.xyz;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 rayOrigin = _WorldSpaceCameraPos.xyz;
                float3 rayDir = normalize(IN.viewVector);

                float2 rayBoxInfo = RayBoxIntersection(rayOrigin, rayDir, _CloudBoundsMin, _CloudBoundsMax);
                float dstToBox = rayBoxInfo.x;
                float dstInsideBox = rayBoxInfo.y;

                if (dstInsideBox <= 0.0) //如果射线在盒子里穿行的距离小于0
                {
                    return half4(0, 0, 0, 0);
                }

                float rawDepth = SampleSceneDepth(IN.uv);
                float4 sceneNDC = float4(IN.uv * 2.0 - 1.0, rawDepth, 1.0);
                float4 sceneWorldPos = mul(_CameraInvVP, sceneNDC);
                sceneWorldPos /= sceneWorldPos.w; //物体的坐标

                //物体到摄像机的距离
                float sceneDistance = length(sceneWorldPos.xyz - rayOrigin);

                if (sceneDistance <= dstToBox) //如果其他物体到镜头的距离 比 这个Shader近
                {
                    return half4(0, 0, 0, 0);
                }
                
                //其他物体距离 与 此Shader的距离 之差
                float maxTravelDistance= sceneDistance - dstToBox;
                //1.其他物体在此Shader内部，则maxTravelDistance<dstInsideBox
                //2.如果在外部，则dstInsideBox<maxTravelDistance
                float actualTravelDistance = min(dstInsideBox,maxTravelDistance);
                
                float dstRatio= actualTravelDistance / (_MaxOpacityDistance + 0.0001);
                float baseOpacity = saturate (dstRatio);
                float density = baseOpacity * _DensityMultiplier;
                density = saturate(density);

                return half4(_BaseColor.rgb, density * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}