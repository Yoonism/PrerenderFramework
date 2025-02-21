#pragma once

float3 HFastTonemap(float3 Color)
{
	float T = rcp(1.0f + max(max(Color.r, Color.g), Color.b));
	return saturate(Color * float(T));
}

float3 HFastTonemapInverse(float3 Color)
{
	float T = rcp(max(float(1.0 / 256.0), saturate(float(1.0 / 1.0f) - max(max(Color.r, Color.g), Color.b)) * float(1.0 / 1.0f)));
	return Color * T;
}

uint PackTonemappedColor24bit(float3 Color)
{
	Color = Color * rcp(max(max(Color.r, Color.g), Color.b) + 1.0f);
	uint RadianceR = uint(Color.r * 255.0f + 0.5f) & 0xFF;
	uint RadianceG = uint(Color.g * 255.0f + 0.5f) & 0xFF;
	uint RadianceB = uint(Color.b * 255.0f + 0.5f) & 0xFF;

	return (RadianceR << 16) | (RadianceG << 8)  | (RadianceB << 0);
}

float3 UnpackTonemappedColor24bit(uint ColorPacked)
{
	float3 Color;
	Color.r = ((ColorPacked >> 16) & 0xFF) / 255.0f;
	Color.g = ((ColorPacked >>  8) & 0xFF) / 255.0f;
	Color.b = ((ColorPacked >>  0) & 0xFF) / 255.0f;
    
	return Color * rcp(1.0 - max(max(Color.r, Color.g), Color.b)); 
}

uint PackVoxelColor(float3 Color, bool IsEmissive)
{
	uint3 ColorPacked;
	if (IsEmissive)
	{
		Color = HFastTonemap(Color);
		ColorPacked.r = uint(Color.r * 255.0f + 0.5f) & 0xFF;
		ColorPacked.g = uint(Color.g * 127.0f + 0.5f) & 0x7F;
		ColorPacked.b = uint(Color.b * 127.0f + 0.5f) & 0x7F;
		return (ColorPacked.r << 15) | (ColorPacked.g << 8) | (ColorPacked.b << 1) | IsEmissive;
	}
	
	ColorPacked.r = uint(Color.x * 255.0f + 0.5f) & 0xFF;
	ColorPacked.g = uint(Color.y * 127.0f + 0.5f) & 0x7F;
	ColorPacked.b = uint(Color.z * 127.0f + 0.5f) & 0x7F;

	return (ColorPacked.r << 15) | (ColorPacked.g << 8) | (ColorPacked.b << 1) | IsEmissive;
}

float3 UnpackVoxelColor(uint Input, out bool IsEmissive)
{
	IsEmissive = Input & 0x1;

	float3 Color;
	
	if (IsEmissive)
	{
		// float H = ((Input >> 0) & 0x3F) / 63.0f;
		// float S = ((Input >> 6) & 0x3F) / 63.0f; 
		// float V = f16tof32((Input >> 12) << 5);		
		// return HsvToRgb(float3(H, S, V));
		
		Color.r = ((Input >> 15) & 0xFF) / 255.0f;
		Color.g = ((Input >>  8) & 0x7F) / 127.0f;
		Color.b = ((Input >>  1) & 0x7F) / 127.0f;
		return Color = HFastTonemapInverse(Color); 
	}
	
	Color.r = float((Input >> 15) & 0xFF) / 255.0f;
	Color.g = float((Input >>  8) & 0x7F) / 127.0f;
	Color.b = float((Input >>  1) & 0x7F) / 127.0f;
	//Color = SRGBToLinear(Color);
	
	return Color;
	
}

uint PackToR11G10B10A1f(float3 InputF3, float InputB)
{
	uint r = (f32tof16(InputF3.x) << 17) & 0xFFE00000; // 11111111.11100000.00000000.00000000
	uint g = (f32tof16(InputF3.y) << 6) & 0x001FF800;  // 00000000.00011111.11111000.00000000
	uint b = (f32tof16(InputF3.z) >> 4) & 0x000007FE;  // 00000000.00000000.00000111.11111110
	uint a = InputB == 1.0f ? 0x00000001 : 0x00000000;
	return r | g | b | a;
}

float3 UnpackFromR11G10B10A1f(uint InputU, inout bool InoutB)
{
	float r = f16tof32((InputU >> 17) & 0x7FF0); // 00000000.00000000.01111111.11110000
	float g = f16tof32((InputU >> 6) & 0x7FF0);  // 00000000.00000000.01111111.11110000
	float b = f16tof32((InputU << 4) & 0x7FE0);  // 00000000.00000000.01111111.11110000
	InoutB = (InputU & 0x00000001) == 0x00000001 ? 1.0f : 0.0f;
	return float3(r, g, b);
}

