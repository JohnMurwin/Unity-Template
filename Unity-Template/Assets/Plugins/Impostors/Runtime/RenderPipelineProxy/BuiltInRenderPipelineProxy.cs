using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Impostors.RenderPipelineProxy
{
    public class BuiltInRenderPipelineProxy : RenderPipelineProxyBase
    {
        private const CameraEvent ForwardRenderingCameraEvent = CameraEvent.BeforeForwardOpaque;
        private const CameraEvent DeferredRenderingCameraEvent = CameraEvent.BeforeGBuffer; // todo test

        [SerializeField]
        private Camera _cameraWhereToScheduleCommandBufferExecution = default;

        public ImpostorTextureRenderingType ImpostorRenderingType = ImpostorTextureRenderingType.Scheduled;

        private CommandBuffer _previousCommandBuffer;

        public enum ImpostorTextureRenderingType
        {
            Scheduled,
            Immediately
        }

        private void Update()
        {
#if UNITY_EDITOR // this allows to inspect impostor's texture rendering through Frame Debugger
            if (Application.isEditor && UnityEditor.EditorApplication.isPaused)
                return;
#endif
            ClearPreviousCommandBuffer();
        }

        protected override void SubscribeToOnPreCull()
        {
            Camera.onPreCull += OnPreCullCalled;
        }

        protected override void UnsubscribeFromOnPreCull()
        {
            Camera.onPreCull -= OnPreCullCalled;
        }

        public override void ScheduleImpostorTextureRendering(CommandBuffer commandBuffer)
        {
            ClearPreviousCommandBuffer();
            switch (ImpostorRenderingType)
            {
                case ImpostorTextureRenderingType.Scheduled:
                    var cameraEvent = GetCameraEvent();
                    _cameraWhereToScheduleCommandBufferExecution.AddCommandBuffer(cameraEvent, commandBuffer);
                    _previousCommandBuffer = commandBuffer;
                    break;
                case ImpostorTextureRenderingType.Immediately:
                    Graphics.ExecuteCommandBuffer(commandBuffer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ImpostorRenderingType));
            }
        }

        public override void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer,
            Camera camera,
            int submeshIndex, MaterialPropertyBlock materialPropertyBlock, bool castShadows, bool receiveShadows,
            bool useLightProbes)
        {
            Graphics.DrawMesh(mesh, position, rotation, material, layer, camera, submeshIndex, materialPropertyBlock,
                castShadows, receiveShadows, useLightProbes);
        }

        [ContextMenu("Clear Previous Command Buffer")]
        private void ClearPreviousCommandBuffer()
        {
            if (_previousCommandBuffer != null)
            {
                var cameraEvent = GetCameraEvent();
                _cameraWhereToScheduleCommandBufferExecution.RemoveCommandBuffer(cameraEvent, _previousCommandBuffer);
                _previousCommandBuffer = null;
            }
        }

        private CameraEvent GetCameraEvent()
        {
            switch (_cameraWhereToScheduleCommandBufferExecution.actualRenderingPath)
            {
                case RenderingPath.Forward:
                    return ForwardRenderingCameraEvent;
                case RenderingPath.DeferredShading:
                    return DeferredRenderingCameraEvent;
                default:
                    Debug.LogError(
                        $"Unsupported rendering path: '{_cameraWhereToScheduleCommandBufferExecution.actualRenderingPath}'. " +
                        $"Either change rendering path or provide custom {nameof(RenderPipelineProxy)}");
                    return ForwardRenderingCameraEvent;
            }
        }
    }
}