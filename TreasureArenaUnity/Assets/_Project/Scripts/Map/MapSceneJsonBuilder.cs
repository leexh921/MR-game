using System.Text;
using UnityEngine;

namespace TreasureArenaMR.Map
{
    public static class MapSceneJsonBuilder
    {
        public static MapJsonModels.MapJson BuildFromMarkers(string mapName, string description, MapExportMarker[] markers)
        {
            MapJsonModels.MapJson map = new MapJsonModels.MapJson
            {
                map_id = ToMapId(mapName),
                map_name = mapName,
                version = "1.0.0",
                description = description
            };

            if (markers == null)
            {
                return map;
            }

            for (int i = 0; i < markers.Length; i++)
            {
                AddMarker(map, markers[i]);
            }

            return map;
        }

        public static string ToMapId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "untitled_map";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
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

            string mapId = builder.ToString().Trim('_');
            return string.IsNullOrEmpty(mapId) ? "untitled_map" : mapId;
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

        private static MapJsonModels.Vector3Json ToVector3Json(Vector3 value)
        {
            return new MapJsonModels.Vector3Json { x = value.x, y = value.y, z = value.z };
        }

        private static MapJsonModels.RotationJson ToRotationJson(Vector3 value)
        {
            return new MapJsonModels.RotationJson { x = value.x, y = value.y, z = value.z };
        }

        private static MapJsonModels.ScaleJson ToScaleJson(Vector3 value)
        {
            return new MapJsonModels.ScaleJson { x = value.x, y = value.y, z = value.z };
        }
    }
}
