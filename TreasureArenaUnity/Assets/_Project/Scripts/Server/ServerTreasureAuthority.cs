using System.Collections.Generic;
using TreasureArenaMR.Map;
using TreasureArenaMR.Network;
using TreasureArenaMR.Shared;
using UnityEngine;
using GameNetworkPlayer = TreasureArenaMR.Network.NetworkPlayer;

namespace TreasureArenaMR.Server
{
    /// <summary>
    /// Server-authoritative treasure spawning, pickup, drop, submit, and scoring.
    /// </summary>
    public sealed class ServerTreasureAuthority : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject _treasurePrefab;

        [Header("Ranges")]
        [SerializeField] private float _pickupDistance = 2.0f;
        [SerializeField] private float _submitDistance = 3.0f;

        private readonly List<NetworkTreasure> _treasures = new List<NetworkTreasure>();

        public IReadOnlyList<NetworkTreasure> Treasures => _treasures;

        public void SpawnTreasures(NetworkManager networkManager, RoomManager roomManager)
        {
            if (_treasures.Count > 0)
                return;
            if (networkManager == null || networkManager.Sandbox == null || !networkManager.IsServer)
            {
                Debug.LogWarning("[ServerTreasureAuthority] Cannot spawn treasures before server sandbox is ready.");
                return;
            }
            if (roomManager == null || roomManager.MapData == null)
            {
                Debug.LogWarning("[ServerTreasureAuthority] MapData not loaded; treasures were not spawned.");
                return;
            }

            if (_treasurePrefab == null)
                _treasurePrefab = Resources.Load<GameObject>("TreasurePrefab");
            if (_treasurePrefab == null)
            {
                Debug.LogError("[ServerTreasureAuthority] NetworkTreasure prefab null. Expected Resources/TreasurePrefab.");
                return;
            }

            List<TreasureSpawnPoint> points = roomManager.MapData.GetTreasureSpawnPoints();
            for (int i = 0; i < points.Count; i++)
            {
                TreasureSpawnPoint point = points[i];
                int score = GetTreasureScore(point.TreasureType, roomManager);
                var treasureObject = networkManager.Sandbox.NetworkInstantiate(
                    _treasurePrefab,
                    point.Position,
                    Quaternion.identity);

                var treasure = treasureObject.GetComponent<NetworkTreasure>();
                if (treasure == null)
                {
                    Debug.LogError("[ServerTreasureAuthority] Treasure prefab missing NetworkTreasure.");
                    continue;
                }

                string treasureId = string.IsNullOrEmpty(point.PointId) ? "treasure_" + i : point.PointId;
                treasure.Initialize(i, treasureId, point.TreasureType, point.Position, score);
                _treasures.Add(treasure);
            }

            Debug.Log("[ServerTreasureAuthority] Spawned treasures: " + _treasures.Count);
        }

        public bool TryPickupNearest(string playerId, RoomManager roomManager)
        {
            if (!TryGetAliveCarrierCandidate(playerId, roomManager, out PlayerInfo player, out GameNetworkPlayer networkPlayer))
                return false;

            if (!string.IsNullOrEmpty(player.carried_treasure_id) || networkPlayer.HasTreasure)
            {
                Debug.LogWarning("[ServerTreasureAuthority] Player already carrying: " + playerId);
                return false;
            }

            NetworkTreasure nearest = FindNearestAvailableTreasure(networkPlayer.transform.position);
            if (nearest == null)
            {
                Debug.LogWarning("[ServerTreasureAuthority] No treasure in pickup range for: " + playerId);
                return false;
            }

            player.carried_treasure_id = nearest.TreasureId;
            networkPlayer.SetCarriedTreasure(nearest);
            nearest.SetCarried(networkPlayer);

            Debug.Log("[ServerTreasureAuthority] Player " + playerId + " picked up treasure " + nearest.TreasureId);
            return true;
        }

