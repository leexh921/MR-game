using TreasureArenaMR.Network;
using UnityEngine;
using UnityEngine.XR;

namespace TreasureArenaMR.Client
{
    public sealed class TreasureInteractionController : MonoBehaviour
    {
        private const string LogPrefix = "[TreasureInput]";

        [SerializeField] private SimpleNetworkClient _client;
        [SerializeField] private KeyCode _editorPickupKey = KeyCode.E;
        [SerializeField] private KeyCode _editorSubmitKey = KeyCode.Q;

        private bool _previousGrip;
        private bool _previousPrimary;

        private void Awake()
        {
            CacheReferences();
        }

        private void Update()
        {
            CacheReferences();
            if (_client == null || !_client.IsConnected)
                return;

            bool pickupPressed = ReadPicoGripPressedThisFrame() || Input.GetKeyDown(_editorPickupKey);
            bool submitPressed = ReadPicoPrimaryPressedThisFrame() || Input.GetKeyDown(_editorSubmitKey);

            if (pickupPressed)
            {
                _client.SendPickupTreasure(string.Empty);
                Debug.Log($"{LogPrefix} pickup request nearest");
            }

            if (submitPressed)
            {
                _client.SendSubmitTreasure();
                Debug.Log($"{LogPrefix} submit request");
            }
        }

        private void CacheReferences()
        {
            if (_client == null)
                _client = FindObjectOfType<SimpleNetworkClient>();
        }

        private bool ReadPicoGripPressedThisFrame()
        {
            InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!rightHand.isValid)
                return false;

            rightHand.TryGetFeatureValue(CommonUsages.gripButton, out bool grip);
            bool pressed = grip && !_previousGrip;
            _previousGrip = grip;
            return pressed;
        }

        private bool ReadPicoPrimaryPressedThisFrame()
        {
            InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!rightHand.isValid)
                return false;

            rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary);
            bool pressed = primary && !_previousPrimary;
            _previousPrimary = primary;
            return pressed;
        }
    }
}
