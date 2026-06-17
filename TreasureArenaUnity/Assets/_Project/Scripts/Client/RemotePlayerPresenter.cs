using System.Collections.Generic;
using TreasureArenaMR.Network;
using UnityEngine;

namespace TreasureArenaMR.Client
{
    public sealed class RemotePlayerPresenter : MonoBehaviour
    {
        private const string LogPrefix = "[SimpleRemotePlayer]";
        private const float InterpolationDelaySeconds = 0.1f;
        private const float StaleSeconds = 1f;

        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private float _heightOffset = -2f;
        [SerializeField] private Transform _localPoseSource;

        private readonly Dictionary<string, PresentedPlayer> _players = new Dictionary<string, PresentedPlayer>();
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

            if (_store == null || _sharedSpace == null || !_sharedSpace.IsReady || _playerPrefab == null)
            {
                if (_playerPrefab == null && !_loggedMissingPrefab)
                {
                    _loggedMissingPrefab = true;
                    Debug.LogError($"{LogPrefix} Player prefab is not assigned.");
                }
                return;
            }

            UpdateLocalAvatar();
            UpdateRemoteAvatars();
        }

        private void UpdateLocalAvatar()
        {
            if (_localPoseSource == null || string.IsNullOrEmpty(_store.LocalPlayerId))
                return;

            PresentedPlayer local = GetOrCreatePlayer(_store.LocalPlayerId, true);
            if (local == null)
                return;

            Vector3 sharedPosition = _sharedSpace.WorldToSharedPosition(_localPoseSource.position);
            float sharedYaw = _sharedSpace.WorldYawToSharedYaw(_localPoseSource.eulerAngles.y);
            local.ApplyLocal(sharedPosition, sharedYaw);
        }

        private void UpdateRemoteAvatars()
        {
            if (!_store.HasPoseSnapshot)
                return;

            PoseSnapshotPayload snapshot = _store.PoseSnapshot;
            for (int i = 0; i < snapshot.players.Count; i++)
            {
                PlayerPoseSnapshot pose = snapshot.players[i];
                if (pose == null || string.IsNullOrEmpty(pose.player_id))
                    continue;
                if (pose.player_id == _store.LocalPlayerId)
                    continue;

                PresentedPlayer remote = GetOrCreatePlayer(pose.player_id, false);
                if (remote == null)
                    continue;

                remote.AddSample(pose);
            }

            float renderTime = Time.time - InterpolationDelaySeconds;
            foreach (PresentedPlayer player in _players.Values)
            {
                if (!player.IsLocal)
                    player.Render(renderTime);
            }
        }

        private PresentedPlayer GetOrCreatePlayer(string playerId, bool isLocal)
        {
            if (_players.TryGetValue(playerId, out PresentedPlayer existing))
                return existing;

            if (_playerPrefab == null || _root == null)
                return null;

            GameObject instance = Instantiate(_playerPrefab, _root);
            instance.name = (isLocal ? "LocalPlayer_" : "RemotePlayer_") + playerId;
            PresentedPlayer player = new PresentedPlayer(playerId, isLocal, instance.transform, _heightOffset);
            _players[playerId] = player;
            Debug.Log($"{LogPrefix} spawned player={playerId} local={isLocal} prefab={_playerPrefab.name}");
            return player;
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
            if (_localPoseSource == null && Camera.main != null)
                _localPoseSource = Camera.main.transform;
        }

        private void EnsureRoot()
        {
            if (_sharedSpace == null || _sharedSpace.SharedRoot == null)
                return;

            if (_root != null && _root.parent == _sharedSpace.SharedRoot)
                return;

            if (_root == null)
                _root = new GameObject("NetworkPlayerAvatars").transform;
            _root.SetParent(_sharedSpace.SharedRoot, false);
            _root.localPosition = Vector3.zero;
            _root.localRotation = Quaternion.identity;
            _root.localScale = Vector3.one;
        }

        private sealed class PresentedPlayer
        {
            private readonly string _playerId;
            private readonly Transform _transform;
            private readonly float _heightOffset;
            private readonly List<Sample> _samples = new List<Sample>();
            private bool _loggedStale;

            public PresentedPlayer(string playerId, bool isLocal, Transform transform, float heightOffset)
            {
                _playerId = playerId;
                IsLocal = isLocal;
                _transform = transform;
                _heightOffset = heightOffset;
            }

            public bool IsLocal { get; }

            public void ApplyLocal(Vector3 sharedPosition, float sharedYaw)
            {
                Apply(sharedPosition, sharedYaw);
            }

            public void AddSample(PlayerPoseSnapshot pose)
            {
                _samples.Add(new Sample
                {
                    receivedTime = Time.time,
                    position = pose.position != null ? pose.position.ToVector3() : Vector3.zero,
                    rotationY = pose.rotation_y,
                    stale = pose.stale
                });

                while (_samples.Count > 8)
                    _samples.RemoveAt(0);
            }

            public void Render(float renderTime)
            {
                if (_samples.Count == 0)
                    return;

                Sample latest = _samples[_samples.Count - 1];
                bool stale = latest.stale || Time.time - latest.receivedTime > StaleSeconds;
                if (stale)
                {
                    if (!_loggedStale)
                    {
                        _loggedStale = true;
                        Debug.LogWarning($"{LogPrefix} stale player={_playerId}");
                    }
                    return;
                }

                _loggedStale = false;
                if (_samples.Count == 1)
                {
                    Apply(latest.position, latest.rotationY);
                    return;
                }

                Sample before = _samples[0];
                Sample after = latest;
                for (int i = 0; i < _samples.Count - 1; i++)
                {
                    if (_samples[i].receivedTime <= renderTime && _samples[i + 1].receivedTime >= renderTime)
                    {
                        before = _samples[i];
                        after = _samples[i + 1];
                        break;
                    }
                }

                float range = after.receivedTime - before.receivedTime;
                float t = range > 0.0001f ? Mathf.Clamp01((renderTime - before.receivedTime) / range) : 1f;
                Vector3 position = Vector3.Lerp(before.position, after.position, t);
                float yaw = Mathf.LerpAngle(before.rotationY, after.rotationY, t);
                Apply(position, yaw);
            }

            private void Apply(Vector3 sharedPosition, float sharedYaw)
            {
                _transform.localPosition = sharedPosition + Vector3.up * _heightOffset;
                _transform.localRotation = Quaternion.Euler(0f, sharedYaw, 0f);
            }
        }

        private struct Sample
        {
            public float receivedTime;
            public Vector3 position;
            public float rotationY;
            public bool stale;
        }
    }
}
