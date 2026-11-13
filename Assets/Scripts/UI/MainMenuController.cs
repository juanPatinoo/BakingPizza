using BreakingPizza.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using BreakingPizza.Audio;
using BreakingPizza.Core;
using BreakingPizza.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BreakingPizza.UI
{
    // Menú principal.
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string sceneToLoad = "IntroVideo";
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeDuration = 0.6f;

        private enum Page { Title, Main, Difficulty, Controls }

        private class MenuItem
        {
            public RectTransform Rt;
            public TextMeshProUGUI Label;
            public TextMeshProUGUI Sub;
            public float Highlight;
        }

        private class Card
        {
            public DifficultyLevel Level;
            public RectTransform Rt;
            public Image Border;
            public Image Fill;
            public ElementTile Tile;
            public float Highlight;
        }

        private Page page = Page.Title;
        private bool loading;
        private float quitArmed;    // > 0: ya se apretó Esc una vez
        private float pageTime;     // segundos desde que se abrió la página actual

        private RectTransform root;
        private CanvasGroup titleGroup, mainGroup, diffGroup, controlsGroup;
        private Image modalDim;
        private RawImage modalBlur;
        private Texture menuVideo;          // la textura del video de fondo de la escena
        private RenderTexture modalBlurRT;
        private TextMeshProUGUI pressText;
        private TextMeshProUGUI hint;

        private readonly List<MenuItem> items = new List<MenuItem>();
        private int selectedItem;
        private RectTransform selector;

        private readonly List<Card> cards = new List<Card>();
        private DifficultyLevel selectedLevel;
        private TextMeshProUGUI blurb;
        private UIButton startButton, backButton, controlsBackButton;

        private void Start()
        {
            if (fadeCanvasGroup != null)
            {
                // Si se llegó con la transición suave (ScreenFader), el panel
                // negro de la escena no hace falta: se apaga directo.
                if (ScreenFader.IsTransitioning)
                {
                    fadeCanvasGroup.alpha = 0f;
                    fadeCanvasGroup.blocksRaycasts = false;
                }
                else
                {
                    fadeCanvasGroup.blocksRaycasts = true;
                    StartCoroutine(FadeRoutine(1f, 0f, () => fadeCanvasGroup.blocksRaycasts = false));
                }
            }

            // Se ocultan los botones viejos de Unity que tenía la escena.
            foreach (var oldButton in FindObjectsByType<Button>())
            {
                if (oldButton.gameObject.scene == gameObject.scene) oldButton.gameObject.SetActive(false);
            }

            selectedLevel = Difficulty.Current;
            Build();

            // Si se vuelve al menú desde el juego, se saltea la pantalla de título.
            if (ScreenFader.IsTransitioning) SetPage(Page.Main, silent: true);
        }

        // Compatibilidad con el botón viejo de la escena.
        public void OnPlayButtonPressed()
        {
            if (fadeCanvasGroup != null) fadeCanvasGroup.blocksRaycasts = true;
            ScreenFader.LoadScene(sceneToLoad);
        }

        // ── Construcción ──

        private void Build()
        {
            var canvas = UIKit.CreateCanvas("MenuCanvas", 5, transform);
            root = (RectTransform)canvas.transform;

            BuildTitle();
            BuildMain();
            BuildModalDim();
            BuildDifficulty();
            BuildControls();

            // Ayuda de teclas abajo a la derecha.
            hint = UIKit.Text("Hint", root, string.Empty, 16f, new Color(1f, 1f, 1f, 0.45f), new Vector2(900f, 24f), Vector2.zero, TextAlignmentOptions.Right);
            UIKit.Anchor(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0.5f));
            hint.rectTransform.anchoredPosition = new Vector2(-40f, 32f);
        }

        private CanvasGroup Group(string name)
        {
            var rt = UIKit.Stretch(UIKit.Rect(name, root, Vector2.zero, Vector2.zero));
            var g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0f;
            g.blocksRaycasts = false;
            return g;
        }

        private void BuildTitle()
        {
            titleGroup = Group("Title");
            var t = titleGroup.transform;
            UIKit.Image("Shade", t, UIKit.Soft, new Color(0f, 0f, 0f, 0.6f), new Vector2(1100f, 220f), new Vector2(0f, -425f));
            pressText = UIKit.Text("Press", t, string.Empty, 30f, Color.white, new Vector2(1000f, 50f), new Vector2(0f, -425f), TextAlignmentOptions.Center, true);
            var tape = UIKit.Image("Tape", t, UIKit.Stripes, Color.white, new Vector2(420f, 6f), new Vector2(0f, -462f));
            tape.pixelsPerUnitMultiplier = 6f;
        }

        private void BuildMain()
        {
            mainGroup = Group("Main");
            var g = (RectTransform)mainGroup.transform;

            // Sombra a la izquierda para que el texto se lea sobre el humo.
            UIKit.Image("Shade", g, UIKit.Soft, new Color(0f, 0f, 0f, 0.85f), new Vector2(1500f, 1900f), new Vector2(-1020f, -40f));

            var column = UIKit.Rect("Column", g, new Vector2(600f, 500f), new Vector2(120f, -170f));
            UIKit.Anchor(column, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));

            UIKit.Text("Section", column, "MENÚ PRINCIPAL", 18f, Palette.Hazmat, new Vector2(560f, 26f), new Vector2(280f, 210f), TextAlignmentOptions.Left, true);
            var tape = UIKit.Image("Tape", column, UIKit.Stripes, Color.white, new Vector2(260f, 6f), new Vector2(130f, 188f));
            tape.pixelsPerUnitMultiplier = 6f;

            selector = UIKit.Box("Selector", column, Palette.Hazmat, new Vector2(8f, 74f), new Vector2(-22f, 100f)).rectTransform;

            AddItem(column, "JUGAR", "Rescatá a Jesse", 110f);
            AddItem(column, "CONTROLES", "Teclado y control", 0f);
            AddItem(column, "SALIR", "Volver al escritorio", -110f);

            // Las X de la columna se miden desde su borde izquierdo.
            for (int i = 0; i < column.childCount; i++)
            {
                var child = (RectTransform)column.GetChild(i);
                child.anchorMin = child.anchorMax = new Vector2(0f, 0.5f);
            }
        }

        private void AddItem(RectTransform column, string label, string sub, float y)
        {
            var item = new MenuItem();
            item.Rt = UIKit.Rect("Item_" + label, column, new Vector2(560f, 96f), new Vector2(280f, y));
            item.Label = UIKit.Text("Label", item.Rt, label, 64f, Color.white, new Vector2(560f, 70f), new Vector2(0f, 10f), TextAlignmentOptions.Left, true);
            item.Sub = UIKit.Text("Sub", item.Rt, sub, 19f, Palette.TextDim, new Vector2(560f, 26f), new Vector2(0f, -34f), TextAlignmentOptions.Left);
            items.Add(item);
        }

        // Fondo de las pantallas de DIFICULTAD y CONTROLES: el video del menú
        // difuminado, con un velo verde oscuro (los colores de la serie).
        private void BuildModalDim()
        {
            foreach (var raw in FindObjectsByType<RawImage>())
            {
                if (raw.gameObject.scene == gameObject.scene && raw.texture is RenderTexture)
                {
                    menuVideo = raw.texture;
                    break;
                }
            }

            if (menuVideo != null)
            {
                modalBlurRT = Blur.CreateTarget(480, 270, "MenuBlur");
                modalBlur = UIKit.Rect("ModalBlur", root, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
                UIKit.Stretch(modalBlur.rectTransform);
                modalBlur.texture = modalBlurRT;
                modalBlur.raycastTarget = false;
                modalBlur.color = new Color(1f, 1f, 1f, 0f);
            }

            modalDim = UIKit.Image("ModalTint", root, null, new Color(0.01f, 0.06f, 0.03f, 0f), Vector2.zero, Vector2.zero);
            UIKit.Stretch(modalDim.rectTransform);
            UIKit.Image("ModalVignette", modalDim.transform, UIKit.Soft, new Color(0.15f, 0.55f, 0.28f, 0.18f), new Vector2(1800f, 1300f), Vector2.zero);
        }

        private void OnDestroy()
        {
            if (modalBlurRT != null) modalBlurRT.Release();
        }

        private void BuildDifficulty()
        {
            diffGroup = Group("Difficulty");
            var g = (RectTransform)diffGroup.transform;

            UIKit.Text("Title", g, "ELEGÍ LA DIFICULTAD", 56f, Color.white, new Vector2(1400f, 70f), new Vector2(0f, 360f), TextAlignmentOptions.Center, true);
            var tape = UIKit.Image("Tape", g, UIKit.Stripes, Color.white, new Vector2(520f, 7f), new Vector2(0f, 318f));
            tape.pixelsPerUnitMultiplier = 6f;
            UIKit.Text("Sub", g, "Cuanto más difícil, más rápidos los minijuegos y más pureza necesitás para rescatar a Jesse.", 22f, Palette.TextDim,
                new Vector2(1400f, 30f), new Vector2(0f, 284f), TextAlignmentOptions.Center);

            for (int i = 0; i < 4; i++)
            {
                cards.Add(BuildCard((DifficultyLevel)i, new Vector2(-555f + i * 370f, 20f)));
            }

            blurb = UIKit.Text("Blurb", g, string.Empty, 26f, Color.white, new Vector2(1400f, 40f), new Vector2(0f, -258f), TextAlignmentOptions.Center);
            blurb.fontStyle = FontStyles.Italic;

            backButton = UIKit.MakeButton(g, "VOLVER", new Vector2(300f, 74f), new Vector2(-190f, -360f), Palette.PanelLight, 28f);
            startButton = UIKit.MakeButton(g, "COMENZAR", new Vector2(360f, 74f), new Vector2(200f, -360f), Palette.BBGreen, 34f);
        }

        private Card BuildCard(DifficultyLevel level, Vector2 pos)
        {
            Color c = Difficulty.ColorOf(level);
            var card = new Card { Level = level };
            card.Border = UIKit.Box("Card_" + level, diffGroup.transform, c.WithAlpha(0.5f), new Vector2(338f, 438f), pos);
            card.Rt = card.Border.rectTransform;
            card.Fill = UIKit.Box("Fill", card.Rt, new Color(0.02f, 0.05f, 0.035f, 0.95f), new Vector2(330f, 430f), Vector2.zero);

            string[] symbols = { "Fa", "Me", "Df", "Im" };
            card.Tile = UIKit.ElementTile(card.Rt, new Vector2(124f, 136f), new Vector2(0f, 118f), Mathf.RoundToInt(Difficulty.RequiredFor(level)), symbols[(int)level], "% pureza");

            UIKit.Text("Name", card.Rt, Difficulty.NameOf(level), 38f, Color.white, new Vector2(320f, 50f), new Vector2(0f, 8f), TextAlignmentOptions.Center, true);
            UIKit.Text("PurityLabel", card.Rt, "PUREZA MÍNIMA", 15f, Palette.TextDim, new Vector2(320f, 22f), new Vector2(0f, -32f), TextAlignmentOptions.Center, true);
            UIKit.Text("Purity", card.Rt, $"{Difficulty.RequiredFor(level):0}%", 44f, c, new Vector2(320f, 52f), new Vector2(0f, -68f), TextAlignmentOptions.Center, true);

            float speed = Difficulty.SpeedFor(level);
            float time = Difficulty.TimeFactorFor(level);
            UIKit.Text("Pace", card.Rt, $"RITMO  x{speed:0.00}     TIEMPO  {time * 100f:0}%", 16f, Color.white, new Vector2(320f, 24f), new Vector2(0f, -118f), TextAlignmentOptions.Center);

            string record = Records.TryGet(level, out float bestPurity, out float bestTime)
                ? $"RÉCORD  {bestPurity:0.0}%  ·  {PizzaScore.FormatTime(bestTime)}"
                : "SIN RÉCORD TODAVÍA";
            UIKit.Text("Record", card.Rt, record, 16f, Palette.MethBlue, new Vector2(320f, 24f), new Vector2(0f, -170f), TextAlignmentOptions.Center, true);
            return card;
        }

        private void BuildControls()
        {
            controlsGroup = Group("Controls");
            var g = (RectTransform)controlsGroup.transform;

            UIKit.Text("Title", g, "CONTROLES", 56f, Color.white, new Vector2(1400f, 70f), new Vector2(0f, 380f), TextAlignmentOptions.Center, true);
            var tape = UIKit.Image("Tape", g, UIKit.Stripes, Color.white, new Vector2(360f, 7f), new Vector2(0f, 338f));
            tape.pixelsPerUnitMultiplier = 6f;

            ControlsTable.Build(g, 270f);

            controlsBackButton = UIKit.MakeButton(g, "VOLVER", new Vector2(300f, 74f), new Vector2(0f, -390f), Palette.PanelLight, 28f);
        }

        // ── Páginas ──

        private void SetPage(Page next, bool silent = false)
        {
            page = next;
            pageTime = 0f;
            quitArmed = 0f;
            if (!silent) Sfx.Play(SfxId.Whoosh, 0.45f, next == Page.Main ? 1f : 1.15f);

            if (next == Page.Difficulty)
            {
                selectedLevel = Difficulty.Current;
                foreach (var c in cards) c.Rt.localScale = Vector3.one * 0.9f;
            }
            if (next == Page.Main)
            {
                for (int i = 0; i < items.Count; i++) items[i].Rt.anchoredPosition = new Vector2(220f, items[i].Rt.anchoredPosition.y);
            }
        }

        private void Update()
        {
            if (root == null) return;
            float dt = Time.unscaledDeltaTime;
            pageTime += dt;

            // Fundidos de cada página (y el velo oscuro de las pantallas modales).
            Fade(titleGroup, page == Page.Title, dt);
            Fade(mainGroup, page == Page.Main, dt);
            Fade(diffGroup, page == Page.Difficulty, dt);
            Fade(controlsGroup, page == Page.Controls, dt);
            bool modal = page == Page.Difficulty || page == Page.Controls;
            float m = Mathf.MoveTowards(modalDim.color.a / 0.62f, modal ? 1f : 0f, dt * 3f);
            modalDim.color = new Color(0.01f, 0.06f, 0.03f, 0.62f * m);
            modalDim.transform.GetChild(0).GetComponent<Image>().color = new Color(0.15f, 0.55f, 0.28f, 0.18f * m);
            if (modalBlur != null)
            {
                modalBlur.color = new Color(1f, 1f, 1f, m);
                if (m > 0.001f) Blur.Apply(menuVideo, modalBlurRT, 60);
            }

            AnimateMain(dt);
            AnimateCards(dt);

            if (loading || CreditsRoll.IsShowing) return;
            quitArmed -= dt;

            switch (page)
            {
                case Page.Title: UpdateTitle(); break;
                case Page.Main: UpdateMain(); break;
                case Page.Difficulty: UpdateDifficulty(); break;
                case Page.Controls: UpdateControls(); break;
            }
        }

        private static void Fade(CanvasGroup g, bool on, float dt)
        {
            g.alpha = Mathf.MoveTowards(g.alpha, on ? 1f : 0f, dt * 4f);
            g.blocksRaycasts = on;
        }

        private bool Esc => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        private void UpdateTitle()
        {
            pressText.text = GameInput.Label("PRESIONÁ ENTER PARA EMPEZAR", "PRESIONÁ X PARA EMPEZAR");
            pressText.alpha = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.2f));
            hint.text = string.Empty;

            // Se espera a que termine el fundido de entrada.
            if (pageTime < 1f) return;
            if (Esc) { ArmQuit(); return; }
            if (GameInput.AnyPressed)
            {
                Sfx.Play(SfxId.Go, 0.7f);
                SetPage(Page.Main);
            }
        }

        private void UpdateMain()
        {
            hint.text = quitArmed > 0f
                ? GameInput.Label("Apretá ESC de nuevo para salir del juego", "Apretá CÍRCULO de nuevo para salir del juego")
                : GameInput.Label("FLECHAS  navegar     ENTER  elegir     ESC  salir", "CRUCETA  navegar     X  elegir     CÍRCULO  salir");

            if (pageTime < 0.25f) return;

            if (GameInput.NavY > 0) Select((selectedItem + items.Count - 1) % items.Count);
            if (GameInput.NavY < 0) Select((selectedItem + 1) % items.Count);

            if (!GameInput.UsingGamepad)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (!RectTransformUtility.RectangleContainsScreenPoint(items[i].Rt, GameInput.Pointer, null)) continue;
                    Select(i);
                    if (GameInput.PointerClick) { Activate(i); return; }
                }
            }

            bool mouseClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (GameInput.ConfirmPressed && !mouseClick) { Activate(selectedItem); return; }
            if (Esc || GameInput.BackPressed) ArmQuit();
        }

        private void Select(int index)
        {
            if (index == selectedItem) return;
            selectedItem = index;
            Sfx.Play(SfxId.Count, 0.4f, 1.1f + index * 0.08f);
        }

        private void Activate(int index)
        {
            Sfx.Play(SfxId.Go, 0.7f);
            StartCoroutine(Tween.Punch(items[index].Rt, 0.08f, 0.25f));
            switch (index)
            {
                case 0: SetPage(Page.Difficulty); break;
                case 1: SetPage(Page.Controls); break;
                default: Quit(); break;
            }
        }

        private void UpdateDifficulty()
        {
            blurb.text = Difficulty.BlurbOf(selectedLevel);
            hint.text = GameInput.Label("FLECHAS  dificultad     ENTER  comenzar     ESC  volver", "CRUCETA  dificultad     X  comenzar     CÍRCULO  volver");

            if (pageTime < 0.25f) return;

            if (GameInput.NavX < 0) SelectLevel((DifficultyLevel)Mathf.Max(0, (int)selectedLevel - 1));
            if (GameInput.NavX > 0) SelectLevel((DifficultyLevel)Mathf.Min(3, (int)selectedLevel + 1));

            if (!GameInput.UsingGamepad && GameInput.PointerClick)
            {
                foreach (var c in cards)
                {
                    if (!RectTransformUtility.RectangleContainsScreenPoint(c.Rt, GameInput.Pointer, null)) continue;
                    // Click en la carta ya elegida = comenzar.
                    if (c.Level == selectedLevel) { Play(); return; }
                    SelectLevel(c.Level);
                }
            }

            startButton.Focused = true; // con el control, X = comenzar
            startButton.Fill.color = Color.Lerp(Palette.BBGreen, Palette.BBGreenBright, (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.5f)) * 0.5f);
            if (startButton.Tick(!GameInput.UsingGamepad)) { Play(); return; }
            if (backButton.Tick(!GameInput.UsingGamepad) || Esc || GameInput.BackPressed) { SetPage(Page.Main); return; }

            bool mouseClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (GameInput.ConfirmPressed && !mouseClick) Play();
        }

        private void SelectLevel(DifficultyLevel level)
        {
            if (level == selectedLevel) return;
            selectedLevel = level;
            Sfx.Play(SfxId.Good, 0.6f, 0.9f + (int)level * 0.12f);
            var card = cards[(int)level];
            StartCoroutine(Tween.Punch(card.Tile.Root, 0.15f, 0.3f));
        }

        private void UpdateControls()
        {
            hint.text = GameInput.Label("ESC  volver", "CÍRCULO  volver");
            if (pageTime < 0.25f) return;

            controlsBackButton.Focused = true;
            bool mouseClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (controlsBackButton.Tick(!GameInput.UsingGamepad) || Esc || GameInput.BackPressed || (GameInput.ConfirmPressed && !mouseClick))
            {
                SetPage(Page.Main);
            }
        }

        // ── Animación ──

        private void AnimateMain(float dt)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                it.Highlight = Mathf.MoveTowards(it.Highlight, i == selectedItem ? 1f : 0f, dt * 6f);
                float k = Ease.OutCubic(it.Highlight);

                // Entran deslizando desde la izquierda, escalonadas.
                float targetX = 280f + 22f * k;
                float delay = i * 0.07f;
                float slide = page == Page.Main ? Mathf.Clamp01((pageTime - delay) * 4f) : 1f;
                float x = Mathf.Lerp(it.Rt.anchoredPosition.x, targetX, page == Page.Main ? Mathf.Max(slide * 12f * dt, 0f) : 1f);
                it.Rt.anchoredPosition = new Vector2(x, it.Rt.anchoredPosition.y);

                it.Label.color = Color.Lerp(new Color(1f, 1f, 1f, 0.55f), Palette.Hazmat, k);
                it.Sub.alpha = Mathf.Lerp(0.35f, 1f, k);
            }

            float targetY = items[selectedItem].Rt.anchoredPosition.y + 8f;
            selector.anchoredPosition = new Vector2(-22f, Mathf.Lerp(selector.anchoredPosition.y, targetY, 1f - Mathf.Exp(-16f * dt)));
        }

        private void AnimateCards(float dt)
        {
            foreach (var c in cards)
            {
                bool on = c.Level == selectedLevel;
                c.Highlight = Mathf.MoveTowards(c.Highlight, on ? 1f : 0f, dt * 6f);
                float k = Ease.OutCubic(c.Highlight);
                Color col = Difficulty.ColorOf(c.Level);

                c.Rt.localScale = Vector3.Lerp(c.Rt.localScale, Vector3.one * (on ? 1.06f : 0.96f), 1f - Mathf.Exp(-12f * dt));
                c.Border.color = Color.Lerp(col.WithAlpha(0.35f), Color.Lerp(col, Color.white, 0.35f), k);
                c.Fill.color = Color.Lerp(new Color(0.02f, 0.05f, 0.035f, 0.92f), Color.Lerp(new Color(0.02f, 0.05f, 0.035f, 0.95f), col, 0.22f), k);
                c.Tile.Fill.color = Color.Lerp(Palette.BBGreenDark, Palette.BBGreen, k);
                c.Tile.Border.color = Color.Lerp(Palette.BBGreen.WithAlpha(0.6f), Palette.BBGreenBright, k);
            }
        }

        // ── Acciones ──

        private void ArmQuit()
        {
            if (quitArmed > 0f) { Quit(); return; }
            quitArmed = 2f;
            Sfx.Play(SfxId.Alarm, 0.4f);
            if (page == Page.Title) pressText.text = "Apretá ESC de nuevo para salir";
        }

        private void Quit()
        {
            if (loading) return;
            loading = true;
            Sfx.Play(SfxId.Go, 0.6f);
            GameQuit.Quit();
        }

        private void Play()
        {
            if (loading) return;
            loading = true;

            Difficulty.Current = selectedLevel;
            Sfx.Play(SfxId.Go);
            StartCoroutine(Tween.Punch(startButton.Rt, 0.15f, 0.25f));
            StartCoroutine(Tween.Punch(cards[(int)selectedLevel].Rt, 0.08f, 0.3f));
            OnPlayButtonPressed();
        }

        private IEnumerator FadeRoutine(float from, float to, Action onComplete)
        {
            float elapsed = 0f;
            fadeCanvasGroup.alpha = from;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = to;
            onComplete?.Invoke();
        }
    }
}
