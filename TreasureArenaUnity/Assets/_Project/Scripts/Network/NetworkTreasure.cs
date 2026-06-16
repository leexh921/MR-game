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
            _state = state.state;
            _scoreValue = state.score_value;
            _carrierPlayerId = state.carrier_player_id;

            if (_state == TreasureState.Spawned || _state == TreasureState.Dropped)
            {
                transform.position = state.position;
            }
        }

        public TreasureRuntimeState ToRuntimeState()
        {
            return new TreasureRuntimeState
            {
                treasure_id = _treasureId,
                treasure_type = _treasureType,
                state = _state,
                score_value = _scoreValue,
                position = transform.position,
                carrier_player_id = _carrierPlayerId
            };
        }
    }
}
