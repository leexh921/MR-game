using System.Collections.Generic;
using TreasureArenaMR.Map;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.MapEditor
{
    public sealed class MapEditorRuntimeUiBinder : MonoBehaviour
    {
        [SerializeField] private MapEditorRuntimeController controller;
        [SerializeField] private MapEditorRuntimeExporter exporter;
        [SerializeField] private Dropdown brushDropdown;
        [SerializeField] private Text statusText;
        [SerializeField] private Text selectedText;
        [SerializeField] private Text modeText;
        [SerializeField] private Button clearBrushButton;
        [SerializeField] private Button nextModeButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button validateButton;
        [SerializeField] private Button exportButton;

        private bool suppressDropdownEvent;

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

            BindController();
            BindButtons();
            PopulateBrushDropdown();
            RefreshLabels();
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.StatusChanged -= OnStatusChanged;
                controller.SelectionChanged -= OnSelectionChanged;
                controller.EditModeChanged -= OnEditModeChanged;
                controller.BrushChanged -= OnBrushChanged;
            }

            if (brushDropdown != null)
            {
                brushDropdown.onValueChanged.RemoveListener(OnBrushDropdownChanged);
            }

            if (clearBrushButton != null)
            {
                clearBrushButton.onClick.RemoveListener(OnClearBrushClicked);
            }

            if (nextModeButton != null)
            {
                nextModeButton.onClick.RemoveListener(OnNextModeClicked);
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveListener(OnDeleteClicked);
            }

            if (validateButton != null)
            {
                validateButton.onClick.RemoveListener(ValidateMap);
            }

            if (exportButton != null)
            {
                exportButton.onClick.RemoveListener(ExportMap);
            }
        }

        public void ValidateMap()
        {
            if (exporter == null)
            {
                SetStatus("No runtime exporter is assigned.");
                return;
            }

            MapValidationResult result = exporter.ValidateCurrentMap();
            SetStatus(result.ok ? "Map validation passed." : "Map validation failed: " + result.GetErrorText());
        }

        public void ExportMap()
        {
            if (exporter == null)
            {
                SetStatus("No runtime exporter is assigned.");
                return;
            }

            MapEditorRuntimeExportResult result = exporter.ExportCurrentMap();
            if (!result.ok)
            {
                SetStatus("Map export failed: " + result.error);
                return;
            }

            string message = "Map exported: " + result.path;
            if (!string.IsNullOrEmpty(result.publicPath))
            {
                message += "\nPublic copy: " + result.publicPath;
            }

            SetStatus(message);
        }

        private void BindController()
        {
            if (controller == null)
            {
                SetStatus("No runtime controller is assigned.");
                return;
            }

            controller.StatusChanged += OnStatusChanged;
            controller.SelectionChanged += OnSelectionChanged;
            controller.EditModeChanged += OnEditModeChanged;
            controller.BrushChanged += OnBrushChanged;
        }

        private void BindButtons()
        {
            if (clearBrushButton != null)
            {
                clearBrushButton.onClick.AddListener(OnClearBrushClicked);
            }

            if (nextModeButton != null)
            {
                nextModeButton.onClick.AddListener(OnNextModeClicked);
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(OnDeleteClicked);
            }

            if (validateButton != null)
            {
                validateButton.onClick.AddListener(ValidateMap);
            }

            if (exportButton != null)
            {
                exportButton.onClick.AddListener(ExportMap);
            }
        }

        private void PopulateBrushDropdown()
        {
            if (brushDropdown == null || controller == null)
            {
                return;
            }

            brushDropdown.ClearOptions();
            List<string> options = new List<string> { "No Brush" };
            IReadOnlyList<MapEditorRuntimeBrush> brushes = controller.Brushes;
            for (int i = 0; i < brushes.Count; i++)
            {
                options.Add(brushes[i].DisplayName);
            }

            brushDropdown.AddOptions(options);
            brushDropdown.onValueChanged.AddListener(OnBrushDropdownChanged);
        }

        private void RefreshLabels()
        {
            if (controller == null)
            {
                return;
            }

            OnStatusChanged(controller.StatusMessage);
            OnSelectionChanged(controller.SelectedObject);
            OnEditModeChanged(controller.EditMode);
            OnBrushChanged(controller.ActiveBrushIndex);
        }

        private void OnStatusChanged(string value)
        {
            SetStatus(value);
        }

        private void OnSelectionChanged(GameObject selected)
        {
            if (selectedText != null)
            {
                selectedText.text = selected == null ? "Selected: none" : "Selected: " + selected.name;
            }
        }

        private void OnEditModeChanged(MapEditorRuntimeEditMode mode)
        {
            if (modeText != null)
            {
                modeText.text = "Mode: " + mode;
            }
        }

        private void OnBrushChanged(int index)
        {
            if (brushDropdown != null && brushDropdown.value != index + 1)
            {
                suppressDropdownEvent = true;
                brushDropdown.value = index + 1;
                suppressDropdownEvent = false;
            }
        }

        private void OnBrushDropdownChanged(int value)
        {
            if (suppressDropdownEvent || controller == null)
            {
                return;
            }

            controller.SelectBrush(value - 1);
        }

        private void OnClearBrushClicked()
        {
            controller?.ClearBrush();
        }

        private void OnNextModeClicked()
        {
            controller?.CycleEditMode();
        }

        private void OnDeleteClicked()
        {
            controller?.DeleteSelected();
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
            {
                statusText.text = value;
            }
        }
    }
}
