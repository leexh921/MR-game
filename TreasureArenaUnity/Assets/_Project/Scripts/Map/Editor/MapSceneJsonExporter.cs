using System.IO;
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
            MapEditorBoundaryData[] boundaries = Object.FindObjectsOfType<MapEditorBoundaryData>(true);
            if (boundaries != null && boundaries.Length > 0)
            {
                map.map_boundary = MapSceneJsonBuilder.ToMapBoundaryJson(
                    boundaries[0].Points,
                    boundaries[0].Height,
                    boundaries[0].BoundaryType);
            }

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
