#pragma once
#include "../Headers/HMain.hlsl"
#include "../Headers/HMath.hlsl"

int _OctahedralSize;
int _ProbeSize;

// ------------------------ RAY SETUP FUNCTIONS ------------------------

// Approximates object thickness in screen-space
float AdaptiveThicknessSearch(H_TEXTURE(_DepthPyramid), float3 PositionWS, float3 RayDirectionWS)
{
    float StepSize = 0.01;
    float StepCount = 4;
    float InsideSurfaceCount = StepCount;
    
    bool IsOffscreen = false;
    for (float i = 0; i < StepCount + 1; i++)
    {
        // Move along the ray with fixed steps
        PositionWS += RayDirectionWS * StepSize;
        float3 PositionNDC = ComputeNormalizedDeviceCoordinatesWithZ(PositionWS, UNITY_MATRIX_VP).xyz;
        
        if (all(PositionNDC.xy > 0) && all(PositionNDC.xy < 1))
        {
            // Sample depth along the ray
            float Depth = H_LOAD(_DepthPyramid, PositionNDC.xy * _ScreenSize.xy).x;

            // Decrement thickness counter each time the ray is above surface
            if (PositionNDC.z > Depth)
                InsideSurfaceCount--;
        }
        else
        {
            IsOffscreen = true;
            break;
        }
    }

    // Occluder thickness is decreased by the number of times the ray appeared above surface
    return IsOffscreen ? 0.03f : lerp(0.015f, 0.05f, saturate(InsideSurfaceCount / StepCount));
}

// Calculates ray origin (with bias) and ray direction in NDC coordinates
void GetRayOriginAndDirectionNDC(float Depth, float MaxDistance, float2 ProbeCoordNDC, float3 PositionWS, float3 RayDirectionWS, float3 GeometryNormalWS, inout float3 RayStartPositionNDC, inout float3 RayDirectionNDC)
{
    float3 ViewDirectionWS = GetWorldSpaceNormalizeViewDir(PositionWS);
    
    // Bias ray origin in world space
    float3 PositionBiasedWS;
    {
        // Calculate view direction bias // TODO: may create artifacrs at grazing angles. Seems unnecessary.
        float3 CameraPositionWS = GetCurrentViewPosition();
        PositionBiasedWS = PositionWS; // CameraPositionWS + (PositionWS - CameraPositionWS) * (1 - 0.001f * rcp(max(dot(GeometryNormalWS, ViewDirectionWS), FLT_EPS)));

        // float3 NormalForBias = GeometryNormalWS;
        float3 NormalForBias = dot(GeometryNormalWS, RayDirectionWS) < 0 ? -GeometryNormalWS : GeometryNormalWS;

        // Calculate normal bias
        float2 CornerCoordNDC = ProbeCoordNDC + 0.5f * _ScreenSize.zw * float(_OctahedralSize);
        float3 CornerPositionWS = ComputeWorldSpacePosition(CornerCoordNDC.xy, Depth, UNITY_MATRIX_I_VP);
        float NormalBias = abs(dot(CornerPositionWS - PositionWS, NormalForBias)) * 2.0f;

        // This can push the ray origin off-screen causing black pixels on the border
        PositionBiasedWS += NormalForBias * NormalBias;
    }

    // Calculate ray start position in screen space
    RayStartPositionNDC = ComputeNormalizedDeviceCoordinatesWithZ(PositionBiasedWS, UNITY_MATRIX_VP).xyz;

    // Calculate ray end clipped position in screen space
    float3 RayEndPositionNDC;
    {
        // Calculate clipped ray distance in world space
        float MaxRayDistanceWS = 100.0f;
        float3 RayDirectionVS = TransformWorldToViewDir(-RayDirectionWS, true);
        float SceneDepth = LinearEyeDepth(PositionBiasedWS, UNITY_MATRIX_V);
        float RayClippedDistanceWS = RayDirectionVS.z < 0.0 ? min(-0.99f * SceneDepth / RayDirectionVS.z, MaxRayDistanceWS) : MaxRayDistanceWS;

        // Calculate ray end position in screen space
        RayEndPositionNDC.xyz = ComputeNormalizedDeviceCoordinatesWithZ(PositionBiasedWS + RayDirectionWS * RayClippedDistanceWS, UNITY_MATRIX_VP).xyz;

        // Recalculate ray end position where it leaves the screen
        float2 ScreenEdgeIntersections = LineBoxIntersect(RayStartPositionNDC, RayEndPositionNDC, 0, 1);
        RayEndPositionNDC = RayStartPositionNDC + (RayEndPositionNDC - RayStartPositionNDC) * ScreenEdgeIntersections.y;
    }

    // Ray direction in screen space
    RayDirectionNDC = RayEndPositionNDC - RayStartPositionNDC;
}


