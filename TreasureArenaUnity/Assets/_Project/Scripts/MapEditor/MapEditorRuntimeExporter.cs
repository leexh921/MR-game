using System.IO;
using TreasureArenaMR.Map;
using UnityEngine;

namespace TreasureArenaMR.MapEditor
{
    public sealed class MapEditorRuntimeExporter : MonoBehaviour
    {
        [SerializeField] private string mapName = "PicoMap";
        [SerializeField] private string mapDescription = "Exported from Pico MR map editor.";
        [SerializeField] private string outputFolderName = "MapExports";

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

<<<<<<< HEAD
=======
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
>>>>>>> origin/develop
            string directory = Path.Combine(Application.persistentDataPath, outputFolderName);
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, map.map_id + ".json");
            File.WriteAllText(path, JsonUtility.ToJson(map, true));
            Debug.Log("Runtime map JSON exported: " + path);
            return MapEditorRuntimeExportResult.Success(path, map);
<<<<<<< HEAD
=======
#endif
>>>>>>> origin/develop
        }

        public MapValidationResult ValidateCurrentMap()
        {
            return new MapValidator().Validate(BuildMapFromScene());
        }

        public MapJsonModels.MapJson BuildMapFromScene()
        {
            MapExportMarker[] markers = FindObjectsOfType<MapExportMarker>(true);
            return MapSceneJsonBuilder.BuildFromMarkers(mapName, mapDescription, markers);
        }
    }

    public sealed class MapEditorRuntimeExportResult
    {
        public bool ok;
        public string path;
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