float3 UnpackFromR11G10B10A1f(uint InputU)
{
	float r = f16tof32((InputU >> 17) & 0x7FF0); // 00000000.00000000.01111111.11110000
	float g = f16tof32((InputU >> 6) & 0x7FF0);  // 00000000.00000000.01111111.11110000
	float b = f16tof32((InputU << 4) & 0x7FE0);  // 00000000.00000000.01111111.11110000
	return float3(r, g, b);
}


uint3 PackCacheRadiance(float3 RadianceFull, float3 RadianceNear)
{
	uint PackedR = (uint(f32tof16(RadianceFull.r)) << 16) | uint(f32tof16(RadianceNear.r));
	uint PackedG = (uint(f32tof16(RadianceFull.g)) << 16) | uint(f32tof16(RadianceNear.g));
	uint PackedB = (uint(f32tof16(RadianceFull.b)) << 16) | uint(f32tof16(RadianceNear.b));

	return uint3(PackedR, PackedG, PackedB);
}

float3 UnpackCacheRadianceFull(uint3 RadiancePacked)
{
	float3 Radiance;
	Radiance.r = f16tof32(((RadiancePacked.r) >> 16) & 0xFFFF);
	Radiance.g = f16tof32(((RadiancePacked.g) >> 16) & 0xFFFF);
	Radiance.b = f16tof32(((RadiancePacked.b) >> 16) & 0xFFFF);
	
	return Radiance;
}

float3 UnpackCacheRadianceNear(uint3 RadiancePacked)
{
	float3 Radiance;
	Radiance.r = f16tof32((RadiancePacked.r) & 0xFFFF);
	Radiance.g = f16tof32((RadiancePacked.g) & 0xFFFF);
	Radiance.b = f16tof32((RadiancePacked.b) & 0xFFFF);
	
	return Radiance;
}

uint PackVoxelNormalIndex(float3 Normal)
{
	float3 AbsDirection = abs(Normal);
	float MaxDirection = max(max(AbsDirection.x, AbsDirection.y), AbsDirection.z);

	if (MaxDirection == AbsDirection.z)
	{
		uint Sign = saturate(sign(Normal.z));
		return 4 + Sign;
	}
	else
	{
		uint Max = step(AbsDirection.x, AbsDirection.y);
		uint Sign = saturate(sign(Normal[Max]));
		return 2 * Max + Sign;
	}

	// Anisotropy Layout:
	// X- == 0 (Buffer_B[0] / .x)
	// X+ == 1 (Buffer_B[1] / .y)
	// Y- == 2 (Buffer_B[2] / .z)
	// Y+ == 3 (Buffer_B[3] / .w)
	// Z- == 4 (Buffer_A[0] / .x)
	// Z+ == 5 (Buffer_A[1] / .y)
	
	// This is the same but in a more illustrative (and probably slow) way:
	
	// if (MaxDirection == AbsDirection.x)
	// {
	// 	if (Normal.x < 0)
	// 		return 0;
	// 	else
	// 		return 1;
	// }
	// else if (MaxDirection == AbsDirection.y)
	// {
	// 	if (Normal.y < 0)
	// 		return 2;
	// 	else
	// 		return 3; 
	// }
	// else 
	// {
	// 	if (Normal.z < 0)
	// 		return 4;
	// 	else
	// 		return 5; 
	// }
}

float3 UnpackVoxelNormalIndex(uint NormalIndex)
{
	float3 VoxelNormals[6] = {float3(-1, 0, 0), float3(1, 0, 0), float3(0, -1, 0), float3(0, 1, 0), float3(0, 0, -1), float3(0, 0, 1)};
	return VoxelNormals[NormalIndex];
}

