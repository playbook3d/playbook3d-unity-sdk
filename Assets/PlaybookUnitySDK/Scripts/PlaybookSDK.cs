using System;
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
        [Tooltip("The number of renders to capture per second for image sequences.")]
        private int framesPerSecond = 24;

        public bool IsCapturingImageSequence => _playbookCapturePasses.IsCapturingImageSequence;

        private PlaybookCapturePasses _playbookCapturePasses;
        private PlaybookNetwork _playbookNetwork;

        private float _interval;
        private float _timePassed;

        private void OnValidate()
        {
            if (_playbookNetwork == null)
            {
                _playbookNetwork = GetComponent<PlaybookNetwork>();
            }

            if (_playbookCapturePasses == null)
            {
                _playbookCapturePasses = GetComponent<PlaybookCapturePasses>();
            }
        }

        private void Awake()
        {
            _interval = 1f / framesPerSecond;

            _playbookNetwork.PlaybookAccountAPIKey = playbookAccountAPIKey;

            _playbookCapturePasses.ImageCaptureComplete += OnImageCaptureComplete;
            _playbookCapturePasses.ImageSequenceCaptureComplete += OnImageSequenceCaptureComplete;
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
    }
}
