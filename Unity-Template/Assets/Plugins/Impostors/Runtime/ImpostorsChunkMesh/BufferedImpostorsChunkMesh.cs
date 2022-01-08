using System;
using Impostors.Structs;
using Impostors.Unsafe;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Impostors.ImpostorsChunkMesh
{
    public class BufferedImpostorsChunkMesh : IImpostorsChunkMesh
    {
        private const MeshUpdateFlags MeshUpdateFlags =
            UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds |
            UnityEngine.Rendering.MeshUpdateFlags.DontValidateIndices |
            UnityEngine.Rendering.MeshUpdateFlags.DontNotifyMeshUsers |
            UnityEngine.Rendering.MeshUpdateFlags.DontResetBoneBounds;

        private static readonly VertexAttributeDescriptor[] MeshLayout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4),
        };

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct VertexData
        {
            public float3 Position;
            public float3 Normal;
            public float4 Color;
            public float4 TexCoord0;
        }

        private Mesh _mesh;

        private NativeArray<VertexData> _vertexDataBuffer;
        private NativeArray<Impostor> _impostors;
        private NativeArray<float> _bounds;
        private NativeCounter _counter;
        private bool _isVertexDataBufferChanged;

        public BufferedImpostorsChunkMesh(int maxImpostorsCount, NativeArray<Impostor> impostors)
        {
            _impostors = impostors;

            _mesh = new Mesh();
            _mesh.MarkDynamic();

            _vertexDataBuffer = new NativeArray<VertexData>(maxImpostorsCount * 4, Allocator.Persistent);

            _mesh.SetVertexBufferParams(maxImpostorsCount * 4, MeshLayout);
            _mesh.SetIndexBufferParams(maxImpostorsCount * 6, IndexFormat.UInt32);

            using (var indexBuffer = new NativeArray<int>(maxImpostorsCount * 6, Allocator.TempJob))
            {
                var jobFillIndexBuffer = new JobFillIndexBuffer() {indexBuffer = indexBuffer};
                jobFillIndexBuffer.Schedule(maxImpostorsCount * 6, 32).Complete();
                _mesh.SetIndexBufferData(indexBuffer, 0, 0, maxImpostorsCount * 6, MeshUpdateFlags);
            }

            _mesh.SetSubMesh(0, new SubMeshDescriptor(0, maxImpostorsCount * 6, MeshTopology.Triangles),
                MeshUpdateFlags);

            _bounds = new NativeArray<float>(6, Allocator.Persistent);
        }

        public JobHandle ScheduleMeshCreation(JobHandle dependsOn = default)
        {
            _isVertexDataBufferChanged = true;
            if (_counter.IsCreated)
            {
                Debug.LogError("Something went wrong...");
                _counter.Dispose();
            }

            _counter = new NativeCounter(Allocator.TempJob);
            var jobFillVertexDataBuffer = new JobFillVertexDataBuffer()
            {
                counter = _counter,
                impostors = _impostors,
                vertexDataBuffer = _vertexDataBuffer
            };
            var jobHandle = jobFillVertexDataBuffer.Schedule(_impostors.Length, dependsOn);

            var jobCalculateBounds = new JobCalculateBounds()
            {
                impostors = _impostors,
                bounds = _bounds
            };
            jobHandle = jobCalculateBounds.Schedule(jobHandle);

            return jobHandle;
        }

        public Mesh GetMesh()
        {
            if (_isVertexDataBufferChanged)
            {
                _isVertexDataBufferChanged = false;
                Profiler.BeginSample("SetVertexBufferData");
                _mesh.SetVertexBufferParams(_counter.Count * 4, MeshLayout);
                _mesh.SetVertexBufferData(_vertexDataBuffer, 0, 0, _counter.Count * 4, 0, MeshUpdateFlags);
                Profiler.EndSample();

                var b = new Bounds();
                var min = new Vector3(_bounds[0], _bounds[2], _bounds[4]);
                var max = new Vector3(_bounds[1], _bounds[3], _bounds[5]);
                b.SetMinMax(min, max);
                _mesh.bounds = b;

                _counter.Dispose();
            }

            return _mesh;
        }

        public void Dispose()
        {
            _vertexDataBuffer.Dispose();
            _mesh.Clear();
            _bounds.Dispose();
            if (_counter.IsCreated)
                _counter.Dispose();
            UnityEngine.Object.Destroy(_mesh);
            _mesh = null;
        }

        [BurstCompile]
        private struct JobFillIndexBuffer : IJobParallelFor
        {
            [WriteOnly]
            public NativeArray<int> indexBuffer;

            public void Execute(int index)
            {
                int id = index % 6;
                int result = index / 6 * 4;
                switch (id)
                {
                    case 1:
                        result += 1;
                        break;
                    case 2:
                    case 3:
                        result += 2;
                        break;
                    case 4:
                        result += 3;
                        break;
                }

                indexBuffer[index] = result;
            }
        }

        [BurstCompile]
        private struct JobCalculateBounds : IJob
        {
            [ReadOnly]
            public NativeArray<Impostor> impostors;

            public NativeArray<float> bounds; // minX, maxX, minY, maxY, minZ, maxZ

            public void Execute()
            {
                if (bounds.Length != 6)
                    throw new Exception("Bounds should have length of 6. Actual value: " + bounds.Length);

                int length = impostors.Length;
                float minX = float.MaxValue;
                float maxX = float.MinValue;
                float minY = float.MaxValue;
                float maxY = float.MinValue;
                float minZ = float.MaxValue;
                float maxZ = float.MinValue;
                for (int i = 0; i < length; i++)
                {
                    var impostor = impostors[i];
                    if (impostor.Exists == false)
                        continue;
                    minX = math.min(minX, impostor.position.x - impostor.quadSize);
                    maxX = math.max(maxX, impostor.position.x + impostor.quadSize);
                    minY = math.min(minY, impostor.position.y - impostor.quadSize);
                    maxY = math.max(maxY, impostor.position.y + impostor.quadSize);
                    minZ = math.min(minZ, impostor.position.z - impostor.quadSize);
                    maxZ = math.max(maxZ, impostor.position.z + impostor.quadSize);
                }

                bounds[0] = minX;
                bounds[1] = maxX;
                bounds[2] = minY;
                bounds[3] = maxY;
                bounds[4] = minZ;
                bounds[5] = maxZ;
            }
        }

        public int GetUsedBytes()
        {
            var res = 0;

            res += Impostors.MemoryUsage.MemoryUsageUtility.GetMemoryUsage(_vertexDataBuffer);

            return res;
        }
    }

    [BurstCompile(DisableSafetyChecks = true)]
    public struct JobFillVertexDataBuffer : IJobFor
    {
        [ReadOnly]
        public NativeArray<Impostor> impostors;

        [NativeDisableContainerSafetyRestriction]
        [WriteOnly]
        public NativeArray<BufferedImpostorsChunkMesh.VertexData> vertexDataBuffer;

        public NativeCounter.Concurrent counter;

        public void Execute(int index)
        {
            Impostor impostor = impostors[index];

            if (impostor.impostorLODGroupInstanceId == 0)
                return;
            int count = counter.Increment();
            int vertexStartIndex = (count - 1) * 4;

            float quadSizeHalf = impostor.quadSize * .5f;
            const float divider = 1 / 100000f;
            const float mult = 100000f;
            float4 color = new float4(
                (impostor.position.x + mult * .5f) * divider,
                (impostor.position.y + mult * .5f) * divider,
                (impostor.position.z + mult * .5f) * divider,
                impostor.time * divider
            );
            float4 uv = impostor.uv;
            float3 normal = impostor.direction;
            BufferedImpostorsChunkMesh.VertexData vertexData = default;
            vertexData.Normal = normal;
            vertexData.Color = color;

            // vertex 0
            vertexData.Position = new float3(-quadSizeHalf, -quadSizeHalf, impostor.zOffset);
            vertexData.TexCoord0 = new float4(uv.x, uv.y, 0, impostor.fadeTime);
            vertexDataBuffer[vertexStartIndex + 0] = vertexData;

            // vertex 1
            vertexData.Position = new float3(-quadSizeHalf, quadSizeHalf, impostor.zOffset);
            vertexData.TexCoord0 = new float4(uv.x, uv.w, 0, impostor.fadeTime);
            vertexDataBuffer[vertexStartIndex + 1] = vertexData;

            // vertex 2
            vertexData.Position = new float3(quadSizeHalf, quadSizeHalf, impostor.zOffset);
            vertexData.TexCoord0 = new float4(uv.z, uv.w, 0, impostor.fadeTime);
            vertexDataBuffer[vertexStartIndex + 2] = vertexData;

            // vertex 3
            vertexData.Position = new float3(quadSizeHalf, -quadSizeHalf, impostor.zOffset);
            vertexData.TexCoord0 = new float4(uv.z, uv.y, 0, impostor.fadeTime);
            vertexDataBuffer[vertexStartIndex + 3] = vertexData;
        }
    }
}