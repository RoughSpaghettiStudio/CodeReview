#define ENABLE_DEBUG_LOGS
// #undef ENABLE_DEBUG_LOGS

using FarmFend.BitTerrain.Data;
using FarmFend.BitTerrain.ECS.Components;
using FarmFend.BitTerrain.Utils;
using FarmFend.BitTerrain.Data;
using FarmFend.Scripts;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


namespace FarmFend.BitTerrain.ECS.Systems
{
    partial struct SegmentInstantiatePrefabsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EventSegmentDestroyPrefabs>();
            state.RequireForUpdate<SegmentTilesPrefabBuffer>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>().
                CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (componentData,  buffer, entity) in SystemAPI.Query<RefRO<SegmentComponent>, DynamicBuffer<SegmentTilesPrefabBuffer>>()
                         .WithChangeFilter<SegmentComponent>()
                         .WithChangeFilter<EventSegmentInstantiatePrefabs>()
                         .WithEntityAccess())
            {
                var terrainOrigin = SystemAPI.GetComponentRO<BitTerrainComponent>(componentData.ValueRO.BitTerrainEntity).ValueRO.TerrainOrigin;
                var segmentOrigin = new float3(
                    terrainOrigin.x + componentData.ValueRO.TileSize / 2f + componentData.ValueRO.TileSize * componentData.ValueRO.SegmentOffset.x,
                    terrainOrigin.y + -componentData.ValueRO.TileSize / 2f + componentData.ValueRO.TileSize * componentData.ValueRO.SegmentOffset.y,
                    terrainOrigin.z + componentData.ValueRO.TileSize / 2f + componentData.ValueRO.TileSize * componentData.ValueRO.SegmentOffset.z);

                for (int y = 7; y >= 0; y--)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        if (SegmentBitMaskUtils.IsTileSet(x, y, componentData.ValueRO.SegmentBitMask))
                        {
                            var tile = ecb.Instantiate(componentData.ValueRO.TilePrefab);
                            ecb.SetName(tile, $"{componentData.ValueRO.SegmentName}_Tile_x{x}:y{y}");
                            ecb.SetComponent(tile,
                                new LocalTransform
                                {
                                    Position = new float3(segmentOrigin.x + x * componentData.ValueRO.TileSize, segmentOrigin.y, segmentOrigin.z + y * componentData.ValueRO.TileSize),
                                    Rotation = Quaternion.identity,
                                    Scale = componentData.ValueRO.TileSize,
                                });
                            ecb.AppendToBuffer(entity, new SegmentTilesPrefabBuffer {Value = tile});
                        }
                    }
                }
                LoggerUtils.Log(componentData.ValueRO.SegmentName);
                ecb.SetComponentEnabled<EventSegmentInstantiatePrefabs>(entity,false);
            }
        }
    }
}
