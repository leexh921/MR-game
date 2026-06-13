using System.IO;
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
            MapEditorRuntimeController controller = FindObjectOfType<MapEditorRuntimeController>();
            if (controller != null)
            {
                map.floor_calibration.is_calibrated = controller.IsFloorCalibrated;
                map.floor_calibration.floor_y = controller.EditorFloorY;
                map.floor_calibration.source = "Manual";
            }

            MapEditorBoundaryData[] boundaries = FindObjectsOfType<MapEditorBoundaryData>(true);
            if (boundaries != null && boundaries.Length > 0)
            {
                map.map_boundary = MapSceneJsonBuilder.ToMapBoundaryJson(
                    boundaries[0].Points,
                    boundaries[0].Height,
                    boundaries[0].BoundaryType);
            }

            return map;
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
