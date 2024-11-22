using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace PlaybookUnitySDK.Scripts
{
    [RequireComponent(typeof(PlaybookMaskGroups))]
    public class PlaybookSDK : MonoBehaviour
    {
        // Capture Image Sequence Properties
        public bool IsCapturingImageSequence { get; private set; }
        
        private int _framesPassed;
        private int _sequenceCount;
    
        private const int RenderEveryNFrames = 3;
        
        // Capture Image Properties
        private bool _renderImage;

        #region Lifecycle Events
        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += CaptureImage;
            RenderPipelineManager.endCameraRendering += CaptureImageSequence;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= CaptureImage;
        }
        #endregion

        #region Private Methods
        private void CaptureImage(ScriptableRenderContext context, Camera cam)
        {
            if (!_renderImage) return;

            _renderImage = false;

            string rendersFolderPath = GetRendersFolderPath();
            SavePNGToRenders(GetRenderImageFilePath(rendersFolderPath));
        }
        
        private void CaptureImageSequence(ScriptableRenderContext context, Camera cam)
        {
            if (!IsCapturingImageSequence) return;

            if (_framesPassed < RenderEveryNFrames)
            {
                _framesPassed++;
                return;
            }

            string rendersFolderPath = GetRendersFolderPath();
            SavePNGToRenders(GetRenderImageFilePath(rendersFolderPath, _sequenceCount));
            
            _framesPassed = 0;
            _sequenceCount++;
        }

        private void SavePNGToRenders(string filePath)
        {
            int width = Screen.width;
            int height = Screen.height;

            Texture2D renderImageTexture = new(width, height, TextureFormat.ARGB32, false);
            Rect renderImageRect = new(0, 0, width, height);
            renderImageTexture.ReadPixels(renderImageRect, 0, 0);
            renderImageTexture.Apply();

            byte[] byteArray = renderImageTexture.EncodeToPNG();
            File.WriteAllBytes(filePath, byteArray);
        }
        
        private string GetRenderImageFilePath(string rendersFolderPath, int fileNum = -1)
        {
            if (!Directory.Exists(rendersFolderPath))
            {
                Directory.CreateDirectory(rendersFolderPath);
            }

            string fileName = fileNum == -1 ? "Image.png" : $"ImageSequence{fileNum}.png";
            return Path.Combine(rendersFolderPath, fileName);
        }

        private string GetRendersFolderPath()
        {
            MonoScript script = MonoScript.FromMonoBehaviour(this);
            string scriptPath = AssetDatabase.GetAssetPath(script);

            string scriptDirectory = Path.GetDirectoryName(scriptPath);
        
            Assert.IsNotNull(scriptDirectory);

            return Path.Combine(scriptDirectory, "../Renders");
        }

        private void ZipRendersFolder(string rendersFolderPath)
        {
            string rendersFolderZipPath = $"{rendersFolderPath}.zip";

            if (Directory.Exists(rendersFolderPath))
            {
                ZipFile.CreateFromDirectory
                (
                    rendersFolderPath, 
                    rendersFolderZipPath, 
                    CompressionLevel.Fastest, 
                    true
                );
            }
            else
            {
                Debug.LogError($"Folder {rendersFolderPath} does not exist.");
            }
        }

        private void DeleteFolderContents(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }

            Directory.CreateDirectory(folderPath);
        }
        #endregion

        #region Public Methods
        public void CaptureImage()
        {
            _renderImage = true;
        }

        public void StartCaptureImageSequence()
        {
            IsCapturingImageSequence = true;

            // Reset properties for image sequence capture
            _framesPassed = RenderEveryNFrames;
            _sequenceCount = 0;
        }

        public void StopCaptureImageSequence()
        {
            IsCapturingImageSequence = false;

            // Create a zip of the image sequences
            string rendersFolderPath = GetRendersFolderPath();
            ZipRendersFolder(rendersFolderPath);
            DeleteFolderContents(rendersFolderPath);
            
            // TODO: Send zip to server
            DeleteFolderContents($"{rendersFolderPath}.zip");
        }
        #endregion
    }
}
