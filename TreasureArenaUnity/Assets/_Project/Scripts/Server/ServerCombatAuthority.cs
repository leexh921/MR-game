using TreasureArenaMR.Network;
using TreasureArenaMR.Shared;
using UnityEngine;
using GameNetworkPlayer = TreasureArenaMR.Network.NetworkPlayer;

namespace TreasureArenaMR.Server
{
    /// <summary>
    /// Server-authoritative combat logic.
    /// </summary>
    public sealed class ServerCombatAuthority : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _hitRadius = 0.5f;

        public AttackResult ProcessAttack(
            string attackerId,
            Vector3 origin,
            Vector3 direction,
            int weaponDamage,
            float weaponRange,
            RoomManager roomManager,
            ServerTreasureAuthority treasureAuthority)
        {
            var result = new AttackResult
            {
                attacker_player_id = attackerId,
                damage = 0
            };

            if (roomManager == null)
                return result;

            GameNetworkPlayer attacker = roomManager.GetNetworkPlayerComponent(attackerId);
            PlayerInfo attackerInfo = roomManager.Players.Find(p => p.player_id == attackerId);
            if (attacker == null || attackerInfo == null || attackerInfo.state != PlayerState.Alive)
                return result;

            GameNetworkPlayer target = RaycastHit(attackerId, attackerInfo.team, origin, direction, weaponRange);
            if (target == null)
            {
                Debug.Log("[ServerCombatAuthority] Attack from " + attackerId + ": miss");
                return result;
            }

            result.hit = true;
            result.target_player_id = target.PlayerId;
            result.damage = weaponDamage;
            result.target_hp_after = ApplyDamage(target.PlayerId, weaponDamage, roomManager, treasureAuthority);

            Debug.Log("[ServerCombatAuthority] Attack from " + attackerId + ": hit "
                + result.target_player_id + " for " + weaponDamage
                + ", hp=" + result.target_hp_after);
            return result;
        }

        public int ApplyDamage(
            string targetPlayerId,
            int damage,
            RoomManager roomManager,
            ServerTreasureAuthority treasureAuthority)
        {
            PlayerInfo player = roomManager.Players.Find(p => p.player_id == targetPlayerId);
            if (player == null) return -1;
            if (player.state != PlayerState.Alive) return -1;

            GameNetworkPlayer networkPlayer = roomManager.GetNetworkPlayerComponent(targetPlayerId);
            if (networkPlayer == null) return -1;

            player.hp = Mathf.Max(0, player.hp - damage);
            networkPlayer.SetHp(player.hp);

            if (player.hp <= 0)
            {
                player.state = PlayerState.GhostRetreat;
                networkPlayer.SetState(PlayerState.GhostRetreat);
                networkPlayer.SetRespawnRemaining(0f);

                treasureAuthority?.DropPlayerTreasure(
                    targetPlayerId,
                    networkPlayer.transform.position,
                    roomManager);

                Debug.Log("[ServerCombatAuthority] Player " + targetPlayerId + " entered GhostRetreat.");
            }

            return player.hp;
        }

        private GameNetworkPlayer RaycastHit(
            string attackerId,
            TeamType attackerTeam,
            Vector3 origin,
            Vector3 direction,
            float range)
        {
            GameNetworkPlayer bestTarget = null;
            float bestDistance = float.MaxValue;
            GameNetworkPlayer[] allPlayers = FindObjectsOfType<GameNetworkPlayer>();
            Vector3 dirNormalized = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;

            for (int i = 0; i < allPlayers.Length; i++)
            {
                GameNetworkPlayer target = allPlayers[i];
                if (target == null) continue;
                if (target.PlayerId == attackerId) continue;
                if (target.State != PlayerState.Alive) continue;
                if (target.Team == TeamType.None || target.Team == attackerTeam) continue;

                Vector3 targetPos = target.transform.position + Vector3.up * 1.0f;
                Vector3 toTarget = targetPos - origin;
                float forwardDistance = Vector3.Dot(toTarget, dirNormalized);
                if (forwardDistance < 0f || forwardDistance > range) continue;

                Vector3 closestPoint = origin + dirNormalized * forwardDistance;
                float closestDist = Vector3.Distance(targetPos, closestPoint);
                if (closestDist > _hitRadius) continue;

                if (forwardDistance < bestDistance)
                {
                    bestDistance = forwardDistance;
                    bestTarget = target;
                }
            }

            return bestTarget;
        }
    }

    public class AttackResult
    {
        public bool hit;
        public string attacker_player_id;
        public string target_player_id;
        public int damage;
        public int target_hp_after;
    }
}
