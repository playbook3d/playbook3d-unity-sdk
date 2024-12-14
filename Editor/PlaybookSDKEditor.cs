using System;
using PlaybookUnitySDK.Runtime;
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
        private SerializedProperty _debugLevel;

        private void OnEnable()
        {
            _playbookAccountAPIKey = serializedObject.FindProperty("playbookAccountAPIKey");
            _framesPerSecond = serializedObject.FindProperty("framesPerSecond");
            _maxFrames = serializedObject.FindProperty("maxFrames");
            _debugLevel = serializedObject.FindProperty("sdkDebugLevel");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PlaybookSDK playbookSDK = (PlaybookSDK)target;

            GUIStyle largeBoldStyle =
                new(EditorStyles.label) { fontSize = 13, fontStyle = FontStyle.Bold };

            // Render results
            if (playbookSDK.ResultImageUrls.Count != 0)
            {
                GUILayout.Label("Result Image URLs", largeBoldStyle);
                foreach (string resultImageUrl in playbookSDK.ResultImageUrls)
                {
                    GUILayout.Label(resultImageUrl);

                    if (GUILayout.Button("Copy URL"))
                    {
                        PlaybookSDK.CopyToClipboard(resultImageUrl);
                    }

                    Repaint();
                }
                GUILayout.Space(20);
            }

            EditorGUILayout.PropertyField(_playbookAccountAPIKey);

            // Render Properties
            GUILayout.Space(10);
            GUILayout.Label("Render Properties", largeBoldStyle);
            EditorGUILayout.PropertyField(_framesPerSecond);
            EditorGUILayout.PropertyField(_maxFrames);

            // Workflow Properties
            GUILayout.Space(10);
            GUILayout.Label("Workflow Properties", largeBoldStyle);

            string[] teamsList = playbookSDK.GetTeams();
            string[] workflowsList = playbookSDK.GetWorkflows();

            playbookSDK.CurrTeamIndex = EditorGUILayout.Popup(
                "Teams",
                playbookSDK.CurrTeamIndex,
                teamsList
            );

            playbookSDK.CurrWorkflowIndex = EditorGUILayout.Popup(
                "Workflows",
                playbookSDK.CurrWorkflowIndex,
                workflowsList
            );

            // Capture Renders
            GUILayout.Space(10);
            GUILayout.Label("Run Workflow", largeBoldStyle);

            EditorGUILayout.PropertyField(_debugLevel);
            GUILayout.Space(10);

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

            bool flags = isInPlayMode && teamsLoaded;

            // Don't allow user to capture image while capturing image sequence
            GUI.enabled = flags && !playbookSDK.IsCapturingImageSequence;
            if (GUILayout.Button("Capture Single Frame"))
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
