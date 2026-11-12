using UnityEngine;

// Foco central de la arena: un haz cálido que cae desde lo alto sobre el jefe y deja un charco de luz en el suelo.
// Reacciona al combate: se enciende con un fundido al empezar, se vuelve más intenso y rojizo cuanto menos vida
// le queda al jefe, late como un corazón en la recta final y se apaga al morir.
public class ArenaFocus : MonoBehaviour
{
    [Header("Haz")]
    public float height = 14f;
    public float spotAngle = 50f;
    public float baseIntensity = 800f;

    private Boss boss;
    private Light spot;
    private GlowBillboard pool;
    private float fade;
    private Vector3 poolBase;

    static readonly Color Warm = new Color(1f, 0.8f, 0.55f);
    static readonly Color Blood = new Color(1f, 0.32f, 0.2f);

    public static ArenaFocus Create(Transform arena, Boss boss)
    {
        GameObject go = new GameObject("FocoJefe");
        go.transform.SetParent(arena, true);

        ArenaFocus focus = go.AddComponent<ArenaFocus>();
        focus.boss = boss;
        focus.Build();
        return focus;
    }

    void Build()
    {
        Vector3 center = boss.transform.position;

        // La altura respeta el techo: si hay uno más bajo, el foco queda un poco por debajo
        float h = height;
        foreach (RaycastHit hit in Physics.RaycastAll(center + Vector3.up * 1f, Vector3.up, height + 6f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(boss.transform)) continue;
            h = Mathf.Min(h, hit.distance - 1.5f);
        }
        h = Mathf.Max(5f, h);

        transform.position = center + Vector3.up * h;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        spot = gameObject.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.spotAngle = spotAngle;
        spot.innerSpotAngle = spotAngle * 0.45f;
        spot.range = h + 10f;
        spot.color = Warm;
        spot.intensity = 0f;
        spot.shadows = LightShadows.Soft;
        spot.shadowStrength = 0.85f;
        spot.shadowBias = 0.05f;
        spot.shadowNormalBias = 0.4f;

        // Charco de luz en el suelo, justo bajo el jefe
        float groundY = center.y - 1f;
        if (Physics.Raycast(center + Vector3.up * 0.5f, Vector3.down, out RaycastHit ground, 12f, ~0, QueryTriggerInteraction.Ignore)) groundY = ground.point.y;
        poolBase = new Vector3(center.x, groundY + 0.05f, center.z);
        pool = GlowBillboard.Create(transform.parent, poolBase, 13f, Warm, true);
        pool.intensity = 0f;
    }

    void Update()
    {
        if (boss == null || spot == null) return;

        bool on = boss.gameObject.activeInHierarchy && !boss.IsDead;
        fade = Mathf.MoveTowards(fade, on ? 1f : 0f, Time.deltaTime / (on ? 1.8f : 1.2f));

        float health = boss.HealthFraction;
        float danger = Mathf.Clamp01((0.6f - health) / 0.6f);            // 0 con vida alta, 1 casi muerto
        float pulse = 1f;
        if (health < 0.3f) pulse = 1f + 0.2f * Mathf.Sin(Time.time * Mathf.Lerp(4f, 11f, (0.3f - health) / 0.3f));

        spot.color = Color.Lerp(Warm, Blood, danger * 0.85f);
        spot.intensity = baseIntensity * (1f + 0.6f * danger) * pulse * Mathf.SmoothStep(0f, 1f, fade);

        pool.color = spot.color;
        pool.intensity = 0.55f * (1f + 0.5f * danger) * pulse * fade;
    }
}
