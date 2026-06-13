using System.Collections.Generic;
using UnityEngine;

namespace TreasureArenaMR.Map
{
    /// <summary>
    /// Validates the minimum map data needed by TeamTreasure MVP.
    /// </summary>
    public sealed class MapValidator
    {
        public MapValidationResult Validate(MapJsonModels.MapJson map, PrefabRegistry prefabRegistry = null)
        {
            MapValidationResult result = new MapValidationResult();
            if (map == null)
            {
                result.errors.Add("map_is_null");
                return result;
            }

            RequireText(result, map.map_id, "missing_map_id");
            RequireText(result, map.map_name, "missing_map_name");
            RequireText(result, map.map_version, "missing_map_version");
            ValidateTeamBases(result, map.team_bases);
            ValidateTreasureSpawnPoints(result, map.treasure_spawn_points);
            ValidateSupplyBoxes(result, map.supply_boxes);
            ValidateMapBoundary(result, map.map_boundary);
            ValidateObjects(result, map.objects, map.map_boundary, prefabRegistry);
            ValidateGameplayPositions(result, map);
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

        private static void ValidateObjects(
            MapValidationResult result,
            List<MapJsonModels.MapObjectJson> objects,
            MapJsonModels.MapBoundaryJson boundary,
            PrefabRegistry prefabRegistry)
        {
            if (objects == null)
            {
                return;
            }

            HashSet<string> objectIds = new HashSet<string>();
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
                if (!string.IsNullOrEmpty(mapObject.object_id) && !objectIds.Add(mapObject.object_id))
                {
                    result.errors.Add("duplicate_object_id:" + mapObject.object_id);
                }

                if (!IsObjectType(mapObject.object_type))
                {
                    result.errors.Add("invalid_object_type:" + mapObject.object_id + ":" + mapObject.object_type);
                }

                if (!IsInteractionType(mapObject.interaction_type))
                {
                    result.errors.Add("invalid_interaction_type:" + mapObject.object_id + ":" + mapObject.interaction_type);
                }

                if ((mapObject.is_grabbable || mapObject.is_openable) && string.IsNullOrEmpty(mapObject.interaction_type))
                {
                    result.errors.Add("missing_interaction_type:" + mapObject.object_id);
                }

                if (mapObject.is_grabbable && mapObject.interaction_type != MapInteractionType.Grab.ToString())
                {
                    result.errors.Add("invalid_grab_interaction_type:" + mapObject.object_id);
                }

                if (mapObject.is_openable && mapObject.interaction_type != MapInteractionType.Openable.ToString())
                {
                    result.errors.Add("invalid_openable_interaction_type:" + mapObject.object_id);
                }

                if (mapObject.is_movable && mapObject.mass <= 0f)
                {
                    result.errors.Add("missing_movable_mass:" + mapObject.object_id);
                }

                if (mapObject.is_shootable && !mapObject.has_collider)
                {
                    result.errors.Add("shootable_missing_collider:" + mapObject.object_id);
                }

                if (boundary != null && !Contains(boundary, mapObject.position))
                {
                    result.errors.Add("object_outside_boundary:" + mapObject.object_id);
                }

                if (prefabRegistry != null)
                {
                    ValidatePrefab(result, mapObject, prefabRegistry);
                }
            }
        }

        private static void ValidateMapBoundary(MapValidationResult result, MapJsonModels.MapBoundaryJson boundary)
        {
            if (boundary == null)
            {
                result.errors.Add("missing_map_boundary");
                return;
            }

            if (boundary.boundary_type != "Polygon")
            {
                result.errors.Add("invalid_boundary_type:" + boundary.boundary_type);
            }

            if (boundary.height <= 0f)
            {
                result.errors.Add("invalid_boundary_height");
            }

            if (boundary.points == null || boundary.points.Count < 3)
            {
                result.errors.Add("invalid_boundary_points_count");
                return;
            }

            for (int i = 0; i < boundary.points.Count; i++)
            {
                if (boundary.points[i] == null)
                {
                    result.errors.Add("boundary_point_is_null:" + i);
                }
            }
        }

