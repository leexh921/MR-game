using System;
using System.Threading.Tasks;
using TreasureArenaMR.Network;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.XR.PXR;
using UnityEngine.Android;
#endif

namespace TreasureArenaMR.Client
{
    public sealed class SharedSpaceManager : MonoBehaviour
    {
        private const string LogPrefix = "[SharedSpace]";
        private const float LogInterval = 2f;

        public static SharedSpaceManager Instance { get; private set; }

        public Transform SharedRoot { get; private set; }
        public bool IsReady { get; private set; }
        public bool IsFailed { get; private set; }
        public bool IsOwner { get; private set; }
        public string AnchorUuid { get; private set; } = "";
        public string Status { get; private set; } = "WaitingForAnchorState";

        [SerializeField] private SimpleNetworkClient _client;

        private ClientMatchStateStore _store;
        private Transform _anchorTransform;
        private bool _operationStarted;
        private bool _readySent;
        private bool _providerStarted;
        private float _logTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureRoot();
            CacheReferences();
            StartSenseDataProviderOnce();
        }

        private void Update()
        {
            CacheReferences();
            UpdateAnchorRootPose();
            ProcessState();
            LogHeartbeat();
        }

        public Vector3 WorldToSharedPosition(Vector3 worldPosition)
        {
            EnsureRoot();
            return SharedRoot.InverseTransformPoint(worldPosition);
        }

        public Vector3 SharedToWorldPosition(Vector3 sharedPosition)
        {
            EnsureRoot();
            return SharedRoot.TransformPoint(sharedPosition);
        }

        public float WorldYawToSharedYaw(float worldYaw)
        {
            EnsureRoot();
            Quaternion sharedRotation = Quaternion.Inverse(SharedRoot.rotation) * Quaternion.Euler(0f, worldYaw, 0f);
            return sharedRotation.eulerAngles.y;
        }

        public float SharedYawToWorldYaw(float sharedYaw)
        {
            EnsureRoot();
            Quaternion worldRotation = SharedRoot.rotation * Quaternion.Euler(0f, sharedYaw, 0f);
            return worldRotation.eulerAngles.y;
        }

        private void ProcessState()
        {
            if (_store == null || !_store.IsConnected || !_store.HasSpaceAnchorState || IsReady || IsFailed)
                return;

            SpaceAnchorStatePayload state = _store.SpaceAnchorState;
            IsOwner = state.owner_player_id == _store.LocalPlayerId;

            if (Application.platform != RuntimePlatform.Android)
            {
                Debug.Log($"{LogPrefix} Editor or non-Android platform, bypassing spatial anchor. platform={Application.platform}");
                MarkLocatedReady("EditorBypass");
                return;
            }

            if (IsOwner && !state.has_anchor && !_operationStarted)
            {
                _operationStarted = true;
                _ = CreateAndPublishAnchorAsync();
                return;
            }

            if (!IsOwner && state.has_anchor && !_operationStarted)
            {
                _operationStarted = true;
                _ = DownloadAndLocateAnchorAsync(state.anchor_uuid);
            }
        }

        private async void StartSenseDataProviderOnce()
        {
            if (_providerStarted)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!await EnsureSpatialDataPermissionAsync())
                return;

            PxrResult providerResult = await PXR_MixedReality.StartSenseDataProvider(PxrSenseDataProviderType.SpatialAnchor);
            if (providerResult == PxrResult.SUCCESS)
                _providerStarted = true;
            Debug.Log($"{LogPrefix} StartSenseDataProvider: {providerResult}");
#endif
        }

        private async Task CreateAndPublishAnchorAsync()
        {
            Status = "Creating";
            Debug.Log($"{LogPrefix} creating spatial anchor owner={IsOwner}");

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!await EnsureSpatialDataPermissionAsync())
            {
                Fail("permission_denied:SPATIAL_DATA");
                return;
            }

            Vector3 anchorPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            anchorPos.y = 0f;

            var createResult = await PXR_MixedReality.CreateSpatialAnchorAsync(anchorPos, Quaternion.identity);
            if (createResult.result != PxrResult.SUCCESS)
            {
                Fail("create_failed:" + createResult.result);
                return;
            }

            ulong anchorHandle = createResult.anchorHandle;
            AnchorUuid = createResult.uuid.ToString();
            CreateAnchorTransform(anchorHandle, createResult.uuid);

            Status = "Persisting";
            PxrResult persistResult = await PXR_MixedReality.PersistSpatialAnchorAsync(anchorHandle);
            if (persistResult != PxrResult.SUCCESS)
            {
                Fail("persist_failed:" + persistResult);
                return;
            }

            Status = "Uploading";
            var uploadResult = await PXR_MixedReality.UploadSpatialAnchorAsync(anchorHandle);
            if (uploadResult.result != PxrResult.SUCCESS)
            {
                Fail("upload_failed:" + uploadResult.result);
                return;
            }

            AnchorUuid = uploadResult.uuid != Guid.Empty ? uploadResult.uuid.ToString() : AnchorUuid;
            _client?.SendSpaceAnchorPublish(AnchorUuid);
            MarkLocatedReady("CreatedAndUploaded");
