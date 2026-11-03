using UnityEngine;

// Partículas de muerte propias de los enemigos de la arena del jefe: los cactus revientan en espinas, pétalos y
// polvo verde; los hongos sueltan una nube de esporas luminosas con un anillo de energía.
public static class EnemyDeathEffects
{
    public enum Kind { Cactus, Mushroom }

    // scale agranda el efecto en enemigos más grandes que un cactus normal
    public static void Play(Kind kind, Vector3 position, float scale = 1f)
    {
        GameObject root = new GameObject(kind == Kind.Cactus ? "MuerteCactus" : "MuerteHongo");
        root.transform.position = position;
        root.transform.localScale = Vector3.one * Mathf.Max(0.5f, scale);
        Object.Destroy(root, 4f);

        if (kind == Kind.Cactus) BuildCactus(root.transform);
        else BuildMushroom(root.transform);
    }

    // ---------- Cactus: espinas, pétalos rosados y polvo ----------

    static void BuildCactus(Transform root)
    {
        // Espinas: rayas afiladas amarillo-verdosas que salen disparadas
        ParticleSystem spines = Burst("Espinas", root, ParticleMaterials.Glow, 42, 0.4f);
        ParticleSystem.MainModule spineMain = spines.main;
        spineMain.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
        spineMain.startSpeed = new ParticleSystem.MinMaxCurve(6f, 13f);
        spineMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
        spineMain.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 1f, 0.3f), new Color(1f, 0.95f, 0.45f));
        spineMain.gravityModifier = 0.35f;
        Sphere(spines, 0.25f);
        ShrinkOverLife(spines);
        ParticleSystemRenderer spineRenderer = spines.GetComponent<ParticleSystemRenderer>();
        spineRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        spineRenderer.lengthScale = 6f;
        spineRenderer.velocityScale = 0.1f;

        // Pétalos rosados que caen girando
        ParticleSystem petals = Burst("Petalos", root, ParticleMaterials.Glow, 28, 0.4f);
        ParticleSystem.MainModule petalMain = petals.main;
        petalMain.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.7f);
        petalMain.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
        petalMain.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.26f);
        petalMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.4f, 0.72f), new Color(1f, 0.75f, 0.88f));
        petalMain.gravityModifier = 0.9f;
        Sphere(petals, 0.3f);
        FadeOverLife(petals);

        // Polvo verde que se expande
        ParticleSystem dust = Burst("Polvo", root, ParticleMaterials.Glow, 6, 0.3f);
        ParticleSystem.MainModule dustMain = dust.main;
        dustMain.startLifetime = 0.6f;
        dustMain.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        dustMain.startSize = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
        dustMain.startColor = new Color(0.45f, 0.8f, 0.25f, 0.35f);
        Sphere(dust, 0.3f);
        FadeOverLife(dust);
    }

    // ---------- Hongo: nube de esporas y anillo de energía ----------

    static void BuildMushroom(Transform root)
    {
        // Esporas luminosas violetas y turquesas que suben flotando
        ParticleSystem spores = Burst("Esporas", root, ParticleMaterials.Glow, 70, 1f);
        ParticleSystem.MainModule sporeMain = spores.main;
        sporeMain.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.6f);
        sporeMain.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 3f);
        sporeMain.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        sporeMain.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.3f, 1f), new Color(0.3f, 0.9f, 1f));
        sporeMain.gravityModifier = -0.08f;
        Sphere(spores, 0.4f);
        ParticleSystem.EmissionModule sporeEmission = spores.emission;
        sporeEmission.rateOverTime = 25f;
        ParticleSystem.NoiseModule noise = spores.noise;
        noise.enabled = true;
        noise.strength = 0.6f;
        noise.frequency = 0.8f;
        ShrinkOverLife(spores);
        FadeOverLife(spores);

        // Nube morada que se infla
        ParticleSystem cloud = Burst("Nube", root, ParticleMaterials.Glow, 5, 0.3f);
        ParticleSystem.MainModule cloudMain = cloud.main;
        cloudMain.startLifetime = 1.2f;
        cloudMain.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1f);
        cloudMain.startSize = new ParticleSystem.MinMaxCurve(2.2f, 3.4f);
        cloudMain.startColor = new Color(0.55f, 0.2f, 0.85f, 0.4f);
        Sphere(cloud, 0.3f);
        ParticleSystem.SizeOverLifetimeModule cloudSize = cloud.sizeOverLifetime;
        cloudSize.enabled = true;
        cloudSize.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.2f)));
        FadeOverLife(cloud);

        // Anillo de energía que se expande por el suelo
        ParticleSystem ring = Burst("Anillo", root, ParticleMaterials.Ring, 1, 0.3f);
        ParticleSystem.MainModule ringMain = ring.main;
        ringMain.startLifetime = 0.55f;
        ringMain.startSpeed = 0f;
        ringMain.startSize = 4f;
        ringMain.startColor = new Color(0.8f, 0.4f, 1f, 0.8f);
        ParticleSystem.SizeOverLifetimeModule ringSize = ring.sizeOverLifetime;
        ringSize.enabled = true;
        ringSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.15f, 1f, 1f));
        FadeOverLife(ring);
        ring.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
    }

    // ---------- Utilidades ----------

    static ParticleSystem Burst(string name, Transform parent, Material material, int count, float duration)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.duration = duration;
        main.loop = false;
        main.playOnAwake = true;
        main.maxParticles = count + 40;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = false;

        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
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

    static void ShrinkOverLife(ParticleSystem particles)
    {
        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
    }

    static void FadeOverLife(ParticleSystem particles)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.4f), new GradientAlphaKey(0f, 1f) });

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = gradient;
    }
}
