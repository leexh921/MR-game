using TreasureArenaMR.Core;
using TreasureArenaMR.Map;
using TreasureArenaMR.Network;
using UnityEngine;

namespace TreasureArenaMR.Server
{
    /// <summary>
    /// Server runtime role entry point.
    /// Bootstraps the project TCP/UDP server and server-side services.
    /// No database persistence in MVP.
    /// </summary>
    public sealed class ServerApp : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private SimpleNetworkServer _simpleNetworkServer;
        [SerializeField] private RoomManager _roomManager;
        [SerializeField] private ServerTreasureAuthority _treasureAuthority;
        [SerializeField] private ServerCombatAuthority _combatAuthority;
        [SerializeField] private bool _autoStart = false;
        [SerializeField] private string _runtimeMapId = "test_map_01";
        [SerializeField] private int _tcpPort = SimpleNetworkProtocol.TcpPort;
        [SerializeField] private int _udpPort = SimpleNetworkProtocol.UdpPort;

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

        public void ConfigurePorts(int tcpPort, int udpPort)
        {
            if (tcpPort > 0)
                _tcpPort = tcpPort;
            if (udpPort > 0)
                _udpPort = udpPort;
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
            _roomManager.Initialize(null);

            // 4. Create default room for MVP
            _roomManager.CreateRoom("room_default", "默认房间", _runtimeMapId);

            // 4b. Load map data
            LoadRuntimeMapData();

            _simpleNetworkServer.StartServer(_roomManager, _treasureAuthority, _combatAuthority, _tcpPort, _udpPort);

            IsRunning = true;
            Debug.Log("[ServerApp] Server booted successfully");
        }

        public void ShutdownServer()
        {
            if (!IsRunning) return;

            Debug.Log("[ServerApp] Shutting down server...");

            if (_roomManager != null)
                _roomManager.Shutdown();
            if (_simpleNetworkServer != null)
                _simpleNetworkServer.StopServer();

            IsRunning = false;
            Debug.Log("[ServerApp] Server shut down");
        }

        private void EnsureRuntimeReferences()
        {
            if (_simpleNetworkServer == null)
                _simpleNetworkServer = FindObjectOfType<SimpleNetworkServer>();
            if (_simpleNetworkServer == null)
                _simpleNetworkServer = new GameObject("SimpleNetworkServer").AddComponent<SimpleNetworkServer>();

            if (_roomManager == null)
                _roomManager = FindObjectOfType<RoomManager>();
            if (_roomManager == null)
            {
                var go = new GameObject("RoomManager");
                _roomManager = go.AddComponent<RoomManager>();
            }

            if (_treasureAuthority == null)
                _treasureAuthority = FindObjectOfType<ServerTreasureAuthority>();
            if (_treasureAuthority == null)
                _treasureAuthority = new GameObject("ServerTreasureAuthority").AddComponent<ServerTreasureAuthority>();

            if (_combatAuthority == null)
                _combatAuthority = FindObjectOfType<ServerCombatAuthority>();
            if (_combatAuthority == null)
                _combatAuthority = new GameObject("ServerCombatAuthority").AddComponent<ServerCombatAuthority>();
        }

        private bool LoadRuntimeMapData()
        {
            var loadResult = new MapLoader().LoadFromMapId(_runtimeMapId);
            if (loadResult.ok)
            {
                _roomManager.SetMapData(loadResult.mapData);
                if (_simpleNetworkServer != null)
                    _simpleNetworkServer.MarkMapRevisionChanged();
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
