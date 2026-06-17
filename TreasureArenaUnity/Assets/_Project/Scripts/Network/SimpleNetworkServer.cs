using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TreasureArenaMR.Map;
using TreasureArenaMR.Server;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Network
{
    public sealed class SimpleNetworkServer : MonoBehaviour
    {
        private const string LogPrefix = "[SimpleNetServer]";
        private const float MatchSnapshotInterval = 0.1f;
        private const float PoseSnapshotInterval = 0.05f;
        private const long PoseStaleMs = 1000;

        [SerializeField] private int _tcpPort = SimpleNetworkProtocol.TcpPort;
        [SerializeField] private int _udpPort = SimpleNetworkProtocol.UdpPort;

        public bool IsRunning { get; private set; }
        public int TcpPort => _tcpPort;
        public int UdpPort => _udpPort;

        private readonly ConcurrentQueue<TcpMessage> _tcpMessages = new ConcurrentQueue<TcpMessage>();
        private readonly ConcurrentQueue<UdpMessage> _udpMessages = new ConcurrentQueue<UdpMessage>();
        private readonly List<ClientSession> _sessions = new List<ClientSession>();
        private readonly object _sessionsLock = new object();

        private TcpListener _tcpListener;
        private UdpClient _udpServer;
        private Thread _tcpAcceptThread;
        private Thread _udpReceiveThread;
        private RoomManager _roomManager;
        private ServerTreasureAuthority _treasureAuthority;
        private ServerCombatAuthority _combatAuthority;
        private int _seq;
        private int _sessionSeq;
        private float _matchSnapshotTimer;
        private float _poseSnapshotTimer;
        private int _mapRevision = 1;

        private void Update()
        {
            if (!IsRunning)
                return;

            DrainTcpMessages();
            DrainUdpMessages();
            BroadcastTicks();
        }

        private void OnDestroy()
        {
            StopServer();
        }

        public void StartServer(
            RoomManager roomManager,
            ServerTreasureAuthority treasureAuthority,
            ServerCombatAuthority combatAuthority,
            int tcpPort,
            int udpPort)
        {
            if (IsRunning)
                return;

            _roomManager = roomManager;
            _treasureAuthority = treasureAuthority;
            _combatAuthority = combatAuthority;
            if (tcpPort > 0)
                _tcpPort = tcpPort;
            if (udpPort > 0)
                _udpPort = udpPort;

            IsRunning = true;
            _tcpListener = new TcpListener(IPAddress.Any, _tcpPort);
            _tcpListener.Start();
            _tcpAcceptThread = new Thread(AcceptTcpLoop) { IsBackground = true, Name = "SimpleNetServerTcpAccept" };
            _tcpAcceptThread.Start();

            _udpServer = new UdpClient(_udpPort);
            _udpReceiveThread = new Thread(ReceiveUdpLoop) { IsBackground = true, Name = "SimpleNetServerUdpReceive" };
            _udpReceiveThread.Start();

            Debug.Log($"{LogPrefix} TCP server started port={_tcpPort}");
            Debug.Log($"{LogPrefix} UDP pose server started port={_udpPort}");
        }

        public void StopServer()
        {
            if (!IsRunning && _tcpListener == null && _udpServer == null)
                return;

            IsRunning = false;
            try { _tcpListener?.Stop(); } catch { }
            try { _udpServer?.Close(); } catch { }
            _tcpListener = null;
            _udpServer = null;

            lock (_sessionsLock)
            {
                for (int i = 0; i < _sessions.Count; i++)
                    _sessions[i].Close();
                _sessions.Clear();
            }

            Debug.Log($"{LogPrefix} Server stopped");
        }

        public void MarkMapRevisionChanged()
        {
            _mapRevision++;
            Debug.Log($"{LogPrefix} map revision changed revision={_mapRevision}");
        }

        private void AcceptTcpLoop()
        {
            while (IsRunning || _tcpListener != null)
            {
                try
                {
                    TcpClient tcpClient = _tcpListener.AcceptTcpClient();
                    tcpClient.NoDelay = true;
                    var session = new ClientSession
                    {
                        sessionId = Interlocked.Increment(ref _sessionSeq),
                        tcpClient = tcpClient,
                        stream = tcpClient.GetStream(),
                        remoteEndPoint = tcpClient.Client.RemoteEndPoint != null
                            ? tcpClient.Client.RemoteEndPoint.ToString()
                            : "unknown",
                        lastTcpServerTimeMs = NowMs()
                    };

                    lock (_sessionsLock)
                        _sessions.Add(session);

                    session.readerThread = new Thread(() => ReadTcpLoop(session))
                    {
                        IsBackground = true,
                        Name = "SimpleNetServerTcpReader_" + session.sessionId
                    };
                    session.readerThread.Start();
                    Debug.Log($"{LogPrefix} TCP client accepted session={session.sessionId} remote={session.remoteEndPoint}");
                }
                catch (SocketException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (IsRunning)
                        Debug.LogError($"{LogPrefix} TCP accept error: {ex.Message}");
                }
            }
        }

        private void ReadTcpLoop(ClientSession session)
        {
            try
            {
                while (IsRunning && session.tcpClient != null && session.tcpClient.Connected)
                {
                    string json = SimpleTcpFraming.ReadFrame(session.stream);
                    _tcpMessages.Enqueue(new TcpMessage(session, json));
                }
            }
            catch (Exception ex)
            {
                if (IsRunning)
                    Debug.LogWarning($"{LogPrefix} TCP client disconnected session={session.sessionId} player={session.playerId} reason={ex.Message}");
            }

            _tcpMessages.Enqueue(new TcpMessage(session, null));
        }

        private void ReceiveUdpLoop()
        {
            while (IsRunning || _udpServer != null)
            {
                try
                {
                    IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] bytes = _udpServer.Receive(ref endpoint);
                    string json = System.Text.Encoding.UTF8.GetString(bytes);
                    _udpMessages.Enqueue(new UdpMessage(endpoint, json));
                }
                catch (SocketException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (IsRunning)
                        Debug.LogError($"{LogPrefix} UDP receive error: {ex.Message}");
                }
            }
        }

        private void DrainTcpMessages()
        {
            while (_tcpMessages.TryDequeue(out TcpMessage message))
            {
                if (message.json == null)
                {
                    DisconnectSession(message.session);
                    continue;
                }

                if (!SimpleNetworkJson.TryParseEnvelope(message.json, out SimpleNetworkEnvelope envelope, out string error))
                {
                    Debug.LogError($"{LogPrefix} Reject TCP session={message.session.sessionId} error={error}");
                    SendCommandResult(message.session, false, error, "Protocol rejected.");
                    DisconnectSession(message.session);
                    continue;
                }

                message.session.lastTcpServerTimeMs = NowMs();
                HandleTcpEnvelope(message.session, envelope);
            }
        }

        private void DrainUdpMessages()
        {
            while (_udpMessages.TryDequeue(out UdpMessage message))
            {
                if (!SimpleNetworkJson.TryParseEnvelope(message.json, out SimpleNetworkEnvelope envelope, out string error))
                {
                    Debug.LogError($"{LogPrefix} Reject UDP {message.endpoint} error={error}");
                    continue;
                }

                if (envelope.type != "player_pose")
                    continue;

                PlayerPosePayload payload = SimpleNetworkJson.ReadPayload<PlayerPosePayload>(envelope);
                ClientSession session = FindSessionByPlayer(envelope.player_id);
                if (session == null)
                {
                    Debug.LogWarning($"{LogPrefix} UDP pose from unknown player={envelope.player_id}");
                    continue;
                }

                session.udpEndPoint = message.endpoint;
                _roomManager.UpdatePlayerPose(envelope.player_id,
                    new Vector3(payload.x, payload.y, payload.z),
                    payload.rotation_y,
                    NowMs());
            }
        }

        private void HandleTcpEnvelope(ClientSession session, SimpleNetworkEnvelope envelope)
        {
            if (envelope.type == "join_room_request")
            {
                JoinRoomRequestPayload payload = SimpleNetworkJson.ReadPayload<JoinRoomRequestPayload>(envelope);
                string playerId = string.IsNullOrEmpty(envelope.player_id)
                    ? "player_" + session.sessionId
                    : envelope.player_id;
                string nickname = string.IsNullOrEmpty(payload.nickname) ? playerId : payload.nickname;

                bool joined = _roomManager.AddSimplePlayer(playerId, nickname);
                if (!joined)
                {
                    SendJoinResult(session, false, "join_rejected", "Room is not joinable.", playerId);
                    return;
                }

                session.playerId = playerId;
                session.roomId = envelope.room_id;
                SendJoinResult(session, true, "", "Joined.", playerId);
                Debug.Log($"{LogPrefix} join_room_request accepted player={playerId} nickname={nickname} clientType={payload.client_type}");
                return;
            }

            if (string.IsNullOrEmpty(session.playerId))
            {
                SendCommandResult(session, false, "not_joined", "Join room before sending commands.");
                return;
            }

            if (envelope.type == "switch_team_request")
            {
                SwitchTeamRequestPayload payload = SimpleNetworkJson.ReadPayload<SwitchTeamRequestPayload>(envelope);
                bool ok = _roomManager.SwitchTeam(session.playerId, SimpleNetworkJson.ParseTeam(payload.target_team));
                SendCommandResult(session, ok, ok ? "" : "switch_team_failed", ok ? "Team changed." : "Invalid team.");
                return;
            }

            if (envelope.type == "start_match_request")
            {
                bool ok = _roomManager.CurrentRoomState == RoomState.Waiting && _roomManager.MapData != null;
                if (ok)
                    _roomManager.SetRoomState(RoomState.Playing);
                SendCommandResult(session, ok, ok ? "" : "start_match_failed", ok ? "Match started." : "Room not ready.");
                return;
            }

            if (envelope.type == "pickup_treasure_request")
            {
                PickupTreasureRequestPayload payload = SimpleNetworkJson.ReadPayload<PickupTreasureRequestPayload>(envelope);
                bool ok = TryPickup(session.playerId, payload.treasure_id);
                SendCommandResult(session, ok, ok ? "" : "pickup_failed", ok ? "Pickup accepted." : "Pickup rejected.");
                return;
            }

            if (envelope.type == "submit_treasure_request")
            {
                bool ok = _treasureAuthority != null && _treasureAuthority.TrySubmit(session.playerId, _roomManager);
                SendCommandResult(session, ok, ok ? "" : "submit_failed", ok ? "Submit accepted." : "Submit rejected.");
                return;
            }

            if (envelope.type == "attack_request")
            {
                AttackRequestPayload payload = SimpleNetworkJson.ReadPayload<AttackRequestPayload>(envelope);
                bool ok = HandleAttack(session.playerId, payload);
                SendCommandResult(session, ok, ok ? "" : "attack_missed", ok ? "Attack hit." : "Attack missed.");
                return;
            }

            if (envelope.type == "heartbeat")
            {
                SendCommandResult(session, true, "", "heartbeat_ack");
                return;
            }

            SendCommandResult(session, false, "unknown_type", envelope.type);
        }

        private bool TryPickup(string playerId, string treasureId)
        {
            if (_treasureAuthority == null || _roomManager == null || _roomManager.MapData == null)
                return false;

            if (!string.IsNullOrEmpty(treasureId))
            {
                List<MapTreasureSpawnPoint> points = _roomManager.MapData.GetTreasureSpawnPoints();
                for (int i = 0; i < points.Count; i++)
                {
                    MapTreasureSpawnPoint point = points[i];
                    string pointId = string.IsNullOrEmpty(point.PointId) ? point.TreasureType.ToString() : point.PointId;
                    if (pointId == treasureId)
                        return _treasureAuthority.TryPickup(playerId, treasureId, point.Position, _roomManager);
                }
            }

            return _treasureAuthority.TryPickupNearest(playerId, _roomManager);
        }

        private bool HandleAttack(string playerId, AttackRequestPayload payload)
        {
            if (_combatAuthority == null || _roomManager == null || _roomManager.CurrentRoomConfig == null)
                return false;

            WeaponConfig weapon = _roomManager.CurrentRoomConfig.weapon_config;
            int damage = weapon != null ? weapon.damage : 25;
            float range = weapon != null ? weapon.range : 15f;
            AttackResult result = _combatAuthority.ProcessAttack(
                playerId,
                payload.weapon_id,
                payload.origin != null ? payload.origin.ToVector3() : Vector3.zero,
                payload.direction != null ? payload.direction.ToVector3() : Vector3.forward,
                damage,
                range,
                _roomManager);
            if (!result.hit)
                return false;

            result.target_hp_after = _combatAuthority.ApplyDamage(result.target_player_id, result.damage, _roomManager);
            return result.target_hp_after >= 0;
        }

        private void BroadcastTicks()
        {
            _matchSnapshotTimer -= Time.deltaTime;
            _poseSnapshotTimer -= Time.deltaTime;

            if (_matchSnapshotTimer <= 0f)
            {
                _matchSnapshotTimer = MatchSnapshotInterval;
                BroadcastTcp("match_snapshot", BuildMatchSnapshot());
            }

            if (_poseSnapshotTimer <= 0f)
            {
                _poseSnapshotTimer = PoseSnapshotInterval;
                BroadcastTcp("pose_snapshot", BuildPoseSnapshot());
                BroadcastUdpPoseSnapshot();
            }
        }

        private MatchSnapshotPayload BuildMatchSnapshot()
        {
            var snapshot = new MatchSnapshotPayload
            {
                room_id = _roomManager.CurrentRoomConfig != null
                    ? _roomManager.CurrentRoomConfig.room_id
                    : SimpleNetworkProtocol.DefaultRoomId,
                room_state = _roomManager.CurrentRoomState.ToString(),
                map_id = _roomManager.CurrentRoomConfig != null ? _roomManager.CurrentRoomConfig.map_id : "",
                map_revision = _mapRevision,
                remaining_time = _roomManager.RemainingTime,
                red_score = _roomManager.RedScore,
                blue_score = _roomManager.BlueScore
            };

            IReadOnlyList<ServerPlayerRuntimeState> players = _roomManager.GetRuntimePlayersSnapshot();
            for (int i = 0; i < players.Count; i++)
            {
                ServerPlayerRuntimeState player = players[i];
                snapshot.players.Add(new PlayerSnapshot
                {
                    player_id = player.playerId,
                    nickname = player.nickname,
                    team = player.team.ToString(),
                    state = player.state.ToString(),
                    hp = player.hp,
                    max_hp = player.maxHp,
                    is_connected = player.isConnected,
                    carried_treasure_id = player.carriedTreasureId,
                    position = new SimpleVector3(player.position),
                    rotation_y = player.rotationY
                });
            }

            if (_roomManager.MapData != null)
            {
                List<MapTreasureSpawnPoint> points = _roomManager.MapData.GetTreasureSpawnPoints();
                for (int i = 0; i < points.Count; i++)
                {
                    MapTreasureSpawnPoint point = points[i];
                    string treasureId = string.IsNullOrEmpty(point.PointId) ? point.TreasureType.ToString() : point.PointId;
                    snapshot.treasures.Add(new TreasureSnapshot
                    {
                        treasure_id = treasureId,
                        treasure_type = point.TreasureType.ToString(),
                        state = TreasureState.Spawned.ToString(),
                        score_value = GetTreasureScore(point.TreasureType),
                        position = new SimpleVector3(point.Position),
                        carrier_player_id = ""
                    });
                }
            }

            return snapshot;
        }

        private PoseSnapshotPayload BuildPoseSnapshot()
        {
            long now = NowMs();
            var snapshot = new PoseSnapshotPayload
            {
                room_id = _roomManager.CurrentRoomConfig != null
                    ? _roomManager.CurrentRoomConfig.room_id
                    : SimpleNetworkProtocol.DefaultRoomId
            };

            IReadOnlyList<ServerPlayerRuntimeState> players = _roomManager.GetRuntimePlayersSnapshot();
            for (int i = 0; i < players.Count; i++)
            {
                ServerPlayerRuntimeState player = players[i];
                bool stale = player.lastPoseServerTimeMs <= 0 || now - player.lastPoseServerTimeMs > PoseStaleMs;
                snapshot.players.Add(new PlayerPoseSnapshot
                {
                    player_id = player.playerId,
                    position = new SimpleVector3(player.position),
                    rotation_y = player.rotationY,
                    server_time_ms = now,
                    stale = stale
                });
            }

            return snapshot;
        }

        private int GetTreasureScore(TreasureType type)
        {
            TreasureScores scores = _roomManager.CurrentRoomConfig != null ? _roomManager.CurrentRoomConfig.treasure_scores : null;
            if (scores == null)
                return 10;
            if (type == TreasureType.Rare)
                return scores.Rare;
            if (type == TreasureType.Final)
                return scores.Final;
            return scores.Normal;
        }

        private void BroadcastTcp(string type, object payload)
        {
            List<ClientSession> sessions = SnapshotSessions();
            for (int i = 0; i < sessions.Count; i++)
                SendEnvelope(sessions[i], type, payload);
        }

        private void BroadcastUdpPoseSnapshot()
        {
            if (_udpServer == null)
                return;

            PoseSnapshotPayload payload = BuildPoseSnapshot();
            SimpleNetworkEnvelope envelope = SimpleNetworkJson.CreateEnvelope(
                "pose_snapshot", NextSeq(), payload.room_id, "server", payload);
            string json = SimpleNetworkJson.ToJson(envelope);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);

            List<ClientSession> sessions = SnapshotSessions();
            for (int i = 0; i < sessions.Count; i++)
            {
                IPEndPoint endpoint = sessions[i].udpEndPoint;
                if (endpoint == null)
                    continue;

                try
                {
                    _udpServer.Send(bytes, bytes.Length, endpoint);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"{LogPrefix} UDP send failed player={sessions[i].playerId} error={ex.Message}");
                }
            }
        }

        private void SendJoinResult(ClientSession session, bool ok, string errorCode, string message, string playerId)
        {
            TeamType team = TeamType.None;
            ServerPlayerRuntimeState runtime;
            if (_roomManager.TryGetPlayerRuntimeState(playerId, out runtime))
                team = runtime.team;

            SendEnvelope(session, "join_room_result", new JoinRoomResultPayload
            {
                ok = ok,
                error_code = errorCode,
                message = message,
                player_id = playerId,
                room_id = _roomManager.CurrentRoomConfig != null
                    ? _roomManager.CurrentRoomConfig.room_id
                    : SimpleNetworkProtocol.DefaultRoomId,
                team = team.ToString()
            });
        }

        private void SendCommandResult(ClientSession session, bool ok, string errorCode, string message)
        {
            SendEnvelope(session, "command_result", new CommandResultPayload
            {
                ok = ok,
                error_code = errorCode,
                message = message
            });
        }

        private void SendEnvelope(ClientSession session, string type, object payload)
        {
            if (session == null || session.stream == null)
                return;

            string roomId = _roomManager != null && _roomManager.CurrentRoomConfig != null
                ? _roomManager.CurrentRoomConfig.room_id
                : SimpleNetworkProtocol.DefaultRoomId;
            SimpleNetworkEnvelope envelope = SimpleNetworkJson.CreateEnvelope(type, NextSeq(), roomId, "server", payload);
            string json = SimpleNetworkJson.ToJson(envelope);
            try
            {
                lock (session.sendLock)
                {
                    SimpleTcpFraming.WriteFrame(session.stream, json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{LogPrefix} TCP send failed session={session.sessionId} player={session.playerId} error={ex.Message}");
                DisconnectSession(session);
            }
        }

        private void DisconnectSession(ClientSession session)
        {
            if (session == null)
                return;

            lock (_sessionsLock)
                _sessions.Remove(session);

            if (!string.IsNullOrEmpty(session.playerId))
            {
                _roomManager.RemoveSimplePlayer(session.playerId);
                BroadcastTcp("disconnect_notice", new DisconnectNoticePayload
                {
                    player_id = session.playerId,
                    reason = "tcp_disconnected"
                });
            }

            session.Close();
        }

        private ClientSession FindSessionByPlayer(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return null;

            lock (_sessionsLock)
            {
                for (int i = 0; i < _sessions.Count; i++)
                {
                    if (_sessions[i].playerId == playerId)
                        return _sessions[i];
                }
            }

            return null;
        }

        private List<ClientSession> SnapshotSessions()
        {
            lock (_sessionsLock)
                return new List<ClientSession>(_sessions);
        }

        private int NextSeq()
        {
            _seq++;
            if (_seq == int.MaxValue)
                _seq = 1;
            return _seq;
        }

        private static long NowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private readonly struct TcpMessage
        {
            public TcpMessage(ClientSession session, string json)
            {
                this.session = session;
                this.json = json;
            }

            public readonly ClientSession session;
            public readonly string json;
        }

        private readonly struct UdpMessage
        {
            public UdpMessage(IPEndPoint endpoint, string json)
            {
                this.endpoint = endpoint;
                this.json = json;
            }

            public readonly IPEndPoint endpoint;
            public readonly string json;
        }

        private sealed class ClientSession
        {
            public int sessionId;
            public string playerId;
            public string roomId;
            public string remoteEndPoint;
            public TcpClient tcpClient;
            public NetworkStream stream;
            public Thread readerThread;
            public IPEndPoint udpEndPoint;
            public long lastTcpServerTimeMs;
            public readonly object sendLock = new object();

            public void Close()
            {
                try { stream?.Close(); } catch { }
                try { tcpClient?.Close(); } catch { }
                stream = null;
                tcpClient = null;
            }
        }
    }
}
