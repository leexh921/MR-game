using UnityEngine;

namespace TreasureArenaMR.Map
{
    public sealed class MapRuntimeLoader : MonoBehaviour
    {
        [SerializeField] private string mapId = "test_map_01";
        [SerializeField] private PrefabRegistry prefabRegistry;
        [SerializeField] private Transform loadedMapParent;
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool clearBeforeLoad = true;

        public MapLoadResult LastLoadResult { get; private set; }
        public MapInstantiationResult LastInstantiationResult { get; private set; }

        private void Start()
        {
            if (loadOnStart)
            {
                LoadConfiguredMap();
            }
        }

        public bool LoadConfiguredMap()
        {
            if (clearBeforeLoad)
            {
                ClearLoadedMap();
            }

            MapLoader loader = new MapLoader();
            LastLoadResult = loader.LoadFromMapId(mapId);
            if (LastLoadResult == null || !LastLoadResult.ok)
            {
                Debug.LogError("Map load failed: " + (LastLoadResult != null ? LastLoadResult.error : "null_result"));
                return false;
            }

            LastInstantiationResult = loader.InstantiateObjects(LastLoadResult.map, prefabRegistry, loadedMapParent != null ? loadedMapParent : transform);
            if (LastInstantiationResult == null || !LastInstantiationResult.ok)
            {
                Debug.LogError("Map instantiate failed: " + (LastInstantiationResult != null ? LastInstantiationResult.error : "null_result"));
                return false;
            }

            Debug.Log("Map loaded and instantiated: " + mapId + " objects=" + LastInstantiationResult.objectCount);
            return true;
        }

        public void ClearLoadedMap()
        {
            Transform parent = loadedMapParent != null ? loadedMapParent : transform;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
