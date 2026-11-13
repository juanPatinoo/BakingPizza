using BreakingPizza.Core;
using BreakingPizza.Interaction;
using BreakingPizza.Minigames;
using UnityEngine;
using UnityEngine.Events;

namespace BreakingPizza.Kitchen
{
    // Mesada de armado y horno: pepperoni y horneado.
    [RequireComponent(typeof(Collider))]
    public class BakingStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameStage stageAfterBaking = GameStage.Result;

        [Header("UI (prompt en el mundo)")]
        [SerializeField] private string assemblePromptText = "Click para armar y hornear la pizza";
        [SerializeField] private string lockedPromptText = "Equipate para empezar";

        [Header("Eventos UI (conectables desde el Inspector)")]
        [Tooltip("Se dispara al empezar el armado.")]
        public UnityEvent OnBakingStarted = new UnityEvent();

        [Tooltip("Se dispara al terminar de hornear.")]
        public UnityEvent OnBakingFinished = new UnityEvent();

        private bool started;
        private bool finished;

        private static bool IsKitchenStageActive()
        {
            return GameManager.Instance != null && GameManager.Instance.CurrentStage == GameStage.Kitchen;
        }

        public string GetPrompt()
        {
            if (!IsKitchenStageActive()) return lockedPromptText;
            if (finished) return "Pizza lista";

            if (KitchenManager.Instance == null || !KitchenManager.Instance.AllIngredientsCollected)
            {
                int collected = KitchenManager.Instance != null ? KitchenManager.Instance.CollectedCount : 0;
                int required = KitchenManager.Instance != null ? KitchenManager.Instance.RequiredIngredients : 0;
                return $"Faltan ingredientes ({collected}/{required})";
            }

            return started ? "Cocinando..." : assemblePromptText;
        }

        public void Interact()
        {
            if (!IsKitchenStageActive() || finished || started) return;
            if (KitchenManager.Instance == null || !KitchenManager.Instance.AllIngredientsCollected) return;

            started = MinigameHost.Run(
                new Minigame[] { new PepperoniMinigame(), new OvenMinigame() },
                (game, score) => PizzaScore.Record(game.Step, score),
                Finish);

            if (started) OnBakingStarted.Invoke();
        }

        private void Finish()
        {
            finished = true;
            OnBakingFinished.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetStage(stageAfterBaking);
            }
            else
            {
                Debug.LogWarning($"{name}: no se encontró GameManager.Instance.", this);
            }
        }
    }
}
