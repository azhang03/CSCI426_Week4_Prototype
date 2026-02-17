using UnityEngine;

namespace Minifantasy
{
    /// <summary>
    /// World-space stamina bar that hovers above the player's head.
    /// Self-contained: creates its own SpriteRenderers at runtime.
    /// Renders above the vignette so it's always visible.
    /// Just attach this to any GameObject — no UI setup needed.
    /// </summary>
    public class StaminaBar : MonoBehaviour
    {
        [Header("Bar Sizing (world units)")]
        [SerializeField] private float barWidth = 1f;
        [SerializeField] private float barHeight = 0.1f;
        [SerializeField] private float heightAbovePlayer = 0.8f;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color lowColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private float lowThreshold = 0.25f;

        [Header("Rendering")]
        [Tooltip("Must be above the vignette (1000) and static overlay (1001).")]
        [SerializeField] private int sortingOrder = 1003;

        private PlayerMovement2D player;
        private Transform playerTransform;
        private GameObject barRoot;
        private SpriteRenderer bgSR;
        private SpriteRenderer fillSR;
        private float currentAlpha = 1f;

        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.GetComponent<PlayerMovement2D>();
                playerTransform = playerObj.transform;
            }

            if (playerTransform != null)
                BuildBar();
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.GetComponent<PlayerMovement2D>();
                    playerTransform = playerObj.transform;
                    BuildBar();
                }
                return;
            }

            if (player == null || fillSR == null) return;

            float ratio = player.CurrentStamina / player.MaxStamina;

            // Scale fill width, anchored to the left edge
            float fillWidth = barWidth * ratio;
            fillSR.transform.localScale = new Vector3(fillWidth, barHeight, 1f);
            fillSR.transform.localPosition = new Vector3(
                -barWidth / 2f + fillWidth / 2f, 0f, 0f);

            // Color: lerp toward red when low
            Color fillColor = ratio <= lowThreshold
                ? Color.Lerp(lowColor, normalColor, ratio / lowThreshold)
                : normalColor;

            // Fade out when full and not sprinting
            float targetAlpha = (ratio >= 1f && !player.IsSprinting) ? 0f : 1f;
            currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, 5f * Time.deltaTime);

            fillColor.a = currentAlpha;
            fillSR.color = fillColor;

            Color bg = bgColor;
            bg.a = bgColor.a * currentAlpha;
            bgSR.color = bg;
        }

        private void BuildBar()
        {
            if (barRoot != null) return;

            Sprite whiteSprite = CreateWhiteSprite();

            // Root (child of player, offset above head)
            barRoot = new GameObject("StaminaBar");
            barRoot.transform.SetParent(playerTransform, false);
            barRoot.transform.localPosition = new Vector3(0f, heightAbovePlayer, 0f);

            // Compensate for player scale so bar stays a fixed world size
            float parentScale = playerTransform.lossyScale.x;
            if (parentScale > 0.001f)
                barRoot.transform.localScale = Vector3.one / parentScale;

            // Background
            GameObject bgObj = new GameObject("BG");
            bgObj.transform.SetParent(barRoot.transform, false);
            bgObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);
            bgSR = bgObj.AddComponent<SpriteRenderer>();
            bgSR.sprite = whiteSprite;
            bgSR.color = bgColor;
            bgSR.sortingOrder = sortingOrder;

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barRoot.transform, false);
            fillSR = fillObj.AddComponent<SpriteRenderer>();
            fillSR.sprite = whiteSprite;
            fillSR.color = normalColor;
            fillSR.sortingOrder = sortingOrder + 1;
        }

        private static Sprite CreateWhiteSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }
    }
}
