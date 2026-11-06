using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ---------- Esporas ----------

// Esporas luminosas violetas y turquesas que suben flotando, como las de los hongos de la sala del jefe
public class SporeField : MonoBehaviour
{
    const int Count = 70;

    class Spore
    {
        public RectTransform rect;
        public Image image;
        public float x, y, speed, sway, swayPhase, size, baseAlpha, hue, drift;
    }

    private readonly Spore[] spores = new Spore[Count];
    private RectTransform area;

    void Awake()
    {
        area = (RectTransform)transform;
        for (int i = 0; i < Count; i++)
        {
            Spore spore = new Spore();
            spore.image = UITheme.Icon(transform, "Espora", BuffIcons.Glow, Color.white);
            spore.rect = spore.image.rectTransform;
            spore.rect.anchorMin = spore.rect.anchorMax = Vector2.zero;
            spore.rect.pivot = new Vector2(0.5f, 0.5f);
            spores[i] = spore;
            Respawn(spore, true);
        }
    }

    void Respawn(Spore spore, bool anywhere)
    {
        float width = Mathf.Max(area.rect.width, 1920f);
        float height = Mathf.Max(area.rect.height, 1080f);

        spore.x = Random.value * width;
        spore.y = anywhere ? Random.value * height : Random.Range(-30f, 40f);
        spore.speed = Random.Range(10f, 42f);
        spore.sway = Random.Range(0.3f, 1.1f);
        spore.swayPhase = Random.value * 6.28f;
        spore.drift = Random.Range(-8f, 8f);
        spore.size = Random.Range(8f, 30f);
        spore.baseAlpha = Random.Range(0.35f, 0.95f);
        spore.hue = Random.value;
        spore.rect.sizeDelta = Vector2.one * spore.size;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;
        float height = Mathf.Max(area.rect.height, 1080f);
        float width = Mathf.Max(area.rect.width, 1920f);

        foreach (Spore spore in spores)
        {
            spore.y += spore.speed * dt;
            spore.x += spore.drift * dt;
            if (spore.y > height + 30f || spore.x < -40f || spore.x > width + 40f) Respawn(spore, false);

            float x = spore.x + Mathf.Sin(t * spore.sway + spore.swayPhase) * 40f;
            float y = spore.y + Mathf.Sin(t * spore.sway * 1.3f + spore.swayPhase * 2f) * 14f;
            spore.rect.anchoredPosition = new Vector2(x, y);

            float life = spore.y / height;
            float fade = Mathf.Clamp01(life * 5f) * Mathf.Clamp01((1f - life) * 2.5f);
            float twinkle = 0.65f + 0.35f * Mathf.Sin(t * 2.4f + spore.swayPhase * 3f);

            Color tint = Color.Lerp(new Color(0.72f, 0.32f, 1f), new Color(0.3f, 0.92f, 1f), spore.hue);
            tint.a = spore.baseAlpha * fade * twinkle * 0.8f;
            spore.image.color = tint;
        }
    }
}
