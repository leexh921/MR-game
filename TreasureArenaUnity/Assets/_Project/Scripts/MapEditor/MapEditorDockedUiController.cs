using System.Collections.Generic;
using TreasureArenaMR.Map;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.MapEditor
{
    public sealed class MapEditorDockedUiController : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private MapEditorRuntimeController controller;
        [SerializeField] private MapEditorRuntimeExporter exporter;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool followCamera = true;
        [SerializeField] private float distanceFromCamera = 2.2f;
        [SerializeField] private bool fitToCameraView = true;
        [SerializeField] private float viewportCoverage = 0.88f;
        [SerializeField] private float designHeight = 720f;
        [SerializeField] private float minimumDesignWidth = 1200f;
        [SerializeField] private float panelMargin = 20f;

        [Header("Panels")]
        [SerializeField] private MapEditorDockedPanel topPanel;
        [SerializeField] private MapEditorDockedPanel leftPanel;
        [SerializeField] private MapEditorDockedPanel rightPanel;
        [SerializeField] private MapEditorDockedPanel bottomPanel;
        [SerializeField] private MapEditorValidationPresenter validationPresenter;
        [SerializeField] private MapEditorObjectInspector objectInspector;

        [Header("Top Bar")]
        [SerializeField] private Text mapNameText;
        [SerializeField] private Text mapIdText;
        [SerializeField] private Text versionText;
        [SerializeField] private Text modeText;
        [SerializeField] private Text countText;
        [SerializeField] private Text validationStatusText;
        [SerializeField] private Button validateButton;
        [SerializeField] private Button exportButton;

        [Header("Left Panel")]
        [SerializeField] private Transform mapObjectsListRoot;
        [SerializeField] private Transform gameplayMarkersListRoot;
        [SerializeField] private Button brushButtonTemplate;

        [Header("Bottom Bar")]
        [SerializeField] private Button placeButton;
        [SerializeField] private Button moveButton;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button scaleButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button clearBrushButton;
        [SerializeField] private Text gridStepText;
        [SerializeField] private Text rotateStepText;
        [SerializeField] private Text scaleStepText;

        private bool hasValidationErrors;
        private readonly Dictionary<string, bool> folderExpanded = new Dictionary<string, bool>();

        private void Awake()
        {
            if (controller == null)
            {
                controller = FindObjectOfType<MapEditorRuntimeController>();
            }

            if (exporter == null)
            {
                exporter = FindObjectOfType<MapEditorRuntimeExporter>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            BindRuntime();
            BindButtons();
            BuildBrushButtons();
            RefreshAll();
            RefreshCameraLayout();
        }

        private void LateUpdate()
        {
            if (!followCamera || targetCamera == null)
            {
                return;
            }

            Transform cameraTransform = targetCamera.transform;
            transform.position = cameraTransform.position + cameraTransform.forward * distanceFromCamera;
            transform.rotation = Quaternion.LookRotation(transform.position - cameraTransform.position, Vector3.up);
            RefreshCameraLayout();
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.StatusChanged -= OnStatusChanged;
                controller.BrushChanged -= OnBrushChanged;
                controller.SelectionChanged -= OnSelectionChanged;
                controller.EditModeChanged -= OnEditModeChanged;
                controller.DraggingChanged -= OnDraggingChanged;
            }
        }

        public void ValidateMap()
        {
            if (exporter == null)
            {
                SetValidationStatus("No exporter", true);
                return;
            }

            MapValidationResult result = exporter.ValidateCurrentMap();
            hasValidationErrors = result == null || !result.ok;
            validationPresenter?.ShowValidation(result);
            SetPanelErrors(hasValidationErrors);
            SetValidationStatus(hasValidationErrors ? "Fix required" : "Valid map", hasValidationErrors);
            if (hasValidationErrors && bottomPanel != null)
            {
                bottomPanel.SetExpanded(true);
            }

            RefreshCounts();
        }

        public void ExportMap()
        {
            if (exporter == null)
            {
                SetValidationStatus("No exporter", true);
                return;
            }

            MapValidationResult validation = exporter.ValidateCurrentMap();
            if (!validation.ok)
            {
                hasValidationErrors = true;
                validationPresenter?.ShowValidation(validation);
                SetPanelErrors(true);
                SetValidationStatus("Export blocked", true);
                if (bottomPanel != null)
                {
                    bottomPanel.SetExpanded(true);
                }
                return;
            }

            MapEditorRuntimeExportResult result = exporter.ExportCurrentMap();
            validationPresenter?.ShowExportResult(result);
            SetValidationStatus(result.ok ? ShortPath(result.path) : "Export failed", !result.ok);
            SetPanelErrors(!result.ok);
        }

        private void BindRuntime()
        {
            if (controller == null)
            {
                return;
            }

            controller.StatusChanged += OnStatusChanged;
            controller.BrushChanged += OnBrushChanged;
            controller.SelectionChanged += OnSelectionChanged;
            controller.EditModeChanged += OnEditModeChanged;
            controller.DraggingChanged += OnDraggingChanged;
        }

        private void BindButtons()
        {
            if (validateButton != null) validateButton.onClick.AddListener(ValidateMap);
            if (exportButton != null) exportButton.onClick.AddListener(ExportMap);
            if (placeButton != null) placeButton.onClick.AddListener(() => controller?.SetEditMode(MapEditorRuntimeEditMode.Place));
            if (moveButton != null) moveButton.onClick.AddListener(() => controller?.SetEditMode(MapEditorRuntimeEditMode.Move));
            if (rotateButton != null) rotateButton.onClick.AddListener(() => controller?.SetEditMode(MapEditorRuntimeEditMode.Rotate));
            if (scaleButton != null) scaleButton.onClick.AddListener(() => controller?.SetEditMode(MapEditorRuntimeEditMode.Scale));
            if (deleteButton != null) deleteButton.onClick.AddListener(() => controller?.DeleteSelected());
            if (clearBrushButton != null) clearBrushButton.onClick.AddListener(() => controller?.ClearBrush());
        }

        private void BuildBrushButtons()
        {
            if (controller == null || brushButtonTemplate == null)
            {
                return;
            }

            ClearList(mapObjectsListRoot);
            ClearList(gameplayMarkersListRoot);
            if (gameplayMarkersListRoot != null)
            {
                gameplayMarkersListRoot.gameObject.SetActive(false);
            }

            IReadOnlyList<MapEditorRuntimeBrush> brushes = controller.Brushes;
            SortedDictionary<string, List<int>> folders = new SortedDictionary<string, List<int>>(System.StringComparer.Ordinal);
            for (int i = 0; i < brushes.Count; i++)
            {
                MapEditorRuntimeBrush brush = brushes[i];
                string folder = string.IsNullOrEmpty(brush.folder) ? DefaultFolderName(brush) : brush.folder;
                if (!folders.TryGetValue(folder, out List<int> indices))
                {
                    indices = new List<int>();
                    folders.Add(folder, indices);
                }

                indices.Add(i);
            }

            foreach (KeyValuePair<string, List<int>> folder in folders)
            {
                bool expanded = !folderExpanded.TryGetValue(folder.Key, out bool value) || value;
                Button folderButton = Instantiate(brushButtonTemplate, mapObjectsListRoot);
                folderButton.gameObject.SetActive(true);
                SetButtonLabel(folderButton, (expanded ? "v " : "> ") + folder.Key + " (" + folder.Value.Count + ")");
                string folderName = folder.Key;
                folderButton.onClick.AddListener(() =>
                {
                    folderExpanded[folderName] = !expanded;
                    BuildBrushButtons();
                });

                if (!expanded)
                {
                    continue;
                }

                for (int i = 0; i < folder.Value.Count; i++)
                {
                    int index = folder.Value[i];
                    MapEditorRuntimeBrush brush = brushes[index];
                    Button button = Instantiate(brushButtonTemplate, mapObjectsListRoot);
                    button.gameObject.SetActive(true);
                    SetButtonLabel(button, "  " + brush.DisplayName);
                    button.onClick.AddListener(() => controller.SelectBrush(index));
                }
            }

            brushButtonTemplate.gameObject.SetActive(false);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            Text text = button != null ? button.GetComponentInChildren<Text>() : null;
            if (text != null)
            {
                text.text = label;
                text.alignment = TextAnchor.MiddleLeft;
            }
        }

        private static string DefaultFolderName(MapEditorRuntimeBrush brush)
        {
            if (brush == null)
            {
                return "Imported";
            }

            return brush.marker_type == MapExportMarkerType.MapObject ? "MapObjects" : "GameplayMarkers";
        }

        private void RefreshAll()
        {
            if (exporter != null)
            {
                if (mapNameText != null) mapNameText.text = exporter.MapName;
                if (mapIdText != null) mapIdText.text = "map_id: " + MapSceneJsonBuilder.ToMapId(exporter.MapName);
            }

            if (versionText != null)
            {
                versionText.text = "v1.0.0";
            }

            if (controller != null)
            {
                OnEditModeChanged(controller.EditMode);
                OnStatusChanged(controller.StatusMessage);
            }

            RefreshCounts();
            RefreshStepTexts();
            SetValidationStatus("Not validated", false);
        }

        private void RefreshCounts()
        {
            MapExportMarker[] markers = FindObjectsOfType<MapExportMarker>(true);
            int redBases = 0;
            int blueBases = 0;
            int treasures = 0;
            for (int i = 0; i < markers.Length; i++)
            {
                MapExportMarker marker = markers[i];
                if (marker.marker_type == MapExportMarkerType.TeamBase && marker.team == TreasureArenaMR.Shared.TeamType.Red) redBases++;
                if (marker.marker_type == MapExportMarkerType.TeamBase && marker.team == TreasureArenaMR.Shared.TeamType.Blue) blueBases++;
                if (marker.marker_type == MapExportMarkerType.TreasureSpawnPoint) treasures++;
            }

            if (countText != null)
            {
                countText.text = $"Objects: {markers.Length}  Red: {redBases}  Blue: {blueBases}  Treasure: {treasures}";
            }
        }

        private void OnStatusChanged(string value)
        {
            validationPresenter?.AddLog(value);
            RefreshCounts();
        }

        private void OnBrushChanged(int _)
        {
            RefreshCounts();
        }

        private void OnSelectionChanged(GameObject selected)
        {
            objectInspector?.Refresh(selected);
            if (selected != null && rightPanel != null)
            {
                rightPanel.SetExpanded(true);
            }
        }

        private void OnEditModeChanged(MapEditorRuntimeEditMode mode)
        {
            if (modeText != null)
            {
                modeText.text = "Mode: " + mode;
            }

            RefreshStepTexts();
        }

        private void OnDraggingChanged(bool dragging)
        {
            topPanel?.SetDimmed(dragging);
            leftPanel?.SetDimmed(dragging);
            rightPanel?.SetDimmed(dragging);
            bottomPanel?.SetDimmed(dragging);
        }

        private void SetValidationStatus(string text, bool isError)
        {
            if (validationStatusText != null)
            {
                validationStatusText.text = text;
                validationStatusText.color = isError ? new Color(1f, 0.35f, 0.35f) : Color.white;
            }
        }

        private void SetPanelErrors(bool hasErrors)
        {
            topPanel?.SetErrorState(hasErrors);
            leftPanel?.SetErrorState(hasErrors);
            rightPanel?.SetErrorState(hasErrors);
            bottomPanel?.SetErrorState(hasErrors);
        }

        private void ClearList(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.GetChild(i).gameObject;
                if (brushButtonTemplate != null && child == brushButtonTemplate.gameObject)
                {
                    continue;
                }

                Destroy(child);
            }
        }

        private void RefreshStepTexts()
        {
            if (controller == null)
            {
                return;
            }

            if (gridStepText != null)
            {
                gridStepText.text = "Grid " + controller.GridSize.ToString("0.##") + "m";
            }

            if (rotateStepText != null)
            {
                rotateStepText.text = "Rotate " + controller.RotateStepDegrees.ToString("0.#") + " deg";
            }

            if (scaleStepText != null)
            {
                scaleStepText.text = "Scale " + controller.ScaleStep.ToString("0.##");
            }
        }

        private void RefreshCameraLayout()
        {
            if (!fitToCameraView || targetCamera == null)
            {
                return;
            }

            RectTransform rootRect = transform as RectTransform;
            if (rootRect == null)
            {
                return;
            }

            float aspect = Mathf.Max(0.1f, targetCamera.aspect);
            float designWidth = Mathf.Max(minimumDesignWidth, designHeight * aspect);
            rootRect.sizeDelta = new Vector2(designWidth, designHeight);

            float viewHeight = targetCamera.orthographic
                ? targetCamera.orthographicSize * 2f
                : 2f * distanceFromCamera * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float viewWidth = viewHeight * aspect;
            Vector2 size = rootRect.sizeDelta;
            float scale = Mathf.Min(viewWidth * viewportCoverage / size.x, viewHeight * viewportCoverage / size.y);
            transform.localScale = Vector3.one * scale;

            LayoutPanel(topPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(designWidth - panelMargin * 2f, 82f), new Vector2(0f, -panelMargin));
            LayoutPanel(bottomPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(designWidth - panelMargin * 2f, 132f), new Vector2(0f, panelMargin));
            LayoutPanel(leftPanel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(260f, 520f), new Vector2(panelMargin, 0f));
            LayoutPanel(rightPanel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(300f, 520f), new Vector2(-panelMargin, 0f));
        }

        private static void LayoutPanel(MapEditorDockedPanel panel, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 position)
        {
            if (panel == null)
            {
                return;
            }

            RectTransform rect = panel.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            RectTransform expanded = rect.Find("Expanded") as RectTransform;
            if (expanded != null)
            {
                expanded.sizeDelta = size;
            }

            RectTransform collapsed = rect.Find("Collapsed") as RectTransform;
            if (collapsed != null)
            {
                collapsed.sizeDelta = new Vector2(Mathf.Min(260f, size.x), 42f);
            }

            panel.RefreshHitArea();
        }

        private static string ShortPath(string path)
        {
            if (string.IsNullOrEmpty(path) || path.Length <= 48)
            {
                return path;
            }

            return "..." + path.Substring(path.Length - 45);
        }
    }
}
