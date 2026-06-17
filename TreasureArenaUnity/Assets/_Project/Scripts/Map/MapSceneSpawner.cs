using TreasureArenaMR.Network;
using TreasureArenaMR.Server;
using UnityEngine;

namespace TreasureArenaMR.Map
{
    /// <summary>
    /// Loads and instantiates map visuals on both server and client.
    /// Server reads mapId from RoomManager. Client waits for ClientMatchStateStore.
    /// </summary>
    public sealed class MapSceneSpawner : MonoBehaviour
    {
        private const float LogInterval = 2f;
        private const string LogPrefix = "[PicoMapSync]";

        [SerializeField] private PrefabRegistry _prefabRegistry;
        [SerializeField] private string _defaultMapId = "pico_map";

        private Transform _mapParent;
        private string _spawnedMapId;
        private int _spawnedMapRevision = -1;
        private float _statusLogTimer;
        private float _waitingLogTimer;
        private bool _loggedInitialState;
        private int _lastConfiguredMapRevision = -1;
        private string _lastRoomManagerMapLog = "";
        private string _lastClientMapIdLog = "";

        private void Start()
        {
            _mapParent = new GameObject("MapRoot").transform;
            _mapParent.SetParent(transform, false);
            LogInitialState();
        }

        private void Update()
        {
            LogStatusHeartbeat();

            MapSpawnSource source = ResolveMapSource();
            if (string.IsNullOrEmpty(source.MapId))
                return;

            if (_spawnedMapId == source.MapId && _spawnedMapRevision == source.Revision)
                return;

            if (SpawnMap(source.MapId))
            {
                _spawnedMapId = source.MapId;
                _spawnedMapRevision = source.Revision;
            }
        }

        private MapSpawnSource ResolveMapSource()
        {
            var roomManager = FindObjectOfType<RoomManager>();
            if (roomManager != null && roomManager.CurrentRoomConfig != null)
            {
                LogRoomManagerMapSource(roomManager);
                return new MapSpawnSource(roomManager.CurrentRoomConfig.map_id, 0);
            }

            var store = ClientMatchStateStore.Instance;
            if (store != null)
            {
                if (!store.HasMatchSnapshot || string.IsNullOrEmpty(store.MatchSnapshot.map_id))
                {
                    LogWaitingForNetworkMap(store);
                    return MapSpawnSource.Empty;
                }

                LogNetworkMapConfigured(store.MatchSnapshot.map_id, store.MatchSnapshot.map_revision, store.MatchSnapshot.room_state, store.MatchSnapshot.remaining_time);
                return new MapSpawnSource(store.MatchSnapshot.map_id, store.MatchSnapshot.map_revision);
            }

            LogWaitingForNetworkMap(null);
            return MapSpawnSource.Empty;
        }

        private bool SpawnMap(string mapId)
        {
            Debug.Log($"{LogPrefix} Spawn requested mapId={mapId} revisionCandidate={_spawnedMapRevision} " +
                $"prefabRegistry={_prefabRegistry != null}");

            var loader = new MapLoader();
            var loadResult = loader.LoadFromMapId(mapId);
            if (!loadResult.ok)
            {
                Debug.LogError($"{LogPrefix} Load failed mapId={mapId} error={loadResult.error}");
                return false;
            }

            Debug.Log($"{LogPrefix} Load succeeded mapId={mapId} path={loadResult.path} " +
                $"objects={(loadResult.map.objects != null ? loadResult.map.objects.Count : 0)} " +
                $"teamBases={(loadResult.map.team_bases != null ? loadResult.map.team_bases.Count : 0)} " +
                $"treasurePoints={(loadResult.map.treasure_spawn_points != null ? loadResult.map.treasure_spawn_points.Count : 0)}");

            ClearSpawnedMap();

            var instResult = loader.InstantiateObjects(loadResult.map, _prefabRegistry, _mapParent);
            if (!instResult.ok)
            {
                Debug.LogError($"{LogPrefix} Instantiate failed mapId={mapId} error={instResult.error} " +
                    $"prefabRegistry={_prefabRegistry != null}");
                return false;
            }

            Debug.Log($"{LogPrefix} Map spawned: {mapId}, objects={instResult.objectCount}, " +
                $"root={(instResult.root != null ? instResult.root.name : "null")}");
            return true;
        }

