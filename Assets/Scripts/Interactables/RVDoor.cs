using BreakingPizza.Core;
using BreakingPizza.Interaction;
using BreakingPizza.Player;
using UnityEngine;

namespace BreakingPizza.Interactables
{
    // Puerta de la casa rodante: lleva al jugador al laboratorio.
    [RequireComponent(typeof(Collider))]
    public class RVDoor : MonoBehaviour, IInteractable
    {
        [Header("Destino al interactuar")]
        [SerializeField] private GameStage stageOnEnter = GameStage.Dressing;

        [Header("Teletransporte del jugador")]
        [Tooltip("Punto dentro de la RV donde aparece el jugador (posición + rotación).")]
        [SerializeField] private Transform interiorSpawnPoint;

        [Tooltip("Raíz del jugador a mover (el objeto con el CharacterController, si lo tenés). Si se deja vacío, se busca por GameObject.FindGameObjectWithTag(\"Player\") como respaldo.")]
        [SerializeField] private Transform playerRig;

        [Header("UI")]
        [SerializeField] private string promptText = "Click para ir al laboratorio";

        public string GetPrompt() => promptText;

        public void Interact()
        {
            if (interiorSpawnPoint == null)
            {
                Debug.LogWarning($"{name}: falta asignar Interior Spawn Point en el Inspector.", this);
                return;
            }

            // Fundido a negro: la pantalla se oscurece, el jugador aparece
            // adentro y vuelve a aclarar (en vez de un salto brusco).
            BreakingPizza.UI.ScreenFader.FadeThroughBlack(() =>
            {
                TeleportPlayer();

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetStage(stageOnEnter);
                }
                else
                {
                    Debug.LogWarning($"{name}: no se encontró GameManager.Instance en la escena.", this);
                }
            });
        }

        private void TeleportPlayer()
        {
            Transform rig = ResolvePlayerRig();
            if (rig == null)
            {
                Debug.LogWarning($"{name}: no se encontró el Player Rig (asignalo en el Inspector o poné tag \"Player\").", this);
                return;
            }

            var fpsController = rig.GetComponent<FirstPersonController>();
            if (fpsController != null)
            {
                fpsController.Teleport(interiorSpawnPoint.position, interiorSpawnPoint.rotation);
                return;
            }

            var characterController = rig.GetComponent<CharacterController>();
            if (characterController != null) characterController.enabled = false;

            rig.SetPositionAndRotation(interiorSpawnPoint.position, interiorSpawnPoint.rotation);

            if (characterController != null) characterController.enabled = true;

            var rigidbody = rig.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }
        }

        private Transform ResolvePlayerRig()
        {
            if (playerRig != null) return playerRig;

            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            return tagged != null ? tagged.transform : null;
        }
    }
}
