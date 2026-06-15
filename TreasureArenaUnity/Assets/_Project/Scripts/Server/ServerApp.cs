using Netick.Unity;
using TreasureArenaMR.Map;
using TreasureArenaMR.Network;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Server
{
    /// <summary>
    /// Server runtime role entry point.
    /// Bootstraps Netick server and server-side services.
    /// No database persistence in MVP.
    /// </summary>
    public sealed class ServerApp : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private RoomManager _roomManager;
        [SerializeField] private bool _autoStart;

        [Header("Network Prefabs")]
        [SerializeField] private GameObject _matchStatePrefab;

        public static ServerApp Instance { get; private set; }
        public bool IsRunning { get; private set; }

        private bool _networkRuntimeInitialized;
        private string _runtimeMapIdOverride;
        private int _mapRevision;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_autoStart)
                StartServer();
        }

        public void StartServer()
        {
            if (IsRunning)
            {
                Debug.LogWarning("[ServerApp] Server already running");
                return;
            }

            Debug.Log("[ServerApp] Booting server...");

            // 1. Ensure NetworkManager exists
            if (_networkManager == null)
                _networkManager = FindObjectOfType<NetworkManager>();
            if (_networkManager == null)
            {
                var go = new GameObject("NetworkManager");
                _networkManager = go.AddComponent<NetworkManager>();
            }

            // 3. Ensure RoomManager exists
            if (_roomManager == null)
                _roomManager = FindObjectOfType<RoomManager>();
            if (_roomManager == null)
            {
                var go = new GameObject("RoomManager");
                _roomManager = go.AddComponent<RoomManager>();
            }
            _roomManager.Initialize(_networkManager);

            // 4. Create default room for MVP
            _roomManager.CreateRoom("room_default", "默认房间", _runtimeMapIdOverride);

            // 4b. Load map data
            var loadResult = new MapLoader().LoadFromMapId(_roomManager.CurrentRoomConfig.map_id);
            if (loadResult.ok)
            {
                _roomManager.SetMapData(loadResult.mapData);
                Debug.Log("[ServerApp] Room map loaded. path=" + loadResult.path
                    + ", " + BuildMapSummary(loadResult.map));
            }
            else
            {
                Debug.LogError($"[ServerApp] Map load failed: {loadResult.error}");
            }

            // 5. Start Netick server
            _networkManager.StartAsServer();

            IsRunning = true;
            Debug.Log("[ServerApp] Server booted successfully");
        }

        public bool ChangeMap(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
            {
                Debug.LogError("[ServerApp] Cannot change map: missing map id.");
                return false;
            }

            if (_roomManager == null)
                _roomManager = FindObjectOfType<RoomManager>();
            if (_roomManager == null)
            {
                Debug.LogError("[ServerApp] Cannot change map: missing RoomManager.");
                return false;
            }

            var loadResult = new MapLoader().LoadFromMapId(mapId);
            if (!loadResult.ok)
            {
                Debug.LogError("[ServerApp] Map change failed: " + loadResult.error);
                return false;
            }

            _runtimeMapIdOverride = mapId;
            _roomManager.ChangeMap(mapId, loadResult.mapData);
            _mapRevision++;
            Debug.Log("[ServerApp] Room map changed. path=" + loadResult.path
                + ", revision=" + _mapRevision
                + ", " + BuildMapSummary(loadResult.map));

            bool networkRuntimeReady = _networkRuntimeInitialized
                && _networkManager != null
                && _networkManager.IsServer
                && _networkManager.Sandbox != null
                && _networkManager.IsSceneLoaded;

            NetworkMatchState matchState = null;
            if (networkRuntimeReady)
            {
                var treasureAuthority = FindObjectOfType<ServerTreasureAuthority>();
                if (treasureAuthority != null)
                {
                    treasureAuthority.ClearTreasures();
                    treasureAuthority.SpawnTreasures(_networkManager, _roomManager);
                }

                matchState = FindObjectOfType<NetworkMatchState>();
            }

            if (matchState != null)
            {
                matchState.SetMap(RuntimeMapCatalog.GetIndex(mapId), _mapRevision);
                matchState.Sync(
                    _roomManager.CurrentRoomState,
                    _roomManager.RedScore,
                    _roomManager.BlueScore,
                    _roomManager.RemainingTime);
            }
            else
            {
                Debug.Log("[ServerApp] Map changed before network runtime synchronization. "
                    + "NetworkMatchState will be initialized by the Netick scene-loaded event.");
            }

            Debug.Log("[ServerApp] Map changed to " + mapId + ", revision=" + _mapRevision);
            return true;
        }

        public void OnNetworkReady()
        {
            Debug.Log("[ServerApp] OnNetworkReady. IsServer="
                + (_networkManager != null && _networkManager.IsServer)
                + ", HasSandbox=" + (_networkManager != null && _networkManager.Sandbox != null)
                + ", IsSceneLoaded=" + (_networkManager != null && _networkManager.IsSceneLoaded));
            TryInitializeNetworkRuntime(true);
        }

        private bool TryInitializeNetworkRuntime(bool logIfBlocked)
        {
            if (_networkRuntimeInitialized)
                return true;

            if (_networkManager == null)
            {
                _networkManager = NetworkManager.Instance != null
                    ? NetworkManager.Instance
                    : FindObjectOfType<NetworkManager>();
            }

            if (_roomManager == null)
                _roomManager = FindObjectOfType<RoomManager>();

            if (_networkManager == null)
            {
                if (logIfBlocked)
                    Debug.LogWarning("[ServerApp] Network runtime not ready: missing NetworkManager.");
                return false;
            }

            if (!_networkManager.IsServer || _networkManager.Sandbox == null)
            {
                if (logIfBlocked)
                    Debug.LogWarning("[ServerApp] Network runtime not ready: sandbox not ready.");
                return false;
            }

            if (!_networkManager.IsSceneLoaded)
            {
                if (logIfBlocked)
                    Debug.LogWarning("[ServerApp] Network runtime not ready: network scene not loaded.");
                return false;
            }

            if (_roomManager == null || _roomManager.CurrentRoomConfig == null)
            {
                if (logIfBlocked)
                    Debug.LogWarning("[ServerApp] Network runtime not ready: room config not ready.");
                return false;
            }

            SpawnMatchState();

            var treasureAuthority = FindObjectOfType<ServerTreasureAuthority>();
            if (treasureAuthority != null)
                treasureAuthority.SpawnTreasures(_networkManager, _roomManager);
            else
                Debug.LogWarning("[ServerApp] ServerTreasureAuthority not found; treasures were not spawned.");

            _networkRuntimeInitialized = true;
            Debug.Log("[ServerApp] Network runtime initialized. Map="
                + _roomManager.CurrentRoomConfig.map_id
                + ", revision=" + Mathf.Max(1, _mapRevision));
            return true;
        }

        public void ShutdownServer()
        {
            if (!IsRunning) return;

            Debug.Log("[ServerApp] Shutting down server...");

            if (_roomManager != null)
                _roomManager.Shutdown();
            if (_networkManager != null)
                _networkManager.Shutdown();

            IsRunning = false;
            _networkRuntimeInitialized = false;
            Debug.Log("[ServerApp] Server shut down");
        }

        public void ConfigureRuntimeMap(string mapId)
        {
            _runtimeMapIdOverride = mapId;
        }

        public void SetAutoStart(bool autoStart)
        {
            _autoStart = autoStart;
        }

        private void SpawnMatchState()
        {
            if (FindObjectOfType<NetworkMatchState>() != null)
                return;

            if (_matchStatePrefab == null)
                _matchStatePrefab = Resources.Load<GameObject>("NetworkMatchState");

            if (_matchStatePrefab == null)
            {
                Debug.LogError("[ServerApp] NetworkMatchState prefab missing in Resources.");
                return;
            }

            var stateObj = _networkManager.Sandbox.NetworkInstantiate(
                _matchStatePrefab,
                Vector3.zero,
                Quaternion.identity);

            var matchState = stateObj.GetComponent<NetworkMatchState>();
            if (matchState != null)
            {
                if (_mapRevision <= 0)
                    _mapRevision = 1;

                int mapIndex = RuntimeMapCatalog.GetIndex(_roomManager.CurrentRoomConfig.map_id);
                matchState.Initialize(
                    _roomManager.CurrentRoomState,
                    _roomManager.RedScore,
                    _roomManager.BlueScore,
                    _roomManager.CurrentRoomConfig.match_time,
                    mapIndex,
                    _mapRevision);
                Debug.Log("[ServerApp] NetworkMatchState ready. Map="
                    + _roomManager.CurrentRoomConfig.map_id
                    + ", index=" + mapIndex
                    + ", revision=" + _mapRevision
                    + ", catalog=[" + string.Join(",", RuntimeMapCatalog.GetMapIds()) + "]");
            }

            Debug.Log("[ServerApp] NetworkMatchState spawned.");
        }

        private static string BuildMapSummary(MapJsonModels.MapJson map)
        {
            if (map == null)
                return "map=null";

            int objectCount = map.objects != null ? map.objects.Count : 0;
            int baseCount = map.team_bases != null ? map.team_bases.Count : 0;
            int treasureCount = map.treasure_spawn_points != null ? map.treasure_spawn_points.Count : 0;
            int supplyCount = map.supply_boxes != null ? map.supply_boxes.Count : 0;
            int boundaryCount = map.map_boundary != null && map.map_boundary.points != null
                ? map.map_boundary.points.Count
                : 0;

            return "mapId=" + map.map_id
                + ", objects=" + objectCount
                + ", teamBases=" + baseCount
                + ", treasurePoints=" + treasureCount
                + ", supplyBoxes=" + supplyCount
                + ", boundaryPoints=" + boundaryCount;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ShutdownServer();
                Instance = null;
            }
        }
    }
}