uint2 PackCacheHitPayload(float3 HitOffset, float3 RayDirection, float3 HitNormal, uint3 HitCoord, float SkyOcclusion, uint HashRank, bool HitFound, bool HitFlag)
{
	uint PackedX;

	// If hit found we pack an offset from voxel coord
	if (HitFound)
	{
		float HitOffsetLength = length(HitOffset);
		HitOffset = -normalize(HitOffset) * 0.5f + 0.5f;
		
		uint OffsetX = uint(HitOffset.x * 127.0f + 0.5f) & 0x7F;
		uint OffsetY = uint(HitOffset.y * 127.0f + 0.5f) & 0x7F;
		uint OffsetZ = uint(HitOffset.z * 127.0f + 0.5f) & 0x7F;

		uint OffsetLength = uint(HitOffsetLength * 255.0f + 0.5f) & 0xFF;

		PackedX = (OffsetX << 25) | (OffsetY << 18) | (OffsetZ << 11) | (OffsetLength << 3) | (HitFlag << 2) | HitFound;
	}
	else
	{
		RayDirection = RayDirection * 0.5 + 0.5;
		uint RayDirectionX = uint(RayDirection.x * 255.0f + 0.5f) & 0xFF;
		uint RayDirectionY = uint(RayDirection.y * 255.0f + 0.5f) & 0xFF;
		uint RayDirectionZ = uint(RayDirection.z * 255.0f + 0.5f) & 0xFF;
		
		uint SkyOcclusionPacked = uint(SkyOcclusion * 31.0f + 0.5f) & 0x1F;
		
		PackedX = (RayDirectionX << 24) | (RayDirectionY << 16) | (RayDirectionZ << 8)  | (SkyOcclusionPacked << 3) | (HitFlag << 2);
	}
	
	uint HitNormalIndex = (PackVoxelNormalIndex(HitNormal) & 0x7) << 2;
	
	uint CoordX = ((HitCoord.x - 1) & 0x1FF) << 5;					
	uint CoordY = ((HitCoord.y - 1) & 0x1FF) << 14;					
	uint CoordZ = ((HitCoord.z - 1) & 0x1FF) << 23;					
	
	uint PackedY = CoordX | CoordY | CoordZ | HitNormalIndex | HashRank;
	
	return uint2(PackedX, PackedY); 
}

bool UnpackCacheHitPayload(uint2 PayloadPacked, inout float3 RayDirection, inout float3 HitOffset, inout float3 HitNormal, inout uint3 HitCoord, inout float SkyOcclusion, inout uint HashRank, inout bool HitFlag)
{
	HitFlag = (PayloadPacked.x >> 2) & 0x1;
	bool IsHit = PayloadPacked.x & 0x1;

	HitOffset = 0;
	RayDirection = 0;
	SkyOcclusion = 0;

	if (IsHit)
	{
		HitOffset.x = float((PayloadPacked.x >> 25) & 0x7F) / 127.0f;
		HitOffset.y = float((PayloadPacked.x >> 18) & 0x7F) / 127.0f;
		HitOffset.z = float((PayloadPacked.x >> 11) & 0x7F) / 127.0f;
		HitOffset = HitOffset * 2 - 1;

		float HitOffsetLength = float((PayloadPacked.x >> 3) & 0xFF) / 255.0f;
		HitOffset *= HitOffsetLength;
	}
	else
	{
		RayDirection.x = float((PayloadPacked.x >> 25) & 0xFF) / 255.0f;
		RayDirection.y = float((PayloadPacked.x >> 18) & 0xFF) / 255.0f;
		RayDirection.z = float((PayloadPacked.x >> 11) & 0xFF) / 255.0f;
		RayDirection = RayDirection * 2 - 1;

		SkyOcclusion = float((PayloadPacked.x >> 3) & 0x1F) / 31.0f;
	}
	
	HitNormal = UnpackVoxelNormalIndex(((PayloadPacked.y) >> 2) & 0x7);
	
	HashRank = PayloadPacked.y & 0x3;
	PayloadPacked.y &= 0xFFFFFFE0;
	
	HitCoord.x = ((PayloadPacked.y >> 5)  & 0x1FF) + 1; 
	HitCoord.y = ((PayloadPacked.y >> 14) & 0x1FF) + 1;
	HitCoord.z = ((PayloadPacked.y >> 23) & 0x1FF) + 1;

	return IsHit;
}


uint2 PackVoxelHitPayload(float3 HitOffset, float HitDistance, float3 HitNormal, uint3 HitCoord, bool HitFound)
{
	float HitOffsetLength = length(HitOffset);
	HitOffset = -normalize(HitOffset) * 0.5f + 0.5f;
	
	uint OffsetX = uint(HitOffset.x * 127.0f + 0.5f) & 0x7F;
	uint OffsetY = uint(HitOffset.y * 127.0f + 0.5f) & 0x7F;
	uint OffsetZ = uint(HitOffset.z * 127.0f + 0.5f) & 0x7F;
	uint OffsetLength = uint(HitOffsetLength * 127.0f + 0.5f) & 0x7F;

	// Approximate distance for cache dimming
	uint DistanceApprox = uint(saturate(HitDistance) * 15.0f + 0.5f) & 0xF;	

	uint PackedX = (OffsetX << 25) | (OffsetY << 18) | (OffsetZ << 11) | (OffsetLength << 4) | DistanceApprox;
	
	uint HitNormalIndex = (PackVoxelNormalIndex(HitNormal) & 0x7) << 2;
	
	uint CoordX = ((HitCoord.x - 1) & 0x1FF) << 5;					
	uint CoordY = ((HitCoord.y - 1) & 0x1FF) << 14;					
	uint CoordZ = ((HitCoord.z - 1) & 0x1FF) << 23;					
	
	uint PackedY = CoordX | CoordY | CoordZ | HitNormalIndex | HitFound; // 1 bit is free
	
	return uint2(PackedX, PackedY);
}

