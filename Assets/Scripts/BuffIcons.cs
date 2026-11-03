using System.Linq;
using UnityEngine;

// Sprites blancos generados por código para los iconos de buffs.
// Comunes: rayo = velocidad, espada = daño. Mejorados: doble rayo, espadas cruzadas y cruz de regeneración.
public static class BuffIcons
{
    const int Size = 128;
    const int Samples = 3;

    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    static readonly Vector2[] BoltPoints =
    {
        new Vector2(0.60f, 0.95f), new Vector2(0.24f, 0.48f), new Vector2(0.45f, 0.48f),
        new Vector2(0.36f, 0.05f), new Vector2(0.76f, 0.58f), new Vector2(0.55f, 0.58f)
    };

    static readonly Vector2[][] SwordPolygons =
    {
        new[] { new Vector2(0.45f, 0.30f), new Vector2(0.55f, 0.30f), new Vector2(0.55f, 0.80f), new Vector2(0.50f, 0.96f), new Vector2(0.45f, 0.80f) },
        new[] { new Vector2(0.28f, 0.23f), new Vector2(0.72f, 0.23f), new Vector2(0.72f, 0.30f), new Vector2(0.28f, 0.30f) },
        new[] { new Vector2(0.47f, 0.10f), new Vector2(0.53f, 0.10f), new Vector2(0.53f, 0.23f), new Vector2(0.47f, 0.23f) },
        new[] { new Vector2(0.43f, 0.04f), new Vector2(0.57f, 0.04f), new Vector2(0.57f, 0.10f), new Vector2(0.43f, 0.10f) }
    };

    static Sprite circle, ring, bolt, doubleBolt, sword, crossedSwords, plus, arrow, bullets, glow, heart, chevrons, diamond, bullet;

    public static Sprite Circle => circle != null ? circle : (circle = Render((x, y) => DistanceSqr(x, y) <= 0.25f));

    public static Sprite Ring => ring != null ? ring : (ring = Render((x, y) =>
    {
        float d = DistanceSqr(x, y);
        return d <= 0.25f && d >= 0.19f;
    }));

    public static Sprite Bolt => bolt != null ? bolt : (bolt = Render(InPolygons(BoltPoints)));

    public static Sprite DoubleBolt => doubleBolt != null ? doubleBolt : (doubleBolt = Render(InPolygons(
        Transform(BoltPoints, 0f, 0.78f, new Vector2(-0.14f, -0.06f)),
        Transform(BoltPoints, 0f, 0.78f, new Vector2(0.14f, 0.06f)))));

    public static Sprite Sword => sword != null ? sword : (sword = Render(InPolygons(SwordPolygons)));

    public static Sprite CrossedSwords => crossedSwords != null ? crossedSwords : (crossedSwords = Render(InPolygons(
        SwordPolygons.Select(polygon => Transform(polygon, 40f, 0.95f, Vector2.zero))
            .Concat(SwordPolygons.Select(polygon => Transform(polygon, -40f, 0.95f, Vector2.zero)))
            .ToArray())));

    public static Sprite Plus => plus != null ? plus : (plus = Render(InPolygons(
        new[] { new Vector2(0.18f, 0.39f), new Vector2(0.82f, 0.39f), new Vector2(0.82f, 0.61f), new Vector2(0.18f, 0.61f) },
        new[] { new Vector2(0.39f, 0.18f), new Vector2(0.61f, 0.18f), new Vector2(0.61f, 0.82f), new Vector2(0.39f, 0.82f) })));

    // Flecha apuntando hacia arriba (marcador de objetivo)
    public static Sprite Arrow => arrow != null ? arrow : (arrow = Render(InPolygons(
        new[] { new Vector2(0.5f, 0.97f), new Vector2(0.95f, 0.45f), new Vector2(0.68f, 0.45f), new Vector2(0.68f, 0.05f), new Vector2(0.32f, 0.05f), new Vector2(0.32f, 0.45f), new Vector2(0.05f, 0.45f) })));

    // Tres balas en fila (Disparo Continuo)
    public static Sprite Bullets => bullets != null ? bullets : (bullets = Render(InPolygons(
        Bullet(0.24f, 0.10f), Bullet(0.50f, 0.18f), Bullet(0.76f, 0.26f))));

