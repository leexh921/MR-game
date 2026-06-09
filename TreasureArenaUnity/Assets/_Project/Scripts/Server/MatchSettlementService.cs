using System;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Server
{
    /// <summary>
    /// Handles match finish: winner calculation and result packaging.
    /// No database persistence in MVP — results are logged to Console only.
    /// </summary>
    public sealed class MatchSettlementService : MonoBehaviour
    {
        public MatchResult SettleMatch(RoomManager roomManager)
        {
            var config = roomManager.CurrentRoomConfig;
            var result = new MatchResult
            {
                match_id = GenerateMatchId(),
                room_id = config.room_id,
                map_id = config.map_id,
                red_score = roomManager.RedScore,
                blue_score = roomManager.BlueScore,
                duration = config.match_time - roomManager.RemainingTime,
                started_at = config.created_at,
                ended_at = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // Determine winner
            if (result.red_score > result.blue_score)
                result.winner_team = "Red";
            else if (result.blue_score > result.red_score)
                result.winner_team = "Blue";
            else
                result.winner_team = "Draw";

            // Build player stats (MVP: placeholder stats)
            foreach (var player in roomManager.Players)
            {
                result.player_stats.Add(new PlayerMatchStat
                {
                    player_id = player.player_id,
                    team = player.team,
                    kills = 0,    // TODO: Track kills during match
                    deaths = 0,   // TODO: Track deaths during match
                    treasures_submitted = 0, // TODO: Track per player
                    score_contribution = 0   // TODO: Track per player
                });
            }

            Debug.Log($"[MatchSettlementService] Match {result.match_id} settled: " +
                $"Red {result.red_score} - Blue {result.blue_score}, Winner: {result.winner_team}");

            return result;
        }

        public void SaveMatchResult(MatchResult result)
        {
            Debug.Log($"[MatchSettlementService] Match result (not persisted): {result.match_id} " +
                $"Red {result.red_score} - Blue {result.blue_score}, Winner: {result.winner_team}");
        }

        private string GenerateMatchId()
        {
            return $"match_{DateTime.UtcNow:yyyyMMddHHmmss}_{UnityEngine.Random.Range(0, 9999):D4}";
        }
    }
}
