#ifndef VOLUMETRIC_CLOUD_MATH_INCLUDED
#define VOLUMETRIC_CLOUD_MATH_INCLUDED

struct CloudVolumeData
{
    float3 boundsMin;
    float density;
    float3 boundsMax;
    float padding;
    float4 color;
};
//射线和包围盒的相交检测
//rayOrigin:射线起点 Camera Position
//rayDir:射线方向
//boxMin:AABB盒的最小点坐标
//boxMax:AABB盒的最大点坐标
//返回值（dstTobox:射线起点到包围盒的距离，dstInsideBox:射线在盒子内部穿行的距离）
float2 RayBoxIntersection(float3 rayOrigin, float3 rayDir, float3 boxMin, float3 boxMax)
{
    float3 invDir = 1.0 / (rayDir+0.0000001);
    float3 t0 = (boxMin - rayOrigin) * invDir;
    float3 t1 = (boxMax - rayOrigin) * invDir;

    //区分进入点和离开点：
    float3 t_min = min(t0, t1);
    float3 t_max = max(t0, t1);

    //AABB包围盒的实际进入距离和实际离开距离：
    float dstA = max(max(t_min.x, t_min.y), t_min.z);
    float dstB = min(min(t_max.x, t_max.y), t_max.z);


    float dstToBox = max(0.0, dstA);
    float dstInsideBox = max(0.0, dstB - dstToBox);
    return float2(dstToBox, dstInsideBox);
}

//计算场景中物体的坐标
float4 GetSceneWorldPos(float2 uv, float4x4 cameraInvVP, float rawDepth)
{
    float4 sceneNDC = float4(uv * 2.0 - 1.0, rawDepth, 1.0);
    float4 sceneWorldPos = mul(cameraInvVP, sceneNDC);
    sceneWorldPos /= sceneWorldPos.w;
    return sceneWorldPos;
}

//计算场景中的物体到相机的真实距离
float GetSceneDepthDistance(float2 uv, float3 rayOrigin, float4x4 cameraInvVP, float rawDepth)
{
    float4 sceneWorldPos = GetSceneWorldPos(uv, cameraInvVP, rawDepth);
    return length(sceneWorldPos.xyz - rayOrigin);
}

//判断一个点是否在AABB盒子中的内部
bool IsPointInsideBox(float3 pos,float3 boxMin,float3 boxMax)
{
    return pos.x >= boxMin.x && pos.x <= boxMax.x &&
           pos.y >= boxMin.y && pos.y <= boxMax.y &&
           pos.z >= boxMin.z && pos.z <= boxMax.z;
}


//步进循环计算(经过步进stepCount的次数后，计算体积雾的最终Alpha值)
//actualTravelDistance:射线实际在其中穿过的距离
//densityMultiplier:浓度控制
//maxOpacityDistance:雾到完全不透明程度所需要射线穿行的距离
half4 CalculateVolumetricAlpha(float3 rayOrigin, float3 rayDir, float dstToBox, float actualTravelDistance,int cloudVolumeCount,StructuredBuffer<CloudVolumeData> cloudVolumes)
{
    int stepCount = 128;
    float stepSize = actualTravelDistance / stepCount;
    float3 rayPos = rayOrigin + rayDir * dstToBox;
    float transmittance=1.0;
    float4 finalColor = float4(0.0,0.0,0.0,0.0); //累加颜色
    
    
    for (int i=0; i<stepCount; i++)
    {
        float currentDensity=0.0f;
        float3 currentColor=float3(0.0,0.0,0.0);
        float colorWeight=0.0;
        
        for (int j=0;j<cloudVolumeCount;j++)
        {
            CloudVolumeData vol = cloudVolumes[j];
            if (IsPointInsideBox(rayPos,vol.boundsMin,vol.boundsMax))
            {
                float localDensity = 1.0 * vol.density;
                
                currentDensity += localDensity;
                currentColor += vol.color.rgb * localDensity;
                colorWeight += localDensity;
            }
        }
        
         if (currentDensity > 0)
         {
             currentColor /= colorWeight;
             float extinction = currentDensity ;
             float stepTransmittance=exp(-extinction * stepSize);
             
             float alpha = (1.0 - stepTransmittance);
             finalColor.rgb += currentColor * alpha * transmittance;
             
             transmittance *= stepTransmittance;
             
             if (transmittance < 0.01)
             {
                 transmittance = 0.0;
                 break;
             }
         }
        rayPos += rayDir * stepSize;
    }
    float finalAlpha = 1.0 - transmittance;
    
    return half4(finalColor.rgb , finalAlpha);
}



#endif
