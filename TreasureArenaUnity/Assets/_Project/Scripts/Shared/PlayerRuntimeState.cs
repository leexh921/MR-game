using System;

namespace TreasureArenaMR.Shared
{
    /// <summary>
    /// Server-synchronized player state used during a match.
    /// Matches json_protocol.md §5.2 PlayerRuntimeState JSON schema.
    /// </summary>
    [Serializable]
    public sealed class PlayerRuntimeState
    {
        public string player_id;
        public string team;
        public string state;
        public int hp;
        public float pos_x;
        public float pos_y;
        public float pos_z;
        public float rotation_y;
        public string carried_treasure_id;

        public PlayerRuntimeState()
        {
            player_id = "";
            team = "None";
            state = "Alive";
            hp = 100;
            pos_x = 0f;
            pos_y = 0f;
            pos_z = 0f;
            rotation_y = 0f;
            carried_treasure_id = "";
        }
    }
}
