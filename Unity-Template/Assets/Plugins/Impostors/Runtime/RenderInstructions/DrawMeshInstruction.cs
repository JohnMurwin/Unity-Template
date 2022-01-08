using UnityEngine;

namespace Impostors.RenderInstructions
{
    public sealed class DrawMeshInstruction : IRenderInstruction
    {
        public readonly Mesh Mesh;
        public readonly Matrix4x4 Matrix;
        public readonly Material Material;
        public readonly int SubmeshIndex;
        public readonly int ShaderPass;
        public readonly MaterialPropertyBlock PropertyBlock;

        public DrawMeshInstruction(Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex, int shaderPass,
            MaterialPropertyBlock propertyBlock)
        {
            Mesh = mesh;
            Matrix = matrix;
            Material = material;
            SubmeshIndex = submeshIndex;
            ShaderPass = shaderPass;
            PropertyBlock = propertyBlock;
        }

        public void ApplyCommandBuffer(CommandBufferProxy bufferProxy)
        {
            bufferProxy.CommandBuffer.DrawMesh(Mesh, Matrix, Material, SubmeshIndex, ShaderPass, PropertyBlock);
        }
    }
}