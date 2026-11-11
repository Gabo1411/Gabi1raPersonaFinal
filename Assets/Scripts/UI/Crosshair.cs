using UnityEngine;
using UnityEngine.UI;

// Mira blanca discreta de cuatro espinas que apuntan al centro, sin punto central. Al disparar las espinas se abren
// y vuelven; al recargar la mira gira a una X y se atenúa.
public class Crosshair : MonoBehaviour
{
    const float BaseGap = 7f;      // distancia del centro al inicio de cada espina
    const float TickLength = 17f;
    const float TickWidth = 6.5f;
    const float ShotKick = 6f;

    static Sprite thorn, diamond;

    private RectTransform spinner;
    private readonly RectTransform[] ticks = new RectTransform[4];
    private readonly RectTransform[] outlines = new RectTransform[4];
    private readonly Image[] tickImages = new Image[4];
    private readonly Image[] outlineImages = new Image[4];

    private PlayerMovement player;
    private Weapon weapon;
    private float spread, angle, lastKick, aim;
    private int lastBullets = -1;

    public static Crosshair Create(Transform parent, PlayerMovement player)
    {
        RectTransform root = UITheme.NewRect("Mira", parent);
        UITheme.Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 120f));
        Crosshair crosshair = root.gameObject.AddComponent<Crosshair>();
        crosshair.player = player;
        crosshair.Build(root);
        return crosshair;
    }

    void Build(RectTransform root)
    {
        spinner = UITheme.NewRect("Giro", root);
        UITheme.Place(spinner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 120f));

        Color shadow = new Color(0.02f, 0.01f, 0.03f, 0.6f);

        // Primero los contornos oscuros (para que la mira se lea sobre fondos claros) y luego las espinas blancas
        for (int i = 0; i < 4; i++) outlineImages[i] = MakeTick(spinner, "Contorno", shadow, 1.5f, out outlines[i]);

        for (int i = 0; i < 4; i++) tickImages[i] = MakeTick(spinner, "Espina", Color.white, 1f, out ticks[i]);
    }

    static Image MakeTick(Transform parent, string name, Color color, float grow, out RectTransform rect)
    {
        Image image = UITheme.Icon(parent, name, Thorn, color);
        rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(TickWidth * grow + (grow > 1f ? 2f : 0f), TickLength * grow);
        return image;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (weapon == null) weapon = FindFirstObjectByType<Weapon>();

        bool reloading = false;
        if (weapon != null)
        {
            reloading = weapon.isReloading && !weapon.DisparoContinuoActivo;

            // Cada disparo abre las espinas de golpe (con el disparo continuo no baja la munición: se usa el tiempo)
            if (lastBullets >= 0 && weapon.bulletsLeft < lastBullets) spread = ShotKick;
            else if (weapon.DisparoContinuoActivo && weapon.isFiring && Time.unscaledTime - lastKick > 0.1f)
            {
                lastKick = Time.unscaledTime;
                spread = ShotKick * 0.8f;
            }
            lastBullets = weapon.bulletsLeft;
        }

        spread = Mathf.MoveTowards(spread, 0f, 55f * dt);
        angle = Mathf.Lerp(angle, reloading ? 45f : 0f, 1f - Mathf.Exp(-12f * dt));
        spinner.localRotation = Quaternion.Euler(0f, 0f, angle);

        // En modo francotirador las espinas se cierran sobre el centro y se vuelven más nítidas
        aim = Mathf.MoveTowards(aim, weapon != null && weapon.SniperActive ? 1f : 0f, 6f * dt);

        float gap = BaseGap + spread + (reloading ? 4f : 0f) - 3.5f * aim;
        float alpha = (reloading ? 0.45f : 0.85f + 0.15f * aim) * (player != null && player.IsDead ? 0f : 1f);

        for (int i = 0; i < 4; i++)
        {
            Vector2 direction = i switch { 0 => Vector2.up, 1 => Vector2.right, 2 => Vector2.down, _ => Vector2.left };
            float rotation = Mathf.Atan2(direction.x, -direction.y) * Mathf.Rad2Deg;
            Vector2 center = direction * (gap + TickLength * 0.5f);

            ticks[i].anchoredPosition = center;
            ticks[i].localRotation = Quaternion.Euler(0f, 0f, rotation);
            outlines[i].anchoredPosition = center;
            outlines[i].localRotation = Quaternion.Euler(0f, 0f, rotation);

            tickImages[i].color = new Color(1f, 1f, 1f, alpha);
            Color o = outlineImages[i].color; o.a = 0.6f * alpha; outlineImages[i].color = o;
        }

    }

    // ---------- Sprites generados por código ----------

    // Espina: triángulo con la punta hacia arriba (hacia el centro de la mira) y la base ancha hacia fuera.
    static Sprite Thorn => thorn != null ? thorn : (thorn = Build(32, 96, (x, y, w, h) =>
    {
        // Base abajo (y = 0), punta arriba (y = h)
        float cx = w * 0.5f;
        float halfBase = w * 0.5f - 1.5f;
        float dEdge = Mathf.Min(y - 1.5f, h - 1.5f - y);

        float t = Mathf.Clamp01((y - 1.5f) / (h - 3f));
        float half = Mathf.Lerp(halfBase, 0.3f, Mathf.Pow(t, 0.8f));   // se afina hacia la punta, algo curvada
        float dSide = (half - Mathf.Abs(x - cx)) * 0.93f;
        return Mathf.Min(dEdge, dSide);
    }));

    static Sprite Diamond => diamond != null ? diamond : (diamond = Build(32, 32, (x, y, w, h) =>
    {
        float dx = Mathf.Abs(x - w * 0.5f), dy = Mathf.Abs(y - h * 0.5f);
        return (w * 0.5f - 2f - dx - dy) * 0.7071f;
    }));

    delegate float Distance(float x, float y, float w, float h);

    // Convierte una distancia con signo (positiva dentro) en una máscara blanca suavizada
    static Sprite Build(int width, int height, Distance inside)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float d = inside(x + 0.5f, y + 0.5f, width, height);
                byte a = (byte)(Mathf.Clamp01(d + 0.5f) * 255f);
                pixels[y * width + x] = new Color32(255, 255, 255, a);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
