using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreasureArenaMR.Network
{
    public sealed class ClientMatchStateStore : MonoBehaviour
    {
        public static ClientMatchStateStore Instance { get; private set; }

        public bool IsConnected { get; private set; }
        public string ConnectionStatus { get; private set; } = "Disconnected";
        public string LocalPlayerId { get; private set; } = "";
        public string RoomId { get; private set; } = SimpleNetworkProtocol.DefaultRoomId;
        public MatchSnapshotPayload MatchSnapshot { get; private set; }
        public PoseSnapshotPayload PoseSnapshot { get; private set; }
        public SpaceAnchorStatePayload SpaceAnchorState { get; private set; }
        public bool HasMatchSnapshot => MatchSnapshot != null;
        public bool HasPoseSnapshot => PoseSnapshot != null;
        public bool HasSpaceAnchorState => SpaceAnchorState != null;

        public event Action OnChanged;

        private readonly Dictionary<string, PlayerSnapshot> _players =
            new Dictionary<string, PlayerSnapshot>();
        private readonly Dictionary<string, PlayerPoseSnapshot> _poses =
            new Dictionary<string, PlayerPoseSnapshot>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetConnectionState(bool connected, string status, string localPlayerId, string roomId)
        {
            IsConnected = connected;
            ConnectionStatus = string.IsNullOrEmpty(status) ? ConnectionStatus : status;
            if (!string.IsNullOrEmpty(localPlayerId))
                LocalPlayerId = localPlayerId;
            if (!string.IsNullOrEmpty(roomId))
                RoomId = roomId;

            OnChanged?.Invoke();
        }

        public void ApplyMatchSnapshot(MatchSnapshotPayload snapshot)
        {
            MatchSnapshot = snapshot;
            _players.Clear();
            if (snapshot != null)
            {
                RoomId = snapshot.room_id;
                for (int i = 0; i < snapshot.players.Count; i++)
                {
                    PlayerSnapshot player = snapshot.players[i];
                    if (player != null && !string.IsNullOrEmpty(player.player_id))
                        _players[player.player_id] = player;
                }
            }

            OnChanged?.Invoke();
        }

        public void ApplyPoseSnapshot(PoseSnapshotPayload snapshot)
        {
            PoseSnapshot = snapshot;
            _poses.Clear();
            if (snapshot != null)
            {
                RoomId = snapshot.room_id;
                for (int i = 0; i < snapshot.players.Count; i++)
                {
                    PlayerPoseSnapshot pose = snapshot.players[i];
                    if (pose != null && !string.IsNullOrEmpty(pose.player_id))
                        _poses[pose.player_id] = pose;
                }
            }

            OnChanged?.Invoke();
        }

        public void ApplySpaceAnchorState(SpaceAnchorStatePayload state)
        {
            SpaceAnchorState = state;
            OnChanged?.Invoke();
        }

        public bool TryGetLocalPlayer(out PlayerSnapshot player)
        {
            return TryGetPlayer(LocalPlayerId, out player);
        }

        public bool TryGetPlayer(string playerId, out PlayerSnapshot player)
        {
            player = null;
            return !string.IsNullOrEmpty(playerId) && _players.TryGetValue(playerId, out player);
        }

        public bool TryGetPose(string playerId, out PlayerPoseSnapshot pose)
        {
            pose = null;
            return !string.IsNullOrEmpty(playerId) && _poses.TryGetValue(playerId, out pose);
        }
    }
}
