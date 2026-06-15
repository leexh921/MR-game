using TreasureArenaMR.Network;
using UnityEngine;

public class ClientTest : MonoBehaviour
{
    private NetworkManager _nm;

    void Start()
    {
        _nm = GetComponent<NetworkManager>();
        if (_nm == null)
        {
            Debug.LogError("[ClientTest] No NetworkManager on this GameObject!");
            return;
        }

        Debug.Log("[ClientTest] Connecting to server...");
        _nm.StartAsClient("test_client_01");
        Debug.Log("[ClientTest] StartAsClient returned — IsRunning=" + _nm.IsRunning + " IsConnected=" + _nm.IsConnected);

        InvokeRepeating(nameof(LogState), 2f, 3f);
        Debug.Log("[ClientTest] InvokeRepeating set");
    }

    void LogState()
    {
        Debug.Log($"[ClientTest] State — IsRunning={_nm.IsRunning}, IsConnected={_nm.IsConnected}");
    }
}
