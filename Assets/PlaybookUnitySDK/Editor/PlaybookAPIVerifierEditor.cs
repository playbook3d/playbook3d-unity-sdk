using System;
using System.Linq;
using PlaybookUnitySDK.Scripts;
using UnityEditor;
using UnityEngine;

namespace PlaybookUnitySDK.Editor
{
    [CustomEditor(typeof(PlaybookAPIVerifier))]
    public class PlaybookAPIVerifierEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            string[] teamsList = PlaybookAPIVerifier.GetTeams();
            string[] workflowsList = PlaybookAPIVerifier.GetWorkflows();

            PlaybookAPIVerifier.CurrTeamIndex = EditorGUILayout.Popup(
                "Teams",
                PlaybookAPIVerifier.CurrTeamIndex,
                teamsList
            );

            PlaybookAPIVerifier.CurrWorkflowIndex = EditorGUILayout.Popup(
                "Workflows",
                PlaybookAPIVerifier.CurrWorkflowIndex,
                workflowsList
            );
        }
    }
}
