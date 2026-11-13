using BreakingPizza.Core;
using BreakingPizza.Interaction;
using BreakingPizza.Minigames;
using UnityEngine;
using UnityEngine.Events;

namespace BreakingPizza.Kitchen
{
    // Ingredientes: salsa y queso.
    [RequireComponent(typeof(Collider))]
    public class IngredientPickup : MonoBehaviour, IInteractable
    {
        public enum IngredientMinigame
        {
            Auto,
            Salsa,
            Queso,
            Ninguno
        }

        [SerializeField] private string ingredientName = "Ingrediente";
        [SerializeField] private IngredientMinigame minigame = IngredientMinigame.Auto;

        [Tooltip("Se dispara cuando se consigue ESTE ingrediente puntual (después de su minijuego).")]
        public UnityEvent OnCollected = new UnityEvent();

        private bool busy;

        public string GetPrompt()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentStage != GameStage.Kitchen) return "Equipate para empezar";
            if (KitchenManager.Instance == null || !KitchenManager.Instance.IsDoughReady) return "Primero amasá la masa";

            switch (ResolveMinigame())
            {
                case IngredientMinigame.Salsa: return $"Click para dosificar {ingredientName}";
                case IngredientMinigame.Queso: return $"Click para rallar {ingredientName}";
                default: return $"Click para agarrar {ingredientName}";
            }
        }

        public void Interact()
        {
            // Chequeo de respaldo: en el flujo normal este objeto arranca
            // desactivado y recién se prende cuando termina el amasado.
            if (GameManager.Instance == null || GameManager.Instance.CurrentStage != GameStage.Kitchen) return;
            if (KitchenManager.Instance == null || !KitchenManager.Instance.IsDoughReady) return;
            if (busy) return;

            switch (ResolveMinigame())
            {
                case IngredientMinigame.Salsa:
                    busy = MinigameHost.Run(new SauceMinigame(), score => Collect(PizzaStep.Salsa, score));
                    break;
                case IngredientMinigame.Queso:
                    busy = MinigameHost.Run(new CheeseMinigame(), score => Collect(PizzaStep.Queso, score));
                    break;
                default:
                    Collect(null, 1f);
                    break;
            }
        }

        private void Collect(PizzaStep? step, float score)
        {
            busy = false;
            if (step.HasValue) PizzaScore.Record(step.Value, score);

            KitchenManager.Instance?.ReportIngredientCollected();
            OnCollected.Invoke();
            gameObject.SetActive(false);
        }

        private IngredientMinigame ResolveMinigame()
        {
            if (minigame != IngredientMinigame.Auto) return minigame;

            string n = (ingredientName + " " + name).ToLowerInvariant();
            if (n.Contains("salsa") || n.Contains("tomate")) return IngredientMinigame.Salsa;
            if (n.Contains("queso")) return IngredientMinigame.Queso;
            return IngredientMinigame.Ninguno;
        }
    }
}
