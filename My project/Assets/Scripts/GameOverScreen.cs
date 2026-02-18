using UnityEngine;
using UnityEngine.UI;

namespace Minifantasy
{
    /// <summary>
    /// Handles win/lose screens and the lose cutscene.
    /// Attach to any GameObject in the scene (e.g. the GameManager object).
    ///
    /// Lose flow:  full-screen image shakes violently while a sound plays →
    ///             image stays still, dimmed overlay + "RENDERED!" menu.
    /// Win flow:   immediate freeze → dimmed overlay + "You did it!" menu.
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        [Header("Lose Cutscene")]
        [Tooltip("Full-screen image shown during the lose cutscene and behind the lose menu.")]
        [SerializeField] private Sprite cutsceneImage;
        [Tooltip("Sound that plays during the lose cutscene shake.")]
        [SerializeField] private AudioClip cutsceneSound;
        [Tooltip("Volume of the cutscene sound.")]
        [Range(0f, 1f)]
        [SerializeField] private float soundVolume = 0.8f;
        [Tooltip("How long the cutscene shakes (seconds). 0 = match the sound clip length.")]
        [SerializeField] private float cutsceneDuration = 0f;
        [Tooltip("Shake offset intensity in reference pixels (1920×1080 space).")]
        [SerializeField] private float shakeIntensity = 25f;

        [Header("Menu Styling")]
        [SerializeField] private Color panelColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        [SerializeField] private Color buttonColor = new Color(0.2f, 0.6f, 0.9f, 1f);

        private Canvas canvas;
        private RectTransform imageRect;
        private AudioSource audioSource;

        private bool cutscenePlaying;
        private float cutsceneTimer;
        private float cutsceneLength;
        private float displayTime;

        private void Update()
        {
            if (!cutscenePlaying) return;

            cutsceneTimer += Time.unscaledDeltaTime;

            if (cutsceneTimer < cutsceneLength)
            {
                float x = Random.Range(-shakeIntensity, shakeIntensity);
                float y = Random.Range(-shakeIntensity, shakeIntensity);
                imageRect.anchoredPosition = new Vector2(x, y);
            }
            else
            {
                cutscenePlaying = false;
                imageRect.anchoredPosition = Vector2.zero;
                ShowMenu(true);
            }
        }

        /* ---------- public API ---------- */

        public void ShowLose(float survivalTime)
        {
            displayTime = survivalTime;
            BuildCanvas();
            BuildFullScreenImage();

            cutsceneLength = cutsceneDuration > 0f
                ? cutsceneDuration
                : (cutsceneSound != null ? cutsceneSound.length : 2f);

            cutsceneTimer = 0f;
            cutscenePlaying = true;

            if (cutsceneSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.clip = cutsceneSound;
                audioSource.volume = soundVolume;
                audioSource.playOnAwake = false;
                audioSource.ignoreListenerPause = true;
                audioSource.Play();
            }
        }

        public void ShowWin(float completionTime)
        {
            displayTime = completionTime;
            BuildCanvas();
            ShowMenu(false);
        }

        /* ---------- UI construction ---------- */

        private void BuildCanvas()
        {
            GameObject obj = new GameObject("GameOverCanvas");
            canvas = obj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;

            CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            obj.AddComponent<GraphicRaycaster>();
        }

        private void BuildFullScreenImage()
        {
            GameObject imgObj = new GameObject("CutsceneImage");
            imgObj.transform.SetParent(canvas.transform, false);

            imageRect = imgObj.AddComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            float pad = shakeIntensity * 4f;
            imageRect.sizeDelta = new Vector2(pad, pad);

            Image img = imgObj.AddComponent<Image>();
            if (cutsceneImage != null)
            {
                img.sprite = cutsceneImage;
                img.preserveAspect = false;
            }
            else
            {
                img.color = new Color(0.1f, 0f, 0f, 1f);
            }
            img.raycastTarget = false;
        }

        private void ShowMenu(bool isLose)
        {
            // Dim overlay (over cutscene image for lose, over game world for win)
            GameObject dimObj = new GameObject("DimOverlay");
            dimObj.transform.SetParent(canvas.transform, false);

            RectTransform dimRect = dimObj.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.sizeDelta = Vector2.zero;

            Image dimImg = dimObj.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, isLose ? 0.5f : 0.7f);
            dimImg.raycastTarget = false;

            // Center panel
            GameObject panel = new GameObject("GameOverMenu");
            panel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(520, 350);

            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = panelColor;
            panelImg.raycastTarget = false;

            VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16;
            vlg.padding = new RectOffset(40, 40, 30, 30);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            Color goldColor = new Color(1f, 0.84f, 0f, 1f);

            if (isLose)
            {
                CreateText(panel.transform, "Title", "RENDERED!", 42,
                    new Color(0.9f, 0.15f, 0.15f, 1f), 55, FontStyle.Bold);
                CreateSpacer(panel.transform, 4);
                CreateText(panel.transform, "Subtitle", "You survived", 24,
                    Color.white, 30);
                CreateText(panel.transform, "Time", FormatTime(displayTime), 32,
                    goldColor, 40, FontStyle.Bold);
            }
            else
            {
                CreateText(panel.transform, "Title", "You did it!", 42,
                    new Color(0.2f, 0.9f, 0.3f, 1f), 55, FontStyle.Bold);
                CreateSpacer(panel.transform, 4);
                CreateText(panel.transform, "Subtitle",
                    "You collected the glasses in:", 24, Color.white, 30);
                CreateText(panel.transform, "Time", FormatTime(displayTime), 32,
                    goldColor, 40, FontStyle.Bold);
            }

            CreateSpacer(panel.transform, 12);
            CreateButton(panel.transform, "RETRY", buttonColor, OnRetry);
        }

        private void OnRetry()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RestartGame();
        }

        /* ---------- UI helpers (matches PauseMenu style) ---------- */

        private void CreateText(Transform parent, string name, string content,
            int fontSize, Color color, float height, FontStyle style = FontStyle.Normal)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(440, height);

            Text text = obj.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void CreateSpacer(Transform parent, float height)
        {
            GameObject obj = new GameObject("Spacer");
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, height);

            LayoutElement layout = obj.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        private void CreateButton(Transform parent, string label, Color btnColor,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject obj = new GameObject("Button_" + label);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250, 50);

            Image img = obj.AddComponent<Image>();
            img.color = btnColor;

            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            ColorBlock colors = btn.colors;
            colors.normalColor = btnColor;
            colors.highlightedColor = new Color(
                btnColor.r + 0.15f, btnColor.g + 0.15f, btnColor.b + 0.15f, 1f);
            colors.pressedColor = new Color(
                btnColor.r - 0.1f, btnColor.g - 0.1f, btnColor.b - 0.1f, 1f);
            btn.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);

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

        private static string FormatTime(float seconds)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return mins > 0 ? $"{mins}:{secs:00}" : $"{secs}s";
        }
    }
}
