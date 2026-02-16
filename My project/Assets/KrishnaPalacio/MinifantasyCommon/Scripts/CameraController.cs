using UnityEngine;

namespace Minifantasy
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Follow Target")]
        [Tooltip("Drag your Player object here. If left empty, searches by tag then by component.")]
        public Transform target;

        [Header("Follow Settings")]
        [SerializeField] private float smoothTime = 0.15f;
        public Vector3 offset = new Vector3(0f, 0f, -10f);

        [Header("Camera Size")]
        [Tooltip("Orthographic size (half-height in world units). Smaller = more zoomed in.")]
        [SerializeField] private float orthographicSize = 5f;

        [Header("Map Boundaries")]
        [Tooltip("Enable to clamp camera within map bounds.")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private float minX = -16f;
        [SerializeField] private float maxX = 14f;
        [SerializeField] private float minY = -10f;
        [SerializeField] private float maxY = 20f;

        private Camera cam;
        private Vector3 velocity = Vector3.zero;

        private void Start()
        {
            cam = GetComponent<Camera>();
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.orthographicSize = orthographicSize;

            FindTarget();

            // Snap to player immediately on start (no slow pan)
            if (target != null)
                transform.position = ClampToMapBounds(target.position + offset);
        }

        private void FindTarget()
        {
            if (target != null) return;

            // Try by tag
            try
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) { target = player.transform; return; }
            }
            catch { /* Tag "Player" might not exist in tag manager */ }

            // Try by component
            PlayerMovement2D movement = FindFirstObjectByType<PlayerMovement2D>();
            if (movement != null) { target = movement.transform; return; }

            // Try by name
            GameObject byName = GameObject.Find("Player");
            if (byName != null) { target = byName.transform; return; }

            // Try by Rigidbody2D (the player has one)
            Rigidbody2D[] rbs = FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
            foreach (Rigidbody2D rb in rbs)
            {
                if (rb.gameObject != gameObject)
                {
                    target = rb.transform;
                    Debug.Log($"CameraController: Following '{target.name}' (found by Rigidbody2D). Drag your Player into the Target field for reliability.");
                    return;
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindTarget();
                if (target != null)
                    transform.position = ClampToMapBounds(target.position + offset);
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            Vector3 clamped = useBounds ? ClampToMapBounds(desiredPosition) : desiredPosition;
            transform.position = Vector3.SmoothDamp(transform.position, clamped, ref velocity, smoothTime);
        }

        private Vector3 ClampToMapBounds(Vector3 position)
        {
            float camHeight = cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;

            float clampedX, clampedY;

            // If the camera is wider than the map, center on the map
            if (camWidth * 2f >= maxX - minX)
                clampedX = (minX + maxX) / 2f;
            else
                clampedX = Mathf.Clamp(position.x, minX + camWidth, maxX - camWidth);

            if (camHeight * 2f >= maxY - minY)
                clampedY = (minY + maxY) / 2f;
            else
                clampedY = Mathf.Clamp(position.y, minY + camHeight, maxY - camHeight);

            return new Vector3(clampedX, clampedY, position.z);
        }

        private void OnDrawGizmosSelected()
        {
            if (!useBounds) return;

            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, 0f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
