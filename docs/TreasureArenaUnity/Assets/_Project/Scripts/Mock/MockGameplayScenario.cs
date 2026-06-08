using System.Collections.Generic;
using TreasureArenaMR.Gameplay;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Mock
{
    /// <summary>
    /// Local smoke scenario for pure gameplay rules. It is not a Unity Test Framework test and has no UI dependency.
    /// </summary>
    public static class MockGameplayScenario
    {
        public static MockGameplaySmokeResult RunSmoke()
        {
            MockRoomFactory roomFactory = new MockRoomFactory();
            MockRoomState room = roomFactory.CreateRoom("mock_rule_room", "Mock Rule Room", "test_map_01");
            room.config.weapon_config.damage = 100;

            PlayerRuntimeState redPlayer = CreateRuntimePlayer("red_runtime", TeamType.Red, room.config.player_max_hp, new Vector3(-1f, 0f, 0f));
            PlayerRuntimeState bluePlayer = CreateRuntimePlayer("blue_runtime", TeamType.Blue, room.config.player_max_hp, new Vector3(1f, 0f, 0f));
            TreasureRuntimeState treasure = new TreasureRuntimeState
            {
                treasure_id = "treasure_mock_01",
                treasure_type = TreasureType.Normal,
                state = TreasureState.Spawned,
                score_value = room.config.treasure_scores.Normal,
                position = Vector3.zero,
                carrier_player_id = null
            };

            List<PlayerRuntimeState> players = new List<PlayerRuntimeState> { redPlayer, bluePlayer };
            List<TreasureRuntimeState> treasures = new List<TreasureRuntimeState> { treasure };
            TeamTreasureRuleService rules = new TeamTreasureRuleService();
            RoomState roomState = room.state;
            int redScore = room.redScore;
            int blueScore = room.blueScore;

            TeamTreasureRuleService.TeamTreasureRuleResult startResult = rules.StartMatch(players, ref roomState);
            TreasureRuleService.TreasureRuleResult redPickupResult = rules.TreasureRules.Pickup(redPlayer, treasure);
            DamageRuleService.DamageRuleResult damageResult = rules.ApplyDamageAndDropTreasure(
                bluePlayer,
                redPlayer,
                room.config.weapon_config,
                treasures);
            bool redWasGhostAfterDamage = redPlayer.state == PlayerState.GhostRetreat;
            bool treasureWasDroppedAfterDamage = treasure.state == TreasureState.Dropped;
            RespawnRuleService.RespawnRuleResult startRespawnResult = rules.RespawnRules.StartRespawn(redPlayer, true);
            RespawnRuleService.RespawnRuleResult completeRespawnResult = rules.RespawnRules.CompleteRespawn(redPlayer, room.config);
            TreasureRuleService.TreasureRuleResult bluePickupResult = rules.TreasureRules.Pickup(bluePlayer, treasure);
            TreasureRuleService.TreasureSubmitResult submitResult = rules.SubmitTreasure(
                bluePlayer,
                treasure,
                true,
                ref redScore,
                ref blueScore);
            TeamTreasureRuleService.TeamTreasureRuleResult finishResult = rules.FinishMatch(ref roomState);

            List<PlayerMatchStat> stats = new List<PlayerMatchStat>
            {
                new PlayerMatchStat
                {
                    player_id = redPlayer.player_id,
                    team = redPlayer.team,
                    deaths = damageResult.targetEnteredGhostRetreat ? 1 : 0
                },
                new PlayerMatchStat
                {
                    player_id = bluePlayer.player_id,
                    team = bluePlayer.team,
                    kills = damageResult.targetEnteredGhostRetreat ? 1 : 0,
                    treasures_submitted = submitResult.ok ? 1 : 0,
                    score_contribution = submitResult.scoreAdded
                }
            };

            MatchResult matchResult = rules.CreateMatchResult(
                "mock_match_001",
                room.config,
                redScore,
                blueScore,
                room.config.match_time,
                stats);

            return new MockGameplaySmokeResult
            {
                startMatchOk = startResult.ok,
                redPickupOk = redPickupResult.ok,
                damageOk = damageResult.ok,
                redEnteredGhostRetreat = redWasGhostAfterDamage && damageResult.targetEnteredGhostRetreat,
                treasureDroppedAfterDamage = treasureWasDroppedAfterDamage,
                startRespawnOk = startRespawnResult.ok,
                completeRespawnOk = completeRespawnResult.ok,
                bluePickupOk = bluePickupResult.ok,
                submitOk = submitResult.ok,
                finishMatchOk = finishResult.ok,
                finalRedState = redPlayer.state,
                finalRedHp = redPlayer.hp,
                finalTreasureState = treasure.state,
                redScore = redScore,
                blueScore = blueScore,
                matchResult = matchResult
            };
        }

        private static PlayerRuntimeState CreateRuntimePlayer(string playerId, TeamType team, int hp, Vector3 position)
        {
            return new PlayerRuntimeState
            {
                player_id = playerId,
                team = team,
                state = PlayerState.Alive,
                hp = hp,
                position = position,
                rotation_y = 0f,
                carried_treasure_id = null
            };
        }
    }

    public sealed class MockGameplaySmokeResult
    {
        public bool startMatchOk;
        public bool redPickupOk;
        public bool damageOk;
        public bool redEnteredGhostRetreat;
        public bool treasureDroppedAfterDamage;
        public bool startRespawnOk;
        public bool completeRespawnOk;
        public bool bluePickupOk;
        public bool submitOk;
        public bool finishMatchOk;
        public PlayerState finalRedState;
        public int finalRedHp;
        public TreasureState finalTreasureState;
        public int redScore;
        public int blueScore;
        public MatchResult matchResult;
    }
}
