using BreakingPizza.Core;
using BreakingPizza.Interaction;
using BreakingPizza.Minigames;
using UnityEngine;
using UnityEngine.Events;

namespace BreakingPizza.Kitchen
{
    // Mesada de amasado.
    [RequireComponent(typeof(Collider))]
    public class DoughStation : MonoBehaviour, IInteractable
    {
        [Header("UI (prompt en el mundo)")]
        [SerializeField] private string promptText = "Click para amasar";
        [SerializeField] private string lockedPromptText = "Equipate para empezar";

        [Header("Eventos")]
        [Tooltip("Se dispara al terminar de amasar. Ya conectado: activar Salsa y Queso, KitchenManager.SetDoughReady, etc.")]
        public UnityEvent OnDoughReady = new UnityEvent();

        private bool busy;
        private bool done;

        private static bool IsKitchenStageActive()
        {
            return GameManager.Instance != null && GameManager.Instance.CurrentStage == GameStage.Kitchen;
        }

        public string GetPrompt()
        {
            if (!IsKitchenStageActive()) return lockedPromptText;
            if (done) return "Masa lista";
            if (busy) return "Amasando...";
            return promptText;
        }

        public void Interact()
        {
            if (!IsKitchenStageActive() || busy || done) return;

            busy = MinigameHost.Run(new KneadMinigame(), score =>
            {
                busy = false;
                done = true;
                PizzaScore.Record(PizzaStep.Masa, score);

                KitchenManager.Instance?.SetDoughReady();
                OnDoughReady.Invoke();
            });
        }
    }
}
