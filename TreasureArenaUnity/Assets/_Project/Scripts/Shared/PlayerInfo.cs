using System;

namespace TreasureArenaMR.Shared
{
    /// <summary>
    /// Stable player information shown in room and team management flows.
    /// </summary>
    [Serializable]
    public sealed class PlayerInfo
    {
        public string player_id;
        public string nickname;
        public TeamType team = TeamType.None;
        public PlayerState state = PlayerState.Alive;
        public int hp = 100;
        public int max_hp = 100;
        public bool is_connected = true;
        public string carried_treasure_id;
    }
}
