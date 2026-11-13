using System.Text.RegularExpressions;
using BreakingPizza.Audio;
using BreakingPizza.Controls;
using BreakingPizza.Minigames;
using BreakingPizza.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BreakingPizza.UI
{
    // Estilo de los carteles de interacción.
    public class PromptStyler : MonoBehaviour
    {
        private TMP_Text source;

        private CanvasGroup group;
        private RectTransform panel;
        private Image border;
        private Image stripe;
        private TextMeshProUGUI label;

        private string lastRaw = string.Empty;
        private bool lastGamepad;
        private float visibility;
        private bool built;
        private Coroutine popRoutine;

        private void Update()
        {
            if (source == null)
            {
                var promptUi = FindAnyObjectByType<InteractionPromptUI>();
                if (promptUi == null) return;
                source = promptUi.GetComponent<TMP_Text>();
            }

            if (!built) Build();

            // Se oculta el texto crudo de la escena (sigue guardando el
            // string, solo deja de dibujarse).
            source.alpha = 0f;

            string raw = source.text ?? string.Empty;
            // Se re-dibuja si cambia el texto o si cambiás entre teclado y control.
            if (raw != lastRaw || GameInput.UsingGamepad != lastGamepad)
            {
                bool wasEmpty = string.IsNullOrEmpty(lastRaw);
                lastRaw = raw;
                lastGamepad = GameInput.UsingGamepad;
                if (!string.IsNullOrEmpty(raw))
                {
                    Apply(raw);
                    if (popRoutine != null) StopCoroutine(popRoutine);
                    popRoutine = StartCoroutine(Tween.Punch(panel, wasEmpty ? 0.12f : 0.05f, 0.25f));
                    if (wasEmpty) Sfx.Play(SfxId.Count, 0.25f, 1.4f);
                }
            }

            bool show = !string.IsNullOrEmpty(raw) && !MinigameHost.IsRunning;
            visibility = Mathf.MoveTowards(visibility, show ? 1f : 0f, 8f * Time.unscaledDeltaTime);
            group.alpha = visibility;
        }

        private void Build()
        {
            built = true;
            var canvas = UIKit.CreateCanvas("PromptCanvas", 20, transform);
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;

            panel = UIKit.Anchor(UIKit.Rect("PromptPanel", canvas.transform, new Vector2(500f, 78f), new Vector2(0f, 190f)), new Vector2(0.5f, 0f));
            UIKit.Image("Shadow", panel, UIKit.Soft, new Color(0f, 0f, 0f, 0.55f), new Vector2(620f, 190f), Vector2.zero);
            border = UIKit.Box("Border", panel, Palette.BBGreen, new Vector2(506f, 84f), Vector2.zero);
            UIKit.Box("Fill", panel, new Color(0.02f, 0.05f, 0.035f, 0.93f), new Vector2(500f, 78f), Vector2.zero);
            stripe = UIKit.Image("Stripe", panel, UIKit.Stripes, Color.white, new Vector2(16f, 66f), new Vector2(-242f, 0f));
            stripe.pixelsPerUnitMultiplier = 5f;

            label = UIKit.Text("Label", panel, string.Empty, 32f, Color.white, new Vector2(460f, 78f), new Vector2(10f, 0f), TextAlignmentOptions.Center, true);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.richText = true;
        }

        private void Apply(string raw)
        {
            bool locked = raw.StartsWith("Faltan") || raw.StartsWith("Equipate") || raw.StartsWith("Equípate");
            label.color = locked ? Palette.Danger : Color.white;
            border.color = locked ? Palette.Danger.WithAlpha(0.85f) : Palette.BBGreen;

            label.text = Format(raw);
            label.ForceMeshUpdate();

            float width = Mathf.Clamp(label.preferredWidth + 96f, 300f, 1500f);
            panel.sizeDelta = new Vector2(width, 78f);
            ResizeChildren(width);
        }

        private void ResizeChildren(float width)
        {
            for (int i = 0; i < panel.childCount; i++)
            {
                var rt = (RectTransform)panel.GetChild(i);
                switch (rt.name)
                {
                    case "Border": rt.sizeDelta = new Vector2(width + 6f, 84f); break;
                    case "Fill": rt.sizeDelta = new Vector2(width, 78f); break;
                    case "Stripe": rt.anchoredPosition = new Vector2(-width / 2f + 20f, 0f); break;
                    case "Label": rt.sizeDelta = new Vector2(width - 60f, 78f); rt.anchoredPosition = new Vector2(12f, 0f); break;
                }
            }
        }

        // Resalta la tecla en amarillo.
        private static string Format(string raw)
        {
            string text = Regex.Replace(raw, @"^Click\b", m => Cap(GameInput.Label("CLICK", "X")));
            text = Regex.Replace(text, @"\b(Presion[eá]|Apret[aá]|Toc[aá]) ([A-Z])\b", m => m.Groups[1].Value + " " + Cap(PadKey(m.Groups[2].Value)));
            text = Regex.Replace(text, @"\((\d+/\d+)\)", m => "<color=#F4C430>(" + m.Groups[1].Value + ")</color>");
            return text;
        }

        private static string PadKey(string key)
        {
            if (!GameInput.UsingGamepad) return key;
            switch (key)
            {
                case "E": return "X";
                case "K": return "TRIÁNGULO";
                default: return key;
            }
        }

        private static string Cap(string key)
        {
            return "<color=#F4C430><b>[" + key + "]</b></color>";
        }
    }
}
