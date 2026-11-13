using System.Collections;
using System.Collections.Generic;
using BreakingPizza.Audio;
using BreakingPizza.Core;
using BreakingPizza.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BreakingPizza.UI
{
    // HUD de la cocina: pasos, pureza y cronómetro.
    public class PeriodicChecklistUI : MonoBehaviour
    {
        private const float TileW = 92f;
        private const float TileH = 104f;
        private const float Gap = 12f;

        private CanvasGroup group;
        private RectTransform panel;
        private readonly Dictionary<PizzaStep, ElementTile> tiles = new Dictionary<PizzaStep, ElementTile>();
        private TextMeshProUGUI purityText;
        private TextMeshProUGUI hintText;
        private TextMeshProUGUI timerText;
        private float shownPurity;
        private bool visible;

        private void Awake()
        {
            Build();
            SetVisible(false, instant: true);
        }

        private void OnEnable()
        {
            GameManager.OnStageChanged += HandleStageChanged;
            PizzaScore.OnStepCompleted += HandleStepCompleted;
            PizzaScore.OnReset += RefreshAll;
        }

        private void OnDisable()
        {
            GameManager.OnStageChanged -= HandleStageChanged;
            PizzaScore.OnStepCompleted -= HandleStepCompleted;
            PizzaScore.OnReset -= RefreshAll;
        }

        private void Build()
        {
            var canvas = UIKit.CreateCanvas("ChecklistCanvas", 10, transform);
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            int n = PizzaScore.AllSteps.Length;
            float width = n * TileW + (n - 1) * Gap + 40f;
            float height = TileH + 120f;

            panel = UIKit.Anchor(UIKit.Rect("Panel", canvas.transform, new Vector2(width, height), new Vector2(28f, -28f)), new Vector2(0f, 1f));
            UIKit.Box("PanelBorder", panel, Palette.BBGreen.WithAlpha(0.55f), new Vector2(width + 6f, height + 6f), Vector2.zero);
            UIKit.Box("PanelFill", panel, new Color(0.02f, 0.05f, 0.03f, 0.88f), new Vector2(width, height), Vector2.zero);
            var stripe = UIKit.Image("Stripe", panel, UIKit.Stripes, Color.white, new Vector2(width - 24f, 6f), new Vector2(0f, height / 2f - 10f));
            stripe.pixelsPerUnitMultiplier = 6f;

            UIKit.Text("Header", panel, "RESCATE DE JESSE", 20f, Palette.Hazmat,
                new Vector2(width - 40f, 30f), new Vector2(0f, height / 2f - 32f), TextAlignmentOptions.Left, true);
            purityText = UIKit.Text("Purity", panel, "PUREZA --.-%", 20f, Palette.MethBlue,
                new Vector2(width - 40f, 30f), new Vector2(0f, height / 2f - 32f), TextAlignmentOptions.Right, true);

            float startX = -width / 2f + 20f + TileW / 2f;
            for (int i = 0; i < n; i++)
            {
                var step = PizzaScore.AllSteps[i];
                var tile = UIKit.ElementTile(panel, new Vector2(TileW, TileH), new Vector2(startX + i * (TileW + Gap), 6f),
                    PizzaScore.Number(step), PizzaScore.Symbol(step), PizzaScore.DisplayName(step));
                tiles[step] = tile;
            }

            hintText = UIKit.Text("Hint", panel, string.Empty, 21f, Color.white,
                new Vector2(width - 40f, 34f), new Vector2(0f, -height / 2f + 26f), TextAlignmentOptions.Left);

            // Cronómetro, en una pestaña debajo del panel.
            var timerTab = UIKit.Box("TimerTab", panel, Palette.BBGreen.WithAlpha(0.55f), new Vector2(216f, 50f), new Vector2(-width / 2f + 108f, -height / 2f - 30f));
            UIKit.Box("TimerFill", timerTab.transform, new Color(0.02f, 0.05f, 0.03f, 0.92f), new Vector2(210f, 44f), Vector2.zero);
            UIKit.Text("TimerLabel", timerTab.transform, "TIEMPO", 16f, Palette.TextDim, new Vector2(90f, 30f), new Vector2(-50f, 0f), TextAlignmentOptions.Left, true);
            timerText = UIKit.Text("Timer", timerTab.transform, "00:00", 28f, Palette.MethBlue, new Vector2(110f, 40f), new Vector2(44f, 0f), TextAlignmentOptions.Right, true);

            RefreshAll();
        }

        private void HandleStageChanged(GameStage stage)
        {
            if (stage == GameStage.Kitchen)
            {
                PizzaScore.Reset();
                SetVisible(true);
                StartCoroutine(IntroAnimation());
            }
            else
            {
                SetVisible(false);
            }
        }

        private void SetVisible(bool show, bool instant = false)
        {
            visible = show;
            if (instant || !isActiveAndEnabled)
            {
                group.alpha = show ? 1f : 0f;
                return;
            }
            StartCoroutine(Tween.Fade(group, group.alpha, show ? 1f : 0f, 0.3f));
        }

        private IEnumerator IntroAnimation()
        {
            // Las casillas "caen" una por una al entrar a la cocina.
            foreach (var step in PizzaScore.AllSteps)
            {
                tiles[step].Root.localScale = Vector3.zero;
            }
            yield return new WaitForSeconds(0.2f);
            foreach (var step in PizzaScore.AllSteps)
            {
                var t = tiles[step].Root;
                Sfx.Play(SfxId.Count, 0.5f, 0.8f + (int)step * 0.1f);
                StartCoroutine(Tween.Run(0.3f, u => t.localScale = Vector3.one * Ease.OutBack(u)));
                yield return new WaitForSeconds(0.09f);
            }
        }

        private void HandleStepCompleted(PizzaStep step, float score)
        {
            RefreshAll();
            if (!tiles.TryGetValue(step, out var tile)) return;

            Sfx.Play(SfxId.Complete, 0.7f);
            StartCoroutine(Tween.Punch(tile.Root, 0.45f, 0.45f));
            UIKit.Burst(this, panel, tile.Root.anchoredPosition, Palette.BBGreenBright, 18, 260f, 8f, -300f, 0.7f, Palette.Hazmat);
        }

        private void RefreshAll()
        {
            PizzaStep? next = null;
            foreach (var step in PizzaScore.AllSteps)
            {
                if (!PizzaScore.IsDone(step) && next == null) next = step;

                if (!tiles.TryGetValue(step, out var tile)) continue;
                if (PizzaScore.TryGet(step, out float score))
                {
                    tile.Fill.color = Color.Lerp(Palette.BBGreen, Palette.BBGreenBright, 0.25f);
                    tile.Border.color = Palette.BBGreenBright;
                    tile.Symbol.color = Color.white;
                    tile.Number.color = Color.white;
                    tile.Label.color = Color.white;
                    tile.Badge.text = $"{score * 100f:0}%";
                    tile.Badge.color = PizzaScore.StepRatingColor(score);
                }
                else
                {
                    tile.Fill.color = Palette.BBGreenDark.WithAlpha(0.9f);
                    tile.Border.color = Palette.BBGreen.WithAlpha(0.5f);
                    tile.Symbol.color = Palette.TextDim;
                    tile.Number.color = Palette.TextDim;
                    tile.Label.color = Palette.TextDim;
                    tile.Badge.text = string.Empty;
                }
            }

            hintText.text = "<color=#F4C430>»</color> " + HintFor(next);
        }

        // Lo que falta hacer en la cocina (lo usa también la AYUDA de la pausa).
        public static string CurrentHint() => HintFor(null);

        private static string HintFor(PizzaStep? next)
        {
            bool salsa = PizzaScore.IsDone(PizzaStep.Salsa);
            bool queso = PizzaScore.IsDone(PizzaStep.Queso);

            if (!PizzaScore.IsDone(PizzaStep.Masa)) return "Amasá la masa en la mesada de amasado";
            if (!salsa && !queso) return "Encontrá la salsa de tomate y el queso";
            if (!salsa) return "Encontrá la salsa de tomate";
            if (!queso) return "Encontrá el queso";
            if (!PizzaScore.IsDone(PizzaStep.Pepperoni)) return "Andá a la mesada de armado: pepperoni y horno";
            if (!PizzaScore.IsDone(PizzaStep.Horno)) return "¡Al horno!";
            return "¡Pizza terminada!";
        }

        private void Update()
        {
            if (!visible) return;

            // Cronómetro: corre mientras falten pasos (la pausa lo congela
            // porque Time.deltaTime vale 0 con Time.timeScale = 0).
            PizzaScore.Tick(Time.deltaTime);
            timerText.text = PizzaScore.FormatTime(PizzaScore.ElapsedSeconds);
            timerText.color = PizzaScore.AllDone ? Palette.BBGreenBright : Palette.MethBlue;

            // Pureza que "cuenta" suavemente hacia el valor real.
            float target = PizzaScore.CompletedCount > 0 ? PizzaScore.CurrentPurity : 0f;
            shownPurity = Mathf.MoveTowards(shownPurity, target, 60f * Time.deltaTime);
            string goal = $"<size=70%><color=#8FA597>meta {PizzaScore.PassPurity:0}%</color></size>";
            if (PizzaScore.CompletedCount > 0)
            {
                purityText.color = shownPurity >= PizzaScore.PassPurity ? Palette.MethBlue : Palette.Danger;
                purityText.text = $"PUREZA {shownPurity:0.0}%  {goal}";
            }
            else
            {
                purityText.color = Palette.MethBlue;
                purityText.text = $"PUREZA --.-%  {goal}";
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
            foreach (var step in PizzaScore.AllSteps)
            {
                if (PizzaScore.IsDone(step)) continue;
                tiles[step].Border.color = IsCurrentObjective(step)
                    ? Color.Lerp(Palette.BBGreen.WithAlpha(0.5f), Palette.Hazmat, pulse)
                    : Palette.BBGreen.WithAlpha(0.5f);
            }
        }

        private static bool IsCurrentObjective(PizzaStep step)
        {
            if (!PizzaScore.IsDone(PizzaStep.Masa)) return step == PizzaStep.Masa;
            if (!PizzaScore.IsDone(PizzaStep.Salsa) || !PizzaScore.IsDone(PizzaStep.Queso))
                return step == PizzaStep.Salsa || step == PizzaStep.Queso;
            if (!PizzaScore.IsDone(PizzaStep.Pepperoni)) return step == PizzaStep.Pepperoni;
            return step == PizzaStep.Horno;
        }
    }
}
