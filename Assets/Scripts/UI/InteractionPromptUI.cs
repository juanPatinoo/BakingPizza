using TMPro;
using UnityEngine;

namespace BreakingPizza.UI
{
    // Cartel de interacción.
    [RequireComponent(typeof(TMP_Text))]
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private bool debugLogging = false;

        private TMP_Text promptText;

        private void Awake()
        {
            promptText = GetComponent<TMP_Text>();
            promptText.text = string.Empty;
        }

        public void ShowPrompt(string text)
        {
            if (debugLogging)
            {
                Debug.Log($"[InteractionPromptUI] ShowPrompt(\"{text}\") en frame {Time.frameCount}", this);
            }

            promptText.text = text;
        }

        public void HidePrompt()
        {
            if (debugLogging)
            {
                Debug.Log($"[InteractionPromptUI] HidePrompt() en frame {Time.frameCount}", this);
            }

            promptText.text = string.Empty;
        }
    }
}
