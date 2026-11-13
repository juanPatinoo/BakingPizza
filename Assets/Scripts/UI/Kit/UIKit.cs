using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BreakingPizza.UI.Kit
{
    // Herramientas para armar la interfaz por código.
    public static class Palette
    {
        public static readonly Color BBGreen = Hex("1E7A3A");
        public static readonly Color BBGreenBright = Hex("3DDC6E");
        public static readonly Color BBGreenDark = Hex("0F3D1F");
        public static readonly Color Panel = new Color(0.035f, 0.075f, 0.05f, 0.95f);
        public static readonly Color PanelLight = Hex("13261A");
        public static readonly Color Hazmat = Hex("F4C430");
        public static readonly Color MethBlue = Hex("5BD0FF");
        public static readonly Color Tomato = Hex("D3302A");
        public static readonly Color TomatoDark = Hex("8E1D18");
        public static readonly Color Cheese = Hex("FFD54A");
        public static readonly Color CheesePale = Hex("FFF1B8");
        public static readonly Color Dough = Hex("F3DBA8");
        public static readonly Color DoughRaw = Hex("E9CFA0");
        public static readonly Color Crust = Hex("D9A05B");
        public static readonly Color CrustBaked = Hex("B8742E");
        public static readonly Color Charred = Hex("3A2416");
        public static readonly Color Pepperoni = Hex("A3241E");
        public static readonly Color PepperoniDark = Hex("6E1410");
        public static readonly Color Wood = Hex("7A4A26");
        public static readonly Color Steel = Hex("B8C2C8");
        public static readonly Color TextDim = Hex("8FA597");
        public static readonly Color Danger = Hex("FF4D4D");
        public static readonly Color Good = Hex("7CE38B");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }

        public static Color WithAlpha(this Color c, float a)
        {
            c.a = a;
            return c;
        }
    }

    public static class UIKit
    {
        private static Sprite circle, ring, rounded, soft, stripes;

        public static Sprite Circle => circle != null ? circle : circle = MakeCircle(256, 0f);
        public static Sprite Ring => ring != null ? ring : ring = MakeCircle(256, 22f);
        public static Sprite Rounded => rounded != null ? rounded : rounded = MakeRounded(64, 22);
        public static Sprite Soft => soft != null ? soft : soft = MakeSoft(128);
        public static Sprite Stripes => stripes != null ? stripes : stripes = MakeStripes(64);

        // ── Construcción ──

        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        // Estira el RectTransform a todo el padre (con margen opcional).
        public static RectTransform Stretch(RectTransform rt, float margin = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
            return rt;
        }

        public static RectTransform Anchor(RectTransform rt, Vector2 anchor, Vector2? pivot = null)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            return rt;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color, Vector2 size, Vector2 pos)
        {
            var rt = Rect(name, parent, size, pos);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sprite == Rounded)
            {
                img.type = UnityEngine.UI.Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1.6f;
            }
            else if (sprite == Stripes)
            {
                img.type = UnityEngine.UI.Image.Type.Tiled;
            }
            return img;
        }

        public static Image Box(string name, Transform parent, Color color, Vector2 size, Vector2 pos)
        {
            return Image(name, parent, Rounded, color, size, pos);
        }

        public static Image Dot(string name, Transform parent, Color color, float diameter, Vector2 pos)
        {
            return Image(name, parent, Circle, color, new Vector2(diameter, diameter), pos);
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string content, float fontSize, Color color,
            Vector2 size, Vector2 pos, TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false)
        {
            var rt = Rect(name, parent, size, pos);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = content;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        // Tile tipo tabla periódica (el sello visual de Breaking Bad):
        // número arriba a la izquierda, símbolo grande, nombre abajo.
        public static ElementTile ElementTile(Transform parent, Vector2 size, Vector2 pos, int number, string symbol, string label)
        {
            var tile = new ElementTile();
            tile.Border = Box("Tile_" + symbol, parent, Palette.BBGreen, size + new Vector2(6f, 6f), pos);
            tile.Root = (RectTransform)tile.Border.transform;
            tile.Fill = Box("Fill", tile.Root, Palette.BBGreenDark, size, Vector2.zero);

            float s = size.y / 104f;
            tile.Number = Text("Number", tile.Fill.transform, number.ToString(), 16f * s, Color.white,
                new Vector2(size.x - 12f, 22f * s), new Vector2(0f, size.y / 2f - 14f * s), TextAlignmentOptions.TopLeft);
            tile.Symbol = Text("Symbol", tile.Fill.transform, symbol, 46f * s, Color.white,
                new Vector2(size.x, 56f * s), new Vector2(0f, 2f * s), TextAlignmentOptions.Center, true);
            tile.Label = Text("Label", tile.Fill.transform, label, 14f * s, Color.white,
                new Vector2(size.x - 6f, 20f * s), new Vector2(0f, -size.y / 2f + 13f * s), TextAlignmentOptions.Center);
            tile.Badge = Text("Badge", tile.Fill.transform, string.Empty, 14f * s, Palette.Hazmat,
                new Vector2(size.x - 10f, 22f * s), new Vector2(0f, size.y / 2f - 14f * s), TextAlignmentOptions.TopRight, true);
            return tile;
        }

        // Tecla estilo "keycap" ([CLICK], [A], [ESPACIO]). Devuelve el ancho
        // para poder encadenar varias en fila.
        public static float KeyCap(Transform parent, string label, Vector2 leftPos, float height = 46f, float fontSize = 24f)
        {
            float width = Mathf.Max(height, 26f + label.Length * fontSize * 0.62f);
            var cap = Box("KeyCap_" + label, parent, new Color(1f, 1f, 1f, 0.35f), new Vector2(width, height), leftPos + new Vector2(width / 2f, 0f));
            Box("KeyTop", cap.transform, Palette.PanelLight, new Vector2(width - 6f, height - 8f), new Vector2(0f, 2f));
            Text("KeyLabel", cap.transform, label, fontSize, Palette.Hazmat, new Vector2(width, height), new Vector2(0f, 2f), TextAlignmentOptions.Center, true);
            return width;
        }

        public static UIButton MakeButton(Transform parent, string label, Vector2 size, Vector2 pos, Color fill, float fontSize = 28f)
        {
            var b = new UIButton();
            b.Border = Box("Button", parent, Palette.BBGreenBright.WithAlpha(0.7f), size + new Vector2(8f, 8f), pos);
            b.Rt = b.Border.rectTransform;
            b.Fill = Box("ButtonFill", b.Rt, fill, size, Vector2.zero);
            b.Label = Text("ButtonLabel", b.Rt, label, fontSize, Color.white, size - new Vector2(10f, 6f), Vector2.zero, TextAlignmentOptions.Center, true);
            return b;
        }

        // ── Efectos ──

        // Texto que aparece con un "pop", sube y se desvanece.
        public static void FloatText(MonoBehaviour runner, Transform parent, string content, Color color, Vector2 pos, float fontSize = 44f)
        {
            var t = Text("FloatText", parent, content, fontSize, color, new Vector2(700f, 90f), pos, TextAlignmentOptions.Center, true);
            runner.StartCoroutine(FloatRoutine(t));
        }

        private static IEnumerator FloatRoutine(TextMeshProUGUI t)
        {
            var rt = t.rectTransform;
            Vector2 start = rt.anchoredPosition;
            Color c = t.color;
            float d = 0.95f;
            for (float e = 0f; e < d; e += Time.deltaTime)
            {
                if (t == null) yield break;
                float u = e / d;
                rt.anchoredPosition = start + Vector2.up * (90f * Ease.OutCubic(u));
                rt.localScale = Vector3.one * (u < 0.15f ? Mathf.Lerp(0.4f, 1.15f, u / 0.15f) : Mathf.Lerp(1.15f, 1f, (u - 0.15f) / 0.2f));
                c.a = u < 0.6f ? 1f : 1f - (u - 0.6f) / 0.4f;
                t.color = c;
                yield return null;
            }
            if (t != null) UnityEngine.Object.Destroy(t.gameObject);
        }

        // Explosión de partículas de UI (harina, salsa, confeti, chispas...).
        public static void Burst(MonoBehaviour runner, Transform parent, Vector2 pos, Color color, int count,
            float speed = 420f, float size = 12f, float gravity = -900f, float life = 0.8f, Color? color2 = null, Sprite sprite = null)
        {
            runner.StartCoroutine(BurstRoutine(parent, pos, color, color2 ?? color, count, speed, size, gravity, life, sprite ?? Circle));
        }

        private static IEnumerator BurstRoutine(Transform parent, Vector2 pos, Color c1, Color c2, int count, float speed, float size, float gravity, float life, Sprite sprite)
        {
            var parts = new List<(Image img, Vector2 vel, float spin)>(count);
            for (int i = 0; i < count; i++)
            {
                float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float sp = speed * UnityEngine.Random.Range(0.35f, 1f);
                float sz = size * UnityEngine.Random.Range(0.6f, 1.3f);
                var img = Image("Particle", parent, sprite, Color.Lerp(c1, c2, UnityEngine.Random.value), new Vector2(sz, sz), pos);
                parts.Add((img, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp, UnityEngine.Random.Range(-400f, 400f)));
            }

            for (float e = 0f; e < life; e += Time.deltaTime)
            {
                float u = e / life;
                for (int i = 0; i < parts.Count; i++)
                {
                    var p = parts[i];
                    if (p.img == null) continue;
                    p.vel.y += gravity * Time.deltaTime;
                    p.vel *= 1f - 1.5f * Time.deltaTime;
                    parts[i] = p;
                    var rt = p.img.rectTransform;
                    rt.anchoredPosition += p.vel * Time.deltaTime;
                    rt.Rotate(0f, 0f, p.spin * Time.deltaTime);
                    var col = p.img.color;
                    col.a = 1f - u * u;
                    p.img.color = col;
                }
                yield return null;
            }

            foreach (var p in parts)
            {
                if (p.img != null) UnityEngine.Object.Destroy(p.img.gameObject);
            }
        }

        // ── Generación de sprites ──

        private static Texture2D NewTexture(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
        }

        private static Sprite MakeCircle(int size, float ringThickness)
        {
            var tex = NewTexture(size, size);
            var px = new Color32[size * size];
            float r = size / 2f - 1f;
            float c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    if (ringThickness > 0f) a *= Mathf.Clamp01(d - (r - ringThickness) + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeRounded(int size, int radius)
        {
            var tex = NewTexture(size, size);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            float b = radius + 1;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        private static Sprite MakeSoft(int size)
        {
            var tex = NewTexture(size, size);
            var px = new Color32[size * size];
            float c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Clamp01(Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c);
                    float a = (1f - d) * (1f - d);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeStripes(int size)
        {
            var tex = NewTexture(size, size);
            tex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color32[size * size];
            Color32 yellow = Palette.Hazmat;
            Color32 black = new Color32(20, 20, 20, 255);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    px[y * size + x] = ((x + y) / (size / 2)) % 2 == 0 ? yellow : black;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    public class UIButton
    {
        public RectTransform Rt;
        public Image Border;
        public Image Fill;
        public TextMeshProUGUI Label;
        public bool Hovered { get; private set; }

        // Con el control no hay mouse: el botón "elegido" se marca con esto
        // (lo maneja quien lo usa, ej. con la cruceta) y se activa con X.
        public bool Focused;

        public bool Tick(bool allowClick = true)
        {
            if (Rt == null) return false;

            bool pad = BreakingPizza.Controls.GameInput.UsingGamepad;
            bool now = pad ? Focused : RectTransformUtility.RectangleContainsScreenPoint(Rt, BreakingPizza.Controls.GameInput.Pointer, null);
            if (now && !Hovered) BreakingPizza.Audio.Sfx.Play(BreakingPizza.Audio.SfxId.Count, 0.5f, 1.2f);
            Hovered = now;

            Rt.localScale = Vector3.Lerp(Rt.localScale, Vector3.one * (Hovered ? 1.06f : 1f), 15f * Time.unscaledDeltaTime);
            Border.color = Hovered ? Palette.Hazmat : Palette.BBGreenBright.WithAlpha(0.7f);
            Label.color = Hovered ? Palette.Hazmat : Color.white;

            // Con el control el click lo decide el dueño del botón (X sobre
            // el botón elegido), así que acá solo cuenta el mouse.
            return allowClick && Hovered && !pad && BreakingPizza.Controls.GameInput.PointerClick;
        }
    }

    public class ElementTile
    {
        public RectTransform Root;
        public Image Border;
        public Image Fill;
        public TextMeshProUGUI Number;
        public TextMeshProUGUI Symbol;
        public TextMeshProUGUI Label;
        public TextMeshProUGUI Badge;
    }

    public static class Ease
    {
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
        public static float InCubic(float t) => Mathf.Pow(Mathf.Clamp01(t), 3f);

        public static float OutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static float OutElastic(float t)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f || t >= 1f) return t;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }
    }

    public static class Tween
    {
        public static IEnumerator Run(float duration, Action<float> step)
        {
            for (float e = 0f; e < duration; e += Time.deltaTime)
            {
                step(e / duration);
                yield return null;
            }
            step(1f);
        }

        // "Aplasta" y rebota (golpe de amasado, acierto, etc.).
        public static IEnumerator Squash(Transform t, Vector3 squash, float duration = 0.35f)
        {
            Vector3 baseScale = Vector3.one;
            yield return Run(duration, u =>
            {
                if (t != null) t.localScale = Vector3.LerpUnclamped(squash, baseScale, Ease.OutElastic(u));
            });
        }

        public static IEnumerator Punch(Transform t, float amount = 0.25f, float duration = 0.3f)
        {
            yield return Run(duration, u =>
            {
                if (t != null) t.localScale = Vector3.one * (1f + amount * Mathf.Sin(u * Mathf.PI) * (1f - u));
            });
            if (t != null) t.localScale = Vector3.one;
        }

        public static IEnumerator Shake(RectTransform rt, float magnitude = 14f, float duration = 0.3f)
        {
            if (rt == null) yield break;
            Vector2 origin = rt.anchoredPosition;
            yield return Run(duration, u =>
            {
                if (rt == null) return;
                float m = magnitude * (1f - u);
                rt.anchoredPosition = origin + new Vector2(UnityEngine.Random.Range(-m, m), UnityEngine.Random.Range(-m, m));
            });
            if (rt != null) rt.anchoredPosition = origin;
        }

        public static IEnumerator Fade(CanvasGroup g, float from, float to, float duration)
        {
            yield return Run(duration, u =>
            {
                if (g != null) g.alpha = Mathf.Lerp(from, to, u);
            });
        }
    }
}
