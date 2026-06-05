using System.Collections.Generic;
using TreasureArenaMR.Network;
using TreasureArenaMR.Shared;
using UnityEngine;
using NetworkPlayer = TreasureArenaMR.Network.NetworkPlayer;

namespace TreasureArenaMR.Server
{
    /// <summary>
    /// Server-authoritative combat logic: hit detection, damage calculation,
    /// HP reduction, and GhostRetreat state transitions.
    /// 
    /// Clients send attack requests; this class decides hit/miss/damage.
    /// </summary>
    public sealed class ServerCombatAuthority : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _hitRadius = 0.5f;

        /// <summary>
        /// Process an attack request from a client.
        /// Returns the result: hit target + damage, or miss.
        /// </summary>
        public AttackResult ProcessAttack(string attackerId, string weaponId,
            Vector3 origin, Vector3 direction, int weaponDamage, float weaponRange)
        {
            var result = new AttackResult
            {
                attacker_player_id = attackerId,
                damage = 0
            };

            // Find targets within range and hit radius
            var hitPlayerId = RaycastHit(attackerId, origin, direction, weaponRange);

            if (!string.IsNullOrEmpty(hitPlayerId))
            {
                result.target_player_id = hitPlayerId;
                result.damage = weaponDamage;
                result.hit = true;
            }

            Debug.Log($"[ServerCombatAuthority] Attack from {attackerId}: " +
                $"{(result.hit ? $"hit {hitPlayerId} for {weaponDamage}" : "miss")}");

            return result;
        }

        /// <summary>
        /// Apply damage to a target player. Returns new HP.
        /// Returns -1 if player not found or already in GhostRetreat.
        /// </summary>
        public int ApplyDamage(string targetPlayerId, int damage, RoomManager roomManager)
        {
            var player = roomManager.Players.Find(p => p.player_id == targetPlayerId);
            if (player == null) return -1;
            if (player.state != "Alive") return -1;

            player.hp -= damage;
            if (player.hp <= 0)
            {
                player.hp = 0;
                player.state = "GhostRetreat";

                // Drop carried treasure
                if (!string.IsNullOrEmpty(player.carried_treasure_id))
                {
                    // TODO: Notify ServerTreasureAuthority to drop treasure at player position
                    Debug.Log($"[ServerCombatAuthority] Player {targetPlayerId} entered GhostRetreat, dropped {player.carried_treasure_id}");
                    player.carried_treasure_id = "";
                }
            }

            return player.hp;
        }

        private string RaycastHit(string attackerId, Vector3 origin, Vector3 direction, float range)
        {
            // TODO: Use Netick player positions for authoritative raycast
            // For now, placeholder: iterate over all non-attacker Alive players
            var networkManager = NetworkManager.Instance;
            if (networkManager == null) return null;

            foreach (var kvp in system_GetPlayers(networkManager))
            {
                var netPlayer = kvp.Value;
                if (netPlayer.PlayerId == attackerId) continue;
                if (netPlayer.State != PlayerState.Alive) continue;

                Vector3 targetPos = netPlayer.transform.position;
                Vector3 toTarget = targetPos - origin;
                float dist = toTarget.magnitude;

                if (dist > range) continue;

                // Check if within hit radius of ray
                Vector3 dirNormalized = direction.normalized;
                Vector3 projection = origin + dirNormalized * Vector3.Dot(toTarget, dirNormalized);
                float closestDist = Vector3.Distance(targetPos, projection);

                if (closestDist <= _hitRadius)
                {
                    return netPlayer.PlayerId;
                }
            }

            return null;
        }

        private Dictionary<string, NetworkPlayer> system_GetPlayers(NetworkManager nm)
        {
            // Reflection-free access: NetworkManager tracks players internally
            // This is a workaround for the private _players dictionary
            var players = new Dictionary<string, NetworkPlayer>();
            var allNetPlayers = FindObjectsOfType<NetworkPlayer>();
            foreach (var np in allNetPlayers)
            {
                if (!string.IsNullOrEmpty(np.PlayerId))
                    players[np.PlayerId] = np;
            }
            return players;
        }
    }

    /// <summary>
    /// Result of a single attack action, broadcast to clients.
    /// </summary>
    public class AttackResult
    {
        public bool hit;
        public string attacker_player_id;
        public string target_player_id;
        public int damage;
        public int target_hp_after;
    }
}
