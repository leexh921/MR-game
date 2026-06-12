using Netick;
using Netick.Unity;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Network
{
    /// <summary>
    /// Server-authoritative network state attached to PlayerPrefab.
    /// Identity strings stay server-local for room bookkeeping; gameplay state is replicated.
    /// </summary>
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        [Header("Server Identity")]
        [SerializeField] private string _playerId = "";
        [SerializeField] private string _nickname = "";

        public Netick.NetworkPlayerId NetickPlayerId { get; set; }

        [Networked] public int TeamValue { get; set; }
        [Networked] public int StateValue { get; set; }
        [Networked] public int HpValue { get; set; }
        [Networked] public int MaxHpValue { get; set; }
        [Networked] public NetworkBool HasTreasureValue { get; set; }
        [Networked] public int CarriedTreasureTypeValue { get; set; }
        [Networked] public int CarriedTreasureIndex { get; set; }
        [Networked] public float RespawnRemaining { get; set; }

        public string PlayerId => _playerId;
        public string Nickname => _nickname;
        public TeamType Team => (TeamType)TeamValue;
        public PlayerState State => (PlayerState)StateValue;
        public int Hp => HpValue;
        public int MaxHp => MaxHpValue;
        public bool HasTreasure => HasTreasureValue;
        public TreasureType CarriedTreasureType => (TreasureType)CarriedTreasureTypeValue;

        public void Initialize(string playerId, string nickname, TeamType team, int maxHp)
        {
            _playerId = playerId;
            _nickname = nickname;
            TeamValue = (int)team;
            StateValue = (int)PlayerState.Alive;
            MaxHpValue = maxHp;
            HpValue = maxHp;
            HasTreasureValue = false;
            CarriedTreasureTypeValue = (int)TreasureType.Normal;
            CarriedTreasureIndex = -1;
            RespawnRemaining = 0f;
        }

        public void SetTeam(TeamType team)
        {
            TeamValue = (int)team;
        }

        public void SetHp(int hp)
        {
            HpValue = Mathf.Clamp(hp, 0, Mathf.Max(1, MaxHpValue));
        }

        public void SetState(PlayerState state)
        {
            StateValue = (int)state;
        }

        public void SetRespawnRemaining(float seconds)
        {
            RespawnRemaining = Mathf.Max(0f, seconds);
        }

        public void SetCarriedTreasure(NetworkTreasure treasure)
        {
            if (treasure == null)
            {
                ClearCarriedTreasure();
                return;
            }

            HasTreasureValue = true;
            CarriedTreasureTypeValue = (int)treasure.TreasureType;
            CarriedTreasureIndex = treasure.TreasureIndex;
        }

        public void ClearCarriedTreasure()
        {
            HasTreasureValue = false;
            CarriedTreasureTypeValue = (int)TreasureType.Normal;
            CarriedTreasureIndex = -1;
        }

        public void ApplyServerState(PlayerRuntimeState state)
        {
            TeamValue = (int)state.team;
            StateValue = (int)state.state;
            HpValue = state.hp;
            HasTreasureValue = !string.IsNullOrEmpty(state.carried_treasure_id);
            transform.position = state.position;
            transform.rotation = Quaternion.Euler(0f, state.rotation_y, 0f);
        }

        public PlayerRuntimeState ToRuntimeState()
        {
            return new PlayerRuntimeState
            {
                player_id = _playerId,
                team = Team,
                state = State,
                hp = HpValue,
                position = transform.position,
                rotation_y = transform.rotation.eulerAngles.y,
                carried_treasure_id = HasTreasure ? "treasure_" + CarriedTreasureIndex : ""
            };
        }
    }
}
