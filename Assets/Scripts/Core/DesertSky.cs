using BreakingPizza.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace BreakingPizza.Core
{
    // Sol y cielo del desierto.
    public static class DesertSky
    {
        private const float SunElevation = 32f;
        private const float SunAzimuthOffset = 30f;

        public static void Apply()
        {
            var player = Object.FindAnyObjectByType<FirstPersonController>();

            Light sun = FindOrCreateSun();

            // Dirección: el sol está en el azimut "mirada del jugador + 30°".
            // La luz apunta en sentido contrario (hacia el suelo).
            float playerYaw = player != null ? player.transform.eulerAngles.y : sun.transform.eulerAngles.y;
            sun.transform.rotation = Quaternion.Euler(SunElevation, playerYaw + SunAzimuthOffset + 180f, 0f);
            sun.color = new Color(1f, 0.93f, 0.78f);
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;

            // Skybox procedural. Se guarda la luz ambiente actual antes de
            // cambiarlo para que el interior no cambie de brillo.
            var shader = Shader.Find("Skybox/Procedural");
            if (shader != null)
            {
                SphericalHarmonicsL2 ambient = RenderSettings.ambientProbe;

                var sky = new Material(shader) { name = "DesertSky (runtime)" };
                sky.SetFloat("_SunDisk", 2f);
                sky.EnableKeyword("_SUNDISK_HIGH_QUALITY");
                sky.SetFloat("_SunSize", 0.06f);
                sky.SetFloat("_SunSizeConvergence", 4f);
                sky.SetFloat("_AtmosphereThickness", 1.15f);
                sky.SetFloat("_Exposure", 1.25f);
                sky.SetColor("_SkyTint", new Color(0.55f, 0.58f, 0.68f));
                sky.SetColor("_GroundColor", new Color(0.55f, 0.45f, 0.34f));
                RenderSettings.skybox = sky;

                RenderSettings.ambientMode = AmbientMode.Custom;
                RenderSettings.ambientProbe = ambient;
            }

            // Las cámaras del jugador dibujan el cielo en vez del color plano.
            if (player != null)
            {
                foreach (var cam in player.GetComponentsInChildren<Camera>(true))
                {
                    cam.clearFlags = CameraClearFlags.Skybox;
                }
            }
            if (Camera.main != null) Camera.main.clearFlags = CameraClearFlags.Skybox;
        }

        private static Light FindOrCreateSun()
        {
            if (RenderSettings.sun != null) return RenderSettings.sun;

            foreach (var l in Object.FindObjectsByType<Light>())
            {
                if (l.type == LightType.Directional && l.enabled) return l;
            }

            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            return light;
        }
    }
}
