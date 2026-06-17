using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TreasureArenaMR.Map
{
    /// <summary>
    /// Shared runtime map list used to encode selected map ids into compact network state.
    /// Server and Pico builds must include the same map json files for indices to match.
    /// </summary>
    public static class RuntimeMapCatalog
    {
        private static readonly string[] FallbackMapIds =
        {
            "map_editor",
            "map_template_gameplay",
            "new_map",
            "pico_map"
        };

        private static string[] _cachedMapIds;

        public static IReadOnlyList<string> MapIds => GetMapIds();

        public static int GetIndex(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
                return 0;

            string[] mapIds = GetMapIds();
            for (int i = 0; i < mapIds.Length; i++)
            {
                if (mapIds[i] == mapId)
                    return i;
            }

            return 0;
        }

        public static string GetMapId(int mapIndex)
        {
            string[] mapIds = GetMapIds();
            if (mapIndex < 0 || mapIndex >= mapIds.Length)
                return null;

            return mapIds[mapIndex];
        }

        public static string[] GetMapIds()
        {
            if (_cachedMapIds != null && _cachedMapIds.Length > 0)
                return _cachedMapIds;

            List<string> mapIds = new List<string>();

            // Primary: baked catalog in Resources (works on all platforms including Android)
            TextAsset catalogAsset = Resources.Load<TextAsset>("map_catalog");
            if (catalogAsset != null && !string.IsNullOrEmpty(catalogAsset.text))
            {
                string[] lines = catalogAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    string id = lines[i].Trim();
                    if (!string.IsNullOrEmpty(id) && !mapIds.Contains(id))
                        mapIds.Add(id);
                }
            }

            // Secondary: scan StreamingAssets directories (Editor / PC standalone)
            if (mapIds.Count == 0)
            {
                AddMapIds(Path.Combine(Application.dataPath, "_Project/StreamingAssets/Maps"), mapIds);
                AddMapIds(Path.Combine(Application.streamingAssetsPath, "Maps"), mapIds);
            }

            _cachedMapIds = mapIds.Count > 0 ? mapIds.ToArray() : FallbackMapIds;
            return _cachedMapIds;
        }

        public static void Refresh()
        {
            _cachedMapIds = null;
        }

        private static void AddMapIds(string mapDirectory, List<string> mapIds)
        {
            if (!Directory.Exists(mapDirectory))
                return;

            string[] files = Directory.GetFiles(mapDirectory, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < files.Length; i++)
            {
                string id = Path.GetFileNameWithoutExtension(files[i]);
                if (!string.IsNullOrEmpty(id) && !mapIds.Contains(id))
                    mapIds.Add(id);
            }
        }
    }
}
