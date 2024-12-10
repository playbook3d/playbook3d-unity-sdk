using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlaybookUnitySDK.Scripts
{
    [RequireComponent(typeof(PlaybookMaskPass))]
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

        // Image sequence properties
        private const int RenderEveryNFrames = 3;
        private int _framesPassed;
        private int _sequenceCount;
        private Coroutine _imageSequenceCoroutine;

        private string _rendersFolderPath;

        private Camera _renderCamera;
        private PlaybookMaskPass _maskPass;
        private RenderTexture _beautyPassRenderTexture;
        private RenderTexture _maskPassRenderTexture;
        private RenderPassProperty[] _renderPassProperties;

        public bool IsCapturingImageSequence { get; private set; }

        #region Lifecycle Events

        private void Awake()
        {
            InitializeProperties();
        }

        private void Update()
        {
            // TODO: Convert to coroutine
            if (IsCapturingImageSequence)
            {
                if (_framesPassed < RenderEveryNFrames)
                {
                    _framesPassed++;
                    return;
                }

                CaptureRenderPasses();

                _framesPassed = 0;
                _sequenceCount++;
            }
        }

        private void OnDestroy()
        {
            PlaybookFileUtilities.DeleteFolderContents(_rendersFolderPath);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize the required properties for capturing images and image sequences.
        /// </summary>
        private void InitializeProperties()
        {
            _renderCamera = GetComponent<Camera>();
            _maskPass = GetComponent<PlaybookMaskPass>();

            Material depthMaterial = new(Shader.Find(DepthShader));
            Material outlineMaterial = new(Shader.Find(OutlineShader));

            _renderPassProperties = new[]
            {
                new RenderPassProperty
                {
                    pass = RenderPass.Depth,
                    texture = new RenderTexture(Screen.width, Screen.height, 32),
                    material = depthMaterial,
                },
                new RenderPassProperty
                {
                    pass = RenderPass.Outline,
                    texture = new RenderTexture(Screen.width, Screen.height, 32),
                    material = outlineMaterial,
                },
            };

            _beautyPassRenderTexture = new RenderTexture(Screen.width, Screen.height, 32);
            _maskPassRenderTexture = new RenderTexture(Screen.width, Screen.height, 32);

            _rendersFolderPath = PlaybookFileUtilities.GetRendersFolderPath(this);
        }

        /// <summary>
        /// Capture all image passes every n frames.
        /// </summary>
        private IEnumerator CaptureImageSequence_CO()
        {
            while (true)
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
        }

        /// <summary>
        /// Capture all render passes.
        /// </summary>
        private void CaptureRenderPasses()
        {
            CaptureBeautyPass();
            CaptureMaskPass();

            foreach (RenderPassProperty renderPassProperty in _renderPassProperties)
            {
                CaptureFullscreenPass(renderPassProperty);
            }
        }

        /// <summary>
        /// Capture the beauty pass. This is the unaltered render.
        /// </summary>
        private void CaptureBeautyPass()
        {
            _renderCamera.targetTexture = _beautyPassRenderTexture;
            _renderCamera.Render();

            SaveImageCapture(_beautyPassRenderTexture, RenderPass.Beauty);
        }

        /// <summary>
        /// Capture the mask pass. This renders gives objects a flat color depending on what
        /// mask group they're in. If a mask group is not specified, objects will be at default
        /// placed on the catch-all layer.
        /// </summary>
        private void CaptureMaskPass()
        {
            _maskPass.SaveProperties();
            _maskPass.SetProperties();

            _renderCamera.targetTexture = _maskPassRenderTexture;
            _renderCamera.Render();

            SaveImageCapture(_maskPassRenderTexture, RenderPass.Mask);

            _maskPass.ResetProperties();
        }

        /// <summary>
        /// Capture a pass that uses a fullscreen shader as a postprocessing render.
        /// </summary>
        private void CaptureFullscreenPass(RenderPassProperty passProperty)
        {
            _renderCamera.targetTexture = passProperty.texture;

            // Clear the previous render
            GL.Clear(true, true, Color.black);

            CommandBuffer command = new() { name = "CaptureShaderEffect" };
            command.Blit(null, passProperty.texture, passProperty.material);

            // Apply the material during rendering
            _renderCamera.AddCommandBuffer(CameraEvent.AfterEverything, command);
            _renderCamera.Render();

            SaveImageCapture(passProperty.texture, passProperty.pass);

            _renderCamera.RemoveCommandBuffer(CameraEvent.AfterEverything, command);
            _renderCamera.targetTexture = null;
        }

        /// <summary>
        /// Save the image capture to the renders folder path after appropriately
        /// naming it.
        /// </summary>
        private void SaveImageCapture(RenderTexture renderTexture, RenderPass pass)
        {
            AsyncGPUReadback.Request(
                renderTexture,
                0,
                TextureFormat.RGBA32,
                request => OnGPUReadbackComplete(request, renderTexture, pass)
            );
        }

        /// <summary>
        /// Saves the captured image to the appropriate file.
        /// </summary>
        private void OnGPUReadbackComplete(
            AsyncGPUReadbackRequest request,
            RenderTexture renderTexture,
            RenderPass pass
        )
        {
            if (request.hasError)
            {
                Debug.LogError("GPU readback error.");
                return;
            }

            string imageName = IsCapturingImageSequence
                ? $"{pass.ToString()}Pass_{_sequenceCount}.png"
                : $"{pass.ToString()}Pass.png";
            string filePath = Path.Combine(_rendersFolderPath, imageName);

            Texture2D imageTexture =
                new(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            imageTexture.Apply();

            // TODO: Find faster alternative
            var v = imageTexture.EncodeToPNG();

            File.WriteAllBytesAsync(filePath, imageTexture.EncodeToPNG());

            Destroy(imageTexture);
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
            IsCapturingImageSequence = true;

            PlaybookFileUtilities.DeleteFolderContents(_rendersFolderPath);

            ResetImageSequenceProperties();

            // _imageSequenceCoroutine = StartCoroutine(CaptureImageSequence_CO());
        }

        public void StopCaptureImageSequence()
        {
            IsCapturingImageSequence = false;

            // Create a zip of the image sequences
            PlaybookFileUtilities.ZipFolderContents(_rendersFolderPath);
            PlaybookFileUtilities.DeleteFolderContents(_rendersFolderPath);

            // TODO: Send zip to server then delete
            // DeleteFolderContents($"{rendersFolderPath}.zip");

            // if (_imageSequenceCoroutine != null)
            // {
            //     StopCoroutine(_imageSequenceCoroutine);
            // }
        }

        #endregion

        private struct RenderPassProperty
        {
            public RenderPass pass;
            public RenderTexture texture;
            public Material material;
        }
    }
}
