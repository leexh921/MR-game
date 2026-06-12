using Netick;
using Netick.Unity;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Network
{
    /// <summary>
    /// Replicated match-level state for HUD and manager display.
    /// </summary>
    public sealed class NetworkMatchState : NetworkBehaviour
    {
        [Networked] public int RoomStateValue { get; set; }
        [Networked] public int RedScore { get; set; }
        [Networked] public int BlueScore { get; set; }
        [Networked] public float RemainingTime { get; set; }

        public RoomState RoomState => (RoomState)RoomStateValue;

        public void Initialize(RoomState state, int redScore, int blueScore, float remainingTime)
        {
            Sync(state, redScore, blueScore, remainingTime);
            Debug.Log("[NetworkMatchState] Initialized.");
        }

        public void Sync(RoomState state, int redScore, int blueScore, float remainingTime)
        {
            RoomStateValue = (int)state;
            RedScore = redScore;
            BlueScore = blueScore;
            RemainingTime = Mathf.Max(0f, remainingTime);
        }
    }
}
