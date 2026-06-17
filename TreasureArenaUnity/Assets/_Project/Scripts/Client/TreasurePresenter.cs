using System.Collections.Generic;
using TreasureArenaMR.Network;
using UnityEngine;

namespace TreasureArenaMR.Client
{
    public sealed class TreasurePresenter : MonoBehaviour
    {
        private const string LogPrefix = "[TreasurePresenter]";

        [SerializeField] private GameObject _normalTreasurePrefab;
        [SerializeField] private GameObject _rareTreasurePrefab;
        [SerializeField] private GameObject _finalTreasurePrefab;

        private readonly Dictionary<string, GameObject> _instances = new Dictionary<string, GameObject>();
        private readonly HashSet<string> _seenThisFrame = new HashSet<string>();
        private ClientMatchStateStore _store;
        private SharedSpaceManager _sharedSpace;
        private Transform _root;
        private bool _loggedMissingPrefab;

        private void Awake()
        {
            CacheReferences();
            EnsureRoot();
        }

        private void Update()
        {
            CacheReferences();
            EnsureRoot();
            if (_store == null || !_store.HasMatchSnapshot || _sharedSpace == null || !_sharedSpace.IsReady)
                return;

            _seenThisFrame.Clear();
            MatchSnapshotPayload snapshot = _store.MatchSnapshot;
            for (int i = 0; i < snapshot.treasures.Count; i++)
            {
                TreasureSnapshot treasure = snapshot.treasures[i];
                if (treasure == null || string.IsNullOrEmpty(treasure.treasure_id))
                    continue;

                _seenThisFrame.Add(treasure.treasure_id);
                UpdateTreasure(treasure);
            }

            HideMissingTreasures();
        }

        private void UpdateTreasure(TreasureSnapshot treasure)
        {
            bool visible = treasure.state == "Spawned" || treasure.state == "Dropped";
            GameObject instance = GetOrCreateInstance(treasure);
            if (instance == null)
                return;

            instance.SetActive(visible);
            if (!visible)
                return;

            Vector3 position = treasure.position != null ? treasure.position.ToVector3() : Vector3.zero;
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.identity;
        }

        private GameObject GetOrCreateInstance(TreasureSnapshot treasure)
        {
            if (_instances.TryGetValue(treasure.treasure_id, out GameObject existing))
                return existing;

            GameObject prefab = ResolvePrefab(treasure.treasure_type);
            if (prefab == null)
            {
                if (!_loggedMissingPrefab)
                {
                    _loggedMissingPrefab = true;
                    Debug.LogError($"{LogPrefix} Treasure prefab is not assigned for type={treasure.treasure_type}");
                }
                return null;
            }

            GameObject instance = Instantiate(prefab, _root);
            instance.name = "Treasure_" + treasure.treasure_id;
            _instances[treasure.treasure_id] = instance;
            Debug.Log($"{LogPrefix} spawned treasure={treasure.treasure_id} type={treasure.treasure_type}");
            return instance;
        }

        private GameObject ResolvePrefab(string treasureType)
        {
            if (treasureType == "Rare")
                return _rareTreasurePrefab;
            if (treasureType == "Final")
                return _finalTreasurePrefab;
            return _normalTreasurePrefab;
        }

        private void HideMissingTreasures()
        {
            foreach (KeyValuePair<string, GameObject> pair in _instances)
            {
                if (!_seenThisFrame.Contains(pair.Key) && pair.Value != null)
                    pair.Value.SetActive(false);
            }
        }

        private void CacheReferences()
        {
            if (_store == null)
                _store = ClientMatchStateStore.Instance != null
                    ? ClientMatchStateStore.Instance
                    : FindObjectOfType<ClientMatchStateStore>();
            if (_sharedSpace == null)
                _sharedSpace = SharedSpaceManager.Instance != null
                    ? SharedSpaceManager.Instance
                    : FindObjectOfType<SharedSpaceManager>();
        }

        private void EnsureRoot()
        {
            if (_sharedSpace == null || _sharedSpace.SharedRoot == null)
                return;

            if (_root != null && _root.parent == _sharedSpace.SharedRoot)
                return;

            if (_root == null)
                _root = new GameObject("Treasures").transform;
            _root.SetParent(_sharedSpace.SharedRoot, false);
            _root.localPosition = Vector3.zero;
            _root.localRotation = Quaternion.identity;
            _root.localScale = Vector3.one;
        }
    }
}
