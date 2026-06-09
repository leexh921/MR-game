using System;
using System.Collections.Generic;
using TreasureArenaMR.Shared;

namespace TreasureArenaMR.Mock
{
    /// <summary>
    /// Creates local mock rooms for Manager UI and LocalTest without networking or database writes.
    /// </summary>
    public sealed class MockRoomFactory
    {
        private readonly MockPlayerFactory playerFactory = new MockPlayerFactory();

        public List<MockRoomState> CreateDefaultRooms()
        {
            return new List<MockRoomState>
            {
                CreateRoom("room_a", "Room A", "test_map_01"),
                CreateRoom("room_b", "Room B", "test_map_01"),
                CreateRoom("room_c", "Room C", "test_map_01")
            };
        }

        public MockRoomState CreateRoom(string roomId, string roomName, string mapId)
        {
            RoomConfig config = CreateDefaultConfig(roomId, roomName, mapId);
            MockRoomState room = new MockRoomState(config);

            room.AddPlayer(playerFactory.CreatePlayer(roomId + "_red_01", "RedPlayer", TeamType.Red, config.player_max_hp));
            room.AddPlayer(playerFactory.CreatePlayer(roomId + "_blue_01", "BluePlayer", TeamType.Blue, config.player_max_hp));

            return room;
        }

        public RoomConfig CreateDefaultConfig(string roomId, string roomName, string mapId)
        {
            return new RoomConfig
            {
                room_id = roomId,
                room_name = roomName,
                game_mode = GameMode.TeamTreasure,
                map_id = mapId,
                max_players = 4,
                match_time = 300f,
                round_count = 1,
                player_max_hp = 100,
                respawn_countdown = 5f,
                weapon_config = new WeaponConfig
                {
                    weapon_id = "energy_gun",
                    damage = 25,
                    range = 15f,
                    cooldown = 0.5f
                },
                treasure_scores = new TreasureScores
                {
                    Normal = 10,
                    Rare = 30,
                    Final = 50
                },
                treasure_refresh_interval = 10f,
                supply_refresh_interval = 20f
            };
        }
    }

    /// <summary>
    /// Local-only room state used by Mock and Manager prototypes before Netick is connected.
    /// </summary>
    [Serializable]
    public sealed class MockRoomState
    {
        public RoomConfig config;
        public RoomState state = RoomState.Waiting;
        public int redScore;
        public int blueScore;
        public readonly List<PlayerInfo> players = new List<PlayerInfo>();

        public MockRoomState(RoomConfig config)
        {
            this.config = config;
        }

        public bool AddPlayer(PlayerInfo player)
        {
            if (player == null || players.Count >= config.max_players)
            {
                return false;
            }

            players.Add(player);
            RefreshReadyState();
            return true;
        }

        public bool SwitchTeam(string playerId, TeamType targetTeam)
        {
            if (targetTeam == TeamType.None)
            {
                return false;
            }

            PlayerInfo player = FindPlayer(playerId);
            if (player == null)
            {
                return false;
            }

            player.team = targetTeam;
            RefreshReadyState();
            return true;
        }

        public bool StartMatch()
        {
            if (!HasPlayerOnTeam(TeamType.Red) || !HasPlayerOnTeam(TeamType.Blue))
            {
                return false;
            }

            state = RoomState.Playing;
            return true;
        }

        public void FinishMatch()
        {
            state = RoomState.Finished;
        }

        public PlayerInfo FindPlayer(string playerId)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].player_id == playerId)
                {
                    return players[i];
                }
            }

            return null;
        }

        public bool HasPlayerOnTeam(TeamType team)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].team == team)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshReadyState()
        {
            if (state == RoomState.Waiting || state == RoomState.Ready)
            {
                state = HasPlayerOnTeam(TeamType.Red) && HasPlayerOnTeam(TeamType.Blue)
                    ? RoomState.Ready
                    : RoomState.Waiting;
            }
        }
    }
}
