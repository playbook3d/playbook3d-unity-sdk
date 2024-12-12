using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlaybookUnitySDK.Scripts
{
    [RequireComponent(typeof(PlaybookCapturePasses))]
    [RequireComponent(typeof(PlaybookNetwork))]
    public class PlaybookSDK : MonoBehaviour
    {
        [SerializeField]
        private string playbookAccountAPIKey;

        [SerializeField]
        [Range(1, 24)]
        [Tooltip("The number of renders to capture per second for image sequences.")]
        private int framesPerSecond = 24;

        [SerializeField]
        [Tooltip("The max amount of frames that will be captured in an image sequence.")]
        private int maxFrames = 144;

        public bool IsCapturingImageSequence => _playbookCapturePasses.IsCapturingImageSequence;
        public string ResultImageUrl { get; set; }

        private static PlaybookSDK _instance;

        private PlaybookNetwork _playbookNetwork;
        private PlaybookCapturePasses _playbookCapturePasses;
        private PlaybookMaskPass _playbookMaskPass;

        private float _interval;
        private float _timePassed;

        private void OnValidate()
        {
            InitializeProperties();
        }

        private void Awake()
        {
            EnsureSingleton();

            InitializeProperties();

            _interval = 1f / framesPerSecond;
            _playbookNetwork.PlaybookAccountAPIKey = playbookAccountAPIKey;

            _playbookCapturePasses.ImageCaptureComplete += OnImageCaptureComplete;
            _playbookCapturePasses.ImageSequenceCaptureComplete += OnImageSequenceCaptureComplete;

            ResultImageUrl = "TEST";
        }

        private void EnsureSingleton()
        {
            if (_instance != null)
            {
                Destroy(_instance);
            }
            _instance = this;
        }

        private void InitializeProperties()
        {
            if (_playbookNetwork == null)
            {
                _playbookNetwork = GetComponent<PlaybookNetwork>();
            }

            if (_playbookCapturePasses == null)
            {
                _playbookCapturePasses = GetComponent<PlaybookCapturePasses>();
            }

            if (_playbookMaskPass == null)
            {
                _playbookMaskPass = GetComponent<PlaybookMaskPass>();
            }
        }

        private void Update()
        {
            if (!IsCapturingImageSequence)
                return;

            // Capture n frames every second
            if (_timePassed < _interval)
            {
                _timePassed += Time.deltaTime;
                return;
            }

            _playbookCapturePasses.CaptureRenderPasses();

            _timePassed = 0;
        }

        private void OnDestroy()
        {
            _playbookCapturePasses.ImageCaptureComplete -= OnImageCaptureComplete;
            _playbookCapturePasses.ImageSequenceCaptureComplete -= OnImageSequenceCaptureComplete;
        }

        #region Image Capture

        private void OnImageCaptureComplete()
        {
            _playbookNetwork.UploadImageFiles();
        }

        private void OnImageSequenceCaptureComplete()
        {
            _playbookNetwork.UploadZipFiles();
        }

        public void InvokeCaptureImage()
        {
            _playbookCapturePasses.InvokeCaptureImage();
        }

        public void StartCaptureImageSequence()
        {
            _playbookCapturePasses.StartCaptureImageSequence();
        }

        public void StopCaptureImageSequence()
        {
            _playbookCapturePasses.StopCaptureImageSequence();
        }

        #endregion

        #region PlaybookNetwork

        public string[] GetTeams() => _playbookNetwork.GetTeams();

        public string[] GetWorkflows() => _playbookNetwork.GetWorkflows();

        #endregion

        #region Mask Pass

        /// <summary>
        /// Set the mask group of the background. Background is default set to the
        /// catch-all mask group.
        /// </summary>
        public static void SetBackgroundMaskGroup(MaskGroup maskGroup)
        {
            _instance._playbookMaskPass.SetBackgroundMaskGroup(maskGroup);
        }

        public static void RemoveBackgroundMaskGroup()
        {
            _instance._playbookMaskPass.SetBackgroundMaskGroup(MaskGroup.CatchAll);
        }

        /// <summary>
        /// Add the given object to the given mask group.
        /// </summary>
        public static void AddObjectToMaskGroup(GameObject maskObject, MaskGroup maskGroup)
        {
            if (maskObject == null)
                return;

            _instance._playbookMaskPass.AddObjectToMaskGroup(maskObject, maskGroup);
        }

        /// <summary>
        /// Add the given objects to the given mask group.
        /// </summary>
        public static void AddObjectsToMaskGroup(List<GameObject> maskObjects, MaskGroup maskGroup)
        {
            if (maskObjects == null)
                return;

            _instance._playbookMaskPass.AddObjectsToMaskGroup(maskObjects, maskGroup);
        }

        /// <summary>
        /// Remove the given object from its current mask group and place it in the
        /// catch-all group.
        /// </summary>
        public static void RemoveObjectFromMaskGroup(GameObject maskObject)
        {
            if (maskObject == null)
                return;

            _instance._playbookMaskPass.AddObjectToMaskGroup(maskObject, MaskGroup.CatchAll);
        }

        /// <summary>
        /// Remove the given objects from their current mask groups and place them in
        /// the catch-all group.
        /// </summary>
        public static void RemoveObjectsFromMaskGroup(List<GameObject> maskObjects)
        {
            if (maskObjects == null)
                return;

            _instance._playbookMaskPass.AddObjectsToMaskGroup(maskObjects, MaskGroup.CatchAll);
        }

        #endregion
    }
}