        private void ClearSpawnedMap()
        {
            if (_mapParent == null)
                return;

            for (int i = _mapParent.childCount - 1; i >= 0; i--)
            {
                Destroy(_mapParent.GetChild(i).gameObject);
            }
        }

        private void LogWaitingForNetworkMap(ClientMatchStateStore store)
        {
            _waitingLogTimer -= Time.deltaTime;
            if (_waitingLogTimer > 0f)
                return;

            _waitingLogTimer = LogInterval;

            Debug.Log($"{LogPrefix} Waiting for ClientMatchStateStore map configuration. " +
                $"store={store != null} connected={(store != null && store.IsConnected)} " +
                $"hasSnapshot={(store != null && store.HasMatchSnapshot)} " +
                $"mapId={(store != null && store.MatchSnapshot != null ? store.MatchSnapshot.map_id : "-")} " +
                $"revision={(store != null && store.MatchSnapshot != null ? store.MatchSnapshot.map_revision : -1)}");
        }

        private void LogInitialState()
        {
            if (_loggedInitialState)
                return;

            _loggedInitialState = true;
            Debug.Log($"{LogPrefix} Start platform={Application.platform} " +
                $"defaultMapId={_defaultMapId} prefabRegistry={_prefabRegistry != null}");
        }

        private void LogStatusHeartbeat()
        {
            _statusLogTimer -= Time.deltaTime;
            if (_statusLogTimer > 0f)
                return;

            _statusLogTimer = LogInterval;
            var store = ClientMatchStateStore.Instance;
            Debug.Log($"{LogPrefix} heartbeat store={store != null} " +
                $"connected={(store != null && store.IsConnected)} " +
                $"hasMatchSnapshot={(store != null && store.HasMatchSnapshot)} " +
                $"snapshotMap={(store != null && store.MatchSnapshot != null ? store.MatchSnapshot.map_id : "-")} " +
                $"snapshotRevision={(store != null && store.MatchSnapshot != null ? store.MatchSnapshot.map_revision : -1)} " +
                $"spawnedMapId={_spawnedMapId ?? "-"} spawnedRevision={_spawnedMapRevision}");
        }

        private void LogNetworkMapConfigured(string mapId, int mapRevision, string roomState, float remainingTime)
        {
            if (_lastClientMapIdLog == mapId &&
                _lastConfiguredMapRevision == mapRevision)
            {
                return;
            }

            _lastClientMapIdLog = mapId;
            _lastConfiguredMapRevision = mapRevision;
            Debug.Log($"{LogPrefix} Network map configured mapId={mapId} " +
                $"mapRevision={mapRevision} roomState={roomState} remaining={remainingTime:F1}");
        }

        private void LogRoomManagerMapSource(RoomManager roomManager)
        {
            string mapId = roomManager.CurrentRoomConfig.map_id;
            string logKey = $"{mapId}|{roomManager.CurrentRoomState}|{roomManager.MapData != null}";
            if (_lastRoomManagerMapLog == logKey)
                return;

            _lastRoomManagerMapLog = logKey;
            Debug.Log($"{LogPrefix} RoomManager map source mapId={mapId} " +
                $"state={roomManager.CurrentRoomState} mapData={roomManager.MapData != null}");
        }

        private readonly struct MapSpawnSource
        {
            public static readonly MapSpawnSource Empty = new MapSpawnSource(null, -1);

            public MapSpawnSource(string mapId, int revision)
            {
                MapId = mapId;
                Revision = revision;
            }

            public string MapId { get; }
            public int Revision { get; }
        }
    }
}
