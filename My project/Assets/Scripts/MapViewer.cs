using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Minifantasy
{
    /// <summary>
    /// Shows a map icon with an "M" keyboard key in the bottom-right corner.
    /// Press M to slide a full-height map image in from the bottom.
    /// Press M again or Escape to slide it back down.
    /// Assign the map sprite later via the Inspector field.
    /// </summary>
    public class MapViewer : MonoBehaviour
    {
        [Header("Map Image")]
        [Tooltip("Assign your map sprite here. Leave null for now — a placeholder will show.")]
        [SerializeField] private Sprite mapSprite;

        [Header("Animation")]
        [SerializeField] private float slideSpeed = 5f;

        [Header("Icon")]
        [Tooltip("Offset from the bottom-right corner (X = inward, Y = upward).")]
        [SerializeField] private Vector2 iconOffset = new Vector2(30f, 30f);
        [Tooltip("Size of the map paper icon.")]
        [SerializeField] private float mapIconSize = 48f;
        [Tooltip("Size of the keyboard key graphic.")]
        [SerializeField] private float keySize = 36f;

        private Canvas canvas;
        private RectTransform mapRect;
        private Image mapImage;
        private CanvasGroup mapCanvasGroup;
        private bool mapOpen;
        private float mapTargetY;
        private float mapHiddenY;
        private float mapShownY;

        private void Start()
        {
            BuildUI();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            bool blocked = (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                        || Time.timeScale == 0f;

            if (Keyboard.current.mKey.wasPressedThisFrame && !blocked)
                ToggleMap();

            if (mapOpen && Keyboard.current.escapeKey.wasPressedThisFrame && !blocked)
                ToggleMap();

            AnimateMap();
        }

        private void ToggleMap()
        {
            mapOpen = !mapOpen;
            mapTargetY = mapOpen ? mapShownY : mapHiddenY;

            if (mapOpen && AudioManager.Instance != null)
                AudioManager.Instance.PlayMapRuffle();
        }

        private void AnimateMap()
        {
            if (mapRect == null) return;

            Vector2 pos = mapRect.anchoredPosition;
            pos.y = Mathf.Lerp(pos.y, mapTargetY, Time.unscaledDeltaTime * slideSpeed);
            mapRect.anchoredPosition = pos;
        }

        /* ---------- UI construction ---------- */

        private void BuildUI()
        {
            // Canvas at very high sorting order (above pause menu at 999)
            GameObject canvasObj = new GameObject("MapViewerCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            BuildMapPanel(canvasObj.transform);
            BuildIcon(canvasObj.transform);
        }

        private void BuildMapPanel(Transform parent)
        {
            GameObject mapObj = new GameObject("MapPanel");
            mapObj.transform.SetParent(parent, false);

            mapRect = mapObj.AddComponent<RectTransform>();
            mapRect.anchorMin = new Vector2(0.5f, 0.5f);
            mapRect.anchorMax = new Vector2(0.5f, 0.5f);
            mapRect.pivot = new Vector2(0.5f, 0.5f);

            // Size: height = full screen (1080 in reference), width from aspect ratio
            float mapH = 1080f;
            float mapW = mapH * 0.7f; // default aspect, overridden if sprite assigned
            if (mapSprite != null && mapSprite.texture != null)
            {
                float aspect = (float)mapSprite.texture.width / mapSprite.texture.height;
                mapW = mapH * aspect;
            }

            mapRect.sizeDelta = new Vector2(mapW, mapH);

            // Semi-transparent dark background behind the map
            Image bgImg = mapObj.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);

            // Actual map image (child, fills parent)
            if (mapSprite != null)
            {
                GameObject imgObj = new GameObject("MapImage");
                imgObj.transform.SetParent(mapObj.transform, false);

                RectTransform imgRect = imgObj.AddComponent<RectTransform>();
                imgRect.anchorMin = Vector2.zero;
                imgRect.anchorMax = Vector2.one;
                imgRect.sizeDelta = Vector2.zero;

                mapImage = imgObj.AddComponent<Image>();
                mapImage.sprite = mapSprite;
                mapImage.preserveAspect = true;
            }
            else
            {
                // Placeholder text if no map assigned yet
                GameObject placeholderObj = new GameObject("Placeholder");
                placeholderObj.transform.SetParent(mapObj.transform, false);

                RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
                phRect.anchorMin = Vector2.zero;
                phRect.anchorMax = Vector2.one;
                phRect.sizeDelta = Vector2.zero;

                Text ph = placeholderObj.AddComponent<Text>();
                ph.text = "MAP\n(assign sprite)";
                ph.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                ph.fontSize = 40;
                ph.color = new Color(1f, 1f, 1f, 0.3f);
                ph.alignment = TextAnchor.MiddleCenter;
            }

            // Start hidden below the screen
            mapHiddenY = -(mapH + 100f);
            mapShownY = 0f;
            mapTargetY = mapHiddenY;
            mapRect.anchoredPosition = new Vector2(0f, mapHiddenY);
        }

        private void BuildIcon(Transform parent)
        {
            // Container anchored to bottom-right
            GameObject iconRoot = new GameObject("MapIcon");
            iconRoot.transform.SetParent(parent, false);

            RectTransform rootRT = iconRoot.AddComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(1f, 0f);
            rootRT.anchorMax = new Vector2(1f, 0f);
            rootRT.pivot = new Vector2(1f, 0f);
            rootRT.anchoredPosition = new Vector2(-iconOffset.x, iconOffset.y);
            rootRT.sizeDelta = new Vector2(mapIconSize, mapIconSize + keySize + 8f);

            VerticalLayoutGroup vlg = iconRoot.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            // --- Map icon (folded paper) ---
            BuildMapIcon(iconRoot.transform);

            // --- Keyboard key with "M" ---
            BuildKeyIcon(iconRoot.transform);
        }

        private void BuildMapIcon(Transform parent)
        {
            GameObject mapIconObj = new GameObject("FoldedMap");
            mapIconObj.transform.SetParent(parent, false);

            RectTransform rt = mapIconObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(mapIconSize, mapIconSize);

            // Parchment-colored background
            Image bg = mapIconObj.AddComponent<Image>();
            bg.color = new Color(0.85f, 0.78f, 0.6f, 1f);

            // Horizontal "fold" lines
            for (int i = 0; i < 3; i++)
            {
                GameObject line = new GameObject($"Line{i}");
                line.transform.SetParent(mapIconObj.transform, false);

                RectTransform lineRT = line.AddComponent<RectTransform>();
                lineRT.anchorMin = new Vector2(0.15f, 0f);
                lineRT.anchorMax = new Vector2(0.85f, 0f);
                float yNorm = 0.3f + i * 0.2f;
                lineRT.anchoredPosition = new Vector2(0f, yNorm * mapIconSize);
                lineRT.sizeDelta = new Vector2(0f, 2f);

                Image lineImg = line.AddComponent<Image>();
                lineImg.color = new Color(0.5f, 0.4f, 0.25f, 0.6f);
            }

            // Small "X marks the spot" dot
            GameObject dot = new GameObject("XMark");
            dot.transform.SetParent(mapIconObj.transform, false);

            RectTransform dotRT = dot.AddComponent<RectTransform>();
            dotRT.anchorMin = new Vector2(0.5f, 0.5f);
            dotRT.anchorMax = new Vector2(0.5f, 0.5f);
            dotRT.sizeDelta = new Vector2(8, 8);

            Image dotImg = dot.AddComponent<Image>();
            dotImg.color = new Color(0.8f, 0.15f, 0.15f, 0.9f);
        }

        private void BuildKeyIcon(Transform parent)
        {
            GameObject keyObj = new GameObject("KeyM");
            keyObj.transform.SetParent(parent, false);

            RectTransform keyRT = keyObj.AddComponent<RectTransform>();
            keyRT.sizeDelta = new Vector2(keySize, keySize);

            // Dark rounded keyboard key background
            Image keyBg = keyObj.AddComponent<Image>();
            keyBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            // Light border (slightly larger rect behind)
            GameObject border = new GameObject("Border");
            border.transform.SetParent(keyObj.transform, false);
            border.transform.SetAsFirstSibling();

            RectTransform borderRT = border.AddComponent<RectTransform>();
            borderRT.anchorMin = Vector2.zero;
            borderRT.anchorMax = Vector2.one;
            borderRT.sizeDelta = new Vector2(4, 4);

            Image borderImg = border.AddComponent<Image>();
            borderImg.color = new Color(0.45f, 0.45f, 0.45f, 0.9f);

            // Inner dark face
            GameObject face = new GameObject("Face");
            face.transform.SetParent(keyObj.transform, false);

            RectTransform faceRT = face.AddComponent<RectTransform>();
            faceRT.anchorMin = Vector2.zero;
            faceRT.anchorMax = Vector2.one;
            faceRT.offsetMin = new Vector2(3, 4);
            faceRT.offsetMax = new Vector2(-3, -2);

            Image faceImg = face.AddComponent<Image>();
            faceImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // "M" label
            GameObject mLabel = new GameObject("M");
            mLabel.transform.SetParent(face.transform, false);

            RectTransform mRT = mLabel.AddComponent<RectTransform>();
            mRT.anchorMin = Vector2.zero;
            mRT.anchorMax = Vector2.one;
            mRT.sizeDelta = Vector2.zero;

            Text mText = mLabel.AddComponent<Text>();
            mText.text = "M";
            mText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mText.fontSize = 18;
            mText.fontStyle = FontStyle.Bold;
            mText.color = Color.white;
            mText.alignment = TextAnchor.MiddleCenter;
        }
    }
}
