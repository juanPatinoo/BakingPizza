using BreakingPizza.Core;
using BreakingPizza.Interaction;
using BreakingPizza.Minigames;
using BreakingPizza.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace BreakingPizza.UI
{
    // Mira en el centro de la pantalla.
    public class Crosshair : MonoBehaviour
    {
        private CanvasGroup group;
        private Image dot, dotShadow, ring;
        private PlayerInteractor interactor;
        private float targetBlend;

        private void Awake()
        {
            var canvas = UIKit.CreateCanvas("CrosshairCanvas", 15, transform);
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;

            var root = (RectTransform)canvas.transform;
            ring = UIKit.Image("Ring", root, UIKit.Ring, Palette.Hazmat, new Vector2(30f, 30f), Vector2.zero);
            dotShadow = UIKit.Dot("DotShadow", root, new Color(0f, 0f, 0f, 0.55f), 10f, Vector2.zero);
            dot = UIKit.Dot("Dot", root, Color.white, 6f, Vector2.zero);
        }

        private void Update()
        {
            if (interactor == null) interactor = FindAnyObjectByType<PlayerInteractor>();

            var stage = GameManager.Instance != null ? GameManager.Instance.CurrentStage : GameStage.Desert;
            bool visible = Cursor.lockState == CursorLockMode.Locked
                           && !MinigameHost.IsRunning
                           && stage != GameStage.Result
                           && stage != GameStage.Twist;

            group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, 8f * Time.unscaledDeltaTime);
            if (!visible) return;

            bool hasTarget = interactor != null && interactor.HasTarget;
            targetBlend = Mathf.MoveTowards(targetBlend, hasTarget ? 1f : 0f, 10f * Time.unscaledDeltaTime);

            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 6f) * targetBlend;
            ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, targetBlend) * pulse;
            ring.color = Palette.Hazmat.WithAlpha(0.9f * targetBlend);

            dot.color = Color.Lerp(new Color(1f, 1f, 1f, 0.85f), Palette.Hazmat, targetBlend);
            float dotSize = Mathf.Lerp(6f, 8f, targetBlend);
            dot.rectTransform.sizeDelta = new Vector2(dotSize, dotSize);
            dotShadow.rectTransform.sizeDelta = new Vector2(dotSize + 4f, dotSize + 4f);
        }
    }
}
