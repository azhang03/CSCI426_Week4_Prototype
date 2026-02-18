using UnityEngine;
using UnityEngine.UI;

namespace Minifantasy
{
    /// <summary>
    /// Real-time debug value readout anchored to the top-right corner.
    /// Shows stamina drain/regen acceleration, Renderman spawn rate, and
    /// exposure gain speed so other programmers can watch the feedback
    /// loops in action. Toggled from the pause menu.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        public static DebugOverlay Instance { get; private set; }

        [Header("Display")]
        [SerializeField] private int fontSize = 18;

        private Canvas canvas;
        private Text valueText;
        private bool visible;
        private bool initialized;

        private PlayerMovement2D playerMovement;
        private ExposureMeter exposureMeter;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start() => EnsureInitialized();

        private void Update()
        {
            if (!visible) return;

            if (playerMovement == null)
                playerMovement = FindFirstObjectByType<PlayerMovement2D>();
            if (exposureMeter == null)
                exposureMeter = FindFirstObjectByType<ExposureMeter>();

            RefreshValues();
        }

        public void SetVisible(bool show)
        {
            visible = show;
            EnsureInitialized();
            if (canvas != null)
                canvas.gameObject.SetActive(show);
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            playerMovement = FindFirstObjectByType<PlayerMovement2D>();
            exposureMeter = FindFirstObjectByType<ExposureMeter>();
            BuildUI();
            canvas.gameObject.SetActive(visible);
        }

        private void RefreshValues()
        {
            if (valueText == null) return;

            float drain = playerMovement != null ? playerMovement.CurrentDrainRate : 0f;
            float regen = playerMovement != null ? playerMovement.CurrentRegenRate : 0f;
            float spawn = GameManager.Instance != null
                ? GameManager.Instance.GetCurrentLifetime() : 0f;
            float gain = exposureMeter != null ? exposureMeter.EffectiveGainRate : 0f;

            valueText.text =
                $"Stamina Decrease Acceleration: {drain:F1}/s\n" +
                $"Stamina Increase Acceleration: {regen:F1}/s\n" +
                $"Renderman Spawn Rate: every {spawn:F1}s\n" +
                $"Exposure Gain Speed: {gain:F2}/s";
        }

        private void BuildUI()
        {
            GameObject canvasObj = new GameObject("DebugOverlayCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1200;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            GameObject textObj = new GameObject("DebugText");
            textObj.transform.SetParent(canvasObj.transform, false);

            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-16f, -16f);

            valueText = textObj.AddComponent<Text>();
            valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valueText.fontSize = fontSize;
            valueText.color = new Color(0.4f, 1f, 0.4f, 1f);
            valueText.alignment = TextAnchor.UpperRight;
            valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            valueText.verticalOverflow = VerticalWrapMode.Overflow;
            valueText.raycastTarget = false;

            ContentSizeFitter fitter = textObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(1f, -1f);
        }
    }
}
