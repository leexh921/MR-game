using TreasureArenaMR.Map;
using TreasureArenaMR.Network;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Server
{
    /// <summary>
    /// Server-authoritative treasure logic: spawn, pickup validation,
    /// drop on GhostRetreat, submit validation, and score awarding.
    /// 
    /// Clients send pickup/submit requests; this class decides outcomes.
    /// </summary>
    public sealed class ServerTreasureAuthority : MonoBehaviour
    {
        [Header("Pickup Settings")]
        [SerializeField] private float _pickupDistance = 2.0f;
        [SerializeField] private float _submitDistance = 3.0f;

        /// <summary>
        /// Attempt to pick up a treasure. Returns true if successful.
        /// Validates: player is Alive, not already carrying, treasure is Spawned,
        /// player is within pickup range.
        /// </summary>
        public bool TryPickup(string playerId, string treasureId, Vector3 treasurePosition, RoomManager roomManager)
        {
            var player = roomManager.Players.Find(p => p.player_id == playerId);
            if (player == null)
            {
                Debug.LogWarning($"[ServerTreasureAuthority] Player not found: {playerId}");
                return false;
            }

            if (player.state != "Alive")
            {
                Debug.LogWarning($"[ServerTreasureAuthority] Player not Alive: {playerId}");
                return false;
            }

            if (!string.IsNullOrEmpty(player.carried_treasure_id))
            {
                Debug.LogWarning($"[ServerTreasureAuthority] Player already carrying: {playerId}");
                return false;
            }

            // Validate distance between player and treasure
            var netPlayer = roomManager.GetNetickPlayer(playerId);
            if (netPlayer != null)
            {
                var playerObj = netPlayer.PlayerObject;
                if (playerObj != null)
                {
                    float dist = Vector3.Distance(playerObj.transform.position, treasurePosition);
                    if (dist > _pickupDistance)
                    {
                        Debug.LogWarning($"[ServerTreasureAuthority] Player {playerId} too far from treasure ({dist:F1} > {_pickupDistance})");
                        return false;
                    }
                }
            }

            player.carried_treasure_id = treasureId;
            Debug.Log($"[ServerTreasureAuthority] Player {playerId} picked up treasure {treasureId}");
            return true;
        }

        /// <summary>
        /// Drop a treasure at a world position (called when player enters GhostRetreat).
        /// </summary>
        public void DropTreasure(string treasureId, Vector3 dropPosition)
        {
            Debug.Log($"[ServerTreasureAuthority] Treasure {treasureId} dropped at {dropPosition}");
            // TODO: Set treasure state to Dropped and position to dropPosition
        }

        /// <summary>
        /// Attempt to submit a carried treasure at team base. Returns true if successful.
        /// Validates: player is Alive, carrying a treasure, within submit distance of their team base.
        /// </summary>
        public bool TrySubmit(string playerId, RoomManager roomManager)
        {
            var player = roomManager.Players.Find(p => p.player_id == playerId);
            if (player == null) return false;
            if (player.state != "Alive") return false;
            if (string.IsNullOrEmpty(player.carried_treasure_id)) return false;

            string teamStr = player.team;
            if (teamStr == "None") return false;

            var teamType = teamStr == "Red" ? TeamType.Red : TeamType.Blue;

            // Validate player is within submit range of their team base
            var mapData = roomManager.MapData;
            if (mapData != null)
            {
                var teamBase = mapData.GetTeamBase(teamType);
                if (teamBase != null)
                {
                    var netPlayer = roomManager.GetNetickPlayer(playerId);
                    if (netPlayer != null && netPlayer.PlayerObject != null)
                    {
                        float dist = Vector3.Distance(netPlayer.PlayerObject.transform.position, teamBase.position);
                        if (dist > teamBase.radius + _submitDistance)
                        {
                            Debug.LogWarning($"[ServerTreasureAuthority] Player {playerId} too far from base ({dist:F1} > {teamBase.radius + _submitDistance})");
                            return false;
                        }
                    }
                }
            }

            int scoreValue = GetTreasureScore(player.carried_treasure_id, roomManager);
            roomManager.AddScore(teamType, scoreValue);

            string treasureId = player.carried_treasure_id;
            player.carried_treasure_id = "";

            int totalScore = teamStr == "Red" ? roomManager.RedScore : roomManager.BlueScore;
            Debug.Log($"[ServerTreasureAuthority] Player {playerId} submitted {treasureId}, " +
                $"team {teamStr} +{scoreValue}, total: {totalScore}");

            return true;
        }

        /// <summary>
        /// Respawn a treasure at its original spawn point.
        /// Called periodically or after submission.
        /// </summary>
        public void RespawnTreasure(string treasureId, Vector3 spawnPosition, TreasureType type)
        {
            Debug.Log($"[ServerTreasureAuthority] Respawning treasure {treasureId} at {spawnPosition}");
            // TODO: Create NetworkTreasure instance or reactivate existing
        }

        private int GetTreasureScore(string treasureId, RoomManager roomManager)
        {
            // TODO: Look up treasure type and return score from RoomConfig
            // For MVP, default to Normal treasure score
            return roomManager.CurrentRoomConfig?.normal_treasure_score ?? 10;
        }
    }
}
