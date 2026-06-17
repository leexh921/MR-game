using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Server
{
    public sealed class ServerPlayerRuntimeState
    {
        public string playerId;
        public string nickname;
        public TeamType team = TeamType.None;
        public PlayerState state = PlayerState.Alive;
        public int hp;
        public int maxHp;
        public bool isConnected = true;
        public string carriedTreasureId = "";
        public Vector3 position;
        public float rotationY;
        public long lastPoseServerTimeMs;
    }
}
