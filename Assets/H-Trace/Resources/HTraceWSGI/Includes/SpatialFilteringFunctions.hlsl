#pragma once
float ProbePlaneWeighting(float4 Plane, float3 WorldPosSample, float DepthLinearCenter, float Multiplier)
{
    float PlaneDistance = abs(dot(float4(WorldPosSample, -1), Plane));
    float DepthDifference = PlaneDistance / DepthLinearCenter;
    float PlaneWeight = exp2(-100.0f * Multiplier * (DepthDifference * DepthDifference));
    return PlaneWeight;
}

float ProbePlaneWeighting2(float3 Normal, float3 WorldPosCenter, float3 WorldPosSample, float DepthLinearCenter, float Multiplier)
{
    float PlaneDistance = abs(dot((WorldPosCenter - WorldPosSample), Normal.xyz)); 
    float DepthDifference = PlaneDistance / DepthLinearCenter;
    float PlaneWeight = exp2(-100.0f * Multiplier * (DepthDifference * DepthDifference));
    return PlaneWeight;
}

float ProbePlaneWeighting3(float3 Normal, float3 WorldPos, float3 WorldPosPrev, float DepthLinearCenter)
{
    // Plane-based rejection
    float4 ScenePlane = float4(Normal, dot(WorldPos, Normal));
    float PlaneDistance = abs(dot(WorldPosPrev - WorldPos, Normal));
    float RelativeDepthDifference = PlaneDistance / DepthLinearCenter;
    return exp2(-100000 * (RelativeDepthDifference * RelativeDepthDifference));
}

float ProbeDepthWeighting(float DepthRawCenter, float DepthRawSample, float Multiplier)
{
    if (DepthRawCenter <= 0)
        return 1.0f;
    //
    // DepthRawCenter = Linear01Depth(DepthRawCenter, _ZBufferParams);
    // DepthRawSample = Linear01Depth(DepthRawSample, _ZBufferParams);

    //return exp(-abs(DepthRawCenter - DepthRawSample) * DepthRawCenter * 4.0f);

     //return max(0.0, 1.0 - abs(DepthRawSample - DepthRawCenter) * 1);
    
    float DepthDifference = abs(DepthRawCenter - DepthRawSample);
    float RelativeDepthDifference = DepthDifference / DepthRawCenter;
    float DepthWeight = DepthRawSample >= 0 ? exp2(-10.0f * Multiplier * (RelativeDepthDifference * RelativeDepthDifference)) : 0;
    return DepthWeight;
}



float ProbeAngleWeighting(float DistanceCenter, float DistanceSample, float3 WorldPosCenter, float3 WorldPosSample, float3 Direction, float MaxAngle)
{
    // if (DistanceCenter > 0)
    //     DistanceSample = min(DistanceSample, DistanceCenter);
    //
    // float3 HitPosSample = WorldPosSample + Direction * DistanceSample;
    // float3 ToSampleHit = HitPosSample - WorldPosCenter;
    // float Angle = FastACos(dot(ToSampleHit, Direction) / length(ToSampleHit));
    // float AngleWeight = 1 - saturate(Angle / MaxAngle);
    //
    // return AngleWeight;
}


float PixelDepthWeighting(float DeptLinearCenter, float DepthLinearSample, float Multiplier)
{
    float DepthDifference = abs(DepthLinearSample - DeptLinearCenter);
    float RelativeDepthDifference = DepthDifference / DeptLinearCenter;
    float DepthWeightNear = exp2(-100.0f * Multiplier * (RelativeDepthDifference * RelativeDepthDifference));
    return DepthWeightNear;
}

// ------------------------ SPATIAL FILTERING FUNCTIONS ------------------------

float PlaneWeighting(float3 WorldPosCenter, float3 WorldPosSample, float3 NormalCenter, float3 NormalSample, float FilterStrength)
{
    float3 SampleDistance = WorldPosCenter - WorldPosSample;
    float SampleDistanceDouble = dot(SampleDistance, SampleDistance);
    float PlaneError = max(abs(dot(SampleDistance, NormalSample)), abs(dot(SampleDistance, NormalCenter)));
    float PlaneWeight = pow(max(0.0, 1.0 - 2.0 * PlaneError / sqrt(SampleDistanceDouble)), FilterStrength);
    return PlaneWeight;
}

float DepthWeighting(float DepthCenter, float DepthSample, float FilterStrength)
{
    float DepthDifference = abs(DepthSample - DepthCenter);
    float RelativeDepthDifference = DepthDifference / DepthCenter;
    float DepthWeight = exp2(-100 * FilterStrength * (RelativeDepthDifference * RelativeDepthDifference));
    return DepthWeight;
}

float NormalWeighting(float3 NormalCenter, float3 NormalSample, float FilterStrength)
{
    float NormalWeight = saturate(dot(normalize(NormalCenter), normalize(NormalSample)));
    NormalWeight = pow(NormalWeight, FilterStrength);
    return NormalWeight;
}

float sqr(float value)
{
    return value * value;
}

float GaussianWeighting(float Radius, float Sigma)
{
    return exp(-sqr(Radius / Sigma));
}