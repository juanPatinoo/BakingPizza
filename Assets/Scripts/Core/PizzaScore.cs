using System;
using System.Collections.Generic;
using UnityEngine;

namespace BreakingPizza.Core
{
    // Pasos de la receta, pureza y cronómetro de la cocina.
    public enum PizzaStep
    {
        Masa,
        Salsa,
        Queso,
        Pepperoni,
        Horno
    }

    public static class PizzaScore
    {
        public static float PassPurity => Difficulty.RequiredPurity;

        public static readonly PizzaStep[] AllSteps = (PizzaStep[])Enum.GetValues(typeof(PizzaStep));

        private static readonly Dictionary<PizzaStep, float> scores = new Dictionary<PizzaStep, float>();

        public static event Action<PizzaStep, float> OnStepCompleted;
        public static event Action OnReset;

        // "Foto" de la pizza para la pantalla final.
        public static readonly List<Vector2> PepperoniPositions = new List<Vector2>(); // normalizadas al radio de la salsa (-1..1)
        public static float BakeAmount;  // 0 = cruda, 1 = dorada
        public static float BurnAmount;  // 0 = nada, 1 = carbón

        public static float ElapsedSeconds { get; private set; }
        public static bool AllDone => scores.Count >= AllSteps.Length;

        public static void Tick(float dt)
        {
            if (!AllDone) ElapsedSeconds += dt;
        }

        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            scores.Clear();
            PepperoniPositions.Clear();
            BakeAmount = 0f;
            BurnAmount = 0f;
            ElapsedSeconds = 0f;
            OnStepCompleted = null;
            OnReset = null;
        }

        public static void Reset()
        {
            scores.Clear();
            PepperoniPositions.Clear();
            BakeAmount = 0f;
            BurnAmount = 0f;
            ElapsedSeconds = 0f;
            OnReset?.Invoke();
        }

        public static void Record(PizzaStep step, float score)
        {
            score = Mathf.Clamp01(score);
            scores[step] = score;
            OnStepCompleted?.Invoke(step, score);
        }

        public static bool IsDone(PizzaStep step) => scores.ContainsKey(step);

        public static bool TryGet(PizzaStep step, out float score) => scores.TryGetValue(step, out score);

        public static int CompletedCount => scores.Count;

        // Pureza "en vivo": promedio solo de los pasos ya hechos.
        public static float CurrentPurity
        {
            get
            {
                if (scores.Count == 0) return 0f;
                float sum = 0f;
                foreach (float v in scores.Values) sum += v;
                return ToPurity(sum / scores.Count);
            }
        }

        // Pureza final: los pasos que no se hicieron cuentan como 0.
        public static float FinalPurity
        {
            get
            {
                float sum = 0f;
                foreach (var s in AllSteps)
                {
                    if (scores.TryGetValue(s, out float v)) sum += v;
                }
                return ToPurity(sum / AllSteps.Length);
            }
        }

        // Pureza = promedio de los pasos como porcentaje (tope 99,9: la
        // perfección absoluta no existe, como diría Gus).
        public static float ToPurity(float average01) => Mathf.Min(99.9f, Mathf.Clamp01(average01) * 100f);

        public static bool Passed => FinalPurity >= PassPurity;

        public static string Symbol(PizzaStep step)
        {
            switch (step)
            {
                case PizzaStep.Masa: return "Ma";
                case PizzaStep.Salsa: return "To";
                case PizzaStep.Queso: return "Qu";
                case PizzaStep.Pepperoni: return "Pp";
                case PizzaStep.Horno: return "Ho";
                default: return "?";
            }
        }

        public static string DisplayName(PizzaStep step)
        {
            switch (step)
            {
                case PizzaStep.Masa: return "Masa";
                case PizzaStep.Salsa: return "Tomate";
                case PizzaStep.Queso: return "Queso";
                case PizzaStep.Pepperoni: return "Pepperoni";
                case PizzaStep.Horno: return "Horno";
                default: return step.ToString();
            }
        }

        public static int Number(PizzaStep step) => (int)step + 1;

        // Calificación de un paso individual (para el sello al terminar
        // cada minijuego).
        public static string StepRating(float score)
        {
            if (score >= 0.92f) return "¡PERFECTO!";
            if (score >= 0.75f) return "¡MUY BIEN!";
            if (score >= 0.5f) return "ZAFA";
            if (score >= 0.25f) return "FLOJO...";
            return "DESASTRE";
        }

        public static Color StepRatingColor(float score)
        {
            if (score >= 0.92f) return new Color(0.36f, 0.82f, 1f);
            if (score >= 0.75f) return new Color(0.49f, 0.89f, 0.55f);
            if (score >= 0.5f) return new Color(0.96f, 0.77f, 0.19f);
            return new Color(1f, 0.3f, 0.3f);
        }
    }
}
