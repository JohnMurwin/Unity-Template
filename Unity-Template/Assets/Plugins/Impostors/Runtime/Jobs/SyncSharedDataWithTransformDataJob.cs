using Impostors.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Impostors.Jobs
{
    [BurstCompile]
    public struct SyncSharedDataWithTransformDataJob : IJobParallelForTransform
    {
        public NativeArray<SharedData> sharedDataArray;

        public void Execute(int index, [ReadOnly] TransformAccess transform)
        {
            var sharedData = sharedDataArray[index];

            if (sharedData.settings.isStatic)
                return;
            
            float4x4 localToWorldMatrix = transform.localToWorldMatrix;
            sharedData.data.position =
                math.mul(localToWorldMatrix, new float4(sharedData.data.localReferencePoint, 1)).xyz;
            sharedData.data.forward = math.mul(transform.rotation, new float3(0, 0, 1));
            //sharedData.data.lossyScale = transform.localToWorldMatrix.lossyScale; // todo: looks like this operation is expensive, need to check UPD: consumes half of the time
            
            // var scale = new float3(
            //     math.length(localToWorldMatrix.c0), 
            //     math.length(localToWorldMatrix.c1),
            //     math.length(localToWorldMatrix.c2)); // this version uses 0.1 on 4k objects

            sharedDataArray[index] = sharedData;
        }
    }
}