using UnityEngine;

// El candelabro de la arena cuelga de sus cadenas y se balancea despacio (más cuanto peor va el jefe),
// y de sus llamas caen brasas y chispas. El modelo original está marcado como estático (no se puede mover),
// así que se dibuja una copia suya que sí se mueve y se apaga el original.
public class ArenaChandelier : MonoBehaviour
{
    private Transform swing;
    private Vector3 pivot, basePosition;
    private Quaternion baseRotation;
    private Boss boss;
    private float seed;

    public static ArenaChandelier Create(Transform arena, Boss boss)
    {
        Transform original = arena.Find("Chandelier");
        if (original == null) return null;

        MeshFilter filter = original.GetComponent<MeshFilter>();
        MeshRenderer renderer = original.GetComponent<MeshRenderer>();
        if (filter == null || renderer == null) return null;

        GameObject go = new GameObject("CandelabroColgante");
        go.transform.SetParent(original.parent, false);
        go.transform.localPosition = original.localPosition;
        go.transform.localRotation = original.localRotation;
        go.transform.localScale = original.localScale;

        go.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
        MeshRenderer copy = go.AddComponent<MeshRenderer>();
        copy.sharedMaterials = renderer.sharedMaterials;
        copy.shadowCastingMode = renderer.shadowCastingMode;
        copy.receiveShadows = renderer.receiveShadows;
        renderer.enabled = false;

        // El collider original (una malla enorme sobre el jefe) ya no corresponde a la copia que se balancea:
        // se apaga para que no detenga ni haga explotar los misiles y las balas que pasan por ahí
        foreach (Collider col in original.GetComponents<Collider>()) col.enabled = false;

        // Las llamas y la luz van con el candelabro
        Transform[] children = new Transform[original.childCount];
        for (int i = 0; i < children.Length; i++) children[i] = original.GetChild(i);
        foreach (Transform child in children) child.SetParent(go.transform, true);

        ArenaChandelier chandelier = go.AddComponent<ArenaChandelier>();
        chandelier.boss = boss;
        chandelier.swing = go.transform;
        chandelier.Build(original);
        return chandelier;
    }

    void Build(Transform original)
    {
        basePosition = swing.position;
        baseRotation = swing.rotation;
        seed = Random.Range(0f, 10f);

        // El punto del que cuelga es el techo que hay justo encima
        pivot = basePosition + Vector3.up * 10f;
        float best = float.MaxValue;
        foreach (RaycastHit hit in Physics.RaycastAll(basePosition + Vector3.up * 1f, Vector3.up, 60f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(swing) || hit.collider.transform.IsChildOf(original)) continue;
            if (hit.distance < best) { best = hit.distance; pivot = hit.point; }
        }

        // La luz de las velas titila suavemente
        Light flameLight = swing.GetComponentInChildren<Light>();
        if (flameLight != null)
        {
            TorchFlicker flicker = flameLight.gameObject.AddComponent<TorchFlicker>();
            flicker.amplitude = 0.07f;
            flicker.jitter = 0f;
            flicker.halo = false;
            flicker.embers = false;
        }

        BuildFalling();
    }

    void Update()
    {
        if (swing == null) return;

        // Oscila en dos ejes con periodos distintos para que no parezca un péndulo mecánico
        float danger = boss != null ? 1f + (1f - boss.HealthFraction) * 0.8f : 1f;
        float t = Time.time + seed;
        float ax = 1.1f * Mathf.Sin(t * 1.19f) + 0.3f * Mathf.Sin(t * 2.9f + 1f);
        float az = 0.85f * Mathf.Sin(t * 0.89f + 1.3f) + 0.25f * Mathf.Sin(t * 2.3f);
        float yaw = 1.6f * Mathf.Sin(t * 0.43f + 0.5f);

        Quaternion rotation = Quaternion.Euler(ax * danger, yaw, az * danger);
        swing.position = pivot + rotation * (basePosition - pivot);
        swing.rotation = rotation * baseRotation;
    }

    // Brasas lentas que flotan hacia abajo y chispas rápidas que caen, saliendo del aro de velas
    void BuildFalling()
    {
        Vector3 ringCenter = new Vector3(0f, -1.3f, 0f) * 1f;

        // Brasas
        ParticleSystem embers = GlowKit.NewParticles("BrasasCayendo", swing, GlowKit.Additive, ringCenter);
        embers.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ParticleSystem.MainModule m = embers.main;
        m.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        m.startSpeed = 0.05f;
        m.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
        m.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.6f, 0.2f, 0.85f), new Color(1f, 0.3f, 0.06f, 0.85f));
        m.gravityModifier = 0.07f;
        m.maxParticles = 80;
        ParticleSystem.EmissionModule e = embers.emission;
        e.rateOverTime = 6f;
        ParticleSystem.ShapeModule s = embers.shape;
        s.shapeType = ParticleSystemShapeType.Circle;
        s.radius = 0.7f;
        s.radiusThickness = 1f;
        ParticleSystem.NoiseModule n = embers.noise;
        n.enabled = true;
        n.strength = 0.5f;
        n.frequency = 0.35f;
        ParticleSystem.SizeOverLifetimeModule size = embers.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.25f));
        GlowKit.FadeOut(embers);
        embers.Play();

        // Chispas
        ParticleSystem sparks = GlowKit.NewParticles("ChispasCayendo", swing, GlowKit.Additive, ringCenter);
        sparks.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ParticleSystem.MainModule sm = sparks.main;
        sm.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2.2f);
        sm.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1.2f);
        sm.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
        sm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.6f, 1f), new Color(1f, 0.6f, 0.15f, 1f));
        sm.gravityModifier = 0.55f;
        sm.maxParticles = 60;
        ParticleSystem.EmissionModule se = sparks.emission;
        se.rateOverTime = 2.5f;
        se.SetBursts(new[] { new ParticleSystem.Burst(1f, 3, 6, 0, 2.6f) });
        ParticleSystem.ShapeModule ss = sparks.shape;
        ss.shapeType = ParticleSystemShapeType.Circle;
        ss.radius = 0.7f;
        ss.radiusThickness = 1f;
        GlowKit.FadeOut(sparks);
        sparks.Play();
    }
}
