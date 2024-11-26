using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace PlaybookUnitySDK.Scripts
{
    public static class PlaybookFileUtilities
    {
        /// <summary>
        /// Get the folder directory of this package ending with "/Renders".
        /// </summary>
        public static string GetRendersFolderPath(MonoBehaviour monoBehaviour)
        {
            MonoScript script = MonoScript.FromMonoBehaviour(monoBehaviour);
            string scriptPath = AssetDatabase.GetAssetPath(script);
            string scriptDirectory = Path.GetDirectoryName(scriptPath);

            if (scriptDirectory == null)
            {
                Debug.LogError($"Could not find directory {scriptPath}");
                return "";
            }

            return Path.Combine(scriptDirectory, "../Renders");
        }

        /// <summary>
        /// Zip up the contents of the given folder path.
        /// </summary>
        public static void ZipFolderContents(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                string rendersFolderZipPath = $"{folderPath}.zip";
                ZipFile.CreateFromDirectory(
                    folderPath,
                    rendersFolderZipPath,
                    CompressionLevel.Fastest,
                    true
                );
            }
            else
            {
                Debug.LogError($"Folder {folderPath} does not exist.");
            }
        }

        /// <summary>
        /// Delete the contents of the given folder path.
        /// </summary>
        public static void DeleteFolderContents(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
                Directory.CreateDirectory(folderPath);
            }
        }
    }
}
