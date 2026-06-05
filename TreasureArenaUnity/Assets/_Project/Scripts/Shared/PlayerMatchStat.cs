using System;

namespace TreasureArenaMR.Shared
{
    /// <summary>
    /// Per-player end-of-match statistics for result display and persistence.
    /// Matches json_protocol.md §7.11 and database_design.md §8 player_match_stat table.
    /// </summary>
    [Serializable]
    public sealed class PlayerMatchStat
    {
        public string player_id;
        public string team;
        public int kills;
        public int deaths;
        public int treasures_submitted;
        public int score_contribution;

        public PlayerMatchStat()
        {
            player_id = "";
            team = "None";
            kills = 0;
            deaths = 0;
            treasures_submitted = 0;
            score_contribution = 0;
        }
    }
}
