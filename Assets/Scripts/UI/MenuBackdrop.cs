using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Fondo animado del menú principal: muro de piedra, enredaderas con espinas violetas que laten y esporas flotando.
// Todo se genera por código y se anima con tiempo sin escala.
public static class MenuBackdrop
{
    public static Canvas Create()
    {
        Canvas canvas = UITheme.MakeCanvas("MenuBackdrop", 5);
        canvas.GetComponent<GraphicRaycaster>().enabled = false;

        // Muro de piedra
        RawImage wall = new GameObject("Muro", typeof(RectTransform)).AddComponent<RawImage>();
        wall.transform.SetParent(canvas.transform, false);
        wall.texture = StoneWall.Build();
        wall.raycastTarget = false;
        UITheme.Stretch(wall.rectTransform);

        // Resplandor violeta que sube desde el suelo, bajo las enredaderas
        Image haze = UITheme.Icon(canvas.transform, "Bruma", BuffIcons.Glow, new Color(0.5f, 0.2f, 0.8f, 0.24f));
        UITheme.Place(haze.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(2600f, 900f));

        // Enredaderas y su brillo
        RectTransform vineRoot = UITheme.NewRect("Enredaderas", canvas.transform);
        UITheme.Stretch(vineRoot);
        MenuVines vines = vineRoot.gameObject.AddComponent<MenuVines>();
        vines.raycastTarget = false;
        RectTransform glowRoot = UITheme.NewRect("BrilloEspinas", canvas.transform);
        UITheme.Stretch(glowRoot);
        VineGlow glow = glowRoot.gameObject.AddComponent<VineGlow>();
        glow.raycastTarget = false;
        glow.source = vines;

        // Esporas como las de los hongos de la sala del jefe
        RectTransform spores = UITheme.NewRect("Esporas", canvas.transform);
        UITheme.Stretch(spores);
        spores.gameObject.AddComponent<SporeField>();

        // Viñeta para oscurecer los bordes
        Image vignette = UITheme.Icon(canvas.transform, "Vineta", UISprites.Vignette, new Color(1f, 1f, 1f, 0.9f));
        UITheme.Stretch(vignette.rectTransform);

        return canvas;
    }
}

// ---------- Muro de piedra procedural ----------

public static class StoneWall
{
    const int Width = 960;
    const int Height = 540;

    static float Hash(float x, float y)
    {
        float v = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
        return v - Mathf.Floor(v);
    }

    static float Value(float x, float y)
    {
        float ix = Mathf.Floor(x), iy = Mathf.Floor(y);
        float fx = x - ix, fy = y - iy;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        float a = Hash(ix, iy), b = Hash(ix + 1f, iy), c = Hash(ix, iy + 1f), d = Hash(ix + 1f, iy + 1f);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    public static Texture2D Build()
    {
        Color32[] pixels = new Color32[Width * Height];
        System.Random rng = new System.Random(4242);
        Color mortar = new Color(0.035f, 0.03f, 0.045f);

        // Fondo por si algún píxel queda sin piedra
        for (int i = 0; i < pixels.Length; i++) pixels[i] = mortar;

        int y0 = 0;
        int rowIndex = 0;
        while (y0 < Height)
        {
            int rowHeight = 46 + rng.Next(0, 26);
            int y1 = Mathf.Min(Height, y0 + rowHeight);
            int x0 = -rng.Next(0, 120);
            while (x0 < Width)
            {
                int stoneWidth = 90 + rng.Next(0, 110);
                int x1 = x0 + stoneWidth;
                FillStone(pixels, Mathf.Max(0, x0), y0, Mathf.Min(Width, x1), y1, x0, x1, (float)rng.NextDouble(), rowIndex);
                x0 = x1;
            }
            y0 = y1;
            rowIndex++;
        }

        Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        texture.name = "MuroDePiedra";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    static void FillStone(Color32[] pixels, int fromX, int y0, int toX, int y1, int stoneX0, int stoneX1, float tone, int row)
    {
        float brightness = 0.7f + 0.5f * tone;
        float tint = Hash(stoneX0 * 0.37f, row * 1.7f);
        Color baseColor = Color.Lerp(new Color(0.27f, 0.27f, 0.31f), new Color(0.3f, 0.26f, 0.32f), tint) * brightness;

        for (int y = y0; y < y1; y++)
        {
            for (int x = fromX; x < toX; x++)
            {
                float edgeNoise = (Value(x * 0.09f, y * 0.09f) - 0.5f) * 5f;
                float left = x - stoneX0, right = stoneX1 - 1 - x;
                float bottom = y - y0, top = y1 - 1 - y;
                float edge = Mathf.Min(Mathf.Min(left, right), Mathf.Min(bottom, top)) + edgeNoise;

                if (edge < 2.2f)
                {
                    float m = Mathf.Clamp01(edge / 2.2f);
                    pixels[y * Width + x] = Color.Lerp(new Color(0.03f, 0.027f, 0.04f), new Color(0.09f, 0.085f, 0.11f), m);
                    continue;
                }

                // Relieve: se ilumina arriba/izquierda y se oscurece abajo/derecha
                float bevel = 0f;
                if (top < 9f) bevel += 0.2f * (1f - top / 9f);
                if (left < 8f) bevel += 0.1f * (1f - left / 8f);
                if (bottom < 10f) bevel -= 0.28f * (1f - bottom / 10f);
                if (right < 8f) bevel -= 0.14f * (1f - right / 8f);

                float surface = Value(x * 0.03f + row, y * 0.03f) * 0.5f + Value(x * 0.12f, y * 0.12f) * 0.3f + Value(x * 0.55f, y * 0.55f) * 0.2f;
                float shade = 0.66f + 0.55f * surface + bevel;

                // Musgo oscuro que se acumula en la parte baja del muro
                float moss = Mathf.SmoothStep(0.55f, 0.9f, Value(x * 0.02f + 40f, y * 0.04f)) * Mathf.Clamp01(1f - (float)y / Height * 1.1f);

                Color c = baseColor * shade;
                c = Color.Lerp(c, new Color(0.08f, 0.14f, 0.1f) * (0.8f + surface * 0.5f), moss * 0.6f);

                // Oscurece hacia arriba para que el muro se pierda en la sombra
                float depth = Mathf.Lerp(1f, 0.45f, (float)y / Height);
                c *= 0.78f * depth;
                c.a = 1f;
                pixels[y * Width + x] = c;
            }
        }
    }
}
