using TreasureArenaMR.Shared;

namespace TreasureArenaMR.Gameplay
{
    /// <summary>
    /// Pure treasure rules for pickup, drop, and submit. Position/base checks are provided by callers.
    /// </summary>
    public sealed class TreasureRuleService
    {
        public bool CanPlayerPickup(PlayerRuntimeState player)
        {
            return player != null
                && player.state == PlayerState.Alive
                && player.team != TeamType.None
                && string.IsNullOrEmpty(player.carried_treasure_id);
        }

        public bool CanTreasureBePickedUp(TreasureRuntimeState treasure)
        {
            return treasure != null
                && (treasure.state == TreasureState.Spawned || treasure.state == TreasureState.Dropped)
                && string.IsNullOrEmpty(treasure.carrier_player_id);
        }

        public TreasureRuleResult Pickup(PlayerRuntimeState player, TreasureRuntimeState treasure)
        {
            TreasureRuleResult result = new TreasureRuleResult
            {
                playerId = player != null ? player.player_id : null,
                treasureId = treasure != null ? treasure.treasure_id : null
            };

            if (!CanPlayerPickup(player))
            {
                result.failureReason = "player_cannot_pickup";
                return result;
            }

            if (!CanTreasureBePickedUp(treasure))
            {
                result.failureReason = "treasure_cannot_be_picked";
                return result;
            }

            player.carried_treasure_id = treasure.treasure_id;
            treasure.state = TreasureState.Carried;
            treasure.carrier_player_id = player.player_id;
            result.ok = true;
            return result;
        }

        public TreasureRuleResult Drop(PlayerRuntimeState player, TreasureRuntimeState treasure)
        {
            TreasureRuleResult result = new TreasureRuleResult
            {
                playerId = player != null ? player.player_id : null,
                treasureId = treasure != null ? treasure.treasure_id : null
            };

            if (player == null || treasure == null)
            {
                result.failureReason = "missing_player_or_treasure";
                return result;
            }

            if (treasure.state != TreasureState.Carried || treasure.carrier_player_id != player.player_id)
            {
                result.failureReason = "treasure_not_carried_by_player";
                return result;
            }

            treasure.state = TreasureState.Dropped;
            treasure.carrier_player_id = null;
            treasure.position = player.position;

            if (player.carried_treasure_id == treasure.treasure_id)
            {
                player.carried_treasure_id = null;
            }

            result.ok = true;
            return result;
        }

        public TreasureSubmitResult Submit(
            PlayerRuntimeState player,
            TreasureRuntimeState treasure,
            bool isInOwnBase,
            ref int redScore,
            ref int blueScore)
        {
            TreasureSubmitResult result = new TreasureSubmitResult
            {
                playerId = player != null ? player.player_id : null,
                treasureId = treasure != null ? treasure.treasure_id : null
            };

            if (player == null || treasure == null)
            {
                result.failureReason = "missing_player_or_treasure";
                return result;
            }

            if (player.state != PlayerState.Alive)
            {
                result.failureReason = "player_not_alive";
                return result;
            }

            if (!isInOwnBase)
            {
                result.failureReason = "not_in_own_base";
                return result;
            }

            if (string.IsNullOrEmpty(player.carried_treasure_id)
                || player.carried_treasure_id != treasure.treasure_id)
            {
                result.failureReason = "player_not_carrying_treasure";
                return result;
            }

            if (treasure.state != TreasureState.Carried || treasure.carrier_player_id != player.player_id)
            {
                result.failureReason = "treasure_not_carried_by_player";
                return result;
            }

            result.scoreAdded = treasure.score_value;
            if (player.team == TeamType.Red)
            {
                redScore += result.scoreAdded;
            }
            else if (player.team == TeamType.Blue)
            {
                blueScore += result.scoreAdded;
            }
            else
            {
                result.failureReason = "invalid_team";
                return result;
            }

            treasure.state = TreasureState.Submitted;
            treasure.carrier_player_id = null;
            player.carried_treasure_id = null;

            result.ok = true;
            result.team = player.team;
            result.redScore = redScore;
            result.blueScore = blueScore;
            return result;
        }

        public sealed class TreasureRuleResult
        {
            public bool ok;
            public string failureReason;
            public string playerId;
            public string treasureId;
        }

        public sealed class TreasureSubmitResult
        {
            public bool ok;
            public string failureReason;
            public string playerId;
            public string treasureId;
            public TeamType team = TeamType.None;
            public int scoreAdded;
            public int redScore;
            public int blueScore;
        }
    }
}
