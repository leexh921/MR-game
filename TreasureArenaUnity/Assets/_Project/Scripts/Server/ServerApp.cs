using TreasureArenaMR.Core;
using TreasureArenaMR.Map;
using TreasureArenaMR.Network;
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
        [SerializeField] private bool _autoStart = false;
        [SerializeField] private string _runtimeMapId = "test_map_01";

        public static ServerApp Instance { get; private set; }
        public bool IsRunning { get; private set; }

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
            {
                StartServer();
            }
        }

        public void SetAutoStart(bool autoStart)
        {
            _autoStart = autoStart;
        }

        public void ConfigureRuntimeMap(string mapId)
        {
            if (!string.IsNullOrEmpty(mapId))
            {
                _runtimeMapId = mapId;
            }
        }

        public bool ChangeMap(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
            {
                return false;
            }

            ConfigureRuntimeMap(mapId);
            EnsureRuntimeReferences();

            if (_roomManager.CurrentRoomConfig != null)
            {
                _roomManager.CurrentRoomConfig.map_id = _runtimeMapId;
            }

            return LoadRuntimeMapData();
        }

        public void StartServer()
        {
            if (IsRunning)
            {
                Debug.LogWarning("[ServerApp] Server already running");
                return;
            }

            Debug.Log("[ServerApp] Booting server...");

            EnsureRuntimeReferences();
            _roomManager.Initialize(_networkManager);

            // 4. Create default room for MVP
            _roomManager.CreateRoom("room_default", "默认房间", _runtimeMapId);

            // 4b. Load map data
            LoadRuntimeMapData();

            // 5. Start Netick server
            _networkManager.StartAsServer();

            IsRunning = true;
            Debug.Log("[ServerApp] Server booted successfully");
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
            Debug.Log("[ServerApp] Server shut down");
        }

        private void EnsureRuntimeReferences()
        {
            if (_networkManager == null)
                _networkManager = FindObjectOfType<NetworkManager>();
            if (_networkManager == null)
            {
                var go = new GameObject("NetworkManager");
                _networkManager = go.AddComponent<NetworkManager>();
            }

            if (_roomManager == null)
                _roomManager = FindObjectOfType<RoomManager>();
            if (_roomManager == null)
            {
                var go = new GameObject("RoomManager");
                _roomManager = go.AddComponent<RoomManager>();
            }
        }

        private bool LoadRuntimeMapData()
        {
            var loadResult = new MapLoader().LoadFromMapId(_runtimeMapId);
            if (loadResult.ok)
            {
                _roomManager.SetMapData(loadResult.mapData);
                return true;
            }

            Debug.LogError($"[ServerApp] Map load failed: {loadResult.error}");
            return false;
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
