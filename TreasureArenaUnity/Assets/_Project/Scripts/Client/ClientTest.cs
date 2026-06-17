using TreasureArenaMR.Network;
using UnityEngine;

public class ClientTest : MonoBehaviour
{
    private SimpleNetworkClient _client;

    void Start()
    {
        _client = GetComponent<SimpleNetworkClient>();
        if (_client == null)
            _client = gameObject.AddComponent<SimpleNetworkClient>();

        Debug.Log("[ClientTest] Connecting to server...");
        _client.Configure("127.0.0.1", SimpleNetworkProtocol.TcpPort, SimpleNetworkProtocol.UdpPort, "test_client_01", "PC Test Client");
        _client.Connect();
        Debug.Log("[ClientTest] Simple client connect returned — IsConnected=" + _client.IsConnected);

        InvokeRepeating(nameof(LogState), 2f, 3f);
        Debug.Log("[ClientTest] InvokeRepeating set");
    }

    void LogState()
    {
        Debug.Log($"[ClientTest] State — IsConnected={_client.IsConnected}");
    }
}
