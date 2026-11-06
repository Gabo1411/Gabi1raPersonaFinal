using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Animación de aparición de los enemigos de la cripta: salen reptando de la tierra, inclinados hacia delante,
// a empujones, mientras sueltan polvo y terrones. Mientras dura no persiguen, no atacan ni reciben disparos.
public class EnemyEmerge : MonoBehaviour, IEntrance
{
    const float MinDuration = 2.1f;
    const float MaxDuration = 2.8f;

    public bool IsEmerging { get; private set; }

    private EnemyBase enemy;
    private NavMeshAgent agent;
    private Collider hitCollider;
    private Animator animator;

    public static EnemyEmerge Begin(EnemyBase enemy, NavMeshAgent agent, Collider hitCollider, Animator animator, Transform lookAt)
    {
        EnemyEmerge emerge = enemy.gameObject.AddComponent<EnemyEmerge>();
        emerge.enemy = enemy;
        emerge.agent = agent;
        emerge.hitCollider = hitCollider;
        emerge.animator = animator;
        emerge.IsEmerging = true;
        emerge.StartCoroutine(emerge.Run(lookAt));
        return emerge;
    }

    IEnumerator Run(Transform lookAt)
    {
        Vector3 groundPos = transform.position;
        Quaternion baseRot = transform.rotation;
        if (lookAt != null)
        {
            Vector3 flat = lookAt.position - groundPos;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f) baseRot = Quaternion.LookRotation(flat.normalized, Vector3.up);
        }

        // Altura real del cuerpo para enterrarlo por completo al empezar
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
        float width = hasBounds ? Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.z), 0.6f, 4f) : 1f;
        float scale = Mathf.Clamp(height / 1.7f, 0.8f, 3f);

        // El pivote del enemigo no está en los pies: la animación gira alrededor de los pies y las partículas salen del suelo
        // El suelo se busca en el NavMesh porque el agente coloca al enemigo a cierta altura sobre él (base offset)
        float groundY = hasBounds ? bounds.min.y : groundPos.y;
        Vector3 restPos = groundPos;
        if (agent != null && NavMesh.SamplePosition(groundPos, out NavMeshHit navHit, 8f, NavMesh.AllAreas))
        {
            groundY = navHit.position.y;
            restPos = new Vector3(groundPos.x, groundY + agent.baseOffset * transform.lossyScale.y, groundPos.z);
        }
        float topAboveRoot = hasBounds ? bounds.max.y - groundPos.y : height;
        height = Mathf.Max(0.5f, restPos.y + topAboveRoot - groundY);
        Vector3 feet = new Vector3(groundPos.x, groundY, groundPos.z);
        Vector3 pivotOffset = restPos - feet;

        if (agent != null) agent.enabled = false;
        if (hitCollider != null) hitCollider.enabled = false;
        float originalSpeed = animator != null ? animator.speed : 1f;
        if (animator != null) animator.speed = 0.35f;

        float depth = height + 0.15f;
        float duration = Random.Range(MinDuration, MaxDuration);
        EmergeEffects effects = EmergeEffects.Create(feet, scale, width);
        effects.Burst(EmergeEffects.BurstKind.Opening);

        float seed = Random.value * 10f;
        float lastPulse = 0f;
        float elapsed = 0f;
        int pulses = 0;

        while (elapsed < duration)
        {
            if (enemy == null || enemy.health <= 0) { effects.Finish(); yield break; }

            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);

            // Sube a empujones: cada impulso arranca un poco más
            float rise = Mathf.SmoothStep(0f, 1f, p);
            float pulse = Mathf.Sin(p * 26f + seed);
            rise += 0.045f * pulse * (1f - p);
            rise = Mathf.Clamp01(rise);

            float tilt = Mathf.Lerp(72f, 0f, Mathf.SmoothStep(0.1f, 1f, p));
            float yaw = 7f * Mathf.Sin(p * 19f + seed) * (1f - p);
            float roll = 5f * Mathf.Sin(p * 23f + seed * 2f) * (1f - p);
            Vector3 shake = (1f - p) * 0.05f * new Vector3(Mathf.Sin(p * 61f + seed), 0f, Mathf.Cos(p * 53f + seed));

            Quaternion tiltRotation = baseRot * Quaternion.Euler(tilt, yaw, roll);
            Vector3 feetNow = feet + Vector3.down * depth * (1f - rise) + shake;
            transform.rotation = tiltRotation;
            transform.position = feetNow + Quaternion.Inverse(baseRot) * tiltRotation * pivotOffset;

            // Terrones y polvo en cada empujón, más fuertes al asomar los hombros
            if (pulse > 0.85f && Time.time - lastPulse > 0.28f)
            {
                lastPulse = Time.time;
                pulses++;
                effects.Burst(EmergeEffects.BurstKind.Push);
                if (pulses == 4) effects.Burst(EmergeEffects.BurstKind.Shoulders);
            }
            effects.SetDust(p < 0.9f);

            yield return null;
        }

        // Se pone en pie y arranca la persecución
        transform.position = restPos;
        transform.rotation = baseRot;
        effects.Burst(EmergeEffects.BurstKind.Free);
        effects.Finish();

        if (animator != null) animator.speed = originalSpeed;
        if (hitCollider != null) hitCollider.enabled = true;
        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh) agent.Warp(restPos);
        }
        IsEmerging = false;
        Destroy(this);
    }
}

