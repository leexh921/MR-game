using System.Collections.Generic;

namespace TreasureArenaMR.Map
{
    /// <summary>
    /// Validates the minimum map data needed by TeamTreasure MVP.
    /// </summary>
    public sealed class MapValidator
    {
        public MapValidationResult Validate(MapJsonModels.MapJson map)
        {
            MapValidationResult result = new MapValidationResult();
            if (map == null)
            {
                result.errors.Add("map_is_null");
                return result;
            }

            RequireText(result, map.map_id, "missing_map_id");
            RequireText(result, map.map_name, "missing_map_name");
            RequireText(result, map.map_version, "missing_version");
            ValidateTeamBases(result, map.team_bases);
            ValidateTreasureSpawnPoints(result, map.treasure_spawn_points);
            ValidateSupplyBoxes(result, map.supply_boxes);
            ValidateObjects(result, map.objects);
            result.ok = result.errors.Count == 0;
            return result;
        }

        private static void ValidateTeamBases(MapValidationResult result, List<MapJsonModels.TeamBaseJson> bases)
        {
            if (bases == null || bases.Count == 0)
            {
                result.errors.Add("missing_team_bases");
                return;
            }

            bool hasRed = false;
            bool hasBlue = false;
            for (int i = 0; i < bases.Count; i++)
            {
                MapJsonModels.TeamBaseJson teamBase = bases[i];
                if (teamBase == null)
                {
                    result.errors.Add("team_base_is_null");
                    continue;
                }

                RequireText(result, teamBase.base_id, "missing_base_id");
                if (teamBase.team == "Red")
                {
                    hasRed = true;
                }
                else if (teamBase.team == "Blue")
                {
                    hasBlue = true;
                }
                else
                {
                    result.errors.Add("invalid_base_team:" + teamBase.team);
                }

                if (teamBase.radius <= 0f)
                {
                    result.errors.Add("invalid_base_radius:" + teamBase.base_id);
                }
            }

            if (!hasRed)
            {
                result.errors.Add("missing_red_base");
            }

            if (!hasBlue)
            {
                result.errors.Add("missing_blue_base");
            }
        }

        private static void ValidateTreasureSpawnPoints(MapValidationResult result, List<MapJsonModels.TreasureSpawnPointJson> points)
        {
            if (points == null || points.Count == 0)
            {
                result.errors.Add("missing_treasure_spawn_points");
                return;
            }

            for (int i = 0; i < points.Count; i++)
            {
                MapJsonModels.TreasureSpawnPointJson point = points[i];
                if (point == null)
                {
                    result.errors.Add("treasure_point_is_null");
                    continue;
                }

                RequireText(result, point.point_id, "missing_treasure_point_id");
                if (!IsTreasureType(point.treasure_type))
                {
                    result.errors.Add("invalid_treasure_type:" + point.treasure_type);
                }

                if (point.radius <= 0f)
                {
                    result.errors.Add("invalid_treasure_point_radius:" + point.point_id);
                }
            }
        }

        private static void ValidateSupplyBoxes(MapValidationResult result, List<MapJsonModels.SupplyBoxJson> supplyBoxes)
        {
            if (supplyBoxes == null)
            {
                return;
            }

            for (int i = 0; i < supplyBoxes.Count; i++)
            {
                MapJsonModels.SupplyBoxJson box = supplyBoxes[i];
                if (box == null)
                {
                    result.errors.Add("supply_box_is_null");
                    continue;
                }

                RequireText(result, box.box_id, "missing_supply_box_id");
                RequireText(result, box.supply_type, "missing_supply_type");
                if (box.refresh_interval <= 0f)
                {
                    result.errors.Add("invalid_supply_refresh_interval:" + box.box_id);
                }
            }
        }

        private static void ValidateObjects(MapValidationResult result, List<MapJsonModels.MapObjectJson> objects)
        {
            if (objects == null)
            {
                return;
            }

            for (int i = 0; i < objects.Count; i++)
            {
                MapJsonModels.MapObjectJson mapObject = objects[i];
                if (mapObject == null)
                {
                    result.errors.Add("map_object_is_null");
                    continue;
                }

                RequireText(result, mapObject.object_id, "missing_object_id");
                RequireText(result, mapObject.prefab_id, "missing_prefab_id:" + mapObject.object_id);
            }
        }

        private static bool IsTreasureType(string value)
        {
            return value == "Normal" || value == "Rare" || value == "Final";
        }

        private static void RequireText(MapValidationResult result, string value, string error)
        {
            if (string.IsNullOrEmpty(value))
            {
                result.errors.Add(error);
            }
        }
    }

    public sealed class MapValidationResult
    {
        public bool ok;
        public readonly List<string> errors = new List<string>();

        public string GetErrorText()
        {
            return string.Join(", ", errors.ToArray());
        }
    }
}
