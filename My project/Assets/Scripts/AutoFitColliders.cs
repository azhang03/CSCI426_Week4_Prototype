using UnityEngine;

namespace Minifantasy
{
    /// <summary>
    /// Attach to any GameObject in the scene.
    /// On Start, finds all BoxCollider2D on pillars and replaces them with
    /// PolygonCollider2D that conforms to the sprite's pixel shape.
    /// </summary>
    public class AutoFitColliders : MonoBehaviour
    {
        [Tooltip("Only replace colliders on pillar/prop objects.")]
        [SerializeField] private bool onlyPillars = true;

        private void Start()
        {
            BoxCollider2D[] colliders = FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None);

            int count = 0;
            foreach (BoxCollider2D col in colliders)
            {
                if (onlyPillars)
                {
                    if (col.GetComponent<ForgottenPlains.FP_Pillar>() == null)
                        continue;
                }

                SpriteRenderer sr = col.GetComponent<SpriteRenderer>();
                if (sr == null || sr.sprite == null) continue;

                GameObject obj = col.gameObject;
                Destroy(col);

                PolygonCollider2D poly = obj.AddComponent<PolygonCollider2D>();
                poly.pathCount = sr.sprite.GetPhysicsShapeCount();
                for (int i = 0; i < poly.pathCount; i++)
                {
                    var path = new System.Collections.Generic.List<Vector2>();
                    sr.sprite.GetPhysicsShape(i, path);
                    poly.SetPath(i, path);
                }

                count++;
            }

            Debug.Log($"AutoFitColliders: Replaced {count} box colliders with pixel-accurate polygon colliders.");
        }
    }
}