        public bool TrySubmit(string playerId, RoomManager roomManager)
        {
            if (!TryGetAliveCarrierCandidate(playerId, roomManager, out PlayerInfo player, out GameNetworkPlayer networkPlayer))
                return false;

            if (string.IsNullOrEmpty(player.carried_treasure_id) || !networkPlayer.HasTreasure)
            {
                Debug.LogWarning("[ServerTreasureAuthority] Player not carrying treasure: " + playerId);
                return false;
            }

            if (player.team == TeamType.None)
                return false;

            MapZone teamBase = roomManager.MapData?.GetTeamBase(player.team);
            if (teamBase == null)
                return false;

            float dist = Vector3.Distance(networkPlayer.transform.position, teamBase.Position);
            if (dist > teamBase.Radius + _submitDistance)
            {
                Debug.LogWarning("[ServerTreasureAuthority] Player " + playerId + " too far from base: " + dist.ToString("F1"));
                return false;
            }

            NetworkTreasure treasure = FindTreasure(player.carried_treasure_id);
            int score = treasure != null ? treasure.Score : GetTreasureScore(networkPlayer.CarriedTreasureType, roomManager);

            roomManager.AddScore(player.team, score);
            if (treasure != null)
                treasure.SetSubmitted();

            string treasureId = player.carried_treasure_id;
            player.carried_treasure_id = "";
            networkPlayer.ClearCarriedTreasure();

            int totalScore = player.team == TeamType.Red ? roomManager.RedScore : roomManager.BlueScore;
            Debug.Log("[ServerTreasureAuthority] Player " + playerId + " submitted " + treasureId
                + ", team " + player.team + " +" + score + ", total: " + totalScore);
            return true;
        }

        public void DropPlayerTreasure(string playerId, Vector3 dropPosition, RoomManager roomManager)
        {
            if (roomManager == null)
                return;

            PlayerInfo player = roomManager.Players.Find(p => p.player_id == playerId);
            if (player == null || string.IsNullOrEmpty(player.carried_treasure_id))
                return;

            NetworkTreasure treasure = FindTreasure(player.carried_treasure_id);
            if (treasure != null)
                treasure.SetDropped(dropPosition);

            var networkPlayer = roomManager.GetNetworkPlayerComponent(playerId);
            if (networkPlayer != null)
                networkPlayer.ClearCarriedTreasure();

            Debug.Log("[ServerTreasureAuthority] Treasure " + player.carried_treasure_id + " dropped at " + dropPosition);
            player.carried_treasure_id = "";
        }

        private bool TryGetAliveCarrierCandidate(
            string playerId,
            RoomManager roomManager,
            out PlayerInfo player,
            out GameNetworkPlayer networkPlayer)
        {
            player = null;
            networkPlayer = null;

            if (roomManager == null || roomManager.Players == null)
                return false;

            player = roomManager.Players.Find(p => p.player_id == playerId);
            if (player == null)
            {
                Debug.LogWarning("[ServerTreasureAuthority] Player not found: " + playerId);
                return false;
            }

            if (player.state != PlayerState.Alive)
            {
                Debug.LogWarning("[ServerTreasureAuthority] Player not Alive: " + playerId);
                return false;
            }

            networkPlayer = roomManager.GetNetworkPlayerComponent(playerId);
            if (networkPlayer == null)
            {
                Debug.LogWarning("[ServerTreasureAuthority] NetworkPlayer not found: " + playerId);
                return false;
            }

            return true;
        }

        private NetworkTreasure FindNearestAvailableTreasure(Vector3 playerPosition)
        {
            NetworkTreasure nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < _treasures.Count; i++)
            {
                NetworkTreasure treasure = _treasures[i];
                if (treasure == null)
                    continue;
                if (treasure.State != TreasureState.Spawned && treasure.State != TreasureState.Dropped)
                    continue;

                float dist = Vector3.Distance(playerPosition, treasure.transform.position);
                if (dist <= _pickupDistance && dist < nearestDistance)
                {
                    nearest = treasure;
                    nearestDistance = dist;
                }
            }

            return nearest;
        }

        private NetworkTreasure FindTreasure(string treasureId)
        {
            for (int i = 0; i < _treasures.Count; i++)
            {
                NetworkTreasure treasure = _treasures[i];
                if (treasure != null && treasure.TreasureId == treasureId)
                    return treasure;
            }

            return null;
        }

        private int GetTreasureScore(TreasureType type, RoomManager roomManager)
        {
            TreasureScores scores = roomManager.CurrentRoomConfig?.treasure_scores;
            if (scores == null)
                return 10;

            switch (type)
            {
                case TreasureType.Rare:
                    return scores.Rare;
                case TreasureType.Final:
                    return scores.Final;
                default:
                    return scores.Normal;
            }
        }
    }
}
