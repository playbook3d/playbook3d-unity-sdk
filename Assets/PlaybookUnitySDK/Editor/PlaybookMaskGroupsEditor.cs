using PlaybookUnitySDK.Scripts;
using UnityEditor;
using UnityEngine;

namespace PlaybookUnitySDK.Editor
{
    [CustomEditor(typeof(PlaybookMaskGroups))]
    public class PlaybookMaskGroupsEditor : UnityEditor.Editor
    {
        private const int MaxMaskGroup = 7;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty maskGroupProp = serializedObject.FindProperty("maskGroups");

            if (maskGroupProp != null)
            {
                maskGroupProp.arraySize = Mathf.Clamp(maskGroupProp.arraySize, 0, MaxMaskGroup);
            }

            EditorGUILayout.PropertyField(maskGroupProp, true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
