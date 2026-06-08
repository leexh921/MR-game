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
        public GameObject prefab;
        public string prefab_id;
        public MapExportMarkerType marker_type = MapExportMarkerType.MapObject;
        public TeamType team = TeamType.None;
        public TreasureType treasure_type = TreasureType.Normal;
        public string supply_type = "WeaponRandom";
        public float radius = 1f;
        public float refresh_interval = 20f;
        public bool has_collider = true;

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
