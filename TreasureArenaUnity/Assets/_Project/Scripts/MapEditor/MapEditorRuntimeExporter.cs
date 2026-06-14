using System.IO;
using System.Collections.Generic;
using TreasureArenaMR.Map;
using UnityEngine;

namespace TreasureArenaMR.MapEditor
{
    public sealed class MapEditorRuntimeExporter : MonoBehaviour
    {
        [SerializeField] private string mapName = "PicoMap";
        [SerializeField] private string mapDescription = "Exported from Pico MR map editor.";
        [SerializeField] private string outputFolderName = "TreasureArenaMR/MapExports";
        private const string PublicAndroidExportFolder = "/storage/emulated/0/Download/TreasureArenaMR/MapExports";

        public string MapName
        {
            get => mapName;
            set => mapName = value;
        }

        public string MapDescription
        {
            get => mapDescription;
            set => mapDescription = value;
        }

        public MapEditorRuntimeExportResult ExportCurrentMap()
        {
            MapJsonModels.MapJson map = BuildMapFromScene();
            MapValidationResult validation = new MapValidator().Validate(map);
            if (!validation.ok)
            {
                string errorText = validation.GetErrorText();
                Debug.LogError("Runtime map export failed: " + errorText);
                return MapEditorRuntimeExportResult.Fail(errorText);
            }

#if UNITY_EDITOR
            string directory = Path.Combine(Application.dataPath, "_Project", "StreamingAssets", "Maps");
            Directory.CreateDirectory(directory);
            string fileName = map.map_id + ".json";
            string absolutePath = Path.Combine(directory, fileName);
            File.WriteAllText(absolutePath, JsonUtility.ToJson(map, true));
            string projectPath = Path.Combine("Assets/_Project/StreamingAssets/Maps", fileName).Replace('\\', '/');
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("Runtime map JSON exported: " + projectPath);
            return MapEditorRuntimeExportResult.Success(projectPath, map);
#else
            string directory = Path.Combine(Application.persistentDataPath, outputFolderName);
            Directory.CreateDirectory(directory);
            string fileName = map.map_id + ".json";
            string path = Path.Combine(directory, fileName);
            string json = JsonUtility.ToJson(map, true);
            File.WriteAllText(path, json);

            string publicPath = TryWritePublicAndroidCopy(fileName, json);
            string message = string.IsNullOrEmpty(publicPath)
                ? "Runtime map JSON exported: " + path
                : "Runtime map JSON exported: " + path + "\nPublic copy: " + publicPath;
            Debug.Log(message);
            return MapEditorRuntimeExportResult.Success(path, publicPath, map);
#endif
        }

        public MapValidationResult ValidateCurrentMap()
        {
            return new MapValidator().Validate(BuildMapFromScene());
        }

        public MapJsonModels.MapJson BuildMapFromScene()
        {
            MapExportMarker[] markers = FindObjectsOfType<MapExportMarker>(true);
            MapJsonModels.MapJson map = MapSceneJsonBuilder.BuildFromMarkers(mapName, mapDescription, markers);
            ApplyBoundary(map, markers);
            MapEditorRuntimeController controller = FindObjectOfType<MapEditorRuntimeController>();
            if (controller != null)
            {
                map.floor_calibration.is_calibrated = controller.IsFloorCalibrated;
                map.floor_calibration.floor_y = controller.EditorFloorY;
                map.floor_calibration.source = "Manual";
            }

            return map;
        }

        private static void ApplyBoundary(MapJsonModels.MapJson map, MapExportMarker[] markers)
        {
            MapEditorBoundaryData boundary = FindObjectOfType<MapEditorBoundaryData>(true);
            if (boundary != null && boundary.IsClosed)
            {
                map.map_boundary = MapSceneJsonBuilder.ToMapBoundaryJson(
                    boundary.Points,
                    boundary.Height,
                    boundary.BoundaryType);
                Debug.Log("Runtime map export using MapEditorBoundaryData.");
                return;
            }

            map.map_boundary = BuildFallbackBoundary(markers);
            Debug.LogWarning("Runtime map export did not find a closed MapEditorBoundaryData. "
                + "Generated a fallback rectangular map_boundary from export markers.");
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

        private static string TryWritePublicAndroidCopy(string fileName, string json)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                Directory.CreateDirectory(PublicAndroidExportFolder);
                string publicPath = Path.Combine(PublicAndroidExportFolder, fileName);
                File.WriteAllText(publicPath, json);
                return publicPath;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Runtime map public export skipped: " + ex.Message);
            }
#endif

            return null;
        }
    }

    public sealed class MapEditorRuntimeExportResult
    {
        public bool ok;
        public string path;
        public string publicPath;
        public string error;
        public MapJsonModels.MapJson map;

        public static MapEditorRuntimeExportResult Success(string path, MapJsonModels.MapJson map)
        {
            return new MapEditorRuntimeExportResult
            {
                ok = true,
                path = path,
                map = map
            };
        }

        public static MapEditorRuntimeExportResult Success(string path, string publicPath, MapJsonModels.MapJson map)
        {
            return new MapEditorRuntimeExportResult
            {
                ok = true,
                path = path,
                publicPath = publicPath,
                map = map
            };
        }

        public static MapEditorRuntimeExportResult Fail(string error)
        {
            return new MapEditorRuntimeExportResult
            {
                ok = false,
                error = error
            };
        }
    }
}
