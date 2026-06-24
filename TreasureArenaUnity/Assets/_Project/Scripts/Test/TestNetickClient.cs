using Netick;
using Netick.Unity;
using UnityEngine;
using Network = Netick.Unity.Network;

/// <summary>
/// Ultra-simple Netick client. Connects, sends position, logs everything.
/// Put this on a GameObject (e.g. on Pico).
/// Position is read from the MainCamera (HMD).
/// </summary>
public class TestNetickClient : NetworkEventsListener
{
    [Header("Connection")]
    [SerializeField] private string _serverIP = "192.168.1.100";
    [SerializeField] private int _serverPort = 7777;
    [SerializeField] private bool _connectOnStart = true;

    private NetworkSandbox _sandbox;
    private NetworkObject _myPlayerObj;
    private float _logTimer;
    private float _reconnectTimer;
    private bool _everConnected;
    private int _reconnectAttempts;

    private void Start()
    {
        Debug.Log("[TestClient] ========================================");
        Debug.Log($"[TestClient] Server={_serverIP}:{_serverPort}");
        Debug.Log($"[TestClient] Platform={Application.platform}");
        Debug.Log($"[TestClient] ConnectOnStart={_connectOnStart}");
        Debug.Log("[TestClient] ========================================");

        if (_connectOnStart)
        {
            Invoke(nameof(Connect), 1f); // Small delay to let everything init
        }
    }

    public void Connect()
    {
        _reconnectAttempts++;
        Debug.Log($"[TestClient] >>> CONNECTING (attempt #{_reconnectAttempts}) to {_serverIP}:{_serverPort} <<<");

        var transport = Resources.Load<NetworkTransportProvider>("LiteNetLibTransport");
        var sandboxPrefab = Resources.Load<GameObject>("SandboxRoot");
        var config = Resources.Load<NetickConfig>("netickConfig");

        Debug.Log($"[TestClient] Transport={transport != null}");
        Debug.Log($"[TestClient] SandboxPrefab={sandboxPrefab != null}");
        Debug.Log($"[TestClient] Config={config != null}");

        if (transport == null || sandboxPrefab == null || config == null)
        {
            Debug.LogError("[TestClient] MISSING RESOURCES! Check Resources folder.");
            return;
        }

        try
        {
            _sandbox = Network.StartAsClient(transport, _serverPort, sandboxPrefab, config);
            _sandbox.Connect(_serverPort, _serverIP);
            Debug.Log("[TestClient] Network.StartAsClient() + Connect() called. Waiting...");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TestClient] Connect exception: {ex}");
            ScheduleReconnect();
        }
    }

    public override void OnStartup(NetworkSandbox sandbox)
    {
        _sandbox = sandbox;
        Debug.Log($"[TestClient] SANDBOX STARTED! isServer={sandbox.IsServer} isClient={sandbox.IsClient}");
    }

    public override void OnConnectedToServer(NetworkSandbox sandbox, NetworkConnection connection)
    {
        _everConnected = true;
        _reconnectAttempts = 0;
        Debug.Log($"[TestClient] >>> CONNECTED TO SERVER! endpoint={sandbox.ServerEndPoint} <<<");

        // Find my player object
        StartCoroutine(FindMyPlayer());
    }

    private System.Collections.IEnumerator FindMyPlayer()
    {
        for (int i = 0; i < 30; i++)
        {
            yield return new WaitForSeconds(0.5f);
            if (_sandbox == null) yield break;

            try
            {
                if (_sandbox.TryGetPlayerObject(_sandbox.LocalPlayer.PlayerId, out var obj))
                {
                    _myPlayerObj = obj;
                    Debug.Log($"[TestClient] Found my player object: {obj.name}");
                    yield break;
                }
            }
            catch { }

            Debug.Log($"[TestClient] Waiting for player object... ({i})");
        }
        Debug.LogWarning("[TestClient] Could not find player object after 15s!");
    }

    public override void OnDisconnectedFromServer(NetworkSandbox sandbox, NetworkConnection connection, TransportDisconnectReason reason)
    {
        Debug.Log($"[TestClient] <<< DISCONNECTED! reason={reason} <<<");
        _myPlayerObj = null;

        if (!_everConnected)
        {
            ScheduleReconnect();
        }
    }

    private void ScheduleReconnect()
    {
        _reconnectTimer = 3f;
        Debug.Log($"[TestClient] Will retry in {_reconnectTimer}s...");
    }

    public override void OnShutdown(NetworkSandbox sandbox)
    {
        Debug.Log("[TestClient] Sandbox shutdown.");
    }

    private void Update()
    {
        // Reconnect
        if (_reconnectTimer > 0f)
        {
            _reconnectTimer -= Time.deltaTime;
            if (_reconnectTimer <= 0f)
                Connect();
        }

        // Log my position (client-side)
        if (Camera.main != null)
        {
            _logTimer -= Time.deltaTime;
            if (_logTimer <= 0f)
            {
                _logTimer = 2f;
                Vector3 pos = Camera.main.transform.position;
                Debug.Log($"[TestClient] My HMD pos=({pos.x:F2}, {pos.y:F2}, {pos.z:F2}) " +
                    $"hasPlayerObj={_myPlayerObj != null} " +
                    $"connected={SafeGet(() => _sandbox != null && _sandbox.IsConnected)}");
            }
        }
    }

    private static bool SafeGet(System.Func<bool> getter) { try { return getter(); } catch { return false; } }
}
