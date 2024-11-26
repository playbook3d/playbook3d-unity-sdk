using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace PlaybookUnitySDK.Scripts
{
    [CustomEditor(typeof(PlaybookSDK))]
    public class PlaybookSDKEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PlaybookSDK playbookSDK = target as PlaybookSDK;

            Assert.IsNotNull(playbookSDK);

            // Disable buttons if not in Play mode
            bool isInPlayMode = EditorApplication.isPlaying;

            if (!isInPlayMode)
            {
                GUILayout.Label("Enter Play mode to start capturing.");
            }

            GUILayout.Label("Capture Renders", EditorStyles.boldLabel);

            // Don't allow user to capture image while capturing image sequence
            GUI.enabled = isInPlayMode && !playbookSDK.IsCapturingImageSequence;
            if (GUILayout.Button("Capture Image"))
            {
                playbookSDK.InvokeCaptureImage();

                EditorUtility.SetDirty(playbookSDK);
            }

            GUILayout.Space(10);

            // Enable start capture button if not already capturing
            GUI.enabled = isInPlayMode && !playbookSDK.IsCapturingImageSequence;
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
        }
    }
}
