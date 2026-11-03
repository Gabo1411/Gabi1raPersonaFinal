using System.Collections.Generic;
using UnityEngine;

// Al morir el jefe revienta por dentro y lanza un botín de cofre de fantasía: monedas, lingotes de oro, gemas,
// pociones, copas, anillos y pergaminos. El jefe se queda en el suelo; el botín sale desde su interior.
public static class BossExplosion
{
    // El jefe es enorme: las piezas del botín se agrandan para que se vean desde lejos
    const float LootScale = 2.4f;

    static Material goldMaterial, barMaterial, silverMaterial, parchmentMaterial, glassMaterial;
    static readonly Dictionary<Color, Material> gemMaterials = new Dictionary<Color, Material>();
    static PhysicsMaterial lootPhysics;

    static readonly Color[] GemColors =
    {
        new Color(0.9f, 0.1f, 0.15f), new Color(0.1f, 0.8f, 0.35f), new Color(0.15f, 0.4f, 0.95f), new Color(0.65f, 0.25f, 0.9f)
    };

    static readonly Color[] PotionColors =
    {
        new Color(0.9f, 0.12f, 0.12f), new Color(0.2f, 0.85f, 0.3f), new Color(0.2f, 0.45f, 0.95f)
    };

    public static void Play(Vector3 center, Vector3 size, Collider[] ignore)
    {
        List<Collider> toIgnore = new List<Collider>(ignore);
        PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
        if (player != null) toIgnore.AddRange(player.GetComponentsInChildren<Collider>());

        SpawnLoot(center, size, toIgnore);
        SpawnFlash(center);

        Sfx.AtPoint(Resources.Load<AudioClip>("Audio/Jefe_Explosion"), center);
    }

    // ---------- Botín ----------

    static void SpawnLoot(Vector3 center, Vector3 size, List<Collider> ignore)
    {
        Mesh cylinder = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
        Mesh cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

        for (int i = 0; i < 80; i++) Launch(MakeCoin(cylinder), 0.05f, 1.15f, 5f, 14f, center, size, ignore);
        for (int i = 0; i < 9; i++) Launch(MakeBar(cube), 0.5f, 0.8f, 4f, 9f, center, size, ignore);
        for (int i = 0; i < 16; i++) Launch(MakeGem(i), 0.1f, 1.5f, 5f, 12f, center, size, ignore);
        for (int i = 0; i < 9; i++) Launch(MakePotion(i), 0.15f, 1.3f, 4f, 10f, center, size, ignore);
        for (int i = 0; i < 4; i++) Launch(MakeGoblet(), 0.2f, 1.0f, 4f, 9f, center, size, ignore);
        for (int i = 0; i < 6; i++) Launch(MakeRing(), 0.08f, 1.6f, 5f, 11f, center, size, ignore);
        for (int i = 0; i < 4; i++) Launch(MakeScroll(cylinder), 0.08f, 1.0f, 4f, 9f, center, size, ignore);
    }

    // Coloca el objeto dentro del cuerpo del jefe y lo dispara hacia afuera desde el centro
    static void Launch(GameObject item, float mass, float clinkPitch, float minSpeed, float maxSpeed, Vector3 center, Vector3 size, List<Collider> ignore)
    {
        Vector3 offset = new Vector3(Random.Range(-0.35f, 0.35f) * size.x, Random.Range(-0.3f, 0.35f) * size.y, Random.Range(-0.35f, 0.35f) * size.z);
        item.transform.position = center + offset;
        item.transform.rotation = Random.rotation;

        Rigidbody body = item.AddComponent<Rigidbody>();
        body.mass = mass * LootScale * LootScale;
        body.linearDamping = 0.05f;
        body.angularDamping = 0.1f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Vector3 direction = (offset.normalized + Vector3.up * 0.9f + Random.insideUnitSphere * 0.4f).normalized;
        body.linearVelocity = direction * Random.Range(minSpeed, maxSpeed);
        body.angularVelocity = Random.insideUnitSphere * 22f;

        // El botín no choca con el jefe (sale de su interior) ni con el jugador
        foreach (Collider own in item.GetComponentsInChildren<Collider>())
        {
            own.sharedMaterial = LootPhysics;
            foreach (Collider other in ignore) if (other != null) Physics.IgnoreCollision(own, other);
        }

        LootItem loot = item.AddComponent<LootItem>();
        loot.clinkPitch = clinkPitch;
    }

