using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace TreasureArenaMR.Database
{
    /// <summary>
    /// Central database HTTP client. Connects to the db_api server
    /// instead of directly to MySQL. No native DLL dependencies.
    /// </summary>
    public sealed class DatabaseManager
    {
        private static DatabaseManager _instance;
        public static DatabaseManager Instance => _instance ?? (_instance = new DatabaseManager());

        private string _apiBaseUrl = "http://127.0.0.1:5500";
        public bool IsReady { get; private set; }

        private DatabaseManager() { }

        public void Initialize()
        {
            var config = LoadDbConfig();
            if (config != null)
            {
                _apiBaseUrl = $"http://{config.host}:{config.port}";
            }

            // Test connection
            var response = Get($"{_apiBaseUrl}/health");
            if (response.success)
            {
                IsReady = true;
                Debug.Log($"[DatabaseManager] Connected to DB API: {_apiBaseUrl}");
            }
            else
            {
                Debug.LogError($"[DatabaseManager] DB API not reachable at {_apiBaseUrl}: {response.error}");
                IsReady = false;
            }
        }

        public void Shutdown()
        {
            IsReady = false;
            Debug.Log("[DatabaseManager] Shutdown");
        }

        public string ApiBaseUrl => _apiBaseUrl;

        // ---- HTTP helpers (sync, acceptable for infrequent DB operations) ----

        public static ApiResponse Get(string url)
        {
            using var request = UnityWebRequest.Get(url);
            request.timeout = 5;
            request.SendWebRequest();
            while (!request.isDone) { }
            return ApiResponse.FromRequest(request);
        }

        public static ApiResponse Put(string url, string jsonBody)
        {
            using var request = UnityWebRequest.Put(url, jsonBody);
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 5;
            request.SendWebRequest();
            while (!request.isDone) { }
            return ApiResponse.FromRequest(request);
        }

        public static ApiResponse Post(string url, string jsonBody)
        {
            using var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 5;
            request.SendWebRequest();
            while (!request.isDone) { }
            return ApiResponse.FromRequest(request);
        }

        public static ApiResponse Delete(string url)
        {
            using var request = UnityWebRequest.Delete(url);
            request.timeout = 5;
            request.SendWebRequest();
            while (!request.isDone) { }
            return ApiResponse.FromRequest(request);
        }

        private DbConfig LoadDbConfig()
        {
            string configPath = Path.Combine(Application.dataPath, "_Project", "StreamingAssets", "Configs", "db_config.json");
            if (!File.Exists(configPath))
            {
                Debug.LogWarning($"[DatabaseManager] db_config.json not found at {configPath}, using defaults");
                return null;
            }

            try
            {
                string json = File.ReadAllText(configPath);
                return JsonUtility.FromJson<DbConfig>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DatabaseManager] Failed to parse db_config.json: {ex.Message}");
                return null;
            }
        }

        [Serializable]
        private class DbConfig
        {
            public string host = "127.0.0.1";
            public int port = 5500;
            public string database = "treasure_arena";
            public string user = "";
            public string password = "";
        }
    }

    /// <summary>
    /// Simple HTTP response wrapper.
    /// </summary>
    public class ApiResponse
    {
        public bool success;
        public string body;
        public string error;
        public long httpCode;

        public static ApiResponse FromRequest(UnityWebRequest request)
        {
            return new ApiResponse
            {
                success = request.result == UnityWebRequest.Result.Success,
                body = request.downloadHandler?.text ?? "",
                error = request.error ?? "",
                httpCode = request.responseCode
            };
        }
    }

    // ---- Data records (shared between repositories) ----

    [Serializable]
    public class PlayerRecord
    {
        public string player_id;
        public string nickname;
        public string created_at;
    }

    [Serializable]
    public class RoomConfigRecord
    {
        public string room_id;
        public string room_name;
        public string map_id;
        public string game_mode;
        public int max_players;
        public float match_time;
        public int player_max_hp;
        public int weapon_damage;
        public float respawn_countdown;
        public int normal_treasure_score;
        public int rare_treasure_score;
        public int final_treasure_score;
        public string created_at;
    }

    [Serializable]
    public class MatchResultRecord
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
    }

    [Serializable]
    public class PlayerMatchStatRecord
    {
        public string player_id;
        public string team;
        public int kills;
        public int deaths;
        public int treasures_submitted;
        public int score_contribution;
    }
}
