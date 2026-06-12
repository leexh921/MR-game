using System.Collections;
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
                _roomManager.SetMapData(loadResult.mapData);
            else
                Debug.LogError($"[ServerApp] Map load failed: {loadResult.error}");

            // 5. Start Netick server
            _networkManager.StartAsServer();

            IsRunning = true;
            Debug.Log("[ServerApp] Server booted successfully");
            StartCoroutine(InitializeNetworkRuntimeWhenReady());
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

            var treasureAuthority = FindObjectOfType<ServerTreasureAuthority>();
            if (treasureAuthority != null)
            {
                treasureAuthority.ClearTreasures();
                if (_networkManager != null && _networkManager.Sandbox != null && _networkManager.IsServer)
                    treasureAuthority.SpawnTreasures(_networkManager, _roomManager);
            }

            var matchState = FindObjectOfType<NetworkMatchState>();
            if (matchState != null)
            {
                matchState.SetMap(RuntimeMapCatalog.GetIndex(mapId), _mapRevision);
                matchState.Sync(
                    _roomManager.CurrentRoomState,
                    _roomManager.RedScore,
                    _roomManager.BlueScore,
                    _roomManager.RemainingTime);
            }

            Debug.Log("[ServerApp] Map changed to " + mapId + ", revision=" + _mapRevision);
            return true;
        }

        public void OnNetworkReady()
        {
            TryInitializeNetworkRuntime(false);
        }

        private IEnumerator InitializeNetworkRuntimeWhenReady()
        {
            const int maxAttempts = 120;
            for (int i = 0; i < maxAttempts; i++)
            {
                if (_networkRuntimeInitialized)
                    yield break;

                if (TryInitializeNetworkRuntime(i == 0 || i == maxAttempts - 1))
                    yield break;

                yield return null;
            }
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

            if (_networkManager.Sandbox == null)
            {
                var sandbox = FindObjectOfType<NetworkSandbox>();
                if (sandbox != null)
                    _networkManager.OnSandboxStarted(sandbox);
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
            }

            Debug.Log("[ServerApp] NetworkMatchState spawned.");
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
