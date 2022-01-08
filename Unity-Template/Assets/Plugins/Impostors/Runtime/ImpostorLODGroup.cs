using System;
using System.Collections.Generic;
using System.Linq;
using Impostors.Managers;
using Impostors.RenderInstructions;
using Impostors.Structs;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Impostors
{
    [RequireComponent(typeof(LODGroup))]
    [DisallowMultipleComponent]
    public class ImpostorLODGroup : MonoBehaviour
    {
        private LODGroup _lodGroup;
        private Transform _transform;

        public bool isStatic = true;

        [Tooltip("List of Impostor Level Of Details, like in LODGroup component. " +
                 "Each LOD contains renderers and screen transition height.\n\n" +
                 "First LOD must be empty!")]
        [SerializeField]
        private ImpostorLOD[] _lods = new ImpostorLOD[]
        {
            new ImpostorLOD(0.1f, new Renderer[0]),
            new ImpostorLOD(0.01f, new Renderer[0]),
        };

        [Tooltip(
            "GENERATED.\nBounds size of ImpostorLODGroup. This value is used to determine whether ImpostorLODGroup is visible in the camera.")]
        [SerializeField]
        private Vector3 _size;

        [Tooltip("GENERATED.\nSize of quad that will be generated for impostor.")]
        [SerializeField]
        private float _quadSize;

        [Tooltip("Determines how far will be generated impostor quad from center of object. " +
                 "This is useful when impostors intersects with ground. " +
                 "Default value of 0.5f works in most cases")]
        [Range(0f, 1f)]
        public float zOffset = 0.5f;

        [Tooltip("Time in seconds that is needed for impostor to fade in/out when impostor changes texture.")]
        [Range(0f, 1f)]
        public float fadeTransitionTime = 0.2f;

        [Tooltip("Euler angles that determines how much direction from camera should change to cause texture update. " +
                 "The less this value, the more often impostor's texture will be updated.")]
        [Min(0.1f)]
        public float deltaCameraAngle = 1f;

        [Tooltip("Determines relative distance change between camera and object that will cause texture update. " +
                 "The less this value, the more often impostor's texture will be updated.")]
        [Min(0.01f)]
        public float deltaDistance = .1f;

        [Tooltip("Check this to update impostor's texture over time.")]
        public bool useUpdateByTime = false;

        [Tooltip("Time in seconds, after which impostor's texture will be updated. " +
                 "The less this value, the more often impostor's texture will be updated. " +
                 "Check 'useUpdateByTime' to take this setting into account.")]
        [Min(0.01f)]
        public float timeInterval = 1f;

        [Tooltip("Check this to update impostor's texture when main directional light changes direction.")]
        public bool useDeltaLightAngle = true;

        [Tooltip("Determines how much direction of main light should change to cause texture update. " +
                 "The less this value, the more often impostor's texture will be updated.\n" +
                 "In euler angles.")]
        [Min(0.01f)]
        public float deltaLightAngle = 3;

        public TextureResolution minTextureResolution = TextureResolution._32x32;
        public TextureResolution maxTextureResolution = TextureResolution._512x512;

        internal int IndexInImpostorsManager = -1;

        public Vector3 Position => _transform.TransformPoint(_lodGroup.localReferencePoint);

        public Vector3 LocalReferencePoint => _lodGroup.localReferencePoint;

        public float LocalHeight => _lodGroup.size * Mathf.Abs(_transform.lossyScale.y);

        public float ZOffsetWorld => _quadSize * zOffset;

        public float FadeInTime => _lodGroup.fadeMode != LODFadeMode.None ? 0.3f : 0f;

        public float FadeOutTime => _lodGroup.fadeMode != LODFadeMode.None ? 1.5f : 0f;

        public float ScreenRelativeTransitionHeight => _lods[0].screenRelativeTransitionHeight;

        public float ScreenRelativeTransitionHeightCull => _lods[_lods.Length - 1].screenRelativeTransitionHeight;

        public float QuadSize => _quadSize;

        public Vector3 Size => _size;

        /// <summary>
        /// Sets impostor's LODs. In most cases you need to use <see cref="SetLODsAndCache"/> instead.
        /// </summary>
        public ImpostorLOD[] LODs
        {
            get { return _lods; }
            set { _lods = value; }
        }

        private bool _isGameObjectWillBeDestroyed;
        
        private static readonly int matFrom = 0;
        private static readonly int matTo = 100;

        #region CACHE

        private float[] _lodGroupOriginalScreenRelativeTransitionHeights;
        private Dictionary<Renderer, RenderInstructionBuffer> _dictRendererToRenderInstructionBuffer;

        #endregion

        private void Awake()
        {
            _lodGroup = GetComponent<LODGroup>();
            _lodGroup.RecalculateBounds();
            _transform = transform;
        }

        private void OnEnable()
        {
            IndexInImpostorsManager = ImpostorLODGroupsManager.Instance.AddImpostorLODGroup(this);
            var lods = _lodGroup.GetLODs();
            _lodGroupOriginalScreenRelativeTransitionHeights = new float[lods.Length];
            float minValue = ScreenRelativeTransitionHeight;
            for (int i = lods.Length - 1; i >= 0; i--)
            {
                _lodGroupOriginalScreenRelativeTransitionHeights[i] = lods[i].screenRelativeTransitionHeight;
                lods[i].screenRelativeTransitionHeight = Mathf.Clamp(lods[i].screenRelativeTransitionHeight,
                    minValue, 1);
                minValue += 0.000001f;
            }

            _lodGroup.SetLODs(lods);
            
            // Note: in Editor static batching works a bit different than in build.
            // In Editor it combines meshes on the fly after all Awake/OnEnable calls, which means we are getting source mesh.
            // In Build meshes are combined in build time which means that in Awake/OnEnable we will get the already combined mesh.
            Cache();
        }

        private void OnDisable()
        {
            ImpostorLODGroupsManager.Instance.RemoveImpostorLODGroup(this);
            IndexInImpostorsManager = -1;
            if (_isGameObjectWillBeDestroyed)
                return;
            var lods = _lodGroup.GetLODs();
            float minValue =
                _lodGroupOriginalScreenRelativeTransitionHeights[
                    _lodGroupOriginalScreenRelativeTransitionHeights.Length - 1];
            for (int i = _lodGroupOriginalScreenRelativeTransitionHeights.Length - 1; i >= 0; i--)
            {
                lods[i].screenRelativeTransitionHeight =
                    Mathf.Max(_lodGroupOriginalScreenRelativeTransitionHeights[i], minValue);
                minValue += 0.000001f;
            }

            _lodGroup.SetLODs(lods);
        }

        private void OnValidate()
        {
            var lodGroup = GetComponent<LODGroup>();
            Debug.Assert(lodGroup);
            float lodGroupCullHeight = lodGroup.GetLODs().Last().screenRelativeTransitionHeight;
            if (_lods[0].screenRelativeTransitionHeight < lodGroupCullHeight)
                _lods[0].screenRelativeTransitionHeight = lodGroupCullHeight;
        }

        internal void AddCommandBufferCommands(CommandBufferProxy bufferProxy, Vector3 cameraPosition,
            float screenSize,
            List<SphericalHarmonicsL2> lightProbes, int lightProbeIndex)
        {
            var cb = bufferProxy.CommandBuffer;
            Vector3 locBillPos = Position;
            Vector3 fromCamToCenter = cameraPosition - locBillPos;

            Quaternion renderingCameraRotation = Quaternion.LookRotation(-fromCamToCenter);
            float impostorQuadSize = _quadSize;

            fromCamToCenter = cameraPosition - locBillPos - fromCamToCenter.normalized * ZOffsetWorld;

            float angleForCamera = 2 * Mathf.Atan2(impostorQuadSize * 0.5f, fromCamToCenter.magnitude) * Mathf.Rad2Deg;
            float zFar = fromCamToCenter.magnitude + QuadSize * 1.5f;
            float zNear = Mathf.Max(fromCamToCenter.magnitude - QuadSize * 1.5f, 0.3f);

            Matrix4x4 V = Matrix4x4.TRS(cameraPosition, renderingCameraRotation, new Vector3(1, 1, -1))
                .inverse;
            Matrix4x4 p = Matrix4x4.Perspective(angleForCamera, 1, zNear, zFar);

            Profiler.BeginSample("Config command buffer");
            cb.SetViewProjectionMatrices(V, p);
            cb.SetGlobalVector(ShaderProperties._WorldSpaceCameraPos, cameraPosition);
            cb.SetGlobalVector(ShaderProperties._ProjectionParams, new Vector4(-1, zNear, zFar, 1 / zFar));

            int lodLevel = -1;
            for (int i = 0; i < _lods.Length; i++)
            {
                lodLevel = i;
                if (_lods[i].screenRelativeTransitionHeight < screenSize)
                    break;
            }

            if (lodLevel == 0 || lodLevel == -1)
                Debug.LogError("This must not happen");

            var renderers = _lods[lodLevel].renderers;
            for (int i = 0; i < renderers.Length; i++)
            {
                var rend = renderers[i];
                var buff = _dictRendererToRenderInstructionBuffer[rend];
                if (buff == null)
                {
                    Debug.LogError($"[IMPOSTORS] There is no RenderInstructionBuffer for {rend.name}. " +
                                   $"Something went wrong. If you often see this message, please report a bug.", this);
                    continue;
                }

                buff.PropertyBlock.CopySHCoefficientArraysFrom(lightProbes, lightProbeIndex, 0, 1);
                buff.Apply(bufferProxy);
            }

            Profiler.EndSample();
        }

        [ContextMenu("Recalculate Bounds")]
        public void RecalculateBounds()
        {
            Bounds bound = new Bounds();
            var renderers = _lods.SelectMany(lod => lod.renderers);
            
            foreach (Renderer r in renderers)
            {
                if (r == null)
                    continue;
                if (bound.extents == Vector3.zero)
                    bound = r.bounds;
                else
                    bound.Encapsulate(r.bounds);
            }

            _size = bound.size;
            _quadSize = ImpostorsUtility.MaxV3(_size);
        }

        [ContextMenu("Update Settings")]
        public void UpdateSettings()
        {
            if (IndexInImpostorsManager != -1)
                ImpostorLODGroupsManager.Instance.UpdateSettings(this);
        }

        [ContextMenu("Cache")]
        public void Cache()
        {
            CreateRenderInstructionsBuffers();
        }

        /// <summary>
        /// Sets LODs and runs additional calculation to update settings.
        ///   - recalculates bound,
        ///   - caches render instructions,
        ///   - updates settings in ImpostorManager
        /// </summary>
        /// <param name="lods"></param>
        public void SetLODsAndCache(ImpostorLOD[] lods)
        {
            LODs = lods;
            RecalculateBounds();
            Cache();
            UpdateSettings();
        }

        /// <summary>
        /// If impostor is created, forces it's texture to update, otherwise, throws an exception.
        /// </summary>
        [ContextMenu("Request Impostor Texture Update")]
        public void RequestImpostorTextureUpdate()
        {
            if (IndexInImpostorsManager == -1)
                throw new Exception("Cannot update impostor texture because impostor is not present in the system.");
            ImpostorLODGroupsManager.Instance.RequestImpostorTextureUpdate(this);
        }

        /// <summary>
        /// Sets flag indicating whether GameObject containing this ImpostorLODGroup component will be destroyed.
        /// This will prevent from costly allocations in OnDisable stage. The effect from this call cannot be undone.
        /// </summary>
        public void MarkGameObjectWillBeDestroyed() => _isGameObjectWillBeDestroyed = true;

        private void CreateRenderInstructionsBuffers()
        {
            var renderers = CollectionsPool.GetListOfRenderers();
            {
                var hasSet = CollectionsPool.GetHashSetOfRenderers();
                for (int i = 0; i < _lods.Length; i++)
                {
                    var rs = _lods[i].renderers;
                    for (int j = 0; j < rs.Length; j++)
                        hasSet.Add(rs[j]);
                }

                renderers.AddRange(hasSet);
            }


            // Disabling renderers that are not presented in LODGroup to make them invisible.
            // (LODGroup doesn't control renderers that are not present in any of its LOD level)
            {
                var lodRenderers = CollectionsPool.GetHashSetOfRenderers();
                lodRenderers.UnionWith(_lodGroup.GetLODs().SelectMany(x => x.renderers));
                foreach (var impostorRenderer in renderers)
                {
                    if (lodRenderers.Contains(impostorRenderer) == false)
                        impostorRenderer.enabled = false;
                }
            }

            if (_dictRendererToRenderInstructionBuffer == null)
                _dictRendererToRenderInstructionBuffer =
                    new Dictionary<Renderer, RenderInstructionBuffer>(renderers.Count);
            _dictRendererToRenderInstructionBuffer.Clear();

            var sharedMaterials = CollectionsPool.GetListOfMaterials();
            var lightmaps = CollectionsPool.GetLightmaps();

            for (int i = 0; i < renderers.Count; i++)
            {
                var renderer = renderers[i];
                var builder = CollectionsPool.GetRenderInstructionBufferBuilder();
                renderer.GetSharedMaterials(sharedMaterials);
                builder.Begin(5 + sharedMaterials.Count);
                renderer.GetPropertyBlock(builder.PropertyBlock);

                SetupLightmaps(builder, renderer, lightmaps);
                
                bool isDrawRenderer = renderer.isPartOfStaticBatch || !(renderer is MeshRenderer) || isStatic == false;
                if (isDrawRenderer)
                {
                    DrawRenderer(renderer, builder, sharedMaterials);
                }
                else
                {
                    DrawMesh(renderer, builder, sharedMaterials);
                }

                _dictRendererToRenderInstructionBuffer.Add(renderer, builder.Build());
            }
        }

        private static void SetupLightmaps(RenderInstructionBufferBuilder builder, Renderer renderer,
            LightmapData[] lightmaps)
        {
            bool hasLightmap = renderer.lightmapIndex != -1;

            if (!hasLightmap)
            {
                builder.DisableShaderKeyword(ShaderKeywords.LIGHTMAP_ON);
                builder.DisableShaderKeyword(ShaderKeywords.DIRLIGHTMAP_COMBINED);
                builder.DisableShaderKeyword(ShaderKeywords.SHADOWS_SHADOWMASK);
                builder.EnableShaderKeyword(ShaderKeywords.LIGHTPROBE_SH);
                return;
            }

            builder.DisableShaderKeyword(ShaderKeywords.LIGHTPROBE_SH);
            builder.EnableShaderKeyword(ShaderKeywords.LIGHTMAP_ON);
            var lightmapData = lightmaps[renderer.lightmapIndex];

            builder.PropertyBlock.SetTexture(ShaderProperties.unity_Lightmap, lightmapData.lightmapColor);
            builder.PropertyBlock.SetVector(ShaderProperties.unity_LightmapST, renderer.lightmapScaleOffset);

            if (lightmapData.shadowMask == null)
            {
                builder.DisableShaderKeyword(ShaderKeywords.SHADOWS_SHADOWMASK);
#if IMPOSTORS_UNITY_PIPELINE_URP
                builder.PropertyBlock.SetVector(ShaderProperties._MainLightColor, Vector4.zero);
#else
                builder.PropertyBlock.SetVector(ShaderProperties._LightColor0, Vector4.zero);
#endif
            }
            else
            {
                builder.EnableShaderKeyword(ShaderKeywords.SHADOWS_SHADOWMASK);
                builder.PropertyBlock.SetTexture(ShaderProperties.unity_ShadowMask, lightmapData.shadowMask);
            }

            if (lightmapData.lightmapDir)
            {
                builder.EnableShaderKeyword(ShaderKeywords.DIRLIGHTMAP_COMBINED);
                builder.PropertyBlock.SetTexture(ShaderProperties.unity_LightmapInd, lightmapData.lightmapDir);
            }
            else
            {
                builder.DisableShaderKeyword(ShaderKeywords.DIRLIGHTMAP_COMBINED);
            }
        }

        private static void DrawMesh(Renderer renderer, RenderInstructionBufferBuilder builder,
            List<Material> sharedMaterials)
        {
            Mesh mesh = null;
            if (renderer is MeshRenderer)
                mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            else
            {
                Debug.LogError(
                    $"[IMPOSTORS] Unsupported renderer type '{renderer.GetType().Name}' on object called '{renderer.name}'. " +
                    $"Please, remove it from impostor's LODs.",
                    renderer);
                return;
            }

            var matrix = renderer.localToWorldMatrix;
            var lossyScale = matrix.lossyScale;
            bool requiresInvertCulling = Mathf.Sign(lossyScale.x * lossyScale.y * lossyScale.z) < 0;
            if (requiresInvertCulling)
                builder.SetInvertCulling(true);
            for (int submeshIndex = matFrom;
                submeshIndex < Mathf.Min(sharedMaterials.Count, matTo);
                submeshIndex++)
            {
                builder.AddRenderInstruction(new DrawMeshInstruction(
                    mesh,
                    matrix,
                    sharedMaterials[submeshIndex],
                    submeshIndex,
                    shaderPass: 0,
                    builder.PropertyBlock
                ));
            }

            if (requiresInvertCulling)
                builder.SetInvertCulling(false);
        }

        private static void DrawRenderer(Renderer renderer, RenderInstructionBufferBuilder builder,
            List<Material> sharedMaterials)
        {
            for (int submeshIndex = matFrom;
                submeshIndex < Mathf.Min(sharedMaterials.Count, matTo);
                submeshIndex++)
            {
                builder.AddRenderInstruction(new DrawRendererInstruction(
                    renderer,
                    sharedMaterials[submeshIndex],
                    submeshIndex,
                    shaderPass: 0,
                    builder.PropertyBlock
                ));
            }
        }
    }
}