using System.Collections.Generic;
using UnityEngine;

namespace BreakingPizza.UI.Kit
{
    // Desenfoque de imágenes.
    public static class Blur
    {
        private static readonly List<RenderTexture> temps = new List<RenderTexture>();

        public static void Apply(Texture src, RenderTexture dst, int minWidth)
        {
            if (src == null || dst == null) return;

            Texture current = src;
            int w = src.width, h = src.height;

            while (w / 2 >= minWidth)
            {
                w /= 2;
                h = Mathf.Max(1, h / 2);
                current = Step(current, w, h);
            }
            while (w * 2 < dst.width)
            {
                w *= 2;
                h *= 2;
                current = Step(current, w, h);
            }
            Graphics.Blit(current, dst);

            foreach (var t in temps) RenderTexture.ReleaseTemporary(t);
            temps.Clear();
        }

        private static Texture Step(Texture src, int w, int h)
        {
            var rt = RenderTexture.GetTemporary(w, h, 0);
            rt.filterMode = FilterMode.Bilinear;
            Graphics.Blit(src, rt);
            temps.Add(rt);
            return rt;
        }

        public static RenderTexture CreateTarget(int width, int height, string name)
        {
            return new RenderTexture(width, height, 0) { filterMode = FilterMode.Bilinear, name = name };
        }
    }
}