#else
            await Task.Yield();
            Fail("shared_anchor_requires_android_build");
#endif
        }

        private async Task DownloadAndLocateAnchorAsync(string anchorUuid)
        {
            if (!Guid.TryParse(anchorUuid, out Guid uuid))
            {
                Fail("invalid_anchor_uuid:" + anchorUuid);
                return;
            }

            AnchorUuid = anchorUuid;
            Status = "Downloading";
            Debug.Log($"{LogPrefix} downloading spatial anchor uuid={anchorUuid}");

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!await EnsureSpatialDataPermissionAsync())
            {
                Fail("permission_denied:SPATIAL_DATA");
                return;
            }

            PxrResult downloadResult = await PXR_MixedReality.DownloadSharedSpatialAnchorAsync(uuid);
            if (downloadResult != PxrResult.SUCCESS)
            {
                Fail("download_failed:" + downloadResult);
                return;
            }

            Status = "Locating";
            var queryResult = await PXR_MixedReality.QuerySpatialAnchorObjectsAsync(new[] { uuid });
            if (queryResult.result != PxrResult.SUCCESS || queryResult.spatialAnchorObjects == null || queryResult.spatialAnchorObjects.Count == 0)
            {
                Fail("query_failed:" + queryResult.result);
                return;
            }

            _anchorTransform = queryResult.spatialAnchorObjects[0].transform;
            _anchorTransform.name = "SharedSpaceAnchor_" + uuid.ToString("N");
            MarkLocatedReady("DownloadedAndLocated");
#else
            await Task.Yield();
            Fail("shared_anchor_requires_android_build");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private async Task<bool> EnsureSpatialDataPermissionAsync()
        {
            const string permission = "com.picovr.permission.SPATIAL_DATA";
            if (Permission.HasUserAuthorizedPermission(permission))
                return true;

            var tcs = new TaskCompletionSource<bool>();
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => tcs.TrySetResult(true);
            callbacks.PermissionDenied += _ => tcs.TrySetResult(false);
            callbacks.PermissionDeniedAndDontAskAgain += _ => tcs.TrySetResult(false);
            Permission.RequestUserPermission(permission, callbacks);
            return await tcs.Task;
        }

        private void CreateAnchorTransform(ulong anchorHandle, Guid uuid)
        {
            GameObject anchorObject = new GameObject("SharedSpaceAnchor_" + uuid.ToString("N"));
            var spatialAnchor = anchorObject.AddComponent<PXR_SpatialAnchor>();
            spatialAnchor.Created = true;
            spatialAnchor.anchorHandle = anchorHandle;
            spatialAnchor.anchorUuid = uuid;
            _anchorTransform = anchorObject.transform;
        }
#endif

        private void MarkLocatedReady(string status)
        {
            IsReady = true;
            Status = status;
            UpdateAnchorRootPose();
            SendReadyOnce();
            Debug.Log($"{LogPrefix} anchor ready uuid={AnchorUuid} owner={IsOwner} status={status}");
        }

        private void Fail(string error)
        {
            IsFailed = true;
            Status = "Failed:" + error;
            _client?.SendSpaceAnchorReady(false, Status);
            Debug.LogError($"{LogPrefix} {Status}");
        }

        private void SendReadyOnce()
        {
            if (_readySent)
                return;

            _readySent = true;
            _client?.SendSpaceAnchorReady(true, Status);
        }

        private void UpdateAnchorRootPose()
        {
            EnsureRoot();
            if (_anchorTransform == null)
                return;

            SharedRoot.SetPositionAndRotation(_anchorTransform.position, _anchorTransform.rotation);
        }

        private void EnsureRoot()
        {
            if (SharedRoot != null)
                return;

            GameObject root = GameObject.Find("SharedSpaceRoot");
            if (root == null)
                root = new GameObject("SharedSpaceRoot");

            SharedRoot = root.transform;
        }

        private void CacheReferences()
        {
            if (_client == null)
                _client = FindObjectOfType<SimpleNetworkClient>();
            if (_store == null)
                _store = ClientMatchStateStore.Instance;
        }

        private void LogHeartbeat()
        {
            _logTimer -= Time.deltaTime;
            if (_logTimer > 0f)
                return;

            _logTimer = LogInterval;
            Debug.Log($"{LogPrefix} heartbeat ready={IsReady} failed={IsFailed} owner={IsOwner} " +
                $"status={Status} uuid={AnchorUuid}");
        }
    }
}