    static GameObject NewItem(string name, Mesh mesh, Material material, Vector3 scale)
    {
        GameObject go = new GameObject(name);
        go.transform.localScale = scale * LootScale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    static GameObject MakeCoin(Mesh cylinder)
    {
        GameObject coin = NewItem("Moneda", cylinder, GoldMaterial, new Vector3(0.34f, 0.03f, 0.34f) * Random.Range(0.8f, 1.3f));
        BoxCollider box = coin.AddComponent<BoxCollider>();
        box.size = new Vector3(1f, 2f, 1f);
        return coin;
    }

    static GameObject MakeBar(Mesh cube)
    {
        GameObject bar = NewItem("Lingote", cube, BarMaterial, new Vector3(0.36f, 0.13f, 0.19f) * Random.Range(0.9f, 1.2f));
        bar.AddComponent<BoxCollider>();
        return bar;
    }

    static GameObject MakeGem(int index)
    {
        Color color = GemColors[index % GemColors.Length];
        GameObject gem = NewItem("Gema", ProceduralMeshes.Gem, GemMaterial(color), Vector3.one * Random.Range(0.8f, 1.3f));
        SphereCollider sphere = gem.AddComponent<SphereCollider>();
        sphere.radius = 0.1f;
        return gem;
    }

    static GameObject MakePotion(int index)
    {
        Color color = PotionColors[index % PotionColors.Length];
        GameObject bottle = NewItem("Pocion", ProceduralMeshes.Bottle, GemMaterial(color), Vector3.one * Random.Range(0.9f, 1.15f));
        CapsuleCollider capsule = bottle.AddComponent<CapsuleCollider>();
        capsule.radius = 0.12f;
        capsule.height = 0.38f;
        capsule.center = new Vector3(0f, 0.19f, 0f);
        return bottle;
    }

    static GameObject MakeGoblet()
    {
        GameObject cup = NewItem("Copa", ProceduralMeshes.Goblet, GoldMaterial, Vector3.one * Random.Range(0.9f, 1.2f));
        CapsuleCollider capsule = cup.AddComponent<CapsuleCollider>();
        capsule.radius = 0.12f;
        capsule.height = 0.32f;
        capsule.center = new Vector3(0f, 0.16f, 0f);
        return cup;
    }

    static GameObject MakeRing()
    {
        GameObject ring = NewItem("Anillo", ProceduralMeshes.Ring, GoldMaterial, Vector3.one * Random.Range(0.9f, 1.2f));
        BoxCollider box = ring.AddComponent<BoxCollider>();
        box.size = new Vector3(0.22f, 0.05f, 0.22f);

        // Piedra engarzada
        GameObject stone = NewItem("Piedra", ProceduralMeshes.Gem, GemMaterial(GemColors[Random.Range(0, GemColors.Length)]), Vector3.one * 0.35f);
        stone.transform.SetParent(ring.transform, false);
        stone.transform.localPosition = new Vector3(0.095f, 0.02f, 0f);
        return ring;
    }

    static GameObject MakeScroll(Mesh cylinder)
    {
        GameObject scroll = NewItem("Pergamino", cylinder, ParchmentMaterial, new Vector3(0.075f, 0.2f, 0.075f));
        BoxCollider box = scroll.AddComponent<BoxCollider>();
        box.size = new Vector3(1f, 2f, 1f);
        return scroll;
    }

    // ---------- Materiales ----------

    static Material NewLit(Color color, float metallic, float smoothness, float emission)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        if (emission > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        return material;
    }

    static Material GoldMaterial => goldMaterial != null ? goldMaterial : (goldMaterial = NewLit(new Color(1f, 0.74f, 0.17f), 1f, 0.82f, 0.22f));
    static Material BarMaterial => barMaterial != null ? barMaterial : (barMaterial = NewLit(new Color(1f, 0.82f, 0.3f), 1f, 0.9f, 0.18f));
    static Material ParchmentMaterial => parchmentMaterial != null ? parchmentMaterial : (parchmentMaterial = NewLit(new Color(0.9f, 0.82f, 0.6f), 0f, 0.1f, 0f));

    static Material GemMaterial(Color color)
    {
        if (!gemMaterials.TryGetValue(color, out Material material) || material == null)
        {
            material = NewLit(color, 0.1f, 0.95f, 0.7f);
            gemMaterials[color] = material;
        }
        return material;
    }

    static PhysicsMaterial LootPhysics
    {
        get
        {
            if (lootPhysics == null)
            {
                lootPhysics = new PhysicsMaterial("Botin")
                {
                    bounciness = 0.5f,
                    dynamicFriction = 0.5f,
                    staticFriction = 0.5f,
                    bounceCombine = PhysicsMaterialCombine.Maximum
                };
            }
            return lootPhysics;
        }
    }

    // ---------- Destello ----------

    static void SpawnFlash(Vector3 center)
    {
        GameObject root = new GameObject("ExplosionDorada");
        root.transform.position = center;
        Object.Destroy(root, 3f);

        Material glow = ParticleMaterials.Glow;

        ParticleSystem flash = NewBurst("Destello", root.transform, glow, 1);
        ParticleSystem.MainModule flashMain = flash.main;
        flashMain.startLifetime = 0.45f;
        flashMain.startSpeed = 0f;
        flashMain.startSize = 12f;
        flashMain.startColor = new Color(1f, 0.85f, 0.4f, 0.9f);
        ParticleSystem.SizeOverLifetimeModule flashSize = flash.sizeOverLifetime;
        flashSize.enabled = true;
        flashSize.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(1f, 1.2f)));

