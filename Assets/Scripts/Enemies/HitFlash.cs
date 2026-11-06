using System.Collections.Generic;
using UnityEngine;

// Destello rojo breve al recibir un impacto. Tiñe los materiales del propio modelo, así que el destello
// tiene exactamente la forma de la silueta del enemigo.
public class HitFlash : MonoBehaviour
{
    const float Duration = 0.2f;
    static readonly Color FlashColor = new Color(1f, 0.1f, 0.06f);

    class Slot
    {
        public Material material;
        public int colorId = -1;
        public Color baseColor;
        public bool hadEmission;
        public Color baseEmission;
    }

    private readonly List<Slot> slots = new List<Slot>();
    private bool built;
    private float timer;

    public static void Play(Component target)
    {
        HitFlash flash = target.GetComponent<HitFlash>();
        if (flash == null) flash = target.gameObject.AddComponent<HitFlash>();
        flash.Trigger();
    }

    void Build()
    {
        built = true;
        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            if (rend is ParticleSystemRenderer || rend is TrailRenderer || rend is LineRenderer) continue;

            // renderer.materials crea copias propias de este enemigo, que se destruyen con él
            foreach (Material material in rend.materials)
            {
                // Los materiales transparentes (auras, brillos, cristales) no forman parte de la silueta sólida
                if (material.renderQueue >= 3000) continue;

                Slot slot = new Slot { material = material };
                if (material.HasProperty("_BaseColor")) slot.colorId = Shader.PropertyToID("_BaseColor");
                else if (material.HasProperty("_Color")) slot.colorId = Shader.PropertyToID("_Color");
                if (slot.colorId >= 0) slot.baseColor = material.GetColor(slot.colorId);

                if (material.HasProperty("_EmissionColor"))
                {
                    slot.hadEmission = material.IsKeywordEnabled("_EMISSION");
                    slot.baseEmission = material.GetColor("_EmissionColor");
                }
                slots.Add(slot);
            }
        }
    }

    void Trigger()
    {
        if (!built) Build();
        timer = Duration;
        enabled = true;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        float k = Mathf.Clamp01(timer / Duration);
        k = k * k * (3f - 2f * k); // salida suave
        Apply(k);
        if (timer <= 0f) enabled = false;
    }

    void Apply(float k)
    {
        foreach (Slot slot in slots)
        {
            if (slot.material == null) continue;

            if (slot.colorId >= 0)
                slot.material.SetColor(slot.colorId, Color.Lerp(slot.baseColor, FlashColor, 0.35f * k));

            if (slot.material.HasProperty("_EmissionColor"))
            {
                if (k > 0f)
                {
                    slot.material.EnableKeyword("_EMISSION");
                    // La emisión propia del material (p. ej. un brillo celeste) se funde hacia el rojo en vez de sumarse
                    Color start = slot.hadEmission ? slot.baseEmission : Color.black;
                    slot.material.SetColor("_EmissionColor", Color.Lerp(start, FlashColor * 0.5f, k));
                }
                else
                {
                    if (!slot.hadEmission) slot.material.DisableKeyword("_EMISSION");
                    slot.material.SetColor("_EmissionColor", slot.baseEmission);
                }
            }
        }
    }

    void OnDestroy()
    {
        foreach (Slot slot in slots) if (slot.material != null) Destroy(slot.material);
    }
}
