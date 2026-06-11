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
        [SerializeField] private float workbenchDistance = 0.95f;
        [SerializeField] private float workbenchVerticalOffset = 0.45f;
        [SerializeField] private float workbenchTiltDegrees = 28f;
        [SerializeField] private Vector2 workbenchDesignSize = new Vector2(1200f, 560f);
        [SerializeField] private float workbenchWorldScale = 0.001f;

        [Header("Panels")]
        [SerializeField] private MapEditorDockedPanel leftInfoPanel;
        [SerializeField] private MapEditorDockedPanel mainBrushPanel;
        [SerializeField] private MapEditorDockedPanel rightInspectorPanel;
        [SerializeField] private MapEditorValidationPresenter validationPresenter;
        [SerializeField] private MapEditorObjectInspector objectInspector;

        [Header("Info Area")]
        [SerializeField] private Text mapNameText;
        [SerializeField] private Text mapIdText;
        [SerializeField] private Text versionText;
        [SerializeField] private Text modeText;
        [SerializeField] private Text countText;
        [SerializeField] private Text validationStatusText;
        [SerializeField] private Button validateButton;
        [SerializeField] private Button exportButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button previewButton;
        [SerializeField] private Button clearMapButton;
        [SerializeField] private Button mapSettingsButton;

        [Header("Brush Area")]
        [SerializeField] private Transform mapObjectsListRoot;
        [SerializeField] private Transform gameplayMarkersListRoot;
        [SerializeField] private Button brushButtonTemplate;
        [SerializeField] private Text leftHandBrushText;
        [SerializeField] private Text rightHandBrushText;
        [SerializeField] private Text activeHandText;
        [SerializeField] private Text hintText;

        [Header("Edit Mode Area")]
        [SerializeField] private Button placeButton;
        [SerializeField] private Button moveButton;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button scaleButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button clearBrushButton;
        [SerializeField] private Text gridStepText;
        [SerializeField] private Text rotateStepText;
        [SerializeField] private Text scaleStepText;

        private readonly List<BrushButtonBinding> brushButtons = new List<BrushButtonBinding>();

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
            RecenterWorkbench(targetCamera);
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.StatusChanged -= OnStatusChanged;
                controller.BrushChanged -= OnBrushChanged;
                controller.HandBrushChanged -= OnHandBrushChanged;
                controller.SelectionChanged -= OnSelectionChanged;
                controller.EditModeChanged -= OnEditModeChanged;
                controller.HandEditModeChanged -= OnHandEditModeChanged;
                controller.ActiveHandChanged -= OnActiveHandChanged;
                controller.DraggingChanged -= OnDraggingChanged;
                controller.WorkbenchToggleRequested -= ToggleWorkbench;
                controller.WorkbenchRecenterRequested -= OnWorkbenchRecenterRequested;
            }
        }

        public void ShowWorkbench()
        {
            gameObject.SetActive(true);
        }

        public void HideWorkbench()
        {
            gameObject.SetActive(false);
        }

        public void ToggleWorkbench()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        public void RecenterWorkbench(Camera cameraOverride = null)
        {
            Camera camera = cameraOverride != null ? cameraOverride : targetCamera;
            if (camera == null)
            {
                camera = Camera.main;
            }

            gameObject.SetActive(true);

            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.sizeDelta = workbenchDesignSize;
            }

            transform.localScale = Vector3.one * workbenchWorldScale;

            if (camera == null)
            {
                transform.position = new Vector3(0f, 0.95f, 0.9f);
                transform.rotation = Quaternion.Euler(60f, 180f, 0f);
                return;
            }

            Transform cameraTransform = camera.transform;
            Vector3 flatForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.001f)
            {
                flatForward = Vector3.ProjectOnPlane(cameraTransform.up, Vector3.up);
            }

            flatForward.Normalize();
            transform.position = cameraTransform.position
                + flatForward * workbenchDistance
                - Vector3.up * workbenchVerticalOffset;

            Quaternion faceUser = Quaternion.LookRotation(cameraTransform.position - transform.position, Vector3.up);
            transform.rotation = faceUser * Quaternion.Euler(workbenchTiltDegrees, 0f, 0f);
        }

        public void ValidateMap()
        {
            if (exporter == null)
            {
                SetValidationStatus("No exporter", true);
                return;
            }

            MapValidationResult result = exporter.ValidateCurrentMap();
            bool hasValidationErrors = result == null || !result.ok;
            validationPresenter?.ShowValidation(result);
            SetPanelErrors(hasValidationErrors);
            SetValidationStatus(hasValidationErrors ? "Fix required" : "Valid map", hasValidationErrors);
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
                validationPresenter?.ShowValidation(validation);
                SetPanelErrors(true);
                SetValidationStatus("Export blocked", true);
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
            controller.HandBrushChanged += OnHandBrushChanged;
            controller.SelectionChanged += OnSelectionChanged;
            controller.EditModeChanged += OnEditModeChanged;
            controller.HandEditModeChanged += OnHandEditModeChanged;
            controller.ActiveHandChanged += OnActiveHandChanged;
            controller.DraggingChanged += OnDraggingChanged;
            controller.WorkbenchToggleRequested += ToggleWorkbench;
            controller.WorkbenchRecenterRequested += OnWorkbenchRecenterRequested;
        }

        private void BindButtons()
        {
            BindPlainButton(validateButton, ValidateMap);
            BindPlainButton(exportButton, ExportMap);
            BindPlainButton(saveButton, () => AddUiLog("Save is TODO for MapEditor MVP."));
            BindPlainButton(loadButton, () => AddUiLog("Load is TODO for MapEditor MVP."));
            BindPlainButton(previewButton, () => AddUiLog("Preview Load is TODO until ME-4."));
            BindPlainButton(clearMapButton, () => AddUiLog("Clear Map requires confirmation; TODO."));
            BindPlainButton(mapSettingsButton, () => AddUiLog("Map Settings panel is TODO."));

            BindHandButton(placeButton, hand => controller?.SetEditMode(MapEditorRuntimeEditMode.Place, hand), () => controller?.SetEditMode(MapEditorRuntimeEditMode.Place));
            BindHandButton(moveButton, hand => controller?.SetEditMode(MapEditorRuntimeEditMode.Move, hand), () => controller?.SetEditMode(MapEditorRuntimeEditMode.Move));
            BindHandButton(rotateButton, hand => controller?.SetEditMode(MapEditorRuntimeEditMode.Rotate, hand), () => controller?.SetEditMode(MapEditorRuntimeEditMode.Rotate));
            BindHandButton(scaleButton, hand => controller?.SetEditMode(MapEditorRuntimeEditMode.Scale, hand), () => controller?.SetEditMode(MapEditorRuntimeEditMode.Scale));
            BindHandButton(clearBrushButton, hand => controller?.ClearBrush(hand), () => controller?.ClearBrush());
            BindPlainButton(deleteButton, () => controller?.DeleteSelected());
        }

        private void BuildBrushButtons()
        {
            brushButtons.Clear();
            if (controller == null || brushButtonTemplate == null)
            {
                return;
            }

            ClearList(mapObjectsListRoot);
            ClearList(gameplayMarkersListRoot);

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
                Transform root = IsGameplayFolder(folder.Key) && gameplayMarkersListRoot != null
                    ? gameplayMarkersListRoot
                    : mapObjectsListRoot;

                if (root == null)
                {
                    continue;
                }

                for (int i = 0; i < folder.Value.Count; i++)
                {
                    int index = folder.Value[i];
                    MapEditorRuntimeBrush brush = brushes[index];
                    Button button = Instantiate(brushButtonTemplate, root);
                    button.gameObject.SetActive(true);
                    ConfigureBrushButton(button, index, brush);
                }
            }

            brushButtonTemplate.gameObject.SetActive(false);
            RefreshBrushHandMarkers();
        }

        private void ConfigureBrushButton(Button button, int index, MapEditorRuntimeBrush brush)
        {
            Text nameText = FindChildText(button.transform, "NameText");
            Text typeText = FindChildText(button.transform, "TypeText");
            Text handText = FindChildText(button.transform, "HandMarkerText");
            if (nameText != null)
            {
                nameText.text = brush.DisplayName;
            }

            if (typeText != null)
            {
                typeText.text = BrushKindLabel(brush);
            }

            if (handText != null)
            {
                handText.text = "";
            }

            button.onClick.AddListener(() => controller?.SelectBrush(index));
            MapEditorRuntimeUiHitTarget hitTarget = button.GetComponent<MapEditorRuntimeUiHitTarget>();
            if (hitTarget == null)
            {
                hitTarget = button.gameObject.AddComponent<MapEditorRuntimeUiHitTarget>();
            }

            hitTarget.HandActivated += hand => controller?.SelectBrush(index, hand);
            brushButtons.Add(new BrushButtonBinding(index, handText));
        }

        private void RefreshAll()
        {
            if (exporter != null)
            {
                if (mapNameText != null) mapNameText.text = "Map: " + exporter.MapName;
                if (mapIdText != null) mapIdText.text = "map_id: " + MapSceneJsonBuilder.ToMapId(exporter.MapName);
            }

            if (versionText != null)
            {
                versionText.text = "version: 1.0.0";
            }

            if (controller != null)
            {
                OnEditModeChanged(controller.EditMode);
                OnStatusChanged(controller.StatusMessage);
            }

            RefreshCounts();
            RefreshStepTexts();
            RefreshHandStatus();
            RefreshBrushHandMarkers();
            SetValidationStatus("Not validated", false);
        }

        private void RefreshCounts()
        {
            MapExportMarker[] markers = FindObjectsOfType<MapExportMarker>(true);
            int redBases = 0;
            int blueBases = 0;
            int treasures = 0;
            int supplyBoxes = 0;
            int bounds = 0;
            for (int i = 0; i < markers.Length; i++)
            {
                MapExportMarker marker = markers[i];
                if (marker.marker_type == MapExportMarkerType.TeamBase && marker.team == TreasureArenaMR.Shared.TeamType.Red) redBases++;
                if (marker.marker_type == MapExportMarkerType.TeamBase && marker.team == TreasureArenaMR.Shared.TeamType.Blue) blueBases++;
                if (marker.marker_type == MapExportMarkerType.TreasureSpawnPoint) treasures++;
                if (marker.marker_type == MapExportMarkerType.SupplyBox) supplyBoxes++;
                if (marker.marker_type == MapExportMarkerType.Bounds) bounds++;
            }

            if (countText != null)
            {
                countText.text = $"Objects: {markers.Length}\nRed Base: {redBases}\nBlue Base: {blueBases}\nTreasure: {treasures}\nSupply: {supplyBoxes}\nBounds: {bounds}";
            }
        }

        private void RefreshHandStatus()
        {
            if (controller == null)
            {
                return;
            }

            if (leftHandBrushText != null)
            {
                leftHandBrushText.text = "Left Hand: " + controller.GetBrushDisplayName(MapEditorHand.Left);
            }

            if (rightHandBrushText != null)
            {
                rightHandBrushText.text = "Right Hand: " + controller.GetBrushDisplayName(MapEditorHand.Right);
            }

            if (activeHandText != null)
            {
                activeHandText.text = "Active: " + controller.ActiveHand;
            }

            if (hintText != null)
            {
                hintText.text = "Hint: Aim at a surface and press Trigger.";
            }
        }

        private void RefreshBrushHandMarkers()
        {
            if (controller == null)
            {
                return;
            }

            for (int i = 0; i < brushButtons.Count; i++)
            {
                BrushButtonBinding binding = brushButtons[i];
                if (binding.handText == null)
                {
                    continue;
                }

                bool left = controller.LeftBrushIndex == binding.index;
                bool right = controller.RightBrushIndex == binding.index;
                binding.handText.text = left && right ? "L/R" : left ? "L" : right ? "R" : "";
            }
        }

        private void OnStatusChanged(string value)
        {
            validationPresenter?.AddLog(value);
            RefreshCounts();
        }

        private void OnBrushChanged(int _)
        {
            RefreshHandStatus();
            RefreshBrushHandMarkers();
        }

        private void OnHandBrushChanged(MapEditorHand hand, int index)
        {
            RefreshHandStatus();
            RefreshBrushHandMarkers();
        }

        private void OnSelectionChanged(GameObject selected)
        {
            objectInspector?.Refresh(selected);
        }

        private void OnEditModeChanged(MapEditorRuntimeEditMode mode)
        {
            if (modeText != null && controller != null)
            {
                modeText.text = "Mode: " + mode + " (" + controller.ActiveHand + ")";
            }

            RefreshStepTexts();
        }

        private void OnHandEditModeChanged(MapEditorHand hand, MapEditorRuntimeEditMode mode)
        {
            OnEditModeChanged(controller != null ? controller.EditMode : mode);
        }

        private void OnActiveHandChanged(MapEditorHand hand)
        {
            RefreshHandStatus();
            if (controller != null)
            {
                OnEditModeChanged(controller.EditMode);
            }
        }

        private void OnDraggingChanged(bool dragging)
        {
            leftInfoPanel?.SetDimmed(dragging);
            mainBrushPanel?.SetDimmed(dragging);
            rightInspectorPanel?.SetDimmed(dragging);
        }

        private void OnWorkbenchRecenterRequested()
        {
            RecenterWorkbench(targetCamera);
        }

        private void SetValidationStatus(string text, bool isError)
        {
            if (validationStatusText != null)
            {
                validationStatusText.text = "Validation: " + text;
                validationStatusText.color = isError ? new Color(1f, 0.35f, 0.35f) : Color.white;
            }
        }

        private void SetPanelErrors(bool hasErrors)
        {
            leftInfoPanel?.SetErrorState(hasErrors);
            mainBrushPanel?.SetErrorState(hasErrors);
            rightInspectorPanel?.SetErrorState(hasErrors);
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

        private void AddUiLog(string value)
        {
            validationPresenter?.AddLog(value);
        }

        private static void BindPlainButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void BindHandButton(Button button, System.Action<MapEditorHand> handAction, UnityEngine.Events.UnityAction mouseAction)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.AddListener(mouseAction);
            MapEditorRuntimeUiHitTarget hitTarget = button.GetComponent<MapEditorRuntimeUiHitTarget>();
            if (hitTarget == null)
            {
                hitTarget = button.gameObject.AddComponent<MapEditorRuntimeUiHitTarget>();
            }

            hitTarget.HandActivated += handAction;
        }

        private static bool IsGameplayFolder(string folder)
        {
            return folder.IndexOf("Gameplay", System.StringComparison.OrdinalIgnoreCase) >= 0
                || folder.IndexOf("Marker", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string DefaultFolderName(MapEditorRuntimeBrush brush)
        {
            if (brush == null)
            {
                return "Imported";
            }

            return brush.marker_type == MapExportMarkerType.MapObject ? "MapObjects" : "GameplayMarkers";
        }

        private static string BrushKindLabel(MapEditorRuntimeBrush brush)
        {
            if (brush == null)
            {
                return "";
            }

            if (brush.marker_type == MapExportMarkerType.TeamBase)
            {
                return brush.team + " Base (Spawn/Respawn/Submit)";
            }

            if (brush.marker_type == MapExportMarkerType.TreasureSpawnPoint)
            {
                return brush.treasure_type + " Treasure";
            }

            return brush.marker_type.ToString();
        }

        private static Text FindChildText(Transform root, string name)
        {
            Transform child = root.Find(name);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static string ShortPath(string path)
        {
            if (string.IsNullOrEmpty(path) || path.Length <= 48)
            {
                return path;
            }

            return "..." + path.Substring(path.Length - 45);
        }

        private readonly struct BrushButtonBinding
        {
            public readonly int index;
            public readonly Text handText;

            public BrushButtonBinding(int index, Text handText)
            {
                this.index = index;
                this.handText = handText;
            }
        }
    }
}
