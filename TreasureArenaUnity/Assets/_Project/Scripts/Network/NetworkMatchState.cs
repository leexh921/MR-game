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
        [Networked] public int MapIndex { get; set; }
        [Networked] public int MapConfiguredValue { get; set; }
        [Networked] public int MapRevision { get; set; }

        public RoomState RoomState => (RoomState)RoomStateValue;
        public bool MapConfigured => MapConfiguredValue != 0;

        public void Initialize(RoomState state, int redScore, int blueScore, float remainingTime, int mapIndex, int mapRevision)
        {
            Sync(state, redScore, blueScore, remainingTime);
            SetMap(mapIndex, mapRevision);
            Debug.Log("[NetworkMatchState] Initialized.");
        }

        public void SetMap(int mapIndex, int mapRevision)
        {
            MapIndex = Mathf.Max(0, mapIndex);
            MapRevision = Mathf.Max(1, mapRevision);
            MapConfiguredValue = 1;
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
