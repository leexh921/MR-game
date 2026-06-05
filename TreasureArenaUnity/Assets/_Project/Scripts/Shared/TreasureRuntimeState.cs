using System;

namespace TreasureArenaMR.Shared
{
    /// <summary>
    /// Server-synchronized treasure state used during pickup, drop, and submit flows.
    /// Matches json_protocol.md §6.1 TreasureRuntimeState JSON schema.
    /// </summary>
    [Serializable]
    public sealed class TreasureRuntimeState
    {
        public string treasure_id;
        public string treasure_type;
        public string state;
        public int score_value;
        public float pos_x;
        public float pos_y;
        public float pos_z;
        public string carrier_player_id;

        public TreasureRuntimeState()
        {
            treasure_id = "";
            treasure_type = "Normal";
            state = "Spawned";
            score_value = 0;
            pos_x = 0f;
            pos_y = 0f;
            pos_z = 0f;
            carrier_player_id = "";
        }
    }
}
