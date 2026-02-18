using UnityEngine;
using System.Collections.Generic;

namespace Minifantasy
{
    /// <summary>
    /// A Renderman instance. Fades in, lingers for its lifetime, then fades out and self-destructs.
    /// Other scripts query ActiveInstances and IsVisibleToPlayer() to drive the exposure meter.
    /// Assign renderman.png on the prefab's SpriteRenderer or via the rendermanSprite field.
    /// </summary>
    public class RendermanController : MonoBehaviour
    {
        [Header("Visuals")]
        [Tooltip("Optional override sprite. If null, uses the existing SpriteRenderer sprite.")]
        [SerializeField] private Sprite rendermanSprite;
        [SerializeField] private float fadeInDuration = 0.6f;
        [SerializeField] private float fadeOutDuration = 1.0f;
        [SerializeField] private int sortingOrder = 5;

        [Header("Lifetime")]
        [Tooltip("Seconds Renderman stays fully visible before fading out.")]
        [SerializeField] private float lifetime = 10f;

        private SpriteRenderer sr;
        private Collider2D shapeCollider;
        private float age;
        private bool fading;
        private bool seenByPlayer;

        /* -------- static registry -------- */

        private static readonly List<RendermanController> instances = new List<RendermanController>();
        public static IReadOnlyList<RendermanController> ActiveInstances => instances;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instances.Clear();

        /* -------- public API -------- */

        /// <summary>Called by GameManager to set lifetime equal to the current spawn interval.</summary>
        public void SetLifetime(float duration) { lifetime = duration; }

        /// <summary>True once the player has seen this particular spawn (used by AudioManager).</summary>
        public bool SeenByPlayer => seenByPlayer;
        public void MarkSeenByPlayer() { seenByPlayer = true; }

        /// <summary>
        /// True when any part of the Renderman sprite overlaps the player's vision circle.
        /// Uses a PolygonCollider2D fitted to the sprite outline for pixel-accurate checks.
        /// Falls back to the bounding box if no collider is available.
        /// </summary>
        public bool IsVisibleToPlayer(Vector2 playerPos, float visionRadius)
        {
            if (sr == null || !sr.enabled || sr.color.a < 0.05f)
                return false;

            Vector2 closest;
            if (shapeCollider != null)
                closest = shapeCollider.ClosestPoint(playerPos);
            else
                closest = sr.bounds.ClosestPoint(playerPos);

            return Vector2.Distance(closest, playerPos) <= visionRadius;
        }

        /// <summary>
        /// True when any part of the sprite overlaps the camera's viewport rectangle.
        /// Prevents exposure from ticking when Renderman is within the vignette circle
        /// but off the edge of the screen.
        /// </summary>
        public bool IsOnScreen()
        {
            Camera cam = Camera.main;
            if (cam == null || sr == null) return false;

            Vector3 camPos = cam.transform.position;
            float camH = cam.orthographicSize;
            float camW = camH * cam.aspect;

            Bounds b = sr.bounds;
            return b.max.x >= camPos.x - camW && b.min.x <= camPos.x + camW
                && b.max.y >= camPos.y - camH && b.min.y <= camPos.y + camH;
        }

        /* -------- lifecycle -------- */

        private void OnEnable()  => instances.Add(this);
        private void OnDisable() => instances.Remove(this);

        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = gameObject.AddComponent<SpriteRenderer>();

            if (rendermanSprite != null)
                sr.sprite = rendermanSprite;

            sr.sortingOrder = sortingOrder;
            sr.color = new Color(1f, 1f, 1f, 0f);

            BuildShapeCollider();
        }

        /// <summary>
        /// Auto-generates a PolygonCollider2D from the sprite's physics shape
        /// so the visibility check traces the actual pixel outline, not the bounding box.
        /// </summary>
        private void BuildShapeCollider()
        {
            if (sr == null || sr.sprite == null) return;

            int shapeCount = sr.sprite.GetPhysicsShapeCount();
            if (shapeCount <= 0) return;

            PolygonCollider2D poly = gameObject.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            poly.pathCount = shapeCount;

            List<Vector2> path = new List<Vector2>();
            for (int i = 0; i < shapeCount; i++)
            {
                path.Clear();
                sr.sprite.GetPhysicsShape(i, path);
                poly.SetPath(i, path);
            }

            shapeCollider = poly;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                return;

            age += Time.deltaTime;

            if (!fading)
            {
                if (age < fadeInDuration)
                {
                    float alpha = Mathf.Clamp01(age / fadeInDuration);
                    sr.color = new Color(1f, 1f, 1f, alpha);
                }
                else if (age >= lifetime)
                {
                    fading = true;
                }
                else
                {
                    sr.color = Color.white;
                }
            }

            if (fading)
            {
                Color c = sr.color;
                c.a -= Time.deltaTime / fadeOutDuration;
                sr.color = c;
                if (c.a <= 0f)
                    Destroy(gameObject);
            }
        }
    }
}
