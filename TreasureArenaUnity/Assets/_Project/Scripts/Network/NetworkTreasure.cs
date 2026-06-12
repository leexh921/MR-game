using Netick;
using Netick.Unity;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Network
{
    /// <summary>
    /// Server-owned treasure runtime state. Visual visibility follows replicated state.
    /// </summary>
    public sealed class NetworkTreasure : NetworkBehaviour
    {
        [Header("Server Identity")]
        [SerializeField] private string _treasureId = "";

        [Networked] public int TreasureIndex { get; set; }
        [Networked] public int TreasureTypeValue { get; set; }
        [Networked] public int StateValue { get; set; }
        [Networked] public int ScoreValue { get; set; }
        [Networked] public int CarrierPlayerObjectId { get; set; }

        private Renderer[] cachedRenderers;
        private Collider[] cachedColliders;
        private TreasureState lastAppliedState = (TreasureState)(-1);

        public string TreasureId => _treasureId;
        public TreasureType TreasureType => (TreasureType)TreasureTypeValue;
        public TreasureState State => (TreasureState)StateValue;
        public int Score => ScoreValue;

        private void Awake()
        {
            CacheVisuals();
        }

        public override void NetworkAwake()
        {
            CacheVisuals();
            ApplyVisualState();
        }

        private void Update()
        {
            ApplyVisualState();
        }

        public void Initialize(int treasureIndex, string treasureId, TreasureType type, Vector3 position, int scoreValue)
        {
            _treasureId = treasureId;
            TreasureIndex = treasureIndex;
            TreasureTypeValue = (int)type;
            StateValue = (int)TreasureState.Spawned;
            ScoreValue = scoreValue;
            CarrierPlayerObjectId = -1;
            transform.position = position;
            ApplyVisualState();
        }

        public void SetSpawned(Vector3 position)
        {
            StateValue = (int)TreasureState.Spawned;
            CarrierPlayerObjectId = -1;
            transform.position = position;
            ApplyVisualState();
        }

        public void SetCarried(NetworkPlayer carrier)
        {
            StateValue = (int)TreasureState.Carried;
            CarrierPlayerObjectId = carrier != null && carrier.Object != null ? carrier.Object.Id : -1;
            ApplyVisualState();
        }

        public void SetDropped(Vector3 position)
        {
            StateValue = (int)TreasureState.Dropped;
            CarrierPlayerObjectId = -1;
            transform.position = position;
            ApplyVisualState();
        }

        public void SetSubmitted()
        {
            StateValue = (int)TreasureState.Submitted;
            CarrierPlayerObjectId = -1;
            ApplyVisualState();
        }

        public void ApplyServerState(TreasureRuntimeState state)
        {
            TreasureTypeValue = (int)state.treasure_type;
            StateValue = (int)state.state;
            ScoreValue = state.score_value;

            if (State == TreasureState.Spawned || State == TreasureState.Dropped)
                transform.position = state.position;

            ApplyVisualState();
        }

        public TreasureRuntimeState ToRuntimeState()
        {
            return new TreasureRuntimeState
            {
                treasure_id = _treasureId,
                treasure_type = TreasureType,
                state = State,
                score_value = ScoreValue,
                position = transform.position,
                carrier_player_id = CarrierPlayerObjectId >= 0 ? CarrierPlayerObjectId.ToString() : ""
            };
        }

        private void CacheVisuals()
        {
            if (cachedRenderers == null || cachedRenderers.Length == 0)
                cachedRenderers = GetComponentsInChildren<Renderer>(true);
            if (cachedColliders == null || cachedColliders.Length == 0)
                cachedColliders = GetComponentsInChildren<Collider>(true);
        }

        private void ApplyVisualState()
        {
            TreasureState state = State;
            if (state == lastAppliedState)
                return;

            lastAppliedState = state;
            bool visible = state == TreasureState.Spawned || state == TreasureState.Dropped;

            CacheVisuals();
            for (int i = 0; i < cachedRenderers.Length; i++)
                if (cachedRenderers[i] != null)
                    cachedRenderers[i].enabled = visible;

            for (int i = 0; i < cachedColliders.Length; i++)
                if (cachedColliders[i] != null)
                    cachedColliders[i].enabled = visible;
        }
    }
}
