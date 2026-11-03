using UnityEngine;
using UnityEngine.UI;

// Brasas que suben flotando detrás de los menús. Usa tiempo sin escala para animarse con el juego en pausa.
public class EmberField : MonoBehaviour
{
    const int Count = 34;

    class Ember
    {
        public RectTransform rect;
        public Image image;
        public float x, y, speed, sway, swayPhase, size, baseAlpha;
    }

    private readonly Ember[] embers = new Ember[Count];
    private RectTransform area;

    void Awake()
    {
        area = (RectTransform)transform;

        for (int i = 0; i < Count; i++)
        {
            Ember ember = new Ember();
            ember.image = UITheme.Icon(transform, "Brasa", BuffIcons.Glow, Color.white);
            ember.rect = ember.image.rectTransform;
            ember.rect.anchorMin = ember.rect.anchorMax = Vector2.zero;
            ember.rect.pivot = new Vector2(0.5f, 0.5f);
            embers[i] = ember;
            Respawn(ember, true);
        }
    }

    void Respawn(Ember ember, bool anywhere)
    {
        float width = Mathf.Max(area.rect.width, 1920f);
        float height = Mathf.Max(area.rect.height, 1080f);

        ember.x = Random.value * width;
        ember.y = anywhere ? Random.value * height : -20f;
        ember.speed = Random.Range(28f, 90f);
        ember.sway = Random.Range(0.5f, 1.6f);
        ember.swayPhase = Random.value * 6.28f;
        ember.size = Random.Range(8f, 26f);
        ember.baseAlpha = Random.Range(0.35f, 0.9f);
        ember.rect.sizeDelta = Vector2.one * ember.size;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;
        float height = Mathf.Max(area.rect.height, 1080f);

        foreach (Ember ember in embers)
        {
            ember.y += ember.speed * dt;
            if (ember.y > height + 20f) Respawn(ember, false);

            float x = ember.x + Mathf.Sin(t * ember.sway + ember.swayPhase) * 34f;
            ember.rect.anchoredPosition = new Vector2(x, ember.y);

            // Se desvanecen al nacer y al llegar arriba, y titilan un poco
            float life = ember.y / height;
            float fade = Mathf.Clamp01(life * 6f) * Mathf.Clamp01((1f - life) * 2.2f);
            float flicker = 0.75f + 0.25f * Mathf.Sin(t * 7f + ember.swayPhase * 3f);

            Color color = new Color(1f, 0.55f + 0.25f * ember.baseAlpha, 0.18f, ember.baseAlpha * fade * flicker);
            ember.image.color = color;
        }
    }
}

// Mantiene el tamaño de un RectTransform igual al de otro más un margen (halo detrás de una tarjeta de alto automático)
public class MatchSize : MonoBehaviour
{
    public RectTransform target;
    public float padding = 240f;

    void LateUpdate()
    {
        if (target == null) return;

        ((RectTransform)transform).sizeDelta = target.rect.size + Vector2.one * padding;
        ((RectTransform)transform).anchoredPosition = target.anchoredPosition;
    }
}
