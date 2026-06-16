using System.IO;
using UnityEngine;

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

            string path = Path.Combine(Application.dataPath, "_Project/StreamingAssets/Maps");
            path = Path.Combine(path, mapId + ".json");
            return LoadFromPath(path);
        }

        public MapLoadResult LoadFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return MapLoadResult.Fail("missing_path");
            }

            if (!File.Exists(path))
            {
                return MapLoadResult.Fail("map_file_not_found:" + path);
            }

            string json = File.ReadAllText(path);
            MapJsonModels.MapJson map = JsonUtility.FromJson<MapJsonModels.MapJson>(json);
            MapValidationResult validation = validator.Validate(map);
            if (!validation.ok)
            {
                return MapLoadResult.Fail("map_validation_failed:" + validation.GetErrorText(), map, validation);
            }

            return MapLoadResult.Success(map, validation, path);
        }

        public MapInstantiationResult InstantiateObjects(
            MapJsonModels.MapJson map,
            PrefabRegistry prefabRegistry,
            Transform parent)
        {
            if (map == null)
            {
                return MapInstantiationResult.Fail("map_is_null");
            }

            MapInstantiationResult registryValidation = ValidatePrefabRegistry(map, prefabRegistry);
            if (!registryValidation.ok)
            {
                return registryValidation;
            }

            GameObject root = new MapRuntimeBuilder().Build(map, prefabRegistry);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            return MapInstantiationResult.Success(root, map.objects != null ? map.objects.Count : 0);
        }

        private static MapInstantiationResult ValidatePrefabRegistry(MapJsonModels.MapJson map, PrefabRegistry prefabRegistry)
        {
            if (prefabRegistry == null)
            {
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
                    return MapInstantiationResult.Fail("missing_prefab_id:" + item.object_id);
                }

                if (!prefabRegistry.TryGetPrefab(item.prefab_id, out GameObject prefab) || prefab == null)
                {
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
