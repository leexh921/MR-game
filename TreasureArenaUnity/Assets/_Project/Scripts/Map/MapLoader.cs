using System;
using System.Collections;
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
        private readonly MapValidator validator = new MapValidator();

        public MapLoadResult LoadFromMapId(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
            {
                return MapLoadResult.Fail("missing_map_id");
            }

            string projectPath = Path.Combine(Application.dataPath, "_Project/StreamingAssets/Maps");
            projectPath = Path.Combine(projectPath, mapId + ".json");
            MapLoadResult projectResult = LoadFromPath(projectPath);
            if (projectResult.ok)
                return projectResult;

            string streamingPath = Path.Combine(Application.streamingAssetsPath, "Maps");
            streamingPath = Path.Combine(streamingPath, mapId + ".json");
            return LoadFromPath(streamingPath);
        }

        public MapLoadResult LoadFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return MapLoadResult.Fail("missing_path");
            }

            string json = ReadAllText(path, out string readError);
            if (string.IsNullOrEmpty(json))
            {
                return MapLoadResult.Fail(readError);
            }

            MapJsonModels.MapJson map = JsonUtility.FromJson<MapJsonModels.MapJson>(json);
            MapValidationResult validation = validator.Validate(map);
            if (!validation.ok)
            {
                return MapLoadResult.Fail("map_validation_failed:" + validation.GetErrorText(), map, validation);
            }

            return MapLoadResult.Success(map, validation, path);
        }

        public MapInstantiationResult InstantiateObjects(MapJsonModels.MapJson map, PrefabRegistry registry, Transform parent)
        {
            if (map == null)
            {
                return MapInstantiationResult.Fail("missing_map");
            }

            if (registry == null)
            {
                return MapInstantiationResult.Fail("missing_prefab_registry");
            }

            GameObject root = new GameObject(string.IsNullOrEmpty(map.map_id) ? "LoadedMap" : "LoadedMap_" + map.map_id);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            if (map.objects == null)
            {
                return MapInstantiationResult.Success(root, 0);
            }

            int created = 0;
            for (int i = 0; i < map.objects.Count; i++)
            {
                MapJsonModels.MapObjectJson mapObject = map.objects[i];
                if (mapObject == null)
                {
                    continue;
                }

                if (!registry.TryGetPrefab(mapObject.prefab_id, out GameObject prefab))
                {
                    UnityEngine.Object.Destroy(root);
                    return MapInstantiationResult.Fail("Missing map prefab: " + mapObject.prefab_id);
                }

                GameObject instance = UnityEngine.Object.Instantiate(prefab, root.transform);
                instance.name = string.IsNullOrEmpty(mapObject.object_id) ? mapObject.prefab_id : mapObject.object_id;
                instance.transform.SetPositionAndRotation(ToVector3(mapObject.position), Quaternion.Euler(ToVector3(mapObject.rotation)));
                instance.transform.localScale = ToVector3(mapObject.scale, Vector3.one);
                created++;
            }

            return MapInstantiationResult.Success(root, created);
        }

        public MapLoadResult LoadAndInstantiateFromMapId(string mapId, PrefabRegistry registry, Transform parent, out MapInstantiationResult instantiation)
        {
            MapLoadResult load = LoadFromMapId(mapId);
            instantiation = load.ok ? InstantiateObjects(load.map, registry, parent) : MapInstantiationResult.Fail(load.error);
            return load;
        }

        private static Vector3 ToVector3(MapJsonModels.Vector3Json value)
        {
            if (value == null)
            {
                return Vector3.zero;
            }

            return new Vector3(value.x, value.y, value.z);
        }

        private static Vector3 ToVector3(MapJsonModels.RotationJson value)
        {
            if (value == null)
            {
                return Vector3.zero;
            }

            return new Vector3(value.x, value.y, value.z);
        }

        private static Vector3 ToVector3(MapJsonModels.ScaleJson value, Vector3 fallback)
        {
            if (value == null)
            {
                return fallback;
            }

            return new Vector3(value.x, value.y, value.z);
        }

        private static string ReadAllText(string path, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(path))
            {
                error = "missing_path";
                return null;
            }

            if (path.Contains("://") || path.Contains(":///"))
            {
                using (UnityWebRequest request = UnityWebRequest.Get(path))
                {
                    UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                    while (!operation.isDone)
                    {
                    }

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        error = "map_file_not_found:" + path + ":" + request.error;
                        return null;
                    }

                    return request.downloadHandler.text;
                }
            }

            if (!File.Exists(path))
            {
                error = "map_file_not_found:" + path;
                return null;
            }

            return File.ReadAllText(path);
        }

        /// <summary>
        /// Non-blocking variant for platforms where StreamingAssets uses jar: URIs (Android).
        /// The caller must StartCoroutine this from a MonoBehaviour.
        /// onComplete receives (json, error) — exactly one will be non-null.
        /// </summary>
        public static IEnumerator ReadAllTextCoroutine(string path, Action<string, string> onComplete)
        {
            if (string.IsNullOrEmpty(path))
            {
                onComplete?.Invoke(null, "missing_path");
                yield break;
            }

            if (path.Contains("://") || path.Contains(":///"))
            {
                using (UnityWebRequest request = UnityWebRequest.Get(path))
                {
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        onComplete?.Invoke(null, "map_file_not_found:" + path + ":" + request.error);
                        yield break;
                    }

                    onComplete?.Invoke(request.downloadHandler.text, null);
                }

                yield break;
            }

            if (!File.Exists(path))
            {
                onComplete?.Invoke(null, "map_file_not_found:" + path);
                yield break;
            }

            onComplete?.Invoke(File.ReadAllText(path), null);
        }

        public static string ResolveMapPath(string mapId)
        {
            string projectPath = Path.Combine(Application.dataPath, "_Project/StreamingAssets/Maps");
            projectPath = Path.Combine(projectPath, mapId + ".json");
            if (File.Exists(projectPath))
                return projectPath;

            string streamingPath = Path.Combine(Application.streamingAssetsPath, "Maps");
            return Path.Combine(streamingPath, mapId + ".json");
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
