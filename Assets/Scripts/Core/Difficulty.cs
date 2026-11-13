using UnityEngine;

namespace BreakingPizza.Core
{
    // Niveles de dificultad: pureza mínima y ritmo de los minijuegos.
    public enum DifficultyLevel
    {
        Facil,
        Medio,
        Dificil,
        Imposible
    }

    public static class Difficulty
    {
        private const string PrefKey = "bp_difficulty";

        public static DifficultyLevel Current
        {
            get
            {
                int v = PlayerPrefs.GetInt(PrefKey, (int)DifficultyLevel.Medio);
                return System.Enum.IsDefined(typeof(DifficultyLevel), v) ? (DifficultyLevel)v : DifficultyLevel.Medio;
            }
            set
            {
                PlayerPrefs.SetInt(PrefKey, (int)value);
                PlayerPrefs.Save();
            }
        }

        public static float RequiredPurity => RequiredFor(Current);

        // Multiplicador de velocidad de los minijuegos.
        public static float Speed => SpeedFor(Current);

        public static float SpeedFor(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.Facil: return 0.85f;
                case DifficultyLevel.Dificil: return 1.15f;
                case DifficultyLevel.Imposible: return 1.25f;
                default: return 1f;
            }
        }

        // Multiplicador del tiempo límite de los minijuegos con reloj.
        public static float TimeFactor => TimeFactorFor(Current);

        public static float TimeFactorFor(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.Facil: return 1.25f;
                case DifficultyLevel.Dificil: return 0.9f;
                case DifficultyLevel.Imposible: return 0.8f;
                default: return 1f;
            }
        }

        public static float RequiredFor(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.Facil: return 40f;
                case DifficultyLevel.Dificil: return 80f;
                case DifficultyLevel.Imposible: return 97f;
                default: return 60f;
            }
        }

        public static string NameOf(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.Facil: return "FÁCIL";
                case DifficultyLevel.Dificil: return "DIFÍCIL";
                case DifficultyLevel.Imposible: return "IMPOSIBLE";
                default: return "MEDIO";
            }
        }

        public static string BlurbOf(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.Facil: return "Minijuegos más lentos y con más tiempo. Para aprender la receta.";
                case DifficultyLevel.Dificil: return "Todo va más rápido y con menos tiempo. Cada error se paga.";
                case DifficultyLevel.Imposible: return "Ritmo endiablado y pureza azul. La de Heisenberg.";
                default: return "El estándar de la calle. Ni muy fácil ni muy cruel.";
            }
        }

        public static Color ColorOf(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.Facil: return new Color(0.24f, 0.86f, 0.43f);
                case DifficultyLevel.Dificil: return new Color(1f, 0.55f, 0.15f);
                case DifficultyLevel.Imposible: return new Color(0.36f, 0.82f, 1f);
                default: return new Color(0.96f, 0.77f, 0.19f);
            }
        }

        public static string Name => NameOf(Current);
    }
}
