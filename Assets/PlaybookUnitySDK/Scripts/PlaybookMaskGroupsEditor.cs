using UnityEditor;
using UnityEngine;

namespace PlaybookUnitySDK.Scripts
{
    [CustomEditor(typeof(PlaybookMaskGroups))]
    public class PlaybookMaskGroupsEditor : Editor
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