// ------------------------ RAY TRAVERSAL FUNCTIONS ------------------------
bool AdvanceRay(float3 RayOrigin, float3 RayDirection, float3 RayDirectionInverse, float2 CurrentMipLevelPosition, float2 CurrentMipResolutionInverse, float2 OffsetFloor, float2 OffsetUV, float SurfaceZ, inout float3 RayPosition, inout float CurrentIntersectionTime)
{
    // Create boundary planes
    float2 PlaneXY = floor(CurrentMipLevelPosition) + OffsetFloor;
    PlaneXY = PlaneXY * CurrentMipResolutionInverse + OffsetUV;
    float3 BoundaryPlanes = float3(PlaneXY, SurfaceZ);

    // Intersect ray with the half box that is pointing away from the ray RayOrigin.
    float3 IntersectionTimes = BoundaryPlanes * RayDirectionInverse - RayOrigin * RayDirectionInverse;

    // Prevent using z plane when shooting out of the depth buffer.
    IntersectionTimes.z = RayDirection.z < 0 ? IntersectionTimes.z : FLT_MAX;
    
    // Choose nearest intersection with a boundary.
    float ClosestIntersectionTime = min(min(IntersectionTimes.x, IntersectionTimes.y), IntersectionTimes.z);
    
    // Larger z means closer to the camera.
    bool AboveSurface = SurfaceZ < RayPosition.z;

    // Decide whether we are able to advance the ray until we hit the xy boundaries or if we had to clamp it at the surface.
    bool SkippedTile = asuint(ClosestIntersectionTime) != asuint(IntersectionTimes.z) && AboveSurface; 

    // Make sure to only advance the ray if we're still above the surface.
    CurrentIntersectionTime = AboveSurface ? ClosestIntersectionTime : CurrentIntersectionTime;

    // Advance ray
    RayPosition = RayOrigin + CurrentIntersectionTime * RayDirection;

    return SkippedTile;
}

bool HierarchicalRaymarch(H_TEXTURE(_DepthPyramid), float3 RayOrigin, float3 RayDirection, uint StepCount, inout float3 HitPositionNDC, inout float3 LastAboveSurfacePositionNDC)
{
    float3 RayDirectionInverse = RayDirection != float3(0.0f, 0.0f, 0.0f) ? float3(1.0f, 1.0f, 1.0f) / RayDirection : float3(FLT_MAX, FLT_MAX, FLT_MAX);

    int BaseMipLevel = 0;
    int CurrentMipLevel = BaseMipLevel;
    
    float2 CurrentMipResolution = _ScreenSize.xy / pow(2.0, CurrentMipLevel);
    float2 CurrentMipResolutionInverse = 1.0f / CurrentMipResolution;

    // Offset to the bounding boxes uv space to intersect the ray with the center of the next pixel. This means we ever so slightly over shoot into the next region. 
    float2 OffsetUV = 0.005f * exp2(BaseMipLevel) / _ScreenSize.xy;
    OffsetUV.x = RayDirection.x < 0.0f ? -OffsetUV.x : OffsetUV.x;
    OffsetUV.y = RayDirection.y < 0.0f ? -OffsetUV.y : OffsetUV.y;

    // Offset applied depending on current mip resolution to move the boundary to the left/right upper/lower border depending on ray RayDirection.
    float2 OffsetFloor;
    OffsetFloor.x = RayDirection.x < 0.0f ? 0 : 1.0f;
    OffsetFloor.y = RayDirection.y < 0.0f ? 0 : 1.0f;
    
    float CurrentIntersectionTime;
    float3 RayPosition;

    // Step out of the current tile to avoid self intersection
    {
        float2 CurrentMipLevelPosition = CurrentMipResolution * RayOrigin.xy;

        // Intersect ray with the half box that is pointing away from the ray RayOrigin.
        float2 PlaneXY = floor(CurrentMipLevelPosition) + OffsetFloor;
        PlaneXY = PlaneXY * CurrentMipResolutionInverse + OffsetUV;

        // o + d * t = p' => t = (p' - o) / d
        float2 IntersectionTimes = PlaneXY * RayDirectionInverse.xy - RayOrigin.xy * RayDirectionInverse.xy;
        CurrentIntersectionTime = min(IntersectionTimes.x, IntersectionTimes.y);
        RayPosition = RayOrigin + CurrentIntersectionTime * RayDirection;
    }
    
    float LastAboveSurfaceTime = CurrentIntersectionTime;
    
    uint i = 0;
    while (i < StepCount && CurrentMipLevel >= BaseMipLevel && CurrentIntersectionTime < 1.0f)
    {
        float2 CurrentMipLevelPosition = CurrentMipResolution * RayPosition.xy;
        float SurfaceZ = H_LOAD_LOD(_DepthPyramid, CurrentMipLevelPosition, CurrentMipLevel).x;
        
        bool SkippedTile = AdvanceRay(RayOrigin, RayDirection, RayDirectionInverse, CurrentMipLevelPosition, CurrentMipResolutionInverse, OffsetFloor, OffsetUV, SurfaceZ, RayPosition, CurrentIntersectionTime);
        
        if (!SkippedTile || CurrentMipLevel < 7)
        {   
            CurrentMipLevel += SkippedTile ? 1 : -1;
            CurrentMipResolution *= SkippedTile ? 0.5 : 2.0f;
            CurrentMipResolutionInverse *= SkippedTile ? 2.0f : 0.5;
        }

        LastAboveSurfaceTime = SkippedTile ? CurrentIntersectionTime : LastAboveSurfaceTime;
        
        i++;
    }

    HitPositionNDC = RayOrigin + CurrentIntersectionTime * RayDirection;
    LastAboveSurfacePositionNDC = CurrentMipLevel > -1 ? RayOrigin + LastAboveSurfaceTime * RayDirection : HitPositionNDC;

    return i <= StepCount ? true : false;
}