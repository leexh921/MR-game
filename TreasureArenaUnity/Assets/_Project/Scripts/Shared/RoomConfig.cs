using System;
using System.Collections.Generic;

namespace TreasureArenaMR.Shared
{
    /// <summary>
    /// Room rule snapshot for match time, HP, respawn countdown, weapon, and treasure score settings.
    /// Matches json_protocol.md §4 RoomConfig JSON schema.
    /// </summary>
    [Serializable]
    public sealed class RoomConfig
    {
        public string room_id;
        public string room_name;
        public string game_mode;
        public string map_id;
        public int max_players;
        public float match_time;
        public int round_count;
        public int player_max_hp;
        public float respawn_countdown;
        public string weapon_id;
        public int weapon_damage;
        public float weapon_range;
        public float weapon_cooldown;
        public int normal_treasure_score;
        public int rare_treasure_score;
        public int final_treasure_score;
        public float treasure_refresh_interval;
        public float supply_refresh_interval;
        public string created_at;

        public RoomConfig()
        {
            room_id = "";
            room_name = "";
            game_mode = "TeamTreasure";
            map_id = "";
            max_players = 4;
            match_time = 300f;
            round_count = 1;
            player_max_hp = 100;
            respawn_countdown = 5f;
            weapon_id = "energy_gun";
            weapon_damage = 25;
            weapon_range = 15f;
            weapon_cooldown = 0.5f;
            normal_treasure_score = 10;
            rare_treasure_score = 30;
            final_treasure_score = 50;
            treasure_refresh_interval = 10f;
            supply_refresh_interval = 20f;
            created_at = "";
        }
    }
}
