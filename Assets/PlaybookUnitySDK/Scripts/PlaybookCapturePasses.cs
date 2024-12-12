using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlaybookUnitySDK.Scripts
{
    [RequireComponent(typeof(PlaybookMaskPass))]
    public class PlaybookCapturePasses : MonoBehaviour
    {
        public enum RenderPass
        {
            Beauty,
            Mask,
            Depth,
            Outline,
        }

        public bool IsCapturingImageSequence { get; private set; }

        public event Action ImageCaptureComplete;
        public event Action ImageSequenceCaptureComplete;

        private const string DepthShader = "Shader Graphs/DepthPassShaderGraph";
        private const string OutlineShader = "Shader Graphs/OutlinePassShaderGraph";

        // Image sequence properties
        private int _sequenceCount;
        private Coroutine _imageSequenceCoroutine;

        private string _rendersFolderPath;

        private Camera _renderCamera;
        private PlaybookMaskPass _maskPass;
        private RenderTexture _beautyPassRenderTexture;
        private RenderTexture _maskPassRenderTexture;
        private RenderPassProperty _depthPassProperties;
        private RenderPassProperty _outlinePassProperties;

        #region Lifecycle Events

        private void Awake()
        {
            InitializeProperties();
        }

        private void OnDestroy()
        {
            PlaybookFileUtilities.DeleteFolderContents(_rendersFolderPath);
            PlaybookFileUtilities.DeleteFile($"{_rendersFolderPath}.zip");
        }

        #endregion

        /// <summary>
        /// Initialize the required properties for capturing images and image sequences.
        /// </summary>
        private void InitializeProperties()
        {
            _renderCamera = GetComponent<Camera>();
            _maskPass = GetComponent<PlaybookMaskPass>();

            InitializeRenderPasses();

            _rendersFolderPath = PlaybookFileUtilities.GetRendersFolderPath(this);
        }

        private void InitializeRenderPasses()
        {
            Material depthMaterial = new(Shader.Find(DepthShader));
            Material outlineMaterial = new(Shader.Find(OutlineShader));

            _depthPassProperties = new RenderPassProperty
            {
                pass = RenderPass.Depth,
                texture = new RenderTexture(Screen.width, Screen.height, 32),
                material = depthMaterial,
            };
            _outlinePassProperties = new RenderPassProperty
            {
                pass = RenderPass.Outline,
                texture = new RenderTexture(Screen.width, Screen.height, 32),
                material = outlineMaterial,
            };

            _beautyPassRenderTexture = new RenderTexture(Screen.width, Screen.height, 32);
            _maskPassRenderTexture = new RenderTexture(Screen.width, Screen.height, 32);
        }

        /// <summary>
        /// Capture all render passes.
        /// </summary>
        public void CaptureRenderPasses()
        {
            _sequenceCount++;

            // Beauty pass
            CaptureImage(_beautyPassRenderTexture, RenderPass.Beauty);

            // Mask pass
            CaptureMaskPass();

            // Depth pass
            CaptureFullscreenPass(_depthPassProperties);

            // Outline pass
            CaptureFullscreenPass(_outlinePassProperties);

            _renderCamera.targetTexture = null;
        }

        /// <summary>
        /// Render an image through the camera attached to this component.
        /// </summary>
        private void CaptureImage(RenderTexture renderTexture, RenderPass renderPass)
        {
            _renderCamera.targetTexture = renderTexture;
            _renderCamera.Render();

            SaveImageCaptureAsFile(renderTexture, renderPass);
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

            CaptureImage(_maskPassRenderTexture, RenderPass.Mask);

            // _maskPass.ResetProperties();
        }

        /// <summary>
        /// Capture a pass that uses a fullscreen shader as a postprocessing render.
        /// </summary>
        private void CaptureFullscreenPass(RenderPassProperty passProperty)
        {
            // Clear the previous render
            GL.Clear(true, true, Color.black);

            CommandBuffer command = new() { name = "CaptureShaderEffect" };
            command.Blit(null, passProperty.texture, passProperty.material);

            _renderCamera.targetTexture = passProperty.texture;

            // Apply the material during rendering
            _renderCamera.AddCommandBuffer(CameraEvent.AfterEverything, command);
            _renderCamera.Render();

            SaveImageCaptureAsFile(passProperty.texture, passProperty.pass);

            _renderCamera.RemoveCommandBuffer(CameraEvent.AfterEverything, command);
        }

        /// <summary>
        /// Save the image capture to the renders folder path after appropriately
        /// naming it.
        /// </summary>
        private async void SaveImageCaptureAsFile(RenderTexture renderTexture, RenderPass pass)
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

            RenderTexture.active = null;
            Destroy(screenshot);
        }

        /// <summary>
        /// Separate the images created by pass and zip them into invidual folders.
        /// </summary>
        private void SeparateFolderContentsByCapture(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Debug.LogError($"Folder {folderPath} does not exist.");
                return;
            }

            foreach (RenderPass pass in Enum.GetValues(typeof(RenderPass)).Cast<RenderPass>())
            {
                string targetFolder = Path.Combine(folderPath, $"{pass.ToString()}Pass");

                // Ensure there is a folder for each render pass type
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }
                else
                {
                    PlaybookFileUtilities.DeleteFolderContents(targetFolder);
                }

                // Move each render pass image to its respective folder
                string[] matchingImages = Directory.GetFiles(folderPath, $"*{pass}*");
                foreach (string image in matchingImages)
                {
                    string fileName = Path.GetFileName(image);
                    string destinationPath = Path.Combine(targetFolder, fileName);

                    File.Move(image, destinationPath);
                }

                PlaybookFileUtilities.ZipFolderContents(targetFolder);
            }
        }

        public void InvokeCaptureImage()
        {
            // Ensure previous renders are cleared
            PlaybookFileUtilities.DeleteFolderContents(_rendersFolderPath);

            CaptureRenderPasses();

            ImageCaptureComplete?.Invoke();
            // TODO: Send images to server
        }

        public void StartCaptureImageSequence()
        {
            IsCapturingImageSequence = true;

            _sequenceCount = 0;

            // Ensure previous renders are cleared
            PlaybookFileUtilities.DeleteFolderContents(_rendersFolderPath);

            // _imageSequenceCoroutine = StartCoroutine(CaptureImageSequence_CO());
        }

        public void StopCaptureImageSequence()
        {
            IsCapturingImageSequence = false;

            // Create zips of the image sequences
            SeparateFolderContentsByCapture(_rendersFolderPath);

            // TODO: Send zip to server

            // if (_imageSequenceCoroutine != null)
            // {
            //     StopCoroutine(_imageSequenceCoroutine);
            // }

            ImageSequenceCaptureComplete?.Invoke();
        }

        private struct RenderPassProperty
        {
            public RenderPass pass;
            public RenderTexture texture;
            public Material material;
        }
    }
}
