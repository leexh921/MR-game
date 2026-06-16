using Netick;
using Netick.Unity;
using TreasureArenaMR.Server;
using TreasureArenaMR.Shared;
using UnityEngine;
using NetickPlayer = Netick.NetworkPlayer;

namespace TreasureArenaMR.Network
{
    /// <summary>
    /// Bridges Netick connection events into the game's RoomManager and NetworkManager.
    /// Attached to the Sandbox prefab. Events fire on all peers (server + clients).
    /// </summary>
    public sealed class SandboxNetworkListener : NetworkEventsListener
    {
        [Header("Player Spawn")]
        [SerializeField] private GameObject _playerPrefab;

        private RoomManager _roomManager;
        private NetworkManager _networkManager;

        private void CacheReferences()
        {
            if (_roomManager == null)
                _roomManager = FindObjectOfType<RoomManager>();

            if (_networkManager == null)
                _networkManager = NetworkManager.Instance != null
                    ? NetworkManager.Instance
                    : FindObjectOfType<NetworkManager>();
        }

        public override void OnStartup(NetworkSandbox sandbox)
        {
            Debug.Log($"[SandboxNetworkListener] Netick sandbox started. IsServer={sandbox.IsServer}, IsClient={sandbox.IsClient}");
            CacheReferences();

            if (_networkManager != null)
                _networkManager.OnSandboxStarted(sandbox);

            // 注意：
            // 这里不要调用 ServerApp.OnNetworkReady()。
            // OnStartup 发生时，Netick Sandbox 已经启动，但 Network Scene 还不一定加载完成。
            // 如果这里提前初始化 ServerApp，就会出现：
            // [ServerApp] Network runtime not ready: network scene not loaded.
            //
            // ServerApp.OnNetworkReady() 应该放在 OnSceneLoaded() 里调用。
        }

        public override void OnShutdown(NetworkSandbox sandbox)
        {
            Debug.Log("[SandboxNetworkListener] Netick sandbox shutting down");
            CacheReferences();

            if (_networkManager != null)
                _networkManager.OnSandboxShutdown();
        }

        public override void OnPlayerConnected(NetworkSandbox sandbox, NetickPlayer networkPlayer)
        {
            // Only server handles player connection setup.
            if (!sandbox.IsServer)
                return;

            CacheReferences();

            string playerId = networkPlayer.PlayerId.ToString();

            Debug.Log($"[SandboxNetworkListener] Player connected: {playerId}");

            if (_roomManager != null)
            {
                _roomManager.AddNetworkPlayer(playerId, $"Player_{playerId}", networkPlayer);
            }

            SpawnPlayer(sandbox, playerId, networkPlayer);

            if (_networkManager != null)
                _networkManager.RegisterPlayer(playerId);
        }

        private void SpawnPlayer(NetworkSandbox sandbox, string playerId, NetickPlayer netPlayer)
        {
            if (_roomManager?.MapData == null)
                return;

            var playerInfo = _roomManager.Players.Find(p => p.player_id == playerId);
            if (playerInfo == null)
                return;

            var teamType = playerInfo.team;
            if (teamType == TeamType.None)
                return;

            var spawnZones = _roomManager.MapData.GetSpawnZones(teamType);
            if (spawnZones == null || spawnZones.Count == 0)
                return;

            Vector3 spawnPos = spawnZones[0];

            Debug.Log($"[SandboxNetworkListener] Spawning player {playerId} ({teamType}) at {spawnPos}");

            if (_playerPrefab == null)
            {
                Debug.LogWarning("[SandboxNetworkListener] Player prefab is missing; cannot spawn network player.");
                return;
            }

            var playerObj = sandbox.NetworkInstantiate(
                _playerPrefab,
                spawnPos,
                Quaternion.identity,
                netPlayer);

            sandbox.SetPlayerObject(netPlayer.PlayerId, playerObj);

            var netComp = playerObj.GetComponent<NetworkPlayer>();
            if (netComp != null)
            {
                int maxHp = _roomManager.CurrentRoomConfig != null
                    ? _roomManager.CurrentRoomConfig.player_max_hp
                    : 100;

                netComp.Initialize(playerId, $"Player_{playerId}", teamType, maxHp);
                netComp.NetickPlayerId = netPlayer.PlayerId;
            }
            else
            {
                Debug.LogWarning("[SandboxNetworkListener] Spawned player object does not have TreasureArenaMR.Network.NetworkPlayer component.");
            }
        }

        public override void OnPlayerDisconnected(
            NetworkSandbox sandbox,
            NetickPlayer networkPlayer,
            TransportDisconnectReason reason)
        {
            if (!sandbox.IsServer)
                return;

            CacheReferences();

            string playerId = networkPlayer.PlayerId.ToString();

            Debug.Log($"[SandboxNetworkListener] Player disconnected: {playerId}, reason={reason}");

            if (_roomManager != null)
                _roomManager.RemoveNetworkPlayer(playerId);

            if (_networkManager != null)
                _networkManager.UnregisterPlayer(playerId);
        }

        public override void OnConnectRequest(NetworkSandbox sandbox, NetworkConnectionRequest request)
        {
            CacheReferences();

            if (_roomManager != null && _roomManager.IsFull)
            {
                Debug.Log("[SandboxNetworkListener] Connection refused: room full");
                request.Refuse();
                return;
            }

            if (_roomManager != null && !_roomManager.CanJoin)
            {
                Debug.Log($"[SandboxNetworkListener] Connection refused: room state={_roomManager.CurrentRoomState}");
                request.Refuse();
                return;
            }
        }

        public override void OnConnectedToServer(NetworkSandbox sandbox, NetworkConnection connection)
        {
            Debug.Log("[SandboxNetworkListener] Connected to server");
            CacheReferences();

            if (_networkManager != null)
                _networkManager.OnConnectedToServer();
        }

        public override void OnDisconnectedFromServer(
            NetworkSandbox sandbox,
            NetworkConnection connection,
            TransportDisconnectReason reason)
        {
            Debug.Log($"[SandboxNetworkListener] Disconnected from server: {reason}");
            CacheReferences();

            if (_networkManager != null)
                _networkManager.OnDisconnectedFromServer(reason);
        }

        public override void OnSceneLoaded(NetworkSandbox sandbox)
        {
            Debug.Log($"[SandboxNetworkListener] Scene loaded: {sandbox.Scene}");
            CacheReferences();

            if (_networkManager != null)
                _networkManager.OnSceneLoaded();

            // 只有等 Netick 的 Network Scene 加载完成后，才通知 ServerApp 初始化网络运行时。
            // 这样 ServerApp.TryInitializeNetworkRuntime() 里检查 IsSceneLoaded 时才会是 true。
            if (sandbox.IsServer)
            {
                var serverApp = ServerApp.Instance != null
                    ? ServerApp.Instance
                    : FindObjectOfType<ServerApp>();

                if (serverApp != null)
                    serverApp.OnNetworkReady();
            }
        }
    }
}