using System;
using System.Collections.Generic;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Network
{
    public static class SimpleNetworkProtocol
    {
        public const int Version = 1;
        public const string DefaultRoomId = "room_default";
        public const int TcpPort = 7777;
        public const int UdpPort = 7778;
    }

    [Serializable]
    public sealed class SimpleNetworkEnvelope
    {
        public int protocol_version = SimpleNetworkProtocol.Version;
        public string type;
        public int seq;
        public string room_id;
        public string player_id;
        public long sent_at_ms;
        public string payload_json;
    }

    [Serializable]
    public sealed class JoinRoomRequestPayload
    {
        public string nickname;
        public string client_type;
    }

    [Serializable]
    public sealed class JoinRoomResultPayload
    {
        public bool ok;
        public string error_code;
        public string message;
        public string player_id;
        public string room_id;
        public string team;
    }

    [Serializable]
    public sealed class SwitchTeamRequestPayload
    {
        public string target_team;
    }

    [Serializable]
    public sealed class StartMatchRequestPayload
    {
    }

    [Serializable]
    public sealed class PickupTreasureRequestPayload
    {
        public string treasure_id;
    }

    [Serializable]
    public sealed class SubmitTreasureRequestPayload
    {
    }

    [Serializable]
    public sealed class AttackRequestPayload
    {
        public string weapon_id;
        public SimpleVector3 origin;
        public SimpleVector3 direction;
        public float client_time;
    }

    [Serializable]
    public sealed class CommandResultPayload
    {
        public bool ok;
        public string error_code;
        public string message;
    }

    [Serializable]
    public sealed class DisconnectNoticePayload
    {
        public string player_id;
        public string reason;
    }

    [Serializable]
    public sealed class PlayerPosePayload
    {
        public float x;
        public float y;
        public float z;
        public float rotation_y;
        public int pose_seq;
    }

    [Serializable]
    public sealed class MatchSnapshotPayload
    {
        public string room_id;
        public string room_state;
        public string map_id;
        public int map_revision;
        public float remaining_time;
        public int red_score;
        public int blue_score;
        public List<PlayerSnapshot> players = new List<PlayerSnapshot>();
        public List<TreasureSnapshot> treasures = new List<TreasureSnapshot>();
    }

    [Serializable]
    public sealed class PoseSnapshotPayload
    {
        public string room_id;
        public List<PlayerPoseSnapshot> players = new List<PlayerPoseSnapshot>();
    }

    [Serializable]
    public sealed class PlayerSnapshot
    {
        public string player_id;
        public string nickname;
        public string team;
        public string state;
        public int hp;
        public int max_hp;
        public bool is_connected;
        public string carried_treasure_id;
        public SimpleVector3 position;
        public float rotation_y;
    }

    [Serializable]
    public sealed class PlayerPoseSnapshot
    {
        public string player_id;
        public SimpleVector3 position;
        public float rotation_y;
        public long server_time_ms;
        public bool stale;
    }

    [Serializable]
    public sealed class TreasureSnapshot
    {
        public string treasure_id;
        public string treasure_type;
        public string state;
        public int score_value;
        public SimpleVector3 position;
        public string carrier_player_id;
    }

    [Serializable]
    public sealed class SimpleVector3
    {
        public float x;
        public float y;
        public float z;

        public SimpleVector3()
        {
        }

        public SimpleVector3(Vector3 value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    public static class SimpleNetworkJson
    {
        public static SimpleNetworkEnvelope CreateEnvelope(string type, int seq, string roomId, string playerId, object payload)
        {
            return new SimpleNetworkEnvelope
            {
                protocol_version = SimpleNetworkProtocol.Version,
                type = type,
                seq = seq,
                room_id = string.IsNullOrEmpty(roomId) ? SimpleNetworkProtocol.DefaultRoomId : roomId,
                player_id = playerId ?? string.Empty,
                sent_at_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                payload_json = payload != null ? JsonUtility.ToJson(payload) : "{}"
            };
        }

        public static T ReadPayload<T>(SimpleNetworkEnvelope envelope) where T : new()
        {
            if (envelope == null || string.IsNullOrEmpty(envelope.payload_json))
                return new T();

            return JsonUtility.FromJson<T>(envelope.payload_json);
        }

        public static string ToJson(SimpleNetworkEnvelope envelope)
        {
            return JsonUtility.ToJson(envelope);
        }

        public static bool TryParseEnvelope(string json, out SimpleNetworkEnvelope envelope, out string error)
        {
            envelope = null;
            error = null;
            if (string.IsNullOrEmpty(json))
            {
                error = "empty_message";
                return false;
            }

            try
            {
                envelope = JsonUtility.FromJson<SimpleNetworkEnvelope>(json);
            }
            catch (Exception ex)
            {
                error = "invalid_json: " + ex.Message;
                return false;
            }

            if (envelope == null || envelope.protocol_version != SimpleNetworkProtocol.Version)
            {
                error = "protocol_version_mismatch";
                return false;
            }

            if (string.IsNullOrEmpty(envelope.type))
            {
                error = "missing_type";
                return false;
            }

            return true;
        }

        public static TeamType ParseTeam(string value)
        {
            if (value == "Red")
                return TeamType.Red;
            if (value == "Blue")
                return TeamType.Blue;
            return TeamType.None;
        }
    }
}
