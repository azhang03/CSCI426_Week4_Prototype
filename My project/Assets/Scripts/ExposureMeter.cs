using UnityEngine;
using UnityEngine.UI;

namespace Minifantasy
{
    /// <summary>
    /// Tracks the player's exposure to Renderman.
    /// If any pixel of a Renderman sprite is within the player's vision radius,
    /// exposure climbs. When no Renderman is visible, exposure decays (slowly).
    /// Hitting max exposure triggers a game-over.
    /// Creates its own UI bar at runtime — just add this component to any GameObject.
    /// </summary>
    public class ExposureMeter : MonoBehaviour
    {
        [Header("Exposure Rates")]
        [Tooltip("Base exposure gained per second while Renderman is visible.")]
        [SerializeField] private float gainRate = 0.15f;
        [Tooltip("Extra gain per second added for each pair of glasses collected.\n" +
                 "e.g. 0.03 with 3 glasses → effective gain = 0.15 + 0.09 = 0.24/s")]
        [SerializeField] private float gainIncreasePerGlasses = 0.03f;
        [Tooltip("Exposure lost per second when no Renderman is visible (should be < gainRate).")]
        [SerializeField] private float decayRate = 0.05f;

        [Header("Visibility")]
        [Tooltip("Fraction of the vision radius to trim for the exposure check (0–1).\n" +
                 "Matches the vignette's soft fade edge which is ~30% of the radius.\n" +
                 "0.3 = exposure only ticks when Renderman is past the fade zone.")]
        [Range(0f, 1f)]
        [SerializeField] private float visibilityBuffer = 0.3f;

        [Header("Stacking")]
        [Tooltip("Each additional visible Renderman multiplies the gain by this factor.\n" +
                 "e.g. 1.5 → two visible = 1.5× gain, three visible = 2× gain.")]
        [SerializeField] private float stackMultiplier = 1.5f;

        [Header("UI Bar")]
        [SerializeField] private Vector2 barSize = new Vector2(250f, 16f);
        [Tooltip("Pixels above the bottom edge of the screen.")]
        [SerializeField] private float bottomMargin = 80f;
        [SerializeField] private Color safeColor  = new Color(0.2f, 0.75f, 0.3f, 1f);
        [SerializeField] private Color dangerColor = new Color(0.95f, 0.1f, 0.1f, 1f);

        private float exposure;
        private Transform playerTransform;
        private VignetteController vignetteController;

        private CanvasGroup canvasGroup;
        private RectTransform fillRect;
        private Image fillImage;
        private float barInnerWidth;
        private bool barVisible = false;

        public float Exposure => exposure;

        /// <summary>Debug helper: instantly maxes out exposure (triggers lose on next frame).</summary>
        public void ForceMaxExposure() { exposure = 1f; }

        /// <summary>Show or hide the exposure UI bar (used by PauseMenu toggle).</summary>
        public void SetBarVisible(bool visible)
        {
            barVisible = visible;
            if (!visible && canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        /* ---------- lifecycle ---------- */

        private void Start()
        {
            vignetteController = FindFirstObjectByType<VignetteController>();
            FindPlayer();
            BuildUI();
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                return;

            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            float rawRadius = vignetteController != null
                ? vignetteController.GetCurrentVisibleRadius()
                : 4f;

            // Shrink the detection radius proportionally to match the vignette's
            // soft fade edge (~30% of radius), so exposure only starts when
            // Renderman is genuinely visible through the darkness
            float visionRadius = Mathf.Max(rawRadius * (1f - visibilityBuffer), 0.1f);

            Vector2 playerPos = playerTransform.position;

            // Count Renderman instances that are BOTH inside the vision circle
            // AND within the camera viewport (vision circle can extend off-screen)
            int visibleCount = 0;
            var list = RendermanController.ActiveInstances;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null
                    && list[i].IsVisibleToPlayer(playerPos, visionRadius)
                    && list[i].IsOnScreen())
                    visibleCount++;
            }

            if (visibleCount > 0)
            {
                int glasses = GameManager.Instance != null ? GameManager.Instance.GlassesCollected : 0;
                float effectiveGain = gainRate + gainIncreasePerGlasses * glasses;
                float mult = 1f + (visibleCount - 1) * (stackMultiplier - 1f);
                exposure += effectiveGain * mult * Time.deltaTime;
            }
            else
            {
                exposure -= decayRate * Time.deltaTime;
            }

            exposure = Mathf.Clamp01(exposure);

            if (exposure >= 1f && GameManager.Instance != null)
                GameManager.Instance.Lose();

            RefreshUI();
        }

        /* ---------- helpers ---------- */

        private void FindPlayer()
        {
            if (playerTransform != null) return;

            try
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) { playerTransform = p.transform; return; }
            }
            catch { }

            PlayerMovement2D pm = FindFirstObjectByType<PlayerMovement2D>();
            if (pm != null) playerTransform = pm.transform;
        }

        /* ---------- UI ---------- */

        private void BuildUI()
        {
            // Canvas
            GameObject canvasObj = new GameObject("ExposureMeterCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Background bar
            GameObject bgObj = new GameObject("Bar_BG");
            bgObj.transform.SetParent(canvasObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0f);
            bgRect.anchorMax = new Vector2(0.5f, 0f);
            bgRect.pivot = new Vector2(0.5f, 0f);
            bgRect.anchoredPosition = new Vector2(0f, bottomMargin);
            bgRect.sizeDelta = barSize;

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.65f);

            // Fill bar (child of background, grows left-to-right)
            GameObject fillObj = new GameObject("Bar_Fill");
            fillObj.transform.SetParent(bgObj.transform, false);

            fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(2f, 0f);
            fillRect.sizeDelta = new Vector2(0f, -4f);

            fillImage = fillObj.AddComponent<Image>();
            fillImage.color = safeColor;

            barInnerWidth = barSize.x - 4f;

            // "EXPOSURE" label above the bar
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(bgObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 4f);
            labelRect.sizeDelta = new Vector2(0f, 20f);

            Text label = labelObj.AddComponent<Text>();
            label.text = "EXPOSURE";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 14;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
        }

        private void RefreshUI()
        {
            if (fillRect == null || !barVisible) return;

            // Width tracks exposure 0→1
            fillRect.sizeDelta = new Vector2(barInnerWidth * exposure, -4f);

            // Color gradient from green to red
            fillImage.color = Color.Lerp(safeColor, dangerColor, exposure);

            // Fade the whole bar in/out
            float targetAlpha = exposure > 0.01f ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(
                canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 3f);
        }
    }
}
