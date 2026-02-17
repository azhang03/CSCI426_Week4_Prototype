using UnityEngine;

namespace Minifantasy
{
    /// <summary>
    /// Vignette that follows the player perfectly by being a SpriteRenderer child of the player.
    /// Renders above all world objects but below UI Canvas.
    /// Self-contained: finds the player and creates its own sprite at runtime.
    /// Only regenerates the texture when glasses are collected (no per-frame cost).
    ///
    /// Static overlay: a second sprite using tiled static.jpg instead of black.
    /// Its alpha is driven by ExposureMeter.Exposure so the vignette visually
    /// transitions from black to TV static as exposure climbs.
    /// </summary>
    public class VignetteController : MonoBehaviour
    {
        [Header("Vignette Settings")]
        [Tooltip("Starting visible radius in world units at 0 glasses (e.g. 1.0 = 1 tile).")]
        [SerializeField] private float startRadius = 1.5f;
        [Tooltip("Multiplier per glasses pickup (e.g. 1.3 = 30% bigger each time). Feels consistent at any starting radius.")]
        [SerializeField] private float growthPerGlasses = 1.4f;

        [Header("Rendering")]
        [Tooltip("Sorting order (high = renders on top of world).")]
        [SerializeField] private int sortingOrder = 1000;

        [Header("Static Overlay")]
        [Tooltip("Assign static.jpg here. Must have Read/Write enabled in import settings.")]
        [SerializeField] private Texture2D staticTexture;

        private float currentRadius;
        private Texture2D vignetteTexture;
        private SpriteRenderer spriteRenderer;
        private Transform playerTransform;
        private GameObject vignetteObj;
        private int texSize = 1024;
        private bool isEnabled = true;
        private float lastCoverWorldSize;
        private bool needsInitialRebuild;

        // Static overlay
        private Texture2D staticOverlayTexture;
        private SpriteRenderer staticSpriteRenderer;
        private GameObject staticObj;
        private bool staticEnabled = true;
        private ExposureMeter exposureMeter;

        private void Start()
        {
            currentRadius = startRadius;
            exposureMeter = FindFirstObjectByType<ExposureMeter>();
            FindPlayer();
            if (playerTransform != null)
                CreateVignette();
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform != null)
                    CreateVignette();
                return;
            }

            UpdateScale();

            if (needsInitialRebuild)
            {
                needsInitialRebuild = false;
                RebuildTexture();
            }
            else
            {
                float coverWorldSize = GetCoverWorldSize();
                if (Mathf.Abs(coverWorldSize - lastCoverWorldSize) > 0.1f)
                    RebuildTexture();
            }

