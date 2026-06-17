using System;
using System.IO;
using TreasureArenaMR.Map;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TreasureArenaMR.Map.Editor
{
    public sealed class ProjectMapStreamingAssetsBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string LogPrefix = "[MapBuildPaths]";
        private const string GeneratedMarkerFileName = ".treasure_arena_generated_maps";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            MirrorProjectMapsIntoUnityStreamingAssets();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            CleanupGeneratedMirror();
        }

        [MenuItem("Tools/TreasureArena/Sync Project Maps For Player Build")]
        public static void MirrorProjectMapsIntoUnityStreamingAssets()
        {
            string sourceDirectory = GetProjectMapsSourceDirectory();
            string targetDirectory = GetBuildMirrorDirectory();

            if (!Directory.Exists(sourceDirectory))
            {
                Debug.LogError($"{LogPrefix} Project map source directory missing: {sourceDirectory}");
                return;
            }

            Directory.CreateDirectory(targetDirectory);
            File.WriteAllText(Path.Combine(targetDirectory, GeneratedMarkerFileName), DateTime.UtcNow.ToString("O"));

            DeleteJsonFiles(targetDirectory);

            string[] sourceFiles = Directory.GetFiles(sourceDirectory, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(sourceFiles, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < sourceFiles.Length; i++)
            {
                string fileName = Path.GetFileName(sourceFiles[i]);
                string targetPath = Path.Combine(targetDirectory, fileName);
                File.Copy(sourceFiles[i], targetPath, true);
            }

            AssetDatabase.Refresh();
            Debug.Log($"{LogPrefix} Project maps synced for build. source={sourceDirectory} target={targetDirectory} count={sourceFiles.Length}");
        }

        [MenuItem("Tools/TreasureArena/Cleanup Generated Build Map Mirror")]
        public static void CleanupGeneratedMirror()
        {
            string targetDirectory = GetBuildMirrorDirectory();
            string markerPath = Path.Combine(targetDirectory, GeneratedMarkerFileName);
            if (!File.Exists(markerPath))
                return;

            DeleteDirectoryAndMeta(targetDirectory);
            DeleteEmptyParents(GetBuildMirrorRootDirectory());
            AssetDatabase.Refresh();
            Debug.Log($"{LogPrefix} Generated build map mirror cleaned: {targetDirectory}");
        }

        private static string GetProjectMapsSourceDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "_Project", "StreamingAssets", "Maps"));
        }

        private static string GetBuildMirrorRootDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "StreamingAssets", "_Project"));
        }

        private static string GetBuildMirrorDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "StreamingAssets", MapPathUtility.PackagedMapsRelativeFolder));
        }

        private static void DeleteJsonFiles(string directory)
        {
            string[] existingFiles = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < existingFiles.Length; i++)
            {
                File.Delete(existingFiles[i]);
                string metaPath = existingFiles[i] + ".meta";
                if (File.Exists(metaPath))
                    File.Delete(metaPath);
            }
        }

        private static void DeleteEmptyParents(string rootDirectory)
        {
            string mapsDirectory = Path.Combine(rootDirectory, "StreamingAssets", "Maps");
            DeleteIfEmpty(mapsDirectory);
            DeleteIfEmpty(Path.GetDirectoryName(mapsDirectory));
            DeleteIfEmpty(rootDirectory);
        }

        private static void DeleteDirectoryAndMeta(string directory)
        {
            if (!Directory.Exists(directory))
                return;

            Directory.Delete(directory, true);
            DeleteMetaFile(directory);
        }

        private static void DeleteIfEmpty(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return;

            if (Directory.GetFileSystemEntries(directory).Length == 0)
            {
                Directory.Delete(directory);
                DeleteMetaFile(directory);
            }
        }

        private static void DeleteMetaFile(string assetPath)
        {
            string metaPath = assetPath + ".meta";
            if (File.Exists(metaPath))
                File.Delete(metaPath);
        }
    }
}
