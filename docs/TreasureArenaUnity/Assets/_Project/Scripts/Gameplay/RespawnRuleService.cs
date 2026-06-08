using TreasureArenaMR.Shared;

namespace TreasureArenaMR.Gameplay
{
    /// <summary>
    /// Pure GhostRetreat and respawn countdown rules. It never teleports the player.
    /// </summary>
    public sealed class RespawnRuleService
    {
        public bool CanStartRespawn(PlayerRuntimeState player, bool isInOwnBase)
        {
            return player != null
                && player.state == PlayerState.GhostRetreat
                && isInOwnBase;
        }

        public RespawnRuleResult StartRespawn(PlayerRuntimeState player, bool isInOwnBase)
        {
            RespawnRuleResult result = new RespawnRuleResult
            {
                playerId = player != null ? player.player_id : null
            };

            if (!CanStartRespawn(player, isInOwnBase))
            {
                result.failureReason = "cannot_start_respawn";
                return result;
            }

            player.state = PlayerState.Respawning;
            result.ok = true;
            result.stateAfter = player.state;
            return result;
        }

        public RespawnRuleResult CompleteRespawn(PlayerRuntimeState player, RoomConfig roomConfig)
        {
            int maxHp = roomConfig != null ? roomConfig.player_max_hp : 100;
            return CompleteRespawn(player, maxHp);
        }

        public RespawnRuleResult CompleteRespawn(PlayerRuntimeState player, int maxHp)
        {
            RespawnRuleResult result = new RespawnRuleResult
            {
                playerId = player != null ? player.player_id : null
            };

            if (player == null)
            {
                result.failureReason = "missing_player";
                return result;
            }

            if (player.state != PlayerState.Respawning)
            {
                result.failureReason = "player_not_respawning";
                return result;
            }

            player.state = PlayerState.Alive;
            player.hp = System.Math.Max(1, maxHp);
            result.ok = true;
            result.stateAfter = player.state;
            result.hpAfter = player.hp;
            return result;
        }

        public sealed class RespawnRuleResult
        {
            public bool ok;
            public string failureReason;
            public string playerId;
            public PlayerState stateAfter;
            public int hpAfter;
        }
    }
}