// Partículas de la salida de la tierra: polvo marrón, terrones que saltan y piedrecillas
public class EmergeEffects : MonoBehaviour
{
    public enum BurstKind { Opening, Push, Shoulders, Free }

    private ParticleSystem dust, clods, pebbles, ring;
    private float scale = 1f;
    private float width = 1f;

    static Material softMaterial, solidMaterial;

    static Material Soft => softMaterial != null ? softMaterial : (softMaterial = Make(BuffIcons.Glow.texture));
    static Material Solid => solidMaterial != null ? solidMaterial : (solidMaterial = Make(MudTexture()));

    static Material Make(Texture texture)
    {
        Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
        Material material = new Material(shader);
        material.SetTexture("_MainTex", texture);
        material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
        return material;
    }

    // Bola de barro irregular con sombreado y motas, para que los terrones no parezcan cuadrados planos
    static Texture2D MudTexture()
    {
        const int N = 64;
        Texture2D texture = new Texture2D(N, N, TextureFormat.RGBA32, false);
        texture.name = "TerronDeBarro";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[N * N];
        Vector2 light = new Vector2(-0.6f, 0.8f);

        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);
                float radius = 0.72f + 0.1f * Mathf.Sin(3f * angle + 1.3f) + 0.07f * Mathf.Sin(5f * angle + 0.4f) + 0.04f * Mathf.Sin(9f * angle);
                float edge = Mathf.Clamp01((radius - dist) / 0.06f);
                float inside = Mathf.Clamp01(dist / radius);

                Vector2 normal = new Vector2(dx, dy).normalized;
                float lit = 0.5f + 0.5f * Vector2.Dot(normal, light) * inside;
                float speckle = Mathf.PerlinNoise(x * 0.45f, y * 0.45f) * 0.35f + Mathf.PerlinNoise(x * 0.12f + 5f, y * 0.12f) * 0.25f;
                float shade = Mathf.Clamp01((0.5f + 0.5f * lit) * (1f - 0.3f * inside * inside) + speckle * 0.3f - 0.1f);
                byte v = (byte)(shade * 255f);
                pixels[y * N + x] = new Color32(v, v, v, (byte)(edge * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    public static EmergeEffects Create(Vector3 position, float scale, float width)
    {
        GameObject root = new GameObject("SalidaDeLaTierra");
        root.transform.position = position;
        EmergeEffects effects = root.AddComponent<EmergeEffects>();
        effects.scale = scale;
        effects.width = width;
        effects.Build();
        Destroy(root, 8f);
        return effects;
    }

    void Build()
    {
        // Polvo marrón que se arrastra por el suelo y sube despacio
        dust = Make("Polvo", Soft, 120);
        ParticleSystem.MainModule dustMain = dust.main;
        dustMain.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.9f);
        dustMain.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.9f);
        dustMain.startSize = new ParticleSystem.MinMaxCurve(0.7f * scale, 1.5f * scale);
        dustMain.startColor = new ParticleSystem.MinMaxGradient(new Color(0.36f, 0.27f, 0.19f, 0.5f), new Color(0.5f, 0.4f, 0.3f, 0.4f));
        dustMain.gravityModifier = -0.04f;
        dustMain.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
        Disc(dust, width * 0.45f);
        Fade(dust, 0.15f);
        Grow(dust, 0.5f, 1.6f);
        ParticleSystem.EmissionModule dustEmission = dust.emission;
        dustEmission.rateOverTime = 0f;

        // Terrones oscuros que saltan y caen
        clods = Make("Terrones", Solid, 150);
        ParticleSystem.MainModule clodMain = clods.main;
        clodMain.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        clodMain.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6f);
        clodMain.startSize = new ParticleSystem.MinMaxCurve(0.09f * scale, 0.26f * scale);
        clodMain.startColor = new ParticleSystem.MinMaxGradient(new Color(0.5f, 0.36f, 0.22f, 1f), new Color(0.72f, 0.55f, 0.36f, 1f));
        clodMain.gravityModifier = 2.2f;
        clodMain.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
        ParticleSystem.RotationOverLifetimeModule spin = clods.rotationOverLifetime;
        spin.enabled = true;
        spin.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
        Disc(clods, width * 0.4f);
        ParticleSystem.ShapeModule clodShape = clods.shape;
        clodShape.shapeType = ParticleSystemShapeType.Cone;
        clodShape.angle = 38f;
        clodShape.radius = width * 0.35f;
        clodShape.rotation = new Vector3(-90f, 0f, 0f);
        Fade(clods, 0.75f);

