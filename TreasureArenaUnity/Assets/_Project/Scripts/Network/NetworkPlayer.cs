using Netick;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Network
{
    /// <summary>
    /// Networked representation of a player, attached to the player prefab.
    /// Server-authoritative state is replicated to clients.
    /// 
    /// TODO Phase 5: Convert to Netick.NetworkBehaviour with [Networked] properties
    /// for automatic state replication instead of manual sync.
    /// </summary>
    public sealed class NetworkPlayer : MonoBehaviour
    {
        [Header("Player Identity")]
        [SerializeField] private string _playerId = "";
        [SerializeField] private string _nickname = "";
        [SerializeField] private TeamType _team = TeamType.None;

        [Header("Runtime State")]
        [SerializeField] private PlayerState _state = PlayerState.Alive;
        [SerializeField] private int _hp = 100;
        [SerializeField] private string _carriedTreasureId = "";

        /// <summary>
        /// Netick's player ID assigned to this player object.
        /// Used by the server to target RPCs and manage ownership.
        /// </summary>
        public Netick.NetworkPlayerId NetickPlayerId { get; set; }

        public string PlayerId => _playerId;
        public string Nickname => _nickname;
        public TeamType Team => _team;
        public PlayerState State => _state;
        public int Hp => _hp;
        public string CarriedTreasureId => _carriedTreasureId;

        public void Initialize(string playerId, string nickname, TeamType team)
        {
            _playerId = playerId;
            _nickname = nickname;
            _team = team;
            _state = PlayerState.Alive;
        }

        public void ApplyServerState(PlayerRuntimeState state)
        {
            _state = ParsePlayerState(state.state);
            _hp = state.hp;
            _carriedTreasureId = state.carried_treasure_id;

            transform.position = new Vector3(state.pos_x, state.pos_y, state.pos_z);
            transform.rotation = Quaternion.Euler(0f, state.rotation_y, 0f);
        }

        public PlayerRuntimeState ToRuntimeState()
        {
            return new PlayerRuntimeState
            {
                player_id = _playerId,
                team = _team == TeamType.Red ? "Red" : (_team == TeamType.Blue ? "Blue" : "None"),
                state = _state.ToString(),
                hp = _hp,
                pos_x = transform.position.x,
                pos_y = transform.position.y,
                pos_z = transform.position.z,
                rotation_y = transform.rotation.eulerAngles.y,
                carried_treasure_id = _carriedTreasureId
            };
        }

        private PlayerState ParsePlayerState(string stateStr)
        {
            switch (stateStr)
            {
                case "GhostRetreat": return PlayerState.GhostRetreat;
                case "Respawning": return PlayerState.Respawning;
                default: return PlayerState.Alive;
            }
        }
    }
}
