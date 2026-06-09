using TreasureArenaMR.Core;
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

            // TODO: Load map data via MapLoader when ready
            // _roomManager.SetMapData(new MapLoader().LoadFromMapId(mapId).mapData);

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
