using System;
using UnityEngine;

namespace TreasureArenaMR.Shared
{
    /// <summary>
    /// Server-synchronized player state used during a match.
    /// </summary>
    [Serializable]
    public sealed class PlayerRuntimeState
    {
        public string player_id;
        public TeamType team = TeamType.None;
        public PlayerState state = PlayerState.Alive;
        public int hp;
        public Vector3 position;
        public float rotation_y;
        public string carried_treasure_id;
    }
}
