using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.MapEditor
{
    public sealed class MapEditorRuntimeUiHitTarget : MonoBehaviour
    {
        [SerializeField] private Button button;

        public event System.Action<MapEditorHand> HandActivated;

        public void Activate()
        {
            Activate(MapEditorHand.Right);
        }

        public void Activate(MapEditorHand hand)
        {
            if (button == null)
            {
                button = GetComponent<Button>();
                if (button == null)
                {
                    button = GetComponentInParent<Button>();
                }
            }

            if (HandActivated != null)
            {
                HandActivated.Invoke(hand);
                return;
            }

            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
                return;
            }

            Toggle toggle = GetComponent<Toggle>();
            if (toggle != null && toggle.interactable)
            {
                toggle.isOn = !toggle.isOn;
                return;
            }

            Dropdown dropdown = GetComponent<Dropdown>();
            if (dropdown != null && dropdown.interactable)
            {
                dropdown.Show();
                return;
            }

            InputField input = GetComponent<InputField>();
            if (input != null && input.interactable)
            {
                input.ActivateInputField();
            }
        }
    }
}
