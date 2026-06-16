using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.MapEditor
{
    public sealed class MapEditorRuntimeUiHitTarget : MonoBehaviour
    {
        [SerializeField] private Button button;

        public void Activate()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
                if (button == null)
                {
                    button = GetComponentInParent<Button>();
                }
            }

            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
            }
        }
    }
}
