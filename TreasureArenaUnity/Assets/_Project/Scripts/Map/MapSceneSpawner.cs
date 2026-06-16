using TreasureArenaMR.Network;
using TreasureArenaMR.Server;
using UnityEngine;

namespace TreasureArenaMR.Map
{
    /// <summary>
    /// Loads and instantiates map visuals on both server and client.
    /// Server reads mapId from RoomManager. Client reads MapIndex from NetworkMatchState.
    /// </summary>
    public sealed class MapSceneSpawner : MonoBehaviour
    {
        [SerializeField] private PrefabRegistry _prefabRegistry;
        [SerializeField] private string _defaultMapId = "pico_map";

        private bool _spawned;
        private Transform _mapParent;

        private void Start()
        {
            _mapParent = new GameObject("MapRoot").transform;
            _mapParent.SetParent(transform, false);
        }

        private void Update()
        {
            if (_spawned) return;

            string mapId = ResolveMapId();
            if (string.IsNullOrEmpty(mapId)) return;

            SpawnMap(mapId);
            _spawned = true;
        }

        private string ResolveMapId()
        {
            var roomManager = FindObjectOfType<RoomManager>();
            if (roomManager != null && roomManager.CurrentRoomConfig != null)
            {
                Debug.Log($"[MapSceneSpawner] Resolved mapId from RoomManager: {roomManager.CurrentRoomConfig.map_id}");
                return roomManager.CurrentRoomConfig.map_id;
            }

            var matchState = FindObjectOfType<NetworkMatchState>();
            if (matchState != null && matchState.MapConfigured)
            {
                string mapId = RuntimeMapCatalog.GetMapId(matchState.MapIndex);
                Debug.Log($"[MapSceneSpawner] Resolved mapId from NetworkMatchState: {mapId}");
                return mapId;
            }

            Debug.Log($"[MapSceneSpawner] No live source found, using default: {_defaultMapId}");
            return _defaultMapId;
        }

        private void SpawnMap(string mapId)
        {
            var loader = new MapLoader();
            var loadResult = loader.LoadFromMapId(mapId);
            if (!loadResult.ok)
            {
                Debug.LogError($"[MapSceneSpawner] Load failed: {loadResult.error}");
                return;
            }

            var instResult = loader.InstantiateObjects(loadResult.map, _prefabRegistry, _mapParent);
            if (!instResult.ok)
            {
                Debug.LogError($"[MapSceneSpawner] Instantiate failed: {instResult.error}");
                return;
            }

            Debug.Log($"[MapSceneSpawner] Map spawned: {mapId}, objects={instResult.objectCount}");
        }
    }
}