        // Piedrecillas grises, menos y más rápidas
        pebbles = Make("Piedras", Solid, 60);
        ParticleSystem.MainModule pebbleMain = pebbles.main;
        pebbleMain.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.2f);
        pebbleMain.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6.5f);
        pebbleMain.startSize = new ParticleSystem.MinMaxCurve(0.04f * scale, 0.1f * scale);
        pebbleMain.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.52f, 0.5f, 1f), new Color(0.75f, 0.7f, 0.64f, 1f));
        pebbleMain.gravityModifier = 2.6f;
        ParticleSystem.ShapeModule pebbleShape = pebbles.shape;
        pebbleShape.shapeType = ParticleSystemShapeType.Cone;
        pebbleShape.angle = 30f;
        pebbleShape.radius = width * 0.3f;
        pebbleShape.rotation = new Vector3(-90f, 0f, 0f);
        Fade(pebbles, 0.8f);

        // Onda de polvo que se expande por el suelo al abrirse y al liberarse
        ring = Make("Onda", Soft, 40);
        ParticleSystem.MainModule ringMain = ring.main;
        ringMain.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.0f);
        ringMain.startSpeed = new ParticleSystem.MinMaxCurve(1.6f * scale, 2.6f * scale);
        ringMain.startSize = new ParticleSystem.MinMaxCurve(0.6f * scale, 1.0f * scale);
        ringMain.startColor = new Color(0.45f, 0.35f, 0.25f, 0.4f);
        ParticleSystem.ShapeModule ringShape = ring.shape;
        ringShape.shapeType = ParticleSystemShapeType.Circle;
        ringShape.radius = width * 0.3f;
        ringShape.radiusThickness = 0f;
        ringShape.arc = 360f;
        ringShape.rotation = new Vector3(-90f, 0f, 0f);
        Fade(ring, 0.1f);
        Grow(ring, 0.6f, 1.5f);
        ParticleSystem.VelocityOverLifetimeModule drag = ring.velocityOverLifetime;
        drag.enabled = false;
    }

    ParticleSystem Make(string name, Material material, int max)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.duration = 5f;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = max;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        particles.Play();
        return particles;
    }

    static void Disc(ParticleSystem particles, float radius)
    {
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 1f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
    }

    // Aparece rápido y se desvanece a partir de fadeStart (0-1) de su vida
    static void Fade(ParticleSystem particles, float fadeStart)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(1f, fadeStart), new GradientAlphaKey(0f, 1f) });
        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = gradient;
    }

    static void Grow(ParticleSystem particles, float from, float to)
    {
        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, from, 1f, to));
    }

    public void SetDust(bool on)
    {
        ParticleSystem.EmissionModule emission = dust.emission;
        emission.rateOverTime = on ? 16f * Mathf.Sqrt(scale) : 0f;
    }

    public void Burst(BurstKind kind)
    {
        switch (kind)
        {
            case BurstKind.Opening:
                dust.Emit(14);
                clods.Emit(16);
                ring.Emit(10);
                break;
            case BurstKind.Push:
                clods.Emit(Random.Range(7, 12));
                pebbles.Emit(Random.Range(2, 5));
                dust.Emit(4);
                break;
            case BurstKind.Shoulders:
                clods.Emit(28);
                pebbles.Emit(9);
                dust.Emit(12);
                ring.Emit(8);
                break;
            case BurstKind.Free:
                clods.Emit(34);
                pebbles.Emit(10);
                dust.Emit(16);
                ring.Emit(14);
                break;
        }
    }

    public void Finish()
    {
        SetDust(false);
    }
}
