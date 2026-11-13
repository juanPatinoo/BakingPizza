using BreakingPizza.Audio;
using BreakingPizza.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BreakingPizza.Core
{
    // Crea el HUD, el audio y los sistemas del juego al cargar la escena.
    public static class KitchenBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Object.FindAnyObjectByType<GameManager>() == null) return;

            // Sol y cielo del desierto (ver DesertSky.cs).
            DesertSky.Apply();

            // Techo y paredes invisibles: que no se pueda salir del laboratorio trepando.
            LabCage.Build();

            if (Object.FindAnyObjectByType<PeriodicChecklistUI>() != null) return;

            PizzaScore.Reset();

            var go = new GameObject("[BreakingPizza HUD]");
            go.AddComponent<PeriodicChecklistUI>();
            go.AddComponent<ResultScreenUI>();
            go.AddComponent<AudioDirector>();
            go.AddComponent<PromptStyler>();
            go.AddComponent<Crosshair>();
            go.AddComponent<PauseMenu>();
            go.AddComponent<ObjectiveUI>();
            go.AddComponent<MexicoFilter>();
            go.AddComponent<BreakingPizza.Player.FirstPersonArms>();
            go.AddComponent<BreakingPizza.Interactables.EasterEggs>();
        }
    }
}
