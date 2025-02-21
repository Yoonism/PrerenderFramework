#pragma once

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

#define H_PI (3.1415926535897932384626433832795)
#define H_PI_HALF (1.5707963267948966192313216916398)
#define H_PI_QUARTER   0.78539816339744830961566084581988

float Random(float2 Seed)
{
	float a = 12.9898;
	float b = 78.233;
	float c = 43758.5453;
	float dt = dot(Seed.xy, float2(a, b));
	float sn = fmod(dt, 3.14);
	return frac(sin(sn) * c);
}

//------------------------ Hash Random functions.

uint Hash1(uint x)
{
	x += (x << 10u);
	x ^= (x >>  6u);
	x += (x <<  3u);
	x ^= (x >> 11u);
	x += (x << 15u);
	return x;
}

uint Hash1Mutate(inout uint h)
{
	uint res = h;
	h = Hash1(h);
	return res;
}

uint Hash_Combine(uint x, uint y)
{
	static const uint M = 1664525u, C = 1013904223u;
	uint seed = (x * M + y + C) * M;

	seed ^= (seed >> 11u);
	seed ^= (seed << 7u) & 0x9d2c5680u;
	seed ^= (seed << 15u) & 0xefc60000u;
	seed ^= (seed >> 18u);
	return seed;
}

uint Hash2(uint2 v)
{
	return Hash_Combine(v.x, Hash1(v.y));
}

uint Hash3(uint3 v)
{
	return Hash_Combine(v.x, Hash2(v.yz));
}

float UintToFloat01(uint h)
{
	static const uint mantissaMask = 0x007FFFFFu;
	static const uint one = 0x3F800000u;

	h &= mantissaMask;
	h |= one;

	float  r2 = asfloat( h );
	return r2 - 1.0;
}

float3 HClipRadiance(float3 Radiance, float MaxLuminance)
{
	if (AnyIsNaN(Radiance) || AnyIsInf(Radiance))
		Radiance = 0.0f;
    
	Radiance = RgbToHsv(Radiance);
	Radiance.z = clamp(Radiance.z, 0.0, MaxLuminance);
	Radiance = HsvToRgb(Radiance);

	return Radiance;
}

float3x3 RotFromToMatrix( float3 from, float3 to )
{
    const float e       = dot(from, to);
    const float f       = abs(e);
    const float3 v      = cross( from, to );
    const float h       = (1.0)/(1.0 + e);
    const float hvx     = h * v.x;
    const float hvz     = h * v.z;
    const float hvxy    = hvx * v.y;
    const float hvxz    = hvx * v.z;
    const float hvyz    = hvz * v.y;

    float3x3 mtx;
    mtx[0][0] = e + hvx * v.x;
    mtx[0][1] = hvxy - v.z;
    mtx[0][2] = hvxz + v.y;

    mtx[1][0] = hvxy + v.z;
    mtx[1][1] = e + h * v.y * v.y;
    mtx[1][2] = hvyz - v.x;

    mtx[2][0] = hvxz - v.y;
    mtx[2][1] = hvyz + v.x;
    mtx[2][2] = e + hvz * v.z;

    return mtx;
}

float HFastSqrt(float x)
{
	return (asfloat(0x1fbd1df5 + (asint(x) >> 1)));
}

float HFastACos( float inX )
{ 
	float pi = 3.141593;
	float half_pi = 1.570796;
	float x = abs(inX); 
	float res = -0.156583 * x + half_pi;
	res *= HFastSqrt(1.0 - x);
	return (inX >= 0) ? res : pi - res;
}


float CalculateHaltonNumber(in uint index, in uint base)
{
	float f      = 1.0f;
	float result = 0.0f;

	for (uint i = index; i > 0;)
	{
		f /= base;
		result = result + f * (i % base);
		i = uint(i / float(base));
	}

	return result;
}

float2 CalculateHaltonSequence(in uint index)
{
	return float2(CalculateHaltonNumber((index & 0xFFu) + 1, 2),
				  CalculateHaltonNumber((index & 0xFFu) + 1, 3));
}

float3x3 ConstructTBN(float3 Normal)
{	
	float3 CrossZ = cross(Normal, float3(0.0, 0.0, 1.0));
	float3 CrossY = cross(Normal, float3(0.0, 1.0, 0.0));
	
	float3 Tangent = length(CrossZ) > length(CrossY) ? CrossZ : CrossY;
	Tangent = normalize(Tangent);
	
	float3 Bitangent = normalize(cross(Tangent, Normal));
	
	return float3x3(Tangent, Bitangent, Normal);
}

float3 OrientedOctahedronToDirectionFast(float2 Coord, float3 Normal)
{
	Coord = 2.0f * Coord - 1.0f;
	
	Coord = float2(Coord.x + Coord.y, Coord.x - Coord.y) * 0.5;
	float3 CellDirection = float3(Coord, 1.0 - abs(Coord.x) - abs(Coord.y));
	CellDirection = normalize(CellDirection);
	CellDirection = mul(CellDirection, ConstructTBN(Normal));
	
	return normalize(CellDirection);
}

float2 DirectionToOrientedOctahedronFast(float3 Direction, float3 Normal)
{
	Direction = mul(ConstructTBN(Normal.xyz), Direction);
	Direction = normalize(Direction);
	Direction.z = saturate(Direction.z);
	
	float2 Coord = Direction.xy * (1.0 / (abs(Direction.x) + abs(Direction.y) + Direction.z));
	Coord = float2(Coord.x + Coord.y, Coord.x - Coord.y);
	
	Coord = 0.5 * Coord + 0.5f;
	
	return Coord * 4;
}

