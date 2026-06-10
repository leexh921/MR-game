using System.Collections.Generic;
using TreasureArenaMR.Map;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.MapEditor
{
    public sealed class MapEditorValidationPresenter : MonoBehaviour
    {
        [SerializeField] private Text recentLogText;
        [SerializeField] private Text errorListText;
        [SerializeField] private int maxLogEntries = 10;

        private readonly List<string> logs = new List<string>();

        public bool HasErrors { get; private set; }

        public void AddLog(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            logs.Insert(0, message);
            while (logs.Count > maxLogEntries)
            {
                logs.RemoveAt(logs.Count - 1);
            }

            RefreshLogs();
        }

        public void ShowValidation(MapValidationResult result)
        {
            HasErrors = result != null && !result.ok;
            if (result == null)
            {
                SetErrorText("Validation result is null.");
                AddLog("Map validation failed: result is null.");
                return;
            }

            if (result.ok)
            {
                SetErrorText("No validation errors.");
                AddLog("Map validation passed.");
                return;
            }

            string errorText = result.GetErrorText();
            SetErrorText(errorText);
            AddLog("Map validation failed: " + errorText);
        }

        public void ShowExportResult(MapEditorRuntimeExportResult result)
        {
            if (result == null)
            {
                AddLog("Map export failed: result is null.");
                return;
            }

            AddLog(result.ok ? "Map exported: " + result.path : "Map export failed: " + result.error);
        }

        private void RefreshLogs()
        {
            if (recentLogText == null)
            {
                return;
            }

            recentLogText.text = logs.Count == 0 ? "Ready." : logs[0];
        }

        private void SetErrorText(string value)
        {
            if (errorListText != null)
            {
                errorListText.text = value;
            }
        }
    }
}
