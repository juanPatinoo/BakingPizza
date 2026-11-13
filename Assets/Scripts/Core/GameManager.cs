using System;
using UnityEngine;
using UnityEngine.Events;

namespace BreakingPizza.Core
{
    // Etapas del juego y avisos de cambio de etapa.
    public enum GameStage
    {
        Desert,
        Dressing,
        Twist,
        Kitchen,
        Result
    }

    [Serializable]
    public class GameStageUnityEvent : UnityEvent<GameStage> { }

    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        private static bool quitting;
        private bool autoCreated;

        public static GameManager Instance
        {
            get
            {
                if (instance == null && Application.isPlaying && !quitting)
                {
                    instance = FindAnyObjectByType<GameManager>();

                    if (instance == null)
                    {
                        var go = new GameObject("GameManager (auto)");
                        var created = go.AddComponent<GameManager>();
                        created.autoCreated = true;
                        instance = created;
                        Debug.LogWarning("[GameManager] No había ninguno en la escena: se creó uno automáticamente.");
                    }
                }
                return instance;
            }
            private set { instance = value; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            quitting = false;
            Application.quitting -= MarkQuitting;
            Application.quitting += MarkQuitting;
        }

        private static void MarkQuitting() => quitting = true;

        [Tooltip("Etapa en la que arranca el juego al cargar la escena.")]
        [SerializeField] private GameStage startingStage = GameStage.Desert;

        [Header("Eventos (conectables desde el Inspector)")]
        [Tooltip("Se dispara cada vez que cambia la etapa. Ideal para conectar paneles de UI, activar/desactivar zonas, etc. sin escribir código nuevo.")]
        public GameStageUnityEvent OnStageChangedUnityEvent = new GameStageUnityEvent();

        public static event Action<GameStage> OnStageChanged;

        public GameStage CurrentStage { get; private set; }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning($"[GameManager] Ya había otro GameManager ({instance.name}); se destruye este duplicado ({name}).", this);
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void Start()
        {
            // Un GameManager creado de apuro en medio de la partida NO debe
            // pisar la etapa actual con la inicial.
            if (autoCreated) return;
            SetStage(startingStage);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void SetStage(GameStage newStage)
        {
            CurrentStage = newStage;
            OnStageChanged?.Invoke(newStage);
            OnStageChangedUnityEvent?.Invoke(newStage);
        }

        public void AdvanceToNextStage()
        {
            int next = (int)CurrentStage + 1;
            if (Enum.IsDefined(typeof(GameStage), next))
            {
                SetStage((GameStage)next);
            }
        }
    }
}
