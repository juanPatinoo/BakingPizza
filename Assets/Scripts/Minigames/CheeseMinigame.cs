using BreakingPizza.Controls;
using System.Collections;
using BreakingPizza.Audio;
using BreakingPizza.Core;
using BreakingPizza.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BreakingPizza.Minigames
{
    // Minijuego 3: rallado de queso.
    public class CheeseMinigame : Minigame
    {
        private static float TimeLimit => 9f * Difficulty.TimeFactor;
        private const float ProgressPerStroke = 0.034f;
        private const float BarWidth = 700f;

        public override PizzaStep Step => PizzaStep.Queso;
        public override string Title => "Rallado de queso";
        public override string Subtitle => "MOLIENDA FINA";
        public override string Instructions => "Alterná <b>A</b> y <b>D</b> (o las flechas) para rallar. Con la luz <color=#FF4D4D><b>ROJA</b></color> ¡FRENÁ! o te rallás los nudillos.";
        public override string Goal => "Rallá todo el queso antes de que se acabe el tiempo.";
        public override string[] Rules => new[]
        {
            "Alterná las teclas <b>A</b> y <b>D</b> (o las flechas) lo más rápido que puedas.",
            "Apretar <b>dos veces la misma tecla</b> no ralla: tenés que ir de una a la otra.",
            "Cuando se prende la luz <color=#F4C430><b>amarilla</b></color> prepárate; con la <color=#FF4D4D><b>ROJA</b></color> ¡FRENÁ!",
            "Si rallás con la luz roja te lastimás los nudillos y perdés progreso y puntos.",
        };
        public override string[] Keys => new[] { "A", "D", "IZQ", "DER" };
        public override string[] GamepadKeys => new[] { "L1", "R1" };
        public override string Tip => "Ritmo parejo gana: apurarte demasiado hace que pises la misma tecla.";

        private RectTransform cheese;
        private Image cheeseImg;
        private RectTransform pile;
        private Image progressFill, timeFill;
        private Image keyA, keyD;
        private Image warningLight;
        private TextMeshProUGUI warningText;
        private Image redVignette;

        private enum Danger { None, Warning, Stop }

        public override void Build()
        {
            // Rallador.
            var grater = UIKit.Rect("Grater", Area, new Vector2(240f, 380f), new Vector2(0f, -10f));
            UIKit.Box("Handle", grater, Palette.Charred, new Vector2(110f, 46f), new Vector2(0f, 205f));
            UIKit.Box("Body", grater, Palette.Steel, new Vector2(240f, 380f), Vector2.zero);
            UIKit.Box("BodyShade", grater, new Color(0f, 0f, 0f, 0.12f), new Vector2(110f, 370f), new Vector2(60f, 0f));
            for (int row = 0; row < 11; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    float x = (col - 2f) * 40f + (row % 2 == 0 ? 0f : 18f) - 8f;
                    UIKit.Box("Hole", grater, new Color(0.2f, 0.22f, 0.25f, 0.9f), new Vector2(22f, 9f), new Vector2(x, 160f - row * 32f));
                }
            }

            // Bloque de queso (se mueve arriba/abajo y se achica).
            cheese = UIKit.Rect("Cheese", Area, new Vector2(190f, 150f), new Vector2(0f, 60f));
            cheeseImg = UIKit.Box("CheeseBlock", cheese, Palette.Cheese, new Vector2(190f, 150f), Vector2.zero);
            for (int i = 0; i < 5; i++)
            {
                UIKit.Dot("CheeseHole", cheese, Palette.Hazmat * 0.9f, Random.Range(14f, 26f), new Vector2(Random.Range(-70f, 70f), Random.Range(-50f, 50f)));
            }

            var pileMask = UIKit.Rect("PileMask", Area, new Vector2(440f, 140f), new Vector2(0f, -229f + 70f));
            pileMask.gameObject.AddComponent<RectMask2D>();
            pile = UIKit.Image("Pile", pileMask, UIKit.Circle, Palette.Cheese, new Vector2(300f, 0f), new Vector2(0f, -70f)).rectTransform;
            UIKit.Box("Plate", Area, new Color(1f, 1f, 1f, 0.85f), new Vector2(420f, 22f), new Vector2(0f, -240f));

            // Teclas.
            keyA = MakeKey(GameInput.Label("A", "L1"), new Vector2(-330f, -20f));
            keyD = MakeKey(GameInput.Label("D", "R1"), new Vector2(330f, -20f));

            // Barras de progreso y tiempo.
            UIKit.Text("ProgressLabel", Area, "QUESO RALLADO", 18f, Palette.TextDim, new Vector2(300f, 24f), new Vector2(-BarWidth / 2f + 150f, 246f), TextAlignmentOptions.Left, true);
            progressFill = MakeBar(Area, new Vector2(BarWidth, 26f), new Vector2(0f, 222f), Palette.Cheese, out _);
            timeFill = MakeBar(Area, new Vector2(BarWidth, 10f), new Vector2(0f, 200f), Palette.MethBlue, out _);

            // Luz de peligro.
            UIKit.Dot("LightRim", Area, new Color(0f, 0f, 0f, 0.7f), 96f, new Vector2(470f, 150f));
            warningLight = UIKit.Dot("Light", Area, new Color(0.2f, 0.2f, 0.2f), 80f, new Vector2(470f, 150f));
            warningText = UIKit.Text("WarningText", Area, string.Empty, 34f, Palette.Danger, new Vector2(260f, 50f), new Vector2(470f, 80f), TextAlignmentOptions.Center, true);

            redVignette = UIKit.Image("RedVignette", Card, null, Palette.Danger.WithAlpha(0f), Vector2.zero, Vector2.zero);
            UIKit.Stretch(redVignette.rectTransform);
        }

        private Image MakeKey(string label, Vector2 pos)
        {
            var key = UIKit.Box("Key" + label, Area, new Color(1f, 1f, 1f, 0.15f), new Vector2(110f, 110f), pos);
            UIKit.Box("KeyTop", key.transform, Palette.PanelLight, new Vector2(96f, 96f), new Vector2(0f, 4f));
            UIKit.Text("KeyLabel", key.transform, label, 54f, Color.white, new Vector2(96f, 96f), new Vector2(0f, 4f), TextAlignmentOptions.Center, true);
            return key;
        }

        public override IEnumerator Play()
        {
            float progress = 0f;
            float elapsed = 0f;
            int hurts = 0;
            int lastKey = 0; // -1 = A, 1 = D
            bool cheeseUp = true;

            Danger danger = Danger.None;
            float nextDanger = Random.Range(1.6f, 2.4f);
            float dangerTimer = 0f;
            float stopDuration = 0.9f;
            float hurtCooldown = 0f;

            while (elapsed < TimeLimit && progress < 1f)
            {
                elapsed += Time.deltaTime;
                hurtCooldown -= Time.deltaTime;

                // ── Ciclo de la luz: nada -> amarilla -> roja -> nada ──
                dangerTimer += Time.deltaTime;
                switch (danger)
                {
                    case Danger.None:
                        if (dangerTimer >= nextDanger)
                        {
                            danger = Danger.Warning;
                            dangerTimer = 0f;
                            warningText.text = "¡ATENTO!";
                            warningText.color = Palette.Hazmat;
                            Sfx.Play(SfxId.Count, 0.8f);
                        }
                        break;
                    case Danger.Warning:
                        if (dangerTimer >= 0.45f)
                        {
                            danger = Danger.Stop;
                            dangerTimer = 0f;
                            stopDuration = Random.Range(0.75f, 1.05f);
                            warningText.text = "¡FRENÁ!";
                            warningText.color = Palette.Danger;
                            Sfx.Play(SfxId.Alarm, 0.8f);
                        }
                        break;
                    case Danger.Stop:
                        if (dangerTimer >= stopDuration)
                        {
                            danger = Danger.None;
                            dangerTimer = 0f;
                            nextDanger = Random.Range(1.3f, 2.4f);
                            warningText.text = "¡DALE!";
                            warningText.color = Palette.BBGreenBright;
                            Run(ClearTextLater(0.5f));
                        }
                        break;
                }

                float blink = Mathf.PingPong(Time.time * 8f, 1f);
                warningLight.color = danger == Danger.Stop ? Color.Lerp(Palette.Danger, Color.white, blink * 0.3f)
                    : danger == Danger.Warning ? Palette.Hazmat
                    : new Color(0.2f, 0.25f, 0.2f);
                redVignette.color = Palette.Danger.WithAlpha(danger == Danger.Stop ? 0.10f + blink * 0.06f : 0f);

                // ── Input ──
                int pressed = ReadKey();
                if (pressed != 0)
                {
                    if (danger == Danger.Stop)
                    {
                        if (hurtCooldown <= 0f)
                        {
                            hurts++;
                            hurtCooldown = 0.5f;
                            progress = Mathf.Max(0f, progress - 0.08f);
                            Sfx.Play(SfxId.Hurt);
                            ShakeCard(20f, 0.35f);
                            Popup("¡AY, LOS NUDILLOS!", Palette.Danger, new Vector2(0f, 150f), 44f);
                            UIKit.Burst(Host, Area, cheese.anchoredPosition, Palette.Danger, 14, 380f, 10f, -900f, 0.6f);
                        }
                    }
                    else if (pressed != lastKey)
                    {
                        lastKey = pressed;
                        progress += ProgressPerStroke;
                        cheeseUp = !cheeseUp;
                        Sfx.PlayVaried(SfxId.Grate, 0.9f, 0.15f);
                        UIKit.Burst(Host, Area, new Vector2(Random.Range(-60f, 60f), -170f), Palette.Cheese, 4, 160f, 9f, -1100f, 0.45f, Palette.CheesePale, UIKit.Rounded);
                        Run(Tween.Punch((pressed < 0 ? keyA : keyD).transform, 0.18f, 0.12f));
                    }
                    else
                    {
                        // Misma tecla dos veces: no ralla, solo un "clack".
                        Run(Tween.Shake((pressed < 0 ? keyA : keyD).rectTransform, 6f, 0.12f));
                    }
                }

                // ── Visual ──
                float shrink = Mathf.Lerp(1f, 0.3f, progress);
                cheeseImg.rectTransform.sizeDelta = new Vector2(190f, 150f * shrink);
                float targetY = (cheeseUp ? 90f : 10f);
                cheese.anchoredPosition = new Vector2(0f, Mathf.Lerp(cheese.anchoredPosition.y, targetY, 18f * Time.deltaTime));
                pile.sizeDelta = new Vector2(Mathf.Lerp(120f, 400f, progress), Mathf.Lerp(0f, 240f, progress));

                SetBar(progressFill, BarWidth - 6f, progress);
                SetBar(timeFill, BarWidth - 6f, 1f - elapsed / TimeLimit);

                HighlightNextKey(lastKey, danger == Danger.Stop);

                yield return null;
            }

            warningText.text = string.Empty;
            redVignette.color = Palette.Danger.WithAlpha(0f);
            progress = Mathf.Clamp01(progress);

            float baseScore = progress >= 1f
                ? 0.8f + 0.2f * Mathf.Clamp01((TimeLimit - elapsed) / (TimeLimit * 0.4f))
                : progress * 0.75f;
            Score = Mathf.Clamp01(baseScore - hurts * 0.12f);

            if (progress >= 1f)
            {
                Popup("¡QUESO LISTO!", Palette.Cheese, new Vector2(0f, 150f));
                UIKit.Burst(Host, Area, new Vector2(0f, -200f), Palette.Cheese, 30, 500f, 12f, -900f, 0.9f, Palette.CheesePale, UIKit.Rounded);
            }
            else
            {
                Popup("¡SE ACABÓ EL TIEMPO!", Palette.Danger, new Vector2(0f, 150f), 40f);
            }
        }

        private IEnumerator ClearTextLater(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (warningText != null && warningText.text == "¡DALE!") warningText.text = string.Empty;
        }

        // A / flecha izq / L1 = -1 ; D / flecha der / R1 = +1 (ver GameInput).
        private static int ReadKey() => GameInput.GrateDirection;

        private void HighlightNextKey(int lastKey, bool stop)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 14f);
            Color idle = new Color(1f, 1f, 1f, 0.15f);
            Color next = stop ? Palette.Danger : Color.Lerp(Palette.Hazmat, Color.white, pulse * 0.4f);
            keyA.color = (lastKey != -1 || stop) ? next : idle;
            keyD.color = (lastKey != 1 || stop) ? next : idle;
        }
    }
}
