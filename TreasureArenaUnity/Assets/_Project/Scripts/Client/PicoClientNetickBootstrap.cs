using System;
using TreasureArenaMR.Network;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.Client
{
    /// <summary>
    /// Minimal Pico-side Netick client launcher for real-device connection tests.
    /// </summary>
    public sealed class PicoClientNetickBootstrap : MonoBehaviour
    {
        [Header("Connection")]
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private string serverAddress = "192.168.61.128";
        [SerializeField] private int serverPort = 7777;
        [SerializeField] private string playerId = "pico_client_01";
        [SerializeField] private bool connectOnStart = true;

        [Header("Debug UI")]
        [SerializeField] private Text statusText;
        [SerializeField] private Button connectButton;
        [SerializeField] private Button disconnectButton;

        private bool lastRunning;
        private bool lastConnected;

        private void Awake()
        {
            if (networkManager == null)
                networkManager = FindObjectOfType<NetworkManager>();

            BindButton(connectButton, Connect);
            BindButton(disconnectButton, Disconnect);
            SubscribeNetworkEvents();
            RefreshStatus("Ready");
        }

        private void Start()
        {
            if (connectOnStart)
                Connect();
        }

        private void Update()
        {
            if (networkManager == null)
                return;

            if (lastRunning == networkManager.IsRunning && lastConnected == networkManager.IsConnected)
                return;

            lastRunning = networkManager.IsRunning;
            lastConnected = networkManager.IsConnected;
            RefreshStatus(lastConnected ? "Connected" : lastRunning ? "Client running, waiting for server" : "Not running");
        }

        private void OnDestroy()
        {
            if (networkManager == null)
                return;

            networkManager.OnClientConnected -= HandleClientConnected;
            networkManager.OnDisconnected -= HandleDisconnected;
        }

        public void Connect()
        {
            if (networkManager == null)
            {
                RefreshStatus("Missing NetworkManager");
                Debug.LogError("[PicoClientNetickBootstrap] Missing NetworkManager.");
                return;
            }

            networkManager.ConfigureEndpoint(serverAddress, serverPort);
            RefreshStatus("Connecting " + serverAddress + ":" + serverPort);
            Debug.Log("[PicoClientNetickBootstrap] Connecting to " + serverAddress + ":" + serverPort);

            try
            {
                networkManager.StartAsClient(playerId);
            }
            catch (Exception ex)
            {
                RefreshStatus("Connect failed: " + ex.Message);
                Debug.LogError("[PicoClientNetickBootstrap] Connect failed: " + ex);
            }
        }

        public void Configure(string address, int port, string id, bool autoConnect)
        {
            if (!string.IsNullOrEmpty(address))
                serverAddress = address;
            if (port > 0)
                serverPort = port;
            if (!string.IsNullOrEmpty(id))
                playerId = id;

            connectOnStart = autoConnect;

            if (networkManager != null)
                networkManager.ConfigureEndpoint(serverAddress, serverPort);

            RefreshStatus("Configured");
        }

        public void Disconnect()
        {
            if (networkManager == null)
                return;

            networkManager.DisconnectFromServer();
            RefreshStatus("Disconnected");
        }

        private void SubscribeNetworkEvents()
        {
            if (networkManager == null)
                return;

            networkManager.OnClientConnected += HandleClientConnected;
            networkManager.OnDisconnected += HandleDisconnected;
        }

        private void HandleClientConnected()
        {
            RefreshStatus("Client started");
        }

        private void HandleDisconnected()
        {
            RefreshStatus("Disconnected");
        }

        private void RefreshStatus(string state)
        {
            string text = "Pico Netick Client\n"
                + "Server: " + serverAddress + ":" + serverPort + "\n"
                + "Player: " + playerId + "\n"
                + "State: " + state;

            if (networkManager != null)
            {
                text += "\nRunning: " + networkManager.IsRunning
                    + "\nConnected: " + networkManager.IsConnected;
            }

            if (statusText != null)
                statusText.text = text;

            Debug.Log("[PicoClientNetickBootstrap] " + text.Replace("\n", " | "));
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }
}
