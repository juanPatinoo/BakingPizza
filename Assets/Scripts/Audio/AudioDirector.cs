using BreakingPizza.Core;
using BreakingPizza.Minigames;
using UnityEngine;

namespace BreakingPizza.Audio
{
    // Elige la música y el ambiente según la etapa del juego.
    public class AudioDirector : MonoBehaviour
    {
        [Range(0f, 0.5f)] [SerializeField] private float desertVolume = 0.16f;
        [Range(0f, 0.5f)] [SerializeField] private float labVolume = 0.14f;
        [Range(0f, 1f)] [SerializeField] private float twistVolume = 0.5f;
        [Range(0f, 0.5f)] [SerializeField] private float cookingVolume = 0.16f;
        [Range(0f, 1f)] [SerializeField] private float windVolume = 0.3f;

        private bool inDesert;
        private float wind;

        private void OnEnable()
        {
            GameManager.OnStageChanged += HandleStageChanged;
        }

        private void OnDisable()
        {
            GameManager.OnStageChanged -= HandleStageChanged;
        }

        private void HandleStageChanged(GameStage stage)
        {
            inDesert = stage == GameStage.Desert;

            switch (stage)
            {
                case GameStage.Desert:
                    Music.Play(MusicTrack.Desert, desertVolume, 4f);
                    Music.Preload(MusicTrack.Lab);
                    break;
                case GameStage.Dressing:
                    Music.Play(MusicTrack.Lab, labVolume, 3f);
                    Music.Preload(MusicTrack.Twist);
                    break;
                case GameStage.Twist:
                    Music.Play(MusicTrack.Twist, twistVolume, 1.2f);
                    Music.Preload(MusicTrack.Cooking);
                    break;
                case GameStage.Kitchen:
                    Music.Play(MusicTrack.Cooking, cookingVolume, 2.5f);
                    Music.Preload(MusicTrack.Credits);
                    break;
                case GameStage.Result:
                    Music.Stop(2.5f);
                    break;
                default:
                    Music.Stop(1f);
                    break;
            }
        }

        private void Update()
        {
            Music.SetDucked(MinigameHost.IsRunning);

            // Viento ambiente: solo en el desierto, con fade al entrar a la RV.
            wind = Mathf.MoveTowards(wind, inDesert ? windVolume : 0f, windVolume * Time.deltaTime / 3f);
            Sfx.SetLoop(LoopId.Wind, wind > 0.001f, wind);
        }
    }
}
