#ifndef VOLUMETRIC_CLOUD_MATH_INCLUDED
#define VOLUMETRIC_CLOUD_MATH_INCLUDED

//射线和包围盒的相交检测
//rayOrigin:射线起点 Camera Position
//rayDir:射线方向
//boxMin:AABB盒的最小点坐标
//boxMax:AABB盒的最大点坐标
//返回值（dstTobox:射线起点到包围盒的距离，dstInsideBox:射线在盒子内部穿行的距离）
float2 RayBoxIntersection(float3 rayOrigin, float3 rayDir, float3 boxMin, float3 boxMax)
{
    float3 invDir= 1.0/rayDir;
    float3 t0= (boxMin - rayOrigin) * invDir;
    float3 t1= (boxMax - rayOrigin) * invDir;
    
    //区分进入点和离开点：
    float3 t_min=min(t0,t1);
    float3 t_max=max(t0,t1);
    
    //AABB包围盒的实际进入距离和实际离开距离：
    float dstA=max(max(t_min.x,t_min.y),t_min.z);
    float dstB=min(min(t_max.x,t_max.y),t_max.z);
    
    
    float dstToBox = max(0.0, dstA);
    float dstInsideBox = max(0.0, dstB - dstToBox);
    return float2(dstToBox,dstInsideBox);
}

#endif