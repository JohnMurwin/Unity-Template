using Impostors.RenderPipelineProxy;
using UnityEditor;

namespace Impostors.Editor
{
    [CustomEditor(typeof(BuiltInRenderPipelineProxy))]
    public class BuiltInRenderPipelineProxyEditor : UnityEditor.Editor
    {
        private SerializedProperty _ImpostorRenderingType;
        private SerializedProperty _cameraWhereToScheduleCommandBufferExecution;
        
        private void OnEnable()
        {
            _ImpostorRenderingType = serializedObject.FindProperty("ImpostorRenderingType");
            _cameraWhereToScheduleCommandBufferExecution =
                serializedObject.FindProperty("_cameraWhereToScheduleCommandBufferExecution");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_ImpostorRenderingType);
            var t = target as BuiltInRenderPipelineProxy;
            if (t.ImpostorRenderingType == BuiltInRenderPipelineProxy.ImpostorTextureRenderingType.Scheduled)
            {
                EditorGUILayout.HelpBox("Scheduled rendering provides better performance, but might work incorrectly with VR projects.", MessageType.Warning);
                EditorGUILayout.PropertyField(_cameraWhereToScheduleCommandBufferExecution);
                if (_cameraWhereToScheduleCommandBufferExecution.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Please, specify camera where CommandBuffers will be scheduled.", MessageType.Error);    
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}