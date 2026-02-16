using UnityEngine;

namespace Minifantasy
{
    /// <summary>
    /// Attach to a GameObject with a Collider2D set to IsTrigger.
    /// When the Player enters the trigger, glasses are collected.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GlassesPickup : MonoBehaviour
    {
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.15f;

        private Vector3 startPos;

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            // Simple floating bob animation
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = startPos + new Vector3(0f, yOffset, 0f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.CollectGlasses();

                Destroy(gameObject);
            }
        }
    }
}
