using System;
using System.Collections;
using System.Collections.Generic;
using BreakingPizza.Audio;
using BreakingPizza.Controls;
using BreakingPizza.Core;
using BreakingPizza.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace BreakingPizza.UI
{
    // Créditos finales.
    public class CreditsRoll : MonoBehaviour
    {
        public enum Ending { Neutral, Win, Lose }

        private const float ContentWidth = 1300f;
        private const float ScrollSpeed = 62f;   // píxeles por segundo (a 1080p)
        private const float FastMultiplier = 5f;
        private const float MusicVolume = 0.75f;

        private const string CreditsVideo = "Credits/creditos";
        private const float VideoHoldAtEnd = 8f;
        private const float VideoVolume = 0.9f;

        public static bool IsShowing { get; private set; }

        private Ending ending;
        private Action onFinished;

        private RectTransform root;
        private CanvasGroup group;       // todo (fondo negro incluido)
        private RectTransform fg;
        private CanvasGroup fgGroup;     // todo menos el fondo negro
        private RectTransform content;
        private RectTransform thanksBlock;
        private RectTransform spinningPizza;
        private TextMeshProUGUI hint;
        private Image glowA, glowB;
        private float cursor;     // distancia desde el borde superior del contenido
        private float stopY;      // posición del contenido en la que termina el rodaje
        private bool skip;

        private struct Particle
        {
            public RectTransform Rt;
            public Image Img;
            public float Speed;
            public float Sway;
            public float Phase;
            public float Alpha;
        }

        private const string BackgroundImage = "Credits/camioneta_atardecer";

        private RectTransform backdrop;   // la foto, para el zoom lento
        private float elapsed;

        private VideoPlayer videoPlayer;
        private RenderTexture videoRT;
        private RawImage videoImage;
        private bool fadingOutVideo;

        private readonly List<Particle> particles = new List<Particle>();
        private readonly List<VideoPlayer> mutedVideos = new List<VideoPlayer>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsShowing = false;
        }

        public static void Show(Ending ending, Action onFinished = null)
        {
            if (IsShowing) return;
            IsShowing = true;

            var go = new GameObject("[CreditsRoll]");
            var roll = go.AddComponent<CreditsRoll>();
            roll.ending = ending;
            roll.onFinished = onFinished;
        }

        private void Start()
        {
            MuteVideos(true);
            Build();
            StartCoroutine(Roll());
        }

        private void OnDestroy()
        {
            if (videoRT != null) videoRT.Release();

            if (IsShowing)
            {
                IsShowing = false;
                MuteVideos(false);
            }
        }

        // El video del menú tiene su propio audio: se silencia para que no
        // se pise con la canción de los créditos.
        private void MuteVideos(bool mute)
        {
            if (mute)
            {
                foreach (var vp in FindObjectsByType<VideoPlayer>())
                {
                    if (vp == null || vp.audioOutputMode != VideoAudioOutputMode.Direct) continue;
                    if (vp.GetDirectAudioMute(0)) continue;
                    vp.SetDirectAudioMute(0, true);
                    mutedVideos.Add(vp);
                }
            }
            else
            {
                foreach (var vp in mutedVideos)
                {
                    if (vp != null) vp.SetDirectAudioMute(0, false);
                }
                mutedVideos.Clear();
            }
        }

        // ── Construcción ──

        private void Build()
        {
            var canvas = UIKit.CreateCanvas("CreditsCanvas", 200, transform);
            root = (RectTransform)canvas.transform;
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = true;

            var bg = UIKit.Image("Bg", root, null, new Color(0.008f, 0.016f, 0.012f, 1f), Vector2.zero, Vector2.zero);
            UIKit.Stretch(bg.rectTransform);
            BuildBackdrop();

            fg = UIKit.Stretch(UIKit.Rect("Foreground", root, Vector2.zero, Vector2.zero));
            fgGroup = fg.gameObject.AddComponent<CanvasGroup>();

            glowA = UIKit.Image("GlowA", fg, UIKit.Soft, Palette.BBGreen.WithAlpha(0.22f), new Vector2(1500f, 1200f), new Vector2(-620f, -260f));
            glowB = UIKit.Image("GlowB", fg, UIKit.Soft, Palette.MethBlue.WithAlpha(0.10f), new Vector2(1300f, 1000f), new Vector2(640f, 280f));

            var crystalLayer = UIKit.Stretch(UIKit.Rect("Crystals", fg, Vector2.zero, Vector2.zero));
            crystalLayer.gameObject.AddComponent<Canvas>();

            var rng = new System.Random(5);
            for (int i = 0; i < 46; i++)
            {
                float size = 4f + (float)rng.NextDouble() * 9f;
                var img = UIKit.Image("Crystal", crystalLayer, i % 3 == 0 ? UIKit.Rounded : UIKit.Circle, Palette.MethBlue, new Vector2(size, size * (i % 3 == 0 ? 1.6f : 1f)),
                    new Vector2(-1000f + (float)rng.NextDouble() * 2000f, -600f + (float)rng.NextDouble() * 1200f));
                var p = new Particle
                {
                    Rt = img.rectTransform,
                    Img = img,
                    Speed = 12f + (float)rng.NextDouble() * 30f,
                    Sway = 6f + (float)rng.NextDouble() * 18f,
                    Phase = (float)rng.NextDouble() * 6.28f,
                    Alpha = 0.08f + (float)rng.NextDouble() * 0.3f,
                };
                img.color = Palette.MethBlue.WithAlpha(p.Alpha);
                p.Rt.localEulerAngles = new Vector3(0f, 0f, (float)rng.NextDouble() * 180f);
                particles.Add(p);
            }

            // Contenido que sube. Pivote arriba: los hijos se ubican hacia
            // abajo desde el borde superior (y negativo).
            content = UIKit.Rect("Content", fg, new Vector2(ContentWidth, 10f), Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);
            // Canvas propio: subir los créditos mueve la capa entera sin
            // reconstruir la geometría de cada texto en cada cuadro.
            content.gameObject.AddComponent<Canvas>();
            BuildContent();

            // Barras de cine encima de todo, con la cinta de peligro.
            BuildBar(top: true);
            BuildBar(top: false);

            hint = UIKit.Text("Hint", fg, string.Empty, 17f, new Color(1f, 1f, 1f, 0.35f), new Vector2(800f, 26f), Vector2.zero, TextAlignmentOptions.Right);
            UIKit.Anchor(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0.5f));
            hint.rectTransform.anchoredPosition = new Vector2(-34f, 42f);
        }

        // Fondo: el video de créditos si está; si no, la foto difuminada.
        private void BuildBackdrop()
        {
            var clip = Resources.Load<VideoClip>(CreditsVideo);
            if (clip != null)
            {
                BuildVideoBackdrop(clip);
                return;
            }

            var tex = Resources.Load<Texture2D>(BackgroundImage);
            if (tex == null) return;

            // Cubre la pantalla manteniendo la proporción (se recorta lo que sobra).
            var holder = UIKit.Stretch(UIKit.Rect("BackdropHolder", root, Vector2.zero, Vector2.zero));
            backdrop = UIKit.Rect("Backdrop", holder, Vector2.zero, Vector2.zero);
            var fitter = backdrop.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = tex.width / (float)tex.height;
            var img = backdrop.gameObject.AddComponent<RawImage>();
            img.texture = tex;
            img.raycastTarget = false;

            // Velo oscuro para que se lean los textos.
            var tint = UIKit.Image("Tint", holder, null, new Color(0.01f, 0.03f, 0.02f, 0.55f), Vector2.zero, Vector2.zero);
            UIKit.Stretch(tint.rectTransform);
        }

        private void BuildVideoBackdrop(VideoClip clip)
        {
            videoRT = new RenderTexture((int)clip.width, (int)clip.height, 0) { name = "CreditsVideo" };

            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.clip = clip;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRT;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            videoPlayer.SetDirectAudioVolume(0, VideoVolume);
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;
            videoPlayer.Prepare();

            var holder = UIKit.Stretch(UIKit.Rect("VideoHolder", root, Vector2.zero, Vector2.zero));
            var rt = UIKit.Rect("Video", holder, Vector2.zero, Vector2.zero);
            var fitter = rt.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = clip.width / (float)Mathf.Max(1u, clip.height);
            videoImage = rt.gameObject.AddComponent<RawImage>();
            videoImage.texture = videoRT;
            videoImage.raycastTarget = false;
            videoImage.color = new Color(1f, 1f, 1f, 0f);

            // Velo suave en toda la pantalla y una franja más oscura detrás
            // de la columna de texto: el video se ve, y los créditos se leen.
            var tint = UIKit.Image("Tint", holder, null, new Color(0f, 0f, 0f, 0.3f), Vector2.zero, Vector2.zero);
            UIKit.Stretch(tint.rectTransform);
            UIKit.Image("TextBand", holder, UIKit.Soft, new Color(0f, 0f, 0f, 0.75f), new Vector2(1900f, 2200f), Vector2.zero);
        }

        private void BuildBar(bool top)
        {
            var bar = UIKit.Rect(top ? "TopBar" : "BottomBar", fg, new Vector2(0f, 84f), Vector2.zero);
            bar.anchorMin = new Vector2(0f, top ? 1f : 0f);
            bar.anchorMax = new Vector2(1f, top ? 1f : 0f);
            bar.pivot = new Vector2(0.5f, top ? 1f : 0f);
            bar.sizeDelta = new Vector2(0f, 84f);
            var fill = UIKit.Image("Fill", bar, null, Color.black, Vector2.zero, Vector2.zero);
            UIKit.Stretch(fill.rectTransform);

            var tape = UIKit.Image("Tape", bar, UIKit.Stripes, new Color(1f, 1f, 1f, 0.55f), Vector2.zero, Vector2.zero);
            tape.pixelsPerUnitMultiplier = 6f;
            var rt = tape.rectTransform;
            rt.anchorMin = new Vector2(0f, top ? 0f : 1f);
            rt.anchorMax = new Vector2(1f, top ? 0f : 1f);
            rt.pivot = new Vector2(0.5f, top ? 0f : 1f);
            rt.sizeDelta = new Vector2(0f, 5f);
        }

        private void BuildContent()
        {
            cursor = 0f;

            // ── Logo ──
            var logo = Block("Logo", 160f);
            BuildLogoWord(logo, "Ba", 56, "king", new Vector2(-250f, 0f));
            BuildLogoWord(logo, "Pi", 84, "zza", new Vector2(190f, 0f));
            Space(10f);
            Line(CreditsData.Tagline, 28f, Palette.TextDim, italic: true);

            // ── Cómo terminó ──
            Space(110f);
            string endingText = ending == Ending.Win ? CreditsData.EndingWin
                : ending == Ending.Lose ? CreditsData.EndingLose
                : CreditsData.EndingNeutral;
            Color endingColor = ending == Ending.Win ? Palette.BBGreenBright : ending == Ending.Lose ? new Color(1f, 0.6f, 0.5f) : Color.white;
            Line(endingText, 38f, endingColor, italic: true);

            // ── Equipo ──
            Space(170f);
            Header("EL EQUIPO");
            Space(40f);
            foreach (var role in CreditsData.Team)
            {
                Line(role.Area, 22f, Palette.Hazmat, bold: true);
                Space(14f);
                foreach (string person in role.People)
                {
                    Line(person, 54f, Color.white, bold: true);
                }
                Space(90f);
            }

            // ── Personajes ──
            Space(40f);
            Header("PERSONAJES");
            Space(30f);
            for (int i = 0; i < CreditsData.Cast.GetLength(0); i++)
            {
                Line(CreditsData.Cast[i, 0], 40f, Color.white, bold: true);
                Line(CreditsData.Cast[i, 1], 23f, Palette.TextDim, italic: true);
                Space(36f);
            }

            // ── Inspiración ──
            Space(70f);
            Header("INSPIRADO EN");
            Space(24f);
            Line(CreditsData.Inspiration, 26f, Color.white);

            // ── Modelos 3D ──
            Space(130f);
            Header("MODELOS 3D");
            Space(16f);
            Line(CreditsData.ModelsLicense, 21f, Palette.TextDim, italic: true);
            Space(30f);
            for (int i = 0; i < CreditsData.Models.GetLength(0); i++)
            {
                Line(CreditsData.Models[i, 0], 30f, Color.white, bold: true);
                Line("por " + CreditsData.Models[i, 1], 23f, Palette.MethBlue);
                Space(26f);
            }

            Space(70f);
            Header("ENTORNO DEL DESIERTO");
            Space(24f);
            Line(CreditsData.Environment, 30f, Color.white, bold: true);

            // ── Música ──
            Space(130f);
            Header("MÚSICA");
            Space(24f);
            BuildMusicCredits();

            // ── Herramientas ──
            Space(130f);
            Header("HECHO CON");
            Space(24f);
            Line(CreditsData.Tools, 26f, Color.white);

            // ── Cierre: tu pizza + gracias ──
            Space(360f);
            thanksBlock = Block("Thanks", 560f);
            if (PizzaScore.CompletedCount > 0)
            {
                var plate = UIKit.Rect("Plate", thanksBlock, new Vector2(300f, 300f), new Vector2(0f, 120f));
                UIKit.Image("Shadow", plate, UIKit.Soft, new Color(0f, 0f, 0f, 0.6f), new Vector2(380f, 380f), new Vector2(8f, -12f));
                UIKit.Dot("Board", plate, Palette.Wood, 330f, Vector2.zero);
                spinningPizza = UIKit.Rect("Pizza", plate, new Vector2(480f, 480f), Vector2.zero);
                spinningPizza.localScale = Vector3.one * (300f / 480f);
                ResultScreenUI.BuildFinalPizza(spinningPizza);
            }
            float textY = PizzaScore.CompletedCount > 0 ? -110f : 40f;
            UIKit.Text("ThanksTitle", thanksBlock, CreditsData.ThanksTitle, 86f, Color.white, new Vector2(1300f, 110f), new Vector2(0f, textY), TextAlignmentOptions.Center, true);
            var quote = UIKit.Text("Quote", thanksBlock, CreditsData.FinalQuote, 30f, Palette.Hazmat, new Vector2(1300f, 50f), new Vector2(0f, textY - 82f), TextAlignmentOptions.Center);
            quote.fontStyle = FontStyles.Italic;

            // El rodaje termina cuando el bloque final queda centrado.
            stopY = -thanksBlock.anchoredPosition.y;
            content.sizeDelta = new Vector2(ContentWidth, cursor);
        }

        private void BuildMusicCredits()
        {
            // Con el video de créditos, la canción es la del video.
            bool hasVideo = Resources.Load<VideoClip>(CreditsVideo) != null;
            string song = hasVideo
                ? (string.IsNullOrEmpty(CreditsData.CreditsSong) ? null : CreditsData.CreditsSong)
                : Music.SongCredit(MusicTrack.Credits);
            if (hasVideo)
            {
                Line(CreditsData.CreditsFootage, 22f, Palette.TextDim, italic: true);
                Space(24f);
            }
            if (song != null)
            {
                Line("Tema de los créditos", 22f, Palette.Hazmat, bold: true);
                Line(song, 34f, Color.white, bold: true);
                Space(30f);
            }

            var extra = new List<string>();
            AddSong(extra, MusicTrack.Desert, "Desierto");
            AddSong(extra, MusicTrack.Lab, "Laboratorio");
            AddSong(extra, MusicTrack.Twist, "Giro");
            AddSong(extra, MusicTrack.Cooking, "Cocina");
            foreach (string s in extra)
            {
                Line(s, 26f, Color.white);
                Space(10f);
            }

            bool anySynth = song == null || extra.Count < 4;
            if (anySynth)
            {
                if (song != null || extra.Count > 0) Space(20f);
                Line(CreditsData.SynthMusic, 26f, song == null && extra.Count == 0 ? Color.white : Palette.TextDim);
            }
        }

        private static void AddSong(List<string> list, MusicTrack track, string label)
        {
            string credit = Music.SongCredit(track);
            if (credit != null) list.Add($"{label}:  {credit}");
        }

        // ── Helpers de maquetado ──

        private RectTransform Block(string name, float height)
        {
            var rt = UIKit.Rect(name, content, new Vector2(ContentWidth, height), new Vector2(0f, -(cursor + height / 2f)));
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            cursor += height;
            return rt;
        }

        private void Space(float px) => cursor += px;

        private TextMeshProUGUI Line(string text, float size, Color color, bool bold = false, bool italic = false, float width = ContentWidth)
        {
            var t = UIKit.Text("Line", content, text, size, color, new Vector2(width, size * 1.4f), Vector2.zero, TextAlignmentOptions.Center, bold);
            if (italic) t.fontStyle |= FontStyles.Italic;
            float h = Mathf.Max(size * 1.25f, t.GetPreferredValues(text, width, 0f).y);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(width, h);
            rt.anchoredPosition = new Vector2(0f, -(cursor + h / 2f));
            cursor += h;
            return t;
        }

        private void Header(string title)
        {
            Line(title, 26f, Palette.BBGreenBright, bold: true);
            Space(10f);
            var tape = UIKit.Image("HeaderTape", content, UIKit.Stripes, Color.white, new Vector2(240f, 7f), Vector2.zero);
            tape.pixelsPerUnitMultiplier = 6f;
            var rt = tape.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -(cursor + 3.5f));
            cursor += 7f;
        }

        private static void BuildLogoWord(RectTransform parent, string symbol, int number, string rest, Vector2 pos)
        {
            var tile = UIKit.ElementTile(parent, new Vector2(120f, 132f), pos + new Vector2(-90f, 0f), number, symbol, string.Empty);
            tile.Fill.color = Palette.BBGreen;
            tile.Border.color = Palette.BBGreenBright;
            tile.Symbol.fontSize = 68f;
            UIKit.Text("Rest", parent, rest, 96f, Color.white, new Vector2(300f, 130f), pos + new Vector2(130f, -8f), TextAlignmentOptions.Left, true);
        }

        // ── Rodaje ──

        private IEnumerator Roll()
        {
            // Un frame para que el Canvas tome el tamaño real de la pantalla.
            yield return null;
            Canvas.ForceUpdateCanvases();
            float screenH = Mathf.Max(600f, root.rect.height);
            content.anchoredPosition = new Vector2(0f, -screenH / 2f - 20f);

            if (videoPlayer != null)
            {
                yield return RollWithVideo();
                yield break;
            }

            Music.Play(MusicTrack.Credits, MusicVolume, 2.5f);
            Sfx.Play(SfxId.Whoosh, 0.5f, 0.8f);
            yield return FadeGroup(group, 0f, 1f, 1.4f);

            // Subida.
            while (!skip && content.anchoredPosition.y < stopY)
            {
                float dt = Time.unscaledDeltaTime;
                float speed = ScrollSpeed * (GameInput.ActionHeld ? FastMultiplier : 1f);
                float y = Mathf.Min(stopY, content.anchoredPosition.y + speed * dt);
                content.anchoredPosition = new Vector2(0f, y);
                yield return null;
            }

            if (!skip)
            {
                content.anchoredPosition = new Vector2(0f, stopY);
                StartCoroutine(Tween.Punch(thanksBlock, 0.06f, 0.6f));
                Sfx.Play(SfxId.Complete, 0.5f, 0.8f);

                // Se queda un rato en el "gracias por jugar".
                for (float t = 0f; t < 7f && !skip; t += Time.unscaledDeltaTime)
                {
                    if (GameInput.ConfirmPressed || GameInput.PointerClick) break;
                    yield return null;
                }
            }

            Music.Stop(skip ? 1.2f : 3.5f);

            if (onFinished != null)
            {
                // Se apaga el contenido y queda la pantalla en negro; quien
                // nos llamó cambia de escena desde ahí (y eso nos destruye).
                yield return FadeGroup(fgGroup, 1f, 0f, skip ? 0.7f : 2.2f);
                onFinished();
                yield break;
            }

            yield return FadeGroup(group, 1f, 0f, skip ? 0.7f : 2.2f);
            IsShowing = false;
            MuteVideos(false);
            Destroy(gameObject);
        }

        // Con video: la posición de los créditos sale del tiempo del video,
        // así quedan sincronizados con la canción aunque haya tirones.
        private IEnumerator RollWithVideo()
        {
            // La música del juego se apaga: suena la canción del video.
            Music.Stop(1.2f);

            float startY = content.anchoredPosition.y;
            for (float wait = 0f; !videoPlayer.isPrepared && wait < 6f; wait += Time.unscaledDeltaTime) yield return null;

            float length = (float)videoPlayer.length;
            float scrollEnd = Mathf.Max(10f, length - VideoHoldAtEnd);
            videoPlayer.Play();
            StartCoroutine(FadeGroup(group, 0f, 1f, 1.2f));

            float t = 0f;
            while (!skip && t < scrollEnd)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                if (videoPlayer.isPlaying)
                {
                    float videoTime = (float)videoPlayer.time;
                    float drift = videoTime - t;
                    if (Mathf.Abs(drift) > 0.75f) t = videoTime;   // salto grande: se alinea de una
                    else t += drift * Mathf.Min(1f, dt * 0.8f);    // diferencia chica: se corrige suave
                }
                else if (videoPlayer.isPrepared && t > 1f && videoPlayer.frame > 0 && !videoPlayer.isPaused)
                {
                    break; // el video terminó antes de tiempo
                }
                float y = Mathf.Lerp(startY, stopY, Mathf.Clamp01(t / scrollEnd));
                content.anchoredPosition = new Vector2(0f, y);
                yield return null;
            }

            if (!skip)
            {
                content.anchoredPosition = new Vector2(0f, stopY);
                StartCoroutine(Tween.Punch(thanksBlock, 0.06f, 0.6f));

                // Se queda en el "gracias por jugar" mientras cierra la canción.
                while (!skip && videoPlayer.isPlaying && videoPlayer.time < length - 1.6f) yield return null;
            }

            // Fundido final: contenido, video y audio juntos.
            fadingOutVideo = true;
            float duration = skip ? 0.8f : 1.5f;
            CanvasGroup fade = onFinished != null ? fgGroup : group;
            for (float e = 0f; e < duration; e += Time.unscaledDeltaTime)
            {
                float u = 1f - e / duration;
                fade.alpha = u;
                videoPlayer.SetDirectAudioVolume(0, VideoVolume * u);
                // Con onFinished el fondo queda negro: el video también se apaga.
                if (onFinished != null && videoImage != null) videoImage.color = new Color(1f, 1f, 1f, u);
                yield return null;
            }
            fade.alpha = 0f;
            videoPlayer.Stop();

            if (onFinished != null)
            {
                onFinished();
                yield break;
            }

            IsShowing = false;
            MuteVideos(false);
            Destroy(gameObject);
        }

        private IEnumerator FadeGroup(CanvasGroup g, float from, float to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                g.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            g.alpha = to;
        }

        private void Update()
        {
            if (GameInput.PausePressed || GameInput.BackPressed) skip = true;

            // El video aparece con un fundido apenas tiene su primer cuadro.
            if (videoImage != null && videoPlayer != null && videoPlayer.isPlaying && videoPlayer.frame > 0 && videoImage.color.a < 1f && !fadingOutVideo)
            {
                videoImage.color = new Color(1f, 1f, 1f, Mathf.MoveTowards(videoImage.color.a, 1f, Time.unscaledDeltaTime * 2f));
            }

            // Zoom lento sobre la foto (efecto "Ken Burns").
            elapsed += Time.unscaledDeltaTime;
            if (backdrop != null)
            {
                float k = Mathf.Clamp01(elapsed / 100f);
                backdrop.localScale = Vector3.one * Mathf.Lerp(1.04f, 1.16f, k);
                backdrop.anchoredPosition = new Vector2(Mathf.Lerp(0f, -40f, k), Mathf.Lerp(0f, 18f, k));
            }

            float dt = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;

            if (hint != null)
            {
                hint.text = videoPlayer != null
                    ? GameInput.Label("ESC para saltar", "CÍRCULO para saltar")
                    : GameInput.Label("MANTENÉ CLICK o ESPACIO para acelerar   ·   ESC para saltar",
                        "MANTENÉ X para acelerar   ·   CÍRCULO para saltar");
            }

            float halfH = root != null ? root.rect.height / 2f + 30f : 600f;
            for (int i = 0; i < particles.Count; i++)
            {
                var p = particles[i];
                if (p.Rt == null) continue;
                Vector2 pos = p.Rt.anchoredPosition;
                pos.y += p.Speed * dt;
                pos.x += Mathf.Sin(time * 0.6f + p.Phase) * p.Sway * dt;
                if (pos.y > halfH) pos.y = -halfH;
                p.Rt.anchoredPosition = pos;
                p.Rt.Rotate(0f, 0f, p.Speed * 0.6f * dt);
                p.Img.color = Palette.MethBlue.WithAlpha(p.Alpha * (0.7f + 0.3f * Mathf.Sin(time * 1.3f + p.Phase)));
            }

            if (glowA != null) glowA.color = Palette.BBGreen.WithAlpha(0.18f + 0.06f * Mathf.Sin(time * 0.5f));
            if (glowB != null) glowB.color = Palette.MethBlue.WithAlpha(0.08f + 0.04f * Mathf.Sin(time * 0.4f + 1.5f));
            if (spinningPizza != null) spinningPizza.Rotate(0f, 0f, -10f * dt);
        }
    }
}
