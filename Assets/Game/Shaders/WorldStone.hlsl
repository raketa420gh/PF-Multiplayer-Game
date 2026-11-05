#ifndef GAME_WORLD_STONE_INCLUDED
#define GAME_WORLD_STONE_INCLUDED

// Box mapping in world space: a face takes the texture of the axis it faces, in metres scaled by _BaseMap_ST
// (tiling = 1 / real tile size). Neighbouring blocks, steps and corners continue one pattern, courses stay level.
// U runs along the face, V up a wall (along -Z on a floor); the tangent matches U so normal maps light correctly.
void WorldStoneMap(float3 positionWS, float3 normalWS, out float2 uv, out half4 tangentWS)
{
    float3 n = abs(normalWS);
    float3 axis;
    float3 tangent;

    if (n.y >= n.x && n.y >= n.z)
    {
        axis = float3(0.0, normalWS.y >= 0.0 ? 1.0 : -1.0, 0.0);
        tangent = float3(1.0, 0.0, 0.0);
    }
    else if (n.x >= n.z)
    {
        axis = float3(normalWS.x >= 0.0 ? 1.0 : -1.0, 0.0, 0.0);
        tangent = float3(0.0, 0.0, -axis.x);
    }
    else
    {
        axis = float3(0.0, 0.0, normalWS.z >= 0.0 ? 1.0 : -1.0);
        tangent = float3(axis.z, 0.0, 0.0);
    }

    float3 bitangent = cross(axis, tangent);
    uv = float2(dot(positionWS, tangent), dot(positionWS, bitangent)) * _BaseMap_ST.xy + _BaseMap_ST.zw;
    tangentWS = half4(tangent, 1.0);
}

#endif
