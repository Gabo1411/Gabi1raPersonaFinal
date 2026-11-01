using UnityEngine;
using UnityEngine.UI;

// Viñeta a pantalla completa: roja al recibir daño (y pulso suave con poca vida), verde al curarse.
public class DamageFeedback : MonoBehaviour
{
    const float FadeSpeed = 1.6f;
    const float LowHealthThreshold = 0.3f;

    static readonly Color DamageColor = new Color(0.8f, 0f, 0f, 1f);
    static readonly Color HealColor = new Color(0.1f, 0.9f, 0.2f, 1f);

    static DamageFeedback instance;

    Image overlay;
    PlayerMovement player;
    float flash;
    float healFlash;

    public static void Show(float strength)
    {
        if (instance == null) Create();
        instance.flash = Mathf.Max(instance.flash, Mathf.Clamp(strength, 0.35f, 1f));
    }

    public static void ShowHeal()
    {
        if (instance == null) Create();
        instance.healFlash = 0.8f;
    }

    static void Create()
    {
        GameObject root = new GameObject("DamageFeedback");
        instance = root.AddComponent<DamageFeedback>();

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        GameObject imageObject = new GameObject("Vignette", typeof(RectTransform));
        imageObject.transform.SetParent(root.transform, false);

        RectTransform rect = (RectTransform)imageObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        instance.overlay = imageObject.AddComponent<Image>();
        instance.overlay.sprite = CreateVignetteSprite();
        instance.overlay.raycastTarget = false;
        instance.overlay.color = new Color(0.8f, 0f, 0f, 0f);
    }

    static Sprite CreateVignetteSprite()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x / (size - 1f)) * 2f - 1f;
                float dy = (y / (size - 1f)) * 2f - 1f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;
                float alpha = Mathf.SmoothStep(0.35f, 1f, distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    void Update()
    {
        if (overlay == null) return;

        flash = Mathf.MoveTowards(flash, 0f, FadeSpeed * Time.unscaledDeltaTime);
        healFlash = Mathf.MoveTowards(healFlash, 0f, FadeSpeed * Time.unscaledDeltaTime);

        if (player == null) player = FindFirstObjectByType<PlayerMovement>();

        float alpha = flash;
        if (player != null && !player.IsDead && player.Health <= player.maxHealth * LowHealthThreshold)
        {
            float pulse = 0.2f + 0.1f * Mathf.Sin(Time.unscaledTime * 4f);
            alpha = Mathf.Max(alpha, pulse);
        }

        // El destello de curación tiene prioridad visual mientras sea más fuerte que el rojo
        Color color = healFlash > alpha ? HealColor : DamageColor;
        if (healFlash > alpha) alpha = healFlash;

        bool gameEnded = GameManager.Instance != null && GameManager.Instance.GameEnded;
        if (gameEnded) alpha = 0f;

        color.a = alpha;
        overlay.color = color;
    }
}
