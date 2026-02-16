using UnityEngine;

namespace Minifantasy
{
    /// <summary>
    /// Renderman spawns at a position and stays still.
    /// If the player is inside its AoE for too long, the player loses.
    /// A visual circle shows the danger zone, and a timer counts down.
    /// </summary>
    public class RendermanController : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private Color dangerColor = new Color(0.6f, 0f, 0f, 0.3f);
        [SerializeField] private Color warningColor = new Color(1f, 0f, 0f, 0.5f);
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float lingerAfterSafe = 1.5f;

        private float radius;
        private float escapeTime;
        private float playerInsideTimer;
        private bool playerInside;
        private float aliveTimer;
        private float maxAliveTime;
        private bool triggered;
        private SpriteRenderer aoeRenderer;
        private SpriteRenderer coreRenderer;
        private Transform playerTransform;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                radius = GameManager.Instance.RendermanRadius;
                escapeTime = GameManager.Instance.EscapeTime;
            }
            else
            {
                radius = 3f;
                escapeTime = 3f;
            }

            maxAliveTime = escapeTime + lingerAfterSafe + 2f;
            playerInsideTimer = 0f;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;

            CreateVisuals();
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                return;

            aliveTimer += Time.deltaTime;

            if (playerTransform == null) return;

            float dist = Vector2.Distance(transform.position, playerTransform.position);
            playerInside = dist <= radius;

            if (playerInside)
            {
                playerInsideTimer += Time.deltaTime;

                // Flash warning as time runs out
                float urgency = playerInsideTimer / escapeTime;
                Color c = Color.Lerp(dangerColor, warningColor, urgency);
                if (aoeRenderer != null)
                    aoeRenderer.color = c;

                if (playerInsideTimer >= escapeTime)
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.Lose();
                }
            }
            else
            {
                // Player escaped - start fade and destroy
                if (playerInsideTimer > 0f || aliveTimer > maxAliveTime)
                {
                    FadeAndDestroy();
                }
            }
        }

        private void CreateVisuals()
        {
            // AoE circle
            GameObject aoeObj = new GameObject("AoE_Circle");
            aoeObj.transform.SetParent(transform);
            aoeObj.transform.localPosition = Vector3.zero;

            aoeRenderer = aoeObj.AddComponent<SpriteRenderer>();
            aoeRenderer.sprite = CreateCircleSprite(64);
            aoeRenderer.color = dangerColor;
            aoeRenderer.sortingOrder = 1;
            aoeObj.transform.localScale = Vector3.one * radius * 2f;

            // Core "Renderman" sprite (simple dark square for now)
            GameObject coreObj = new GameObject("Renderman_Core");
            coreObj.transform.SetParent(transform);
            coreObj.transform.localPosition = Vector3.zero;

            coreRenderer = coreObj.AddComponent<SpriteRenderer>();
            coreRenderer.sprite = CreateSquareSprite();
            coreRenderer.color = new Color(0.15f, 0f, 0.15f, 0.9f);
            coreRenderer.sortingOrder = 2;
            coreObj.transform.localScale = Vector3.one * 0.8f;
        }

        private void FadeAndDestroy()
        {
            if (aoeRenderer != null)
            {
                Color c = aoeRenderer.color;
                c.a -= Time.deltaTime * 2f;
                aoeRenderer.color = c;
            }

            if (coreRenderer != null)
            {
                Color c = coreRenderer.color;
                c.a -= Time.deltaTime * 2f;
                coreRenderer.color = c;
            }

            if (aoeRenderer != null && aoeRenderer.color.a <= 0f)
                Destroy(gameObject);
        }

        private static Sprite CreateCircleSprite(int resolution)
        {
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            float center = resolution / 2f;
            float radiusPx = center - 1f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= radiusPx)
                        tex.SetPixel(x, y, Color.white);
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f), resolution);
        }

        private static Sprite CreateSquareSprite()
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }
    }
}
