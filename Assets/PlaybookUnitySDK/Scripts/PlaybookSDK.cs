using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlaybookUnitySDK.Scripts
{
    [RequireComponent(typeof(PlaybookMaskGroups))]
    public class PlaybookSDK : MonoBehaviour
    {
        public enum RenderPass
        {
            Beauty,
            Mask,
            Depth,
            Outline,
        }

        private const string DepthShader = "Shader Graphs/DepthPassShaderGraph";
        private const string OutlineShader = "Shader Graphs/OutlinePassShaderGraph";

        private const int NumberOfPasses = 2;
        private const int RenderEveryNFrames = 3;

        private int _framesPassed;
        private int _sequenceCount;

        private string _rendersFolderPath;

        private Camera _renderCamera;
        private RenderTexture[] _renderTextures;
        private RenderTexture _beautyPassRenderTexture;
        private RenderPassMaterial[] _renderPassMaterials;

        private Coroutine _imageSequenceCoroutine;

        public bool IsCapturingImageSequence { get; private set; }

        #region Lifecycle Events

        private void Awake()
        {
            InitializeProperties();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize the required properties for capturing images and image sequences.
        /// </summary>
        private void InitializeProperties()
        {
            _renderCamera = GetComponent<Camera>();

            Material depthMaterial = new(Shader.Find(DepthShader));
            Material outlineMaterial = new(Shader.Find(OutlineShader));

            _renderPassMaterials = new[]
            {
                new RenderPassMaterial { pass = RenderPass.Depth, material = depthMaterial },
                new RenderPassMaterial { pass = RenderPass.Outline, material = outlineMaterial },
            };

            _beautyPassRenderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            _renderTextures = new RenderTexture[NumberOfPasses];
            for (int i = 0; i < NumberOfPasses; i++)
            {
                _renderTextures[i] = new RenderTexture(Screen.width, Screen.height, 24);
            }

            _rendersFolderPath = PlaybookFileUtilities.GetRendersFolderPath(this);
        }

        /// <summary>
        /// Capture all image passes every n frames.
        /// </summary>
        private IEnumerator CaptureImageSequence_CO()
        {
            if (_framesPassed < RenderEveryNFrames)
            {
                _framesPassed++;
                yield return null;
            }

            CaptureRenderPasses();

            _framesPassed = 0;
            _sequenceCount++;
        }

        /// <summary>
        /// Capture all render passes.
        /// </summary>
        private void CaptureRenderPasses()
        {
            CaptureBeautyPass();

            for (int i = 0; i < NumberOfPasses; i++)
            {
                _renderCamera.targetTexture = _renderTextures[i];

                // Clear the previous render
                GL.Clear(true, true, Color.black);

                CommandBuffer command = new() { name = "CaptureShaderEffect" };
                command.Blit(null, _renderTextures[i], _renderPassMaterials[i].material);

                // Apply the material during rendering
                _renderCamera.AddCommandBuffer(CameraEvent.AfterEverything, command);
                _renderCamera.Render();

                SaveImageCapture(_renderTextures[i], _renderPassMaterials[i].pass);

                _renderCamera.RemoveCommandBuffer(CameraEvent.AfterEverything, command);
            }

            _renderCamera.targetTexture = null;
        }

        private void CaptureBeautyPass()
        {
            GL.Clear(true, true, Color.black);

            _renderCamera.targetTexture = _beautyPassRenderTexture;
            _renderCamera.Render();

            SaveImageCapture(_beautyPassRenderTexture, RenderPass.Beauty);
        }

        /// <summary>
        /// Save the image capture to the renders folder path after appropriately
        /// naming it.
        /// </summary>
        private async void SaveImageCapture(RenderTexture renderTexture, RenderPass pass)
        {
            RenderTexture.active = renderTexture;
            Texture2D screenshot =
                new(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            screenshot.Apply();

            byte[] bytes = screenshot.EncodeToPNG();
            string imageName = IsCapturingImageSequence
                ? $"{pass.ToString()}Pass_{_sequenceCount}.png"
                : $"{pass.ToString()}Pass.png";
            string filePath = Path.Combine(_rendersFolderPath, imageName);
            await File.WriteAllBytesAsync(filePath, bytes);

            await PlaybookAPIVerifier.UploadImageFile(pass, filePath);

            RenderTexture.active = null;
            Destroy(screenshot);
        }

        private void ResetImageSequenceProperties()
        {
            _framesPassed = RenderEveryNFrames;
            _sequenceCount = 0;
        }

        #endregion

        #region Public Methods

        public void InvokeCaptureImage()
        {
            PlaybookFileUtilities.DeleteFolderContents(_rendersFolderPath);

            CaptureRenderPasses();
            // TODO: Send images to server
        }

        public void StartCaptureImageSequence()
        {
            PlaybookFileUtilities.DeleteFolderContents(_rendersFolderPath);

            IsCapturingImageSequence = true;

            ResetImageSequenceProperties();

            _imageSequenceCoroutine = StartCoroutine(CaptureImageSequence_CO());
        }

        public void StopCaptureImageSequence()
        {
            IsCapturingImageSequence = false;

            // Create a zip of the image sequences
            PlaybookFileUtilities.ZipFolderContents(_rendersFolderPath);
            PlaybookFileUtilities.DeleteFolderContents(_rendersFolderPath);

            // TODO: Send zip to server then delete
            // DeleteFolderContents($"{rendersFolderPath}.zip");

            if (_imageSequenceCoroutine != null)
            {
                StopCoroutine(_imageSequenceCoroutine);
            }
        }

        #endregion

        private struct RenderPassMaterial
        {
            public RenderPass pass;
            public Material material;
        }
    }
}
