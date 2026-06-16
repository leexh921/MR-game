using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.MapEditor
{
    public sealed class MapEditorDockedPanel : MonoBehaviour
    {
        [SerializeField] private string panelTitle = "Panel";
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private BoxCollider hitCollider;
        [SerializeField] private GameObject expandedRoot;
        [SerializeField] private GameObject collapsedRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text collapsedTitleText;
        [SerializeField] private Image errorDot;
        [SerializeField] private Image collapsedErrorDot;
        [SerializeField] private Button collapseButton;
        [SerializeField] private Button expandButton;
        [SerializeField] private Button pinButton;
        [SerializeField] private Text pinText;
        [SerializeField] private bool expanded = true;
        [SerializeField] private bool pinned;
        [SerializeField] private float expandedAlpha = 0.75f;
        [SerializeField] private float collapsedAlpha = 0.45f;
        [SerializeField] private float dimmedAlpha = 0.35f;

        public bool IsExpanded => expanded;
        public bool IsPinned => pinned;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (hitCollider == null)
            {
                hitCollider = GetComponentInChildren<BoxCollider>(true);
            }

            if (collapseButton != null)
            {
                collapseButton.onClick.AddListener(ToggleExpanded);
            }

            if (expandButton != null)
            {
                expandButton.onClick.AddListener(ToggleExpanded);
            }

            if (pinButton != null)
            {
                pinButton.onClick.AddListener(TogglePinned);
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (collapseButton != null)
            {
                collapseButton.onClick.RemoveListener(ToggleExpanded);
            }

            if (expandButton != null)
            {
                expandButton.onClick.RemoveListener(ToggleExpanded);
            }

            if (pinButton != null)
            {
                pinButton.onClick.RemoveListener(TogglePinned);
            }
        }

        public void SetExpanded(bool value)
        {
            expanded = value;
            Refresh();
        }

        public void SetPinned(bool value)
        {
            pinned = value;
            Refresh();
        }

        public void ToggleExpanded()
        {
            SetExpanded(!expanded);
        }

        public void TogglePinned()
        {
            SetPinned(!pinned);
        }

        public void SetErrorState(bool hasError)
        {
            if (errorDot != null)
            {
                errorDot.gameObject.SetActive(hasError);
            }

            if (collapsedErrorDot != null)
            {
                collapsedErrorDot.gameObject.SetActive(hasError);
            }
        }

        public void SetDimmed(bool dimmed)
        {
            if (canvasGroup == null || pinned)
            {
                return;
            }

            canvasGroup.alpha = dimmed ? dimmedAlpha : (expanded ? expandedAlpha : collapsedAlpha);
        }

        public void RefreshHitArea()
        {
            RefreshHitCollider();
        }

        private void Refresh()
        {
            if (titleText != null)
            {
                titleText.text = panelTitle;
            }

            if (collapsedTitleText != null)
            {
                collapsedTitleText.text = panelTitle;
            }

            if (expandedRoot != null)
            {
                expandedRoot.SetActive(expanded);
            }

            if (collapsedRoot != null)
            {
                collapsedRoot.SetActive(!expanded);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = expanded ? expandedAlpha : collapsedAlpha;
            }

            if (pinText != null)
            {
                pinText.text = pinned ? "Pinned" : "Pin";
            }

            RefreshHitCollider();
        }

        private void RefreshHitCollider()
        {
            if (hitCollider == null)
            {
                return;
            }

            RectTransform activeRect = null;
            if (expanded && expandedRoot != null)
            {
                activeRect = expandedRoot.GetComponent<RectTransform>();
            }
            else if (!expanded && collapsedRoot != null)
            {
                activeRect = collapsedRoot.GetComponent<RectTransform>();
            }

            if (activeRect == null)
            {
                activeRect = GetComponent<RectTransform>();
            }

            hitCollider.size = new Vector3(activeRect.rect.width, activeRect.rect.height, 8f);
            hitCollider.center = activeRect.localPosition;
        }
    }
}