            UpdateStaticAlpha();
        }

        private float GetCoverWorldSize()
        {
            Camera cam = Camera.main;
            if (cam == null) return 106f;
            float camHeight = cam.orthographicSize * 2f;
            float camWidth = camHeight * cam.aspect;
            return Mathf.Max(camHeight, camWidth) * 6f;
        }

        public void OnGlassesCollected(int collected, int total)
        {
            currentRadius = startRadius * Mathf.Pow(growthPerGlasses, collected);
            RebuildTexture();
        }

        public void SetVignetteEnabled(bool enabled)
        {
            isEnabled = enabled;
            if (vignetteObj != null)
                vignetteObj.SetActive(enabled);
            if (staticObj != null)
                staticObj.SetActive(enabled && staticEnabled);
        }

        public void SetStaticEnabled(bool enabled)
        {
            staticEnabled = enabled;
            if (staticObj != null)
                staticObj.SetActive(isEnabled && staticEnabled);
        }

        public float GetCurrentVisibleRadius()
        {
            return currentRadius;
        }

        private void FindPlayer()
        {
            if (playerTransform != null) return;

            try
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) { playerTransform = player.transform; return; }
            }
            catch { }

            PlayerMovement2D movement = FindFirstObjectByType<PlayerMovement2D>();
            if (movement != null) { playerTransform = movement.transform; return; }

            GameObject byName = GameObject.Find("Player");
            if (byName != null) playerTransform = byName.transform;
        }

        private void CreateVignette()
        {
            if (vignetteObj != null) return;

            vignetteObj = new GameObject("Vignette");
            vignetteObj.transform.SetParent(playerTransform, false);
            vignetteObj.transform.localPosition = Vector3.zero;

            spriteRenderer = vignetteObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.enabled = isEnabled;

            // Static overlay (same parent, one sorting order higher)
            if (staticTexture != null)
            {
                staticObj = new GameObject("StaticOverlay");
                staticObj.transform.SetParent(playerTransform, false);
                staticObj.transform.localPosition = Vector3.zero;

                staticSpriteRenderer = staticObj.AddComponent<SpriteRenderer>();
                staticSpriteRenderer.sortingOrder = sortingOrder + 1;
                staticSpriteRenderer.color = new Color(1f, 1f, 1f, 0f);
                staticObj.SetActive(isEnabled && staticEnabled);
            }

            needsInitialRebuild = true;
        }

        private void UpdateScale()
        {
            Camera cam = Camera.main;
            if (cam == null || vignetteObj == null) return;

            float camHeight = cam.orthographicSize * 2f;
            float camWidth = camHeight * cam.aspect;
            float coverSize = Mathf.Max(camHeight, camWidth) * 6f;
            vignetteObj.transform.localScale = Vector3.one * coverSize;

            if (staticObj != null)
                staticObj.transform.localScale = Vector3.one * coverSize;
        }

        private void UpdateStaticAlpha()
        {
            if (staticSpriteRenderer == null) return;

            if (exposureMeter == null)
                exposureMeter = FindFirstObjectByType<ExposureMeter>();

            float exposure = exposureMeter != null ? exposureMeter.Exposure : 0f;
            staticSpriteRenderer.color = new Color(1f, 1f, 1f, exposure);
        }

        private void RebuildTexture()
        {
            if (vignetteTexture == null)
            {
                vignetteTexture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
                vignetteTexture.filterMode = FilterMode.Bilinear;
                vignetteTexture.wrapMode = TextureWrapMode.Clamp;
            }

            float coverWorldSize = GetCoverWorldSize();
            lastCoverWorldSize = coverWorldSize;

            float center = texSize / 2f;
            float innerRadPx = (currentRadius / coverWorldSize) * texSize;
            float edgeSoftness = Mathf.Max(texSize * 0.01f, innerRadPx * 0.3f);

            // Read static texture pixels once for tiling (if available)
            Color[] staticPixels = null;
            int staticW = 0, staticH = 0;
            bool hasStatic = staticTexture != null;
            if (hasStatic)
            {
                try
                {
                    staticPixels = staticTexture.GetPixels();
                    staticW = staticTexture.width;
                    staticH = staticTexture.height;
                }
                catch
                {
                    Debug.LogWarning("VignetteController: static.jpg must have Read/Write enabled in import settings.");
                    hasStatic = false;
                }
            }

            // Allocate static overlay texture if needed
            if (hasStatic && staticOverlayTexture == null)
            {
                staticOverlayTexture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
                staticOverlayTexture.filterMode = FilterMode.Bilinear;
                staticOverlayTexture.wrapMode = TextureWrapMode.Clamp;
            }

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha;

                    if (dist < innerRadPx)
                        alpha = 0f;
                    else if (dist < innerRadPx + edgeSoftness)
                        alpha = (dist - innerRadPx) / edgeSoftness;
                    else
                        alpha = 1f;

                    vignetteTexture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));

                    // Build static overlay with same hole shape but tiled static pixels
                    if (hasStatic)
                    {
                        int sx = x % staticW;
                        int sy = y % staticH;
                        Color sc = staticPixels[sy * staticW + sx];
                        staticOverlayTexture.SetPixel(x, y, new Color(sc.r, sc.g, sc.b, alpha));
                    }
                }
            }

            vignetteTexture.Apply();

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = Sprite.Create(
                    vignetteTexture,
                    new Rect(0, 0, texSize, texSize),
                    new Vector2(0.5f, 0.5f),
                    texSize
                );
            }

            if (hasStatic)
            {
                staticOverlayTexture.Apply();

                if (staticSpriteRenderer != null)
                {
                    staticSpriteRenderer.sprite = Sprite.Create(
                        staticOverlayTexture,
                        new Rect(0, 0, texSize, texSize),
                        new Vector2(0.5f, 0.5f),
                        texSize
                    );
                }
            }
        }
    }
}
