using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace TreasureArenaMR.Core
{
    /// <summary>
    /// Boot scene entry point for selecting the map editor or game client flow.
    /// </summary>
    public sealed class AppBootstrap : MonoBehaviour
    {
        [SerializeField] private string mapEditorSceneName = "MapEditor";
        [SerializeField] private string gameSceneName = "Game";

        private void Start()
        {
            if (SceneManager.GetActiveScene().name != ProjectConstants.SceneBoot)
            {
                return;
            }

            EnsureEventSystem();
            CreateBootMenu();
        }

        public void EnterMapEditor()
        {
            SceneManager.LoadScene(mapEditorSceneName);
        }

        public void EnterGameClient()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemGo = new GameObject("EventSystem");
                eventSystem = eventSystemGo.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<XRUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        private void CreateBootMenu()
        {
            Canvas existing = FindObjectOfType<Canvas>();
            if (existing != null && existing.name == "BootMenuCanvas")
            {
                return;
            }

            GameObject canvasGo = new GameObject("BootMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(900f, 520f);
            canvasGo.transform.position = new Vector3(0f, 1.55f, 2.0f);
            canvasGo.transform.rotation = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one * 0.0028f;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateText(canvasGo.transform, "Title", "Treasure Arena MR", font, 42, new Vector2(0f, 160f), new Vector2(820f, 70f), FontStyle.Bold);
            CreateText(canvasGo.transform, "Subtitle", "Select entry", font, 24, new Vector2(0f, 100f), new Vector2(820f, 44f), FontStyle.Normal);

            CreateButton(canvasGo.transform, "MapEditorButton", "地图编辑器", font, new Vector2(0f, 20f), EnterMapEditor);
            CreateButton(canvasGo.transform, "GameClientButton", "游戏客户端", font, new Vector2(0f, -90f), EnterGameClient);
        }

        private static Text CreateText(Transform parent, string name, string value, Font font, int fontSize, Vector2 position, Vector2 size, FontStyle style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Text text = go.GetComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Font font, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(520f, 76f);
            rect.anchoredPosition = position;

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.14f, 0.45f, 0.75f, 0.95f);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            Text text = CreateText(go.transform, "Text", label, font, 30, Vector2.zero, rect.sizeDelta, FontStyle.Bold);
            text.color = Color.white;
            return button;
        }
    }
}
