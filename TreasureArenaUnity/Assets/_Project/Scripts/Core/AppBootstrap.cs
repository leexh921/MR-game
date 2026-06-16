using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
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

        private Collider mapEditorButtonCollider;
        private Collider gameClientButtonCollider;
        private LineRenderer pointerLine;
        private bool previousTriggerPressed;

        private void Start()
        {
            if (SceneManager.GetActiveScene().name != ProjectConstants.SceneBoot)
            {
                return;
            }

            EnsureCamera();
            EnsureEventSystem();
            CreateBootMenu();
        }

        private void Update()
        {
            if (SceneManager.GetActiveScene().name != ProjectConstants.SceneBoot)
            {
                return;
            }

            HandlePicoPointer();
        }

        public void EnterMapEditor()
        {
            SceneManager.LoadScene(mapEditorSceneName);
        }

        public void EnterGameClient()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.06f, 1f);
            camera.nearClipPlane = 0.05f;
            cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.position = new Vector3(0f, 1.6f, -2.4f);
            cameraGo.transform.rotation = Quaternion.identity;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemGo = new GameObject("EventSystem");
                eventSystem = eventSystemGo.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
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

            Button mapEditorButton = CreateButton(canvasGo.transform, "MapEditorButton", "地图编辑器", font, new Vector2(0f, 20f), EnterMapEditor);
            Button gameClientButton = CreateButton(canvasGo.transform, "GameClientButton", "游戏客户端", font, new Vector2(0f, -90f), EnterGameClient);
            mapEditorButtonCollider = mapEditorButton.GetComponent<Collider>();
            gameClientButtonCollider = gameClientButton.GetComponent<Collider>();

            pointerLine = new GameObject("BootMenuPointer").AddComponent<LineRenderer>();
            pointerLine.positionCount = 2;
            pointerLine.startWidth = 0.01f;
            pointerLine.endWidth = 0.004f;
            pointerLine.material = new Material(Shader.Find("Sprites/Default"));
            pointerLine.startColor = new Color(0.2f, 0.85f, 1f, 1f);
            pointerLine.endColor = new Color(0.2f, 0.85f, 1f, 0.15f);
            pointerLine.enabled = false;
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
            BoxCollider collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(rect.sizeDelta.x, rect.sizeDelta.y, 20f);
            collider.center = Vector3.zero;

            Text text = CreateText(go.transform, "Text", label, font, 30, Vector2.zero, rect.sizeDelta, FontStyle.Bold);
            text.color = Color.white;
            return button;
        }

        private void HandlePicoPointer()
        {
            InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!rightHand.isValid
                || !rightHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position)
                || !rightHand.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                SetPointerLine(false, Vector3.zero, Vector3.zero);
                previousTriggerPressed = false;
                return;
            }

            bool triggerPressed = false;
            rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);

            Ray ray = new Ray(position, rotation * Vector3.forward);
            Vector3 end = ray.origin + ray.direction * 6f;
            if (Physics.Raycast(ray, out RaycastHit hit, 6f))
            {
                end = hit.point;
                if (triggerPressed && !previousTriggerPressed)
                {
                    if (hit.collider == mapEditorButtonCollider)
                    {
                        EnterMapEditor();
                    }
                    else if (hit.collider == gameClientButtonCollider)
                    {
                        EnterGameClient();
                    }
                }
            }

            SetPointerLine(true, ray.origin, end);
            previousTriggerPressed = triggerPressed;
        }

        private void SetPointerLine(bool visible, Vector3 start, Vector3 end)
        {
            if (pointerLine == null)
            {
                return;
            }

            pointerLine.enabled = visible;
            if (!visible)
            {
                return;
            }

            pointerLine.SetPosition(0, start);
            pointerLine.SetPosition(1, end);
        }
    }
}
