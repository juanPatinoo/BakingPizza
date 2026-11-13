using System.Collections;
using BreakingPizza.Audio;
using BreakingPizza.Core;
using BreakingPizza.UI.Kit;
using TMPro;
using UnityEngine;

namespace BreakingPizza.UI
{
    // Placa de lugar al llegar al desierto.
    public class ObjectiveUI : MonoBehaviour
    {
        private const string Title = "DESIERTO DE NUEVO MÉXICO";
        private const string Subtitle = "Afueras de Albuquerque";
        private const float CharsPerSecond = 16f;

        private static readonly Color TitleColor = new Color(1f, 0.83f, 0.38f);
        private static readonly Color SubtitleColor = new Color(1f, 0.94f, 0.78f);

        private CanvasGroup cardGroup;
        private TextMeshProUGUI titleText, subtitleText;
        private RectTransform tape;
        private bool shown;

        private void Awake()
        {
            var canvas = UIKit.CreateCanvas("LocationCanvas", 9, transform);
            var root = (RectTransform)canvas.transform;

            var card = UIKit.Anchor(UIKit.Rect("LocationCard", root, new Vector2(900f, 170f), new Vector2(-70f, 80f)), new Vector2(1f, 0f));
            cardGroup = card.gameObject.AddComponent<CanvasGroup>();
            cardGroup.alpha = 0f;

            // Sombra suave detrás, hacia la esquina.
            UIKit.Image("Shade", card, UIKit.Soft, new Color(0.08f, 0.04f, 0f, 0.6f), new Vector2(1100f, 360f), new Vector2(120f, 10f));

            titleText = UIKit.Text("Title", card, string.Empty, 50f, TitleColor, new Vector2(900f, 64f), new Vector2(0f, 38f), TextAlignmentOptions.Right, true);
            titleText.characterSpacing = 6f;
            subtitleText = UIKit.Text("Subtitle", card, string.Empty, 28f, SubtitleColor, new Vector2(900f, 40f), new Vector2(0f, -14f), TextAlignmentOptions.Right);
            subtitleText.fontStyle = FontStyles.Italic;
            subtitleText.characterSpacing = 3f;

            var stripe = UIKit.Image("Tape", card, UIKit.Stripes, new Color(1f, 0.85f, 0.45f, 0.9f), new Vector2(420f, 6f), new Vector2(240f, -50f));
            stripe.pixelsPerUnitMultiplier = 6f;
            tape = stripe.rectTransform;
            tape.localScale = new Vector3(0f, 1f, 1f);
            tape.pivot = new Vector2(1f, 0.5f);
            tape.anchoredPosition = new Vector2(450f, -50f);
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
            if (stage != GameStage.Desert || shown) return;
            shown = true;
            StartCoroutine(LocationCard());
        }

        private IEnumerator LocationCard()
        {
            // Espera a que termine el fundido de entrada a la escena.
            yield return new WaitForSeconds(1.6f);
            cardGroup.alpha = 1f;

            yield return Type(titleText, Title, 1f);
            yield return new WaitForSeconds(0.25f);
            yield return Type(subtitleText, Subtitle, 0.75f);
            yield return Tween.Run(0.5f, u => tape.localScale = new Vector3(Ease.OutCubic(u), 1f, 1f));

            // Cursor titilando un rato al final del subtítulo.
            for (float t = 0f; t < 3.5f; t += Time.deltaTime)
            {
                subtitleText.text = Subtitle + (Mathf.Repeat(t, 0.8f) < 0.4f ? "_" : " ");
                yield return null;
            }

            // Se borra hacia atrás, rápido.
            yield return Tween.Run(0.3f, u => tape.localScale = new Vector3(1f - u, 1f, 1f));
            yield return Erase(subtitleText, Subtitle);
            yield return Erase(titleText, Title);
            cardGroup.alpha = 0f;
        }

        // Máquina de escribir: letra por letra, con "_" de cursor y tecleo.
        private IEnumerator Type(TextMeshProUGUI target, string text, float volume)
        {
            for (int i = 1; i <= text.Length; i++)
            {
                target.text = text.Substring(0, i) + "_";
                if (text[i - 1] != ' ') Sfx.Play(SfxId.Count, 0.22f * volume, Random.Range(0.75f, 1.05f));
                float wait = 1f / CharsPerSecond * Random.Range(0.7f, 1.4f);
                if (text[i - 1] == ' ') wait *= 1.6f;
                yield return new WaitForSeconds(wait);
            }
            target.text = text;
        }

        private static IEnumerator Erase(TextMeshProUGUI target, string text)
        {
            for (int i = text.Length; i >= 0; i--)
            {
                target.text = text.Substring(0, i) + (i > 0 ? "_" : string.Empty);
                yield return new WaitForSeconds(0.022f);
            }
        }
    }
}
