using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Network
{
    /// <summary>
    /// Networked representation of a treasure item.
    /// Server-authoritative state is replicated to clients.
    /// TODO: Extend with Netick.NetworkBehaviour for network sync.
    /// </summary>
    public sealed class NetworkTreasure : MonoBehaviour
    {
        [Header("Treasure Identity")]
        [SerializeField] private string _treasureId = "";
        [SerializeField] private TreasureType _treasureType = TreasureType.Normal;

        [Header("Runtime State")]
        [SerializeField] private TreasureState _state = TreasureState.Spawned;
        [SerializeField] private int _scoreValue = 10;
        [SerializeField] private string _carrierPlayerId = "";

        public string TreasureId => _treasureId;
        public TreasureType TreasureType => _treasureType;
        public TreasureState State => _state;
        public int ScoreValue => _scoreValue;
        public string CarrierPlayerId => _carrierPlayerId;

        public void Initialize(string treasureId, TreasureType type, Vector3 position, int scoreValue)
        {
            _treasureId = treasureId;
            _treasureType = type;
            _state = TreasureState.Spawned;
            _scoreValue = scoreValue;
            transform.position = position;
        }

        public void ApplyServerState(TreasureRuntimeState state)
        {
            _state = ParseTreasureState(state.state);
            _scoreValue = state.score_value;
            _carrierPlayerId = state.carrier_player_id;

            if (_state == TreasureState.Spawned || _state == TreasureState.Dropped)
            {
                transform.position = new Vector3(state.pos_x, state.pos_y, state.pos_z);
            }
        }

        public TreasureRuntimeState ToRuntimeState()
        {
            return new TreasureRuntimeState
            {
                treasure_id = _treasureId,
                treasure_type = _treasureType.ToString(),
                state = _state.ToString(),
                score_value = _scoreValue,
                pos_x = transform.position.x,
                pos_y = transform.position.y,
                pos_z = transform.position.z,
                carrier_player_id = _carrierPlayerId
            };
        }

        private TreasureState ParseTreasureState(string stateStr)
        {
            switch (stateStr)
            {
                case "Carried": return TreasureState.Carried;
                case "Dropped": return TreasureState.Dropped;
                case "Submitted": return TreasureState.Submitted;
                default: return TreasureState.Spawned;
            }
        }
    }
}
