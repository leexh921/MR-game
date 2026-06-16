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

        [Header("Match State Sync")]
        [SerializeField] private GameObject _networkMatchStatePrefab;

        private RoomManager _roomManager;
        private NetworkManager _networkManager;
        private NetworkMatchState _networkMatchStateInstance;

        private void CacheReferences()
        {
            if (_roomManager == null)
                _roomManager = FindObjectOfType<RoomManager>();
            if (_networkManager == null)
                _networkManager = FindObjectOfType<NetworkManager>();
        }

        public override void OnStartup(NetworkSandbox sandbox)
        {
            Debug.Log($"[SandboxNetworkListener] Netick sandbox started. IsServer={sandbox.IsServer}, IsClient={sandbox.IsClient}");
            CacheReferences();

            if (_networkManager != null)
                _networkManager.OnSandboxStarted(sandbox);

            if (sandbox.IsServer && _networkMatchStatePrefab != null)
            {
                var obj = sandbox.NetworkInstantiate(_networkMatchStatePrefab, Vector3.zero, Quaternion.identity);
                _networkMatchStateInstance = obj.GetComponent<NetworkMatchState>();
                Debug.Log($"[SandboxNetworkListener] NetworkMatchState spawned: {_networkMatchStateInstance != null}");
            }
        }

        public override void OnShutdown(NetworkSandbox sandbox)
        {
            Debug.Log("[SandboxNetworkListener] Netick sandbox shutting down");

            if (_networkManager != null)
                _networkManager.OnSandboxShutdown();
        }

        public override void OnPlayerConnected(NetworkSandbox sandbox, NetickPlayer networkPlayer)
        {
            // Only server handles player connection setup
            if (!sandbox.IsServer) return;

            CacheReferences();
            string playerId = networkPlayer.PlayerId.ToString();

            Debug.Log($"[SandboxNetworkListener] Player connected: {playerId}");

            if (_roomManager != null)
            {
                _roomManager.AddNetworkPlayer(playerId, $"Player_{playerId}", networkPlayer);
            }

            // Spawn player at team spawn position
            SpawnPlayer(sandbox, playerId, networkPlayer);

            if (_networkManager != null)
                _networkManager.RegisterPlayer(playerId);
        }

        private void SpawnPlayer(NetworkSandbox sandbox, string playerId, NetickPlayer netPlayer)
        {
            if (_roomManager?.MapData == null)
            {
                Debug.LogWarning($"[SandboxNetworkListener] SpawnPlayer blocked: MapData is null (roomManager={_roomManager != null})");
                return;
            }

            var playerInfo = _roomManager.Players.Find(p => p.player_id == playerId);
            if (playerInfo == null)
            {
                Debug.LogWarning($"[SandboxNetworkListener] SpawnPlayer blocked: playerInfo not found for {playerId}. Players count={_roomManager.Players.Count}");
                return;
            }

            var teamType = playerInfo.team;
            if (teamType == TeamType.None)
            {
                Debug.LogWarning($"[SandboxNetworkListener] SpawnPlayer blocked: team is None for {playerId}");
                return;
            }

            var spawnZones = _roomManager.MapData.GetSpawnZones(teamType);
            if (spawnZones == null || spawnZones.Count == 0)
            {
                Debug.LogWarning($"[SandboxNetworkListener] SpawnPlayer blocked: no spawn zones for team {teamType}");
                return;
            }

            Vector3 spawnPos = spawnZones[0]; // First available spawn point

            Debug.Log($"[SandboxNetworkListener] Spawning player {playerId} ({teamType}) at {spawnPos}");

            if (_playerPrefab == null)
            {
                Debug.LogError("[SandboxNetworkListener] SpawnPlayer blocked: _playerPrefab is not assigned in inspector!");
                return;
            }

            var playerObj = sandbox.NetworkInstantiate(_playerPrefab,
                spawnPos, Quaternion.identity, netPlayer);
            sandbox.SetPlayerObject(netPlayer.PlayerId, playerObj);

            var netComp = playerObj.GetComponent<NetworkPlayer>();
            if (netComp != null)
            {
                netComp.Initialize(playerId, $"Player_{playerId}", teamType);
                netComp.NetickPlayerId = netPlayer.PlayerId;
            }
        }

        public override void OnPlayerDisconnected(
            NetworkSandbox sandbox, NetickPlayer networkPlayer, TransportDisconnectReason reason)
        {
            if (!sandbox.IsServer) return;

            CacheReferences();
            string playerId = networkPlayer.PlayerId.ToString();
            Debug.Log($"[SandboxNetworkListener] Player disconnected: {playerId}, reason={reason}");

            if (sandbox.TryGetPlayerObject(networkPlayer.PlayerId, out var playerObj))
            {
                sandbox.Destroy(playerObj);
                Debug.Log($"[SandboxNetworkListener] Despawned player object for {playerId}");
            }
            else
            {
                Debug.LogWarning($"[SandboxNetworkListener] No player object found to despawn for {playerId}");
            }

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

            if (_networkManager != null)
                _networkManager.OnConnectedToServer();
        }

        public override void OnDisconnectedFromServer(
            NetworkSandbox sandbox, NetworkConnection connection, TransportDisconnectReason reason)
        {
            Debug.Log($"[SandboxNetworkListener] Disconnected from server: {reason}");

            if (_networkManager != null)
                _networkManager.OnDisconnectedFromServer(reason);
        }

        public override void OnSceneLoaded(NetworkSandbox sandbox)
        {
            Debug.Log($"[SandboxNetworkListener] Scene loaded: {sandbox.Scene}");

            if (_networkManager != null)
                _networkManager.OnSceneLoaded();
        }
    }
}
