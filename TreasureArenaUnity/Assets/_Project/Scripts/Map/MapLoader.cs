using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace TreasureArenaMR.Map
{
    /// <summary>
    /// Loads map JSON from StreamingAssets and validates it. Runtime instantiation is handled later.
    /// </summary>
    public sealed class MapLoader
    {
        private const string LogPrefix = "[PicoMapSync]";

        private readonly MapValidator validator = new MapValidator();

        public MapLoadResult LoadFromMapId(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
            {
                return MapLoadResult.Fail("missing_map_id");
            }

            string fileName = mapId + ".json";
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "Maps", fileName);
            Debug.Log($"{LogPrefix} MapLoader LoadFromMapId mapId={mapId} streamingPath={streamingPath} " +
                $"platform={Application.platform}");

            MapLoadResult streamingResult = LoadFromPath(streamingPath);
            if (streamingResult.ok)
            {
                Debug.Log($"{LogPrefix} MapLoader loaded from StreamingAssets mapId={mapId} path={streamingResult.path}");
                return streamingResult;
            }

#if UNITY_EDITOR
            string editorPath = Path.Combine(Application.dataPath, "_Project/StreamingAssets/Maps", fileName);
            Debug.Log($"{LogPrefix} MapLoader StreamingAssets read failed, trying editor fallback. " +
                $"mapId={mapId} error={streamingResult.error} editorPath={editorPath}");

            MapLoadResult editorResult = LoadFromPath(editorPath);
            if (editorResult.ok)
            {
                Debug.Log($"{LogPrefix} MapLoader loaded from editor fallback mapId={mapId} path={editorResult.path}");
                return editorResult;
            }

            Debug.LogError($"{LogPrefix} MapLoader failed mapId={mapId} " +
                $"streamingError={streamingResult.error} editorError={editorResult.error}");
            return MapLoadResult.Fail(streamingResult.error + "; editor_fallback:" + editorResult.error);
#else
            Debug.LogError($"{LogPrefix} MapLoader failed mapId={mapId} error={streamingResult.error}");
            return streamingResult;
#endif
        }

        public MapLoadResult LoadFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return MapLoadResult.Fail("missing_path");
            }

            if (!TryReadText(path, out string json, out string error))
            {
                Debug.LogWarning($"{LogPrefix} MapLoader read failed path={path} error={error}");
                return MapLoadResult.Fail(error);
            }

            MapJsonModels.MapJson map = JsonUtility.FromJson<MapJsonModels.MapJson>(json);
            MapValidationResult validation = validator.Validate(map);
            if (!validation.ok)
            {
                Debug.LogError($"{LogPrefix} MapLoader validation failed path={path} " +
                    $"errors={validation.GetErrorText()}");
                return MapLoadResult.Fail("map_validation_failed:" + validation.GetErrorText(), map, validation);
            }

            Debug.Log($"{LogPrefix} MapLoader validation ok path={path} mapId={map.map_id} " +
                $"objects={(map.objects != null ? map.objects.Count : 0)}");
            return MapLoadResult.Success(map, validation, path);
        }

        public MapInstantiationResult InstantiateObjects(
            MapJsonModels.MapJson map,
            PrefabRegistry prefabRegistry,
            Transform parent)
        {
            if (map == null)
            {
                Debug.LogError($"{LogPrefix} InstantiateObjects failed: map_is_null");
                return MapInstantiationResult.Fail("map_is_null");
            }

            MapInstantiationResult registryValidation = ValidatePrefabRegistry(map, prefabRegistry);
            if (!registryValidation.ok)
            {
                Debug.LogError($"{LogPrefix} InstantiateObjects registry validation failed " +
                    $"mapId={map.map_id} error={registryValidation.error}");
                return registryValidation;
            }

            GameObject root = new MapRuntimeBuilder().Build(map, prefabRegistry);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            return MapInstantiationResult.Success(root, map.objects != null ? map.objects.Count : 0);
        }

        private static bool TryReadText(string path, out string text, out string error)
        {
            text = null;
            error = null;

            if (CanReadDirectly(path))
            {
                if (!File.Exists(path))
                {
                    error = "map_file_not_found:" + path;
                    return false;
                }

                text = File.ReadAllText(path);
                return true;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(path))
            {
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    error = "map_file_read_failed:" + path + ":" + request.error;
                    return false;
                }

                text = request.downloadHandler.text;
                return true;
            }
        }

        private static bool CanReadDirectly(string path)
        {
            return !path.Contains("://") && !path.Contains(":///");
        }

        private static MapInstantiationResult ValidatePrefabRegistry(MapJsonModels.MapJson map, PrefabRegistry prefabRegistry)
        {
            if (prefabRegistry == null)
            {
                Debug.LogError($"{LogPrefix} PrefabRegistry missing.");
                return MapInstantiationResult.Fail("missing_map_prefab_registry");
            }

            if (map.objects == null)
            {
                return MapInstantiationResult.Success(null, 0);
            }

            for (int i = 0; i < map.objects.Count; i++)
            {
                MapJsonModels.MapObjectJson item = map.objects[i];
                if (item == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(item.prefab_id))
                {
                    Debug.LogError($"{LogPrefix} Prefab id missing objectId={item.object_id}");
                    return MapInstantiationResult.Fail("missing_prefab_id:" + item.object_id);
                }

                if (!prefabRegistry.TryGetPrefab(item.prefab_id, out GameObject prefab) || prefab == null)
                {
                    Debug.LogError($"{LogPrefix} Prefab not registered prefabId={item.prefab_id} " +
                        $"objectId={item.object_id}");
                    return MapInstantiationResult.Fail("prefab_not_registered:" + item.prefab_id);
                }
            }

            return MapInstantiationResult.Success(null, map.objects.Count);
        }
    }

    public sealed class MapLoadResult
    {
        public bool ok;
        public string error;
        public string path;
        public MapJsonModels.MapJson map;
        public MapData mapData;
        public MapValidationResult validation;

        public static MapLoadResult Success(MapJsonModels.MapJson map, MapValidationResult validation, string path)
        {
            return new MapLoadResult
            {
                ok = true,
                map = map,
                mapData = new MapData(map),
                validation = validation,
                path = path
            };
        }

        public static MapLoadResult Fail(string error)
        {
            return Fail(error, null, null);
        }

        public static MapLoadResult Fail(string error, MapJsonModels.MapJson map, MapValidationResult validation)
        {
            return new MapLoadResult
            {
                ok = false,
                error = error,
                map = map,
                mapData = null,
                validation = validation
            };
        }
    }

    public sealed class MapInstantiationResult
    {
        public bool ok;
        public string error;
        public GameObject root;
        public int objectCount;

        public static MapInstantiationResult Success(GameObject root, int objectCount)
        {
            return new MapInstantiationResult
            {
                ok = true,
                root = root,
                objectCount = objectCount
            };
        }

        public static MapInstantiationResult Fail(string error)
        {
            return new MapInstantiationResult
            {
                ok = false,
                error = error
            };
        }
    }
}
