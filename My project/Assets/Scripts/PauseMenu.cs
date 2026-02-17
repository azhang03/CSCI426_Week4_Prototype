using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Minifantasy
{
    public class PauseMenu : MonoBehaviour
    {
        [Header("Styling")]
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.85f);
        [SerializeField] private Color panelColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color buttonColor = new Color(0.2f, 0.6f, 0.9f, 1f);
        [SerializeField] private Color exitButtonColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color toggleColor = new Color(0.3f, 0.9f, 0.4f, 1f);

        private Canvas canvas;
        private GameObject pauseMenuRoot;
        private bool isPaused = false;
        private VignetteController vignetteController;
        private ExposureMeter exposureMeter;

        void Start()
        {
            vignetteController = FindFirstObjectByType<VignetteController>();
            exposureMeter = FindFirstObjectByType<ExposureMeter>();
            FindOrCreateCanvas();
            CreateUI();
            pauseMenuRoot.SetActive(false);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                    return;

                TogglePause();
            }
        }

        void TogglePause()
        {
            isPaused = !isPaused;
            pauseMenuRoot.SetActive(isPaused);
            Time.timeScale = isPaused ? 0f : 1f;
        }

        public void Resume()
        {
            isPaused = false;
            pauseMenuRoot.SetActive(false);
            Time.timeScale = 1f;
        }

        public void ExitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void FindOrCreateCanvas()
        {
            // Always create our own canvas so we control sorting and raycasting
            GameObject canvasObj = new GameObject("PauseMenuCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            // Ensure an EventSystem exists — without one, no UI clicks are processed
            EventSystem existingES = FindFirstObjectByType<EventSystem>();
            if (existingES != null)
            {
                // Make sure it has the new Input System module
                if (existingES.GetComponent<InputSystemUIInputModule>() == null)
                    existingES.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            else
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<InputSystemUIInputModule>();
            }
        }

        void CreateUI()
        {
            // Fullscreen darkened overlay
            pauseMenuRoot = new GameObject("PauseMenu");
            pauseMenuRoot.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = pauseMenuRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;

            // No background overlay — just the floating panel

            // Center panel
            GameObject centerPanel = new GameObject("CenterPanel");
            centerPanel.transform.SetParent(pauseMenuRoot.transform, false);

            RectTransform panelRect = centerPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(500, 350);

            Image panelImage = centerPanel.AddComponent<Image>();
            panelImage.color = panelColor;
            panelImage.raycastTarget = false;

            VerticalLayoutGroup vlg = centerPanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.padding = new RectOffset(40, 40, 30, 30);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Title
            CreateText(centerPanel.transform, "Title", "PAUSED", 36, textColor, 50, FontStyle.Bold);

            // Spacer
            CreateSpacer(centerPanel.transform, 10);

            // Vignette toggle
            CreateVignetteToggle(centerPanel.transform);

            // Exposure Bar toggle
            CreateExposureBarToggle(centerPanel.transform);

            // Spacer
            CreateSpacer(centerPanel.transform, 10);

            // Resume button
            CreateButton(centerPanel.transform, "RESUME", buttonColor, Resume);

            // Exit button
            CreateButton(centerPanel.transform, "EXIT", exitButtonColor, ExitGame);
        }

        void CreateVignetteToggle(Transform parent)
        {
            GameObject toggleObj = new GameObject("VignetteToggle");
            toggleObj.transform.SetParent(parent, false);

            RectTransform rt = toggleObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 36);

            HorizontalLayoutGroup hlg = toggleObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Checkbox background
            GameObject boxBg = CreateUIRect(toggleObj.transform, "Background", new Vector2(28, 28), new Color(0.25f, 0.25f, 0.25f, 1f));

            // Checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(boxBg.transform, false);

            RectTransform checkRT = checkmark.AddComponent<RectTransform>();
            checkRT.anchorMin = new Vector2(0.15f, 0.15f);
            checkRT.anchorMax = new Vector2(0.85f, 0.85f);
            checkRT.offsetMin = Vector2.zero;
            checkRT.offsetMax = Vector2.zero;

            Image checkImg = checkmark.AddComponent<Image>();
            checkImg.color = toggleColor;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform, false);

            RectTransform labelRT = labelObj.AddComponent<RectTransform>();
            labelRT.sizeDelta = new Vector2(180, 36);

            Text label = labelObj.AddComponent<Text>();
            label.text = "Vignette";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 22;
            label.color = textColor;
            label.alignment = TextAnchor.MiddleLeft;

            // Toggle component
            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = true;
            toggle.graphic = checkImg;
            toggle.targetGraphic = boxBg.GetComponent<Image>();
            toggle.onValueChanged.AddListener(OnVignetteToggled);
        }

        void OnVignetteToggled(bool isOn)
        {
            if (vignetteController == null)
                vignetteController = FindFirstObjectByType<VignetteController>();

            if (vignetteController != null)
                vignetteController.SetVignetteEnabled(isOn);
            else
                Debug.LogWarning("PauseMenu: Could not find VignetteController in scene.");
        }

        void CreateExposureBarToggle(Transform parent)
        {
            GameObject toggleObj = new GameObject("ExposureBarToggle");
            toggleObj.transform.SetParent(parent, false);

            RectTransform rt = toggleObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 36);

            HorizontalLayoutGroup hlg = toggleObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            GameObject boxBg = CreateUIRect(toggleObj.transform, "Background", new Vector2(28, 28), new Color(0.25f, 0.25f, 0.25f, 1f));

            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(boxBg.transform, false);

            RectTransform checkRT = checkmark.AddComponent<RectTransform>();
            checkRT.anchorMin = new Vector2(0.15f, 0.15f);
            checkRT.anchorMax = new Vector2(0.85f, 0.85f);
            checkRT.offsetMin = Vector2.zero;
            checkRT.offsetMax = Vector2.zero;

            Image checkImg = checkmark.AddComponent<Image>();
            checkImg.color = toggleColor;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform, false);

            RectTransform labelRT = labelObj.AddComponent<RectTransform>();
            labelRT.sizeDelta = new Vector2(180, 36);

            Text label = labelObj.AddComponent<Text>();
            label.text = "Exposure Bar";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 22;
            label.color = textColor;
            label.alignment = TextAnchor.MiddleLeft;

            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = false;
            toggle.graphic = checkImg;
            toggle.targetGraphic = boxBg.GetComponent<Image>();
            toggle.onValueChanged.AddListener(OnExposureBarToggled);
        }

        void OnExposureBarToggled(bool isOn)
        {
            if (exposureMeter == null)
                exposureMeter = FindFirstObjectByType<ExposureMeter>();
            if (vignetteController == null)
                vignetteController = FindFirstObjectByType<VignetteController>();

            if (exposureMeter != null)
                exposureMeter.SetBarVisible(isOn);
            if (vignetteController != null)
                vignetteController.SetStaticEnabled(!isOn);
        }

        void CreateText(Transform parent, string name, string content, int fontSize, Color color, float height, FontStyle style = FontStyle.Normal)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, height);

            Text text = textObj.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        void CreateSpacer(Transform parent, float height)
        {
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(parent, false);

            RectTransform rect = spacer.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, height);

            LayoutElement layout = spacer.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        void CreateButton(Transform parent, string label, Color btnColor, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObj = new GameObject("Button_" + label);
            buttonObj.transform.SetParent(parent, false);

            RectTransform btnRect = buttonObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(250, 50);

            Image btnImage = buttonObj.AddComponent<Image>();
            btnImage.color = btnColor;

            Button btn = buttonObj.AddComponent<Button>();
            btn.targetGraphic = btnImage;
            btn.onClick.AddListener(onClick);

            ColorBlock colors = btn.colors;
            colors.normalColor = btnColor;
            colors.highlightedColor = new Color(btnColor.r + 0.15f, btnColor.g + 0.15f, btnColor.b + 0.15f, 1f);
            colors.pressedColor = new Color(btnColor.r - 0.1f, btnColor.g - 0.1f, btnColor.b - 0.1f, 1f);
            btn.colors = colors;

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            Text btnText = textObj.AddComponent<Text>();
            btnText.text = label;
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 24;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.fontStyle = FontStyle.Bold;
        }

        GameObject CreateUIRect(Transform parent, string name, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = color;

            return obj;
        }
    }
}
