using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BreakingPizza.UI
{
    // Transiciones entre escenas y fundidos a negro.
    public class ScreenFader : MonoBehaviour
    {
        private const float CrossFadeDuration = 1.1f;
        private const float BlackFadeDuration = 0.45f;

        public static bool IsTransitioning { get; private set; }

        private static ScreenFader instance;

        private static string queuedScene;

        private Canvas canvas;
        private RawImage snapshot;
        private Image black;
        private Texture2D snapshotTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            IsTransitioning = false;
            queuedScene = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (instance != null) return;
            var go = new GameObject("[ScreenFader]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ScreenFader>();
            instance.Build();
            instance.StartCoroutine(instance.InitialFadeIn());
        }

        private void Build()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // por encima de todo

            snapshot = NewFullScreen<RawImage>("Snapshot");
            snapshot.enabled = false;

            black = NewFullScreen<Image>("Black");
            black.color = Color.black;
        }

        private T NewFullScreen<T>(string name) where T : Graphic
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var g = go.AddComponent<T>();
            g.raycastTarget = false;
            return g;
        }

        // ── API ──

        public static void LoadScene(string sceneName)
        {
            if (instance == null) Create();
            if (IsTransitioning)
            {
                queuedScene = sceneName;
                return;
            }
            instance.StartCoroutine(instance.CrossFadeToScene(sceneName));
        }

        // Al terminar una transición, si quedó una escena pedida, se carga.
        private void FinishTransition()
        {
            IsTransitioning = false;
            if (queuedScene == null) return;
            string next = queuedScene;
            queuedScene = null;
            StartCoroutine(CrossFadeToScene(next));
        }

        public static void FadeThroughBlack(Action atMidpoint)
        {
            if (instance == null) Create();
            if (IsTransitioning)
            {
                atMidpoint?.Invoke();
                return;
            }
            instance.StartCoroutine(instance.BlackFade(atMidpoint));
        }

        // ── Rutinas ──

        private IEnumerator InitialFadeIn()
        {
            SetBlack(1f);
            yield return null;
            yield return null;
            yield return Fade(1f, 0f, 0.8f, SetBlack);
        }

        private IEnumerator CrossFadeToScene(string sceneName)
        {
            IsTransitioning = true;

            // 1) Foto de la pantalla tal como se ve ahora (con su UI).
            yield return new WaitForEndOfFrame();
            if (snapshotTexture != null) Destroy(snapshotTexture);
            snapshotTexture = ScreenCapture.CaptureScreenshotAsTexture();
            snapshot.texture = snapshotTexture;
            snapshot.color = Color.white;
            snapshot.enabled = true;

            // 2) La foto se oscurece apenas mientras se carga la escena nueva.
            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogWarning($"[ScreenFader] No se pudo cargar la escena \"{sceneName}\" (¿está en Build Settings?).");
                snapshot.enabled = false;
                IsTransitioning = false;
                yield break;
            }
            op.allowSceneActivation = false;

            yield return Fade(1f, 0.55f, 0.35f, v => snapshot.color = new Color(v, v, v, 1f));
            while (op.progress < 0.9f) yield return null;

            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            // 3) Un par de frames para que la escena nueva se dibuje y
            //    después la foto se desvanece encima de ella.
            yield return null;
            yield return null;
            yield return Fade(1f, 0f, CrossFadeDuration, a => snapshot.color = new Color(0.55f, 0.55f, 0.55f, a));

            snapshot.enabled = false;
            snapshot.texture = null;
            Destroy(snapshotTexture);
            snapshotTexture = null;
            FinishTransition();
        }

        private IEnumerator BlackFade(Action atMidpoint)
        {
            IsTransitioning = true;
            yield return Fade(0f, 1f, BlackFadeDuration, SetBlack);
            try
            {
                atMidpoint?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            yield return new WaitForSeconds(0.15f);
            yield return Fade(1f, 0f, BlackFadeDuration * 1.3f, SetBlack);
            FinishTransition();
        }

        private void SetBlack(float alpha)
        {
            black.color = new Color(0f, 0f, 0f, alpha);
            black.enabled = alpha > 0.001f;
        }

        // Interpolación suave (ease in-out) con tiempo real, así funciona
        // aunque algo pause el juego.
        private static IEnumerator Fade(float from, float to, float duration, Action<float> apply)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float u = t / duration;
                u = u * u * (3f - 2f * u);
                apply(Mathf.Lerp(from, to, u));
                yield return null;
            }
            apply(to);
        }
    }
}