bool UnpackVoxelHitPayload(uint2 PayloadPacked, inout float HitDistanceApprox, inout float3 HitOffset, inout float3 HitNormal, inout uint3 HitCoord)
{
	bool IsHit = PayloadPacked.y & 0x1;

	HitOffset = 0;
	HitOffset.x = float((PayloadPacked.x >> 25) & 0x7F) / 127.0f;
	HitOffset.y = float((PayloadPacked.x >> 18) & 0x7F) / 127.0f;
	HitOffset.z = float((PayloadPacked.x >> 11) & 0x7F) / 127.0f;
	HitOffset = HitOffset * 2 - 1;

	float HitOffsetLength = float((PayloadPacked.x >> 4) & 0x7F) / 127.0f;
	HitOffset *= HitOffsetLength;

	HitDistanceApprox = float(PayloadPacked.x & 0xF) / 15.0f;
	
	HitNormal = UnpackVoxelNormalIndex(((PayloadPacked.y) >> 2) & 0x7);
	
	PayloadPacked.y &= 0xFFFFFFE0;
	
	HitCoord.x = ((PayloadPacked.y >> 5)  & 0x1FF) + 1; 
	HitCoord.y = ((PayloadPacked.y >> 14) & 0x1FF) + 1;
	HitCoord.z = ((PayloadPacked.y >> 23) & 0x1FF) + 1;

	return IsHit;
}

uint PackHitOffset(float3 HitOffset)
{
	float HitOffsetLength = length(HitOffset);
	HitOffset = -normalize(HitOffset) * 0.5f + 0.5f;
	
	uint OffsetX = uint(HitOffset.x * 255.0f + 0.5f) & 0xFF;
	uint OffsetY = uint(HitOffset.y * 255.0f + 0.5f) & 0xFF;
	uint OffsetZ = uint(HitOffset.z * 255.0f + 0.5f) & 0xFF;

	uint OffsetLength = uint(HitOffsetLength * 255.0f + 0.5f) & 0xFF;

	return (OffsetX << 24) | (OffsetY << 16) | (OffsetZ << 8) | (OffsetLength << 0);
}

float3 UnpackHitOffset(uint HitOffsetPacked)
{
	float3 HitOffset;
	HitOffset.x = float((HitOffsetPacked >> 24) & 0xFF) / 255.0f;
	HitOffset.y = float((HitOffsetPacked >> 16) & 0xFF) / 255.0f;
	HitOffset.z = float((HitOffsetPacked >>  8) & 0xFF) / 255.0f;
	HitOffset = HitOffset * 2 - 1;
	
	float HitOffsetLength = float(HitOffsetPacked & 0xFF) / 255.0f;
	
	return HitOffset * HitOffsetLength;
}

uint PackHashKey(uint3 HitCoord, float3 HitNormal)
{
	uint HitNormalIndex = (PackVoxelNormalIndex(HitNormal) & 0x7) << 2; // 00000000.00000000.00000000.00011100
	
	uint HitCoordX = ((HitCoord.x - 1) & 0x1FF) << 5;					// 00000000.00000000.00111111.11100000
	uint HitCoordY = ((HitCoord.y - 1) & 0x1FF) << 14;					// 00000000.01111111.11000000.00000000
	uint HitCoordZ = ((HitCoord.z - 1) & 0x1FF) << 23;					// 11111111.10000000.00000000.00000000

	return HitCoordX | HitCoordY | HitCoordZ | HitNormalIndex;
}

uint3 UnpackHitCoordFromHashKey(uint HashKey)
{	
	HashKey = HashKey & 0xFFFFFFE0;
	
	uint3 HitCoord;
	HitCoord.x = ((HashKey >> 5) & 0x1FF) + 1; 
	HitCoord.y = ((HashKey >> 14) & 0x1FF) + 1;
	HitCoord.z = ((HashKey >> 23) & 0x1FF) + 1;

	return HitCoord;
}

float3 UnpackHitNormalFromHashKey(uint HashKey)
{	
	return UnpackVoxelNormalIndex(((HashKey) >> 2) & 0x7);
}

uint PackHitDistance(float input1, bool input2, bool input3)
{
	uint f1 = f32tof16(input1) & 0xFFFC;
	uint b1 = input2 == true ? 1 : 0;
	uint b2 = input3 == true ? 1 : 0;
	return f1 | b1 << 1 | b2;
}

