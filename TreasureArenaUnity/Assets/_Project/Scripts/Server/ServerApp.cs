using TreasureArenaMR.Core;
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
        [SerializeField] private bool _autoStart = true;

        [Header("Network Prefabs")]
        [SerializeField] private GameObject _matchStatePrefab;

        public static ServerApp Instance { get; private set; }
        public bool IsRunning { get; private set; }

        private bool _networkRuntimeInitialized;

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
            _roomManager.CreateRoom("room_default", "默认房间");

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
        }

        public void OnNetworkReady()
        {
            if (_networkRuntimeInitialized)
                return;
            if (_networkManager == null || !_networkManager.IsServer || _networkManager.Sandbox == null)
                return;
            if (_roomManager == null || _roomManager.CurrentRoomConfig == null)
                return;

            SpawnMatchState();

            var treasureAuthority = FindObjectOfType<ServerTreasureAuthority>();
            if (treasureAuthority != null)
                treasureAuthority.SpawnTreasures(_networkManager, _roomManager);
            else
                Debug.LogWarning("[ServerApp] ServerTreasureAuthority not found; treasures were not spawned.");

            _networkRuntimeInitialized = true;
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
                matchState.Initialize(
                    _roomManager.CurrentRoomState,
                    _roomManager.RedScore,
                    _roomManager.BlueScore,
                    _roomManager.CurrentRoomConfig.match_time);
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
