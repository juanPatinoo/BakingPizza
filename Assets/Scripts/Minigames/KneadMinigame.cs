using System.Collections;
using BreakingPizza.Audio;
using BreakingPizza.Core;
using BreakingPizza.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BreakingPizza.Minigames
{
    // Minijuego 1: amasado.
    public class KneadMinigame : Minigame
    {
        private const int Rounds = 5;
        private const float BarWidth = 820f;

        public override PizzaStep Step => PizzaStep.Masa;
        public override string Title => "Amasado de precisión";
        public override string Subtitle => "SÍNTESIS DE LA BASE";
        public override string Instructions => "<b>Click</b> o <b>Espacio</b> cuando la aguja pase por la zona. Amarillo = PERFECTO. 5 golpes.";
        public override string Goal => "Amasá la masa hasta dejarla lisa y elástica.";
        public override string[] Rules => new[]
        {
            "Una aguja blanca recorre la barra de lado a lado, sin parar.",
            "Hacé click cuando pase por la zona: <color=#F4C430><b>amarillo = PERFECTO</b></color>, <color=#3DDC6E><b>verde = BIEN</b></color>.",
            "Son <b>5 golpes</b>. En cada uno la zona cambia de lugar, se achica y la aguja va más rápido.",
            "Si acertás varios seguidos armás un <b>COMBO</b> que te da puntos extra.",
        };
        public override string[] Keys => new[] { "CLICK", "ESPACIO" };
        public override string[] GamepadKeys => new[] { "X", "R2" };
        public override string Tip => "Mirá la zona, no la aguja: apretá un poquito antes de que llegue.";

        private RectTransform dough;
        private Image doughImg;
        private Image goodZone, perfectZone, needle;
        private Image[] pips;
        private TextMeshProUGUI comboText;

        public override void Build()
        {
            // Mesada + sombra + bollo de masa.
            UIKit.Box("Board", Area, Palette.Wood, new Vector2(620f, 300f), new Vector2(0f, 70f));
            UIKit.Box("BoardInner", Area, Palette.Wood * 1.15f, new Vector2(590f, 270f), new Vector2(0f, 70f));
            UIKit.Image("Shadow", Area, UIKit.Soft, new Color(0f, 0f, 0f, 0.45f), new Vector2(360f, 90f), new Vector2(0f, 10f));

            dough = UIKit.Rect("DoughRoot", Area, new Vector2(300f, 220f), new Vector2(0f, 70f));
            doughImg = UIKit.Image("Dough", dough, UIKit.Circle, Palette.DoughRaw, new Vector2(300f, 210f), Vector2.zero);
            UIKit.Image("Highlight", dough, UIKit.Soft, new Color(1f, 1f, 1f, 0.45f), new Vector2(150f, 80f), new Vector2(-50f, 45f));
            // Grumos (desaparecen a medida que acertás).
            for (int i = 0; i < 6; i++)
            {
                var lump = UIKit.Dot("Lump", dough, Palette.Crust.WithAlpha(0.35f), Random.Range(18f, 30f),
                    new Vector2(Random.Range(-100f, 100f), Random.Range(-60f, 60f)));
                lump.name = "Lump" + i;
            }

            // Barra de timing.
            var barBg = UIKit.Box("BarBg", Area, new Color(0f, 0f, 0f, 0.6f), new Vector2(BarWidth + 16f, 62f), new Vector2(0f, -150f));
            UIKit.Box("BarTrack", barBg.transform, Palette.PanelLight, new Vector2(BarWidth, 46f), Vector2.zero);
            goodZone = UIKit.Box("GoodZone", barBg.transform, Palette.BBGreen, new Vector2(100f, 46f), Vector2.zero);
            perfectZone = UIKit.Box("PerfectZone", barBg.transform, Palette.Hazmat, new Vector2(30f, 46f), Vector2.zero);
            needle = UIKit.Box("Needle", barBg.transform, Color.white, new Vector2(10f, 84f), Vector2.zero);

            // Indicadores de golpes.
            pips = new Image[Rounds];
            for (int i = 0; i < Rounds; i++)
            {
                pips[i] = UIKit.Dot("Pip" + i, Area, new Color(1f, 1f, 1f, 0.18f), 28f, new Vector2((i - (Rounds - 1) / 2f) * 44f, -222f));
            }

            comboText = UIKit.Text("Combo", Area, string.Empty, 40f, Palette.Hazmat, new Vector2(300f, 60f), new Vector2(430f, 150f), TextAlignmentOptions.Center, true);
        }

        public override IEnumerator Play()
        {
            float total = 0f;
            int combo = 0;
            int bestCombo = 0;

            for (int round = 0; round < Rounds; round++)
            {
                float k = round / (float)(Rounds - 1);
                float goodWidth = Mathf.Lerp(0.26f, 0.13f, k);
                float perfectWidth = goodWidth * 0.32f;
                float center = Random.Range(goodWidth / 2f + 0.03f, 1f - goodWidth / 2f - 0.03f);
                float speed = Mathf.Lerp(0.85f, 1.65f, k) * Difficulty.Speed;

                SetZone(goodZone, center, goodWidth);
                SetZone(perfectZone, center, perfectWidth);
                pips[round].color = Color.white;
                Run(Tween.Punch(pips[round].transform, 0.4f, 0.25f));

                // La aguja arranca desde una punta (alternando) en cada golpe.
                float v = round % 2 == 0 ? 0f : 1f;
                float dir = round % 2 == 0 ? 1f : -1f;

                while (true)
                {
                    v += dir * speed * Time.deltaTime;
                    if (v >= 1f) { v = 1f; dir = -1f; }
                    if (v <= 0f) { v = 0f; dir = 1f; }
                    needle.rectTransform.anchoredPosition = new Vector2((v - 0.5f) * BarWidth, 0f);

                    if (ActionPressed) break;
                    yield return null;
                }

                float dist = Mathf.Abs(v - center);
                float hit;
                Vector2 popPos = new Vector2((v - 0.5f) * BarWidth, -80f);

                if (dist <= perfectWidth / 2f)
                {
                    hit = 1f;
                    combo++;
                    pips[round].color = Palette.Hazmat;
                    Sfx.Play(SfxId.Perfect);
                    Sfx.PlayVaried(SfxId.Thud);
                    Popup("¡PERFECTO!", Palette.Hazmat, popPos);
                    UIKit.Burst(Host, Area, new Vector2(0f, 90f), Color.white, 26, 520f, 12f, -800f, 0.8f, Palette.Dough);
                    Run(Tween.Squash(dough, new Vector3(1.35f, 0.62f, 1f)));
                }
                else if (dist <= goodWidth / 2f)
                {
                    hit = 0.6f;
                    combo++;
                    pips[round].color = Palette.BBGreenBright;
                    Sfx.Play(SfxId.Good);
                    Sfx.PlayVaried(SfxId.Thud);
                    Popup("BIEN", Palette.BBGreenBright, popPos);
                    UIKit.Burst(Host, Area, new Vector2(0f, 90f), Color.white, 14, 420f, 10f, -800f, 0.7f, Palette.Dough);
                    Run(Tween.Squash(dough, new Vector3(1.25f, 0.75f, 1f)));
                }
                else
                {
                    hit = 0f;
                    combo = 0;
                    pips[round].color = Palette.Danger;
                    Sfx.Play(SfxId.Miss);
                    Sfx.PlayVaried(SfxId.Thud, 0.5f);
                    Popup("¡FALLASTE!", Palette.Danger, popPos);
                    ShakeCard();
                    Run(Tween.Squash(dough, new Vector3(0.85f, 1.15f, 1f)));
                }

                bestCombo = Mathf.Max(bestCombo, combo);
                total += hit;
                UpdateDoughLook(total / Rounds);

                if (combo >= 2)
                {
                    comboText.text = $"COMBO x{combo}";
                    Run(Tween.Punch(comboText.transform, 0.35f, 0.3f));
                }
                else
                {
                    comboText.text = string.Empty;
                }

                // Pausa corta mostrando dónde quedó la aguja.
                yield return FlashZone(hit > 0f ? Color.white : Palette.Danger);
            }

            float comboBonus = bestCombo >= Rounds ? 0.08f : bestCombo >= 3 ? 0.04f : 0f;
            Score = Mathf.Clamp01(total / Rounds + comboBonus);
        }

        private IEnumerator FlashZone(Color flash)
        {
            Color g = goodZone.color;
            Color p = perfectZone.color;
            yield return Tween.Run(0.4f, u =>
            {
                float f = Mathf.PingPong(u * 4f, 1f);
                goodZone.color = Color.Lerp(g, flash, f * 0.6f);
                perfectZone.color = Color.Lerp(p, flash, f * 0.6f);
            });
            goodZone.color = g;
            perfectZone.color = p;
        }

        private void SetZone(Image zone, float center, float width)
        {
            var rt = zone.rectTransform;
            rt.sizeDelta = new Vector2(width * BarWidth, rt.sizeDelta.y);
            rt.anchoredPosition = new Vector2((center - 0.5f) * BarWidth, 0f);
        }

        // La masa pasa de grumosa/opaca a lisa/clara según cuánto acertaste.
        private void UpdateDoughLook(float quality01)
        {
            doughImg.color = Color.Lerp(Palette.DoughRaw, Palette.Dough * 1.04f, quality01);
            for (int i = 0; i < dough.childCount; i++)
            {
                var child = dough.GetChild(i);
                if (!child.name.StartsWith("Lump")) continue;
                var img = child.GetComponent<Image>();
                img.color = img.color.WithAlpha(0.35f * (1f - quality01));
            }
        }
    }
}
