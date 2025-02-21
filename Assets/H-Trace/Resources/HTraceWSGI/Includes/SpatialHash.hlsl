#pragma once
#include "../Headers/HMain.hlsl"

RWStructuredBuffer<uint> _HashBuffer_Key;
RWStructuredBuffer<uint> _HashBuffer_Counter;
RWStructuredBuffer<uint2> _HashBuffer_Payload;
RWStructuredBuffer<uint4> _HashBuffer_Radiance;
RWStructuredBuffer<uint4> _HashBuffer_Position;

#define MAX_LOOKUP_ITERATIONS 16

uint _HashStorageSize;
uint _HashUpdateFraction;

uint HashPCG(uint Value)
{
    uint State = Value * 747796405u + 2891336453u;
    uint Word = ((State >> ((State >> 28u) + 4u)) ^ State) * 277803737u;
    return (Word >> 22u) ^ Word;
}

uint HashGetIndex(uint3 Coord, uint AnisotropyIndex)
{
   return  HashPCG(Coord.x + HashPCG(Coord.y + HashPCG(Coord.z + HashPCG(AnisotropyIndex) ))) % _HashStorageSize;  
}


bool HashFindAny(uint Index, uint Key, inout uint Rank, out uint LowestRankedIndex, out uint ProbingIndex, out bool IsEmpty)
{
    IsEmpty = false;

    for (int i = 0; i < MAX_LOOKUP_ITERATIONS; i++)
    {
        uint CurrentKey = _HashBuffer_Key[Index];
        uint CurrentRank = CurrentKey & 0x3;
        
        CurrentKey = CurrentKey & 0xFFFFFFFC; //0xFFFFFFE0;
        Key = Key & 0xFFFFFFFC;

        if (CurrentKey == Key)
        {
            ProbingIndex = Index;
            Rank = CurrentRank;
            IsEmpty = false;
            return true;
        }
        else if (CurrentKey == 0)
        {
            ProbingIndex = Index;
            IsEmpty = true;
        }
        else if (CurrentRank < Rank)
        {
            LowestRankedIndex = Index;
            Rank = CurrentRank;
        }
        
        Index++;
    }
    
    return false;
}

bool HashFindValid(uint Index, uint Key, out uint IndexFound)
{
    for (int i = 0; i < MAX_LOOKUP_ITERATIONS; i++)
    {
        uint CurrentKey = _HashBuffer_Key[Index];
        
        CurrentKey = CurrentKey & 0xFFFFFFFC;
        Key = Key & 0xFFFFFFFC;
        
        if (CurrentKey == Key && CurrentKey!= 0)
        {
            IndexFound = Index;
            return true;
        }
        
        Index++;
    }
    
    return false;
}