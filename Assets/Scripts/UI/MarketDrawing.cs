using UnityEngine;

// Iconos propios en código: también se usan en el plano y no dependen de una fuente de emojis.
public static class MarketDrawing
{
    public static Material Material(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        var material = new Material(shader);
        material.color = color;
        return material;
    }
    public static void Fill(Rect rect, Color color)
    {
        Color old = GUI.color; GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
    }
    public static Texture2D Icon(int kind)
    {
        const int size = 48;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float u = (x + .5f) / size * 2f - 1f, v = (y + .5f) / size * 2f - 1f;
            Color c = Color.clear;
            if (kind == 0)
            {
                float width = .57f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((v + .85f) / 1.7f));
                if (Mathf.Abs(v) < .85f && Mathf.Abs(u) < width)
                    c = Mathf.Abs(u) < .035f ? new Color(.8f, 1f, .48f) : new Color(.2f, .9f, .35f);
            }
            else if (kind == 1)
            {
                if (Mathf.Abs(u) < .6f && Mathf.Abs(v) < .85f)
                    c = Mathf.Abs(v) > .63f ? new Color(.95f, .25f, .12f) : new Color(1f, .75f, .12f);
                if (Mathf.Abs(u) < .22f && Mathf.Abs(v) < .3f) c = Color.white;
            }
            else if (v > -.75f && v < .9f && Mathf.Abs(u) < (.9f - v) * .5f)
                c = Color.white;
            pixels[y * size + x] = c;
        }
        texture.SetPixels(pixels); texture.Apply(); texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }
}
