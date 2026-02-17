using UnityEngine;
using UnityEngine.UI;

namespace Minifantasy
{
    /// <summary>
    /// Displays a "X/5 Glasses Collected" message at the top of the screen
    /// that fades in, holds, then fades out. Self-building UI.
    /// Call Show(collected, total) from GameManager.
    /// </summary>
    public class GlassesNotification : MonoBehaviour
    {
        public static GlassesNotification Instance { get; private set; }

        [Header("Timing")]
        [SerializeField] private float fadeInDuration = 0.4f;
        [SerializeField] private float holdDuration = 1.5f;
        [SerializeField] private float fadeOutDuration = 0.6f;

        [Header("Positioning")]
        [Tooltip("Pixels below the top edge of the screen.")]
        [SerializeField] private float topMargin = 60f;
        [SerializeField] private int fontSize = 32;

        private CanvasGroup canvasGroup;
        private Text messageText;
        private float timer;
        private float totalDuration;
        private bool showing;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            BuildUI();
            totalDuration = fadeInDuration + holdDuration + fadeOutDuration;
        }

        private void Update()
        {
            if (!showing) return;

            timer += Time.unscaledDeltaTime;

            if (timer < fadeInDuration)
            {
                canvasGroup.alpha = timer / fadeInDuration;
            }
            else if (timer < fadeInDuration + holdDuration)
            {
                canvasGroup.alpha = 1f;
            }
            else if (timer < totalDuration)
            {
                float fadeProgress = (timer - fadeInDuration - holdDuration) / fadeOutDuration;
                canvasGroup.alpha = 1f - fadeProgress;
            }
            else
            {
                canvasGroup.alpha = 0f;
                showing = false;
            }
        }

        public void Show(int collected, int total)
        {
            if (messageText != null)
                messageText.text = $"{collected}/{total} Glasses Collected";

            timer = 0f;
            showing = true;
            canvasGroup.alpha = 0f;
        }

        private void BuildUI()
        {
            GameObject canvasObj = new GameObject("GlassesNotificationCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1050;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            GameObject textObj = new GameObject("NotificationText");
            textObj.transform.SetParent(canvasObj.transform, false);

            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -topMargin);
            rt.sizeDelta = new Vector2(600f, 60f);

            messageText = textObj.AddComponent<Text>();
            messageText.text = "";
            messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            messageText.fontSize = fontSize;
            messageText.fontStyle = FontStyle.Bold;
            messageText.color = Color.white;
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Drop shadow for readability
            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }
    }
}
