using BreakingPizza.Audio;
using BreakingPizza.Controls;
using BreakingPizza.Core;
using BreakingPizza.Minigames;
using BreakingPizza.Player;
using BreakingPizza.UI.Kit;
using TMPro;
using UnityEngine;

namespace BreakingPizza.UI
{
    // Menú de pausa y ayuda.
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private string mainMenuScene = "MainMenu";

        public static bool IsPaused { get; private set; }

        private CanvasGroup group;
        private RectTransform panel;
        private UIButton[] buttons;
        private TextMeshProUGUI hint;
        private TextMeshProUGUI difficultyText;
        private int focus;
        private float visible;

        private RectTransform help;
        private TextMeshProUGUI helpObjective;
        private UIButton helpBack;
        private bool helpOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsPaused = false;
        }

        private void Awake()
        {
            var canvas = UIKit.CreateCanvas("PauseCanvas", 90, transform);
            var root = (RectTransform)canvas.transform;
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            canvas.enabled = false;

            var dim = UIKit.Image("Dim", root, null, new Color(0f, 0f, 0f, 0.72f), Vector2.zero, Vector2.zero);
            UIKit.Stretch(dim.rectTransform);

            panel = UIKit.Rect("Panel", root, new Vector2(600f, 560f), Vector2.zero);
            UIKit.Box("Border", panel, Palette.BBGreen.WithAlpha(0.8f), new Vector2(608f, 568f), Vector2.zero);
            UIKit.Box("Fill", panel, Palette.Panel, new Vector2(600f, 560f), Vector2.zero);
            var stripe = UIKit.Image("Stripe", panel, UIKit.Stripes, Color.white, new Vector2(560f, 12f), new Vector2(0f, 258f));
            stripe.pixelsPerUnitMultiplier = 4f;

            var tile = UIKit.ElementTile(panel, new Vector2(86f, 96f), new Vector2(-230f, 190f), 0, "Ps", "Pausa");
            tile.Fill.color = Palette.BBGreen;
            tile.Border.color = Palette.BBGreenBright;
            UIKit.Text("Title", panel, "PAUSA", 60f, Color.white, new Vector2(400f, 70f), new Vector2(40f, 212f), TextAlignmentOptions.Left, true);
            difficultyText = UIKit.Text("Sub", panel, string.Empty, 19f, Palette.Hazmat, new Vector2(400f, 28f), new Vector2(40f, 158f), TextAlignmentOptions.Left, true);
            difficultyText.textWrappingMode = TextWrappingModes.NoWrap;

            buttons = new[]
            {
                UIKit.MakeButton(panel, "CONTINUAR", new Vector2(440f, 70f), new Vector2(0f, 82f), Palette.BBGreen, 30f),
                UIKit.MakeButton(panel, "AYUDA", new Vector2(440f, 70f), new Vector2(0f, -2f), Palette.PanelLight, 30f),
                UIKit.MakeButton(panel, "MENÚ PRINCIPAL", new Vector2(440f, 70f), new Vector2(0f, -86f), Palette.PanelLight, 30f),
                UIKit.MakeButton(panel, "SALIR DEL JUEGO", new Vector2(440f, 70f), new Vector2(0f, -170f), Palette.TomatoDark, 30f),
            };

            BuildHelp(root);

            hint = UIKit.Text("Hint", panel, string.Empty, 18f, new Color(1f, 1f, 1f, 0.45f), new Vector2(560f, 26f), new Vector2(0f, -236f), TextAlignmentOptions.Center);
        }

        // Pantalla de AYUDA: qué hacer ahora + controles.
        private void BuildHelp(RectTransform root)
        {
            help = UIKit.Rect("Help", root, new Vector2(1440f, 900f), Vector2.zero);
            UIKit.Box("Border", help, Palette.BBGreen.WithAlpha(0.8f), new Vector2(1448f, 908f), Vector2.zero);
            UIKit.Box("Fill", help, Palette.Panel, new Vector2(1440f, 900f), Vector2.zero);
            var stripe = UIKit.Image("Stripe", help, UIKit.Stripes, Color.white, new Vector2(1400f, 12f), new Vector2(0f, 428f));
            stripe.pixelsPerUnitMultiplier = 4f;

            UIKit.Text("Title", help, "AYUDA", 52f, Color.white, new Vector2(1300f, 64f), new Vector2(0f, 370f), TextAlignmentOptions.Center, true);
            UIKit.Text("NowLabel", help, "¿QUÉ HAGO AHORA?", 18f, Palette.Hazmat, new Vector2(1300f, 26f), new Vector2(0f, 318f), TextAlignmentOptions.Center, true);
            helpObjective = UIKit.Text("Objective", help, string.Empty, 28f, Palette.MethBlue, new Vector2(1300f, 40f), new Vector2(0f, 284f), TextAlignmentOptions.Center, true);

            ControlsTable.Build(help, 214f, 1300f, 56f);

            helpBack = UIKit.MakeButton(help, "VOLVER", new Vector2(300f, 66f), new Vector2(0f, -385f), Palette.PanelLight, 26f);
            help.gameObject.SetActive(false);
        }

        // Pista de la etapa actual, para la AYUDA.
        private static string ObjectiveHint()
        {
            var gm = GameManager.Instance;
            if (gm == null) return string.Empty;
            switch (gm.CurrentStage)
            {
                case GameStage.Desert: return "Entrá a la casa rodante: acercate a la puerta y apretá E.";
                case GameStage.Dressing: return "Agarrá la máscara (E) y ponétela (K).";
                case GameStage.Kitchen: return PeriodicChecklistUI.CurrentHint();
                default: return string.Empty;
            }
        }

        private void SetHelp(bool open)
        {
            helpOpen = open;
            help.gameObject.SetActive(open);
            panel.gameObject.SetActive(!open);
            if (open) helpObjective.text = ObjectiveHint();
            Sfx.Play(SfxId.Whoosh, 0.4f, open ? 1.15f : 1f);
        }

        private void OnDestroy()
        {
            // Por si se destruye con el juego pausado (cambio de escena).
            if (IsPaused) RestoreTime();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            visible = Mathf.MoveTowards(visible, IsPaused ? 1f : 0f, dt * 7f);
            group.alpha = visible;
            group.GetComponent<Canvas>().enabled = visible > 0.001f;
            panel.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, visible);

            if (!IsPaused)
            {
                if (GameInput.PausePressed && CanPause()) Open();
                return;
            }

            if (helpOpen)
            {
                hint.text = string.Empty;
                helpBack.Focused = true;
                bool click = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
                if (helpBack.Tick() || GameInput.PausePressed || GameInput.BackPressed || (GameInput.ConfirmPressed && !click)) SetHelp(false);
                return;
            }

            if (GameInput.PausePressed || GameInput.BackPressed)
            {
                Close();
                return;
            }

            if (GameInput.NavY > 0) focus = (focus + buttons.Length - 1) % buttons.Length;
            if (GameInput.NavY < 0) focus = (focus + 1) % buttons.Length;

            for (int i = 0; i < buttons.Length; i++)
            {
                // Con el mouse, el botón bajo el puntero pasa a ser el elegido.
                if (!GameInput.UsingGamepad && buttons[i].Hovered) focus = i;
                buttons[i].Focused = i == focus;
                if (buttons[i].Tick())
                {
                    Activate(i);
                    return;
                }
            }

            bool mouseClick = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
            if (GameInput.ConfirmPressed && !mouseClick) Activate(focus);

            hint.text = GameInput.Label("ESC: volver al juego   ·   FLECHAS + ENTER: elegir", "OPTIONS o CÍRCULO: volver   ·   CRUCETA + X: elegir");
        }

        private static bool CanPause()
        {
            if (MinigameHost.IsRunning || ScreenFader.IsTransitioning) return false;
            var gm = GameManager.Instance;
            return gm != null && gm.CurrentStage != GameStage.Result;
        }

        private void Open()
        {
            IsPaused = true;
            focus = 0;
            if (helpOpen) SetHelp(false);
            difficultyText.text = $"{Difficulty.Name}  ·  PUREZA MÍNIMA {PizzaScore.PassPurity:0}%";
            Time.timeScale = 0f;
            AudioListener.pause = true;
            FirstPersonController.Instance?.SetGameplayInputEnabled(false);
        }

        private void Close()
        {
            RestoreTime();
            FirstPersonController.Instance?.SetGameplayInputEnabled(true);
        }

        private static void RestoreTime()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void Activate(int index)
        {
            switch (index)
            {
                case 0:
                    Close();
                    break;
                case 1:
                    SetHelp(true);
                    break;
                case 2:
                    RestoreTime();
                    ScreenFader.LoadScene(mainMenuScene);
                    break;
                case 3:
                    RestoreTime();
                    GameQuit.Quit();
                    break;
            }
        }
    }

    // Cierra el juego con un fundido a negro. En el Editor detiene el Play
    // (Application.Quit no hace nada en el Editor).
    public static class GameQuit
    {
        public static void Quit()
        {
            ScreenFader.FadeThroughBlack(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }
    }
}
