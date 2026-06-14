using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using TreasureArenaMR.MapEditor;

namespace TreasureArenaMR.Map.Editor
{
    /// <summary>
    /// Exports a Unity scene to map JSON.
    ///
    /// <b>Export rule:</b> Only GameObjects carrying a MapExportMarker component are exported.
    /// Child objects without their own MapExportMarker are NOT exported, even if their parent
    /// has one. This rule supports both:
    ///
    /// <b>Coarse-grained (MVP):</b> One MapExportMarker(MapObject) on Geometry / StaticMapRoot
    /// produces a single objects entry for the whole static map.
    ///
    /// <b>Fine-grained (Pico editor):</b> Each placed prefab carries its own
    /// MapExportMarker(MapObject), producing one objects entry per prefab.
    ///
    /// Gameplay markers (TeamBase, TreasureSpawnPoint, SupplyBox) are exported into
    /// their respective arrays. Map boundary is exported from MapEditorBoundaryData.
    /// </summary>
    public static class MapSceneJsonExporter
    {
        [MenuItem("Tools/TreasureArena/Export Current Scene Map JSON")]
        public static void ExportCurrentScene()
        {
            ExportCurrentSceneToDefaultPath(true);
        }

        public static MapSceneJsonExportResult ExportCurrentSceneToDefaultPath(bool showDialog)
        {
            Scene scene = SceneManager.GetActiveScene();

            // Only GameObjects with MapExportMarker are exported. Child objects without
            // their own marker are skipped — no recursive auto-export of children.
            MapExportMarker[] markers = Object.FindObjectsOfType<MapExportMarker>(true);
            MapJsonModels.MapJson map = MapSceneJsonBuilder.BuildFromMarkers(
                scene.name,
                "Exported from Unity scene: " + scene.name,
                markers);
            ApplyBoundary(map, markers);

            string mapId = map.map_id;

            MapValidationResult validation = new MapValidator().Validate(map);
            if (!validation.ok)
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog("Map JSON Export Failed", validation.GetErrorText(), "OK");
                }

                Debug.LogError("Map JSON export failed: " + validation.GetErrorText());
                return MapSceneJsonExportResult.Fail(validation.GetErrorText());
            }

            string directory = Path.Combine(Application.dataPath, "_Project/StreamingAssets/Maps");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, mapId + ".json");
            File.WriteAllText(path, JsonUtility.ToJson(map, true));
            AssetDatabase.Refresh();
            Debug.Log("Map JSON exported: " + path);
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Map JSON Exported", path, "OK");
            }

            return MapSceneJsonExportResult.Success(path, map);
        }

        public static string ToMapId(string sceneName)
        {
            return MapSceneJsonBuilder.ToMapId(sceneName);
        }

        private static void ApplyBoundary(MapJsonModels.MapJson map, MapExportMarker[] markers)
        {
            MapEditorBoundaryData boundary = Object.FindObjectOfType<MapEditorBoundaryData>(true);
            if (boundary != null && boundary.IsClosed)
            {
                map.map_boundary = MapSceneJsonBuilder.ToMapBoundaryJson(
                    boundary.Points,
                    boundary.Height,
                    boundary.BoundaryType);
                Debug.Log("Map JSON export using MapEditorBoundaryData.");
                return;
            }

            map.map_boundary = BuildFallbackBoundary(markers);
            Debug.LogWarning("Map JSON export did not find a closed MapEditorBoundaryData. "
                + "Generated a fallback rectangular map_boundary from export markers. "
                + "For final maps, draw or configure an explicit boundary.");
        }

        private static MapJsonModels.MapBoundaryJson BuildFallbackBoundary(MapExportMarker[] markers)
        {
            const float defaultHalfSize = 5f;
            const float padding = 1.5f;
            bool hasPoint = false;
            float minX = 0f;
            float maxX = 0f;
            float minZ = 0f;
            float maxZ = 0f;
            float floorY = 0f;

            if (markers != null)
            {
                for (int i = 0; i < markers.Length; i++)
                {
                    MapExportMarker marker = markers[i];
                    if (marker == null)
                        continue;

                    Vector3 position = marker.transform.position;
                    if (!hasPoint)
                    {
                        minX = maxX = position.x;
                        minZ = maxZ = position.z;
                        floorY = position.y;
                        hasPoint = true;
                    }
                    else
                    {
                        minX = Mathf.Min(minX, position.x);
                        maxX = Mathf.Max(maxX, position.x);
                        minZ = Mathf.Min(minZ, position.z);
                        maxZ = Mathf.Max(maxZ, position.z);
                        floorY = Mathf.Min(floorY, position.y);
                    }
                }
            }

            if (!hasPoint)
            {
                minX = -defaultHalfSize;
                maxX = defaultHalfSize;
                minZ = -defaultHalfSize;
                maxZ = defaultHalfSize;
                floorY = 0f;
            }
            else
            {
                minX -= padding;
                maxX += padding;
                minZ -= padding;
                maxZ += padding;
            }

            List<Vector3> points = new List<Vector3>
            {
                new Vector3(minX, floorY, minZ),
                new Vector3(maxX, floorY, minZ),
                new Vector3(maxX, floorY, maxZ),
                new Vector3(minX, floorY, maxZ)
            };
            return MapSceneJsonBuilder.ToMapBoundaryJson(points, 2.5f);
        }
    }

    public sealed class MapSceneJsonExportResult
    {
        public bool ok;
        public string path;
        public string error;
        public MapJsonModels.MapJson map;

        public static MapSceneJsonExportResult Success(string path, MapJsonModels.MapJson map)
        {
            return new MapSceneJsonExportResult
            {
                ok = true,
                path = path,
                map = map
            };
        }

        public static MapSceneJsonExportResult Fail(string error)
        {
            return new MapSceneJsonExportResult
            {
                ok = false,
                error = error
            };
        }
    }
}
