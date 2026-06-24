using Netick;
using Netick.Unity;
using UnityEngine;
using Network = Netick.Unity.Network;

/// <summary>
/// Ultra-simple Netick server. No game logic — just starts, accepts clients, logs position updates.
/// Put this on a GameObject in an empty scene.
/// </summary>
public class TestNetickServer : NetworkEventsListener
{
    [Header("Server Settings")]
    [SerializeField] private int _port = 7777;
    [SerializeField] private GameObject _playerPrefab;

    private NetworkSandbox _sandbox;
    private float _logTimer;

    private void Start()
    {
        Debug.Log("[TestServer] ========================================");
        Debug.Log("[TestServer] Starting Netick server...");
        Debug.Log($"[TestServer] Port={_port}");
        Debug.Log($"[TestServer] PlayerPrefab={(_playerPrefab != null ? _playerPrefab.name : "NULL! Assign it!")}");
        Debug.Log($"[TestServer] ========================================");

        var transport = Resources.Load<NetworkTransportProvider>("LiteNetLibTransport");
        var sandboxPrefab = Resources.Load<GameObject>("SandboxRoot");
        var config = Resources.Load<NetickConfig>("netickConfig");

        Debug.Log($"[TestServer] Transport={transport != null}");
        Debug.Log($"[TestServer] SandboxPrefab={sandboxPrefab != null}");
        Debug.Log($"[TestServer] Config={config != null}");

        if (transport == null|| sandboxPrefab == null || config == null)
        {
            Debug.LogError("[TestServer] MISSING RESOURCES! Check Resources folder.");
            return;
        }

        Network.StartAsServer(transport, _port, sandboxPrefab, config);
        Debug.Log("[TestServer] Network.StartAsServer() called. Waiting for OnStartup...");
    }

    public override void OnStartup(NetworkSandbox sandbox)
    {
        _sandbox = sandbox;
        Debug.Log($"[TestServer] SANDBOX STARTED! isServer={sandbox.IsServer} isClient={sandbox.IsClient}");
        Debug.Log($"[TestServer] Server listening on port {_port}");
    }

    public override void OnPlayerConnected(NetworkSandbox sandbox, Netick.NetworkPlayer player)
    {
        Debug.Log($"[TestServer] >>> PLAYER CONNECTED! playerId={player.PlayerId} <<<");

        if (_playerPrefab == null)
        {
            Debug.LogError("[TestServer] No player prefab assigned! Can't spawn.");
            return;
        }

        // Spawn player at origin
        var obj = sandbox.NetworkInstantiate(_playerPrefab, Vector3.zero, Quaternion.identity, player);
        sandbox.SetPlayerObject(player.PlayerId, obj);

        Debug.Log($"[TestServer] Spawned player object: {(obj != null ? obj.name : "NULL")}");

        // Log position every second for this player
        StartCoroutine(LogPlayerPosition(player.PlayerId, obj));
    }

    private System.Collections.IEnumerator LogPlayerPosition(Netick.NetworkPlayerId playerId, NetworkObject obj)
    {
        while (obj != null)
        {
            yield return new WaitForSeconds(1f);
            if (obj != null)
                Debug.Log($"[TestServer] Player[{playerId}] pos={obj.transform.position} rot={obj.transform.rotation.eulerAngles}");
        }
    }

    public override void OnPlayerDisconnected(NetworkSandbox sandbox, Netick.NetworkPlayer player, TransportDisconnectReason reason)
    {
        Debug.Log($"[TestServer] <<< PLAYER DISCONNECTED! playerId={player.PlayerId} reason={reason} <<<");
    }

    public override void OnConnectRequest(NetworkSandbox sandbox, NetworkConnectionRequest request)
    {
        Debug.Log($"[TestServer] Connect request received. Accepting.");
    }

    public override void OnShutdown(NetworkSandbox sandbox)
    {
        Debug.Log("[TestServer] Server shut down.");
    }

    private void Update()
    {
        _logTimer -= Time.deltaTime;
        if (_logTimer <= 0f)
        {
            _logTimer = 5f;
            if (_sandbox != null)
            {
                Debug.Log($"[TestServer] ALIVE — isRunning={SafeGet(() => _sandbox.IsRunning)} " +
                    $"isServer={SafeGet(() => _sandbox.IsServer)}");
            }
            else
            {
                Debug.Log("[TestServer] ALIVE — sandbox is null, server not started yet.");
            }
        }
    }

    private static bool SafeGet(System.Func<bool> getter) { try { return getter(); } catch { return false; } }
}
