using System;
using System.Collections.Generic;

namespace TreasureArenaMR.Map
{
    /// <summary>
    /// C# models that match the MVP map JSON protocol.
    /// </summary>
    public sealed class MapJsonModels
    {
        [Serializable]
        public sealed class MapJson
        {
            public string map_id;
            public string map_name;
            public string version = "1.0.0";
            public string description;
            public List<MapObjectJson> objects = new List<MapObjectJson>();
            public List<TeamBaseJson> team_bases = new List<TeamBaseJson>();
            public List<TreasureSpawnPointJson> treasure_spawn_points = new List<TreasureSpawnPointJson>();
            public List<SupplyBoxJson> supply_boxes = new List<SupplyBoxJson>();
            public BoundsJson bounds;
        }

        [Serializable]
        public sealed class MapObjectJson
        {
            public string object_id;
            public string prefab_id;
            public Vector3Json position;
            public RotationJson rotation;
            public ScaleJson scale;
            public bool has_collider;
        }

        [Serializable]
        public sealed class TeamBaseJson
        {
            public string base_id;
            public string team;
            public Vector3Json position;
            public float radius = 1f;
        }

        [Serializable]
        public sealed class TreasureSpawnPointJson
        {
            public string point_id;
            public string treasure_type;
            public Vector3Json position;
            public float radius = 0.5f;
        }

        [Serializable]
        public sealed class SupplyBoxJson
        {
            public string box_id;
            public Vector3Json position;
            public string supply_type;
            public float refresh_interval = 20f;
        }

        [Serializable]
        public sealed class BoundsJson
        {
            public Vector3Json center;
            public Vector3Json size;
        }

        [Serializable]
        public sealed class Vector3Json
        {
            public float x;
            public float y;
            public float z;
        }

        [Serializable]
        public sealed class RotationJson
        {
            public float x;
            public float y;
            public float z;
        }

        [Serializable]
        public sealed class ScaleJson
        {
            public float x = 1f;
            public float y = 1f;
            public float z = 1f;
        }
    }
}
