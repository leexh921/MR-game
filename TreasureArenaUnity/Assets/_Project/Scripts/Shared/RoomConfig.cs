using System;

namespace TreasureArenaMR.Shared
{
    /// <summary>
    /// Room rule snapshot for match time, HP, respawn countdown, weapon, and treasure score settings.
    /// </summary>
    [Serializable]
    public sealed class RoomConfig
    {
        public string room_id = "room_001";
        public string room_name = "测试房间 001";
        public GameMode game_mode = GameMode.TeamTreasure;
        public string map_id = "test_map_01";
        public int max_players = 4;
        public float match_time = 300f;
        public int round_count = 1;
        public int player_max_hp = 100;
        public float respawn_countdown = 5f;
        public WeaponConfig weapon_config = new WeaponConfig();
        public TreasureScores treasure_scores = new TreasureScores();
        public float treasure_refresh_interval = 10f;
        public float supply_refresh_interval = 20f;
    }

    /// <summary>
    /// Treasure score values keyed to the protocol's treasure type names.
    /// </summary>
    [Serializable]
    public sealed class TreasureScores
    {
        public int Normal = 10;
        public int Rare = 30;
        public int Final = 50;
    }
}
