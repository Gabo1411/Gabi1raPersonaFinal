using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Aparición de los hongos de la arena del jefe: un brote diminuto late unos instantes mientras las esporas
// se acumulan, y luego se infla con un rebote hasta su tamaño con una explosión de esporas y un anillo de energía.
public class EnemySproutIn : MonoBehaviour, IEntrance
{
    const float SproutTime = 1.3f;
    const float GrowTime = 0.9f;
    const float SeedScale = 0.12f;

    public bool IsEmerging { get; private set; }

    private EnemyBase enemy;
    private NavMeshAgent agent;
    private Collider hitCollider;

    public static EnemySproutIn Begin(EnemyBase enemy, NavMeshAgent agent, Collider hitCollider)
    {
        EnemySproutIn sprout = enemy.gameObject.AddComponent<EnemySproutIn>();
        sprout.enemy = enemy;
        sprout.agent = agent;
        sprout.hitCollider = hitCollider;
        sprout.IsEmerging = true;
        sprout.StartCoroutine(sprout.Run());
        return sprout;
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 2.2f, c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    IEnumerator Run()
    {
        Vector3 groundPos = transform.position;
        Vector3 fullScale = transform.localScale;

        float height = 1.7f;
        bool hasBounds = false;
        Bounds bounds = new Bounds(groundPos, Vector3.zero);
        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            if (rend is ParticleSystemRenderer) continue;
            if (!hasBounds) { bounds = rend.bounds; hasBounds = true; }
            else bounds.Encapsulate(rend.bounds);
        }
        if (hasBounds) height = Mathf.Max(bounds.size.y, 0.5f);
        float size = Mathf.Clamp(height / 1.7f, 0.8f, 3f);

        // Suelo y altura de reposo (el agente deja al enemigo a cierta altura sobre el NavMesh)
        float groundY = hasBounds ? bounds.min.y : groundPos.y;
        Vector3 restPos = groundPos;
        if (agent != null && NavMesh.SamplePosition(groundPos, out NavMeshHit hit, 8f, NavMesh.AllAreas))
        {
            groundY = hit.position.y;
            restPos = new Vector3(groundPos.x, groundY + agent.baseOffset * transform.lossyScale.y, groundPos.z);
        }
        float pivotHeight = restPos.y - groundY;
        Vector3 feet = new Vector3(groundPos.x, groundY, groundPos.z);

        if (agent != null) agent.enabled = false;
        if (hitCollider != null) hitCollider.enabled = false;

        SproutEffects effects = SproutEffects.Create(feet, size);

        // Fase 1: brote diminuto que late mientras se acumulan las esporas
        float elapsed = 0f;
        while (elapsed < SproutTime)
        {
            if (enemy == null || enemy.health <= 0) { effects.Finish(); yield break; }
            elapsed += Time.deltaTime;
            float p = elapsed / SproutTime;
            float beat = 0.5f + 0.5f * Mathf.Sin(elapsed * (8f + p * 14f));
            float s = SeedScale * (0.9f + 0.35f * beat) * (1f + p * 0.5f);
            SetScale(fullScale, s, feet, pivotHeight);
            effects.SetGlow(0.4f + 0.6f * beat * p + 0.3f * p);
            yield return null;
        }

        // Fase 2: se infla con rebote hasta su tamaño
        elapsed = 0f;
        bool popped = false;
        while (elapsed < GrowTime)
        {
            if (enemy == null || enemy.health <= 0) { effects.Finish(); yield break; }
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / GrowTime);
            float s = Mathf.LerpUnclamped(SeedScale, 1f, EaseOutBack(p));
            SetScale(fullScale, s, feet, pivotHeight);
            effects.SetGlow(1f - p);
            if (!popped && p > 0.45f) { popped = true; effects.Pop(); }
            yield return null;
        }

        transform.localScale = fullScale;
        transform.position = restPos;
        effects.Finish();

        if (hitCollider != null) hitCollider.enabled = true;
        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh) agent.Warp(restPos);
        }
        IsEmerging = false;
        Destroy(this);
    }

    // Al cambiar de tamaño el enemigo sigue apoyado en el suelo aunque su pivote no esté en los pies
    void SetScale(Vector3 fullScale, float s, Vector3 feet, float pivotHeight)
    {
        transform.localScale = fullScale * s;
        transform.position = feet + Vector3.up * pivotHeight * s;
    }
}

