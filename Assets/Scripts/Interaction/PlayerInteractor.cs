using BreakingPizza.Controls;
using BreakingPizza.Minigames;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace BreakingPizza.Interaction
{
    // Detecta qué objeto mira el jugador y lo deja interactuar.
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private float interactDistance = 3f;
        [SerializeField] private LayerMask interactableLayers = ~0;

        [Header("Cámara")]
        [Tooltip("Cámara en primera persona desde la que se lanza el rayo. Si se deja vacío, usa Camera.main.")]
        [SerializeField] private Camera playerCamera;

        [Header("Eventos UI (conectables desde el Inspector)")]
        public UnityEvent<string> OnPromptShown = new UnityEvent<string>();
        public UnityEvent OnPromptHidden = new UnityEvent();

        [Header("Debug")]
        [Tooltip("Dibuja el rayo en Scene View (verde si pega en un IInteractable, rojo si no) y tira logs en Console al encontrar/perder un interactuable y al interactuar. Apagalo una vez que confirmes que todo anda.")]
        [SerializeField] private bool debugLogging = false;

        private IInteractable currentInteractable;
        private string currentInteractableName;
        private string currentPrompt;

        // ¿Estás apuntando a algo con lo que se puede interactuar? (lo usa
        // la mira del centro y los brazos).
        public bool HasTarget => currentInteractable != null;

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (MinigameHost.IsRunning || IsResultStage() || UI.PauseMenu.IsPaused)
            {
                ClearCurrent();
                return;
            }

            UpdateCurrentInteractable();
            ReadInput();
        }

        private static bool IsResultStage()
        {
            return Core.GameManager.Instance != null && Core.GameManager.Instance.CurrentStage == Core.GameStage.Result;
        }

        private void ClearCurrent()
        {
            if (currentInteractable == null) return;
            currentInteractable = null;
            currentInteractableName = null;
            currentPrompt = null;
            OnPromptHidden.Invoke();
        }

        private static readonly RaycastHit[] hitBuffer = new RaycastHit[16];

        private bool RaycastPreferInteractable(Ray ray, out RaycastHit result)
        {
            result = default;
            int count = Physics.RaycastNonAlloc(ray, hitBuffer, interactDistance, interactableLayers);
            if (count == 0) return false;

            // Se ignora todo lo que cuelga del jugador (máscara en la mano o
            // en la cara, brazos, etc.): nunca debe tapar lo que se mira.
            Transform self = Player.FirstPersonController.Instance != null ? Player.FirstPersonController.Instance.transform : null;

            int nearest = -1;
            for (int i = 0; i < count; i++)
            {
                if (self != null && hitBuffer[i].collider.transform.IsChildOf(self)) continue;
                if (nearest < 0 || hitBuffer[i].distance < hitBuffer[nearest].distance) nearest = i;
            }
            if (nearest < 0) return false;
            result = hitBuffer[nearest];
            if (result.collider.TryGetComponent<IInteractable>(out _)) return true;

            float limit = result.distance + 0.35f;
            int best = -1;
            for (int i = 0; i < count; i++)
            {
                if (hitBuffer[i].distance > limit) continue;
                if (self != null && hitBuffer[i].collider.transform.IsChildOf(self)) continue;
                if (!hitBuffer[i].collider.TryGetComponent<IInteractable>(out _)) continue;
                if (best < 0 || hitBuffer[i].distance < hitBuffer[best].distance) best = i;
            }
            if (best >= 0) result = hitBuffer[best];
            return true;
        }

        private void UpdateCurrentInteractable()
        {
            if (playerCamera == null) return;

            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            bool didHit = RaycastPreferInteractable(ray, out RaycastHit hit);

            IInteractable interactable = null;
            bool hitIsInteractable = didHit && hit.collider.TryGetComponent(out interactable);

            if (debugLogging)
            {
                Debug.DrawRay(ray.origin, ray.direction * interactDistance, hitIsInteractable ? Color.green : Color.red);
            }

            if (hitIsInteractable)
            {
                string prompt = interactable.GetPrompt();
                if (ReferenceEquals(interactable, currentInteractable) && prompt != currentPrompt)
                {
                    currentPrompt = prompt;
                    OnPromptShown.Invoke(prompt);
                }

                if (!ReferenceEquals(interactable, currentInteractable))
                {
                    currentInteractable = interactable;
                    currentInteractableName = hit.collider.name;
                    currentPrompt = prompt;
                    OnPromptShown.Invoke(prompt);

                    if (debugLogging)
                    {
                        Debug.Log($"[PlayerInteractor] Encontrado: {interactable.GetType().Name} \"{hit.collider.name}\" — prompt \"{interactable.GetPrompt()}\"", hit.collider);
                    }
                }

                return;
            }

            if (currentInteractable != null)
            {
                if (debugLogging)
                {
                    Debug.Log($"[PlayerInteractor] Perdido: {currentInteractable.GetType().Name} \"{currentInteractableName}\"");
                }

                currentInteractable = null;
                currentInteractableName = null;
                currentPrompt = null;
                OnPromptHidden.Invoke();
            }
        }

        private void ReadInput()
        {
            // Click, E, o X en el control (ver GameInput).
            if (!GameInput.InteractPressed) return;

            if (currentInteractable == null)
            {
                if (debugLogging)
                {
                    Debug.Log("[PlayerInteractor] Se apretó interactuar pero currentInteractable es null (el rayo no está pegando en ningún IInteractable ahora mismo).");
                }

                return;
            }

            if (debugLogging)
            {
                Debug.Log($"[PlayerInteractor] Interact() -> {currentInteractable.GetType().Name} \"{currentInteractableName}\"");
            }

            currentInteractable.Interact();
        }
    }
}
