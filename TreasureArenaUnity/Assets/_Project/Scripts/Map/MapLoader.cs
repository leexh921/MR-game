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
}
