using System;
using System.Collections.Generic;
using TreasureArenaMR.Map;
using TreasureArenaMR.Network;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Server
{
    /// <summary>
    /// PC Home scene entry point for the combined Manager + Netick Server MVP.
    /// </summary>
    public sealed class ServerBootstrap : MonoBehaviour
    {
        [Header("Server")]
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private ServerApp _serverApp;
        [SerializeField] private RoomManager _roomManager;
        [SerializeField] private ServerTreasureAuthority _treasureAuthority;
        [SerializeField] private ServerCombatAuthority _combatAuthority;
        [SerializeField] private RoomStateSynchronizer _roomStateSynchronizer;
        [SerializeField] private int _serverPort = 7777;
        [SerializeField] private string _defaultMapId = "pico_map";
        [SerializeField] private bool _startServerOnAwake;

        public event Action OnStatusChanged;

        public NetworkManager NetworkManager => _networkManager;
        public ServerApp ServerApp => _serverApp;
        public RoomManager RoomManager => _roomManager;
        public string SelectedMapId { get; private set; }
        public bool IsServerRunning => _serverApp != null && _serverApp.IsRunning;

        private void Awake()
        {
            EnsureReferences();
            SelectedMapId = string.IsNullOrEmpty(SelectedMapId) ? _defaultMapId : SelectedMapId;

            if (_serverApp != null)
            {
                _serverApp.SetAutoStart(false);
                _serverApp.ConfigureRuntimeMap(SelectedMapId);
            }

            if (_startServerOnAwake)
                CreateOrStartRoom();
        }

        public IReadOnlyList<string> GetAvailableMapIds()
        {
            return RuntimeMapCatalog.MapIds;
        }

        public void CreateOrStartRoom()
        {
            EnsureReferences();
            if (_networkManager != null)
                _networkManager.ConfigureEndpoint(null, _serverPort);

            if (_serverApp == null)
            {
                Debug.LogError("[ServerBootstrap] Missing ServerApp.");
                return;
            }

            _serverApp.ConfigureRuntimeMap(SelectedMapId);
            if (!_serverApp.IsRunning)
                _serverApp.StartServer();
            else
                _serverApp.ChangeMap(SelectedMapId);

            OnStatusChanged?.Invoke();
        }

        public void SelectNextMap()
        {
            IReadOnlyList<string> mapIds = GetAvailableMapIds();
            if (mapIds == null || mapIds.Count == 0)
                return;

            int currentIndex = RuntimeMapCatalog.GetIndex(SelectedMapId);
            int nextIndex = (currentIndex + 1) % mapIds.Count;
            SelectMap(mapIds[nextIndex]);
        }

        public bool SelectMap(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
                return false;

            SelectedMapId = mapId;
            EnsureReferences();

            bool changed = true;
            if (_serverApp != null && _serverApp.IsRunning)
                changed = _serverApp.ChangeMap(mapId);
            else if (_serverApp != null)
                _serverApp.ConfigureRuntimeMap(mapId);

            OnStatusChanged?.Invoke();
            return changed;
        }

        public bool StartMatch(int minPlayersToStart)
        {
            EnsureReferences();
            if (_networkManager == null || !_networkManager.IsServer || _roomManager == null)
            {
                Debug.LogWarning("[ServerBootstrap] Cannot start match before server is running.");
                return false;
            }

            if (_roomManager.CurrentRoomState != RoomState.Waiting)
            {
                Debug.LogWarning("[ServerBootstrap] Cannot start match from state: " + _roomManager.CurrentRoomState);
                return false;
            }

            if (_roomManager.MapData == null)
            {
                Debug.LogWarning("[ServerBootstrap] Cannot start match before map is loaded.");
                return false;
            }

            int playerCount = _roomManager.Players != null ? _roomManager.Players.Count : 0;
            if (playerCount < minPlayersToStart)
            {
                Debug.LogWarning("[ServerBootstrap] Not enough players to start: " + playerCount + "/" + minPlayersToStart);
                return false;
            }

            _roomManager.SetRoomState(RoomState.Playing);
            OnStatusChanged?.Invoke();
            return true;
        }

        public void StopMatch()
        {
            EnsureReferences();
            if (_roomManager == null)
                return;

            _roomManager.SetRoomState(RoomState.Finished);
            OnStatusChanged?.Invoke();
        }

        public bool SwitchTeam(string playerId, TeamType team)
        {
            EnsureReferences();
            bool changed = _roomManager != null && _roomManager.SwitchTeam(playerId, team);
            if (changed)
                OnStatusChanged?.Invoke();
            return changed;
        }

        private void EnsureReferences()
        {
            if (_networkManager == null)
                _networkManager = NetworkManager.Instance != null
                    ? NetworkManager.Instance
                    : FindObjectOfType<NetworkManager>();

            if (_networkManager == null)
                _networkManager = new GameObject("NetworkManager").AddComponent<NetworkManager>();

            if (_roomManager == null)
                _roomManager = FindObjectOfType<RoomManager>();
            if (_roomManager == null)
                _roomManager = new GameObject("RoomManager").AddComponent<RoomManager>();

            if (_serverApp == null)
                _serverApp = ServerApp.Instance != null
                    ? ServerApp.Instance
                    : FindObjectOfType<ServerApp>();
            if (_serverApp == null)
                _serverApp = new GameObject("ServerApp").AddComponent<ServerApp>();

            if (_treasureAuthority == null)
                _treasureAuthority = FindObjectOfType<ServerTreasureAuthority>();
            if (_treasureAuthority == null)
                _treasureAuthority = new GameObject("ServerTreasureAuthority").AddComponent<ServerTreasureAuthority>();

            if (_combatAuthority == null)
                _combatAuthority = FindObjectOfType<ServerCombatAuthority>();
            if (_combatAuthority == null)
                _combatAuthority = new GameObject("ServerCombatAuthority").AddComponent<ServerCombatAuthority>();

            if (_roomStateSynchronizer == null)
                _roomStateSynchronizer = FindObjectOfType<RoomStateSynchronizer>();
            if (_roomStateSynchronizer == null)
                _roomStateSynchronizer = new GameObject("RoomStateSynchronizer").AddComponent<RoomStateSynchronizer>();
        }
    }
}
