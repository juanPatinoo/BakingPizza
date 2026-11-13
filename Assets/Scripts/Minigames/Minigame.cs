using BreakingPizza.Controls;
using System;
using System.Collections;
using BreakingPizza.Audio;
using BreakingPizza.Core;
using BreakingPizza.Player;
using BreakingPizza.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BreakingPizza.Minigames
{
    // Base de los minijuegos y la tarjeta que los muestra.
    public abstract class Minigame
    {
        public abstract PizzaStep Step { get; }
        public abstract string Title { get; }
        public abstract string Subtitle { get; }
        public abstract string Instructions { get; }

        // Pantalla de reglas que se muestra ANTES de empezar (el jugador
        // lee con calma y apreta "¡LISTO!" cuando quiere).
        public virtual string Goal => string.Empty;
        public virtual string[] Rules => new string[0];
        public virtual string[] Keys => new[] { "CLICK" };
        // Teclas a mostrar cuando se juega con control.
        public virtual string[] GamepadKeys => new[] { "X" };
        public virtual string Tip => string.Empty;

        public float Score { get; protected set; }

        protected MinigameHost Host { get; private set; }
        protected RectTransform Area { get; private set; }
        protected RectTransform Card { get; private set; }

        internal void Attach(MinigameHost host, RectTransform area, RectTransform card)
        {
            Host = host;
            Area = area;
            Card = card;
        }

        public abstract void Build();
        public abstract IEnumerator Play();
        public virtual void Cleanup() { }

        // ── Helpers de input (Input System nuevo) ──

        // Click / Espacio / X / R2 (ver GameInput).
        protected static bool ActionPressed => GameInput.ActionPressed;

        protected static bool ActionHeld => GameInput.ActionHeld;

        // Click sobre el puntero: click del mouse o X con el cursor virtual.
        protected static bool ClickPressed => GameInput.PointerClick;

        // Posición del mouse en coordenadas locales de un RectTransform
        // (el Canvas es Screen Space - Overlay, así que la cámara es null).
        protected static Vector2 MouseLocal(RectTransform rt)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, GameInput.Pointer, null, out Vector2 p);
            return p;
        }

        // ── Helpers de feedback ──

        protected void Popup(string text, Color color, Vector2 pos, float size = 46f)
        {
            UIKit.FloatText(Host, Area, text, color, pos, size);
        }

        protected void ShakeCard(float magnitude = 14f, float duration = 0.3f)
        {
            Host.StartCoroutine(Tween.Shake(Card, magnitude, duration));
        }

        protected Coroutine Run(IEnumerator routine) => Host.StartCoroutine(routine);

        // Barra horizontal simple (fondo + relleno anclado a la izquierda).
        protected static Image MakeBar(Transform parent, Vector2 size, Vector2 pos, Color fill, out Image background)
        {
            background = UIKit.Box("BarBg", parent, new Color(0f, 0f, 0f, 0.55f), size, pos);
            var fillImg = UIKit.Box("BarFill", background.transform, fill, size - new Vector2(6f, 6f), Vector2.zero);
            var rt = fillImg.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(3f, 0f);
            return fillImg;
        }

        protected static void SetBar(Image fill, float maxWidth, float value01)
        {
            var rt = fill.rectTransform;
            rt.sizeDelta = new Vector2(Mathf.Max(0f, maxWidth * Mathf.Clamp01(value01)), rt.sizeDelta.y);
            fill.enabled = value01 > 0.001f;
        }
    }

    public class MinigameHost : MonoBehaviour
    {
        public static bool IsRunning { get; private set; }

        private static MinigameHost instance;

        public static MinigameHost Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject("[MinigameHost]").AddComponent<MinigameHost>();
                }
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsRunning = false;
            instance = null;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                IsRunning = false;
            }
        }

        public static bool Run(Minigame[] games, Action<Minigame, float> onEach, Action onAll = null)
        {
            if (IsRunning || games == null || games.Length == 0) return false;
            IsRunning = true;
            Instance.StartCoroutine(Instance.RunAll(games, onEach, onAll));
            return true;
        }

        public static bool Run(Minigame game, Action<float> onComplete)
        {
            return Run(new[] { game }, (g, s) => onComplete?.Invoke(s));
        }

        private IEnumerator RunAll(Minigame[] games, Action<Minigame, float> onEach, Action onAll)
        {
            FirstPersonController.Instance?.SetGameplayInputEnabled(false);

            foreach (var game in games)
            {
                yield return RunOne(game);
                try
                {
                    onEach?.Invoke(game, game.Score);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            FirstPersonController.Instance?.SetGameplayInputEnabled(true);
            IsRunning = false;
            onAll?.Invoke();
        }

        private IEnumerator RunOne(Minigame game)
        {
            // ── Tarjeta ──
            var canvas = UIKit.CreateCanvas("MinigameCanvas", 50, transform);
            var root = (RectTransform)canvas.transform;
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var backdrop = UIKit.Image("Backdrop", root, null, new Color(0f, 0f, 0f, 0.62f), Vector2.zero, Vector2.zero);
            UIKit.Stretch(backdrop.rectTransform);

            // cardRoot es el que se anima (escala/temblor); adentro van el
            // borde (detrás) y la tarjeta en sí (delante).
            Vector2 cardSize = new Vector2(1240f, 800f);
            var cardRoot = UIKit.Rect("CardRoot", root, cardSize, new Vector2(0f, -10f));
            UIKit.Box("CardBorder", cardRoot, Palette.BBGreen.WithAlpha(0.7f), cardSize + new Vector2(8f, 8f), Vector2.zero);
            var card = UIKit.Box("Card", cardRoot, Palette.Panel, cardSize, Vector2.zero).rectTransform;

            // Franja de peligro arriba (estilo cinta de "material peligroso").
            var stripe = UIKit.Image("HazardStripe", card, UIKit.Stripes, Color.white, new Vector2(cardSize.x - 40f, 14f), new Vector2(0f, cardSize.y / 2f - 22f));
            stripe.pixelsPerUnitMultiplier = 4f;

            // Encabezado: tile del elemento + título + subtítulo.
            var tile = UIKit.ElementTile(card, new Vector2(92f, 104f), new Vector2(-cardSize.x / 2f + 90f, cardSize.y / 2f - 100f),
                PizzaScore.Number(game.Step), PizzaScore.Symbol(game.Step), PizzaScore.DisplayName(game.Step));
            tile.Fill.color = Palette.BBGreen;
            tile.Border.color = Palette.BBGreenBright;

            UIKit.Text("Subtitle", card, $"PASO {PizzaScore.Number(game.Step)} / {PizzaScore.AllSteps.Length}  ·  {game.Subtitle}", 22f, Palette.Hazmat,
                new Vector2(900f, 30f), new Vector2(-cardSize.x / 2f + 600f, cardSize.y / 2f - 68f), TextAlignmentOptions.Left, true);
            UIKit.Text("Title", card, game.Title, 52f, Color.white,
                new Vector2(900f, 64f), new Vector2(-cardSize.x / 2f + 600f, cardSize.y / 2f - 112f), TextAlignmentOptions.Left, true);

            // Zona de juego.
            var area = UIKit.Rect("Area", card, new Vector2(cardSize.x - 60f, 520f), new Vector2(0f, -22f));

            // Instrucciones (pie de la tarjeta).
            var instrBg = UIKit.Box("InstructionsBg", card, new Color(0f, 0f, 0f, 0.45f), new Vector2(cardSize.x - 60f, 62f), new Vector2(0f, -cardSize.y / 2f + 46f));
            UIKit.Text("Instructions", instrBg.transform, GameInput.Adapt(game.Instructions), 23f, Palette.CheesePale,
                new Vector2(cardSize.x - 90f, 60f), Vector2.zero);

            // Capa superior para la cuenta regresiva y el sello.
            var overlay = UIKit.Rect("Overlay", card, cardSize, Vector2.zero);

            game.Attach(this, area, cardRoot);
            try
            {
                game.Build();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            // ── Entrada ──
            Sfx.Play(SfxId.Whoosh, 0.8f);
            yield return Tween.Run(0.35f, u =>
            {
                group.alpha = u;
                cardRoot.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, Ease.OutBack(u));
            });

            // ── Reglas: el jugador lee y apreta "¡LISTO!" ──
            yield return Briefing(game, card, cardSize, area, instrBg.gameObject);

            // ── Cuenta regresiva ──
            yield return new WaitForSeconds(0.3f);
            var countText = UIKit.Text("Countdown", overlay, string.Empty, 150f, Palette.Hazmat, new Vector2(900f, 200f), new Vector2(0f, 10f), TextAlignmentOptions.Center, true);
            for (int i = 3; i >= 1; i--)
            {
                countText.text = i.ToString();
                Sfx.Play(SfxId.Tick);
                yield return Tween.Run(0.5f, u =>
                {
                    countText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, Ease.OutCubic(u * 2f));
                    countText.alpha = 1f - Mathf.Clamp01((u - 0.7f) / 0.3f);
                });
            }
            countText.text = "¡A COCINAR!";
            countText.fontSize = 110f;
            countText.color = Palette.BBGreenBright;
            Sfx.Play(SfxId.Go);
            Host_StartFade(countText, 0.5f);

            // ── Juego ──
            yield return game.Play();

            try
            {
                game.Cleanup();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            Sfx.StopAllLoops();

            // ── Sello de calificación ──
            yield return new WaitForSeconds(0.35f);
            float score = game.Score;
            var dim = UIKit.Box("StampDim", overlay, new Color(0f, 0f, 0f, 0.55f), cardSize, Vector2.zero);
            Color rc = PizzaScore.StepRatingColor(score);
            var stampBorder = UIKit.Box("StampBorder", overlay, rc, new Vector2(640f, 230f), new Vector2(0f, 10f));
            UIKit.Box("StampFill", stampBorder.transform, Palette.Panel, new Vector2(626f, 216f), Vector2.zero);
            UIKit.Text("Rating", stampBorder.transform, PizzaScore.StepRating(score), 76f, rc, new Vector2(620f, 100f), new Vector2(0f, 38f), TextAlignmentOptions.Center, true);
            UIKit.Text("Purity", stampBorder.transform, $"Pureza del paso: {score * 100f:0}%", 30f, Color.white, new Vector2(620f, 50f), new Vector2(0f, -52f));

            Sfx.Play(SfxId.Stamp);
            Sfx.Play(score >= 0.75f ? SfxId.Perfect : score >= 0.5f ? SfxId.Good : SfxId.Miss, 0.8f);
            var stampRt = stampBorder.rectTransform;
            yield return Tween.Run(0.28f, u =>
            {
                dim.color = new Color(0f, 0f, 0f, 0.55f * u);
                stampRt.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, Ease.OutCubic(u));
                stampRt.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-18f, -5f, u));
            });
            StartCoroutine(Tween.Shake(cardRoot, 10f, 0.25f));
            if (score >= 0.75f)
            {
                UIKit.Burst(this, overlay, new Vector2(0f, 10f), rc, 40, 700f, 14f, -600f, 1.1f, Palette.Hazmat);
            }

            yield return new WaitForSeconds(1.7f);

            // ── Salida ──
            yield return Tween.Run(0.25f, u =>
            {
                group.alpha = 1f - u;
                cardRoot.localScale = Vector3.one * Mathf.Lerp(1f, 0.92f, u);
            });

            Destroy(canvas.gameObject);
        }

        private IEnumerator Briefing(Minigame game, RectTransform card, Vector2 cardSize, RectTransform area, GameObject footer)
        {
            area.gameObject.SetActive(false);
            footer.SetActive(false);

            float pw = cardSize.x - 60f;
            float ph = 600f;
            float left = -pw / 2f + 40f;
            float top = ph / 2f;
            float textW = pw - 80f;

            var panel = UIKit.Rect("Briefing", card, new Vector2(pw, ph), new Vector2(0f, -70f));
            UIKit.Box("BriefBorder", panel, Palette.BBGreen.WithAlpha(0.5f), new Vector2(pw + 4f, ph + 4f), Vector2.zero);
            UIKit.Box("BriefFill", panel, new Color(0f, 0f, 0f, 0.42f), new Vector2(pw, ph), Vector2.zero);

            Func<string, string, float, Color, float, float, TextMeshProUGUI> leftText = (name, text, size, color, y, w) =>
                UIKit.Text(name, panel, text, size, color, new Vector2(w, size * 1.7f), new Vector2(left + w / 2f, y), TextAlignmentOptions.Left, true);

            leftText("GoalLabel", "OBJETIVO", 20f, Palette.Hazmat, top - 26f, 300f);
            var goal = leftText("Goal", GameInput.Adapt(game.Goal), 32f, Color.white, top - 68f, textW);
            goal.textWrappingMode = TextWrappingModes.NoWrap;

            var rescue = UIKit.Text("Rescue", panel, $"RESCATE DE JESSE  ·  PUREZA MÍNIMA {PizzaScore.PassPurity:0}%", 18f, Palette.MethBlue,
                new Vector2(620f, 26f), new Vector2(pw / 2f - 40f - 310f, top - 26f), TextAlignmentOptions.Right, true);
            rescue.textWrappingMode = TextWrappingModes.NoWrap;

            UIKit.Box("Divider", panel, Palette.BBGreen.WithAlpha(0.7f), new Vector2(textW, 3f), new Vector2(0f, top - 100f));

            leftText("RulesLabel", "REGLAS", 20f, Palette.Hazmat, top - 128f, 300f);

            string[] rules = game.Rules;
            for (int i = 0; i < rules.Length && i < 4; i++)
            {
                float y = top - 182f - i * 64f;
                var dot = UIKit.Dot("RuleDot" + i, panel, Palette.BBGreen, 40f, new Vector2(left + 20f, y));
                UIKit.Text("RuleNum", dot.transform, (i + 1).ToString(), 24f, Color.white, new Vector2(40f, 40f), Vector2.zero, TextAlignmentOptions.Center, true);
                var rt = UIKit.Text("Rule" + i, panel, GameInput.Adapt(rules[i]), 25f, Color.white, new Vector2(textW - 70f, 60f), new Vector2(left + 56f + (textW - 70f) / 2f, y), TextAlignmentOptions.Left);
                rt.richText = true;
            }

            float keysY = top - 438f;
            leftText("KeysLabel", "CONTROLES", 20f, Palette.Hazmat, keysY, 200f);
            float x = left + 180f;
            foreach (string key in (GameInput.UsingGamepad ? game.GamepadKeys : game.Keys))
            {
                x += UIKit.KeyCap(panel, key, new Vector2(x, keysY), 44f, 22f) + 12f;
            }

            if (!string.IsNullOrEmpty(game.Tip))
            {
                var tip = leftText("Tip", "<color=#F4C430><b>CONSEJO:</b></color>  " + GameInput.Adapt(game.Tip), 22f, new Color(1f, 1f, 1f, 0.8f), top - 490f, textW);
                tip.richText = true;
                tip.fontStyle = FontStyles.Normal;
            }

            var button = UIKit.MakeButton(panel, "¡LISTO PARA COCINAR!", new Vector2(440f, 66f), new Vector2(0f, -ph / 2f + 46f), Palette.BBGreen, 28f);
            UIKit.Text("EnterHint", panel, GameInput.Label("o presioná ENTER", "o apretá X"), 20f, Palette.TextDim, new Vector2(300f, 30f), new Vector2(380f, -ph / 2f + 46f), TextAlignmentOptions.Left);

            Sfx.Play(SfxId.Whoosh, 0.5f, 1.2f);
            yield return Tween.Run(0.3f, u => panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.94f, 1f, Ease.OutBack(u)));

            // Se ignoran los primeros instantes para que el click con el que
            // se abrió el minijuego no lo cierre sin leer.
            float armed = 0.5f;
            bool ready = false;
            button.Focused = true; // con el control, X = LISTO
            while (!ready)
            {
                armed -= Time.deltaTime;
                bool canAct = armed <= 0f;

                bool clicked = button.Tick(canAct);
                bool enter = canAct && GameInput.ConfirmPressed;

                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
                button.Fill.color = Color.Lerp(Palette.BBGreen, Palette.BBGreenBright, pulse * 0.55f);

                ready = clicked || enter;
                yield return null;
            }

            Sfx.Play(SfxId.Go, 0.7f);
            yield return Tween.Run(0.2f, u => panel.localScale = Vector3.one * Mathf.Lerp(1f, 1.04f, u));

            Destroy(panel.gameObject);
            area.gameObject.SetActive(true);
            footer.SetActive(true);
        }

        private void Host_StartFade(TextMeshProUGUI t, float duration)
        {
            StartCoroutine(Tween.Run(duration, u =>
            {
                if (t == null) return;
                t.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, Ease.OutCubic(u));
                t.alpha = 1f - Ease.InCubic(u);
            }));
        }
    }
}
