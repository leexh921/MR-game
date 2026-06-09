using System.Globalization;
using TreasureArenaMR.Map;
using TreasureArenaMR.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.MapEditor
{
    public sealed class MapEditorObjectInspector : MonoBehaviour
    {
        [SerializeField] private MapEditorRuntimeController controller;
        [SerializeField] private Text selectedNameText;
        [SerializeField] private Text selectedTypeText;
        [SerializeField] private InputField idInput;
        [SerializeField] private InputField prefabIdInput;
        [SerializeField] private Toggle hasColliderToggle;
        [SerializeField] private InputField positionXInput;
        [SerializeField] private InputField positionYInput;
        [SerializeField] private InputField positionZInput;
        [SerializeField] private InputField rotationXInput;
        [SerializeField] private InputField rotationYInput;
        [SerializeField] private InputField rotationZInput;
        [SerializeField] private InputField scaleXInput;
        [SerializeField] private InputField scaleYInput;
        [SerializeField] private InputField scaleZInput;
        [SerializeField] private Dropdown teamDropdown;
        [SerializeField] private Dropdown treasureTypeDropdown;
        [SerializeField] private InputField radiusInput;
        [SerializeField] private InputField supplyTypeInput;
        [SerializeField] private InputField refreshIntervalInput;

        private GameObject selectedObject;
        private MapExportMarker selectedMarker;
        private bool suppressEvents;

        private void Awake()
        {
            if (controller == null)
            {
                controller = FindObjectOfType<MapEditorRuntimeController>();
            }

            BindController();
            BindInputs();
            SetupDropdowns();
            Refresh(null);
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.SelectionChanged -= Refresh;
            }
        }

        public void Refresh(GameObject selected)
        {
            selectedObject = selected;
            selectedMarker = selectedObject != null ? selectedObject.GetComponent<MapExportMarker>() : null;
            suppressEvents = true;

            if (selectedNameText != null)
            {
                selectedNameText.text = selectedObject == null ? "Selected: none" : "Selected: " + selectedObject.name;
            }

            if (selectedTypeText != null)
            {
                selectedTypeText.text = selectedMarker == null ? "Type: none" : "Type: " + selectedMarker.marker_type;
            }

            SetText(idInput, selectedMarker != null ? selectedMarker.GetDefaultId() : "");
            SetText(prefabIdInput, selectedMarker != null ? selectedMarker.prefab_id : "");
            SetToggle(hasColliderToggle, selectedMarker != null && selectedMarker.has_collider);

            Transform t = selectedObject != null ? selectedObject.transform : null;
            Vector3 position = t != null ? t.position : Vector3.zero;
            Vector3 rotation = t != null ? t.eulerAngles : Vector3.zero;
            Vector3 scale = t != null ? t.localScale : Vector3.one;
            SetText(positionXInput, F(position.x));
            SetText(positionYInput, F(position.y));
            SetText(positionZInput, F(position.z));
            SetText(rotationXInput, F(rotation.x));
            SetText(rotationYInput, F(rotation.y));
            SetText(rotationZInput, F(rotation.z));
            SetText(scaleXInput, F(scale.x));
            SetText(scaleYInput, F(scale.y));
            SetText(scaleZInput, F(scale.z));

            if (selectedMarker != null)
            {
                SetDropdown(teamDropdown, (int)selectedMarker.team);
                SetDropdown(treasureTypeDropdown, (int)selectedMarker.treasure_type);
                SetText(radiusInput, F(selectedMarker.radius));
                SetText(supplyTypeInput, selectedMarker.supply_type);
                SetText(refreshIntervalInput, F(selectedMarker.refresh_interval));
            }
            else
            {
                SetDropdown(teamDropdown, 0);
                SetDropdown(treasureTypeDropdown, 0);
                SetText(radiusInput, "");
                SetText(supplyTypeInput, "");
                SetText(refreshIntervalInput, "");
            }

            RefreshFieldAvailability();
            suppressEvents = false;
        }

        private void BindController()
        {
            if (controller != null)
            {
                controller.SelectionChanged += Refresh;
            }
        }

        private void BindInputs()
        {
            Bind(idInput, ApplyId);
            Bind(positionXInput, _ => ApplyTransform());
            Bind(positionYInput, _ => ApplyTransform());
            Bind(positionZInput, _ => ApplyTransform());
            Bind(rotationXInput, _ => ApplyTransform());
            Bind(rotationYInput, _ => ApplyTransform());
            Bind(rotationZInput, _ => ApplyTransform());
            Bind(scaleXInput, _ => ApplyTransform());
            Bind(scaleYInput, _ => ApplyTransform());
            Bind(scaleZInput, _ => ApplyTransform());
            Bind(radiusInput, ApplyMarkerNumbers);
            Bind(supplyTypeInput, ApplySupplyType);
            Bind(refreshIntervalInput, ApplyMarkerNumbers);

            if (hasColliderToggle != null)
            {
                hasColliderToggle.onValueChanged.AddListener(ApplyHasCollider);
            }

            if (teamDropdown != null)
            {
                teamDropdown.onValueChanged.AddListener(ApplyTeam);
            }

            if (treasureTypeDropdown != null)
            {
                treasureTypeDropdown.onValueChanged.AddListener(ApplyTreasureType);
            }
        }

        private void SetupDropdowns()
        {
            if (teamDropdown != null && teamDropdown.options.Count == 0)
            {
                teamDropdown.AddOptions(new System.Collections.Generic.List<string> { "None", "Red", "Blue" });
            }

            if (treasureTypeDropdown != null && treasureTypeDropdown.options.Count == 0)
            {
                treasureTypeDropdown.AddOptions(new System.Collections.Generic.List<string> { "Normal", "Rare", "Final" });
            }
        }

        private void ApplyId(string value)
        {
            if (suppressEvents || selectedMarker == null)
            {
                return;
            }

            selectedMarker.id = value;
            if (!string.IsNullOrEmpty(value))
            {
                selectedMarker.gameObject.name = value;
            }

            Refresh(selectedObject);
        }

        private void ApplyTransform()
        {
            if (suppressEvents || selectedObject == null)
            {
                return;
            }

            selectedObject.transform.position = new Vector3(
                Parse(positionXInput, selectedObject.transform.position.x),
                Parse(positionYInput, selectedObject.transform.position.y),
                Parse(positionZInput, selectedObject.transform.position.z));
            selectedObject.transform.eulerAngles = new Vector3(
                Parse(rotationXInput, selectedObject.transform.eulerAngles.x),
                Parse(rotationYInput, selectedObject.transform.eulerAngles.y),
                Parse(rotationZInput, selectedObject.transform.eulerAngles.z));
            selectedObject.transform.localScale = new Vector3(
                Mathf.Max(0.01f, Parse(scaleXInput, selectedObject.transform.localScale.x)),
                Mathf.Max(0.01f, Parse(scaleYInput, selectedObject.transform.localScale.y)),
                Mathf.Max(0.01f, Parse(scaleZInput, selectedObject.transform.localScale.z)));
        }

        private void ApplyHasCollider(bool value)
        {
            if (!suppressEvents && selectedMarker != null)
            {
                selectedMarker.has_collider = value;
            }
        }

        private void ApplyTeam(int value)
        {
            if (!suppressEvents && selectedMarker != null)
            {
                selectedMarker.team = (TeamType)Mathf.Clamp(value, 0, 2);
            }
        }

        private void ApplyTreasureType(int value)
        {
            if (!suppressEvents && selectedMarker != null)
            {
                selectedMarker.treasure_type = (TreasureType)Mathf.Clamp(value, 0, 2);
            }
        }

        private void ApplySupplyType(string value)
        {
            if (!suppressEvents && selectedMarker != null)
            {
                selectedMarker.supply_type = value;
            }
        }

        private void ApplyMarkerNumbers(string _)
        {
            if (suppressEvents || selectedMarker == null)
            {
                return;
            }

            selectedMarker.radius = Mathf.Max(0.01f, Parse(radiusInput, selectedMarker.radius));
            selectedMarker.refresh_interval = Mathf.Max(0.01f, Parse(refreshIntervalInput, selectedMarker.refresh_interval));
        }

        private static void Bind(InputField input, UnityEngine.Events.UnityAction<string> action)
        {
            if (input != null)
            {
                input.onEndEdit.AddListener(action);
            }
        }

        private static void SetText(InputField input, string value)
        {
            if (input != null)
            {
                input.text = value;
            }
        }

        private static void SetToggle(Toggle toggle, bool value)
        {
            if (toggle != null)
            {
                toggle.isOn = value;
            }
        }

        private static void SetDropdown(Dropdown dropdown, int value)
        {
            if (dropdown != null)
            {
                dropdown.value = Mathf.Clamp(value, 0, dropdown.options.Count - 1);
            }
        }

        private void RefreshFieldAvailability()
        {
            bool hasMarker = selectedMarker != null;
            MapExportMarkerType markerType = hasMarker ? selectedMarker.marker_type : MapExportMarkerType.MapObject;

            SetInteractable(idInput, hasMarker);
            SetInteractable(prefabIdInput, false);
            SetInteractable(positionXInput, selectedObject != null);
            SetInteractable(positionYInput, selectedObject != null);
            SetInteractable(positionZInput, selectedObject != null);
            SetInteractable(rotationXInput, selectedObject != null && markerType != MapExportMarkerType.Bounds);
            SetInteractable(rotationYInput, selectedObject != null && markerType != MapExportMarkerType.Bounds);
            SetInteractable(rotationZInput, selectedObject != null && markerType != MapExportMarkerType.Bounds);
            SetInteractable(scaleXInput, selectedObject != null);
            SetInteractable(scaleYInput, selectedObject != null);
            SetInteractable(scaleZInput, selectedObject != null);

            SetInteractable(hasColliderToggle, hasMarker && markerType == MapExportMarkerType.MapObject);
            SetInteractable(teamDropdown, hasMarker && markerType == MapExportMarkerType.TeamBase);
            SetInteractable(treasureTypeDropdown, hasMarker && markerType == MapExportMarkerType.TreasureSpawnPoint);
            SetInteractable(radiusInput, hasMarker && (markerType == MapExportMarkerType.TeamBase || markerType == MapExportMarkerType.TreasureSpawnPoint));
            SetInteractable(supplyTypeInput, hasMarker && markerType == MapExportMarkerType.SupplyBox);
            SetInteractable(refreshIntervalInput, hasMarker && markerType == MapExportMarkerType.SupplyBox);
        }

        private static void SetInteractable(Selectable selectable, bool value)
        {
            if (selectable != null)
            {
                selectable.interactable = value;
            }
        }

        private static float Parse(InputField input, float fallback)
        {
            if (input == null || string.IsNullOrEmpty(input.text))
            {
                return fallback;
            }

            return float.TryParse(input.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : fallback;
        }

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