// Partículas del brote: esporas que se acumulan, luz violeta y estallido final
public class SproutEffects : MonoBehaviour
{
    private ParticleSystem gather, burst, ring;
    private Light glow;
    private float size = 1f;

    public static SproutEffects Create(Vector3 position, float size)
    {
        GameObject root = new GameObject("BroteDeHongo");
        root.transform.position = position;
        SproutEffects effects = root.AddComponent<SproutEffects>();
        effects.size = size;
        effects.Build();
        Destroy(root, 6f);
        return effects;
    }

    void Build()
    {
        // Esporas que convergen hacia el brote (velocidad negativa = hacia dentro)
        gather = Make("Acumulan", ParticleMaterials.Glow, 120, new Vector3(0f, 0.25f * size, 0f));
        ParticleSystem.MainModule g = gather.main;
        g.loop = true;
        g.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
        g.startSpeed = new ParticleSystem.MinMaxCurve(-2.4f * size, -1.2f * size);
        g.startSize = new ParticleSystem.MinMaxCurve(0.07f * size, 0.17f * size);
        g.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.3f, 1f), new Color(0.3f, 0.9f, 1f));
        Sphere(gather, 1.5f * size);
        Shrink(gather);
        ParticleSystem.EmissionModule ge = gather.emission;
        ge.rateOverTime = 45f;

        // Estallido de esporas al inflarse
        burst = Make("Esporas", ParticleMaterials.Glow, 140, new Vector3(0f, 0.5f * size, 0f));
        ParticleSystem.MainModule b = burst.main;
        b.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        b.startSpeed = new ParticleSystem.MinMaxCurve(1.5f * size, 4.5f * size);
        b.startSize = new ParticleSystem.MinMaxCurve(0.1f * size, 0.24f * size);
        b.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.3f, 1f), new Color(0.3f, 0.9f, 1f));
        b.gravityModifier = -0.06f;
        Sphere(burst, 0.3f * size);
        Shrink(burst);
        ParticleSystem.NoiseModule noise = burst.noise;
        noise.enabled = true;
        noise.strength = 0.6f;
        noise.frequency = 0.8f;
        ParticleSystem.EmissionModule be = burst.emission;
        be.rateOverTime = 0f;

        // Anillo de energía por el suelo
        ring = Make("Anillo", ParticleMaterials.Ring, 4, new Vector3(0f, 0.08f, 0f));
        ParticleSystem.MainModule r = ring.main;
        r.startLifetime = 0.7f;
        r.startSpeed = 0f;
        r.startSize = 5.5f * size;
        r.startColor = new Color(0.8f, 0.4f, 1f, 0.85f);
        ParticleSystem.ShapeModule rs = ring.shape;
        rs.enabled = false;
        ParticleSystem.SizeOverLifetimeModule rz = ring.sizeOverLifetime;
        rz.enabled = true;
        rz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.12f, 1f, 1f));
        Fade(ring);
        ring.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        ParticleSystem.EmissionModule re = ring.emission;
        re.rateOverTime = 0f;

        // Luz violeta que late con el brote
        GameObject lightObject = new GameObject("Luz");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = Vector3.up * 0.5f * size;
        glow = lightObject.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(0.7f, 0.35f, 1f);
        glow.range = 5f * size;
        glow.intensity = 0f;
    }

    ParticleSystem Make(string name, Material material, int max, Vector3 localPosition)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPosition;
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 5f;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = max;
        main.scalingMode = ParticleSystemScalingMode.Local;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        particles.Play();
        return particles;
    }

    static void Sphere(ParticleSystem particles, float radius)
    {
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
    }

    static void Shrink(ParticleSystem particles)
    {
        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));
    }

    static void Fade(ParticleSystem particles)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.4f), new GradientAlphaKey(0f, 1f) });
        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = gradient;
    }

    public void SetGlow(float amount)
    {
        if (glow != null) glow.intensity = 2f * size * Mathf.Clamp01(amount);
    }

    public void Pop()
    {
        burst.Emit(70);
        ring.Emit(1);
    }

    public void Finish()
    {
        ParticleSystem.EmissionModule e = gather.emission;
        e.rateOverTime = 0f;
        if (glow != null) glow.intensity = 0f;
    }
}
