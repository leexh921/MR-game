using System;
using System.Collections.Generic;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Database
{
    /// <summary>
    /// Room_config table CRUD via HTTP to db_api.
    /// </summary>
    public sealed class RoomRepository
    {
        private string _base => DatabaseManager.Instance.ApiBaseUrl;

        public void Insert(RoomConfig config)
        {
            var json = JsonUtility.ToJson(new RoomInsertBody
            {
                room_id = config.room_id,
                room_name = config.room_name,
                map_id = config.map_id,
                game_mode = config.game_mode,
                max_players = config.max_players,
                match_time = config.match_time,
                round_count = config.round_count,
                player_max_hp = config.player_max_hp,
                weapon_id = config.weapon_id,
                weapon_damage = config.weapon_damage,
                weapon_range = config.weapon_range,
                weapon_cooldown = config.weapon_cooldown,
                respawn_countdown = config.respawn_countdown,
                normal_treasure_score = config.normal_treasure_score,
                rare_treasure_score = config.rare_treasure_score,
                final_treasure_score = config.final_treasure_score
            });

            var response = DatabaseManager.Post($"{_base}/room", json);
            if (!response.success)
                Debug.LogError($"[RoomRepository] Insert failed: {response.error}");
        }

        public RoomConfigRecord GetById(string roomId)
        {
            var response = DatabaseManager.Get($"{_base}/room/{roomId}");
            if (response.success)
                return JsonUtility.FromJson<RoomConfigRecord>(response.body);
            return null;
        }

        public List<RoomConfigRecord> GetAll()
        {
            var response = DatabaseManager.Get($"{_base}/room");
            if (response.success)
            {
                var wrapper = $"{{\"list\":{response.body}}}";
                var result = JsonUtility.FromJson<RoomListWrapper>(wrapper);
                return result.list ?? new List<RoomConfigRecord>();
            }
            return new List<RoomConfigRecord>();
        }

        public void Delete(string roomId)
        {
            var response = DatabaseManager.Delete($"{_base}/room/{roomId}");
            if (!response.success)
                Debug.LogError($"[RoomRepository] Delete failed: {response.error}");
        }

        [Serializable]
        private class RoomInsertBody
        {
            public string room_id;
            public string room_name;
            public string map_id;
            public string game_mode;
            public int max_players;
            public float match_time;
            public int round_count;
            public int player_max_hp;
            public string weapon_id;
            public int weapon_damage;
            public float weapon_range;
            public float weapon_cooldown;
            public float respawn_countdown;
            public int normal_treasure_score;
            public int rare_treasure_score;
            public int final_treasure_score;
        }

        [Serializable] private class RoomListWrapper { public List<RoomConfigRecord> list; }
    }
}