        ParticleSystem sparks = NewBurst("Chispas", root.transform, glow, 90);
        ParticleSystem.MainModule sparkMain = sparks.main;
        sparkMain.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        sparkMain.startSpeed = new ParticleSystem.MinMaxCurve(4f, 14f);
        sparkMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        sparkMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.78f, 0.2f), new Color(1f, 0.95f, 0.7f));
        sparkMain.gravityModifier = 0.5f;
        ParticleSystem.ShapeModule sparkShape = sparks.shape;
        sparkShape.enabled = true;
        sparkShape.shapeType = ParticleSystemShapeType.Sphere;
        sparkShape.radius = 1.2f;
        ParticleSystem.SizeOverLifetimeModule sparkSize = sparks.sizeOverLifetime;
        sparkSize.enabled = true;
        sparkSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        GameObject lightObject = new GameObject("Luz");
        lightObject.transform.SetParent(root.transform, false);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.8f, 0.4f);
        light.range = 18f;
        light.intensity = 10f;
        light.shadows = LightShadows.None;
        lightObject.AddComponent<FadeLight>().duration = 0.9f;
    }

    static ParticleSystem NewBurst(string name, Transform parent, Material material, int count)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = true;
        main.maxParticles = Mathf.Max(count, 2);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = false;

        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        particles.Play();
        return particles;
    }
}

// Pieza del botín: rebota con tintineo y se encoge hasta desaparecer mucho después, ya con el botín quieto
public class LootItem : MonoBehaviour
{
    const float ShrinkTime = 1f;

    static float nextClink;
    static AudioClip clink;

    public float clinkPitch = 1f;

    private float life;
    private float age;
    private Vector3 baseScale;

    void Start()
    {
        life = Random.Range(50f, 75f);
        baseScale = transform.localScale;
        if (clink == null) clink = Resources.Load<AudioClip>("Audio/Moneda_Tintineo");
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age < life) return;

        float t = (age - life) / ShrinkTime;
        transform.localScale = baseScale * Mathf.Clamp01(1f - t);
        if (t >= 1f) Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Se limita la cantidad de tintineos por segundo para que no se amontonen
        if (collision.relativeVelocity.sqrMagnitude < 2f || Time.time < nextClink || clink == null) return;

        nextClink = Time.time + 0.045f;

        GameObject sound = new GameObject("Tintineo");
        sound.transform.position = transform.position;
        AudioSource source = sound.AddComponent<AudioSource>();
        source.clip = clink;
        source.pitch = clinkPitch * Random.Range(0.9f, 1.15f);
        source.volume = Random.Range(0.25f, 0.5f) * Sfx.Volume;
        source.spatialBlend = 1f;
        source.Play();
        Destroy(sound, clink.length / source.pitch + 0.1f);
    }
}

// Apaga una luz de forma gradual
public class FadeLight : MonoBehaviour
{
    public float duration = 1f;

    private Light target;
    private float initialIntensity;
    private float elapsed;

    void Start()
    {
        target = GetComponent<Light>();
        initialIntensity = target.intensity;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        target.intensity = initialIntensity * (1f - t) * (1f - t);
    }
}

// Materiales aditivos para efectos de partículas generados por código
public static class ParticleMaterials
{
    static Material glow, ring;

    public static Material Glow => glow != null ? glow : (glow = Additive(BuffIcons.Glow.texture));
    public static Material Ring => ring != null ? ring : (ring = Additive(BuffIcons.Ring.texture));

    public static Material Additive(Texture texture)
    {
        Material material = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        material.SetTexture("_MainTex", texture);
        material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
        return material;
    }
}

