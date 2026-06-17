using System.Collections.Generic;
using TreasureArenaMR.Map;
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
        [SerializeField] private Transform _localRightHandCarryAnchor;
        [SerializeField] private Vector3 _carryLocalPositionOffset = Vector3.zero;
        [SerializeField] private Vector3 _carryLocalEulerOffset = Vector3.zero;

        private readonly Dictionary<string, GameObject> _instances = new Dictionary<string, GameObject>();
        private readonly HashSet<string> _loggedMissingCarryAnchors = new HashSet<string>();
        private readonly HashSet<string> _seenThisFrame = new HashSet<string>();
        private ClientMatchStateStore _store;
        private SharedSpaceManager _sharedSpace;
        private RemotePlayerPresenter _remotePlayerPresenter;
        private Transform _root;
        private bool _loggedMissingPrefab;
        private bool _sceneTreasureMarkersHidden;

        internal void Initialize(Transform localRightHandCarryAnchor, GameObject normalPrefab, GameObject rarePrefab, GameObject finalPrefab)
        {
            _localRightHandCarryAnchor = localRightHandCarryAnchor;
            _normalTreasurePrefab = normalPrefab;
            _rareTreasurePrefab = rarePrefab;
            _finalTreasurePrefab = finalPrefab;
        }

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

            HideSceneTreasureMarkers();

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
            GameObject instance = GetOrCreateInstance(treasure);
            if (instance == null)
                return;

            if (treasure.state == "Submitted")
            {
                instance.SetActive(false);
                return;
            }

            if (treasure.state == "Carried")
            {
                UpdateCarriedTreasure(treasure, instance);
                return;
            }

            bool visibleAtWorldPosition = treasure.state == "Spawned" || treasure.state == "Dropped";
            instance.SetActive(visibleAtWorldPosition);
            if (!visibleAtWorldPosition)
                return;

            if (instance.transform.parent != _root)
                instance.transform.SetParent(_root, false);

            Vector3 position = treasure.position != null ? treasure.position.ToVector3() : Vector3.zero;
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.identity;
        }

        private void UpdateCarriedTreasure(TreasureSnapshot treasure, GameObject instance)
        {
            if (!TryResolveCarryAnchor(treasure.carrier_player_id, out Transform anchor))
            {
                instance.SetActive(false);
                return;
            }

            if (instance.transform.parent != anchor)
                instance.transform.SetParent(anchor, false);

            instance.SetActive(true);
            instance.transform.localPosition = _carryLocalPositionOffset;
            instance.transform.localRotation = Quaternion.Euler(_carryLocalEulerOffset);
        }

        private bool TryResolveCarryAnchor(string carrierPlayerId, out Transform anchor)
        {
            anchor = null;
            if (string.IsNullOrEmpty(carrierPlayerId))
                return false;

            if (_store != null && carrierPlayerId == _store.LocalPlayerId)
            {
                anchor = _localRightHandCarryAnchor;
                if (anchor == null)
                    LogMissingCarryAnchorOnce("local right hand carry anchor missing");
                return anchor != null;
            }

            if (_remotePlayerPresenter != null &&
                _remotePlayerPresenter.TryGetCarryAnchor(carrierPlayerId, out anchor))
            {
                return true;
            }

            LogMissingCarryAnchorOnce("remote carry anchor missing player=" + carrierPlayerId);
            return false;
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

        private void HideSceneTreasureMarkers()
        {
            if (_sceneTreasureMarkersHidden)
                return;

            _sceneTreasureMarkersHidden = true;
            MapExportMarker[] markers = FindObjectsOfType<MapExportMarker>(true);
            int hidden = 0;
            for (int i = 0; i < markers.Length; i++)
            {
                MapExportMarker marker = markers[i];
                if (marker == null || marker.marker_type != MapExportMarkerType.TreasureSpawnPoint)
                    continue;

                marker.gameObject.SetActive(false);
                hidden++;
            }

            Debug.Log($"{LogPrefix} scene treasure export markers hidden={hidden}; runtime treasures are driven by server snapshot.");
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
            if (_remotePlayerPresenter == null)
                _remotePlayerPresenter = FindObjectOfType<RemotePlayerPresenter>();
        }

        private void LogMissingCarryAnchorOnce(string message)
        {
            if (_loggedMissingCarryAnchors.Add(message))
                Debug.LogError($"{LogPrefix} {message}");
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
