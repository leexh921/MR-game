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
        [SerializeField] private float expandedAlpha = 0.75f;
        [SerializeField] private float dimmedAlpha = 0.35f;

        public bool IsExpanded => true;
        public bool IsPinned => false;

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

            Refresh();
        }

        private void OnDestroy()
        {
        }

        public void SetExpanded(bool value)
        {
            Refresh();
        }

        public void SetPinned(bool value)
        {
            Refresh();
        }

        public void ToggleExpanded()
        {
            Refresh();
        }

        public void TogglePinned()
        {
            Refresh();
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
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = dimmed ? dimmedAlpha : expandedAlpha;
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
                expandedRoot.SetActive(true);
            }

            if (collapsedRoot != null)
            {
                collapsedRoot.SetActive(false);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = expandedAlpha;
            }

            if (collapseButton != null) collapseButton.gameObject.SetActive(false);
            if (expandButton != null) expandButton.gameObject.SetActive(false);
            if (pinButton != null) pinButton.gameObject.SetActive(false);
            if (pinText != null)
            {
                pinText.text = "";
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
            if (expandedRoot != null)
            {
                activeRect = expandedRoot.GetComponent<RectTransform>();
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
