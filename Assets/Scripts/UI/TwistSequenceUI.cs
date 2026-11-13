using BreakingPizza.Controls;
using System.Collections;
using BreakingPizza.Audio;
using BreakingPizza.Core;
using BreakingPizza.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BreakingPizza.UI
{
    // Giro narrativo: el mensaje de Walter.
    [RequireComponent(typeof(TMP_Text))]
    public class TwistSequenceUI : MonoBehaviour
    {
        private struct Line
        {
            public string Text;
            public Color Color;
            public float Hold;

            public Line(string text, Color color, float hold = 1.6f)
            {
                Text = text;
                Color = color;
                Hold = hold;
            }
        }

        [Header("Stage siguiente")]
        [SerializeField] private GameStage nextStage = GameStage.Kitchen;

        private TMP_Text sceneText;
        private bool playing;
        private bool skipRequested;

        private CanvasGroup group;
        private RectTransform topBar, bottomBar, dialog;
        private TextMeshProUGUI body;
        private TextMeshProUGUI caption;
        private TextMeshProUGUI skipHint;

        private Line[] Script => new[]
        {
            new Line("Esperá un segundo.", Color.white),
            new Line("Esto no es lo que pensás que es.", Color.white),
            new Line("No vinimos a cocinar eso... vinimos a hacer una <color=#F4C430><b>PIZZA</b></color>.", Color.white, 2.0f),
            new Line("Secuestraron a <color=#5BD0FF><b>Jesse</b></color>.", Palette.Danger, 1.8f),
            new Line("Quieren una pizza a cambio de su vida.", Color.white, 2.0f),
            new Line("Y la vas a cocinar <color=#F4C430><b>vos</b></color>.", Color.white, 1.8f),
            new Line($"Pureza mínima: <color=#F4C430><b>{PizzaScore.PassPurity:0}%</b></color>. Ni un punto menos.", Color.white, 2.6f),
        };

        private void Awake()
        {
            sceneText = GetComponent<TMP_Text>();
            sceneText.text = string.Empty;
            sceneText.alpha = 0f;
        }

        private void OnEnable()
        {
            GameManager.OnStageChanged += HandleStageChanged;
        }

        private void OnDisable()
        {
            GameManager.OnStageChanged -= HandleStageChanged;
        }

        private void HandleStageChanged(GameStage stage)
        {
            if (stage == GameStage.Twist && !playing)
            {
                StartCoroutine(PlaySequence());
            }
        }

        private void Update()
        {
            // El texto de la escena nunca debe verse (lo dibujamos nosotros).
            if (sceneText != null) sceneText.alpha = 0f;

            if (!playing) return;
            if (GameInput.SkipPressed && !PauseMenu.IsPaused) skipRequested = true;
            if (skipHint != null) skipHint.text = GameInput.Label("CLICK para saltar", "X para saltar");
        }

        private void BuildUI()
        {
            var canvas = UIKit.CreateCanvas("TwistCanvas", 30);
            var root = (RectTransform)canvas.transform;
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            var dim = UIKit.Image("Dim", root, null, new Color(0f, 0f, 0f, 0.38f), Vector2.zero, Vector2.zero);
            UIKit.Stretch(dim.rectTransform);

            // Barras de cine.
            topBar = UIKit.Anchor(UIKit.Rect("TopBar", root, new Vector2(0f, 150f), Vector2.zero), new Vector2(0.5f, 1f));
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.sizeDelta = new Vector2(0f, 150f);
            UIKit.Image("TopFill", topBar, null, Color.black, Vector2.zero, Vector2.zero);
            UIKit.Stretch((RectTransform)topBar.GetChild(0));

            bottomBar = UIKit.Rect("BottomBar", root, new Vector2(0f, 190f), Vector2.zero);
            bottomBar.anchorMin = new Vector2(0f, 0f);
            bottomBar.anchorMax = new Vector2(1f, 0f);
            bottomBar.pivot = new Vector2(0.5f, 0f);
            bottomBar.sizeDelta = new Vector2(0f, 190f);
            UIKit.Image("BottomFill", bottomBar, null, Color.black, Vector2.zero, Vector2.zero);
            UIKit.Stretch((RectTransform)bottomBar.GetChild(0));

            // Cintas de peligro en el borde de cada barra.
            var topTape = UIKit.Image("TopTape", topBar, UIKit.Stripes, Color.white, new Vector2(0f, 10f), Vector2.zero);
            topTape.pixelsPerUnitMultiplier = 5f;
            topTape.rectTransform.anchorMin = new Vector2(0f, 0f);
            topTape.rectTransform.anchorMax = new Vector2(1f, 0f);
            topTape.rectTransform.pivot = new Vector2(0.5f, 0f);
            topTape.rectTransform.sizeDelta = new Vector2(0f, 10f);

            var botTape = UIKit.Image("BottomTape", bottomBar, UIKit.Stripes, Color.white, new Vector2(0f, 10f), Vector2.zero);
            botTape.pixelsPerUnitMultiplier = 5f;
            botTape.rectTransform.anchorMin = new Vector2(0f, 1f);
            botTape.rectTransform.anchorMax = new Vector2(1f, 1f);
            botTape.rectTransform.pivot = new Vector2(0.5f, 1f);
            botTape.rectTransform.sizeDelta = new Vector2(0f, 10f);

            // Diálogo en la barra de abajo: tile + texto.
            dialog = UIKit.Rect("Dialog", bottomBar, new Vector2(1500f, 150f), new Vector2(0f, 0f));
            dialog.anchorMin = dialog.anchorMax = new Vector2(0.5f, 0.5f);
            dialog.anchoredPosition = new Vector2(0f, -4f);

            var tile = UIKit.ElementTile(dialog, new Vector2(92f, 104f), new Vector2(-690f, 0f), 74, "Ww", "Walter");
            tile.Fill.color = Palette.BBGreen;
            tile.Border.color = Palette.BBGreenBright;

            caption = UIKit.Text("Caption", dialog, "WALTER WHITE", 20f, Palette.Hazmat, new Vector2(1250f, 28f), new Vector2(75f, 52f), TextAlignmentOptions.Left, true);
            body = UIKit.Text("Body", dialog, string.Empty, 46f, Color.white, new Vector2(1250f, 100f), new Vector2(75f, -8f), TextAlignmentOptions.Left, true);
            body.richText = true;

            var hint = skipHint = UIKit.Text("SkipHint", root, "CLICK para saltar", 18f, new Color(1f, 1f, 1f, 0.35f), new Vector2(300f, 24f), Vector2.zero, TextAlignmentOptions.Right);
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(1f, 0f);
            hint.rectTransform.pivot = new Vector2(1f, 0.5f);
            hint.rectTransform.anchoredPosition = new Vector2(-30f, 210f);
        }

        private IEnumerator PlaySequence()
        {
            playing = true;
            skipRequested = false;
            if (group == null) BuildUI();

            // Entran las barras de cine.
            Sfx.Play(SfxId.Whoosh, 0.7f);
            yield return Tween.Run(0.6f, u =>
            {
                group.alpha = Mathf.Clamp01(u * 2f);
                float k = Ease.OutCubic(u);
                topBar.anchoredPosition = new Vector2(0f, Mathf.Lerp(150f, 0f, k));
                bottomBar.anchoredPosition = new Vector2(0f, Mathf.Lerp(-190f, 0f, k));
            });

            foreach (Line line in Script)
            {
                if (skipRequested) break;
                yield return TypeLine(line);
            }

            // Salen las barras.
            Sfx.Play(SfxId.Whoosh, 0.6f);
            yield return Tween.Run(0.5f, u =>
            {
                float k = Ease.InCubic(u);
                topBar.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 150f, k));
                bottomBar.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -190f, k));
                group.alpha = 1f - u;
            });

            body.text = string.Empty;
            playing = false;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetStage(nextStage);
            }
        }

        // Máquina de escribir: respeta los tags de rich text (maxVisibleCharacters).
        private IEnumerator TypeLine(Line line)
        {
            body.color = line.Color;
            body.text = line.Text;
            body.maxVisibleCharacters = 0;
            body.ForceMeshUpdate();

            int total = body.textInfo.characterCount;
            float elapsed = 0f;
            int shown = 0;
            const float charsPerSecond = 38f;

            while (shown < total && !skipRequested)
            {
                elapsed += Time.deltaTime;
                int target = Mathf.Min(total, Mathf.FloorToInt(elapsed * charsPerSecond));
                if (target != shown)
                {
                    shown = target;
                    body.maxVisibleCharacters = shown;
                    if (shown % 2 == 0) Sfx.Play(SfxId.Count, 0.18f, Random.Range(0.8f, 1.1f));
                }
                yield return null;
            }

            body.maxVisibleCharacters = total;
            if (line.Color == Palette.Danger) Sfx.Play(SfxId.Alarm, 0.5f);

            float hold = 0f;
            while (hold < line.Hold && !skipRequested)
            {
                hold += Time.deltaTime;
                yield return null;
            }
        }
    }
}
