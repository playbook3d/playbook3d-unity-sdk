using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace PlaybookUnitySDK.Scripts
{
    [RequireComponent(typeof(PlaybookMaskGroups))]
    public class PlaybookSDK : MonoBehaviour
    {
        public bool IsCapturingImageSequence { get; private set; }
    
        private bool _renderImage;
        private int _framesPassed;
        private int _sequenceCount;

        private const int RenderEveryNFrames = 2;
    
        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += CaptureImage;
            RenderPipelineManager.endCameraRendering += CaptureImageSequence;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= CaptureImage;
        }

        private void CaptureImage(ScriptableRenderContext context, Camera cam)
        {
            if (!_renderImage) return;

            _renderImage = false;

            SavePNGToRenders(GetRenderImageFilePath());
        }
        
        private void CaptureImageSequence(ScriptableRenderContext context, Camera cam)
        {
            if (!IsCapturingImageSequence) return;

            if (_framesPassed < RenderEveryNFrames)
            {
                _framesPassed++;
                return;
            }
            
            SavePNGToRenders(GetRenderImageFilePath(_sequenceCount));
            
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
        
        private string GetRenderImageFilePath(int fileNum = -1)
        {
            MonoScript script = MonoScript.FromMonoBehaviour(this);
            string scriptPath = AssetDatabase.GetAssetPath(script);

            string scriptDirectory = Path.GetDirectoryName(scriptPath);
        
            Assert.IsNotNull(scriptDirectory);

            string rendersFolderPath = Path.Combine(scriptDirectory, "../Renders");

            if (!Directory.Exists(rendersFolderPath))
            {
                Directory.CreateDirectory(rendersFolderPath);
            }

            string fileName = fileNum == -1 ? "Image.png" : $"ImageSequence{fileNum}.png";
            return Path.Combine(rendersFolderPath, fileName);
        }

        public void CaptureImage()
        {
            _renderImage = true;
        }

        public void StartCaptureImageSequence()
        {
            IsCapturingImageSequence = true;

            // Reset properties for image sequence capture
            _framesPassed = 0;
            _sequenceCount = 0;
        }

        public void StopCaptureImageSequence()
        {
            IsCapturingImageSequence = false;
        }
    }
}
