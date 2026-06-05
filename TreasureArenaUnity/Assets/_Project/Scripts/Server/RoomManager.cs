using System;
using System.Collections.Generic;
using Netick;
using TreasureArenaMR.Network;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Server
{
    /// <summary>
    /// Server-owned room lifecycle: creation, joining, team assignment, and state transitions.
    /// Maintains room state in memory; uses RoomRepository for persistence snapshot.
    /// Integrates with Netick via SandboxNetworkListener.
    /// </summary>
    public sealed class RoomManager : MonoBehaviour
    {
        [Header("Default Config")]
        [SerializeField] private string _defaultMapId = "test_map_01";
        [SerializeField] private int _defaultMaxPlayers = 4;
        [SerializeField] private float _defaultMatchTime = 300f;
        [SerializeField] private int _defaultMaxHp = 100;
        [SerializeField] private int _defaultWeaponDamage = 25;
        [SerializeField] private float _defaultRespawnCountdown = 5f;

        public RoomConfig CurrentRoomConfig { get; private set; }
        public RoomState CurrentRoomState { get; private set; }
        public List<PlayerInfo> Players { get; private set; }
        public int RedScore { get; private set; }
        public int BlueScore { get; private set; }
        public float RemainingTime { get; private set; }

        public bool IsFull => Players != null && CurrentRoomConfig != null && Players.Count >= CurrentRoomConfig.max_players;
        public bool CanJoin => CurrentRoomState == RoomState.Waiting;

        private NetworkManager _networkManager;

        // Maps player_id to Netick NetworkPlayer (for RPC targeting, spawn ownership, etc.)
        private readonly Dictionary<string, Netick.NetworkPlayer> _netickPlayers =
            new Dictionary<string, Netick.NetworkPlayer>();

        public event Action<RoomState> OnRoomStateChanged;
        public event Action<PlayerInfo> OnPlayerJoined;
        public event Action<PlayerInfo> OnPlayerLeft;
        public event Action<PlayerInfo, TeamType> OnTeamChanged;

        private float _matchTimer;
        private bool _timerRunning;

        public void Initialize(NetworkManager networkManager)
        {
            _networkManager = networkManager;
            Players = new List<PlayerInfo>();
            CurrentRoomState = RoomState.Waiting;
            RedScore = 0;
            BlueScore = 0;
            Debug.Log("[RoomManager] Initialized");
        }

        public void Shutdown()
        {
            Players?.Clear();
            _netickPlayers.Clear();
            CurrentRoomState = RoomState.Waiting;
            Debug.Log("[RoomManager] Shutdown");
        }

        // ---- Netick bridge methods ----

        public void AddNetworkPlayer(string playerId, string nickname, Netick.NetworkPlayer netPlayer)
        {
            if (Players.Count >= CurrentRoomConfig.max_players)
            {
                Debug.LogWarning($"[RoomManager] Room full, rejecting: {playerId}");
                return;
            }

            if (Players.Exists(p => p.player_id == playerId))
            {
                Debug.LogWarning($"[RoomManager] Player already in room: {playerId}");
                return;
            }

            var playerInfo = new PlayerInfo
            {
                player_id = playerId,
                nickname = nickname,
                team = "None",
                state = "Alive",
                hp = CurrentRoomConfig.player_max_hp,
                max_hp = CurrentRoomConfig.player_max_hp,
                is_connected = true,
                carried_treasure_id = ""
            };

            Players.Add(playerInfo);
            _netickPlayers[playerId] = netPlayer;
            OnPlayerJoined?.Invoke(playerInfo);

            Debug.Log($"[RoomManager] Network player added: {playerId} ({nickname}), " +
                $"total={Players.Count}/{CurrentRoomConfig.max_players}");

            // Auto-assign team (simple balancing)
            AutoAssignTeam(playerId);
        }

        public void RemoveNetworkPlayer(string playerId)
        {
            var player = Players.Find(p => p.player_id == playerId);
            if (player != null)
            {
                player.is_connected = false;
                // Keep the player in list for match history, but mark disconnected
                // For MVP, just remove entirely
                Players.Remove(player);
            }

            _netickPlayers.Remove(playerId);
            OnPlayerLeft?.Invoke(player);
            Debug.Log($"[RoomManager] Network player removed: {playerId}, " +
                $"total={Players.Count}/{CurrentRoomConfig.max_players}");
        }

        public Netick.NetworkPlayer GetNetickPlayer(string playerId)
        {
            _netickPlayers.TryGetValue(playerId, out var netPlayer);
            return netPlayer;
        }

        // ---- Room management ----

        public RoomConfig CreateRoom(string roomId, string roomName, string mapId = null)
        {
            CurrentRoomConfig = new RoomConfig
            {
                room_id = roomId,
                room_name = roomName,
                game_mode = "TeamTreasure",
                map_id = mapId ?? _defaultMapId,
                max_players = _defaultMaxPlayers,
                match_time = _defaultMatchTime,
                round_count = 1,
                player_max_hp = _defaultMaxHp,
                respawn_countdown = _defaultRespawnCountdown,
                weapon_id = "energy_gun",
                weapon_damage = _defaultWeaponDamage,
                weapon_range = 15f,
                weapon_cooldown = 0.5f,
                normal_treasure_score = 10,
                rare_treasure_score = 30,
                final_treasure_score = 50,
                treasure_refresh_interval = 10f,
                supply_refresh_interval = 20f,
                created_at = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            Players.Clear();
            _netickPlayers.Clear();
            CurrentRoomState = RoomState.Waiting;
            RedScore = 0;
            BlueScore = 0;
            Debug.Log($"[RoomManager] Room created: {roomId}");
            return CurrentRoomConfig;
        }

        public bool SwitchTeam(string playerId, TeamType targetTeam)
        {
            if (targetTeam == TeamType.None) return false;

            var player = Players.Find(p => p.player_id == playerId);
            if (player == null) return false;

            string teamStr = targetTeam == TeamType.Red ? "Red" : "Blue";
            player.team = teamStr;
            OnTeamChanged?.Invoke(player, targetTeam);
            Debug.Log($"[RoomManager] Player {playerId} switched to {teamStr}");
            return true;
        }

        private void AutoAssignTeam(string playerId)
        {
            int redCount = GetPlayerCountByTeam(TeamType.Red);
            int blueCount = GetPlayerCountByTeam(TeamType.Blue);

            TeamType assignedTeam = redCount <= blueCount ? TeamType.Red : TeamType.Blue;
            SwitchTeam(playerId, assignedTeam);
        }

        // ---- Room state ----

        public void SetRoomState(RoomState newState)
        {
            if (CurrentRoomState == newState) return;

            CurrentRoomState = newState;
            OnRoomStateChanged?.Invoke(newState);
            Debug.Log($"[RoomManager] Room state -> {newState}");

            if (newState == RoomState.Playing)
            {
                StartTimer();
            }
            else if (newState == RoomState.Finished)
            {
                StopTimer();
            }
        }

        public void UpdateConfig(RoomConfig newConfig)
        {
            if (CurrentRoomState != RoomState.Waiting)
            {
                Debug.LogWarning("[RoomManager] Cannot update config after room has started");
                return;
            }
            CurrentRoomConfig = newConfig;
            Debug.Log($"[RoomManager] Config updated for room {newConfig.room_id}");
        }

        public void AddScore(TeamType team, int score)
        {
            if (team == TeamType.Red) RedScore += score;
            else if (team == TeamType.Blue) BlueScore += score;
        }

        public int GetPlayerCountByTeam(TeamType team)
        {
            string teamStr = team == TeamType.Red ? "Red" : "Blue";
            return Players.FindAll(p => p.team == teamStr).Count;
        }

        // ---- Timer ----

        private void StartTimer()
        {
            _matchTimer = CurrentRoomConfig.match_time;
            RemainingTime = _matchTimer;
            _timerRunning = true;
        }

        private void StopTimer()
        {
            _timerRunning = false;
        }

        private void Update()
        {
            if (!_timerRunning) return;

            _matchTimer -= Time.deltaTime;
            RemainingTime = _matchTimer;

            if (_matchTimer <= 0f)
            {
                _matchTimer = 0f;
                RemainingTime = 0f;
                SetRoomState(RoomState.Finished);
            }
        }
    }
}
