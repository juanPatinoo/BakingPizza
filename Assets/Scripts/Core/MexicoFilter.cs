using BreakingPizza.Player;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BreakingPizza.Core
{
    // Filtro de color amarillento del desierto (post-procesado).
    public class MexicoFilter : MonoBehaviour
    {
        private const float FadeSpeed = 1.2f; // peso por segundo

        private Volume volume;
        private float target;

        private void Awake()
        {
            var go = new GameObject("[MexicoFilter Volume]");
            go.transform.SetParent(transform, false);
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 0f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "MexicoFilter (runtime)";
            volume.sharedProfile = profile;

            var white = profile.Add<WhiteBalance>();
            white.temperature.Override(38f);
            white.tint.Override(6f);

            var color = profile.Add<ColorAdjustments>();
            color.colorFilter.Override(new Color(1f, 0.87f, 0.62f));
            color.contrast.Override(18f);
            color.saturation.Override(10f);
            color.postExposure.Override(0.1f);

            var split = profile.Add<SplitToning>();
            split.shadows.Override(new Color(0.55f, 0.38f, 0.2f));
            split.highlights.Override(new Color(1f, 0.86f, 0.5f));
            split.balance.Override(-15f);

            var vignette = profile.Add<Vignette>();
            vignette.color.Override(new Color(0.22f, 0.12f, 0.04f));
            vignette.intensity.Override(0.32f);
            vignette.smoothness.Override(0.45f);

            var grain = profile.Add<FilmGrain>();
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(0.28f);
            grain.response.Override(0.75f);
        }

        private void Start()
        {
            // Post-procesado prendido en las cámaras del jugador.
            var player = FindAnyObjectByType<FirstPersonController>();
            if (player != null)
            {
                foreach (var cam in player.GetComponentsInChildren<Camera>(true))
                {
                    cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                }
            }
            if (Camera.main != null) Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }

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
            target = stage == GameStage.Desert ? 1f : 0f;
        }

        private void Update()
        {
            volume.weight = Mathf.MoveTowards(volume.weight, target, FadeSpeed * Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            if (volume != null && volume.sharedProfile != null) Destroy(volume.sharedProfile);
        }
    }
}