        private static void ValidateGameplayPositions(MapValidationResult result, MapJsonModels.MapJson map)
        {
            if (map.map_boundary == null)
            {
                return;
            }

            if (map.team_bases != null)
            {
                for (int i = 0; i < map.team_bases.Count; i++)
                {
                    MapJsonModels.TeamBaseJson item = map.team_bases[i];
                    if (item != null && !Contains(map.map_boundary, item.position))
                    {
                        result.errors.Add("team_base_outside_boundary:" + item.base_id);
                    }
                }
            }

            if (map.treasure_spawn_points != null)
            {
                for (int i = 0; i < map.treasure_spawn_points.Count; i++)
                {
                    MapJsonModels.TreasureSpawnPointJson item = map.treasure_spawn_points[i];
                    if (item != null && !Contains(map.map_boundary, item.position))
                    {
                        result.errors.Add("treasure_point_outside_boundary:" + item.point_id);
                    }
                }
            }

            if (map.supply_boxes != null)
            {
                for (int i = 0; i < map.supply_boxes.Count; i++)
                {
                    MapJsonModels.SupplyBoxJson item = map.supply_boxes[i];
                    if (item != null && !Contains(map.map_boundary, item.position))
                    {
                        result.errors.Add("supply_box_outside_boundary:" + item.box_id);
                    }
                }
            }
        }

        private static void ValidatePrefab(MapValidationResult result, MapJsonModels.MapObjectJson mapObject, PrefabRegistry prefabRegistry)
        {
            if (!prefabRegistry.TryGetPrefab(mapObject.prefab_id, out GameObject prefab))
            {
                result.errors.Add("prefab_id_not_registered:" + mapObject.prefab_id);
                return;
            }

            if (mapObject.is_shootable && prefab.GetComponentInChildren<Collider>(true) == null)
            {
                result.errors.Add("prefab_missing_collider:" + mapObject.object_id);
            }

            if (mapObject.is_movable && prefab.GetComponentInChildren<Rigidbody>(true) == null)
            {
                result.errors.Add("movable_prefab_missing_rigidbody:" + mapObject.object_id);
            }
        }

        private static bool IsObjectType(string value)
        {
            return value == MapObjectType.StaticFloor.ToString()
                || value == MapObjectType.StaticObstacle.ToString()
                || value == MapObjectType.PhysicsProp.ToString()
                || value == MapObjectType.OpenableObject.ToString();
        }

        private static bool IsInteractionType(string value)
        {
            return value == MapInteractionType.None.ToString()
                || value == MapInteractionType.Grab.ToString()
                || value == MapInteractionType.Openable.ToString();
        }

        private static bool Contains(MapJsonModels.MapBoundaryJson boundary, MapJsonModels.Vector3Json point)
        {
            if (boundary == null || point == null || boundary.points == null || boundary.points.Count < 3)
            {
                return true;
            }

            float floorY = boundary.points[0] != null ? boundary.points[0].y : point.y;
            if (point.y < floorY - 0.05f || point.y > floorY + boundary.height + 0.05f)
            {
                return false;
            }

            bool inside = false;
            int previous = boundary.points.Count - 1;
            for (int current = 0; current < boundary.points.Count; current++)
            {
                MapJsonModels.Vector3Json a = boundary.points[current];
                MapJsonModels.Vector3Json b = boundary.points[previous];
                if (a == null || b == null)
                {
                    previous = current;
                    continue;
                }

                bool crosses = (a.z > point.z) != (b.z > point.z);
                if (crosses)
                {
                    float xAtZ = (b.x - a.x) * (point.z - a.z) / (b.z - a.z) + a.x;
                    if (point.x < xAtZ)
                    {
                        inside = !inside;
                    }
                }

                previous = current;
            }

            return inside;
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