float UnpackHitDistance(uint input, out bool b1, out bool b2)
{
	float out1 = f16tof32(input & 0xFFFC);
	b1 = (input & 0x2) == 0x2;
	b2 = (input & 0x1) == 0x1;
	return out1;
}

uint4 PackFilteringOffsetsX8(int2 Coords[8])
{
	uint4 PackedCoords;
	uint2 CoordsA, CoordsB;
	
	CoordsA.x = (abs(Coords[0].x) & 0x7F) | (sign(Coords[0].x) < 0 ? 0x80 : 0x0);
	CoordsA.y = (abs(Coords[0].y) & 0x7F) | (sign(Coords[0].y) < 0 ? 0x80 : 0x0);
	CoordsB.x = (abs(Coords[1].x) & 0x7F) | (sign(Coords[1].x) < 0 ? 0x80 : 0x0);
	CoordsB.y = (abs(Coords[1].y) & 0x7F) | (sign(Coords[1].y) < 0 ? 0x80 : 0x0);

	PackedCoords.x = (CoordsA.x << 0) | (CoordsA.y << 8) | (CoordsB.x << 16) | (CoordsB.y << 24);

	CoordsA.x = (abs(Coords[2].x) & 0x7F) | (sign(Coords[2].x) < 0 ? 0x80 : 0x0);
	CoordsA.y = (abs(Coords[2].y) & 0x7F) | (sign(Coords[2].y) < 0 ? 0x80 : 0x0);
	CoordsB.x = (abs(Coords[3].x) & 0x7F) | (sign(Coords[3].x) < 0 ? 0x80 : 0x0);
	CoordsB.y = (abs(Coords[3].y) & 0x7F) | (sign(Coords[3].y) < 0 ? 0x80 : 0x0);

	PackedCoords.y = (CoordsA.x << 0) | (CoordsA.y << 8) | (CoordsB.x << 16) | (CoordsB.y << 24);

	CoordsA.x = (abs(Coords[4].x) & 0x7F) | (sign(Coords[4].x) < 0 ? 0x80 : 0x0);
	CoordsA.y = (abs(Coords[4].y) & 0x7F) | (sign(Coords[4].y) < 0 ? 0x80 : 0x0);
	CoordsB.x = (abs(Coords[5].x) & 0x7F) | (sign(Coords[5].x) < 0 ? 0x80 : 0x0);
	CoordsB.y = (abs(Coords[5].y) & 0x7F) | (sign(Coords[5].y) < 0 ? 0x80 : 0x0);

	PackedCoords.z = (CoordsA.x << 0) | (CoordsA.y << 8) | (CoordsB.x << 16) | (CoordsB.y << 24);

	CoordsA.x = (abs(Coords[6].x) & 0x7F) | (sign(Coords[6].x) < 0 ? 0x80 : 0x0);
	CoordsA.y = (abs(Coords[6].y) & 0x7F) | (sign(Coords[6].y) < 0 ? 0x80 : 0x0);
	CoordsB.x = (abs(Coords[7].x) & 0x7F) | (sign(Coords[7].x) < 0 ? 0x80 : 0x0);
	CoordsB.y = (abs(Coords[7].y) & 0x7F) | (sign(Coords[7].y) < 0 ? 0x80 : 0x0);

	PackedCoords.w = (CoordsA.x << 0) | (CoordsA.y << 8) | (CoordsB.x << 16) | (CoordsB.y << 24);
	
	return PackedCoords;
}

