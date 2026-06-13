using UnityEngine;

namespace TreasureArenaMR.Map
{
    public sealed class MapRuntimeObject : MonoBehaviour
    {
        public string object_id;
        public string prefab_id;
        public MapObjectType object_type = MapObjectType.StaticObstacle;
        public MapInteractionType interaction_type = MapInteractionType.None;
        public bool is_movable;
        public bool is_grabbable;
        public bool is_openable;
        public bool is_shootable;
        public bool blocks_bullet;
        public bool decal_enabled;
        public float mass;
    }
}
