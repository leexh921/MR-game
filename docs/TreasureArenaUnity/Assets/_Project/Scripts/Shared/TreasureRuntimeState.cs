using System;
using UnityEngine;

namespace TreasureArenaMR.Shared
{
    /// <summary>
    /// Server-synchronized treasure state used during pickup, drop, and submit flows.
    /// </summary>
    [Serializable]
    public sealed class TreasureRuntimeState
    {
        public string treasure_id;
        public TreasureType treasure_type = TreasureType.Normal;
        public TreasureState state = TreasureState.Spawned;
        public int score_value;
        public Vector3 position;
        public string carrier_player_id;
    }
}
