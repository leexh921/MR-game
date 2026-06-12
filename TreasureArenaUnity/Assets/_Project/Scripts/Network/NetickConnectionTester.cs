using System.Collections;
using UnityEngine;
using Netick;
using Netick.Unity;

/// <summary>
/// Netick 连接测试脚本
/// 挂载到场景中任意 GameObject，配置好参数后运行即可测试服务端/客户端能否连接。
/// </summary>
public class NetickConnectionTester : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector 配置
    // ─────────────────────────────────────────────

    [Header("运行模式")]
    [Tooltip("Server = 只启动服务端并等待连接\nClient = 只启动客户端并尝试连接")]
    public StartMode Mode = StartMode.Server;

    [Header("网络参数")]
    [Tooltip("监听/连接的端口号")]
    public int Port = 7777;

    [Tooltip("客户端模式下要连接的服务器 IP（本机测试用 127.0.0.1）")]
    public string ServerIP = "192.168.156.195";

    [Header("Transport")]
    [Tooltip("拖入项目中的 NetworkTransportProvider（如 LiteNetLibTransportProvider）")]
    public NetworkTransportProvider Transport;

    [Header("超时设置")]
    [Tooltip("客户端等待连接成功的最长秒数，超时后报告失败")]
    public float ConnectTimeoutSeconds = 10f;

    // ─────────────────────────────────────────────
    // 内部状态
    // ─────────────────────────────────────────────

    private NetworkSandbox _sandbox;
    private bool _resultReported = false;

    // ─────────────────────────────────────────────

    public enum StartMode { Server, Client }

    // ─────────────────────────────────────────────
    // 生命周期
    // ─────────────────────────────────────────────

    private void Start()
    {
        if (Transport == null)
        {
            LogError("Transport 未设置！请在 Inspector 中拖入 NetworkTransportProvider 组件。");
            return;
        }

        if (Mode == StartMode.Server)
            StartServer();
        else
            StartClient();
    }

    private void OnDestroy()
    {
        // 退出时关闭 Netick，避免端口残留
        if (_sandbox != null)
            Netick.Unity.Network.Shutdown();
    }

    // ─────────────────────────────────────────────
    // 服务端
    // ─────────────────────────────────────────────

    private void StartServer()
    {
        Log($"[Server] 正在启动服务端，监听端口 {Port} ...");

        _sandbox = Netick.Unity.Network.StartAsServer(Transport, Port);

        // 订阅连接事件
        _sandbox.Events.OnClientConnected += OnClientConnected;
        _sandbox.Events.OnClientDisconnected += OnClientDisconnected;

        Log($"[Server] 服务端已启动，等待客户端连接...");
    }

    private void OnClientConnected(NetworkSandbox sandbox, NetworkConnection client)
    {
        Log($"[Server] ✅ 客户端连接成功！来自: {client}");
    }

    private void OnClientDisconnected(NetworkSandbox sandbox, NetworkConnection client, TransportDisconnectReason reason)
    {
        Log($"[Server] 客户端断开连接，原因: {reason}");
    }

    // ─────────────────────────────────────────────
    // 客户端
    // ─────────────────────────────────────────────

    private void StartClient()
    {
        Log($"[Client] 正在启动客户端，目标服务器: {ServerIP}:{Port} ...");

        _sandbox = Netick.Unity.Network.StartAsClient(Transport, Port);

        // 订阅连接相关事件
        _sandbox.Events.OnConnectedToServer += OnConnectedToServer;
        _sandbox.Events.OnDisconnectedFromServer += OnDisconnectedFromServer;
        _sandbox.Events.OnConnectFailed += OnConnectFailed;

        // 发起连接
        _sandbox.Connect(Port, ServerIP);

        Log($"[Client] 连接请求已发出，等待响应（超时 {ConnectTimeoutSeconds}s）...");

        StartCoroutine(ConnectTimeoutCoroutine());
    }

    private void OnConnectedToServer(NetworkSandbox sandbox, NetworkConnection server)
    {
        _resultReported = true;
        Log($"[Client] ✅ 成功连接到服务器 {ServerIP}:{Port} ！连接测试通过。");
    }

    private void OnDisconnectedFromServer(NetworkSandbox sandbox, NetworkConnection server, TransportDisconnectReason reason)
    {
        if (!_resultReported)
        {
            _resultReported = true;
            LogError($"[Client] ❌ 从服务器断开，原因: {reason}");
        }
    }

    private void OnConnectFailed(NetworkSandbox sandbox, ConnectionFailedReason reason)
    {
        _resultReported = true;
        LogError($"[Client] ❌ 连接失败，原因: {reason}");
    }

    private IEnumerator ConnectTimeoutCoroutine()
    {
        yield return new WaitForSeconds(ConnectTimeoutSeconds);

        if (!_resultReported)
        {
            _resultReported = true;
            LogError($"[Client] ❌ 连接超时（{ConnectTimeoutSeconds}s 内未收到服务器响应），请检查 IP/端口/防火墙。");
        }
    }

    // ─────────────────────────────────────────────
    // 工具
    // ─────────────────────────────────────────────

    private static void Log(string msg) => Debug.Log($"[NetickTester] {msg}");
    private static void LogError(string msg) => Debug.LogError($"[NetickTester] {msg}");
}