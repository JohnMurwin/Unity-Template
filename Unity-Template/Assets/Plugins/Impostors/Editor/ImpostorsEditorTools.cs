using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Impostors.Managers;
using Impostors.Structs;
using Impostors.RenderPipelineProxy;
#if IMPOSTORS_UNITY_PIPELINE_URP
using Impostors.URP;
#endif
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Impostors.Editor
{
    public static class ImpostorsEditorTools
    {
        [MenuItem("Tools/Impostors/Create Scene Managers")]
        public static void CreateSceneManagers()
        {
            var impostorLodGroupManager = Object.FindObjectOfType<ImpostorLODGroupsManager>();
            if (impostorLodGroupManager == null)
            {
                impostorLodGroupManager = new GameObject("IMPOSTORS").AddComponent<ImpostorLODGroupsManager>();
                impostorLodGroupManager.transform.SetAsFirstSibling();

                Undo.RegisterCreatedObjectUndo(impostorLodGroupManager.gameObject, "impostors manager");
            }

            var mainCameras = Object.FindObjectsOfType<Camera>().Where(x => x.CompareTag("MainCamera")).ToArray();
            if (mainCameras.Length > 1)
            {
                EditorUtility.DisplayDialog("Impostors: Create Scene Managers",
                    $"Failed!\nThere are {mainCameras.Length} cameras with 'MainCamera' tag in the scene. " +
                    $"Cannot automatically decide which cameras should render impostors.\n\n" +
                    "Select desired camera in hierarchy and run 'Tools/Impostors/Create Camera Manager(s)' to complete system setup.",
                    "Ok");
                return;
            }
            
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                EditorUtility.DisplayDialog("Impostors: Create Scene Managers",
                    "Failed!\nCannot find MainCamera in the scene.\n\n" +
                    "Select desired camera in hierarchy and run 'Tools/Impostors/Create Camera Manager(s)' to complete system setup.",
                    "Ok");
                return;
            }

            CreateManagerForCamera(mainCamera, impostorLodGroupManager.transform);

            EditorUtility.DisplayDialog("Impostors: Create Scene Managers",
                "Successfully created camera manager for:\n" +
                $"'{GetFullGameObjectPath(mainCamera.transform)}'", "Ok");

            EditorGUIUtility.PingObject(impostorLodGroupManager);
        }

        [MenuItem("Tools/Impostors/Create Camera Manager(s)")]
        public static void CreatePerCameraManagers()
        {
            var sceneManager = Object.FindObjectOfType<ImpostorLODGroupsManager>();
            if (sceneManager == null)
            {
                EditorUtility.DisplayDialog("Impostors: Create Camera Manager(s)",
                    $"Failed!\nThis command requires instance of '{nameof(ImpostorLODGroupsManager)}' in the scene.\n\n" +
                    $"Please, run 'Tools/Impostors/Create Scene Managers' before calling this command.", "Ok");
                return;
            }

            var cameras = Selection.gameObjects
                .Select(x => x.GetComponent<Camera>())
                .Where(x => x != null).ToArray();

            if (cameras.Length == 0)
            {
                EditorUtility.DisplayDialog("Impostors: Create Camera Manager(s)",
                    "Failed!\nThis command requires you to select at least one GameObject with a Camera component.",
                    "Ok");
                return;
            }

            foreach (var camera in cameras)
            {
                if (camera.GetComponent<CameraImpostorsManager>())
                {
                    continue;
                }

                var go = CreateManagerForCamera(camera, sceneManager.transform);
                Undo.RegisterCreatedObjectUndo(go, "Create Camera Manager");
            }
        }

        public static GameObject CreateManagerForCamera(Camera camera, Transform parent)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));
            var go = new GameObject($"{camera.name} Impostors");
            
            Type renderPipelineProxyType = RenderPipelineProxyTypeProvider.Get();
            RenderPipelineProxyBase renderPipelineProxy =
                (RenderPipelineProxyBase) go.AddComponent(renderPipelineProxyType);
            
            go.SetActive(false);
            var impostorableObjectsManager = go.AddComponent<CameraImpostorsManager>();
            impostorableObjectsManager.transform.SetParent(parent);
            var type = typeof(CameraImpostorsManager);
            impostorableObjectsManager.mainCamera = camera;
            
            type.GetField("_directionalLight", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(
                impostorableObjectsManager,
                Object.FindObjectsOfType<Light>().FirstOrDefault(x => x.type == LightType.Directional));


            type.GetField("_renderPipelineProxy", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(
                impostorableObjectsManager,
                renderPipelineProxy);

            renderPipelineProxy.GetType().GetField("_cameraWhereToScheduleCommandBufferExecution",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(
                renderPipelineProxy,
                camera);
            
            UnityEditorInternal.ComponentUtility.MoveComponentDown(renderPipelineProxy);

            go.SetActive(true);
            return go;
        }

        [MenuItem("Tools/Impostors/Setup Impostor(s)")]
        public static void SetupImpostorLODGroups()
        {
            Transform[] selected = Selection.transforms;
            if (selected.Length == 0)
            {
                EditorUtility.DisplayDialog("Impostors: Setup Impostor(s)",
                    "Failed!\nPlease, select gameObjects with LODGroup component to setup impostors.",
                    "Ok");
                return;
            }

            Undo.SetCurrentGroupName("Setup Impostor(s)");
            bool hasProblems = false;

            foreach (Transform trans in selected)
            {
                var lodGroup = trans.GetComponent<LODGroup>();
                if (lodGroup == null && lodGroup.GetComponent<ImpostorLODGroup>() == null)
                {
                    hasProblems = true;
                    Debug.LogError(
                        $"[Impostors] Cannot add {nameof(ImpostorLODGroup)} to '{trans.name}' because it has no {nameof(LODGroup)} component. Impostors work only in combination with {nameof(LODGroup)} component. Click to navigate to this object.",
                        trans);
                    continue;
                }

                SetupImpostorLODGroupToObject(lodGroup);
            }

            if (hasProblems)
                EditorUtility.DisplayDialog(
                    "Impostors: Setup Impostor(s)",
                    "Automatic imposter setup faced some problems. Look at the console for more details.",
                    "Ok");
        }

        [MenuItem("Tools/Impostors/Remove Impostor(s)")]
        public static void RemoveImpostorLODGroups()
        {
            Transform[] _selected = Selection.transforms;
            if (_selected.Length == 0)
            {
                EditorUtility.DisplayDialog("Impostors: Remove Impostor(s)",
                    "Failed!\nNo gameObject selected. Please, select gameObject to remove imposter.",
                    "Ok");
                return;
            }

            Undo.SetCurrentGroupName("Remove Impostor(s)");

            ImpostorLODGroup[] ilods;
            foreach (Transform trans in _selected)
            {
                ilods = trans.GetComponentsInChildren<ImpostorLODGroup>(true);
                for (int i = 0; i < ilods.Length; i++)
                {
                    Undo.DestroyObjectImmediate(ilods[i]);
                }
            }
        }

        [MenuItem("Tools/Impostors/Playmode/Optimize Scene")]
        public static void OptimizeScene()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Impostors: Optimize Scene command",
                    "Failed!\nThis command is meant to be used in Playmode only!\n\n" +
                    "For more information about this command please look in the Documentation.",
                    "Ok");
                return;
            }

            Undo.SetCurrentGroupName("Optimize Scene");
            if (Object.FindObjectOfType<ImpostorLODGroupsManager>() == null)
                CreateSceneManagers();
            var lodGroups = Object.FindObjectsOfType<LODGroup>();
            var title = "Optimizing Scene with Impostors";
            int i = 0;
            int count = 0;
            foreach (var lodGroup in lodGroups)
            {
                i++;
                if (lodGroup.size * ImpostorsUtility.MaxV3(lodGroup.transform.lossyScale) < 2f)
                    continue;
                count++;
                EditorUtility.DisplayProgressBar(title, lodGroup.name, i / (float) lodGroups.Length);
                if (lodGroup.GetComponent<ImpostorLODGroup>() == null)
                    SetupImpostorLODGroupToObject(lodGroup);
            }

            EditorUtility.ClearProgressBar();
            Debug.Log(
                $"[Impostors: Optimize Scene command] Populated scene with {count} {nameof(ImpostorLODGroup)} components");
        }

        [MenuItem("Tools/Impostors/Playmode/Enable Impostors")]
        public static void EnableImpostors()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Impostors: Enable Impostors command",
                    "Failed!\nThis command is meant to be used in Playmode only.",
                    "Ok");
                return;
            }

            var lodGroups = Object.FindObjectsOfType<ImpostorLODGroup>();
            foreach (var lodGroup in lodGroups)
            {
                lodGroup.enabled = true;
            }
        }

        [MenuItem("Tools/Impostors/Playmode/Disable Impostors")]
        public static void DisableImpostors()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Impostors: Enable Impostors command",
                    "Failed!\nThis command is meant to be used in Playmode only.",
                    "Ok");
                return;
            }

            var lodGroups = Object.FindObjectsOfType<ImpostorLODGroup>();
            foreach (var lodGroup in lodGroups)
            {
                lodGroup.enabled = false;
            }
        }


        private static void SetupImpostorLODGroupToObject(LODGroup lodGroup)
        {
            var lods = lodGroup.GetLODs();
            Undo.RegisterCompleteObjectUndo(lodGroup.gameObject, "Setup ImpostorLODGroup");
            lodGroup.gameObject.SetActive(false);
            var impostorLODGroup = Undo.AddComponent<ImpostorLODGroup>(lodGroup.gameObject);
            ImpostorLOD impostorLodEmpty = new ImpostorLOD();
            impostorLodEmpty.screenRelativeTransitionHeight =
                Mathf.Clamp(lods[lods.Length - 1].screenRelativeTransitionHeight, 0.1f, 0.9f);
            impostorLodEmpty.renderers = new Renderer[0];
            ImpostorLOD impostorLOD = new ImpostorLOD();
            impostorLOD.screenRelativeTransitionHeight = 0.005f;
            impostorLOD.renderers = lods.Last().renderers;
            impostorLODGroup.zOffset = .25f;

            impostorLODGroup.LODs = new[] {impostorLodEmpty, impostorLOD};

            impostorLODGroup.RecalculateBounds();
            lodGroup.gameObject.SetActive(true);
        }

        private static void DestroyComponentsIfAny<T>(this Component component) where T : Component
        {
            var components = component.GetComponents<T>();
            foreach (var c in components)
            {
                Object.Destroy(c);
            }
        }

        private static string GetFullGameObjectPath(Transform t)
        {
            string path = t.gameObject.scene.name + "/" + string.Join("/",
                t.GetComponentsInParent<Transform>().Select(x => x.name).Reverse().ToArray());

            return path;
        }
    }
}