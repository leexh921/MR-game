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
}
