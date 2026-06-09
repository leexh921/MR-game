using System;
using System.Collections.Generic;

namespace TreasureArenaMR.Shared
{
    /// <summary>
    /// Final match result data for Manager display and database persistence.
    /// </summary>
    [Serializable]
    public sealed class MatchResult
    {
        public string match_id;
        public string room_id;
        public string map_id;
        public int red_score;
        public int blue_score;
        public TeamType winner_team = TeamType.None;
        public float duration;
        public List<PlayerMatchStat> player_stats = new List<PlayerMatchStat>();
    }
}
