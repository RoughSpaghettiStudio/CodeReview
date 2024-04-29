#define ENABLE_DEBUG_LOGS
// #undef ENABLE_DEBUG_LOGS

using FarmFend.BitTerrain.Data;
using FarmFend.BitTerrain.ECS.Components;
using FarmFend.BitTerrain.Utils;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace FarmFend.BitTerrain.ECS.Systems
{
    partial struct BitTerrainSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            var ecb = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (segmentData, segmentEvents, tilesPrefabBuffers, segmentEntity) in SystemAPI.Query<RefRO<SegmentComponent>, RefRO<SegmentEventMaskComponent>, DynamicBuffer<SegmentTilesPrefabBuffer>>()
                         .WithChangeFilter<SegmentComponent>()
                         .WithEntityAccess())
            {
                // Event1 Destroy all tile prefabs
                if (tilesPrefabBuffers.Length > 0 && SegmentEvents.IsEventMaskSet(SegmentEvents.Events.Event1, segmentEvents.ValueRO.Value))
                {
                    ecb.DestroyEntity(tilesPrefabBuffers.AsNativeArray().Reinterpret<Entity>());
                }

                // Event2 Instantiate all tile prefabs
                if (SegmentEvents.IsEventMaskSet(SegmentEvents.Events.Event2, segmentEvents.ValueRO.Value))
                {
                    var terrainOrigin = SystemAPI.GetComponentRO<BitTerrainComponent>(segmentData.ValueRO.BitTerrainEntity).ValueRO.TerrainOrigin;
                    var segmentOrigin = new float3(
                        terrainOrigin.x + segmentData.ValueRO.TileSize / 2f + segmentData.ValueRO.TileSize * segmentData.ValueRO.SegmentOffset.x,
                        terrainOrigin.y + -segmentData.ValueRO.TileSize / 2f + segmentData.ValueRO.TileSize * segmentData.ValueRO.SegmentOffset.y,
                        terrainOrigin.z + segmentData.ValueRO.TileSize / 2f + segmentData.ValueRO.TileSize * segmentData.ValueRO.SegmentOffset.z);

                    for (int y = 7; y >= 0; y--)
                    {
                        for (int x = 0; x < 8; x++)
                        {
                            if (SegmentBitMaskUtils.IsTileSet(x, y, segmentData.ValueRO.SegmentBitMask))
                            {
                                var entity = ecb.Instantiate(segmentData.ValueRO.TilePrefab);
                                ecb.SetName(entity, $"{segmentData.ValueRO.SegmentName}_Tile_x{x}:y{y}");
                                ecb.SetComponent(entity,
                                    new LocalTransform
                                    {
                                        Position = new float3(segmentOrigin.x + x * segmentData.ValueRO.TileSize, segmentOrigin.y, segmentOrigin.z + y * segmentData.ValueRO.TileSize),
                                        Rotation = Quaternion.identity,
                                        Scale = segmentData.ValueRO.TileSize,
                                    });
                                ecb.AppendToBuffer(segmentEntity, new SegmentTilesPrefabBuffer {Value = entity});
                            }
                        }
                    }
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
