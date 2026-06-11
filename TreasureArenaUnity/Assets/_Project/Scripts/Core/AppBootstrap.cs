using TreasureArenaMR.Client;
using TreasureArenaMR.Map;
using TreasureArenaMR.Mock;
using TreasureArenaMR.Network;
using TreasureArenaMR.Server;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.Core
{
    /// <summary>
    /// Entry point for selecting Server, PicoClient, or LocalTest flow in Game.unity.
    /// </summary>
    public sealed class AppBootstrap : MonoBehaviour
    {
        [Header("Role")]
        [SerializeField] private AppRole defaultRole = AppRole.LocalTest;
        [SerializeField] private bool androidBuildRunsAsPicoClient = true;

        [Header("Connection")]
        [SerializeField] private string serverAddress = "192.168.61.128";
        [SerializeField] private int serverPort = 7777;
        [SerializeField] private string playerId = "pico_client_01";

        [Header("Map")]
        [SerializeField] private string runtimeMapId = "test_map_01";
        [SerializeField] private Transform runtimeMapParent;

        [Header("Runtime Roots")]
        [SerializeField] private GameObject desktopCameraRoot;
        [SerializeField] private GameObject picoXrRoot;
        [SerializeField] private GameObject networkRuntimeRoot;
        [SerializeField] private GameObject localTestRoot;

        [Header("Runtime Components")]
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private ServerApp serverApp;
        [SerializeField] private PicoClientNetickBootstrap picoClientBootstrap;
        [SerializeField] private LocalTestGameplayDriver localTestDriver;

        [Header("Status")]
        [SerializeField] private Text statusText;

        private GameObject runtimeMapRoot;

        public AppRole ActiveRole { get; private set; }

        private void Awake()
        {
            ActiveRole = ResolveRole();
            ApplyRoleObjectState();
            ConfigureNetworkEndpoint();
            UpdateStatus("Role: " + ActiveRole);
        }

        private void Start()
        {
            switch (ActiveRole)
            {
                case AppRole.Server:
                case AppRole.Manager:
                    BuildStaticMapVisual();
                    StartServerRole();
                    break;
                case AppRole.PicoClient:
                    BuildStaticMapVisual();
                    StartPicoClientRole();
                    break;
                case AppRole.LocalTest:
                    StartLocalTestRole();
                    break;
            }
        }

        private AppRole ResolveRole()
        {
            AppRole role = defaultRole;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (androidBuildRunsAsPicoClient)
                role = AppRole.PicoClient;
#endif

            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-appRole" && i + 1 < args.Length)
                {
                    if (System.Enum.TryParse(args[i + 1], true, out AppRole parsedRole))
                        role = parsedRole;
                }
                else if (args[i] == "-serverAddress" && i + 1 < args.Length)
                {
                    serverAddress = args[i + 1];
                }
                else if (args[i] == "-serverPort" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out int parsedPort))
                        serverPort = parsedPort;
                }
                else if (args[i] == "-playerId" && i + 1 < args.Length)
                {
                    playerId = args[i + 1];
                }
            }

            return role;
        }

        private void ApplyRoleObjectState()
        {
            bool isPicoClient = ActiveRole == AppRole.PicoClient;
            bool isLocalTest = ActiveRole == AppRole.LocalTest;
            bool usesNetwork = ActiveRole == AppRole.Server || ActiveRole == AppRole.Manager || ActiveRole == AppRole.PicoClient;

            SetActive(desktopCameraRoot, !isPicoClient);
            SetActive(picoXrRoot, isPicoClient);
            SetActive(networkRuntimeRoot, usesNetwork);
            SetActive(localTestRoot, isLocalTest);

            if (localTestDriver != null)
                localTestDriver.enabled = isLocalTest;
            if (serverApp != null)
                serverApp.enabled = ActiveRole == AppRole.Server || ActiveRole == AppRole.Manager;
            if (picoClientBootstrap != null)
                picoClientBootstrap.enabled = isPicoClient;
        }

        private void ConfigureNetworkEndpoint()
        {
            if (networkManager != null)
                networkManager.ConfigureEndpoint(serverAddress, serverPort);

            /* DEBUG: Configure method doesn't exist on PicoClientNetickBootstrap
            if (picoClientBootstrap != null)
                picoClientBootstrap.Configure(serverAddress, serverPort, playerId, false);
            */
        }

        private void StartServerRole()
        {
            if (serverApp == null)
            {
                UpdateStatus("Server role missing ServerApp.");
                Debug.LogError("[AppBootstrap] Server role missing ServerApp.");
                return;
            }

            UpdateStatus("Starting server on port " + serverPort);
            serverApp.StartServer();
        }

        private void StartPicoClientRole()
        {
            if (picoClientBootstrap == null)
            {
                UpdateStatus("PicoClient role missing PicoClientNetickBootstrap.");
                Debug.LogError("[AppBootstrap] PicoClient role missing PicoClientNetickBootstrap.");
                return;
            }

            UpdateStatus("Connecting to " + serverAddress + ":" + serverPort);
            picoClientBootstrap.Connect();
        }

        private void StartLocalTestRole()
        {
            UpdateStatus("LocalTest ready.");
        }

        private void BuildStaticMapVisual()
        {
            if (string.IsNullOrEmpty(runtimeMapId))
                return;

            DestroyRuntimeMapRoot();

            MapLoadResult loadResult = new MapLoader().LoadFromMapId(runtimeMapId);
            if (!loadResult.ok)
            {
                UpdateStatus("Map load failed: " + loadResult.error);
                Debug.LogError("[AppBootstrap] Map load failed: " + loadResult.error);
                return;
            }

            runtimeMapRoot = new MapRuntimeBuilder().Build(loadResult.map);
            runtimeMapRoot.name = "RuntimeMap_" + loadResult.map.map_id;
            if (runtimeMapParent != null)
                runtimeMapRoot.transform.SetParent(runtimeMapParent, false);

            Debug.Log("[AppBootstrap] Runtime map visual loaded: " + loadResult.map.map_id);
        }

        private void DestroyRuntimeMapRoot()
        {
            if (runtimeMapRoot == null)
                return;

            if (Application.isPlaying)
                Destroy(runtimeMapRoot);
            else
                DestroyImmediate(runtimeMapRoot);

            runtimeMapRoot = null;
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
                statusText.text = "Game Runtime\n" + message;

            Debug.Log("[AppBootstrap] " + message);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }
    }
}
