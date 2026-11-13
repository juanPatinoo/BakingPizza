using BreakingPizza.UI.Kit;
using TMPro;
using UnityEngine;

namespace BreakingPizza.UI
{
    // Tabla de controles (menú y ayuda).
    public static class ControlsTable
    {
        private static readonly string[,] Rows =
        {
            { "Caminar", "W A S D", "STICK IZQ" },
            { "Mirar", "MOUSE", "STICK DER" },
            { "Interactuar", "E  /  CLICK", "X" },
            { "Ponerse la máscara", "K", "TRIÁNGULO" },
            { "Minijuegos: golpear / mantener", "CLICK  /  ESPACIO", "X  /  R2" },
            { "Rallar queso", "A  y  D", "L1  y  R1" },
            { "Pepperoni: apuntar y soltar", "MOUSE + CLICK", "STICK + X" },
            { "Pausa", "ESC", "OPTIONS" },
        };

        // "top" es la Y del encabezado; las filas van hacia abajo.
        // Devuelve la Y de la última fila.
        public static float Build(Transform parent, float top, float width = 1300f, float rowHeight = 62f)
        {
            float colAction = -width / 2f + 30f;
            float colKeys = width * 0.054f;
            float colPad = width * 0.346f;

            float y = top;
            UIKit.Text("H1", parent, "ACCIÓN", 17f, Palette.Hazmat, new Vector2(400f, 26f), new Vector2(colAction + 200f, y), TextAlignmentOptions.Left, true);
            UIKit.Text("H2", parent, "TECLADO Y MOUSE", 17f, Palette.Hazmat, new Vector2(300f, 26f), new Vector2(colKeys, y), TextAlignmentOptions.Center, true);
            UIKit.Text("H3", parent, "CONTROL", 17f, Palette.Hazmat, new Vector2(300f, 26f), new Vector2(colPad, y), TextAlignmentOptions.Center, true);

            for (int i = 0; i < Rows.GetLength(0); i++)
            {
                y -= rowHeight;
                UIKit.Box("Row", parent, new Color(0f, 0f, 0f, i % 2 == 0 ? 0.45f : 0.3f), new Vector2(width, rowHeight - 6f), new Vector2(0f, y));
                UIKit.Text("Action", parent, Rows[i, 0], 24f, Color.white, new Vector2(520f, 34f), new Vector2(colAction + 260f, y), TextAlignmentOptions.Left);
                CenteredKey(parent, Rows[i, 1], new Vector2(colKeys, y));
                CenteredKey(parent, Rows[i, 2], new Vector2(colPad, y));
            }
            return y;
        }

        // Tecla centrada en "center" (KeyCap se ubica desde su borde izquierdo).
        private static void CenteredKey(Transform parent, string label, Vector2 center)
        {
            const float h = 40f, fs = 18f;
            float width = Mathf.Max(h, 26f + label.Length * fs * 0.62f);
            UIKit.KeyCap(parent, label, center - new Vector2(width / 2f, 0f), h, fs);
        }
    }
}
