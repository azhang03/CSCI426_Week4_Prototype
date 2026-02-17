using UnityEngine;

namespace Minifantasy
{
    /// <summary>
    /// Drives the player's Animator based on movement.
    /// Plays Idle when stationary, Run when moving.
    /// Flips the sprite horizontally when moving left.
    /// Add this alongside PlayerMovement2D on the player GameObject.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("Animation State Names")]
        [Tooltip("Must match the state name in the Animator Controller.")]
        [SerializeField] private string idleState = "Monk_Idle_Red";
        [SerializeField] private string runState  = "Monk_Run_Red";
        [SerializeField] private string healState = "Monk_Heal_Red";

        private Animator animator;
        private SpriteRenderer sr;
        private Rigidbody2D rb;
        private string currentState;
        private float healTimer;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            sr = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (healTimer > 0f)
            {
                healTimer -= Time.deltaTime;
                if (healTimer <= 0f)
                    currentState = null; // force re-evaluation next frame
                else
                    return; // let heal animation finish
            }

            bool moving = rb.linearVelocity.sqrMagnitude > 0.01f;
            PlayState(moving ? runState : idleState);

            // Flip sprite when moving left, unflip when moving right
            if (moving && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
                sr.flipX = rb.linearVelocity.x < 0f;
        }

        /// <summary>Play the heal animation once (call from GameManager on glasses pickup).</summary>
        public void PlayHeal()
        {
            animator.Play(healState, 0, 0f);
            currentState = healState;
            // Heal clip is 6 frames at 10fps = 0.6s
            healTimer = 0.6f;
        }

        private void PlayState(string stateName)
        {
            if (currentState == stateName) return;
            animator.Play(stateName);
            currentState = stateName;
        }
    }
}
