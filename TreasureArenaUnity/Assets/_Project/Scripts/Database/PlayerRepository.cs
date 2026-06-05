using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreasureArenaMR.Database
{
    /// <summary>
    /// Player table CRUD via HTTP to db_api.
    /// </summary>
    public sealed class PlayerRepository
    {
        private string _base => DatabaseManager.Instance.ApiBaseUrl;

        public void InsertOrUpdate(string playerId, string nickname)
        {
            var json = $"{{\"nickname\":\"{Escape(nickname)}\"}}";
            var response = DatabaseManager.Put($"{_base}/player/{playerId}", json);
            if (!response.success)
                Debug.LogError($"[PlayerRepository] InsertOrUpdate failed: {response.error}");
        }

        public PlayerRecord GetById(string playerId)
        {
            var response = DatabaseManager.Get($"{_base}/player/{playerId}");
            if (response.success)
                return JsonUtility.FromJson<PlayerRecord>(response.body);
            return null;
        }

        public bool Exists(string playerId)
        {
            var response = DatabaseManager.Get($"{_base}/player/{playerId}/exists");
            if (response.success)
            {
                var result = JsonUtility.FromJson<ExistsResult>(response.body);
                return result.exists;
            }
            return false;
        }

        public List<PlayerRecord> GetAll()
        {
            var response = DatabaseManager.Get($"{_base}/player");
            if (response.success)
            {
                var wrapper = $"{{\"list\":{response.body}}}";
                var result = JsonUtility.FromJson<PlayerListWrapper>(wrapper);
                return result.list ?? new List<PlayerRecord>();
            }
            return new List<PlayerRecord>();
        }

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        [Serializable] private class ExistsResult { public bool exists; }
        [Serializable] private class PlayerListWrapper { public List<PlayerRecord> list; }
    }
}
