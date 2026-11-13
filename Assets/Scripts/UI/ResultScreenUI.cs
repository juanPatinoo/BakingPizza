using BreakingPizza.Controls;
using System.Collections;
using System.Collections.Generic;
using BreakingPizza.Audio;
using BreakingPizza.Core;
using BreakingPizza.Minigames;
using BreakingPizza.Player;
using BreakingPizza.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BreakingPizza.UI
{
    // Pantalla final: informe del rescate.
    public class ResultScreenUI : MonoBehaviour
    {
        [SerializeField] private string mainMenuScene = "MainMenu";

        private bool shown;
        private bool acceptingInput;
        private UIButton retryButton;
        private UIButton continueButton;
        private string retryText = "VOLVER A JUGAR";
        private int focus = 1; // 0 = reintentar, 1 = continuar
        private bool passed;

        private RenderTexture backgroundRT;
        private readonly List<(RectTransform rt, Image img, float speed, float phase, float alpha)> crystals =
            new List<(RectTransform, Image, float, float, float)>();

        private void OnEnable()
        {
            GameManager.OnStageChanged += HandleStageChanged;
        }

        private void OnDisable()
        {
            GameManager.OnStageChanged -= HandleStageChanged;
        }

        private void OnDestroy()
        {
            if (backgroundRT != null) backgroundRT.Release();
        }

        private void HandleStageChanged(GameStage stage)
        {
            if (stage == GameStage.Result && !shown)
            {
                shown = true;
                StartCoroutine(Show());
            }
        }

        private IEnumerator Show()
        {
            // Espera a que termine la tarjeta del último minijuego.
            while (MinigameHost.IsRunning) yield return null;
            yield return new WaitForSeconds(0.3f);

            // Foto del juego en este momento: será el fondo, difuminado.
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            backgroundRT = Blur.CreateTarget(960, 540, "ResultBackground");
            Blur.Apply(shot, backgroundRT, 90);
            Destroy(shot);

            FirstPersonController.Instance?.SetGameplayInputEnabled(false);

            float purity = PizzaScore.FinalPurity;
            passed = purity >= PizzaScore.PassPurity;
            float seconds = PizzaScore.ElapsedSeconds;
            bool hadRecord = Records.TryGet(Difficulty.Current, out float oldPurity, out float oldTime);
            bool newRecord = Records.Submit(Difficulty.Current, purity, seconds);
            Color accent = passed ? Palette.BBGreenBright : Palette.Danger;

            var canvas = UIKit.CreateCanvas("ResultCanvas", 100, transform);
            var root = (RectTransform)canvas.transform;
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            BuildBackground(root, passed);
            BuildBar(root, top: true);
            BuildBar(root, top: false);

            // ── Encabezado ──
            var logo = UIKit.Rect("Logo", root, new Vector2(1000f, 150f), new Vector2(-560f, 392f));
            logo.localScale = Vector3.one * 0.55f;
            BuildLogoWord(logo, "Ba", 56, "king", new Vector2(-250f, 0f));
            BuildLogoWord(logo, "Pi", 84, "zza", new Vector2(190f, 0f));

            UIKit.Text("ReportTitle", root, "INFORME DE LABORATORIO", 30f, Palette.Hazmat, new Vector2(800f, 40f), new Vector2(480f, 408f), TextAlignmentOptions.Right, true);
            int batch = Random.Range(1000, 10000);
            UIKit.Text("Batch", root, $"LOTE N° {batch}   ·   DIFICULTAD {Difficulty.Name}", 18f, Palette.TextDim, new Vector2(800f, 26f), new Vector2(480f, 374f), TextAlignmentOptions.Right, true);
            var headTape = UIKit.Image("HeadTape", root, UIKit.Stripes, Color.white, new Vector2(1720f, 6f), new Vector2(0f, 340f));
            headTape.pixelsPerUnitMultiplier = 6f;

            // ── Muestra: tu pizza ──
            UIKit.Text("SampleLabel", root, "MUESTRA ANALIZADA", 18f, Palette.TextDim, new Vector2(480f, 26f), new Vector2(-560f, 292f), TextAlignmentOptions.Center, true);
            var pizzaRoot = UIKit.Rect("PizzaRoot", root, new Vector2(480f, 480f), new Vector2(-560f, 40f));
            UIKit.Image("PizzaGlow", pizzaRoot, UIKit.Soft, accent.WithAlpha(0.18f), new Vector2(700f, 700f), Vector2.zero);
            UIKit.Image("PizzaShadow", pizzaRoot, UIKit.Soft, new Color(0f, 0f, 0f, 0.6f), new Vector2(560f, 560f), new Vector2(12f, -18f));
            UIKit.Dot("Board", pizzaRoot, Palette.Wood, 470f, Vector2.zero);
            UIKit.Dot("BoardInner", pizzaRoot, Palette.Wood * 1.12f, 444f, Vector2.zero);
            var pizza = UIKit.Rect("Pizza", pizzaRoot, new Vector2(480f, 480f), Vector2.zero);
            pizza.localScale = Vector3.one * (410f / 480f);
            BuildFinalPizza(pizza);

            // Tiempo de cocina + récord.
            var timeBox = UIKit.Box("TimeBorder", root, Palette.BBGreen.WithAlpha(0.6f), new Vector2(426f, 86f), new Vector2(-560f, -258f));
            UIKit.Box("TimeFill", timeBox.transform, new Color(0.02f, 0.05f, 0.035f, 0.92f), new Vector2(420f, 80f), Vector2.zero);
            UIKit.Text("TimeLabel", timeBox.transform, "TIEMPO DE\nCOCINA", 16f, Palette.TextDim, new Vector2(180f, 60f), new Vector2(-100f, 0f), TextAlignmentOptions.Left, true);
            UIKit.Text("Time", timeBox.transform, PizzaScore.FormatTime(seconds), 50f, Color.white, new Vector2(220f, 70f), new Vector2(90f, 0f), TextAlignmentOptions.Right, true);

            string recordLine = newRecord ? "¡NUEVO RÉCORD!"
                : hadRecord ? $"RÉCORD: {oldPurity:0.0}%  ·  {PizzaScore.FormatTime(oldTime)}"
                : string.Empty;
            var recordText = UIKit.Text("Record", root, recordLine, newRecord ? 24f : 18f, newRecord ? Palette.Hazmat : Palette.TextDim,
                new Vector2(480f, 30f), new Vector2(-560f, -322f), TextAlignmentOptions.Center, true);
            recordText.alpha = 0f;

            // ── Informe ──
            var report = UIKit.Box("ReportBorder", root, Palette.BBGreen.WithAlpha(0.7f), new Vector2(1006f, 666f), new Vector2(330f, -20f)).rectTransform;
            UIKit.Box("ReportFill", report, new Color(0.015f, 0.04f, 0.028f, 0.93f), new Vector2(1000f, 660f), Vector2.zero);

            UIKit.Text("PurityLabel", report, $"PUREZA FINAL   ·   mínimo {PizzaScore.PassPurity:0}%", 22f, Palette.TextDim,
                new Vector2(620f, 30f), new Vector2(-150f, 282f), TextAlignmentOptions.Left, true);
            UIKit.Image("PurityGlow", report, UIKit.Soft, Palette.MethBlue.WithAlpha(passed ? 0.22f : 0.08f), new Vector2(760f, 300f), new Vector2(-230f, 190f));
            var purityText = UIKit.Text("Purity", report, "0.0%", 150f, Palette.MethBlue, new Vector2(620f, 170f), new Vector2(-150f, 190f), TextAlignmentOptions.Left, true);

            // Sello de tinta con el veredicto.
            var stamp = UIKit.Box("Stamp", report, accent, new Vector2(370f, 170f), new Vector2(285f, 190f)).rectTransform;
            UIKit.Box("StampInner", stamp, new Color(0.015f, 0.04f, 0.028f, 0.96f), new Vector2(354f, 154f), Vector2.zero);
            UIKit.Box("StampLine", stamp, accent, new Vector2(330f, 3f), new Vector2(0f, -46f));
            UIKit.Text("StampTop", stamp, passed ? "JESSE" : "MISIÓN", 52f, accent, new Vector2(340f, 60f), new Vector2(0f, 26f), TextAlignmentOptions.Center, true);
            UIKit.Text("StampBottom", stamp, passed ? "RESCATADO" : "FALLIDA", 34f, accent, new Vector2(340f, 44f), new Vector2(0f, -22f), TextAlignmentOptions.Center, true);
            UIKit.Text("StampFoot", stamp, passed ? "PIZZA ACEPTADA" : "PIZZA RECHAZADA", 14f, accent.WithAlpha(0.8f), new Vector2(340f, 20f), new Vector2(0f, -62f), TextAlignmentOptions.Center, true);
            stamp.localScale = Vector3.zero;

            string line = passed
                ? "Los secuestradores aceptaron la pizza. Jesse está a salvo."
                : "Los secuestradores rechazaron tu pizza. Jesse sigue en peligro.";
            var verdictLine = UIKit.Text("VerdictLine", report, line, 23f, Color.white, new Vector2(920f, 34f), new Vector2(0f, 80f), TextAlignmentOptions.Left);

            // Rango, con su propia ficha.
            GetRank(purity, out string rank, out string rankSymbol, out string quote, out Color rankColor);
            var rankTile = UIKit.ElementTile(report, new Vector2(92f, 102f), new Vector2(-404f, -16f), Mathf.RoundToInt(purity), rankSymbol, "rango");
            rankTile.Fill.color = Color.Lerp(Palette.BBGreenDark, rankColor, 0.35f);
            rankTile.Border.color = rankColor;
            UIKit.Text("RankLabel", report, "RANGO", 16f, Palette.TextDim, new Vector2(760f, 22f), new Vector2(26f, 22f), TextAlignmentOptions.Left, true);
            var rankText = UIKit.Text("Rank", report, rank, 38f, rankColor, new Vector2(760f, 46f), new Vector2(26f, -12f), TextAlignmentOptions.Left, true);
            var quoteText = UIKit.Text("Quote", report, $"\"{quote}\"", 20f, new Color(1f, 1f, 1f, 0.75f), new Vector2(760f, 30f), new Vector2(26f, -50f), TextAlignmentOptions.Left);
            quoteText.fontStyle = FontStyles.Italic;
            var rankGroup = new[] { verdictLine, rankText, quoteText };
            foreach (var t in rankGroup) t.alpha = 0f;
            rankTile.Root.localScale = Vector3.zero;

            var midTape = UIKit.Image("MidTape", report, UIKit.Stripes, new Color(1f, 1f, 1f, 0.6f), new Vector2(920f, 5f), new Vector2(0f, -102f));
            midTape.pixelsPerUnitMultiplier = 6f;
            UIKit.Text("BreakdownLabel", report, "DESGLOSE POR PASO", 16f, Palette.TextDim, new Vector2(920f, 22f), new Vector2(0f, -126f), TextAlignmentOptions.Left, true);

            // Pasos: fichas que se llenan como probetas.
            var steps = new List<(ElementTile tile, Image level, float score)>();
            for (int i = 0; i < PizzaScore.AllSteps.Length; i++)
            {
                var step = PizzaScore.AllSteps[i];
                PizzaScore.TryGet(step, out float s);
                var tile = UIKit.ElementTile(report, new Vector2(118f, 132f), new Vector2(-360f + i * 180f, -228f),
                    PizzaScore.Number(step), PizzaScore.Symbol(step), PizzaScore.DisplayName(step));
                tile.Fill.color = Palette.BBGreenDark;
                tile.Border.color = Palette.BBGreen.WithAlpha(0.6f);

                var level = UIKit.Box("Level", tile.Fill.transform, PizzaScore.StepRatingColor(s).WithAlpha(0.42f), new Vector2(118f, 0f), Vector2.zero);
                var lrt = level.rectTransform;
                lrt.anchorMin = new Vector2(0.5f, 0f);
                lrt.anchorMax = new Vector2(0.5f, 0f);
                lrt.pivot = new Vector2(0.5f, 0f);
                lrt.anchoredPosition = Vector2.zero;
                level.transform.SetAsFirstSibling(); // detrás de los textos
                level.enabled = false;
                tile.Badge.text = string.Empty;
                steps.Add((tile, level, s));
            }

            // ── Botones ──
            retryText = passed ? "VOLVER A JUGAR" : "REINTENTAR";
            retryButton = UIKit.MakeButton(root, retryText + "  (R)", new Vector2(420f, 70f), new Vector2(-240f, -416f), passed ? Palette.PanelLight : Palette.TomatoDark);
            continueButton = UIKit.MakeButton(root, "CONTINUAR  (ENTER)", new Vector2(420f, 70f), new Vector2(240f, -416f), Palette.BBGreen);
            retryButton.Rt.localScale = continueButton.Rt.localScale = Vector3.zero;

            // ── Animación de entrada ──
            Sfx.Play(SfxId.Whoosh);
            yield return Tween.Fade(group, 0f, 1f, 0.6f);
            StartCoroutine(SpinPizza(pizza));

            // Conteo de la pureza.
            float lastTick = 0f;
            yield return Tween.Run(2.2f, u =>
            {
                float v = Mathf.Lerp(0f, purity, Ease.OutCubic(u));
                purityText.text = $"{v:0.0}%";
                purityText.color = v >= PizzaScore.PassPurity ? Palette.MethBlue : Color.Lerp(Palette.Danger, Palette.Hazmat, v / PizzaScore.PassPurity);
                if (v - lastTick >= 3f)
                {
                    lastTick = v;
                    Sfx.Play(SfxId.Count, 0.35f, 0.8f + u * 0.6f);
                }
            });
            purityText.text = $"{purity:0.0}%";
            purityText.color = passed ? Palette.MethBlue : Palette.Danger;
            StartCoroutine(Tween.Punch(purityText.transform, 0.15f, 0.35f));
            yield return new WaitForSeconds(0.3f);

            // El sello cae sobre el informe.
            Sfx.Play(SfxId.Stamp);
            Sfx.Play(passed ? SfxId.Fanfare : SfxId.Fail);
            yield return Tween.Run(0.3f, u =>
            {
                stamp.localScale = Vector3.one * Mathf.Lerp(2.4f, 1f, Ease.OutCubic(u));
                stamp.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-22f, -8f, u));
            });
            StartCoroutine(Tween.Shake(report, 12f, 0.3f));
            if (passed) StartCoroutine(Confetti(root));
            else UIKit.Burst(this, report, stamp.anchoredPosition, Palette.Danger, 30, 500f, 12f, -500f, 1f, Palette.TomatoDark);

            // Rango.
            Sfx.Play(SfxId.Good, 0.6f);
            yield return Tween.Run(0.4f, u =>
            {
                foreach (var t in rankGroup) t.alpha = u;
                rankTile.Root.localScale = Vector3.one * Ease.OutBack(u);
                recordText.alpha = u;
            });
            if (newRecord)
            {
                Sfx.Play(SfxId.Perfect, 0.7f);
                StartCoroutine(Tween.Punch(recordText.transform, 0.3f, 0.4f));
            }

            // Las fichas de los pasos se llenan una por una.
            foreach (var st in steps)
            {
                var s = st;
                Color rc = PizzaScore.StepRatingColor(s.score);
                s.tile.Border.color = rc;
                s.level.enabled = true;
                Sfx.Play(SfxId.Count, 0.45f, 0.8f + s.score * 0.6f);
                yield return Tween.Run(0.28f, u =>
                {
                    s.level.rectTransform.sizeDelta = new Vector2(118f, 132f * s.score * Ease.OutCubic(u));
                    s.tile.Badge.text = $"{s.score * 100f * u:0}%";
                });
                s.tile.Badge.color = Color.white;
                StartCoroutine(Tween.Punch(s.tile.Root, 0.12f, 0.25f));
            }

            // Botones.
            yield return Tween.Run(0.3f, u => retryButton.Rt.localScale = continueButton.Rt.localScale = Vector3.one * Ease.OutBack(u));
            acceptingInput = true;
        }

        private void Update()
        {
            AnimateCrystals();

            if (!acceptingInput || CreditsRoll.IsShowing) return;

            // Las etiquetas muestran la tecla del dispositivo en uso.
            retryButton.Label.text = GameInput.Label(retryText + "  (R)", retryText);
            continueButton.Label.text = GameInput.Label("CONTINUAR  (ENTER)", "CONTINUAR");

            // CONTINUAR late suave: es el camino natural (créditos y menú).
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
            continueButton.Fill.color = Color.Lerp(Palette.BBGreen, Palette.BBGreenBright, pulse * 0.4f);

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.rKey.wasPressedThisFrame) { Restart(); return; }
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) { ShowCredits(); return; }
            }

            // Control: cruceta/stick elige el botón y X lo activa.
            if (GameInput.NavX < 0) focus = 0;
            if (GameInput.NavX > 0) focus = 1;
            retryButton.Focused = focus == 0;
            continueButton.Focused = focus == 1;
            if (GameInput.UsingGamepad && GameInput.ConfirmPressed)
            {
                if (focus == 0) Restart(); else ShowCredits();
                return;
            }

            if (retryButton.Tick()) { Restart(); return; }
            if (continueButton.Tick()) ShowCredits();
        }

        // Créditos finales; al terminar (o saltearlos) se vuelve al menú.
        private void ShowCredits()
        {
            acceptingInput = false;
            Sfx.Play(SfxId.Go);
            CreditsRoll.Show(passed ? CreditsRoll.Ending.Win : CreditsRoll.Ending.Lose, GoToMenu);
        }

        private void Restart()
        {
            acceptingInput = false;
            Sfx.Play(SfxId.Go);
            ScreenFader.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void GoToMenu()
        {
            acceptingInput = false;
            Sfx.Play(SfxId.Go);
            if (Application.CanStreamedLevelBeLoaded(mainMenuScene))
            {
                ScreenFader.LoadScene(mainMenuScene);
            }
            else
            {
                Debug.LogWarning($"[ResultScreenUI] La escena \"{mainMenuScene}\" no está en Build Settings; se reinicia la actual.");
                ScreenFader.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        // ── Construcción ──

        // Fondo: el juego congelado y difuminado, velo verde, viñeta y
        // cristales azules flotando.
        private void BuildBackground(RectTransform root, bool win)
        {
            var bg = UIKit.Rect("Background", root, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            UIKit.Stretch(bg.rectTransform);
            bg.texture = backgroundRT;
            bg.raycastTarget = false;

            var tint = UIKit.Image("Tint", root, null, new Color(0.01f, 0.05f, 0.03f, 0.72f), Vector2.zero, Vector2.zero);
            UIKit.Stretch(tint.rectTransform);
            UIKit.Image("GlowL", root, UIKit.Soft, (win ? Palette.BBGreen : Palette.TomatoDark).WithAlpha(0.3f), new Vector2(1300f, 1100f), new Vector2(-560f, -20f));
            UIKit.Image("GlowR", root, UIKit.Soft, (win ? Palette.MethBlue : Palette.Danger).WithAlpha(0.1f), new Vector2(1300f, 900f), new Vector2(420f, 60f));

            var rng = new System.Random(21);
            for (int i = 0; i < 34; i++)
            {
                float size = 4f + (float)rng.NextDouble() * 8f;
                var img = UIKit.Image("Crystal", root, i % 3 == 0 ? UIKit.Rounded : UIKit.Circle, Palette.MethBlue, new Vector2(size, size * (i % 3 == 0 ? 1.6f : 1f)),
                    new Vector2(-980f + (float)rng.NextDouble() * 1960f, -560f + (float)rng.NextDouble() * 1120f));
                float alpha = 0.06f + (float)rng.NextDouble() * 0.22f;
                img.rectTransform.localEulerAngles = new Vector3(0f, 0f, (float)rng.NextDouble() * 180f);
                crystals.Add((img.rectTransform, img, 10f + (float)rng.NextDouble() * 24f, (float)rng.NextDouble() * 6.28f, alpha));
            }
        }

        private void AnimateCrystals()
        {
            float dt = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;
            foreach (var c in crystals)
            {
                if (c.rt == null) continue;
                Vector2 pos = c.rt.anchoredPosition;
                pos.y += c.speed * dt;
                pos.x += Mathf.Sin(time * 0.6f + c.phase) * 10f * dt;
                if (pos.y > 580f) pos.y = -580f;
                c.rt.anchoredPosition = pos;
                c.rt.Rotate(0f, 0f, c.speed * 0.6f * dt);
                c.img.color = Palette.MethBlue.WithAlpha(c.alpha * (0.7f + 0.3f * Mathf.Sin(time * 1.3f + c.phase)));
            }
        }

        // Barra de cine con cinta de peligro (como en el giro y los créditos).
        private static void BuildBar(RectTransform root, bool top)
        {
            var bar = UIKit.Rect(top ? "TopBar" : "BottomBar", root, new Vector2(0f, 64f), Vector2.zero);
            bar.anchorMin = new Vector2(0f, top ? 1f : 0f);
            bar.anchorMax = new Vector2(1f, top ? 1f : 0f);
            bar.pivot = new Vector2(0.5f, top ? 1f : 0f);
            bar.sizeDelta = new Vector2(0f, 64f);
            var fill = UIKit.Image("Fill", bar, null, Color.black, Vector2.zero, Vector2.zero);
            UIKit.Stretch(fill.rectTransform);

            var tape = UIKit.Image("Tape", bar, UIKit.Stripes, new Color(1f, 1f, 1f, 0.6f), Vector2.zero, Vector2.zero);
            tape.pixelsPerUnitMultiplier = 6f;
            var rt = tape.rectTransform;
            rt.anchorMin = new Vector2(0f, top ? 0f : 1f);
            rt.anchorMax = new Vector2(1f, top ? 0f : 1f);
            rt.pivot = new Vector2(0.5f, top ? 0f : 1f);
            rt.sizeDelta = new Vector2(0f, 6f);
        }

        private static void BuildLogoWord(RectTransform parent, string symbol, int number, string rest, Vector2 pos)
        {
            var tile = UIKit.ElementTile(parent, new Vector2(120f, 132f), pos + new Vector2(-90f, 0f), number, symbol, string.Empty);
            tile.Fill.color = Palette.BBGreen;
            tile.Border.color = Palette.BBGreenBright;
            tile.Symbol.fontSize = 68f;
            UIKit.Text("Rest", parent, rest, 96f, Color.white, new Vector2(300f, 130f), pos + new Vector2(130f, -8f), TextAlignmentOptions.Left, true);
        }

        // También la usan los créditos finales (la pizza girando al final).
        internal static void BuildFinalPizza(RectTransform pizza)
        {
            float bake = PizzaScore.IsDone(PizzaStep.Horno) ? PizzaScore.BakeAmount : 0f;
            float burn = PizzaScore.BurnAmount;
            Color crust = Color.Lerp(Color.Lerp(Palette.DoughRaw, Palette.CrustBaked, bake), Palette.Charred, burn * 0.8f);
            Color cheese = Color.Lerp(Color.Lerp(Palette.CheesePale, Palette.Cheese * 0.95f, bake), Palette.CrustBaked, burn * 0.6f);
            Minigames.PepperoniMinigame.BuildPizzaBase(pizza, 480f, crust, cheese);

            foreach (var p in PizzaScore.PepperoniPositions)
            {
                Minigames.PepperoniMinigame.BuildSlice(pizza, p * 205f, 1f);
            }

            var rng = new System.Random(3);
            int spots = Mathf.RoundToInt(burn * 18f);
            for (int i = 0; i < spots; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = Mathf.Sqrt((float)rng.NextDouble()) * 210f;
                UIKit.Dot("Burn", pizza, Palette.Charred.WithAlpha(0.8f), 26f + (float)rng.NextDouble() * 34f, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
        }

        private static IEnumerator SpinPizza(RectTransform pizza)
        {
            while (pizza != null)
            {
                pizza.Rotate(0f, 0f, -8f * Time.deltaTime);
                yield return null;
            }
        }

        private IEnumerator Confetti(RectTransform root)
        {
            Color[] colors = { Palette.BBGreenBright, Palette.Hazmat, Palette.MethBlue, Color.white, Palette.Tomato };
            for (int i = 0; i < 6; i++)
            {
                var c = colors[i % colors.Length];
                UIKit.Burst(this, root, new Vector2(Random.Range(-800f, 800f), Random.Range(200f, 450f)), c, 30, 700f, 14f, -700f, 2.2f,
                    colors[(i + 2) % colors.Length], UIKit.Rounded);
                yield return new WaitForSeconds(0.25f);
            }
        }

        // Rango según la pureza, con su "símbolo" de tabla periódica.
        private static void GetRank(float purity, out string rank, out string symbol, out string quote, out Color color)
        {
            if (purity >= 95f)
            {
                rank = "HEISENBERG";
                symbol = "Hb";
                quote = "Say my name. Pureza nivel azul.";
                color = Palette.MethBlue;
            }
            else if (purity >= 85f)
            {
                rank = "SOCIO DE GUS FRING";
                symbol = "Gf";
                quote = "Los Pollos Hermanos quiere esta receta.";
                color = Palette.BBGreenBright;
            }
            else if (purity >= 72f)
            {
                rank = "JESSE PINKMAN";
                symbol = "Jp";
                quote = "¡Yeah, Mr. White! ¡Yeah, science!";
                color = Palette.Hazmat;
            }
            else if (purity >= PizzaScore.PassPurity)
            {
                rank = "AMATEUR DE LA RV";
                symbol = "Rv";
                quote = "Zafó por poco. Saul diría que fue un milagro.";
                color = new Color(1f, 0.6f, 0.2f);
            }
            else if (purity >= 40f)
            {
                rank = "SKINNY PETE";
                symbol = "Sp";
                quote = "Casi... pero casi no cuenta.";
                color = new Color(1f, 0.45f, 0.25f);
            }
            else
            {
                rank = "TUCO SALAMANCA";
                symbol = "Tc";
                quote = "Esto no es cocinar. Esto es un desastre.";
                color = Palette.Danger;
            }
        }
    }
}