    // Resplandor circular suave (halo del marcador de objetivo)
    public static Sprite Glow => glow != null ? glow : (glow = RenderSoft((x, y) =>
    {
        float d = Mathf.Sqrt(DistanceSqr(x, y)) * 2f;
        return Mathf.Pow(Mathf.Clamp01(1f - d), 1.6f);
    }));

    static Vector2[] Bullet(float x, float baseY)
    {
        return new[]
        {
            new Vector2(x - 0.075f, baseY), new Vector2(x + 0.075f, baseY),
            new Vector2(x + 0.075f, baseY + 0.50f), new Vector2(x, baseY + 0.72f), new Vector2(x - 0.075f, baseY + 0.50f)
        };
    }

    // Corazón (vida)
    public static Sprite Heart => heart != null ? heart : (heart = Render((x, y) =>
    {
        float px = (x - 0.5f) * 2.5f, py = (y - 0.47f) * 2.5f;
        float a = px * px + py * py - 1f;
        return a * a * a - px * px * py * py * py <= 0f;
    }));

    // Dos chevrones hacia la derecha (dash)
    public static Sprite Chevrons => chevrons != null ? chevrons : (chevrons = Render(InPolygons(
        new[] { new Vector2(0.10f, 0.2f), new Vector2(0.36f, 0.5f), new Vector2(0.10f, 0.8f), new Vector2(0.24f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0.24f, 0.2f) },
        new[] { new Vector2(0.46f, 0.2f), new Vector2(0.72f, 0.5f), new Vector2(0.46f, 0.8f), new Vector2(0.60f, 0.8f), new Vector2(0.86f, 0.5f), new Vector2(0.60f, 0.2f) })));

    public static Sprite Diamond => diamond != null ? diamond : (diamond = Render(InPolygons(
        new[] { new Vector2(0.5f, 0.97f), new Vector2(0.97f, 0.5f), new Vector2(0.5f, 0.03f), new Vector2(0.03f, 0.5f) })));

    // Una sola bala vertical (contador de munición)
    public static Sprite SingleBullet => bullet != null ? bullet : (bullet = Render(InPolygons(
        new[] { new Vector2(0.22f, 0.02f), new Vector2(0.78f, 0.02f), new Vector2(0.78f, 0.58f), new Vector2(0.5f, 0.98f), new Vector2(0.22f, 0.58f) })));

    delegate bool Shape(float x, float y);

    static float DistanceSqr(float x, float y) => (x - 0.5f) * (x - 0.5f) + (y - 0.5f) * (y - 0.5f);

    // Rota (grados, antihorario), escala respecto al centro y desplaza un polígono
    static Vector2[] Transform(Vector2[] points, float degrees, float scale, Vector2 offset)
    {
        Quaternion rotation = Quaternion.Euler(0f, 0f, degrees);
        return points.Select(p => Center + (Vector2)(rotation * ((p - Center) * scale)) + offset).ToArray();
    }

    static Shape InPolygons(params Vector2[][] polygons)
    {
        return (x, y) =>
        {
            foreach (Vector2[] polygon in polygons)
            {
                if (Contains(polygon, x, y)) return true;
            }
            return false;
        };
    }

    // Prueba de punto dentro de polígono (par-impar)
    static bool Contains(Vector2[] polygon, float x, float y)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if ((polygon[i].y > y) != (polygon[j].y > y) &&
                x < (polygon[j].x - polygon[i].x) * (y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
            {
                inside = !inside;
            }
        }
        return inside;
    }

    // Rasteriza la forma con supermuestreo para obtener bordes suaves
    static Sprite Render(Shape shape)
    {
        Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int py = 0; py < Size; py++)
        {
            for (int px = 0; px < Size; px++)
            {
                int hits = 0;
                for (int sy = 0; sy < Samples; sy++)
                {
                    for (int sx = 0; sx < Samples; sx++)
                    {
                        float x = (px + (sx + 0.5f) / Samples) / Size;
                        float y = (py + (sy + 0.5f) / Samples) / Size;
                        if (shape(x, y)) hits++;
                    }
                }
                texture.SetPixel(px, py, new Color(1f, 1f, 1f, hits / (float)(Samples * Samples)));
            }
        }
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f));
    }

    static Sprite RenderSoft(System.Func<float, float, float> alpha)
    {
        Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int py = 0; py < Size; py++)
        {
            for (int px = 0; px < Size; px++)
            {
                texture.SetPixel(px, py, new Color(1f, 1f, 1f, alpha((px + 0.5f) / Size, (py + 0.5f) / Size)));
            }
        }
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f));
    }
}
