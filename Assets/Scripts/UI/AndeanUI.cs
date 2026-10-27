using UnityEngine;

// Iconografia andina para la interfaz IMGUI (GameHUD y FloatingWindows):
// - Aguayo: franja de tejido con rayas de colores y una fila de rombos al centro.
// - Chakana: cruz andina escalonada.
// Todo se dibuja con rectangulos, sin texturas importadas.
public static class AndeanUI
{
    public static readonly Color Magenta = new Color(0.80f, 0.10f, 0.42f);
    public static readonly Color Orange = new Color(0.96f, 0.47f, 0.10f);
    public static readonly Color Yellow = new Color(0.99f, 0.80f, 0.16f);
    public static readonly Color Green = new Color(0.10f, 0.56f, 0.30f);
    public static readonly Color Red = new Color(0.78f, 0.12f, 0.16f);
    public static readonly Color Blue = new Color(0.16f, 0.30f, 0.66f);

    // Rayas de arriba hacia abajo (fraccion de la altura) y la banda central con rombos
    private static readonly float[] StripeHeights = { 0.14f, 0.12f, 0.48f, 0.12f, 0.14f };
    private static readonly Color[] StripeColors = { Magenta, Yellow, Red, Green, Blue };
    private static readonly Color[] DiamondColors = { Yellow, Green, Color.white, Orange };

    // Chakana de 9x9 celdas, con el hueco del centro
    private static readonly string[] Chakana =
    {
        "...XXX...",
        "...XXX...",
        ".XXXXXXX.",
        "XXXXXXXXX",
        "XXXX.XXXX",
        "XXXXXXXXX",
        ".XXXXXXX.",
        "...XXX...",
        "...XXX...",
    };

    public static void DrawAguayo(Rect r)
    {
        if (Event.current.type != EventType.Repaint) return;

        float y = r.y;
        Rect band = r;
        for (int i = 0; i < StripeHeights.Length; i++)
        {
            float h = r.height * StripeHeights[i];
            Fill(new Rect(r.x, y, r.width, h), StripeColors[i]);
            if (i == 2) band = new Rect(r.x, y, r.width, h);
            y += h;
        }

        // Rombos (cuadrados girados 45°), recortados al borde de la franja
        float size = band.height * 0.62f;
        if (size < 2f) return;
        float step = size * 1.7f;
        GUI.BeginGroup(band);
        Matrix4x4 matrix = GUI.matrix;
        int k = 0;
        for (float x = step * 0.5f; x < band.width + size; x += step, k++)
        {
            Vector2 center = new Vector2(x, band.height / 2f);
            GUIUtility.RotateAroundPivot(45f, center);
            Fill(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), DiamondColors[k % DiamondColors.Length]);
            GUI.matrix = matrix;
        }
        GUI.EndGroup();
    }

    public static void DrawChakana(Rect r, Color color)
    {
        if (Event.current.type != EventType.Repaint) return;
        float cw = r.width / 9f, ch = r.height / 9f;
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                if (Chakana[row][col] == 'X')
                    Fill(new Rect(r.x + col * cw, r.y + row * ch, Mathf.Ceil(cw), Mathf.Ceil(ch)), color);
    }

    private static void Fill(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = new Color(c.r, c.g, c.b, c.a * old.a);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }
}
