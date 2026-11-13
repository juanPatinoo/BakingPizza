using System.Collections;
using BreakingPizza.Audio;
using BreakingPizza.Core;
using BreakingPizza.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BreakingPizza.Minigames
{
    // Minijuego 2: dosificación de salsa.
    public class SauceMinigame : Minigame
    {
        private const int Rounds = 3;
        private const float CapacityMl = 500f;
        private const float InnerHeight = 340f;
        private const float InnerWidth = 112f;

        public override PizzaStep Step => PizzaStep.Salsa;
        public override string Title => "Dosificación de salsa";
        public override string Subtitle => "MEDICIÓN DE REACTIVOS";
        public override string Instructions => "<b>Mantené</b> click o Espacio para verter. Soltá justo en la línea amarilla: al soltar queda medido.";
        public override string Goal => "Medí la salsa de tomate con precisión de químico.";
        public override string[] Rules => new[]
        {
            "Son <b>3 probetas</b>. Cada una tiene una <color=#F4C430><b>línea amarilla</b></color> con los ml exactos que hay que llegar.",
            "<b>Mantené apretado</b> para verter. Cuanto más mantenés, más rápido sale el chorro.",
            "En cuanto <b>soltás</b>, el chorro se corta y esa probeta queda medida: no podés seguir llenándola.",
            "Si llegás al borde de la probeta <color=#FF4D4D><b>se rebalsa</b></color> y esa medida vale 0.",
        };
        public override string[] Keys => new[] { "CLICK (mantener)", "ESPACIO" };
        public override string[] GamepadKeys => new[] { "X (mantener)", "R2" };
        public override string Tip => "El chorro se acelera: soltá justo cuando llegues a la línea amarilla.";

        private class Cylinder
        {
            public RectTransform Root;
            public Image Glass;
            public Image Fill;
            public Image Target;
            public Image Band;
            public TextMeshProUGUI TargetLabel;
            public TextMeshProUGUI Reading;
            public float TargetLevel;
        }

        private Cylinder[] cylinders;
        private RectTransform bottle;
        private Image stream;

        public override void Build()
        {
            cylinders = new Cylinder[Rounds];
            for (int i = 0; i < Rounds; i++)
            {
                cylinders[i] = BuildCylinder(new Vector2((i - 1) * 300f, -40f), i);
            }

            // Botella de salsa que se mueve arriba de la probeta activa.
            bottle = UIKit.Rect("Bottle", Area, new Vector2(110f, 120f), new Vector2(-300f, 215f));
            UIKit.Box("BottleBody", bottle, Palette.TomatoDark, new Vector2(96f, 92f), new Vector2(0f, 10f));
            UIKit.Box("BottleLabel", bottle, Palette.CheesePale, new Vector2(80f, 34f), new Vector2(0f, 12f));
            UIKit.Text("BottleText", bottle, "SALSA", 18f, Palette.TomatoDark, new Vector2(80f, 30f), new Vector2(0f, 12f), TextAlignmentOptions.Center, true);
            UIKit.Box("Spout", bottle, Palette.TomatoDark, new Vector2(30f, 34f), new Vector2(0f, -48f));

            stream = UIKit.Box("Stream", Area, Palette.Tomato, new Vector2(14f, 10f), Vector2.zero);
            stream.rectTransform.pivot = new Vector2(0.5f, 1f);
            stream.enabled = false;
        }

        private Cylinder BuildCylinder(Vector2 pos, int index)
        {
            var c = new Cylinder();
            c.Root = UIKit.Rect("Cylinder" + index, Area, new Vector2(180f, 400f), pos);

            // Vidrio: contorno claro + interior apenas visible.
            UIKit.Box("GlassEdge", c.Root, new Color(1f, 1f, 1f, 0.45f), new Vector2(InnerWidth + 14f, InnerHeight + 16f), Vector2.zero);
            c.Glass = UIKit.Box("Glass", c.Root, new Color(0.08f, 0.14f, 0.12f, 0.95f), new Vector2(InnerWidth, InnerHeight + 4f), Vector2.zero);
            UIKit.Box("Base", c.Root, new Color(1f, 1f, 1f, 0.5f), new Vector2(InnerWidth + 50f, 14f), new Vector2(0f, -InnerHeight / 2f - 12f));

            c.Fill = UIKit.Box("Fill", c.Root, Palette.Tomato, new Vector2(InnerWidth - 6f, 0f), new Vector2(0f, -InnerHeight / 2f));
            c.Fill.rectTransform.pivot = new Vector2(0.5f, 0f);
            c.Fill.enabled = false;

            // Graduación cada 50 ml.
            for (int ml = 50; ml < CapacityMl; ml += 50)
            {
                float y = -InnerHeight / 2f + ml / CapacityMl * InnerHeight;
                bool major = ml % 100 == 0;
                UIKit.Box("Tick", c.Root, new Color(1f, 1f, 1f, major ? 0.55f : 0.3f), new Vector2(major ? 26f : 14f, 3f), new Vector2(-InnerWidth / 2f + (major ? 13f : 7f), y));
                if (major)
                {
                    UIKit.Text("TickLabel", c.Root, ml.ToString(), 14f, new Color(1f, 1f, 1f, 0.55f), new Vector2(50f, 20f), new Vector2(-InnerWidth / 2f - 32f, y), TextAlignmentOptions.Right);
                }
            }

            // Brillo del vidrio.
            UIKit.Box("Shine", c.Root, new Color(1f, 1f, 1f, 0.12f), new Vector2(12f, InnerHeight - 30f), new Vector2(InnerWidth / 2f - 18f, 0f));

            c.Band = UIKit.Box("Band", c.Root, Palette.Hazmat.WithAlpha(0.18f), new Vector2(InnerWidth + 20f, 20f), Vector2.zero);
            c.Target = UIKit.Box("Target", c.Root, Palette.Hazmat, new Vector2(InnerWidth + 30f, 5f), Vector2.zero);
            c.TargetLabel = UIKit.Text("TargetLabel", c.Root, string.Empty, 20f, Palette.Hazmat, new Vector2(110f, 30f), new Vector2(InnerWidth / 2f + 62f, 0f), TextAlignmentOptions.Left, true);
            c.Band.enabled = c.Target.enabled = false;

            c.Reading = UIKit.Text("Reading", c.Root, "- ml", 24f, Palette.TextDim, new Vector2(180f, 34f), new Vector2(0f, -InnerHeight / 2f - 42f), TextAlignmentOptions.Center, true);
            return c;
        }

        public override IEnumerator Play()
        {
            float total = 0f;

            for (int round = 0; round < Rounds; round++)
            {
                var c = cylinders[round];

                // Objetivo al azar, redondeado a 25 ml.
                float targetMl = Mathf.Round(Random.Range(0.35f, 0.85f) * CapacityMl / 25f) * 25f;
                c.TargetLevel = targetMl / CapacityMl;
                float yTarget = -InnerHeight / 2f + c.TargetLevel * InnerHeight;
                c.Target.rectTransform.anchoredPosition = new Vector2(0f, yTarget);
                c.Band.rectTransform.anchoredPosition = new Vector2(0f, yTarget);
                c.TargetLabel.rectTransform.anchoredPosition = new Vector2(InnerWidth / 2f + 62f, yTarget);
                c.TargetLabel.text = $"{targetMl:0} ml";
                c.Band.enabled = c.Target.enabled = true;
                c.Reading.color = Color.white;
                c.Reading.text = "0 ml";

                // Botella se mueve a la probeta.
                Vector2 from = bottle.anchoredPosition;
                Vector2 to = new Vector2(c.Root.anchoredPosition.x, 215f);
                yield return Tween.Run(0.3f, u => bottle.anchoredPosition = Vector2.Lerp(from, to, Ease.OutCubic(u)));
                Run(Tween.Punch(c.Root, 0.06f, 0.25f));

                float maxFlow = (0.30f + round * 0.07f) * Difficulty.Speed; // fracción de la probeta por segundo
                float flow = 0f;
                float level = 0f;
                bool everPoured = false;
                bool overflow = false;

                while (true)
                {
                    bool held = ActionHeld;
                    if (everPoured && !held) break;

                    flow = held ? Mathf.MoveTowards(flow, maxFlow, 1.8f * maxFlow * Time.deltaTime) : 0f;
                    level += flow * Time.deltaTime;

                    if (held) everPoured = true;

                    bool pouring = flow > 0.005f;
                    Sfx.SetLoop(LoopId.Pour, pouring, Mathf.Clamp01(flow / maxFlow) * 0.9f, 0.9f + level * 0.3f);
                    bottle.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, -35f, flow / maxFlow));

                    UpdateFill(c, level);
                    UpdateStream(c, pouring, level, flow / maxFlow);

                    if (level >= 1f)
                    {
                        overflow = true;
                        break;
                    }

                    yield return null;
                }

                stream.enabled = false;
                Sfx.SetLoop(LoopId.Pour, false);
                bottle.localEulerAngles = Vector3.zero;

                float score;
                Vector2 pop = c.Root.anchoredPosition + new Vector2(0f, 120f);
                if (overflow)
                {
                    score = 0f;
                    UpdateFill(c, 1f);
                    c.Reading.text = "¡REBALSÓ!";
                    c.Reading.color = Palette.Danger;
                    Sfx.Play(SfxId.Splash);
                    Sfx.Play(SfxId.Miss);
                    UIKit.Burst(Host, Area, c.Root.anchoredPosition + new Vector2(0f, InnerHeight / 2f - 40f), Palette.Tomato, 40, 600f, 16f, -1200f, 1f, Palette.TomatoDark);
                    ShakeCard(18f, 0.35f);
                    Popup("¡SE REBALSÓ!", Palette.Danger, pop);
                }
                else
                {
                    float diffMl = (level - c.TargetLevel) * CapacityMl;
                    float err = Mathf.Abs(level - c.TargetLevel);
                    score = err <= 0.015f ? 1f : Mathf.Clamp01(1f - (err - 0.015f) / 0.12f);
                    string sign = diffMl >= 0f ? "+" : "-";
                    c.Reading.text = $"{level * CapacityMl:0} ml";
                    if (score >= 0.95f)
                    {
                        Sfx.Play(SfxId.Perfect);
                        Popup("¡EXACTO!", Palette.MethBlue, pop);
                        c.Reading.color = Palette.MethBlue;
                    }
                    else if (score >= 0.6f)
                    {
                        Sfx.Play(SfxId.Good);
                        Popup($"BIEN ({sign}{Mathf.Abs(diffMl):0} ml)", Palette.BBGreenBright, pop, 36f);
                        c.Reading.color = Palette.BBGreenBright;
                    }
                    else
                    {
                        Sfx.Play(SfxId.Miss);
                        Popup($"{sign}{Mathf.Abs(diffMl):0} ml", Palette.Danger, pop, 40f);
                        c.Reading.color = Palette.Danger;
                        ShakeCard(8f, 0.2f);
                    }
                }

                total += score;
                yield return new WaitForSeconds(0.7f);
            }

            Score = total / Rounds;
        }

        public override void Cleanup()
        {
            Sfx.SetLoop(LoopId.Pour, false);
        }

        private static void UpdateFill(Cylinder c, float level)
        {
            float h = Mathf.Clamp01(level) * InnerHeight;
            c.Fill.enabled = h > 1f;
            c.Fill.rectTransform.sizeDelta = new Vector2(InnerWidth - 6f, h);
            c.Reading.text = $"{Mathf.Clamp01(level) * CapacityMl:0} ml";
        }

        private void UpdateStream(Cylinder c, bool pouring, float level, float intensity)
        {
            stream.enabled = pouring;
            if (!pouring) return;

            float top = 215f - 48f - 17f;
            float bottom = c.Root.anchoredPosition.y - InnerHeight / 2f + Mathf.Clamp01(level) * InnerHeight;
            var rt = stream.rectTransform;
            rt.anchoredPosition = new Vector2(c.Root.anchoredPosition.x, top);
            rt.sizeDelta = new Vector2(Mathf.Lerp(6f, 18f, intensity), Mathf.Max(0f, top - bottom));
        }
    }
}
