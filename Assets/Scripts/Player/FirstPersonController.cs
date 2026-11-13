using BreakingPizza.Audio;
using BreakingPizza.Controls;
using BreakingPizza.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BreakingPizza.Player
{
    // Movimiento y cámara en primera persona.
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("La cámara hija que rota en pitch (mirar arriba/abajo). Asignar la Main Camera hija de este GameObject.")]
        [SerializeField] private Transform cameraTransform;

        [Header("Movimiento")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float runSpeed = 7.5f;
        [Tooltip("Desactivado: se camina siempre. Tildalo para poder correr con Shift.")]
        [SerializeField] private bool allowRunning = false;

        [Header("Mouse look")]
        [SerializeField] private float mouseSensitivity = 0.15f;
        [Tooltip("Cuánto podés mirar hacia arriba/abajo antes de topar, en grados desde el horizonte.")]
        [SerializeField] private float maxLookAngle = 85f;

        [Header("Control (gamepad)")]
        [Tooltip("Grados por segundo al girar con el stick derecho a fondo.")]
        [SerializeField] private float gamepadLookSpeed = 170f;

        [Header("Salto y gravedad")]
        [Tooltip("Este es un simulador sin plataformeo, así que el salto no es indispensable, pero se deja porque casi no suma complejidad (la gravedad manual ya es necesaria para que el CharacterController se mantenga pegado al piso) y es un reflejo que cualquier jugador prueba. Desmarcá esto si preferís que Space no haga nada.")]
        [SerializeField] private bool enableJump = true;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -20f;

        [Header("Seguridad (no caerse del mapa)")]
        [Tooltip("Si el jugador cae esta cantidad de metros por debajo del último lugar firme donde estuvo, vuelve ahí.")]
        [SerializeField] private float fallResetDistance = 25f;

        [Header("Pasos")]
        [SerializeField] private bool footsteps = true;
        [Tooltip("Metros entre pisada y pisada caminando (corriendo se acorta un poco por la cadencia).")]
        [SerializeField] private float strideLength = 1.9f;
        [Range(0f, 1f)] [SerializeField] private float footstepVolume = 0.22f;

        public static FirstPersonController Instance { get; private set; }

        private CharacterController controller;
        private float pitch;
        private float verticalVelocity;
        private bool cursorLocked;
        private bool jumpTemporarilyDisabled;
        private bool gameplayInputSuspended;
        private Vector3 lastSafePosition;
        private float safeTimer;
        private float stepDistance;
        private bool wasGrounded = true;
        private float airTime;

        private void Awake()
        {
            Instance = this;
            controller = GetComponent<CharacterController>();

            pitch = 0f;
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.identity;
            }
            else
            {
                Debug.LogWarning($"{name}: falta asignar \"Camera Transform\" en el Inspector — el mouse look no va a funcionar sin eso.", this);
            }

            SetCursorLocked(true);
            lastSafePosition = transform.position;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                SetCursorLocked(cursorLocked);
            }
        }

        private void Update()
        {
            HandleCursorLock();
            HandleLook();
            HandleMovement();
            HandleFallSafety();
        }

        private void HandleFallSafety()
        {
            if (controller.isGrounded)
            {
                safeTimer += Time.deltaTime;
                if (safeTimer >= 0.5f)
                {
                    safeTimer = 0f;
                    lastSafePosition = transform.position;
                }
            }

            if (transform.position.y < lastSafePosition.y - fallResetDistance)
            {
                Debug.LogWarning("[FirstPersonController] El jugador se cayó del mapa: vuelve al último lugar firme.");
                Teleport(lastSafePosition + Vector3.up * 0.3f, Quaternion.Euler(pitch, transform.eulerAngles.y, 0f));
            }
        }

        private void HandleCursorLock()
        {
            if (gameplayInputSuspended) return;

            // Si el cursor quedó libre y el jugador agarra el control, se
            // vuelve a capturar solo (con el control no se usa el cursor).
            if (!cursorLocked && GameInput.UsingGamepad)
            {
                SetCursorLocked(true);
                return;
            }

            Mouse mouse = Mouse.current;
            if (!cursorLocked && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                SetCursorLocked(true);
                return;
            }

            if (cursorLocked && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void HandleLook()
        {
            if (gameplayInputSuspended) return;

            if (cameraTransform == null) return;

            // Stick derecho del control (funciona aunque el cursor esté libre).
            Vector2 stick = GameInput.LookStick;
            if (stick != Vector2.zero)
            {
                float speed = gamepadLookSpeed * Time.deltaTime;
                transform.Rotate(Vector3.up * (stick.x * speed));
                pitch = Mathf.Clamp(pitch - stick.y * speed * 0.75f, -maxLookAngle, maxLookAngle);
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || !cursorLocked) return;

            Vector2 delta = mouse.delta.ReadValue();

            float yaw = delta.x * mouseSensitivity;
            pitch -= delta.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

            transform.Rotate(Vector3.up * yaw);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            Keyboard keyboard = Keyboard.current;

            bool canMove = !gameplayInputSuspended;

            // WASD + stick izquierdo (GameInput ya los combina).
            Vector2 move = canMove ? GameInput.Move : Vector2.zero;
            Vector3 inputDirection = new Vector3(move.x, 0f, move.y);

            Vector3 worldDirection = transform.TransformDirection(inputDirection);
            bool running = allowRunning && keyboard != null && keyboard.leftShiftKey.isPressed;
            float speed = running ? runSpeed : walkSpeed;

            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f;

                if (canMove && enableJump && !jumpTemporarilyDisabled && keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 motion = worldDirection * speed + Vector3.up * verticalVelocity;
            controller.Move(motion * Time.deltaTime);

            if (footsteps) HandleFootsteps(running);
        }

        private void HandleFootsteps(bool running)
        {
            bool grounded = controller.isGrounded;

            if (!grounded)
            {
                airTime += Time.deltaTime;
            }
            else
            {
                if (!wasGrounded && airTime > 0.25f)
                {
                    Sfx.Play(SfxId.Land, footstepVolume * 1.2f, Random.Range(0.9f, 1.05f));
                    stepDistance = 0f;
                }
                airTime = 0f;
            }
            wasGrounded = grounded;

            Vector3 v = controller.velocity;
            float speed = new Vector2(v.x, v.z).magnitude;
            if (!grounded || speed < 0.6f || gameplayInputSuspended)
            {
                // Al frenar, la próxima pisada arranca "a la mitad" del
                // paso para que el primer paso suene casi enseguida.
                stepDistance = Mathf.Max(stepDistance, strideLength * 0.6f);
                return;
            }

            stepDistance += speed * Time.deltaTime;
            float stride = running ? strideLength * 1.25f : strideLength;
            if (stepDistance >= stride)
            {
                stepDistance = 0f;
                Sfx.Step(IsOnDirt(), footstepVolume * (running ? 1.15f : 1f));
            }
        }

        private bool IsOnDirt()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentStage == GameStage.Desert) return true;

            // Fuera de esa etapa, tierra solo si el piso es un Terrain.
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2f, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.collider is TerrainCollider;
            }
            return false;
        }

        public void SetJumpEnabled(bool jumpEnabled)
        {
            jumpTemporarilyDisabled = !jumpEnabled;
        }

        public void SetGameplayInputEnabled(bool inputEnabled)
        {
            gameplayInputSuspended = !inputEnabled;

            if (!inputEnabled)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                SetCursorLocked(true);
            }
        }

        private void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;

            Vector3 euler = rotation.eulerAngles;
            float targetYaw = euler.y;
            float targetPitch = euler.x > 180f ? euler.x - 360f : euler.x;
            targetPitch = Mathf.Clamp(targetPitch, -maxLookAngle, maxLookAngle);

            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, targetYaw, 0f));
            pitch = targetPitch;
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            verticalVelocity = 0f;
            controller.enabled = true;
        }
    }
}
