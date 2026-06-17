using System;
using TreasureArenaMR.Network;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.Client
{
    /// <summary>
    /// Pico-side TCP/UDP client launcher for real-device connection tests.
    /// </summary>
    public sealed class PicoClientNetickBootstrap : MonoBehaviour
    {
        [Header("Connection")]
        [SerializeField] private SimpleNetworkClient simpleNetworkClient;
        [SerializeField] private LocalPlayerPoseSender poseSender;
        [SerializeField] private RemotePlayerPresenter remotePlayerPresenter;
        [SerializeField] private SharedSpaceManager sharedSpaceManager;
        [SerializeField] private TreasureInteractionController treasureInteractionController;
        [SerializeField] private TreasurePresenter treasurePresenter;
        [SerializeField] private string serverAddress = "192.168.61.128";
        [SerializeField] private int tcpPort = SimpleNetworkProtocol.TcpPort;
        [SerializeField] private int udpPort = SimpleNetworkProtocol.UdpPort;
        [SerializeField] private string playerId = "pico_client_01";
        [SerializeField] private string nickname = "Pico Client";
        [SerializeField] private bool connectOnStart = true;

        [Header("Debug UI")]
        [SerializeField] private Text statusText;
        [SerializeField] private Button connectButton;
        [SerializeField] private Button disconnectButton;

        private bool lastRunning;
        private bool lastConnected;

        private void Awake()
        {
            if (simpleNetworkClient == null)
                simpleNetworkClient = FindObjectOfType<SimpleNetworkClient>();
            if (simpleNetworkClient == null)
                simpleNetworkClient = gameObject.AddComponent<SimpleNetworkClient>();
            if (poseSender == null)
                poseSender = FindObjectOfType<LocalPlayerPoseSender>();
            if (poseSender == null)
                poseSender = gameObject.AddComponent<LocalPlayerPoseSender>();
            if (remotePlayerPresenter == null)
                remotePlayerPresenter = FindObjectOfType<RemotePlayerPresenter>();
            if (remotePlayerPresenter == null)
                remotePlayerPresenter = gameObject.AddComponent<RemotePlayerPresenter>();
            if (sharedSpaceManager == null)
                sharedSpaceManager = FindObjectOfType<SharedSpaceManager>();
            if (sharedSpaceManager == null)
                sharedSpaceManager = gameObject.AddComponent<SharedSpaceManager>();
            if (treasureInteractionController == null)
                treasureInteractionController = FindObjectOfType<TreasureInteractionController>();
            if (treasureInteractionController == null)
                treasureInteractionController = gameObject.AddComponent<TreasureInteractionController>();
            if (treasurePresenter == null)
                treasurePresenter = FindObjectOfType<TreasurePresenter>();
            if (treasurePresenter == null)
                treasurePresenter = gameObject.AddComponent<TreasurePresenter>();

            BindButton(connectButton, Connect);
            BindButton(disconnectButton, Disconnect);
            RefreshStatus("Ready");
        }

        private void Start()
        {
            if (connectOnStart)
                Connect();
        }

        private void Update()
        {
            if (simpleNetworkClient == null)
                return;

            bool running = simpleNetworkClient.IsConnected;
            bool connected = simpleNetworkClient.IsConnected;
            if (lastRunning == running && lastConnected == connected)
                return;

            lastRunning = running;
            lastConnected = connected;
            RefreshStatus(connected ? "Connected" : "Not running");
        }

        public void Connect()
        {
            if (simpleNetworkClient == null)
            {
                RefreshStatus("Missing SimpleNetworkClient");
                Debug.LogError("[PicoClientNetickBootstrap] Missing SimpleNetworkClient.");
                return;
            }

            simpleNetworkClient.Configure(serverAddress, tcpPort, udpPort, playerId, nickname);
            RefreshStatus("Connecting " + serverAddress + ":" + tcpPort);
            Debug.Log("[PicoClientNetickBootstrap] Connecting to " + serverAddress + ":" + tcpPort + " udp=" + udpPort);

            try
            {
                simpleNetworkClient.Connect();
            }
            catch (Exception ex)
            {
                RefreshStatus("Connect failed: " + ex.Message);
                Debug.LogError("[PicoClientNetickBootstrap] Connect failed: " + ex);
            }
        }

        public void Disconnect()
        {
            if (simpleNetworkClient == null)
                return;

            simpleNetworkClient.Disconnect();
            RefreshStatus("Disconnected");
        }

        private void RefreshStatus(string state)
        {
            string text = "Pico TCP/UDP Client\n"
                + "Server: " + serverAddress + ":" + tcpPort + " UDP " + udpPort + "\n"
                + "Player: " + playerId + "\n"
                + "State: " + state;

            if (simpleNetworkClient != null)
            {
                text += "\nConnected: " + simpleNetworkClient.IsConnected;
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
