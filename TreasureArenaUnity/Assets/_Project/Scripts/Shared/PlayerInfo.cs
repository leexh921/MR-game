using System;

namespace TreasureArenaMR.Shared
{
    /// <summary>
    /// Stable player information shown in room and team management flows.
    /// Matches json_protocol.md §5.1 PlayerInfo JSON schema.
    /// </summary>
    [Serializable]
    public sealed class PlayerInfo
    {
        public string player_id;
        public string nickname;
        public string team;
        public string state;
        public int hp;
        public int max_hp;
        public bool is_connected;
        public string carried_treasure_id;

        public PlayerInfo()
        {
            player_id = "";
            nickname = "";
            team = "None";
            state = "Alive";
            hp = 100;
            max_hp = 100;
            is_connected = false;
            carried_treasure_id = "";
        }
    }
}
