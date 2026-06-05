using System;
using System.Collections.Generic;

namespace TreasureArenaMR.Shared
{
    /// <summary>
    /// Final match result data for Manager display and database persistence.
    /// Matches json_protocol.md §7.11 and database_design.md §7 match_result table.
    /// </summary>
    [Serializable]
    public sealed class MatchResult
    {
        public string match_id;
        public string room_id;
        public string map_id;
        public int red_score;
        public int blue_score;
        public string winner_team;
        public float duration;
        public string started_at;
        public string ended_at;
        public List<PlayerMatchStat> player_stats;

        public MatchResult()
        {
            match_id = "";
            room_id = "";
            map_id = "";
            red_score = 0;
            blue_score = 0;
            winner_team = "Draw";
            duration = 0f;
            started_at = "";
            ended_at = "";
            player_stats = new List<PlayerMatchStat>();
        }
    }
}
