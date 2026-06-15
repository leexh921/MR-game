using System;
using System.Collections;
using TreasureArenaMR.Map;
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

        [Header("Runtime Map")]
        [SerializeField] private PrefabRegistry prefabRegistry;
        [SerializeField] private Transform runtimeMapParent;

        private bool lastRunning;
        private bool lastConnected;
        private GameObject runtimeMapRoot;
        private int loadedMapIndex = -1;
        private int loadedMapRevision = -1;
        private bool mapLoading;

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
            {
                TryBuildNetworkMapVisual();
                return;
            }

            lastRunning = networkManager.IsRunning;
            lastConnected = networkManager.IsConnected;
            RefreshStatus(lastConnected ? "Connected" : lastRunning ? "Client running, waiting for server" : "Not running");
            TryBuildNetworkMapVisual();
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
            if (networkManager != null && (networkManager.IsRunning || networkManager.IsConnected))
            {
                Debug.LogWarning("[PicoClientNetickBootstrap] Already running or connected; skipping.");
                return;
            }

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
            DestroyRuntimeMapRoot();
            RefreshStatus("Disconnected");
        }

        private void TryBuildNetworkMapVisual()
        {
            if (mapLoading)
                return;

            var matchState = FindObjectOfType<NetworkMatchState>();
            if (matchState == null || !matchState.MapConfigured)
                return;

            if (loadedMapIndex == matchState.MapIndex && loadedMapRevision == matchState.MapRevision)
                return;

            string mapId = RuntimeMapCatalog.GetMapId(matchState.MapIndex);
            if (string.IsNullOrEmpty(mapId))
            {
                Debug.LogError("[PicoClientNetickBootstrap] Unknown network map index: " + matchState.MapIndex);
                return;
            }

            int targetIndex = matchState.MapIndex;
            int targetRevision = matchState.MapRevision;
            StartCoroutine(LoadMapCoroutine(mapId, targetIndex, targetRevision));
        }

        private IEnumerator LoadMapCoroutine(string mapId, int targetIndex, int targetRevision)
        {
            mapLoading = true;
            DestroyRuntimeMapRoot();

            string path = MapLoader.ResolveMapPath(mapId);
            string json = null;
            string error = null;
            bool complete = false;

            yield return MapLoader.ReadAllTextCoroutine(path, (j, e) =>
            {
                json = j;
                error = e;
                complete = true;
            });

            if (!string.IsNullOrEmpty(error))
            {
                RefreshStatus("Map load failed: " + error);
                Debug.LogError("[PicoClientNetickBootstrap] Map load failed: " + error);
                mapLoading = false;
                yield break;
            }

            MapJsonModels.MapJson map = JsonUtility.FromJson<MapJsonModels.MapJson>(json);

            MapLoader loader = new MapLoader();
            MapInstantiationResult instantiateResult = loader.InstantiateObjects(map, prefabRegistry, runtimeMapParent);
            if (!instantiateResult.ok)
            {
                Debug.LogWarning("[PicoClientNetickBootstrap] Prefab instantiate failed, falling back to primitives: " + instantiateResult.error);
                runtimeMapRoot = new MapRuntimeBuilder().Build(map);
                runtimeMapRoot.name = "RuntimeMap_" + map.map_id;
                if (runtimeMapParent != null)
                    runtimeMapRoot.transform.SetParent(runtimeMapParent, false);
            }
            else
            {
                runtimeMapRoot = instantiateResult.root;
                runtimeMapRoot.name = "RuntimeMap_" + map.map_id;
            }

            loadedMapIndex = targetIndex;
            loadedMapRevision = targetRevision;

            RefreshStatus("Loaded map " + map.map_id);
            Debug.Log("[PicoClientNetickBootstrap] Runtime map visual loaded: " + map.map_id);
            mapLoading = false;
        }

        private void DestroyRuntimeMapRoot()
        {
            loadedMapIndex = -1;
            loadedMapRevision = -1;

            if (runtimeMapRoot == null)
                return;

            if (Application.isPlaying)
                Destroy(runtimeMapRoot);
            else
                DestroyImmediate(runtimeMapRoot);

            runtimeMapRoot = null;
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
