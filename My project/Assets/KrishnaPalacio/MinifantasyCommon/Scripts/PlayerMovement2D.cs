using UnityEngine;
using UnityEngine.InputSystem;

namespace Minifantasy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 6f;
        [SerializeField] private bool normalizeDiagonal = true;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaRegenDelay = 0.8f;

        [Header("Drain Feedback (less stamina = faster drain)")]
        [Tooltip("Drain rate when stamina is full.")]
        [SerializeField] private float drainRateMin = 20f;
        [Tooltip("Drain rate when stamina is empty.")]
        [SerializeField] private float drainRateMax = 50f;

        [Header("Regen Feedback (more stamina = faster regen)")]
        [Tooltip("Regen rate when stamina is empty.")]
        [SerializeField] private float regenRateMin = 5f;
        [Tooltip("Regen rate when stamina is full.")]
        [SerializeField] private float regenRateMax = 25f;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private bool isSprinting;
        private float currentStamina;
        private float regenTimer;

        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public bool IsSprinting => isSprinting;
        public bool IsMoving => moveInput.sqrMagnitude > 0.01f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            currentStamina = maxStamina;
        }

        private void Update()
        {
            moveInput = ReadMoveInput();

            if (normalizeDiagonal && moveInput.sqrMagnitude > 1f)
                moveInput.Normalize();

            bool wantsToSprint = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
            isSprinting = wantsToSprint && IsMoving && currentStamina > 0f;

            if (isSprinting)
            {
                float staminaRatio = currentStamina / maxStamina;
                float drain = Mathf.Lerp(drainRateMax, drainRateMin, staminaRatio);
                currentStamina -= drain * Time.deltaTime;
                currentStamina = Mathf.Max(currentStamina, 0f);
                regenTimer = staminaRegenDelay;
            }
            else
            {
                regenTimer -= Time.deltaTime;
                if (regenTimer <= 0f)
                {
                    float staminaRatio = currentStamina / maxStamina;
                    float regen = Mathf.Lerp(regenRateMin, regenRateMax, staminaRatio);
                    currentStamina += regen * Time.deltaTime;
                    currentStamina = Mathf.Min(currentStamina, maxStamina);
                }
            }
        }

        private void FixedUpdate()
        {
            float speed = isSprinting ? sprintSpeed : walkSpeed;
            rb.linearVelocity = moveInput * speed;
        }

        private static Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                    input.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                    input.y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    input.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    input.x += 1f;
            }

            if (input == Vector2.zero && Gamepad.current != null)
                input = Gamepad.current.leftStick.ReadValue();

            if (input.sqrMagnitude > 1f)
                input.Normalize();

            return input;
        }
    }
}