// Mallas sencillas de aspecto low-poly (caras planas) generadas por código para el botín
public static class ProceduralMeshes
{
    static Mesh gem, bottle, goblet, ring;

    public static Mesh Gem => gem != null ? gem : (gem = BuildGem(0.1f, 0.09f, 0.13f, 6));

    public static Mesh Bottle => bottle != null ? bottle : (bottle = Lathe("Botella", new[]
    {
        (0f, 0f), (0.1f, 0f), (0.13f, 0.05f), (0.14f, 0.14f), (0.1f, 0.22f), (0.045f, 0.27f),
        (0.045f, 0.35f), (0.06f, 0.36f), (0.06f, 0.38f), (0f, 0.38f)
    }, 8));

    public static Mesh Goblet => goblet != null ? goblet : (goblet = Lathe("Copa", new[]
    {
        (0f, 0f), (0.12f, 0f), (0.12f, 0.02f), (0.04f, 0.045f), (0.03f, 0.14f), (0.1f, 0.18f),
        (0.14f, 0.3f), (0.13f, 0.31f), (0.0f, 0.3f)
    }, 10));

    public static Mesh Ring => ring != null ? ring : (ring = BuildTorus("Anillo", 0.09f, 0.022f, 12, 5));

    // Superficie de revolución alrededor del eje Y. Perfil: (radio, altura).
    static Mesh Lathe(string name, (float r, float y)[] profile, int sides)
    {
        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i < profile.Length; i++)
        {
            for (int s = 0; s <= sides; s++)
            {
                float angle = 2f * Mathf.PI * s / sides;
                positions.Add(new Vector3(Mathf.Cos(angle) * profile[i].r, profile[i].y, Mathf.Sin(angle) * profile[i].r));
            }
        }

        List<int> triangles = new List<int>();
        int stride = sides + 1;
        for (int i = 0; i < profile.Length - 1; i++)
        {
            for (int s = 0; s < sides; s++)
            {
                int a = i * stride + s, b = a + 1, c = a + stride, d = c + 1;
                triangles.AddRange(new[] { a, b, c, b, d, c });
            }
        }
        return ToFlatMesh(name, positions, triangles);
    }

    // Toro tumbado: el anillo queda en el plano XZ
    static Mesh BuildTorus(string name, float majorRadius, float minorRadius, int majorSegments, int minorSegments)
    {
        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i <= majorSegments; i++)
        {
            float phi = 2f * Mathf.PI * i / majorSegments;
            Vector3 radial = new Vector3(Mathf.Cos(phi), 0f, Mathf.Sin(phi));
            for (int j = 0; j <= minorSegments; j++)
            {
                float theta = 2f * Mathf.PI * j / minorSegments;
                positions.Add(radial * majorRadius + (radial * Mathf.Cos(theta) + Vector3.up * Mathf.Sin(theta)) * minorRadius);
            }
        }

        List<int> triangles = new List<int>();
        int stride = minorSegments + 1;
        for (int i = 0; i < majorSegments; i++)
        {
            for (int j = 0; j < minorSegments; j++)
            {
                int a = i * stride + j, b = a + 1, c = a + stride, d = c + 1;
                triangles.AddRange(new[] { a, b, c, b, d, c });
            }
        }
        return ToFlatMesh(name, positions, triangles);
    }

    // Gema: bipirámide con la punta superior más corta que la inferior
    static Mesh BuildGem(float radius, float topHeight, float bottomHeight, int sides)
    {
        List<Vector3> positions = new List<Vector3> { new Vector3(0f, topHeight, 0f), new Vector3(0f, -bottomHeight, 0f) };
        for (int i = 0; i < sides; i++)
        {
            float angle = 2f * Mathf.PI * i / sides;
            positions.Add(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }

        List<int> triangles = new List<int>();
        for (int i = 0; i < sides; i++)
        {
            int a = 2 + i, b = 2 + (i + 1) % sides;
            triangles.AddRange(new[] { 0, b, a, 1, a, b });
        }
        return ToFlatMesh("Gema", positions, triangles);
    }

    static Mesh ToFlatMesh(string name, List<Vector3> source, List<int> triangles)
    {
        List<Vector3> positions = new List<Vector3>(triangles.Count);
        List<int> indices = new List<int>(triangles.Count);
        for (int i = 0; i < triangles.Count; i++)
        {
            positions.Add(source[triangles[i]]);
            indices.Add(i);
        }

        Mesh mesh = new Mesh { name = name };
        mesh.SetVertices(positions);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
