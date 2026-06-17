using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Map
{
    public enum MapExportMarkerType
    {
        MapObject,
        TeamBase,
        TreasureSpawnPoint,
        SupplyBox,
        [System.Obsolete("Retained for serialization compatibility; not used in active editor workflows.")]
        Bounds
    }

    /// <summary>
    /// Marks scene objects for map JSON export. This avoids relying on GameObject names for map semantics.
    ///
    /// MapObject supports two granularity modes:
    ///
    /// <b>Coarse-grained (MVP):</b> Place a single MapExportMarker on the root GameObject
    /// (e.g. Geometry or StaticMapRoot). The exporter emits one objects entry representing
    /// the entire static map. Recommended naming: obj_static_map / static_map_template_01.
    ///
    /// <b>Fine-grained (Pico editor):</b> Place a MapExportMarker on each placed prefab.
    /// The exporter emits one objects entry per prefab. Recommended naming:
    /// obj_wall_001 / wall_01, obj_cover_001 / cover_01, etc.
    ///
    /// Both modes coexist in the same objects array without protocol changes.
    /// Gameplay markers (TeamBase, TreasureSpawnPoint, SupplyBox) are always exported
    /// separately into their respective arrays regardless of which objects mode is used.
    /// </summary>
    public sealed class MapExportMarker : MonoBehaviour
    {
        /// <summary>
        /// MapObject can mean a whole-map root (coarse) or a single placed prefab (fine).
        /// The exporter does not distinguish by GameObject name — it only looks at this enum.
        /// </summary>
        public MapExportMarkerType marker_type = MapExportMarkerType.MapObject;
        public string id;
        public string prefab_id;
        public TeamType team = TeamType.None;
        public TreasureType treasure_type = TreasureType.Normal;
        public string supply_type = "WeaponRandom";
        public float refresh_interval = 20f;
        public float radius = 1f;
        public bool has_collider = true;
        public MapObjectType object_type = MapObjectType.StaticObstacle;
        public MapInteractionType interaction_type = MapInteractionType.None;
        public bool is_movable;
        public bool is_grabbable;
        public bool is_openable;
        public bool is_shootable = true;
        public bool blocks_bullet = true;
        public bool decal_enabled = true;
        public float mass;

        public string GetDefaultId()
        {
            return string.IsNullOrEmpty(id) ? gameObject.name : id;
        }
    }
}
