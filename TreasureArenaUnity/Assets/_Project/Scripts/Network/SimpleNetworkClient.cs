using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Network
{
    public sealed class SimpleNetworkClient : MonoBehaviour
    {
        private const string LogPrefix = "[SimpleNetClient]";

        [SerializeField] private string _serverAddress = "127.0.0.1";
        [SerializeField] private int _tcpPort = SimpleNetworkProtocol.TcpPort;
        [SerializeField] private int _udpPort = SimpleNetworkProtocol.UdpPort;
        [SerializeField] private string _playerId = "pico_client_01";
        [SerializeField] private string _nickname = "Pico Client";
        [SerializeField] private string _roomId = SimpleNetworkProtocol.DefaultRoomId;

        public bool IsConnected { get; private set; }
        public string PlayerId => _playerId;
        public string ServerAddress => _serverAddress;
        public int TcpPort => _tcpPort;
        public int UdpPort => _udpPort;

        private readonly ConcurrentQueue<string> _incoming = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();
        private TcpClient _tcpClient;
        private NetworkStream _tcpStream;
        private UdpClient _udpClient;
        private IPEndPoint _udpRemoteEndPoint;
        private Thread _tcpReaderThread;
        private Thread _udpReaderThread;
        private int _seq;
        private readonly object _sendLock = new object();
        private ClientMatchStateStore _store;
        private bool _connectInProgress;
        private string _lastSpaceAnchorLogKey = "";

        private void Awake()
        {
            EnsureStore();
        }

        private void Update()
        {
            while (_mainThreadActions.TryDequeue(out Action action))
            {
                action?.Invoke();
            }

            while (_incoming.TryDequeue(out string json))
            {
                HandleIncoming(json);
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        public void Configure(string serverAddress, int tcpPort, int udpPort, string playerId, string nickname)
        {
            if (!string.IsNullOrEmpty(serverAddress))
                _serverAddress = serverAddress;
            if (tcpPort > 0)
                _tcpPort = tcpPort;
            if (udpPort > 0)
                _udpPort = udpPort;
            if (!string.IsNullOrEmpty(playerId))
                _playerId = playerId;
            if (!string.IsNullOrEmpty(nickname))
                _nickname = nickname;
        }

        public void Connect()
        {
            if (IsConnected || _connectInProgress)
                return;

            EnsureStore();
            Debug.Log($"{LogPrefix} Connecting TCP {_serverAddress}:{_tcpPort} UDP {_udpPort} player={_playerId}");
            _connectInProgress = true;
            _store.SetConnectionState(false, "Connecting", _playerId, _roomId);

            string address = _serverAddress;
            int tcpPort = _tcpPort;
            int udpPort = _udpPort;
            string clientType = Application.platform.ToString();
            Thread connectThread = new Thread(() => ConnectInBackground(address, tcpPort, udpPort, clientType))
            {
                IsBackground = true,
                Name = "SimpleNetClientConnect"
            };
            connectThread.Start();
        }

        public void Disconnect()
        {
            if (!IsConnected && _tcpClient == null && _udpClient == null)
                return;

            IsConnected = false;
            _connectInProgress = false;
            CloseSockets();
            if (_store != null)
                _store.SetConnectionState(false, "Disconnected", _playerId, _roomId);
            Debug.Log($"{LogPrefix} Disconnected player={_playerId}");
        }

        public void SendPlayerPose(Vector3 position, float rotationY)
        {
            if (!IsConnected || _udpClient == null || _udpRemoteEndPoint == null)
                return;

            var payload = new PlayerPosePayload
            {
                x = position.x,
                y = position.y,
                z = position.z,
                rotation_y = rotationY,
                pose_seq = NextSeq()
            };
            SimpleNetworkEnvelope envelope = SimpleNetworkJson.CreateEnvelope(
                "player_pose", payload.pose_seq, _roomId, _playerId, payload);
            string json = SimpleNetworkJson.ToJson(envelope);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            _udpClient.Send(bytes, bytes.Length, _udpRemoteEndPoint);
        }

        public void SendPickupTreasure(string treasureId)
        {
            SendEnvelope("pickup_treasure_request", new PickupTreasureRequestPayload { treasure_id = treasureId });
        }

        public void SendSubmitTreasure()
        {
            SendEnvelope("submit_treasure_request", new SubmitTreasureRequestPayload());
        }

        public void SendAttack(Vector3 origin, Vector3 direction, string weaponId)
        {
            SendEnvelope("attack_request", new AttackRequestPayload
            {
                weapon_id = string.IsNullOrEmpty(weaponId) ? "energy_gun" : weaponId,
                origin = new SimpleVector3(origin),
                direction = new SimpleVector3(direction),
                client_time = Time.time
            });
        }

        public void SendSwitchTeam(TeamType team)
        {
            SendEnvelope("switch_team_request", new SwitchTeamRequestPayload { target_team = team.ToString() });
        }

        public void SendStartMatch()
        {
            SendEnvelope("start_match_request", new StartMatchRequestPayload());
        }

        public void SendSpaceAnchorPublish(string anchorUuid)
        {
            SendEnvelope("space_anchor_publish", new SpaceAnchorPublishPayload { anchor_uuid = anchorUuid ?? string.Empty });
        }

        public void SendSpaceAnchorReady(bool ready, string status)
        {
            SendEnvelope("space_anchor_ready", new SpaceAnchorReadyPayload
            {
                ready = ready,
                status = status ?? string.Empty
            });
        }

        private void SendEnvelope(string type, object payload)
        {
            if (!IsConnected || _tcpStream == null)
                return;

            SimpleNetworkEnvelope envelope = SimpleNetworkJson.CreateEnvelope(type, NextSeq(), _roomId, _playerId, payload);
            string json = SimpleNetworkJson.ToJson(envelope);
            try
            {
                lock (_sendLock)
                {
                    SimpleTcpFraming.WriteFrame(_tcpStream, json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} TCP send failed type={type} error={ex.Message}");
                Disconnect();
            }
        }

        private void StartTcpReader()
        {
            _tcpReaderThread = new Thread(ReadTcpLoop) { IsBackground = true, Name = "SimpleNetClientTcpReader" };
            _tcpReaderThread.Start();
        }

        private void ConnectInBackground(string address, int tcpPort, int udpPort, string clientType)
        {
            try
            {
                TcpClient tcpClient = new TcpClient();
                IAsyncResult connectResult = tcpClient.BeginConnect(address, tcpPort, null, null);
                bool connected = connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));
                if (!connected)
                {
                    tcpClient.Close();
                    throw new TimeoutException("tcp_connect_timeout");
                }

                tcpClient.EndConnect(connectResult);
                tcpClient.NoDelay = true;
                NetworkStream stream = tcpClient.GetStream();

                UdpClient udpClient = new UdpClient();
                IPEndPoint udpRemote = new IPEndPoint(IPAddress.Parse(address), udpPort);

                _mainThreadActions.Enqueue(() =>
                {
                    _tcpClient = tcpClient;
                    _tcpStream = stream;
                    _udpClient = udpClient;
                    _udpRemoteEndPoint = udpRemote;
                    IsConnected = true;
                    _connectInProgress = false;
                    _store.SetConnectionState(true, "TCP connected", _playerId, _roomId);
                    StartTcpReader();
                    StartUdpReader();
                    SendEnvelope("join_room_request", new JoinRoomRequestPayload
                    {
                        nickname = _nickname,
                        client_type = clientType
                    });
                });
            }
            catch (Exception ex)
            {
                _mainThreadActions.Enqueue(() =>
                {
                    Debug.LogError($"{LogPrefix} Connect failed: {ex.Message}");
                    IsConnected = false;
                    _connectInProgress = false;
                    _store.SetConnectionState(false, "Connect failed: " + ex.Message, _playerId, _roomId);
                    CloseSockets();
                });
            }
        }

        private void StartUdpReader()
        {
            _udpReaderThread = new Thread(ReadUdpLoop) { IsBackground = true, Name = "SimpleNetClientUdpReader" };
            _udpReaderThread.Start();
        }

        private void ReadTcpLoop()
        {
            try
            {
                while (IsConnected && _tcpStream != null)
                {
                    string json = SimpleTcpFraming.ReadFrame(_tcpStream);
                    _incoming.Enqueue(json);
                }
            }
            catch (Exception ex)
            {
                if (IsConnected)
                    Debug.LogError($"{LogPrefix} TCP read stopped: {ex.Message}");
            }
        }

        private void ReadUdpLoop()
        {
            try
            {
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
                while (IsConnected && _udpClient != null)
                {
                    byte[] bytes = _udpClient.Receive(ref endpoint);
                    string json = System.Text.Encoding.UTF8.GetString(bytes);
                    _incoming.Enqueue(json);
                }
            }
            catch (Exception ex)
            {
                if (IsConnected)
                    Debug.LogWarning($"{LogPrefix} UDP read stopped: {ex.Message}");
            }
        }

        private void HandleIncoming(string json)
        {
            if (!SimpleNetworkJson.TryParseEnvelope(json, out SimpleNetworkEnvelope envelope, out string error))
            {
                Debug.LogError($"{LogPrefix} Reject inbound message: {error}");
                Disconnect();
                return;
            }

            if (envelope.type == "join_room_result")
            {
                JoinRoomResultPayload payload = SimpleNetworkJson.ReadPayload<JoinRoomResultPayload>(envelope);
                if (!payload.ok)
                {
                    Debug.LogError($"{LogPrefix} Join rejected error={payload.error_code} message={payload.message}");
                    Disconnect();
                    return;
                }

                _playerId = payload.player_id;
                _roomId = payload.room_id;
                _store.SetConnectionState(true, "Joined room", _playerId, _roomId);
                Debug.Log($"{LogPrefix} Joined room={_roomId} player={_playerId} team={payload.team}");
                return;
            }

            if (envelope.type == "match_snapshot")
            {
                _store.ApplyMatchSnapshot(SimpleNetworkJson.ReadPayload<MatchSnapshotPayload>(envelope));
                return;
            }

            if (envelope.type == "pose_snapshot")
            {
                _store.ApplyPoseSnapshot(SimpleNetworkJson.ReadPayload<PoseSnapshotPayload>(envelope));
                return;
            }

            if (envelope.type == "space_anchor_state")
            {
                SpaceAnchorStatePayload payload = SimpleNetworkJson.ReadPayload<SpaceAnchorStatePayload>(envelope);
                _store.ApplySpaceAnchorState(payload);
                string logKey = $"{payload.owner_player_id}|{payload.has_anchor}|{payload.all_players_ready}|{payload.state}|{payload.anchor_uuid}|{payload.error}";
                if (_lastSpaceAnchorLogKey != logKey)
                {
                    _lastSpaceAnchorLogKey = logKey;
                    Debug.Log($"{LogPrefix} space_anchor_state owner={payload.owner_player_id} hasAnchor={payload.has_anchor} " +
                        $"ready={payload.all_players_ready} state={payload.state} uuid={payload.anchor_uuid} error={payload.error}");
                }
                return;
            }

            if (envelope.type == "command_result")
            {
                CommandResultPayload payload = SimpleNetworkJson.ReadPayload<CommandResultPayload>(envelope);
                Debug.Log($"{LogPrefix} command_result ok={payload.ok} error={payload.error_code} message={payload.message}");
                return;
            }

            if (envelope.type == "disconnect_notice")
            {
                DisconnectNoticePayload payload = SimpleNetworkJson.ReadPayload<DisconnectNoticePayload>(envelope);
                Debug.LogWarning($"{LogPrefix} disconnect_notice player={payload.player_id} reason={payload.reason}");
                return;
            }

            Debug.LogWarning($"{LogPrefix} Unknown inbound type={envelope.type}");
        }

        private int NextSeq()
        {
            _seq++;
            if (_seq == int.MaxValue)
                _seq = 1;
            return _seq;
        }

        private void EnsureStore()
        {
            if (_store != null)
                return;

            _store = ClientMatchStateStore.Instance;
            if (_store == null)
                _store = new GameObject("ClientMatchStateStore").AddComponent<ClientMatchStateStore>();
        }

        private void CloseSockets()
        {
            try { _tcpStream?.Close(); } catch { }
            try { _tcpClient?.Close(); } catch { }
            try { _udpClient?.Close(); } catch { }
            _tcpStream = null;
            _tcpClient = null;
            _udpClient = null;
        }
    }
}
