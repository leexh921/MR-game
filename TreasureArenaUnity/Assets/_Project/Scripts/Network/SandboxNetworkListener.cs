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
                _networkManager = FindObjectOfType<NetworkManager>();
        }

        public override void OnStartup(NetworkSandbox sandbox)
        {
            Debug.Log($"[SandboxNetworkListener] Netick sandbox started. IsServer={sandbox.IsServer}, IsClient={sandbox.IsClient}");
            CacheReferences();

            if (_networkManager != null)
                _networkManager.OnSandboxStarted(sandbox);
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
            if (_roomManager?.MapData == null) return;

            var playerInfo = _roomManager.Players.Find(p => p.player_id == playerId);
            if (playerInfo == null) return;

            var teamType = playerInfo.team == "Red" ? TeamType.Red :
                           playerInfo.team == "Blue" ? TeamType.Blue : TeamType.None;
            if (teamType == TeamType.None) return;

            var spawnZone = _roomManager.MapData.GetFirstSpawn(teamType);
            if (spawnZone == null) return;

            Debug.Log($"[SandboxNetworkListener] Spawning player {playerId} ({teamType}) at {spawnZone.position}");

            if (_playerPrefab != null)
            {
                var playerObj = sandbox.NetworkInstantiate(_playerPrefab,
                    spawnZone.position, Quaternion.identity, netPlayer);
                sandbox.SetPlayerObject(netPlayer.PlayerId, playerObj);

                // Attach NetworkPlayer component data
                var netComp = playerObj.GetComponent<NetworkPlayer>();
                if (netComp != null)
                {
                    netComp.Initialize(playerId, $"Player_{playerId}", teamType);
                    netComp.NetickPlayerId = netPlayer.PlayerId;
                }
            }
        }

        public override void OnPlayerDisconnected(
            NetworkSandbox sandbox, NetickPlayer networkPlayer, TransportDisconnectReason reason)
        {
            if (!sandbox.IsServer) return;

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
