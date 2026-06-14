using UnityEngine;
using UnityEngine.XR;

namespace TreasureArenaMR.UI
{
    /// <summary>
    /// Toggles the Pico in-game panel with the HMD menu button and keeps it centered in view.
    /// This script only controls UI presentation; gameplay state remains server authoritative.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class PicoMenuPanelController : MonoBehaviour
    {
        [Header("Visibility")]
        [SerializeField] private bool visibleOnStart;
        [SerializeField] private KeyCode editorToggleKey = KeyCode.M;
        [SerializeField] private bool useSecondaryButtonFallback = true;
        [SerializeField] private bool usePrimaryButtonFallback;

        [Header("Follow")]
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private float panelDistance = 1.35f;
        [SerializeField] private float verticalOffset;
        [SerializeField] private bool followWhileVisible = true;
        [SerializeField] private float followLerp = 20f;

        private Canvas targetCanvas;
        private CanvasGroup canvasGroup;
        private bool isVisible;
        private bool previousMenuPressed;

        private void Awake()
        {
            targetCanvas = GetComponent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void Start()
        {
            SetVisible(visibleOnStart, true);
            SnapToCamera();
        }

        private void Update()
        {
            bool menuPressed = IsPicoMenuPressed();
            bool pressedThisFrame = menuPressed && !previousMenuPressed;
            previousMenuPressed = menuPressed;

#if UNITY_EDITOR
            if (Input.GetKeyDown(editorToggleKey))
                pressedThisFrame = true;
#endif

            if (pressedThisFrame)
                SetVisible(!isVisible, true);
        }

        private void LateUpdate()
        {
            if (isVisible && followWhileVisible)
                SnapToCamera();
        }

        public void Show()
        {
            SetVisible(true, true);
        }

        public void Hide()
        {
            SetVisible(false, false);
        }

        public void Toggle()
        {
            SetVisible(!isVisible, true);
        }

        private void SetVisible(bool visible, bool snap)
        {
            isVisible = visible;

            if (targetCanvas != null)
                targetCanvas.enabled = visible;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            if (visible && snap)
                SnapToCamera();
        }

        private void SnapToCamera()
        {
            Transform target = ResolveCameraTarget();
            if (target == null)
                return;

            Vector3 desiredPosition = target.position
                + target.forward * Mathf.Max(0.2f, panelDistance)
                + target.up * verticalOffset;
            Quaternion desiredRotation = Quaternion.LookRotation(desiredPosition - target.position, target.up);

            if (followLerp <= 0f)
            {
                transform.SetPositionAndRotation(desiredPosition, desiredRotation);
                return;
            }

            float t = 1f - Mathf.Exp(-followLerp * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, t);
        }

        private Transform ResolveCameraTarget()
        {
            if (cameraTarget != null)
                return cameraTarget;

            Camera mainCamera = Camera.main;
            return mainCamera != null ? mainCamera.transform : null;
        }

        private bool IsPicoMenuPressed()
        {
            if (IsButtonPressed(XRNode.LeftHand, CommonUsages.menuButton)
                || IsButtonPressed(XRNode.RightHand, CommonUsages.menuButton))
            {
                return true;
            }

            if (useSecondaryButtonFallback
                && (IsButtonPressed(XRNode.LeftHand, CommonUsages.secondaryButton)
                    || IsButtonPressed(XRNode.RightHand, CommonUsages.secondaryButton)))
            {
                return true;
            }

            return usePrimaryButtonFallback
                && (IsButtonPressed(XRNode.LeftHand, CommonUsages.primaryButton)
                    || IsButtonPressed(XRNode.RightHand, CommonUsages.primaryButton));
        }

        private static bool IsButtonPressed(XRNode node, InputFeatureUsage<bool> button)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            return device.isValid
                && device.TryGetFeatureValue(button, out bool pressed)
                && pressed;
        }
    }
}
