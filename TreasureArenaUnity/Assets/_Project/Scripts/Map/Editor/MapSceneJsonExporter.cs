using System.IO;
using System.Text;
using TreasureArenaMR.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    /// Gameplay markers (TeamBase, TreasureSpawnPoint, SupplyBox, Bounds) are exported into
    /// their respective arrays regardless of parent-child relationships.
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
            string mapId = ToMapId(scene.name);
            MapJsonModels.MapJson map = new MapJsonModels.MapJson
            {
                map_id = mapId,
                map_name = scene.name,
                version = "1.0.0",
                description = "Exported from Unity scene: " + scene.name
            };

            // Only GameObjects with MapExportMarker are exported. Child objects without
            // their own marker are skipped — no recursive auto-export of children.
            MapExportMarker[] markers = Object.FindObjectsOfType<MapExportMarker>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                AddMarker(map, markers[i]);
            }

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

        private static void AddMarker(MapJsonModels.MapJson map, MapExportMarker marker)
        {
            if (marker == null)
            {
                return;
            }

            switch (marker.marker_type)
            {
                case MapExportMarkerType.TeamBase:
                    map.team_bases.Add(new MapJsonModels.TeamBaseJson
                    {
                        base_id = marker.GetDefaultId(),
                        team = marker.team.ToString(),
                        position = ToVector3Json(marker.transform.position),
                        radius = marker.radius
                    });
                    break;

                case MapExportMarkerType.TreasureSpawnPoint:
                    map.treasure_spawn_points.Add(new MapJsonModels.TreasureSpawnPointJson
                    {
                        point_id = marker.GetDefaultId(),
                        treasure_type = marker.treasure_type.ToString(),
                        position = ToVector3Json(marker.transform.position),
                        radius = marker.radius
                    });
                    break;

                case MapExportMarkerType.SupplyBox:
                    map.supply_boxes.Add(new MapJsonModels.SupplyBoxJson
                    {
                        box_id = marker.GetDefaultId(),
                        position = ToVector3Json(marker.transform.position),
                        supply_type = marker.supply_type,
                        refresh_interval = marker.refresh_interval
                    });
                    break;

                case MapExportMarkerType.Bounds:
                    map.bounds = new MapJsonModels.BoundsJson
                    {
                        center = ToVector3Json(marker.transform.position),
                        size = ToVector3Json(marker.transform.localScale)
                    };
                    break;

                default:
                    // MapObject: handles both coarse-grained (whole-map root) and
                    // fine-grained (individual prefab) exports. The exporter does not
                    // distinguish between the two — both produce one objects entry.
                    map.objects.Add(new MapJsonModels.MapObjectJson
                    {
                        object_id = marker.GetDefaultId(),
                        prefab_id = string.IsNullOrEmpty(marker.prefab_id) ? marker.gameObject.name : marker.prefab_id,
                        position = ToVector3Json(marker.transform.position),
                        rotation = ToRotationJson(marker.transform.eulerAngles),
                        scale = ToScaleJson(marker.transform.localScale),
                        has_collider = marker.has_collider
                    });
                    break;
            }
        }

        private static string ToMapId(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return "untitled_map";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < sceneName.Length; i++)
            {
                char c = sceneName[i];
                if (char.IsUpper(c) && i > 0 && builder[builder.Length - 1] != '_')
                {
                    builder.Append('_');
                }

                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(char.ToLowerInvariant(c));
                }
                else if (builder.Length > 0 && builder[builder.Length - 1] != '_')
                {
                    builder.Append('_');
                }
            }

            return builder.ToString().Trim('_');
        }

        private static MapJsonModels.Vector3Json ToVector3Json(Vector3 value)
        {
            return new MapJsonModels.Vector3Json
            {
                x = value.x,
                y = value.y,
                z = value.z
            };
        }

        private static MapJsonModels.RotationJson ToRotationJson(Vector3 value)
        {
            return new MapJsonModels.RotationJson
            {
                x = value.x,
                y = value.y,
                z = value.z
            };
        }

        private static MapJsonModels.ScaleJson ToScaleJson(Vector3 value)
        {
            return new MapJsonModels.ScaleJson
            {
                x = value.x,
                y = value.y,
                z = value.z
            };
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
