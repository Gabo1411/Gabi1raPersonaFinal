using UnityEngine;

// Arte de la interfaz generado por código: marcos de piedra con bordes de bronce, losas para botones,
// separadores ornamentales y viñeta.
public static class UISprites
{
    static Sprite frame, slab, divider, vignette;

    public static Sprite Frame => frame != null ? frame : (frame = MakeFrame());
    public static Sprite Slab => slab != null ? slab : (slab = MakeSlab());
    public static Sprite Divider => divider != null ? divider : (divider = MakeDivider());
    public static Sprite Vignette => vignette != null ? vignette : (vignette = MakeVignette());

    static readonly Color32 Clear = new Color32(0, 0, 0, 0);

    static Sprite Build(Color32[] pixels, int width, int height, Vector4 border)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixels32(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    static float Noise(float x, float y)
    {
        return Mathf.PerlinNoise(x * 0.09f, y * 0.09f) * 0.6f + Mathf.PerlinNoise(x * 0.45f + 31f, y * 0.45f + 17f) * 0.4f;
    }

    // Distancia hacia dentro desde el borde de un rectángulo con las esquinas cortadas (chaflán)
    static float EdgeDistance(float fx, float fy, float width, float height, float cut)
    {
        float cx = Mathf.Min(fx, width - fx), cy = Mathf.Min(fy, height - fy);
        float edge = Mathf.Min(cx, cy);
        float corner = (cx + cy - cut) * 0.7071f;
        return Mathf.Min(edge, corner);
    }

    // Marco de piedra oscura con doble borde de bronce y remaches en las esquinas (9-slice, borde de 44 px)
    static Sprite MakeFrame()
    {
        const int S = 128;
        const float Cut = 20f;
        Color32[] pixels = new Color32[S * S];

        Color bronzeBright = new Color(0.72f, 0.53f, 0.27f), bronzeDim = new Color(0.4f, 0.28f, 0.14f);

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float d = EdgeDistance(fx, fy, S, S, Cut);
                float alpha = Mathf.Clamp01(d + 0.5f);
                if (alpha <= 0f) { pixels[y * S + x] = Clear; continue; }

                Color c;
                if (d < 3.2f) c = Color.Lerp(bronzeDim, bronzeBright, fy / S);
                else if (d < 5.2f) c = new Color(0.03f, 0.025f, 0.03f);
                else if (d < 7f) c = bronzeDim;
                else if (d < 9f) c = new Color(0.04f, 0.035f, 0.04f);
                else
                {
                    float grain = Noise(fx, fy);
                    float shade = 0.08f + 0.055f * grain;
                    float inner = Mathf.Clamp01((d - 9f) / 28f);
                    shade *= 0.62f + 0.38f * inner;
                    shade += 0.012f * (fy / S);
                    c = new Color(shade * 1.02f, shade * 0.97f, shade * 1.1f);
                }

                // Remaches de bronce en las esquinas
                float ox = Mathf.Min(fx, S - fx), oy = Mathf.Min(fy, S - fy);
                float stud = Mathf.Abs(ox - 27f) + Mathf.Abs(oy - 27f);
                if (stud < 6.5f) c = stud < 3f ? new Color(0.95f, 0.78f, 0.45f) : bronzeBright;

                pixels[y * S + x] = new Color(c.r, c.g, c.b, alpha);
            }
        }
        return Build(pixels, S, S, new Vector4(44, 44, 44, 44));
    }

    // Losa en tonos de gris con bisel: se tiñe con el color del botón (9-slice, borde de 20 px)
    static Sprite MakeSlab()
    {
        const int S = 64;
        const float Cut = 9f;
        Color32[] pixels = new Color32[S * S];

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float d = EdgeDistance(fx, fy, S, S, Cut);
                float alpha = Mathf.Clamp01(d + 0.5f);
                if (alpha <= 0f) { pixels[y * S + x] = Clear; continue; }

                float v;
                if (d < 1.8f) v = 0.3f;
                else if (d < 3.6f) v = fy > S * 0.5f ? 1f : 0.45f;
                else
                {
                    v = 0.74f + 0.06f * Noise(fx * 1.6f, fy * 1.6f) + 0.1f * (fy / S - 0.5f);
                    v *= 0.88f + 0.12f * Mathf.Clamp01((d - 3.6f) / 14f);
                }
                pixels[y * S + x] = new Color(v, v, v, alpha);
            }
        }
        return Build(pixels, S, S, new Vector4(20, 20, 20, 20));
    }

    // Línea con rombo central y dos rombos pequeños a los lados; se desvanece hacia los extremos
    static Sprite MakeDivider()
    {
        const int W = 512, H = 32;
        Color32[] pixels = new Color32[W * H];

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float dx = fx - W / 2f, dy = fy - H / 2f;
                float a = 0f;

                float center = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (center < 10f) a = 1f;
                else if (center < 11.5f) a = Mathf.Clamp01(11.5f - center);

                float side = Mathf.Abs(Mathf.Abs(dx) - 30f) + Mathf.Abs(dy);
                if (side < 4.5f) a = Mathf.Max(a, 1f);

                // Líneas finas a ambos lados con desvanecido
                float ax = Mathf.Abs(dx);
                if (ax > 44f && ax < 250f && Mathf.Abs(dy) < 1.1f)
                {
                    float fade = 1f - Mathf.Clamp01((ax - 44f) / 206f);
                    a = Mathf.Max(a, fade * Mathf.Clamp01(1.6f - Mathf.Abs(dy)));
                }

                pixels[y * W + x] = new Color(1f, 1f, 1f, a);
            }
        }
        return Build(pixels, W, H, Vector4.zero);
    }

    // Oscurecimiento radial hacia los bordes de la pantalla
    static Sprite MakeVignette()
    {
        const int S = 256;
        Color32[] pixels = new Color32[S * S];

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f) / S * 2f - 1f, dy = (y + 0.5f) / S * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;
                float a = Mathf.Pow(Mathf.SmoothStep(0.3f, 1f, r), 1.3f) * 0.9f;
                pixels[y * S + x] = new Color(0f, 0f, 0f, a);
            }
        }
        return Build(pixels, S, S, Vector4.zero);
    }
}
