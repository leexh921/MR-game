using System.Collections.Generic;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Map
{
    /// <summary>
    /// A single zone on the map (spawn, respawn, base, treasure point, supply box).
    /// Maps to json_protocol.md spawn_zones / respawn_zones / team_bases entries.
    /// </summary>
    [System.Serializable]
    public sealed class MapZone
    {
        public string zone_id;
        public string team;
        public Vector3 position;
        public float radius;

        public MapZone() { }

        public MapZone(string zoneId, string team, Vector3 pos, float radius)
        {
            zone_id = zoneId;
            this.team = team;
            position = pos;
            this.radius = radius;
        }

        public TeamType TeamEnum
        {
            get
            {
                if (team == "Red") return TeamType.Red;
                if (team == "Blue") return TeamType.Blue;
                return TeamType.None;
            }
        }
    }

    /// <summary>
    /// Parsed map data ready for Server authority use.
    /// Provides coordinate access for spawn/respawn/base/treasure zones.
    /// 
    /// MVP: uses hardcoded test_map_01 data until MapLoader reads JSON.
    /// When MapLoader is ready, replace CreateTestMap() with real parsing.
    /// </summary>
    public sealed class MapData
    {
        public List<MapZone> spawnZones      = new List<MapZone>();
        public List<MapZone> respawnZones    = new List<MapZone>();
        public List<MapZone> teamBases       = new List<MapZone>();
        public List<MapZone> treasurePoints  = new List<MapZone>();

        public List<MapZone> GetSpawnZones(TeamType team)
        {
            string t = team == TeamType.Red ? "Red" : "Blue";
            return spawnZones.FindAll(z => z.team == t);
        }

        public MapZone GetTeamBase(TeamType team)
        {
            string t = team == TeamType.Red ? "Red" : "Blue";
            return teamBases.Find(z => z.team == t);
        }

        public MapZone GetRespawnZone(TeamType team)
        {
            string t = team == TeamType.Red ? "Red" : "Blue";
            return respawnZones.Find(z => z.team == t);
        }

        public MapZone GetFirstSpawn(TeamType team)
        {
            string t = team == TeamType.Red ? "Red" : "Blue";
            return spawnZones.Find(z => z.team == t);
        }

        /// <summary>
        /// Hardcoded test_map_01 data matching StreamingAssets/Maps/test_map_01.json.
        /// TODO: Replace with MapLoader.LoadFromMapId() when ready.
        /// </summary>
        public static MapData CreateTestMap()
        {
            var map = new MapData();

            map.spawnZones.Add(new MapZone("red_spawn_1", "Red",  new Vector3(-4f, 0f, 1.5f),  1.0f));
            map.spawnZones.Add(new MapZone("red_spawn_2", "Red",  new Vector3(-4f, 0f, -1.5f), 1.0f));
            map.spawnZones.Add(new MapZone("blue_spawn_1", "Blue", new Vector3(4f, 0f, 1.5f),  1.0f));
            map.spawnZones.Add(new MapZone("blue_spawn_2", "Blue", new Vector3(4f, 0f, -1.5f), 1.0f));

            map.respawnZones.Add(new MapZone("red_respawn", "Red",   new Vector3(-5f, 0f, 0f), 1.5f));
            map.respawnZones.Add(new MapZone("blue_respawn", "Blue", new Vector3(5f, 0f, 0f),  1.5f));

            map.teamBases.Add(new MapZone("red_base", "Red",   new Vector3(-5.5f, 0f, 0f), 1.5f));
            map.teamBases.Add(new MapZone("blue_base", "Blue", new Vector3(5.5f, 0f, 0f),  1.5f));

            map.treasurePoints.Add(new MapZone("treasure_n_001", "None", new Vector3(0f, 0.5f, 2.5f),  0.5f));
            map.treasurePoints.Add(new MapZone("treasure_n_002", "None", new Vector3(0f, 0.5f, -2.5f), 0.5f));
            map.treasurePoints.Add(new MapZone("treasure_r_001", "None", new Vector3(-2f, 0.5f, 0f),  0.5f));
            map.treasurePoints.Add(new MapZone("treasure_r_002", "None", new Vector3(2f, 0.5f, 0f),   0.5f));

            return map;
        }
    }
}
