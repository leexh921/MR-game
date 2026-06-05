using System;
using System.Collections.Generic;
using Netick;
using Netick.Unity;
using Netick.Transport;
using TreasureArenaMR.Core;
using UnityEngine;
using NetickNetwork = Netick.Unity.Network;
using NetworkPlayer = TreasureArenaMR.Network.NetworkPlayer;

namespace TreasureArenaMR.Network
{
    /// <summary>
    /// Central networking facade wrapping Netick.NetworkSandbox.
    /// Handles server/client/host startup, player tracking, and network state.
    /// 
    /// References a LiteNetLibTransportProvider asset and a Sandbox prefab
    /// (both configured in Unity Editor via Inspector).
    /// </summary>
    public sealed class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Netick References")]
        [SerializeField] private NetworkTransportProvider _transport;
        [SerializeField] private GameObject _sandboxPrefab;
        [SerializeField] private NetickConfig _netickConfig;

        [Header("Network Settings")]
        [SerializeField] private int _serverPort = 7777;
        [SerializeField] private string _serverAddress = "127.0.0.1";
        [SerializeField] private int _maxPlayers = 4;

        public bool IsServer => _sandbox != null && _sandbox.IsServer;
        public bool IsClient => _sandbox != null && _sandbox.IsClient;
        public bool IsRunning => _sandbox != null && _sandbox.IsRunning;
        public bool IsConnected => _sandbox != null && _sandbox.IsConnected;

        public string LocalPlayerId { get; private set; }

        private NetworkSandbox _sandbox;

        public NetworkSandbox Sandbox => _sandbox;

        public event Action OnServerStarted;
        public event Action OnClientConnected;
        public event Action OnDisconnected;
        public event Action<string> OnPlayerJoined;
        public event Action<string> OnPlayerLeft;

        private readonly HashSet<string> _connectedPlayerIds = new HashSet<string>();

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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void StartAsServer()
        {
            Debug.Log($"[NetworkManager] Starting Netick server on port {_serverPort}");
            NetickNetwork.StartAsServer(_transport, _serverPort, _sandboxPrefab, _netickConfig);
            OnServerStarted?.Invoke();
        }

        public void StartAsClient(string playerId)
        {
            LocalPlayerId = playerId;
            Debug.Log($"[NetworkManager] Starting Netick client, connecting to {_serverAddress}:{_serverPort}");
            var sandbox = NetickNetwork.StartAsClient(_transport, _sandboxPrefab, _netickConfig);
            sandbox.Connect(_serverPort, _serverAddress);
            _sandbox = sandbox;
            OnClientConnected?.Invoke();
        }

        public void StartAsHost()
        {
            Debug.Log($"[NetworkManager] Starting Netick host on port {_serverPort}");
            NetickNetwork.StartAsHost(_transport, _serverPort, _sandboxPrefab, _netickConfig);
            OnServerStarted?.Invoke();
            OnClientConnected?.Invoke();
        }

        public void Shutdown()
        {
            Debug.Log("[NetworkManager] Shutting down Netick");
            if (_sandbox != null)
            {
                if (_sandbox.IsClient)
                    _sandbox.DisconnectFromServer();
                _sandbox = null;
            }
            _connectedPlayerIds.Clear();
        }

        // ---- Called by SandboxNetworkListener ----

        public void OnSandboxStarted(NetworkSandbox sandbox)
        {
            _sandbox = sandbox;
            Debug.Log($"[NetworkManager] Sandbox ready. ServerEndPoint={sandbox.ServerEndPoint}");
        }

        public void OnSandboxShutdown()
        {
            _sandbox = null;
            _connectedPlayerIds.Clear();
            Debug.Log("[NetworkManager] Sandbox shut down");
        }

        public void OnConnectedToServer()
        {
            Debug.Log($"[NetworkManager] Client connected to server at {_sandbox?.ServerEndPoint}");
        }

        public void OnDisconnectedFromServer(TransportDisconnectReason reason)
        {
            Debug.Log($"[NetworkManager] Client disconnected: {reason}");
            OnDisconnected?.Invoke();
        }

        public void OnSceneLoaded()
        {
            Debug.Log("[NetworkManager] Scene loaded callback received");
        }

        public void RegisterPlayer(string playerId)
        {
            _connectedPlayerIds.Add(playerId);
            OnPlayerJoined?.Invoke(playerId);
            Debug.Log($"[NetworkManager] Player registered: {playerId}, total={_connectedPlayerIds.Count}");
        }

        public void UnregisterPlayer(string playerId)
        {
            _connectedPlayerIds.Remove(playerId);
            OnPlayerLeft?.Invoke(playerId);
            Debug.Log($"[NetworkManager] Player unregistered: {playerId}, total={_connectedPlayerIds.Count}");
        }

        public int ConnectedPlayerCount => _connectedPlayerIds.Count;

        public bool IsPlayerConnected(string playerId)
        {
            return _connectedPlayerIds.Contains(playerId);
        }

        // ---- Disconnect from server (client-side) ----

        public void DisconnectFromServer()
        {
            if (_sandbox != null && _sandbox.IsClient)
            {
                Debug.Log("[NetworkManager] Disconnecting from server");
                _sandbox.DisconnectFromServer();
            }
        }
    }
}
