using System.Collections.Generic;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Map
{
    /// <summary>
    /// Runtime-facing map access wrapper for server authority code.
    /// </summary>
    public sealed class MapData
    {
        private readonly MapJsonModels.MapJson map;

        public MapData(MapJsonModels.MapJson map)
        {
            this.map = map;
        }

        public List<Vector3> GetSpawnZones(TeamType team)
        {
            List<Vector3> spawnZones = new List<Vector3>();
            if (team == TeamType.None || map == null || map.team_bases == null)
            {
                return spawnZones;
            }

            string teamName = team.ToString();
            for (int i = 0; i < map.team_bases.Count; i++)
            {
                MapJsonModels.TeamBaseJson teamBase = map.team_bases[i];
                if (teamBase != null && teamBase.team == teamName)
                {
                    spawnZones.Add(ToVector3(teamBase.position));
                }
            }

            return spawnZones;
        }

        public MapZone GetTeamBase(TeamType team)
        {
            return FindTeamBase(team);
        }

        public MapZone GetRespawnZone(TeamType team)
        {
            return FindTeamBase(team);
        }

        public List<TreasureSpawnPoint> GetTreasureSpawnPoints()
        {
            List<TreasureSpawnPoint> points = new List<TreasureSpawnPoint>();
            if (map == null || map.treasure_spawn_points == null)
            {
                return points;
            }

            for (int i = 0; i < map.treasure_spawn_points.Count; i++)
            {
                MapJsonModels.TreasureSpawnPointJson point = map.treasure_spawn_points[i];
                if (point == null)
                    continue;

                points.Add(new TreasureSpawnPoint(
                    string.IsNullOrEmpty(point.point_id) ? "treasure_point_" + i : point.point_id,
                    ParseTreasureType(point.treasure_type),
                    ToVector3(point.position),
                    point.radius));
            }

            return points;
        }

        public List<MapObjectDefinition> GetObjects()
        {
            List<MapObjectDefinition> objects = new List<MapObjectDefinition>();
            if (map == null || map.objects == null)
            {
                return objects;
            }

            for (int i = 0; i < map.objects.Count; i++)
            {
                MapJsonModels.MapObjectJson item = map.objects[i];
                if (item == null)
                {
                    continue;
                }

                objects.Add(new MapObjectDefinition(item));
            }

            return objects;
        }

        public MapBoundary GetMapBoundary()
        {
            if (map == null || map.map_boundary == null)
            {
                return null;
            }

            List<Vector3> points = new List<Vector3>();
            if (map.map_boundary.points != null)
            {
                for (int i = 0; i < map.map_boundary.points.Count; i++)
                {
                    if (map.map_boundary.points[i] != null)
                    {
                        points.Add(ToVector3(map.map_boundary.points[i]));
                    }
                }
            }

            return new MapBoundary(map.map_boundary.boundary_type, map.map_boundary.height, points);
        }

        public bool ContainsInMapBoundary(Vector3 position)
        {
            MapBoundary boundary = GetMapBoundary();
            return boundary == null || boundary.Contains(position);
        }

        private MapZone FindTeamBase(TeamType team)
        {
            if (team == TeamType.None || map == null || map.team_bases == null)
            {
                return null;
            }

            string teamName = team.ToString();
            for (int i = 0; i < map.team_bases.Count; i++)
            {
                MapJsonModels.TeamBaseJson teamBase = map.team_bases[i];
                if (teamBase != null && teamBase.team == teamName)
                {
                    return new MapZone(ToVector3(teamBase.position), teamBase.radius);
                }
            }

            return null;
        }

        private static Vector3 ToVector3(MapJsonModels.Vector3Json value)
        {
            if (value == null)
            {
                return Vector3.zero;
            }

            return new Vector3(value.x, value.y, value.z);
        }

        private static TreasureType ParseTreasureType(string value)
        {
            if (System.Enum.TryParse(value, true, out TreasureType parsed))
                return parsed;

            return TreasureType.Normal;
        }
    }

    public sealed class MapZone
    {
        public MapZone(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }

        public Vector3 Position { get; private set; }
        public float Radius { get; private set; }
    }

    public sealed class TreasureSpawnPoint
    {
        public TreasureSpawnPoint(string pointId, TreasureType treasureType, Vector3 position, float radius)
        {
            PointId = pointId;
            TreasureType = treasureType;
            Position = position;
            Radius = radius;
        }

        public string PointId { get; private set; }
        public TreasureType TreasureType { get; private set; }
        public Vector3 Position { get; private set; }
        public float Radius { get; private set; }
    }

    public sealed class MapBoundary
    {
        private readonly List<Vector3> points;

        public MapBoundary(string boundaryType, float height, List<Vector3> points)
        {
            BoundaryType = string.IsNullOrEmpty(boundaryType) ? "Polygon" : boundaryType;
            Height = height;
            this.points = points ?? new List<Vector3>();
        }

        public string BoundaryType { get; private set; }
        public float Height { get; private set; }
        public IReadOnlyList<Vector3> Points => points;

        public bool Contains(Vector3 position)
        {
            if (points.Count < 3 || BoundaryType != "Polygon")
            {
                return true;
            }

            float floorY = points[0].y;
            if (position.y < floorY || position.y > floorY + Height)
            {
                return false;
            }

            bool inside = false;
            int previous = points.Count - 1;
            for (int current = 0; current < points.Count; current++)
            {
                Vector3 a = points[current];
                Vector3 b = points[previous];
                bool crosses = (a.z > position.z) != (b.z > position.z);
                if (crosses)
                {
                    float xAtZ = (b.x - a.x) * (position.z - a.z) / (b.z - a.z) + a.x;
                    if (position.x < xAtZ)
                    {
                        inside = !inside;
                    }
                }

                previous = current;
            }

            return inside;
        }
    }

    public sealed class MapObjectDefinition
    {
        public MapObjectDefinition(MapJsonModels.MapObjectJson json)
        {
            ObjectId = json.object_id;
            PrefabId = json.prefab_id;
            ObjectType = ParseObjectType(json.object_type);
            InteractionType = ParseInteractionType(json.interaction_type);
            Position = ToVector3(json.position);
            Rotation = ToVector3(json.rotation);
            Scale = ToVector3(json.scale);
            HasCollider = json.has_collider;
            IsMovable = json.is_movable;
            IsGrabbable = json.is_grabbable;
            IsOpenable = json.is_openable;
            IsShootable = json.is_shootable;
            BlocksBullet = json.blocks_bullet;
            DecalEnabled = json.decal_enabled;
            Mass = json.mass;
        }

        public string ObjectId { get; private set; }
        public string PrefabId { get; private set; }
        public MapObjectType ObjectType { get; private set; }
        public MapInteractionType InteractionType { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector3 Rotation { get; private set; }
        public Vector3 Scale { get; private set; }
        public bool HasCollider { get; private set; }
        public bool IsMovable { get; private set; }
        public bool IsGrabbable { get; private set; }
        public bool IsOpenable { get; private set; }
        public bool IsShootable { get; private set; }
        public bool BlocksBullet { get; private set; }
        public bool DecalEnabled { get; private set; }
        public float Mass { get; private set; }

        private static MapObjectType ParseObjectType(string value)
        {
            if (System.Enum.TryParse(value, true, out MapObjectType parsed))
            {
                return parsed;
            }

            return MapObjectType.StaticObstacle;
        }

        private static MapInteractionType ParseInteractionType(string value)
        {
            if (System.Enum.TryParse(value, true, out MapInteractionType parsed))
            {
                return parsed;
            }

            return MapInteractionType.None;
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

        private static Vector3 ToVector3(MapJsonModels.ScaleJson value)
        {
            if (value == null)
            {
                return Vector3.one;
            }

            return new Vector3(value.x, value.y, value.z);
        }
    }
}
