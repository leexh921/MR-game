using TreasureArenaMR.Map;
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
        public bool TryPickup(string playerId, string treasureId, RoomManager roomManager)
        {
            var player = roomManager.Players.Find(p => p.player_id == playerId);
            if (player == null)
            {
                Debug.LogWarning($"[ServerTreasureAuthority] Player not found: {playerId}");
                return false;
            }

            if (player.state != PlayerState.Alive)
            {
                Debug.LogWarning($"[ServerTreasureAuthority] Player not Alive: {playerId}");
                return false;
            }

            if (!string.IsNullOrEmpty(player.carried_treasure_id))
            {
                Debug.LogWarning($"[ServerTreasureAuthority] Player already carrying: {playerId}");
                return false;
            }

            if (!roomManager.TryGetTreasure(treasureId, out TreasureRuntimeState treasure))
            {
                Debug.LogWarning($"[ServerTreasureAuthority] Treasure not found: {treasureId}");
                return false;
            }

            if (treasure.state != TreasureState.Spawned && treasure.state != TreasureState.Dropped)
            {
                Debug.LogWarning($"[ServerTreasureAuthority] Treasure not available: {treasureId} state={treasure.state}");
                return false;
            }

            if (!roomManager.TryGetPlayerPosition(playerId, out Vector3 playerPosition))
            {
                Debug.LogWarning($"[ServerTreasureAuthority] Missing authoritative pose for player: {playerId}");
                return false;
            }

            float dist = Vector3.Distance(playerPosition, treasure.position);
            if (dist > _pickupDistance)
            {
                Debug.LogWarning($"[ServerTreasureAuthority] Player {playerId} too far from treasure ({dist:F1} > {_pickupDistance})");
                return false;
            }

            player.carried_treasure_id = treasureId;
            roomManager.MarkTreasureCarried(treasureId, playerId);
            Debug.Log($"[ServerTreasureAuthority] Player {playerId} picked up treasure {treasureId}");
            return true;
        }

        public bool TryPickupNearest(string playerId, RoomManager roomManager)
        {
            if (roomManager == null || roomManager.MapData == null)
            {
                return false;
            }

            if (!roomManager.TryGetPlayerPosition(playerId, out Vector3 playerPosition))
            {
                return false;
            }

            if (!roomManager.TryFindNearestAvailableTreasure(playerPosition, out TreasureRuntimeState nearest, out float nearestDistance))
                return false;

            if (nearestDistance > _pickupDistance)
            {
                Debug.LogWarning($"[ServerTreasureAuthority] Nearest treasure too far player={playerId} distance={nearestDistance:F1} max={_pickupDistance}");
                return false;
            }

            return TryPickup(playerId, nearest.treasure_id, roomManager);
        }

        /// <summary>
        /// Drop a treasure at a world position (called when player enters GhostRetreat).
        /// </summary>
        public void DropTreasure(string treasureId, Vector3 dropPosition, RoomManager roomManager)
        {
            if (roomManager == null)
            {
                Debug.LogError($"[ServerTreasureAuthority] Cannot drop treasure {treasureId}: RoomManager is null.");
                return;
            }

            roomManager.MarkTreasureDropped(treasureId, dropPosition);
            Debug.Log($"[ServerTreasureAuthority] Treasure {treasureId} dropped at {dropPosition}");
        }

        /// <summary>
        /// Attempt to submit a carried treasure at team base. Returns true if successful.
        /// Validates: player is Alive, carrying a treasure, within submit distance of their team base.
        /// </summary>
        public bool TrySubmit(string playerId, RoomManager roomManager)
        {
            var player = roomManager.Players.Find(p => p.player_id == playerId);
            if (player == null) return false;
            if (player.state != PlayerState.Alive) return false;
            if (string.IsNullOrEmpty(player.carried_treasure_id)) return false;

            var playerTeam = player.team;
            if (playerTeam == TeamType.None) return false;

            // Validate player is within submit range of their team base
            var mapData = roomManager.MapData;
            if (mapData != null)
            {
                var teamBase = mapData.GetTeamBase(playerTeam);
                if (teamBase != null)
                {
                    if (!roomManager.TryGetPlayerPosition(playerId, out Vector3 playerPosition))
                    {
                        Debug.LogWarning($"[ServerTreasureAuthority] Missing authoritative pose for submit: {playerId}");
                        return false;
                    }

                    float dist = Vector3.Distance(playerPosition, teamBase.Position);
                    if (dist > teamBase.Radius + _submitDistance)
                    {
                        Debug.LogWarning($"[ServerTreasureAuthority] Player {playerId} too far from base ({dist:F1} > {teamBase.Radius + _submitDistance})");
                        return false;
                    }
                }
            }

            string treasureId = player.carried_treasure_id;
            if (!roomManager.TryGetTreasure(treasureId, out TreasureRuntimeState treasure))
                return false;

            int scoreValue = treasure.score_value;
            roomManager.AddScore(playerTeam, scoreValue);

            player.carried_treasure_id = "";
            roomManager.MarkTreasureSubmitted(treasureId);

            int totalScore = playerTeam == TeamType.Red ? roomManager.RedScore : roomManager.BlueScore;
            Debug.Log($"[ServerTreasureAuthority] Player {playerId} submitted {treasureId}, " +
                $"team {playerTeam} +{scoreValue}, total: {totalScore}");

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
    }
}
