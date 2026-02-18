using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minifantasy
{
    /// <summary>
    /// Central audio controller. Attach to any scene GameObject.
    /// Manages: forest ambience, exposure static, 4th-glasses ambience swap,
    /// footsteps (land / water), glasses chime, and Renderman first-sighting sting.
    /// All clips and volumes are assignable in Inspector.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Forest Ambience (loops until 4th glasses)")]
        [SerializeField] private AudioClip forestClip;
        [Range(0f, 1f)]
        [SerializeField] private float forestVolume = 0.5f;

        [Header("4th-Glasses Ambience (replaces forest permanently)")]
        [SerializeField] private AudioClip fourthGlassesClip;
        [Range(0f, 1f)]
        [SerializeField] private float fourthGlassesVolume = 0.5f;

        [Header("Ambience Crossfade")]
        [Tooltip("How fast the ambience fades in/out (volume units per second).")]
        [SerializeField] private float ambienceFadeSpeed = 1.0f;

        [Header("Static Sound (driven by Exposure meter)")]
        [SerializeField] private AudioClip staticClip;
        [Tooltip("Static volume when exposure first appears.")]
        [Range(0f, 1f)]
        [SerializeField] private float staticVolumeBase = 0.05f;
        [Tooltip("Static volume at full exposure.")]
        [Range(0f, 1f)]
        [SerializeField] private float staticVolumeMax = 0.8f;
        [Tooltip("How fast the static fades in/out (volume units per second).")]
        [SerializeField] private float staticFadeSpeed = 2f;

        [Header("Footsteps")]
        [SerializeField] private AudioClip walkClip;
        [Range(0f, 1f)]
        [SerializeField] private float walkVolume = 0.3f;
        [SerializeField] private AudioClip waterWalkClip;
        [Range(0f, 1f)]
        [SerializeField] private float waterWalkVolume = 0.4f;

        [Header("Glasses Chime")]
        [SerializeField] private AudioClip glassesChimeClip;
        [Range(0f, 1f)]
        [SerializeField] private float chimeVolume = 0.7f;

        [Header("Renderman First-Sighting Sting")]
        [SerializeField] private AudioClip rendermanSightClip;
        [Range(0f, 1f)]
        [SerializeField] private float sightVolume = 0.6f;

        [Header("Map")]
        [SerializeField] private AudioClip mapRuffleClip;
        [Range(0f, 1f)]
        [SerializeField] private float mapRuffleVolume = 0.5f;

        private AudioSource ambienceSource;
        private AudioSource staticSource;
        private AudioSource walkSource;
        private AudioSource oneShotSource;

        private ExposureMeter exposureMeter;
        private PlayerMovement2D playerMovement;
        private Transform playerTransform;
        private Tilemap waterTilemap;

        private bool fourthGlassesActive;
        private bool sourcesPaused;

        /* ---------- lifecycle ---------- */

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            exposureMeter = FindFirstObjectByType<ExposureMeter>();
            FindPlayer();
            FindWater();
            CreateSources();

            if (forestClip != null)
            {
                ambienceSource.clip = forestClip;
                ambienceSource.volume = forestVolume;
                ambienceSource.Play();
            }
        }

        private void Update()
        {
            if (exposureMeter == null)
                exposureMeter = FindFirstObjectByType<ExposureMeter>();
            if (playerTransform == null)
                FindPlayer();

            UpdateAmbienceAndStatic();
            UpdateFootsteps();
        }

        /* ---------- public API ---------- */

        public void PlayGlassesChime()
        {
            if (glassesChimeClip != null && oneShotSource != null)
                oneShotSource.PlayOneShot(glassesChimeClip, chimeVolume);
        }

        public void OnFourthGlassesCollected()
        {
            if (fourthGlassesActive) return;
            fourthGlassesActive = true;

            if (fourthGlassesClip != null && ambienceSource != null)
            {
                ambienceSource.clip = fourthGlassesClip;
                ambienceSource.volume = 0f;
                ambienceSource.Play();
            }
        }

        public void OnRendermanFirstSeen()
        {
            if (fourthGlassesActive) return;
            if (rendermanSightClip != null && oneShotSource != null)
                oneShotSource.PlayOneShot(rendermanSightClip, sightVolume);
        }

        public void PlayMapRuffle()
        {
            if (mapRuffleClip != null && oneShotSource != null)
                oneShotSource.PlayOneShot(mapRuffleClip, mapRuffleVolume);
        }

        /* ---------- init helpers ---------- */

        private void FindPlayer()
        {
            if (playerTransform != null) return;
            try
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                {
                    playerTransform = p.transform;
                    playerMovement = p.GetComponent<PlayerMovement2D>();
                }
            }
            catch { }
        }

        private void FindWater()
        {
            GameObject water = GameObject.Find("Water");
            if (water != null)
                waterTilemap = water.GetComponent<Tilemap>();
        }

        private void CreateSources()
        {
            ambienceSource = MakeLoop("Ambience");
            staticSource   = MakeLoop("Static");
            walkSource     = MakeLoop("Walk");

            oneShotSource = gameObject.AddComponent<AudioSource>();
            oneShotSource.playOnAwake = false;
        }

        private AudioSource MakeLoop(string label)
        {
            GameObject obj = new GameObject($"Audio_{label}");
            obj.transform.SetParent(transform, false);
            AudioSource src = obj.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            return src;
        }

        /* ---------- per-frame ---------- */

        private void UpdateAmbienceAndStatic()
        {
            float dt = Time.unscaledDeltaTime;
            bool gameOver = GameManager.Instance != null && GameManager.Instance.IsGameOver;
            bool blocked = gameOver || Time.timeScale == 0f;

            if (blocked)
            {
                if (!sourcesPaused)
                {
                    if (gameOver)
                    {
                        if (ambienceSource != null) ambienceSource.Stop();
                        if (staticSource != null) staticSource.Stop();
                    }
                    else
                    {
                        if (ambienceSource != null) ambienceSource.Pause();
                        if (staticSource != null) staticSource.Pause();
                    }
                    sourcesPaused = true;
                }
                return;
            }

            if (sourcesPaused)
            {
                if (ambienceSource != null) ambienceSource.UnPause();
                if (staticSource != null) staticSource.UnPause();
                sourcesPaused = false;
            }

            float exposure = exposureMeter != null ? exposureMeter.Exposure : 0f;
            bool hasExposure = exposure > 0.001f;

            // --- ambience: full volume when no exposure, silent when any ---
            float maxAmbVol = fourthGlassesActive ? fourthGlassesVolume : forestVolume;
            float ambTarget = hasExposure ? 0f : maxAmbVol;
            if (ambienceSource != null)
                ambienceSource.volume = Mathf.MoveTowards(
                    ambienceSource.volume, ambTarget, ambienceFadeSpeed * dt);

            // --- static: volume proportional to exposure ---
            if (staticClip != null && staticSource != null)
            {
                if (!staticSource.isPlaying && hasExposure)
                {
                    staticSource.clip = staticClip;
                    staticSource.volume = 0f;
                    staticSource.Play();
                }

                float stTarget = hasExposure
                    ? Mathf.Lerp(staticVolumeBase, staticVolumeMax, exposure)
                    : 0f;
                staticSource.volume = Mathf.MoveTowards(
                    staticSource.volume, stTarget, staticFadeSpeed * dt);

                if (staticSource.isPlaying && staticSource.volume < 0.001f && !hasExposure)
                    staticSource.Stop();
            }
        }

        private void UpdateFootsteps()
        {
            if (playerMovement == null || walkSource == null) return;

            bool blocked = Time.timeScale == 0f
                || (GameManager.Instance != null && GameManager.Instance.IsGameOver);

            if (blocked || !playerMovement.IsMoving)
            {
                if (walkSource.isPlaying) walkSource.Stop();
                return;
            }

            bool onWater = IsPlayerOnWater();
            AudioClip desired = (onWater && waterWalkClip != null) ? waterWalkClip : walkClip;
            float desiredVol = (onWater && waterWalkClip != null) ? waterWalkVolume : walkVolume;

            if (desired == null)
            {
                if (walkSource.isPlaying) walkSource.Stop();
                return;
            }

            if (walkSource.clip != desired)
            {
                walkSource.clip = desired;
                walkSource.volume = desiredVol;
                walkSource.Play();
            }
            else if (!walkSource.isPlaying)
            {
                walkSource.volume = desiredVol;
                walkSource.Play();
            }
        }

        private bool IsPlayerOnWater()
        {
            if (waterTilemap == null || playerTransform == null) return false;
            Vector3Int cell = waterTilemap.WorldToCell(playerTransform.position);
            return waterTilemap.HasTile(cell);
        }
    }
}
