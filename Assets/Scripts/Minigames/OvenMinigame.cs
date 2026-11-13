using System.Collections;
using System.Collections.Generic;
using BreakingPizza.Audio;
using BreakingPizza.Core;
using BreakingPizza.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BreakingPizza.Minigames
{
    // Minijuego 5: horneado.
    public class OvenMinigame : Minigame
    {
        private static float TimeLimit => 20f * Difficulty.TimeFactor;
        private const float GaugeHeight = 420f;

        public override PizzaStep Step => PizzaStep.Horno;
        public override string Title => "Horneado controlado";
        public override string Subtitle => "REACCIÓN TÉRMICA";
        public override string Instructions => "<b>Mantené</b> click o Espacio para subir el fuego, soltá para bajarlo. Mantené la aguja en la zona verde.";
        public override string Goal => "Horneá la pizza sin quemarla controlando la temperatura.";
        public override string[] Rules => new[]
        {
            "<b>Mantené apretado</b> para subir el fuego y <b>soltá</b> para bajarlo. Tiene inercia, no responde al instante.",
            "Mantené la aguja blanca adentro de la <color=#3DDC6E><b>zona verde</b></color>: ahí se cocina la pizza.",
            "La zona verde <b>se va moviendo</b> sola, tenés que seguirla.",
            "Si te pasás de caliente se cocina lento y <color=#FF4D4D><b>SE QUEMA</b></color>. Llegá al 100 % de cocción antes de que se acabe el tiempo.",
        };
        public override string[] Keys => new[] { "CLICK (mantener)", "ESPACIO" };
        public override string[] GamepadKeys => new[] { "X (mantener)", "R2" };
        public override string Tip => "Usá toques cortos para no pasarte: soltar a tiempo es más importante que subir rápido.";

        private RectTransform pizza;
        private Image pizzaCrust, pizzaCheese;
        private readonly List<Image> burnSpots = new List<Image>();
        private Image ovenGlow, topElement, bottomElement;
        private Image zone, marker;
        private TextMeshProUGUI tempText, warnText, timeText;
        private Image cookFill, burnFill;
        private RectTransform smokeLayer;
        private readonly List<Image> flames = new List<Image>();

        public override void Build()
        {
            // ── Horno ──
            Vector2 ovenPos = new Vector2(-150f, 20f);
            UIKit.Box("OvenBody", Area, new Color(0.16f, 0.17f, 0.18f), new Vector2(640f, 440f), ovenPos);
            UIKit.Box("OvenWindow", Area, new Color(0.06f, 0.035f, 0.02f), new Vector2(580f, 380f), ovenPos);
            ovenGlow = UIKit.Image("Glow", Area, UIKit.Soft, new Color(1f, 0.45f, 0.1f, 0.2f), new Vector2(620f, 420f), ovenPos);
            topElement = UIKit.Box("TopElement", Area, new Color(0.35f, 0.1f, 0.05f), new Vector2(520f, 10f), ovenPos + new Vector2(0f, 168f));
            bottomElement = UIKit.Box("BottomElement", Area, new Color(0.35f, 0.1f, 0.05f), new Vector2(520f, 10f), ovenPos + new Vector2(0f, -168f));

            pizza = UIKit.Rect("Pizza", Area, new Vector2(300f, 300f), ovenPos);
            pizzaCrust = UIKit.Dot("Crust", pizza, Palette.DoughRaw, 300f, Vector2.zero);
            UIKit.Dot("Sauce", pizza, Palette.Tomato, 266f, Vector2.zero);
            pizzaCheese = UIKit.Dot("Cheese", pizza, Palette.CheesePale, 250f, Vector2.zero);
            foreach (var p in PizzaScore.PepperoniPositions)
            {
                PepperoniMinigame.BuildSlice(pizza, p * 130f, 1f, 40f);
            }
            var rng = new System.Random(11);
            for (int i = 0; i < 14; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = Mathf.Sqrt((float)rng.NextDouble()) * 135f;
                var spot = UIKit.Dot("Burn", pizza, Palette.Charred.WithAlpha(0f), 18f + (float)rng.NextDouble() * 26f, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                burnSpots.Add(spot);
            }
            // Reflejo del vidrio por delante.
            UIKit.Box("GlassShine", Area, new Color(1f, 1f, 1f, 0.05f), new Vector2(560f, 120f), ovenPos + new Vector2(0f, 110f));
            smokeLayer = UIKit.Rect("Smoke", Area, new Vector2(600f, 400f), ovenPos);

            // ── Barras de cocción / quemado ──
            UIKit.Text("CookLabel", Area, "COCCIÓN", 18f, Palette.TextDim, new Vector2(200f, 24f), new Vector2(-370f, -230f), TextAlignmentOptions.Left, true);
            cookFill = MakeBar(Area, new Vector2(460f, 22f), new Vector2(-90f, -230f), Palette.CrustBaked * 1.3f, out _);
            UIKit.Text("BurnLabel", Area, "QUEMADO", 18f, Palette.TextDim, new Vector2(200f, 24f), new Vector2(-370f, -258f), TextAlignmentOptions.Left, true);
            burnFill = MakeBar(Area, new Vector2(460f, 12f), new Vector2(-90f, -258f), Palette.Danger, out _);

            // ── Termómetro ──
            Vector2 gaugePos = new Vector2(330f, 10f);
            UIKit.Box("GaugeFrame", Area, new Color(0f, 0f, 0f, 0.6f), new Vector2(96f, GaugeHeight + 20f), gaugePos);
            var track = UIKit.Box("GaugeTrack", Area, Palette.PanelLight, new Vector2(76f, GaugeHeight), gaugePos);
            // Degradé frío->caliente con franjas.
            for (int i = 0; i < 12; i++)
            {
                float u = i / 11f;
                UIKit.Box("Heat", track.transform, Color.Lerp(new Color(0.2f, 0.4f, 0.9f, 0.25f), new Color(1f, 0.2f, 0.1f, 0.35f), u),
                    new Vector2(70f, GaugeHeight / 12f - 2f), new Vector2(0f, -GaugeHeight / 2f + (i + 0.5f) * GaugeHeight / 12f));
            }
            zone = UIKit.Box("Zone", track.transform, Palette.BBGreenBright.WithAlpha(0.65f), new Vector2(76f, 80f), Vector2.zero);
            marker = UIKit.Box("Marker", track.transform, Color.white, new Vector2(104f, 12f), Vector2.zero);
            tempText = UIKit.Text("Temp", Area, "0°C", 28f, Color.white, new Vector2(140f, 40f), gaugePos + new Vector2(118f, 0f), TextAlignmentOptions.Left, true);

            // Llamas debajo del termómetro.
            for (int i = 0; i < 3; i++)
            {
                flames.Add(UIKit.Image("Flame", Area, UIKit.Soft, new Color(1f, 0.55f, 0.1f, 0.8f), new Vector2(46f, 64f), gaugePos + new Vector2((i - 1) * 28f, -GaugeHeight / 2f - 40f)));
            }

            warnText = UIKit.Text("Warn", Area, string.Empty, 40f, Palette.Danger, new Vector2(600f, 60f), ovenPos + new Vector2(0f, 250f), TextAlignmentOptions.Center, true);
            timeText = UIKit.Text("Time", Area, string.Empty, 26f, Palette.MethBlue, new Vector2(200f, 34f), new Vector2(470f, -250f), TextAlignmentOptions.Right, true);
        }

        public override IEnumerator Play()
        {
            float temp = 0.2f;
            float vel = 0f;
            float cook = 0f;
            float burn = 0f;
            float inZoneTime = 0f;
            float elapsed = 0f;
            float seed = Random.Range(0f, 100f);
            float smokeTimer = 0f;
            bool wasInZone = false;

            Sfx.SetLoop(LoopId.Sizzle, true, 0.2f);

            while (elapsed < TimeLimit && cook < 1f)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                // ── Física de la temperatura ──
                vel += (ActionHeld ? 2.3f : -1.7f) * dt;
                vel = Mathf.Clamp(vel, -1.1f, 1.1f);
                temp += vel * dt;
                if (temp <= 0f) { temp = 0f; vel = Mathf.Max(0f, vel); }
                if (temp >= 1f) { temp = 1f; vel = Mathf.Min(0f, vel) * 0.3f; }

                // ── Zona ideal que deriva ──
                float zoneH = Mathf.Lerp(0.24f, 0.16f, cook) / Difficulty.Speed;
                float c = Mathf.Lerp(0.22f, 0.8f, Mathf.PerlinNoise(elapsed * 0.32f * Difficulty.Speed, seed));
                bool inZone = Mathf.Abs(temp - c) <= zoneH / 2f;
                bool tooHot = temp > c + zoneH / 2f;

                if (inZone)
                {
                    cook += 0.125f * dt;
                    inZoneTime += dt;
                }
                else if (tooHot)
                {
                    float over = temp - (c + zoneH / 2f);
                    cook += 0.04f * dt;
                    burn += (0.12f + over * 0.9f) * dt;
                }

                if (inZone && !wasInZone) Sfx.Play(SfxId.Count, 0.6f);
                wasInZone = inZone;

                // ── Visual ──
                zone.rectTransform.sizeDelta = new Vector2(76f, zoneH * GaugeHeight);
                zone.rectTransform.anchoredPosition = new Vector2(0f, (c - 0.5f) * GaugeHeight);
                zone.color = (inZone ? Palette.BBGreenBright : Palette.BBGreen).WithAlpha(inZone ? 0.85f : 0.55f);
                marker.rectTransform.anchoredPosition = new Vector2(0f, (temp - 0.5f) * GaugeHeight);
                marker.color = inZone ? Color.white : tooHot ? Palette.Danger : Palette.MethBlue;
                tempText.rectTransform.anchoredPosition = new Vector2(448f, 10f + (temp - 0.5f) * GaugeHeight);
                tempText.text = $"{Mathf.Lerp(120f, 520f, temp):0}°C";
                tempText.color = marker.color;

                float heat = temp;
                ovenGlow.color = new Color(1f, 0.45f, 0.1f, 0.12f + heat * 0.45f);
                Color element = Color.Lerp(new Color(0.35f, 0.1f, 0.05f), new Color(1f, 0.55f, 0.15f), heat);
                topElement.color = element;
                bottomElement.color = element;
                for (int i = 0; i < flames.Count; i++)
                {
                    float flick = 0.75f + 0.25f * Mathf.Sin(Time.time * (17f + i * 5f) + i);
                    flames[i].rectTransform.sizeDelta = new Vector2(46f, Mathf.Lerp(18f, 90f, heat) * flick);
                    flames[i].color = Color.Lerp(new Color(0.3f, 0.5f, 1f, 0.8f), new Color(1f, 0.5f, 0.1f, 0.9f), heat);
                }

                UpdatePizzaLook(cook, burn);
                SetBar(cookFill, 454f, cook);
                SetBar(burnFill, 454f, Mathf.Clamp01(burn));
                timeText.text = $"{Mathf.Max(0f, TimeLimit - elapsed):0.0}s";

                warnText.text = tooHot ? "¡SE QUEMA!" : (!inZone && temp < c ? "MÁS FUEGO" : string.Empty);
                warnText.color = tooHot ? Palette.Danger : Palette.MethBlue;
                warnText.alpha = tooHot ? 0.6f + 0.4f * Mathf.PingPong(Time.time * 6f, 1f) : 0.8f;

                Sfx.SetLoop(LoopId.Sizzle, true, 0.15f + heat * 0.7f, 0.85f + heat * 0.3f);

                if (tooHot)
                {
                    smokeTimer -= dt;
                    if (smokeTimer <= 0f)
                    {
                        smokeTimer = 0.12f;
                        Run(Smoke());
                    }
                }

                yield return null;
            }

            Sfx.SetLoop(LoopId.Sizzle, false);
            warnText.text = string.Empty;
            cook = Mathf.Clamp01(cook);
            burn = Mathf.Clamp01(burn);

            float inZoneRatio = elapsed > 0f ? inZoneTime / elapsed : 0f;
            Score = Mathf.Clamp01(cook * 0.6f + inZoneRatio * 0.4f - burn * 0.5f);

            PizzaScore.BakeAmount = cook;
            PizzaScore.BurnAmount = burn;

            if (cook >= 1f)
            {
                Sfx.Play(SfxId.Complete);
                Popup(burn > 0.5f ? "LISTA... Y CHAMUSCADA" : "¡PIZZA LISTA!", burn > 0.5f ? Palette.Hazmat : Palette.BBGreenBright, new Vector2(-150f, 20f), 48f);
                UIKit.Burst(Host, Area, new Vector2(-150f, 20f), Palette.Hazmat, 30, 600f, 10f, -400f, 0.9f, Color.white);
            }
            else
            {
                Popup("¡QUEDÓ CRUDA!", Palette.Danger, new Vector2(-150f, 20f), 48f);
            }
            yield return new WaitForSeconds(0.5f);
        }

        public override void Cleanup()
        {
            Sfx.SetLoop(LoopId.Sizzle, false);
        }

        private void UpdatePizzaLook(float cook, float burn)
        {
            Color crust = Color.Lerp(Palette.DoughRaw, Palette.CrustBaked, cook);
            crust = Color.Lerp(crust, Palette.Charred, Mathf.Clamp01(burn) * 0.8f);
            pizzaCrust.color = crust;

            Color cheese = Color.Lerp(Palette.CheesePale, Palette.Cheese * 0.95f, cook);
            cheese = Color.Lerp(cheese, Palette.CrustBaked, Mathf.Clamp01(burn) * 0.6f);
            pizzaCheese.color = cheese.WithAlpha(0.95f);

            for (int i = 0; i < burnSpots.Count; i++)
            {
                float threshold = i / (float)burnSpots.Count;
                float a = Mathf.Clamp01((burn - threshold * 0.8f) * 3f);
                burnSpots[i].color = Palette.Charred.WithAlpha(a * 0.85f);
            }
        }

        private IEnumerator Smoke()
        {
            var puff = UIKit.Image("Puff", smokeLayer, UIKit.Soft, new Color(0.5f, 0.5f, 0.5f, 0.5f), new Vector2(60f, 60f),
                new Vector2(Random.Range(-120f, 120f), Random.Range(-60f, 40f)));
            Vector2 start = puff.rectTransform.anchoredPosition;
            yield return Tween.Run(1.1f, u =>
            {
                if (puff == null) return;
                puff.rectTransform.anchoredPosition = start + new Vector2(Mathf.Sin(u * 6f) * 12f, u * 170f);
                puff.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(60f, 150f, u);
                puff.color = new Color(0.45f, 0.45f, 0.45f, 0.5f * (1f - u));
            });
            if (puff != null) Object.Destroy(puff.gameObject);
        }
    }
}