void UnpackFilteringOffsetsX8(uint4 OffsetsPacked, inout int2 Offsets[8])
{
	Offsets[0].x = ((OffsetsPacked.x >> 0)	& 0x7F) * (((OffsetsPacked.x >> 0)	& 0x80) ? -1 : 1);
	Offsets[0].y = ((OffsetsPacked.x >> 8)	& 0x7F) * (((OffsetsPacked.x >> 8)	& 0x80) ? -1 : 1);
	Offsets[1].x = ((OffsetsPacked.x >> 16)	& 0x7F) * (((OffsetsPacked.x >> 16)	& 0x80) ? -1 : 1);
	Offsets[1].y = ((OffsetsPacked.x >> 24)	& 0x7F) * (((OffsetsPacked.x >> 24)	& 0x80) ? -1 : 1);
	Offsets[2].x = ((OffsetsPacked.y >> 0)	& 0x7F) * (((OffsetsPacked.y >> 0)	& 0x80) ? -1 : 1);
	Offsets[2].y = ((OffsetsPacked.y >> 8)	& 0x7F) * (((OffsetsPacked.y >> 8)	& 0x80) ? -1 : 1);
	Offsets[3].x = ((OffsetsPacked.y >> 16)	& 0x7F) * (((OffsetsPacked.y >> 16)	& 0x80) ? -1 : 1);
	Offsets[3].y = ((OffsetsPacked.y >> 24)	& 0x7F) * (((OffsetsPacked.y >> 24)	& 0x80) ? -1 : 1);
	Offsets[4].x = ((OffsetsPacked.z >> 0)	& 0x7F) * (((OffsetsPacked.z >> 0)	& 0x80) ? -1 : 1);
	Offsets[4].y = ((OffsetsPacked.z >> 8)	& 0x7F) * (((OffsetsPacked.z >> 8)	& 0x80) ? -1 : 1);
	Offsets[5].x = ((OffsetsPacked.z >> 16)	& 0x7F) * (((OffsetsPacked.z >> 16)	& 0x80) ? -1 : 1);
	Offsets[5].y = ((OffsetsPacked.z >> 24)	& 0x7F) * (((OffsetsPacked.z >> 24)	& 0x80) ? -1 : 1);
	Offsets[6].x = ((OffsetsPacked.w >> 0)	& 0x7F) * (((OffsetsPacked.w >> 0)	& 0x80) ? -1 : 1);
	Offsets[6].y = ((OffsetsPacked.w >> 8)	& 0x7F) * (((OffsetsPacked.w >> 8)	& 0x80) ? -1 : 1);
	Offsets[7].x = ((OffsetsPacked.w >> 16)	& 0x7F) * (((OffsetsPacked.w >> 16)	& 0x80) ? -1 : 1);
	Offsets[7].y = ((OffsetsPacked.w >> 24)	& 0x7F) * (((OffsetsPacked.w >> 24)	& 0x80) ? -1 : 1);
}

uint4 PackFilteringWeightsX8(float Weights[8])
{
	uint4 PackedWeights;
	PackedWeights.x = ((uint(Weights[0] * 255.0f + 0.5f) & 0xFF) << 0) | ((uint(Weights[1] * 255.0f + 0.5f) & 0xFF) << 8);
	PackedWeights.y = ((uint(Weights[2] * 255.0f + 0.5f) & 0xFF) << 0) | ((uint(Weights[3] * 255.0f + 0.5f) & 0xFF) << 8);
	PackedWeights.z = ((uint(Weights[4] * 255.0f + 0.5f) & 0xFF) << 0) | ((uint(Weights[5] * 255.0f + 0.5f) & 0xFF) << 8);
	PackedWeights.w = ((uint(Weights[6] * 255.0f + 0.5f) & 0xFF) << 0) | ((uint(Weights[7] * 255.0f + 0.5f) & 0xFF) << 8);

	return PackedWeights.xyzw;
}

void UnpackFilteringWeightsX8(uint4 PackedWeights, inout float Weights[8])
{
	Weights[0] = ((PackedWeights.x >> 0) & 0xFF) / 255.0f;
	Weights[1] = ((PackedWeights.x >> 8) & 0xFF) / 255.0f;
	Weights[2] = ((PackedWeights.y >> 0) & 0xFF) / 255.0f;
	Weights[3] = ((PackedWeights.y >> 8) & 0xFF) / 255.0f;
	Weights[4] = ((PackedWeights.z >> 0) & 0xFF) / 255.0f;
	Weights[5] = ((PackedWeights.z >> 8) & 0xFF) / 255.0f;
	Weights[6] = ((PackedWeights.w >> 0) & 0xFF) / 255.0f;
	Weights[7] = ((PackedWeights.w >> 8) & 0xFF) / 255.0f;
}

uint PackProbeAO(float Mask, float Samplecount, float MaxSamplecount)
{
	uint MaskPacked = uint(Mask * 255.0f + 0.5f) & 0xFF;
	uint SamplecountPacked = uint(Samplecount / MaxSamplecount * 255.0f + 0.5f) & 0xFF;

	return (MaskPacked << 8) | (SamplecountPacked << 0);
}

float2 UnpackProbeAO(uint GuidanceMaskPacked, float MaxSamplecount)
{
	float2 GuidanceMask;
	GuidanceMask.x = ((GuidanceMaskPacked >> 8) & 0xFF) / 255.0f;
	GuidanceMask.y = ((GuidanceMaskPacked >> 0) & 0xFF) / 255.0f * MaxSamplecount;
	return GuidanceMask;
}

uint PackDirection24bit(float3 Direction)
{
	Direction = Direction * 0.5 + 0.5;
	uint DirectionX = uint(Direction.x * 255.0f + 0.5f) & 0xFF;
	uint DirectionY = uint(Direction.y * 255.0f + 0.5f) & 0xFF;
	uint DirectionZ = uint(Direction.z * 255.0f + 0.5f) & 0xFF;
    
	return (DirectionX << 16) | (DirectionY << 8) | (DirectionZ << 0);
}

