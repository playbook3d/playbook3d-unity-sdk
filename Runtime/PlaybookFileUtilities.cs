using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace PlaybookUnitySDK.Runtime
{
    public static class PlaybookFileUtilities
    {
        /// <summary>
        /// Get the folder directory of this package ending with "/Renders".
        /// </summary>
        public static string GetRendersFolderPath()
        {
            string renderFoldersPath = Application.persistentDataPath + "/Renders";

            if (!Directory.Exists(renderFoldersPath))
            {
                Directory.CreateDirectory(renderFoldersPath);
            }

            return renderFoldersPath;
        }

        /// <summary>
        /// Zip up the contents of the given folder path.
        /// </summary>
        public static void ZipFolderContents(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                PlaybookLogger.LogError($"Folder {folderPath} does not exist.");
                return;
            }

            string folderZipPath = $"{folderPath}.zip";

            DeleteFile(folderZipPath);

            ZipFile.CreateFromDirectory(folderPath, folderZipPath, CompressionLevel.Fastest, true);
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

        public static void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
