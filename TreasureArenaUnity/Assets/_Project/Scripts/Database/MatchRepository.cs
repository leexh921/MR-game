using System;
using System.Collections.Generic;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Database
{
    /// <summary>
    /// Match_result and player_match_stat persistence via HTTP to db_api.
    /// </summary>
    public sealed class MatchRepository
    {
        private string _base => DatabaseManager.Instance.ApiBaseUrl;

        public void InsertMatchResult(MatchResult result)
        {
            var body = new MatchInsertBody
            {
                match_id = result.match_id,
                room_id = result.room_id,
                map_id = result.map_id,
                red_score = result.red_score,
                blue_score = result.blue_score,
                winner_team = result.winner_team,
                duration = result.duration,
                started_at = result.started_at,
                ended_at = result.ended_at,
                player_stats = new List<StatBody>()
            };

            foreach (var stat in result.player_stats)
            {
                body.player_stats.Add(new StatBody
                {
                    player_id = stat.player_id,
                    team = stat.team,
                    kills = stat.kills,
                    deaths = stat.deaths,
                    treasures_submitted = stat.treasures_submitted,
                    score_contribution = stat.score_contribution
                });
            }

            var json = JsonUtility.ToJson(body);
            var response = DatabaseManager.Post($"{_base}/match", json);
            if (!response.success)
                Debug.LogError($"[MatchRepository] InsertMatchResult failed: {response.error}");
            else
                Debug.Log($"[MatchRepository] Match saved: {result.match_id}");
        }

        public MatchResultRecord GetMatchById(string matchId)
        {
            var response = DatabaseManager.Get($"{_base}/match/{matchId}");
            if (response.success)
            {
                var wrapper = JsonUtility.FromJson<MatchDetailWrapper>(response.body);
                return wrapper.match;
            }
            return null;
        }

        public List<PlayerMatchStatRecord> GetPlayerStatsForMatch(string matchId)
        {
            var response = DatabaseManager.Get($"{_base}/match/{matchId}");
            if (response.success)
            {
                var wrapper = JsonUtility.FromJson<MatchDetailWrapper>(response.body);
                return wrapper.player_stats ?? new List<PlayerMatchStatRecord>();
            }
            return new List<PlayerMatchStatRecord>();
        }

        public List<MatchResultRecord> GetAllMatches()
        {
            var response = DatabaseManager.Get($"{_base}/match");
            if (response.success)
            {
                var wrapper = $"{{\"list\":{response.body}}}";
                var result = JsonUtility.FromJson<MatchListWrapper>(wrapper);
                return result.list ?? new List<MatchResultRecord>();
            }
            return new List<MatchResultRecord>();
        }

        [Serializable]
        private class MatchInsertBody
        {
            public string match_id;
            public string room_id;
            public string map_id;
            public int red_score;
            public int blue_score;
            public string winner_team;
            public float duration;
            public string started_at;
            public string ended_at;
            public List<StatBody> player_stats;
        }

        [Serializable]
        private class StatBody
        {
            public string player_id;
            public string team;
            public int kills;
            public int deaths;
            public int treasures_submitted;
            public int score_contribution;
        }

        [Serializable]
        private class MatchDetailWrapper
        {
            public MatchResultRecord match;
            public List<PlayerMatchStatRecord> player_stats;
        }

        [Serializable] private class MatchListWrapper { public List<MatchResultRecord> list; }
    }
}
