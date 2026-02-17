using UnityEngine;

namespace Minifantasy
{
    /// <summary>
    /// Collectible lifesaver. Bobs in the world like glasses.
    /// On pickup: attaches the lifesaver sprite behind the player (who appears
    /// sitting in the middle of it), disables water collision, and self-destructs.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LifesaverPickup : MonoBehaviour
    {
        [Header("World Bob (before pickup)")]
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.15f;

        [Header("Attached Bob (on player)")]
        [Tooltip("Bob speed once attached to the player.")]
        [SerializeField] private float attachedBobSpeed = 2.5f;
        [Tooltip("Bob amplitude once attached (subtle float effect).")]
        [SerializeField] private float attachedBobHeight = 0.06f;

        private Vector3 startPos;

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = startPos + new Vector3(0f, yOffset, 0f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            AttachToPlayer(other.transform);
            DisableWaterCollision();
            Destroy(gameObject);
        }

        private void AttachToPlayer(Transform player)
        {
            // Grab the sprite from this pickup before it's destroyed
            Sprite sprite = null;
            SpriteRenderer pickupSR = GetComponent<SpriteRenderer>();
            if (pickupSR != null)
                sprite = pickupSR.sprite;

            // Create a child object on the player
            GameObject lifesaverObj = new GameObject("Lifesaver");
            lifesaverObj.transform.SetParent(player, false);
            lifesaverObj.transform.localPosition = Vector3.zero;

            SpriteRenderer sr = lifesaverObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            // Render behind the player so the character appears inside the ring
            SpriteRenderer playerSR = player.GetComponent<SpriteRenderer>();
            sr.sortingOrder = playerSR != null ? playerSR.sortingOrder - 1 : 0;

            // Add gentle bob so it looks like it's floating
            LifesaverBob bob = lifesaverObj.AddComponent<LifesaverBob>();
            bob.SetParams(attachedBobSpeed, attachedBobHeight);
        }

        private void DisableWaterCollision()
        {
            GameObject water = GameObject.Find("Water");
            if (water == null) return;

            // Disable every collider on the Water tilemap
            Collider2D[] colliders = water.GetComponents<Collider2D>();
            foreach (Collider2D col in colliders)
                col.enabled = false;

            Debug.Log("[Lifesaver] Water collision disabled.");
        }
    }

    /// <summary>
    /// Tiny helper that bobs a transform's local Y position.
    /// Attached at runtime by LifesaverPickup.
    /// </summary>
    public class LifesaverBob : MonoBehaviour
    {
        private float speed = 2.5f;
        private float height = 0.06f;

        public void SetParams(float bobSpeed, float bobHeight)
        {
            speed = bobSpeed;
            height = bobHeight;
        }

        private void Update()
        {
            float yOffset = Mathf.Sin(Time.time * speed) * height;
            transform.localPosition = new Vector3(0f, yOffset, 0f);
        }
    }
}
