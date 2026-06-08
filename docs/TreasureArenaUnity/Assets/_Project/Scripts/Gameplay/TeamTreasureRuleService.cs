using System.Collections.Generic;
using TreasureArenaMR.Shared;

namespace TreasureArenaMR.Gameplay
{
    /// <summary>
    /// Facade for pure TeamTreasure match rules. It does not load maps, sync network state, or write DB rows.
    /// </summary>
    public sealed class TeamTreasureRuleService
    {
        private readonly DamageRuleService damageRules = new DamageRuleService();
        private readonly TreasureRuleService treasureRules = new TreasureRuleService();
        private readonly RespawnRuleService respawnRules = new RespawnRuleService();

        public DamageRuleService DamageRules
        {
            get { return damageRules; }
        }

        public TreasureRuleService TreasureRules
        {
            get { return treasureRules; }
        }

        public RespawnRuleService RespawnRules
        {
            get { return respawnRules; }
        }

        public TeamTreasureRuleResult StartMatch(IList<PlayerRuntimeState> players, ref RoomState roomState)
        {
            TeamTreasureRuleResult result = new TeamTreasureRuleResult();
            if (roomState == RoomState.Playing)
            {
                result.failureReason = "match_already_started";
                return result;
            }

            if (!HasAlivePlayerOnTeam(players, TeamType.Red) || !HasAlivePlayerOnTeam(players, TeamType.Blue))
            {
                result.failureReason = "missing_team_players";
                return result;
            }

            roomState = RoomState.Playing;
            result.ok = true;
            result.roomState = roomState;
            return result;
        }

        public TeamTreasureRuleResult FinishMatch(ref RoomState roomState)
        {
            roomState = RoomState.Finished;
            return new TeamTreasureRuleResult
            {
                ok = true,
                roomState = roomState
            };
        }

        public TeamTreasureRuleResult AddScore(
            TeamType team,
            int score,
            ref int redScore,
            ref int blueScore)
        {
            TeamTreasureRuleResult result = new TeamTreasureRuleResult();
            if (score <= 0)
            {
                result.failureReason = "invalid_score";
                return result;
            }

            if (team == TeamType.Red)
            {
                redScore += score;
            }
            else if (team == TeamType.Blue)
            {
                blueScore += score;
            }
            else
            {
                result.failureReason = "invalid_team";
                return result;
            }

            result.ok = true;
            result.redScore = redScore;
            result.blueScore = blueScore;
            return result;
        }

        public TeamType GetWinnerTeam(int redScore, int blueScore)
        {
            if (redScore > blueScore)
            {
                return TeamType.Red;
            }

            if (blueScore > redScore)
            {
                return TeamType.Blue;
            }

            return TeamType.None;
        }

        public DamageRuleService.DamageRuleResult ApplyDamageAndDropTreasure(
            PlayerRuntimeState attacker,
            PlayerRuntimeState target,
            WeaponConfig weaponConfig,
            IList<TreasureRuntimeState> treasures)
        {
            DamageRuleService.DamageRuleResult result = damageRules.ApplyDamage(attacker, target, weaponConfig);
            if (!result.ok || !result.targetEnteredGhostRetreat || string.IsNullOrEmpty(result.droppedTreasureId))
            {
                return result;
            }

            TreasureRuntimeState droppedTreasure = FindTreasure(treasures, result.droppedTreasureId);
            if (droppedTreasure != null)
            {
                treasureRules.Drop(target, droppedTreasure);
            }

            return result;
        }

        public TreasureRuleService.TreasureSubmitResult SubmitTreasure(
            PlayerRuntimeState player,
            TreasureRuntimeState treasure,
            bool isInOwnBase,
            ref int redScore,
            ref int blueScore)
        {
            return treasureRules.Submit(player, treasure, isInOwnBase, ref redScore, ref blueScore);
        }

        public MatchResult CreateMatchResult(
            string matchId,
            RoomConfig roomConfig,
            int redScore,
            int blueScore,
            float duration,
            IList<PlayerMatchStat> playerStats)
        {
            MatchResult result = new MatchResult
            {
                match_id = matchId,
                room_id = roomConfig != null ? roomConfig.room_id : null,
                map_id = roomConfig != null ? roomConfig.map_id : null,
                red_score = redScore,
                blue_score = blueScore,
                winner_team = GetWinnerTeam(redScore, blueScore),
                duration = duration
            };

            if (playerStats != null)
            {
                for (int i = 0; i < playerStats.Count; i++)
                {
                    result.player_stats.Add(playerStats[i]);
                }
            }

            return result;
        }

        private static bool HasAlivePlayerOnTeam(IList<PlayerRuntimeState> players, TeamType team)
        {
            if (players == null)
            {
                return false;
            }

            for (int i = 0; i < players.Count; i++)
            {
                PlayerRuntimeState player = players[i];
                if (player != null && player.team == team && player.state == PlayerState.Alive)
                {
                    return true;
                }
            }

            return false;
        }

        private static TreasureRuntimeState FindTreasure(IList<TreasureRuntimeState> treasures, string treasureId)
        {
            if (treasures == null || string.IsNullOrEmpty(treasureId))
            {
                return null;
            }

            for (int i = 0; i < treasures.Count; i++)
            {
                TreasureRuntimeState treasure = treasures[i];
                if (treasure != null && treasure.treasure_id == treasureId)
                {
                    return treasure;
                }
            }

            return null;
        }

        public sealed class TeamTreasureRuleResult
        {
            public bool ok;
            public string failureReason;
            public RoomState roomState;
            public int redScore;
            public int blueScore;
        }
    }
}
