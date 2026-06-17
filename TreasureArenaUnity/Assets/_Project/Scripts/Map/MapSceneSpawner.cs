using TreasureArenaMR.Network;
using TreasureArenaMR.Server;
using UnityEngine;

namespace TreasureArenaMR.Map
{
    /// <summary>
    /// Loads and instantiates map visuals on both server and client.
    /// Server reads mapId from RoomManager. Client waits for NetworkMatchState.
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
        private int _lastConfiguredMapIndex = -1;
        private int _lastConfiguredMapRevision = -1;
        private string _lastRoomManagerMapLog = "";

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

            var matchState = FindObjectOfType<NetworkMatchState>();
            if (matchState != null)
            {
                if (!matchState.MapConfigured)
                {
                    LogWaitingForNetworkMap();
                    return MapSpawnSource.Empty;
                }

                string mapId = RuntimeMapCatalog.GetMapId(matchState.MapIndex);
                if (string.IsNullOrEmpty(mapId))
                {
                    Debug.LogWarning($"{LogPrefix} Network map index is invalid: " +
                        $"mapIndex={matchState.MapIndex} mapRevision={matchState.MapRevision}");
                    return MapSpawnSource.Empty;
                }

                LogNetworkMapConfigured(matchState, mapId);
                return new MapSpawnSource(mapId, matchState.MapRevision);
            }

            var networkManager = NetworkManager.Instance;
            if (networkManager != null && !networkManager.IsServer)
            {
                LogWaitingForNetworkMap();
                return MapSpawnSource.Empty;
            }

            Debug.Log($"{LogPrefix} No network source, using default mapId={_defaultMapId}");
            return new MapSpawnSource(_defaultMapId, 0);
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

        private void LogWaitingForNetworkMap()
        {
            _waitingLogTimer -= Time.deltaTime;
            if (_waitingLogTimer > 0f)
                return;

            _waitingLogTimer = LogInterval;

            var networkManager = NetworkManager.Instance;
            var matchState = FindObjectOfType<NetworkMatchState>();
            Debug.Log($"{LogPrefix} Waiting for NetworkMatchState map configuration. " +
                $"networkManager={networkManager != null} " +
                $"isServer={(networkManager != null && networkManager.IsServer)} " +
                $"isClient={(networkManager != null && networkManager.IsClient)} " +
                $"isRunning={(networkManager != null && networkManager.IsRunning)} " +
                $"isConnected={(networkManager != null && networkManager.IsConnected)} " +
                $"matchState={matchState != null} " +
                $"mapConfigured={(matchState != null && matchState.MapConfigured)}");
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
            var networkManager = NetworkManager.Instance;
            var matchState = FindObjectOfType<NetworkMatchState>();
            Debug.Log($"{LogPrefix} heartbeat networkManager={networkManager != null} " +
                $"isServer={(networkManager != null && networkManager.IsServer)} " +
                $"isClient={(networkManager != null && networkManager.IsClient)} " +
                $"isRunning={(networkManager != null && networkManager.IsRunning)} " +
                $"isConnected={(networkManager != null && networkManager.IsConnected)} " +
                $"matchState={matchState != null} " +
                $"mapConfigured={(matchState != null && matchState.MapConfigured)} " +
                $"spawnedMapId={_spawnedMapId ?? "-"} spawnedRevision={_spawnedMapRevision}");
        }

        private void LogNetworkMapConfigured(NetworkMatchState matchState, string mapId)
        {
            if (_lastConfiguredMapIndex == matchState.MapIndex &&
                _lastConfiguredMapRevision == matchState.MapRevision)
            {
                return;
            }

            _lastConfiguredMapIndex = matchState.MapIndex;
            _lastConfiguredMapRevision = matchState.MapRevision;
            Debug.Log($"{LogPrefix} Network map configured mapIndex={matchState.MapIndex} " +
                $"mapRevision={matchState.MapRevision} mapId={mapId} " +
                $"roomState={matchState.RoomState} remaining={matchState.RemainingTime:F1}");
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
