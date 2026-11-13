using BreakingPizza.Controls;
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
    // Minijuego 4: pepperoni.
    public class PepperoniMinigame : Minigame
    {
        private const int Slices = 8;
        private static float TimeLimit => 14f * Difficulty.TimeFactor;
        private const float SauceRadius = 205f;
        private const float SliceDiameter = 66f;
        private const float CoverRadius = 80f;

        public override PizzaStep Step => PizzaStep.Pepperoni;
        public override string Title => "Armado: pepperoni";
        public override string Subtitle => "DISTRIBUCIÓN UNIFORME";
        public override string Instructions => "<b>Click</b> para soltar cada rodaja sobre la pizza (¡gira!). Repartilas parejo: amontonadas o afuera no cuentan.";
        public override string Goal => "Repartí las rodajas de pepperoni de forma pareja sobre la pizza.";
        public override string[] Rules => new[]
        {
            "La pizza <b>gira</b> sobre la tabla. Tenés <b>8 rodajas</b> y poco tiempo.",
            "Hacé click para soltar una rodaja justo donde está el mouse.",
            "Se mide qué tan <b>pareja</b> queda la cobertura: las rodajas encimadas o amontonadas no suman.",
            "Las que caen <color=#FF4D4D><b>afuera de la salsa</b></color> se pierden. Cada vez gira más rápido.",
        };
        public override string[] Keys => new[] { "CLICK" };
        public override string[] GamepadKeys => new[] { "STICK (mover)", "X (soltar)" };
        public override string Tip => "Primero cubrí el borde y después rellená el centro.";

        private RectTransform pizza;
        private RectTransform ghost;
        private Image timeFill;
        private TextMeshProUGUI remainingText;
        private readonly List<Image> stackIcons = new List<Image>();
        private readonly List<Vector2> placed = new List<Vector2>();

        public override void Build()
        {
            Vector2 center = new Vector2(-110f, -20f);
            UIKit.Image("BoardShadow", Area, UIKit.Soft, new Color(0f, 0f, 0f, 0.5f), new Vector2(620f, 620f), center + new Vector2(10f, -14f));
            UIKit.Dot("Board", Area, Palette.Wood, 540f, center);
            UIKit.Dot("BoardInner", Area, Palette.Wood * 1.12f, 510f, center);

            pizza = UIKit.Rect("Pizza", Area, new Vector2(480f, 480f), center);
            BuildPizzaBase(pizza, 480f, Palette.Crust, Palette.Cheese);

            // Rodaja "fantasma" que sigue al mouse.
            ghost = BuildSlice(Area, Vector2.zero, 0.55f).rectTransform;

            // Panel derecho: rodajas que quedan + tiempo.
            var side = UIKit.Box("SidePanel", Area, new Color(0f, 0f, 0f, 0.35f), new Vector2(300f, 420f), new Vector2(410f, -20f));
            UIKit.Text("SideTitle", side.transform, "RODAJAS", 22f, Palette.TextDim, new Vector2(260f, 30f), new Vector2(0f, 180f), TextAlignmentOptions.Center, true);
            for (int i = 0; i < Slices; i++)
            {
                int row = i / 4;
                int col = i % 4;
                stackIcons.Add(BuildSlice(side.transform, new Vector2((col - 1.5f) * 62f, 110f - row * 66f), 1f));
            }
            remainingText = UIKit.Text("Remaining", side.transform, $"{Slices}", 72f, Color.white, new Vector2(260f, 90f), new Vector2(0f, -60f), TextAlignmentOptions.Center, true);
            UIKit.Text("TimeLabel", side.transform, "TIEMPO", 18f, Palette.TextDim, new Vector2(260f, 26f), new Vector2(0f, -140f), TextAlignmentOptions.Center, true);
            timeFill = MakeBar(side.transform, new Vector2(240f, 16f), new Vector2(0f, -166f), Palette.MethBlue, out _);
        }

        // Masa + salsa + queso con algo de textura (reutilizado también por
        // el horno y la pantalla final).
        public static void BuildPizzaBase(RectTransform parent, float diameter, Color crust, Color cheese)
        {
            float k = diameter / 480f;
            UIKit.Dot("Crust", parent, crust, diameter, Vector2.zero);
            UIKit.Dot("CrustInner", parent, crust * 1.08f, diameter - 26f * k, Vector2.zero);
            UIKit.Dot("Sauce", parent, Palette.Tomato, 430f * k, Vector2.zero);
            UIKit.Dot("Cheese", parent, cheese.WithAlpha(0.92f), 404f * k, Vector2.zero);

            var rng = new System.Random(7);
            for (int i = 0; i < 26; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = Mathf.Sqrt((float)rng.NextDouble()) * 180f * k;
                bool sauceSpot = i % 3 == 0;
                UIKit.Dot(sauceSpot ? "SauceSpot" : "CheeseSpot", parent,
                    sauceSpot ? Palette.Tomato.WithAlpha(0.55f) : Palette.CheesePale.WithAlpha(0.6f),
                    (14f + (float)rng.NextDouble() * 22f) * k, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
        }

        public static Image BuildSlice(Transform parent, Vector2 pos, float alpha, float diameter = SliceDiameter)
        {
            var outer = UIKit.Dot("Pepperoni", parent, Palette.PepperoniDark.WithAlpha(alpha), diameter, pos);
            UIKit.Dot("Inner", outer.transform, Palette.Pepperoni.WithAlpha(alpha), diameter * 0.84f, Vector2.zero);
            float k = diameter / SliceDiameter;
            UIKit.Dot("Fat1", outer.transform, new Color(1f, 0.8f, 0.7f, 0.45f * alpha), 9f * k, new Vector2(-12f, 8f) * k);
            UIKit.Dot("Fat2", outer.transform, new Color(1f, 0.8f, 0.7f, 0.45f * alpha), 7f * k, new Vector2(10f, -6f) * k);
            UIKit.Dot("Fat3", outer.transform, new Color(1f, 0.8f, 0.7f, 0.45f * alpha), 6f * k, new Vector2(4f, 14f) * k);
            return outer;
        }

        public override IEnumerator Play()
        {
            int used = 0;
            float elapsed = 0f;

            // Con el control, la rodaja guía arranca en el centro de la pizza.
            if (GameInput.UsingGamepad)
            {
                GameInput.SetPointer(RectTransformUtility.WorldToScreenPoint(null, pizza.position));
            }

            while (used < Slices && elapsed < TimeLimit)
            {
                elapsed += Time.deltaTime;
                float spin = Mathf.Lerp(30f, 75f, used / (float)Slices) * Difficulty.Speed;
                pizza.Rotate(0f, 0f, -spin * Time.deltaTime);

                Vector2 mouse = MouseLocal(Area);
                ghost.anchoredPosition = mouse;
                SetBar(timeFill, 234f, 1f - elapsed / TimeLimit);

                if (ClickPressed)
                {
                    used++;
                    stackIcons[Slices - used].enabled = false;
                    foreach (Transform t in stackIcons[Slices - used].transform) t.gameObject.SetActive(false);
                    remainingText.text = (Slices - used).ToString();
                    Run(Tween.Punch(remainingText.transform, 0.3f, 0.2f));

                    // Posición en el espacio de la pizza (ya contempla la rotación).
                    Vector2 local = MouseLocal(pizza);
                    if (local.magnitude > SauceRadius + SliceDiameter * 0.15f)
                    {
                        Sfx.Play(SfxId.Miss, 0.7f);
                        Popup("¡AFUERA!", Palette.Danger, mouse + new Vector2(0f, 40f), 34f);
                        Run(DropOff(BuildSlice(Area, mouse, 1f).rectTransform));
                    }
                    else
                    {
                        var slice = BuildSlice(pizza, local, 1f);
                        Run(Tween.Squash(slice.transform, new Vector3(1.5f, 1.5f, 1f), 0.3f));
                        Sfx.PlayVaried(SfxId.Place, 1f, 0.12f);

                        bool overlapping = false;
                        foreach (var p in placed)
                        {
                            if (Vector2.Distance(p, local) < SliceDiameter * 0.7f) overlapping = true;
                        }
                        placed.Add(local);

                        if (overlapping)
                        {
                            Popup("¡ENCIMADA!", Palette.Hazmat, mouse + new Vector2(0f, 40f), 30f);
                        }
                        else
                        {
                            UIKit.Burst(Host, Area, mouse, Palette.Tomato, 6, 200f, 7f, -600f, 0.4f);
                        }
                    }
                }

                yield return null;
            }

            ghost.gameObject.SetActive(false);

            if (used < Slices)
            {
                Popup("¡SE ACABÓ EL TIEMPO!", Palette.Danger, new Vector2(-110f, 0f), 40f);
            }

            float coverage = ComputeCoverage();
            Score = Mathf.Clamp01((coverage - 0.25f) / 0.6f);

            // Se guardan las posiciones (normalizadas) para el horno y la
            // pantalla final.
            PizzaScore.PepperoniPositions.Clear();
            foreach (var p in placed) PizzaScore.PepperoniPositions.Add(p / SauceRadius);

            Popup($"Cobertura: {coverage * 100f:0}%", Color.white, new Vector2(-110f, -60f), 36f);
            yield return new WaitForSeconds(0.6f);
        }

        private float ComputeCoverage()
        {
            int inside = 0;
            int covered = 0;
            const float step = 12f;
            for (float x = -SauceRadius; x <= SauceRadius; x += step)
            {
                for (float y = -SauceRadius; y <= SauceRadius; y += step)
                {
                    var pt = new Vector2(x, y);
                    if (pt.magnitude > SauceRadius) continue;
                    inside++;
                    foreach (var p in placed)
                    {
                        if ((p - pt).sqrMagnitude <= CoverRadius * CoverRadius)
                        {
                            covered++;
                            break;
                        }
                    }
                }
            }
            return inside == 0 ? 0f : covered / (float)inside;
        }

        private IEnumerator DropOff(RectTransform slice)
        {
            Vector2 start = slice.anchoredPosition;
            yield return Tween.Run(0.6f, u =>
            {
                if (slice == null) return;
                slice.anchoredPosition = start + new Vector2(40f * u, -500f * u * u);
                slice.localEulerAngles = new Vector3(0f, 0f, 300f * u);
            });
            if (slice != null) Object.Destroy(slice.gameObject);
        }
    }
}
