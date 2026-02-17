using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

namespace Minifantasy
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Glasses Settings")]
        [Tooltip("Total glasses needed to win.")]
        [SerializeField] private int totalGlasses = 5;

        [Header("Renderman Timing")]
        [Tooltip("Renderman lifetime (and first-spawn delay) at 0 glasses.")]
        [SerializeField] private float baseLifetime = 10f;
        [Tooltip("Renderman lifetime at max glasses collected.")]
        [SerializeField] private float minLifetime = 3f;
        [Tooltip("Seconds between one Renderman despawning and the next spawning.")]
        [SerializeField] private float respawnDelay = 0.5f;

        [Header("Renderman Spawn Positioning")]
        [Tooltip("Minimum spawn distance from the player.")]
        [SerializeField] private float minSpawnDistance = 5f;
        [Tooltip("Maximum spawn distance from the player.")]
        [SerializeField] private float maxSpawnDistance = 8f;
        [Tooltip("Half-angle (degrees) of the cone for tailored spawns. 22.5 = 45° total cone.")]
        [SerializeField] private float tailoredHalfAngle = 22.5f;

        [Header("Map Bounds (must match CameraController)")]
        [SerializeField] private float mapMinX = -16f;
        [SerializeField] private float mapMaxX = 14f;
        [SerializeField] private float mapMinY = -10f;
        [SerializeField] private float mapMaxY = 20f;

        [Header("References")]
        [SerializeField] private GameObject rendermanPrefab;
        [SerializeField] private VignetteController vignetteController;
        [SerializeField] private TextMeshProUGUI glassesText;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;

        private int glassesCollected = 0;
        private float spawnTimer;
        private float respawnTimer;
        private bool firstSpawnDone;
        private int spawnCount;
        private bool gameOver;

        private Transform playerTransform;
        private Vector2 lastMoveDir = Vector2.up;
        private Vector3 prevPlayerPos;

        public int GlassesCollected => glassesCollected;
        public int TotalGlasses => totalGlasses;
        public bool IsGameOver => gameOver;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            spawnTimer = baseLifetime;
            UpdateUI();

            if (vignetteController == null)
                vignetteController = FindFirstObjectByType<VignetteController>();

            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);

            FindPlayer();

            float startRadius = vignetteController != null
                ? vignetteController.GetCurrentVisibleRadius() : 0f;
            Debug.Log($"[Game] Start — visible radius: {startRadius:F2}");
        }

        private void Update()
        {
            HandleDebugKeys();

            if (gameOver) return;

            TrackPlayerDirection();

            if (!firstSpawnDone)
            {
                // Initial delay before the very first Renderman appears
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0f)
                {
                    SpawnRenderman();
                    firstSpawnDone = true;
                }
            }
            else if (RendermanController.ActiveInstances.Count == 0)
            {
                // Previous Renderman despawned — wait a beat, then spawn the next
                respawnTimer += Time.deltaTime;
                if (respawnTimer >= respawnDelay)
                {
                    SpawnRenderman();
                    respawnTimer = 0f;
                }
            }
            else
            {
                respawnTimer = 0f;
            }
        }

        /* ---------- debug ---------- */

        private void HandleDebugKeys()
        {
            if (Keyboard.current == null) return;

            // R = full restart (reload scene as if pressing Play)
            if (Keyboard.current.rKey.wasPressedThisFrame)
                RestartGame();

            // 1 = give one pair of glasses
            if (Keyboard.current.digit1Key.wasPressedThisFrame && !gameOver)
                CollectGlasses();
        }

        /* ---------- player tracking ---------- */

        private void FindPlayer()
        {
            if (playerTransform != null) return;

            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                playerTransform = p.transform;
                prevPlayerPos = playerTransform.position;
            }
        }

        private void TrackPlayerDirection()
        {
            if (playerTransform == null) { FindPlayer(); return; }

            Vector2 delta = (Vector2)(playerTransform.position - prevPlayerPos);
            if (delta.sqrMagnitude > 0.0001f)
                lastMoveDir = delta.normalized;
            prevPlayerPos = playerTransform.position;
        }

        /* ---------- spawning ---------- */

        public float GetCurrentLifetime()
        {
            float t = (float)glassesCollected / Mathf.Max(totalGlasses, 1);
            return Mathf.Lerp(baseLifetime, minLifetime, t);
        }

        private void SpawnRenderman()
        {
            if (rendermanPrefab == null || playerTransform == null) return;

            bool tailored = (spawnCount % 2 == 0);
            const int maxAttempts = 20;
            Vector3 spawnPos = playerTransform.position;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float angleDeg;
                if (tailored)
                {
                    float baseAngle = Mathf.Atan2(lastMoveDir.y, lastMoveDir.x) * Mathf.Rad2Deg;
                    angleDeg = baseAngle + Random.Range(-tailoredHalfAngle, tailoredHalfAngle);
                }
                else
                {
                    angleDeg = Random.Range(0f, 360f);
                }

                float angleRad = angleDeg * Mathf.Deg2Rad;
                float dist = Random.Range(minSpawnDistance, maxSpawnDistance);
                Vector2 offset = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * dist;
                spawnPos = playerTransform.position + (Vector3)offset;

                if (IsInsideMap(spawnPos))
                    break;
            }

            // Final safety clamp in case all attempts landed out of bounds
            spawnPos.x = Mathf.Clamp(spawnPos.x, mapMinX, mapMaxX);
            spawnPos.y = Mathf.Clamp(spawnPos.y, mapMinY, mapMaxY);

            float spawnDist = Vector2.Distance(playerTransform.position, spawnPos);
            Debug.Log($"[Renderman] Spawn #{spawnCount + 1} ({(tailored ? "tailored" : "random")}) — distance: {spawnDist:F2}, lifetime: {GetCurrentLifetime():F2}s");

            GameObject rm = Instantiate(rendermanPrefab, spawnPos, Quaternion.identity);
            RendermanController rmc = rm.GetComponent<RendermanController>();
            if (rmc != null)
                rmc.SetLifetime(GetCurrentLifetime());
            spawnCount++;
        }

        private bool IsInsideMap(Vector3 pos)
        {
            return pos.x >= mapMinX && pos.x <= mapMaxX
                && pos.y >= mapMinY && pos.y <= mapMaxY;
        }

        /* ---------- game flow ---------- */

        public void CollectGlasses()
        {
            if (gameOver) return;

            glassesCollected++;
            UpdateUI();

            if (vignetteController != null)
                vignetteController.OnGlassesCollected(glassesCollected, totalGlasses);

            float newRadius = vignetteController != null
                ? vignetteController.GetCurrentVisibleRadius() : 0f;
            Debug.Log($"[Game] Glasses {glassesCollected}/{totalGlasses} — visible radius: {newRadius:F2}");

            if (GlassesNotification.Instance != null)
                GlassesNotification.Instance.Show(glassesCollected, totalGlasses);

            // Play heal animation on the player
            if (playerTransform != null)
            {
                PlayerAnimator pa = playerTransform.GetComponent<PlayerAnimator>();
                if (pa != null) pa.PlayHeal();
            }

            if (glassesCollected >= totalGlasses)
                Win();
        }

        public void Lose()
        {
            if (gameOver) return;
            gameOver = true;
            Time.timeScale = 0f;
            if (losePanel != null) losePanel.SetActive(true);
        }

        private void Win()
        {
            if (gameOver) return;
            gameOver = true;
            Time.timeScale = 0f;
            if (winPanel != null) winPanel.SetActive(true);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void UpdateUI()
        {
            if (glassesText != null)
                glassesText.text = $"Glasses: {glassesCollected} / {totalGlasses}";
        }
    }
}
