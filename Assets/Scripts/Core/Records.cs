using UnityEngine;

namespace BreakingPizza.Core
{
    // Récords de pureza y tiempo por dificultad.
    public static class Records
    {
        private static string Key(DifficultyLevel level, string field) => $"bp_record_{(int)level}_{field}";

        public static bool TryGet(DifficultyLevel level, out float purity, out float seconds)
        {
            purity = PlayerPrefs.GetFloat(Key(level, "purity"), -1f);
            seconds = PlayerPrefs.GetFloat(Key(level, "time"), 0f);
            return purity >= 0f;
        }

        // Guarda la partida si supera al récord. Devuelve true si es récord.
        public static bool Submit(DifficultyLevel level, float purity, float seconds)
        {
            bool better;
            if (!TryGet(level, out float bestPurity, out float bestTime))
            {
                better = true;
            }
            else
            {
                better = purity > bestPurity + 0.01f || (Mathf.Abs(purity - bestPurity) <= 0.01f && seconds < bestTime);
            }

            if (better)
            {
                PlayerPrefs.SetFloat(Key(level, "purity"), purity);
                PlayerPrefs.SetFloat(Key(level, "time"), seconds);
                PlayerPrefs.Save();
            }
            return better;
        }
    }
}
