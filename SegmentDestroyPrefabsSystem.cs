#define ENABLE_DEBUG_LOGS

using FarmFend.BitTerrain.ECS.Components;
using FarmFend.Scripts;
using Unity.Burst;
using Unity.Entities;

namespace FarmFend.BitTerrain.ECS.Systems
{
    partial struct SegmentDestroyPrefabsSystem : ISystem
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

            foreach (var (componentData, buffer, entity ) in SystemAPI.Query<
                             RefRO<SegmentComponent>, 
                             DynamicBuffer<SegmentTilesPrefabBuffer>>()
                         .WithAny<EventSegmentDestroyPrefabs>()
                         .WithEntityAccess())
            {
                LoggerUtils.Log(componentData.ValueRO.SegmentName);
                ecb.SetComponentEnabled<EventSegmentDestroyPrefabs>(entity,false);
                ecb.DestroyEntity(buffer.AsNativeArray().Reinterpret<Entity>());
            }
        }
    }
}
