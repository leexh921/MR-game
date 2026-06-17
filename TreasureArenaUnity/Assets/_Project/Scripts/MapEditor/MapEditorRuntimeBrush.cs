using System;
using TreasureArenaMR.Map;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.MapEditor
{
    [Serializable]
    public sealed class MapEditorRuntimeBrush
    {
        public string label;
        public string folder = "MapObjects";
        public GameObject prefab;
        public Sprite thumbnail;
        public string prefab_id;
        public MapExportMarkerType marker_type = MapExportMarkerType.MapObject;
        public TeamType team = TeamType.None;
        public TreasureType treasure_type = TreasureType.Normal;
        public string supply_type = "WeaponRandom";
        public float radius = 1f;
        public float refresh_interval = 20f;
        public bool has_collider = true;
        public MapObjectType object_type = MapObjectType.StaticObstacle;
        public MapInteractionType interaction_type = MapInteractionType.None;
        public Vector3 default_scale = Vector3.one;
        public Vector3 default_rotation;
        public bool is_movable;
        public bool is_grabbable;
        public bool is_openable;
        public bool is_shootable = true;
        public bool blocks_bullet = true;
        public bool decal_enabled = true;
        public float mass;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(label))
                {
                    return label;
                }

                return prefab != null ? prefab.name : marker_type.ToString();
            }
        }

        public string PrefabId
        {
            get
            {
                if (!string.IsNullOrEmpty(prefab_id))
                {
                    return prefab_id;
                }

                return prefab != null ? prefab.name : string.Empty;
            }
        }
    }
}
