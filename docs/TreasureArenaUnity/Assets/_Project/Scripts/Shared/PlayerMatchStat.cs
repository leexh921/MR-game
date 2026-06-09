using System;

namespace TreasureArenaMR.Shared
{
    /// <summary>
    /// Per-player end-of-match statistics for result display and persistence.
    /// </summary>
    [Serializable]
    public sealed class PlayerMatchStat
    {
        public string player_id;
        public TeamType team = TeamType.None;
        public int kills;
        public int deaths;
        public int treasures_submitted;
        public int score_contribution;
    }
}