float3 UnpackDirection24bit(uint DirectionPacked)
{
	float3 Direction;
	Direction.x = float((DirectionPacked >> 16) & 0xFF) / 255.0f;
	Direction.y = float((DirectionPacked >>  8) & 0xFF) / 255.0f;
	Direction.z = float((DirectionPacked >>  0) & 0xFF) / 255.0f;
	Direction = Direction * 2 - 1;

	return Direction;
}

uint4 PackWorldPosNormal(float3 WorldPos, float3 Normal)
{
	Normal = Normal * 0.5f + 0.5f;
	uint NormalPackedX = uint(Normal.x * 1023.0f + 0.5f) & 0x3FF;	// 11111111.11000000.00000000.00000000
	uint NormalPackedY = uint(Normal.y * 1023.0f + 0.5f) & 0x3FF;	// 00000000.00111111.11110000.00000000
	uint NormalPackedZ = uint(Normal.z * 1023.0f + 0.5f) & 0x3FF;	// 00000000.00000000.00001111.11111100

	uint NormalPacked = (NormalPackedX << 22) | (NormalPackedY << 12) | (NormalPackedZ << 2);
	
	return uint4(asuint(WorldPos.x), asuint(WorldPos.y), asuint(WorldPos.z), NormalPacked);
}

void UnpackWorldPosNormal(uint4 WorldPosNormalPacked, inout float3 WorldPos, inout float3 Normal)
{	
	Normal.x = ((WorldPosNormalPacked.w >> 22) & 0x3FF) / 1023.0f;
	Normal.y = ((WorldPosNormalPacked.w >> 12) & 0x3FF) / 1023.0f;
	Normal.z = ((WorldPosNormalPacked.w >>  2) & 0x3FF) / 1023.0f;
	Normal = Normal * 2.0f - 1.0f;
	
	WorldPos.x = asfloat(WorldPosNormalPacked.x);
	WorldPos.y = asfloat(WorldPosNormalPacked.y);
	WorldPos.z = asfloat(WorldPosNormalPacked.z);
}

uint2 PackNormalDepth(float3 Normal, float Depth)
{
	Normal = Normal * 0.5f + 0.5f;
	uint NormalPackedX = uint(Normal.x * 1023.0f + 0.5f) & 0x3FF;	// 11111111.11000000.00000000.00000000
	uint NormalPackedY = uint(Normal.y * 1023.0f + 0.5f) & 0x3FF;	// 00000000.00111111.11110000.00000000
	uint NormalPackedZ = uint(Normal.z * 1023.0f + 0.5f) & 0x3FF;	// 00000000.00000000.00001111.11111100

	uint NormalPacked = (NormalPackedX << 22) | (NormalPackedY << 12) | (NormalPackedZ << 2);
	
	return uint2(NormalPacked, asuint(Depth));
}

float4 UnpackNormalDepthFull(uint2 NormalDepthPacked)
{
	float3 Normal;
	Normal.x = ((NormalDepthPacked.x >> 22) & 0x3FF) / 1023.0f;
	Normal.y = ((NormalDepthPacked.x >> 12) & 0x3FF) / 1023.0f;
	Normal.z = ((NormalDepthPacked.x >>  2) & 0x3FF) / 1023.0f;
	Normal = Normal * 2.0f - 1.0f;

	return float4(Normal.xyz, asfloat(NormalDepthPacked.y));
}

uint2 PackNormalDepth2(float3 Normal, float Depth, uint2 Offset)
{
	uint3 NormalPacked;
	Normal = Normal * 0.5f + 0.5f;
	NormalPacked.x = uint(Normal.x * 255.0f + 0.5f) & 0xFF;	// 11111111.00000000.00000000.00000000
	NormalPacked.y = uint(Normal.y * 255.0f + 0.5f) & 0xFF;	// 00000000.11111111.00000000.00000000
	NormalPacked.z = uint(Normal.z * 255.0f + 0.5f) & 0xFF;	// 00000000.00000000.11111111.00000000

	uint NormalOffsetPacked = (NormalPacked.x << 24) | (NormalPacked.y << 16) | (NormalPacked.z << 8) | (Offset.x << 4) | (Offset.y << 0);
	
	return uint2(NormalOffsetPacked, asuint(Depth));
}

float3 UnpackNormal(uint2 NormalDepthPacked)
{
	float3 Normal;
	Normal.x = ((NormalDepthPacked.x >> 24) & 0xFF) / 255.0f;
	Normal.y = ((NormalDepthPacked.x >> 16) & 0xFF) / 255.0f;
	Normal.z = ((NormalDepthPacked.x >>  8) & 0xFF) / 255.0f;
	Normal = Normal * 2.0f - 1.0f;
	
	return Normal;
}

