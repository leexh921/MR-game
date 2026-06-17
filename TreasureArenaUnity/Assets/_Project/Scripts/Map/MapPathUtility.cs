using System.IO;
using UnityEngine;

namespace TreasureArenaMR.Map
{
    /// <summary>
    /// Single source of truth for project map JSON locations.
    /// Source maps live under Assets/_Project/StreamingAssets/Maps.
    /// Player builds read the mirrored package path _Project/StreamingAssets/Maps.
    /// </summary>
    public static class MapPathUtility
    {
        public const string ProjectMapsAssetFolder = "Assets/_Project/StreamingAssets/Maps";
        public const string PackagedMapsRelativeFolder = "_Project/StreamingAssets/Maps";

        public static string EditorMapsDirectory =>
            Path.Combine(Application.dataPath, "_Project", "StreamingAssets", "Maps");

        public static string PackagedMapsDirectory =>
            CombineUnityPath(Application.streamingAssetsPath, PackagedMapsRelativeFolder);

        public static string GetRuntimeMapPath(string fileName)
        {
#if UNITY_EDITOR
            return GetEditorMapPath(fileName);
#else
            return GetPackagedMapPath(fileName);
#endif
        }

        public static string GetEditorMapPath(string fileName)
        {
            return Path.Combine(EditorMapsDirectory, fileName);
        }

        public static string GetPackagedMapPath(string fileName)
        {
            return CombineUnityPath(PackagedMapsDirectory, fileName);
        }

        public static string CombineUnityPath(string root, string relativePath)
        {
            string normalizedRoot = (root ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            string normalizedRelative = (relativePath ?? string.Empty).Replace('\\', '/').TrimStart('/');
            if (string.IsNullOrEmpty(normalizedRoot))
                return normalizedRelative;
            if (string.IsNullOrEmpty(normalizedRelative))
                return normalizedRoot;

            return normalizedRoot + "/" + normalizedRelative;
        }
    }
}
