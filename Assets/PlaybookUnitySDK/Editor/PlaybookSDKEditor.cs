using System;
using PlaybookUnitySDK.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace PlaybookUnitySDK.Editor
{
    [CustomEditor(typeof(PlaybookSDK))]
    public class PlaybookSDKEditor : UnityEditor.Editor
    {
        private SerializedProperty _playbookAccountAPIKey;
        private SerializedProperty _framesPerSecond;
        private SerializedProperty _maxFrames;

        private void OnEnable()
        {
            _playbookAccountAPIKey = serializedObject.FindProperty("playbookAccountAPIKey");
            _framesPerSecond = serializedObject.FindProperty("framesPerSecond");
            _maxFrames = serializedObject.FindProperty("maxFrames");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PlaybookSDK playbookSDK = (PlaybookSDK)target;

            GUIStyle largeBoldStyle =
                new(EditorStyles.label) { fontSize = 13, fontStyle = FontStyle.Bold };

            if (!string.IsNullOrEmpty(((PlaybookSDK)target).ResultImageUrl))
            {
                GUILayout.Label("Result Image URL", largeBoldStyle);
                GUILayout.TextField(((PlaybookSDK)target).ResultImageUrl);
            }

            GUILayout.Space(10);
            EditorGUILayout.PropertyField(_playbookAccountAPIKey);

            // Render Properties
            GUILayout.Space(10);
            GUILayout.Label("Render Properties", largeBoldStyle);
            EditorGUILayout.PropertyField(_framesPerSecond);
            EditorGUILayout.PropertyField(_maxFrames);

            // Workflow Properties
            GUILayout.Space(10);
            GUILayout.Label("Workflow Properties", largeBoldStyle);

            string[] teamsList = ((PlaybookSDK)target).GetTeams();
            string[] workflowsList = ((PlaybookSDK)target).GetWorkflows();

            PlaybookNetwork.CurrTeamIndex = EditorGUILayout.Popup(
                "Teams",
                PlaybookNetwork.CurrTeamIndex,
                teamsList
            );

            PlaybookNetwork.CurrWorkflowIndex = EditorGUILayout.Popup(
                "Workflows",
                PlaybookNetwork.CurrWorkflowIndex,
                workflowsList
            );

            // Capture Renders
            GUILayout.Space(10);
            GUILayout.Label("Capture Renders", largeBoldStyle);

            // Disable buttons if not in Play mode
            bool isInPlayMode = EditorApplication.isPlaying;

            // Disable buttons if teams not loaded
            bool teamsLoaded = teamsList[0] != "None";

            if (!isInPlayMode)
            {
                GUI.color = Color.red;
                GUILayout.Label("Enter Play mode to start capturing.", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            else if (!teamsLoaded)
            {
                GUI.color = Color.red;
                GUILayout.Label("Credentials have not been loaded in.", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }

            bool flags = isInPlayMode; // && teamsLoaded;

            // Don't allow user to capture image while capturing image sequence
            GUI.enabled = flags && !playbookSDK.IsCapturingImageSequence;
            if (GUILayout.Button("Capture Image"))
            {
                playbookSDK.InvokeCaptureImage();

                EditorUtility.SetDirty(playbookSDK);
            }

            GUILayout.Space(10);

            // Enable start capture button if not already capturing
            GUI.enabled = flags && !playbookSDK.IsCapturingImageSequence;
            if (GUILayout.Button("Start Capture Image Sequence"))
            {
                playbookSDK.StartCaptureImageSequence();

                EditorUtility.SetDirty(playbookSDK);
            }

            // Enable stop capture button if in the process of capturing
            GUI.enabled = isInPlayMode && playbookSDK.IsCapturingImageSequence;
            if (GUILayout.Button("Stop Capture Image Sequence"))
            {
                playbookSDK.StopCaptureImageSequence();

                EditorUtility.SetDirty(playbookSDK);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
