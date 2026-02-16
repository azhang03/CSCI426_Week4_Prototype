using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace Minifantasy
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Glasses Settings")]
        [Tooltip("Total glasses needed to win.")]
        [SerializeField] private int totalGlasses = 4;

        [Header("Renderman Spawn Timing")]
        [Tooltip("Base interval (seconds) between Renderman spawns at 0 glasses.")]
        [SerializeField] private float baseSpawnInterval = 10f;
        [Tooltip("Minimum interval (seconds) at max glasses collected.")]
        [SerializeField] private float minSpawnInterval = 3f;
        [Tooltip("How many seconds the player has to leave Renderman's AoE.")]
        [SerializeField] private float escapeTime = 3f;
        [Tooltip("Radius of Renderman's danger zone.")]
        [SerializeField] private float rendermanRadius = 3f;

        [Header("References")]
        [SerializeField] private GameObject rendermanPrefab;
        [SerializeField] private VignetteController vignetteController;
        [SerializeField] private TextMeshProUGUI glassesText;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;

        private int glassesCollected = 0;
        private float spawnTimer;
        private bool gameOver = false;

        public int GlassesCollected => glassesCollected;
        public int TotalGlasses => totalGlasses;
        public float EscapeTime => escapeTime;
        public float RendermanRadius => rendermanRadius;
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
            spawnTimer = baseSpawnInterval;
            UpdateUI();

            if (vignetteController == null)
                vignetteController = FindFirstObjectByType<VignetteController>();

            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
        }

        private void Update()
        {
            if (gameOver) return;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnRenderman();
                spawnTimer = GetCurrentSpawnInterval();
            }
        }

        public float GetCurrentSpawnInterval()
        {
            // Positive feedback loop: more glasses -> shorter interval -> more danger
            float t = (float)glassesCollected / Mathf.Max(totalGlasses, 1);
            return Mathf.Lerp(baseSpawnInterval, minSpawnInterval, t);
        }

        public void CollectGlasses()
        {
            if (gameOver) return;

            glassesCollected++;
            UpdateUI();

            if (vignetteController != null)
                vignetteController.OnGlassesCollected(glassesCollected, totalGlasses);

            // Reset spawn timer to give a brief breather after pickup
            spawnTimer = GetCurrentSpawnInterval();

            if (glassesCollected >= totalGlasses)
                Win();
        }

        private void SpawnRenderman()
        {
            if (rendermanPrefab == null) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            // Spawn Renderman at a random offset near the player (within visible area)
            float vignetteRadius = vignetteController != null
                ? vignetteController.GetCurrentVisibleRadius()
                : 4f;

            // Spawn within the player's visible area so they see it coming
            float spawnDist = Random.Range(1f, Mathf.Max(vignetteRadius * 0.7f, 2f));
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnDist;
            Vector3 spawnPos = player.transform.position + (Vector3)offset;

            Instantiate(rendermanPrefab, spawnPos, Quaternion.identity);
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
