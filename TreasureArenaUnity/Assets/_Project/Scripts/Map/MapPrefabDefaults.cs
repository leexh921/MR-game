using UnityEngine;

namespace TreasureArenaMR.Map
{
    public sealed class MapPrefabDefaults : MonoBehaviour
    {
        public MapObjectType object_type = MapObjectType.StaticObstacle;
        public MapInteractionType interaction_type = MapInteractionType.None;
        public Vector3 default_scale = Vector3.one;
        public Vector3 default_rotation;
        public bool has_collider = true;
        public bool is_movable;
        public bool is_grabbable;
        public bool is_openable;
        public bool is_shootable = true;
        public bool blocks_bullet = true;
        public bool decal_enabled = true;
        public float mass;
    }
}
