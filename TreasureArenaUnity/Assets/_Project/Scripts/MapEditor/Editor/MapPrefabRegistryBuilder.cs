using System.Collections.Generic;
using TreasureArenaMR.Map;
using UnityEditor;
using UnityEngine;

namespace TreasureArenaMR.MapEditor.Editor
{
    public static class MapPrefabRegistryBuilder
    {
        private const string UserPrefabsDir = "Assets/_Project/Prefabs/MapEditor/Palette/Prefabs";
        private static readonly string[] ScanDirectories = {
            "Assets/_Project/Prefabs/MapEditor/Palette/Prefabs",
            "Assets/_Project/Prefabs/Map",
            "Assets/_Project/Prefabs/Gun"
        };
        private const string RegistryFolder = "Assets/_Project/ScriptableObjects/Map";
        private const string RegistryPath = RegistryFolder + "/MapPrefabRegistry.asset";

        [MenuItem("Tools/TreasureArena/地图编辑器/重建 Map Prefab Registry")]
        public static void RebuildRegistry()
        {
            EnsureFolder("Assets/_Project", "ScriptableObjects");
            EnsureFolder("Assets/_Project/ScriptableObjects", "Map");
            EnsureFolder("Assets/_Project/Prefabs/MapEditor/Palette", "Prefabs");

            PrefabRegistry registry = AssetDatabase.LoadAssetAtPath<PrefabRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<PrefabRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", ScanDirectories);
            string[] prefabPaths = new string[prefabGuids.Length];
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                prefabPaths[i] = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            }

            System.Array.Sort(prefabPaths, System.StringComparer.Ordinal);
            List<PrefabRegistry.Entry> entries = new List<PrefabRegistry.Entry>();
            HashSet<string> usedIds = new HashSet<string>(System.StringComparer.Ordinal);

            for (int i = 0; i < prefabPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
                if (prefab == null)
                {
                    continue;
                }

                string prefabId = GetPrefabId(prefab);
                if (string.IsNullOrEmpty(prefabId))
                {
                    Debug.LogWarning("Map prefab skipped because prefab_id is empty: " + prefabPaths[i]);
                    continue;
                }

                if (!usedIds.Add(prefabId))
                {
                    Debug.LogWarning("Duplicate map prefab_id skipped: " + prefabId + " at " + prefabPaths[i]);
                    continue;
                }

                ValidatePrefab(prefab, prefabId, prefabPaths[i]);
                entries.Add(new PrefabRegistry.Entry(prefabId, prefab));
            }

            registry.SetEntries(entries);
            EnsurePreloadedAsset(registry);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MapPrefabRegistry rebuilt: " + entries.Count + " prefabs from " + ScanDirectories.Length + " directories");
        }

        private static string GetPrefabId(GameObject prefab)
        {
            MapExportMarker marker = prefab.GetComponent<MapExportMarker>();
            return marker != null && !string.IsNullOrEmpty(marker.prefab_id) ? marker.prefab_id : prefab.name;
        }

        private static void ValidatePrefab(GameObject prefab, string prefabId, string prefabPath)
        {
            MapExportMarker marker = prefab.GetComponent<MapExportMarker>();
            if (marker == null)
            {
                Debug.LogWarning("Map prefab should have MapExportMarker on root: " + prefabPath);
            }
            else if (marker.marker_type != MapExportMarkerType.MapObject)
            {
                Debug.LogWarning("Map prefab marker_type should be MapObject: " + prefabId);
            }

            if (prefab.GetComponentInChildren<Collider>(true) == null)
            {
                Debug.LogWarning("Map prefab should have at least one Collider: " + prefabId);
            }
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static void EnsurePreloadedAsset(PrefabRegistry registry)
        {
            Object[] preloadedAssets = PlayerSettings.GetPreloadedAssets();
            for (int i = 0; i < preloadedAssets.Length; i++)
            {
                if (preloadedAssets[i] == registry)
                {
                    return;
                }
            }

            Object[] next = new Object[preloadedAssets.Length + 1];
            preloadedAssets.CopyTo(next, 0);
            next[next.Length - 1] = registry;
            PlayerSettings.SetPreloadedAssets(next);
        }
    }
}
