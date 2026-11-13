using System;
using UnityEngine;
using UnityEngine.Events;

namespace BreakingPizza.Kitchen
{
    // Estado de la cocina: masa lista e ingredientes conseguidos.
    [Serializable]
    public class IntIntUnityEvent : UnityEvent<int, int> { }

    public class KitchenManager : MonoBehaviour
    {
        public static KitchenManager Instance { get; private set; }

        [SerializeField] private int requiredIngredients = 2;

        [Header("Eventos (conectables desde el Inspector)")]
        public IntIntUnityEvent OnIngredientCountChanged = new IntIntUnityEvent();
        public UnityEvent OnAllIngredientsCollected = new UnityEvent();

        public int CollectedCount { get; private set; }
        public int RequiredIngredients => requiredIngredients;
        public bool AllIngredientsCollected => CollectedCount >= requiredIngredients;

        public bool IsDoughReady { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetDoughReady()
        {
            IsDoughReady = true;
        }

        public void ReportIngredientCollected()
        {
            if (AllIngredientsCollected) return;

            CollectedCount++;
            OnIngredientCountChanged.Invoke(CollectedCount, requiredIngredients);

            if (AllIngredientsCollected)
            {
                OnAllIngredientsCollected.Invoke();
            }
        }
    }
}