float3 OrientedOctahedronToDirection(float2 Coord, float3 Normal)
{
	Coord = 2.0f * Coord - 1.0f;
	
	Coord = float2(Coord.x + Coord.y, Coord.x - Coord.y) * 0.5f;
	float2 AbsCoord = abs(Coord);
	float Distance = 1.0f - (AbsCoord.x + AbsCoord.y); 
	float Radius = 1.0f - abs(Distance);
	float Phi = (Radius == 0.0f) ? 0.0f : H_PI_QUARTER * ((AbsCoord.y - AbsCoord.x) / Radius + 1.0f);
	float RadiusSqr = Radius * Radius;
	float SinTheta = Radius * sqrt(2.0f - RadiusSqr);
	float SinPhi, CosPhi;
	sincos(Phi, SinPhi, CosPhi);
	float x = SinTheta * sign(Coord.x) * CosPhi;
	float y = SinTheta * sign(Coord.y) * SinPhi;
	float z = sign(Distance) * (1.0f - RadiusSqr);
	
	float3 CellDirection = float3(x, y, z);
	CellDirection = normalize(CellDirection);
	
	//CellDirection = mul(CreateTBN(Normal), CellDirection);
	CellDirection = mul(CellDirection, ConstructTBN(Normal));
	
	return normalize(CellDirection);
}

float2 DirectionToOrientedOctahedron(float3 Direction, float3 Normal)
{
	Direction = mul(ConstructTBN(Normal.xyz), Direction);
	Direction = normalize(Direction);
	
	float3 AbsDir = abs(Direction);
	float Radius = sqrt(1.0f - AbsDir.z);
	float Epsilon = 5.42101086243e-20; // 2^-64 (this avoids 0/0 without changing the rest of the mapping)
	float x = min(AbsDir.x, AbsDir.y) / (max(AbsDir.x, AbsDir.y) + Epsilon);
	
	// Coefficients for 6th degree minimax approximation of atan(x)*2/pi, x=[0,1].
	const float t1 = 0.406758566246788489601959989e-5f;
	const float t2 = 0.636226545274016134946890922156f;
	const float t3 = 0.61572017898280213493197203466e-2f;
	const float t4 = -0.247333733281268944196501420480f;
	const float t5 = 0.881770664775316294736387951347e-1f;
	const float t6 = 0.419038818029165735901852432784e-1f;
	const float t7 = -0.251390972343483509333252996350e-1f;
	
	// Polynomial approximation of atan(x)*2/pi
	float Phi = t6 + t7 * x;
	Phi = t5 + Phi * x;
	Phi = t4 + Phi * x;
	Phi = t3 + Phi * x;
	Phi = t2 + Phi * x;
	Phi = t1 + Phi * x;
		
	Phi = (AbsDir.x >= AbsDir.y) ? Phi : 1.0f - Phi;
	float t = Phi * Radius;
	float s = Radius - t;
	float2 Coord = float2(s, t);
	Coord *= sign(Direction).xy;
	Coord = float2(Coord.x + Coord.y, Coord.x - Coord.y);
	
	Coord = 0.5 * Coord + 0.5f;
	//Coord = clamp(Coord, 0.0f, 0.99f);
	
	return Coord * 4;
}


// This simplified version assume that we care about the result only when we are inside the box
float RayBoxIntersect(float3 start, float3 dir, float3 boxMin, float3 boxMax)
{
	float3 invDir = rcp(dir);

	// Find the ray intersection with box plane
	float3 rbmin = (boxMin - start) * invDir;
	float3 rbmax = (boxMax - start) * invDir;

	float3 rbminmax = (dir > 0.0) ? rbmax : rbmin;

	return min(min(rbminmax.x, rbminmax.y), rbminmax.z);
}

float2 LineBoxIntersect(float3 RayOrigin, float3 RayEnd, float3 BoxMin, float3 BoxMax)
{
	float3 InvRayDir = 1.0f / (RayEnd - RayOrigin);
	
	//find the ray intersection with each of the 3 planes defined by the minimum extrema.
	float3 FirstPlaneIntersections = (BoxMin - RayOrigin) * InvRayDir;
	//find the ray intersection with each of the 3 planes defined by the maximum extrema.
	float3 SecondPlaneIntersections = (BoxMax - RayOrigin) * InvRayDir;
	//get the closest of these intersections along the ray
	float3 ClosestPlaneIntersections = min(FirstPlaneIntersections, SecondPlaneIntersections);
	//get the furthest of these intersections along the ray
	float3 FurthestPlaneIntersections = max(FirstPlaneIntersections, SecondPlaneIntersections);

	float2 BoxIntersections;
	//find the furthest near intersection
	BoxIntersections.x = max(ClosestPlaneIntersections.x, max(ClosestPlaneIntersections.y, ClosestPlaneIntersections.z));
	//find the closest far intersection
	BoxIntersections.y = min(FurthestPlaneIntersections.x, min(FurthestPlaneIntersections.y, FurthestPlaneIntersections.z));
	//clamp the intersections to be between RayOrigin and RayEnd on the ray
	return saturate(BoxIntersections);
}

// Sampling.hlsl  ------------------------------->
real2 HSampleDiskCubic(real u1, real u2)
{
	real r   = u1;
	real phi = TWO_PI * u2;

	real sinPhi, cosPhi;
	sincos(phi, sinPhi, cosPhi);

	return r * real2(cosPhi, sinPhi);
}

//-------------------------------------------------------------->