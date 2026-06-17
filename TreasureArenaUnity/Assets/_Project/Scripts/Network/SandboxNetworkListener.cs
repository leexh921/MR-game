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
        private const string LogPrefix = "[NetickSpawnSync]";

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
            Debug.Log($"{LogPrefix} Sandbox started isServer={sandbox.IsServer} isClient={sandbox.IsClient} " +
                $"playerPrefab={(_playerPrefab != null ? _playerPrefab.name : "null")} " +
                $"networkMatchStatePrefab={(_networkMatchStatePrefab != null ? _networkMatchStatePrefab.name : "null")}");
            CacheReferences();

            if (_networkManager != null)
                _networkManager.OnSandboxStarted(sandbox);
            else
                Debug.LogWarning($"{LogPrefix} NetworkManager missing on startup.");

            if (sandbox.IsServer && _networkMatchStatePrefab != null)
            {
                var obj = sandbox.NetworkInstantiate(_networkMatchStatePrefab, Vector3.zero, Quaternion.identity);
                _networkMatchStateInstance = obj.GetComponent<NetworkMatchState>();
                Debug.Log($"{LogPrefix} NetworkMatchState spawn requested " +
                    $"object={(obj != null ? obj.name : "null")} component={_networkMatchStateInstance != null}");
            }
            else if (sandbox.IsServer)
            {
                Debug.LogError($"{LogPrefix} NetworkMatchState spawn blocked: prefab is null.");
            }
        }

        public override void OnShutdown(NetworkSandbox sandbox)
        {
            Debug.Log($"{LogPrefix} Sandbox shutting down isServer={sandbox.IsServer} isClient={sandbox.IsClient}");

            if (_networkManager != null)
                _networkManager.OnSandboxShutdown();
        }

        public override void OnPlayerConnected(NetworkSandbox sandbox, NetickPlayer networkPlayer)
        {
            // Only server handles player connection setup
            if (!sandbox.IsServer) return;

            CacheReferences();
            string playerId = networkPlayer.PlayerId.ToString();

            Debug.Log($"{LogPrefix} Player connected playerId={playerId} " +
                $"roomManager={_roomManager != null} mapData={(_roomManager != null && _roomManager.MapData != null)}");

            if (_roomManager != null)
            {
                _roomManager.AddNetworkPlayer(playerId, $"Player_{playerId}", networkPlayer);
            }
            else
            {
                Debug.LogError($"{LogPrefix} Player add blocked: RoomManager missing.");
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
                Debug.LogWarning($"{LogPrefix} SpawnPlayer blocked: MapData is null " +
                    $"roomManager={_roomManager != null} playerId={playerId}");
                return;
            }

            var playerInfo = _roomManager.Players.Find(p => p.player_id == playerId);
            if (playerInfo == null)
            {
                Debug.LogWarning($"{LogPrefix} SpawnPlayer blocked: playerInfo not found " +
                    $"playerId={playerId} players={_roomManager.Players.Count}");
                return;
            }

            var teamType = playerInfo.team;
            if (teamType == TeamType.None)
            {
                Debug.LogWarning($"{LogPrefix} SpawnPlayer blocked: team is None playerId={playerId}");
                return;
            }

            var spawnZones = _roomManager.MapData.GetSpawnZones(teamType);
            if (spawnZones == null || spawnZones.Count == 0)
            {
                Debug.LogWarning($"{LogPrefix} SpawnPlayer blocked: no spawn zones " +
                    $"playerId={playerId} team={teamType}");
                return;
            }

            Vector3 spawnPos = spawnZones[0]; // First available spawn point

            Debug.Log($"{LogPrefix} Spawning player playerId={playerId} team={teamType} " +
                $"spawnPos={spawnPos} prefab={(_playerPrefab != null ? _playerPrefab.name : "null")}");

            if (_playerPrefab == null)
            {
                Debug.LogError($"{LogPrefix} SpawnPlayer blocked: _playerPrefab is not assigned.");
                return;
            }

            var playerObj = sandbox.NetworkInstantiate(_playerPrefab,
                spawnPos, Quaternion.identity, netPlayer);
            sandbox.SetPlayerObject(netPlayer.PlayerId, playerObj);

            Debug.Log($"{LogPrefix} Player NetworkInstantiate done playerId={playerId} " +
                $"object={(playerObj != null ? playerObj.name : "null")}");

            if (playerObj == null)
            {
                Debug.LogError($"{LogPrefix} SpawnPlayer failed: NetworkInstantiate returned null " +
                    $"playerId={playerId} prefab={_playerPrefab.name}");
                return;
            }

            var netComp = playerObj.GetComponent<NetworkPlayer>();
            if (netComp != null)
            {
                netComp.Initialize(playerId, $"Player_{playerId}", teamType);
                netComp.NetickPlayerId = netPlayer.PlayerId;
                Debug.Log($"{LogPrefix} NetworkPlayer initialized playerId={playerId} " +
                    $"netickPlayerId={netPlayer.PlayerId}");
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} Spawned player missing NetworkPlayer component " +
                    $"playerId={playerId} object={(playerObj != null ? playerObj.name : "null")}");
            }
        }

        public override void OnPlayerDisconnected(
            NetworkSandbox sandbox, NetickPlayer networkPlayer, TransportDisconnectReason reason)
        {
            if (!sandbox.IsServer) return;

            CacheReferences();
            string playerId = networkPlayer.PlayerId.ToString();
            Debug.Log($"{LogPrefix} Player disconnected playerId={playerId} reason={reason}");

            if (sandbox.TryGetPlayerObject(networkPlayer.PlayerId, out var playerObj))
            {
                sandbox.Destroy(playerObj);
                Debug.Log($"{LogPrefix} Despawned player object playerId={playerId}");
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} No player object found to despawn playerId={playerId}");
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
                Debug.Log($"{LogPrefix} Connection refused: room full");
                request.Refuse();
                return;
            }

            if (_roomManager != null && !_roomManager.CanJoin)
            {
                Debug.Log($"{LogPrefix} Connection refused: room state={_roomManager.CurrentRoomState}");
                request.Refuse();
                return;
            }
        }

        public override void OnConnectedToServer(NetworkSandbox sandbox, NetworkConnection connection)
        {
            CacheReferences();
            Debug.Log($"{LogPrefix} Client OnConnectedToServer endpoint={sandbox.ServerEndPoint} " +
                $"networkManager={_networkManager != null}");

            if (_networkManager != null)
                _networkManager.OnConnectedToServer();
        }

        public override void OnDisconnectedFromServer(
            NetworkSandbox sandbox, NetworkConnection connection, TransportDisconnectReason reason)
        {
            CacheReferences();
            Debug.Log($"{LogPrefix} Client disconnected reason={reason}");

            if (_networkManager != null)
                _networkManager.OnDisconnectedFromServer(reason);
        }

        public override void OnSceneLoaded(NetworkSandbox sandbox)
        {
            Debug.Log($"{LogPrefix} Scene loaded scene={sandbox.Scene} " +
                $"isServer={sandbox.IsServer} isClient={sandbox.IsClient}");

            if (_networkManager != null)
                _networkManager.OnSceneLoaded();
        }
    }
}