float4 UnpackNormalDepth(uint2 NormalDepthPacked, inout float2 Offset)
{
	float3 Normal;
	Normal.x = ((NormalDepthPacked.x >> 24) & 0xFF) / 255.0f;
	Normal.y = ((NormalDepthPacked.x >> 16) & 0xFF) / 255.0f;
	Normal.z = ((NormalDepthPacked.x >>  8) & 0xFF) / 255.0f;
	Normal = Normal * 2.0f - 1.0f;

	Offset.x = (NormalDepthPacked.x >> 4) & 0xF;
	Offset.y = (NormalDepthPacked.x >> 0) & 0xF;
	
	return float4(Normal.xyz, asfloat(NormalDepthPacked.y));
}

uint2 PackReprojectionCoord(uint2 ReprojectionCoord, uint2 BestOffset, bool ReprojectionFailed)
{
	uint PackedX = (ReprojectionCoord.x & 0x3FFF) | (BestOffset.x << 15) | (ReprojectionFailed << 14);
	uint PackedY = (ReprojectionCoord.y & 0x3FFF) | (BestOffset.y << 15) | (ReprojectionFailed << 14);
	
	return uint2(PackedX, PackedY);
}

uint2 UnpackReprojectionCoord(uint2 ReprojectionCoordPacked)
{
	uint2 ReprojectionCoord;
	ReprojectionCoord.x = ReprojectionCoordPacked.x & 0x3FFF;
	ReprojectionCoord.y = ReprojectionCoordPacked.y & 0x3FFF;
	
	return ReprojectionCoord;
}

uint2 UnpackBestReprojectionCoord(uint2 ReprojectionCoordPacked, inout uint2 BestOffset, inout bool ReprojectionFailed)
{
	uint2 ReprojectionCoord;
	ReprojectionCoord.x = ReprojectionCoordPacked.x & 0x3FFF;
	ReprojectionCoord.y = ReprojectionCoordPacked.y & 0x3FFF;
	
	BestOffset.x = (ReprojectionCoordPacked.x >> 15) & 0x1;
	BestOffset.y = (ReprojectionCoordPacked.y >> 15) & 0x1;
	
	ReprojectionFailed = (ReprojectionCoordPacked.x >> 14) & 0x1;
	
	return ReprojectionCoord;
}

uint2 PackPersistentReprojectionCoord(uint2 ReprojectionCoord, uint ReprojectionIndex)
{
	uint PackedX = (ReprojectionCoord.x & 0x1FFF) | (ReprojectionIndex << 13);
	uint PackedY = (ReprojectionCoord.y & 0x1FFF) | (ReprojectionIndex << 13);
	
	return uint2(PackedX, PackedY);
}


uint2 UnpackPersistentReprojectionCoord(uint2 ReprojectionCoordPacked, inout uint ReprojectionIndex)
{
	uint2 ReprojectionCoord;
	ReprojectionCoord.x = ReprojectionCoordPacked.x & 0x1FFF;
	ReprojectionCoord.y = ReprojectionCoordPacked.y & 0x1FFF;

	ReprojectionIndex = (ReprojectionCoordPacked.x >> 13) & 0x7;

	return ReprojectionCoord;
}

uint PackVoxelBiasOffset(float3 RayOriginBiasedWS, float3 RayOriginUnbiasedWS)
{
	float Distance = length(RayOriginBiasedWS - RayOriginUnbiasedWS);
	float3 Direction = normalize(RayOriginBiasedWS - RayOriginUnbiasedWS) * 0.5 + 0.5;
	
	uint DirectionX = uint(Direction.x * 255.0f + 0.5f) & 0xFF;
	uint DirectionY = uint(Direction.y * 255.0f + 0.5f) & 0xFF;
	uint DirectionZ = uint(Direction.z * 255.0f + 0.5f) & 0xFF;
	uint DistancePacked = uint(Distance * 255.0f + 0.5f) & 0xFF;
    
	return (DirectionX << 24) | (DirectionY << 16) | (DirectionZ << 8) | (DistancePacked << 0);
}

float4 UnpackVoxelBiasOffset(uint BiasDataPacked)
{
	float3 Direction;
	Direction.x = float((BiasDataPacked >> 24) & 0xFF) / 255.0f;
	Direction.y = float((BiasDataPacked >> 16) & 0xFF) / 255.0f;
	Direction.z = float((BiasDataPacked >>  8) & 0xFF) / 255.0f;
	Direction = Direction * 2 - 1;

	float Distance = float((BiasDataPacked >> 0) & 0xFF) / 255.0f;

	return float4(Direction.xyz, Distance);
}