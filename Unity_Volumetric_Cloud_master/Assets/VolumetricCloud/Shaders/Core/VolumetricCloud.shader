Shader "Hidden/VolumetricCloudSystem/VolumetricCloud"
{
    Properties
    {  }
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
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "VolumetricCloudPass"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "../Includes/RaymarchMath.hlsl"
 
            
            StructuredBuffer<CloudVolumeData> _CloudVolumes;
            int _CloudVolumeCount;
            
            float3 _GlobalBoundsMin;
            float3 _GlobalBoundsMax;
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
                
                float4 worldPos=GetSceneWorldPos(OUT.uv,_CameraInvVP,depth);
                OUT.viewVector = worldPos.xyz - _WorldSpaceCameraPos.xyz;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 rayOrigin = _WorldSpaceCameraPos.xyz;
                float3 rayDir = normalize(IN.viewVector);

                float2 rayBoxInfo = RayBoxIntersection(rayOrigin, rayDir, _GlobalBoundsMin, _GlobalBoundsMax);
                float dstToBox = rayBoxInfo.x;
                float dstInsideBox = rayBoxInfo.y;

                if (dstInsideBox <= 0.0) //如果射线在盒子里穿行的距离小于0
                {
                    return half4(0, 0, 0, 0);
                }

                float rawDepth = SampleSceneDepth(IN.uv);
                float sceneDistance=GetSceneDepthDistance(IN.uv,rayOrigin,_CameraInvVP,rawDepth);

                if (sceneDistance <= dstToBox) //如果其他物体到镜头的距离 比 这个Shader近
                {
                    return half4(0, 0, 0, 0);
                }
                
                //其他物体距离 与 此Shader的距离 之差
                float maxTravelDistance= sceneDistance - dstToBox;
                //1.其他物体在此Shader内部，则maxTravelDistance<dstInsideBox
                //2.如果在外部，则dstInsideBox<maxTravelDistance
                float actualTravelDistance = min(dstInsideBox,maxTravelDistance);
                
                
                
                half4 targetColor=CalculateVolumetricAlpha(rayOrigin,rayDir,dstToBox,actualTravelDistance,_CloudVolumeCount,_CloudVolumes);

                return targetColor;
            }
            ENDHLSL
        }
    }
}