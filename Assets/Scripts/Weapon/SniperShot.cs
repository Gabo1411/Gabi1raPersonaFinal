using System.Collections.Generic;
using UnityEngine;

// Disparo del modo francotirador: el propio cristal sale despedido como un proyectil eléctrico amarillo, muy rápido,
// que atraviesa a los enemigos y se detiene al chocar con una pared o con el jefe. Todo se construye por código.
public class SniperShot : MonoBehaviour
{
    static readonly Color Electric = new Color(1f, 0.9f, 0.12f, 1f);
    static Mesh crystalMesh;
    static Material solidMaterial, glowMaterial;

    private Vector3 direction;
    private float speed, remaining, radius = 0.3f;
    private int damage;
    private bool finished;
    private Transform crystal;
    private Light glow;
    private TrailRenderer trail;
    private readonly HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

    public static void Fire(Vector3 origin, Vector3 direction, int damage, float speed, float range)
    {
        GameObject go = new GameObject("CristalFrancotirador");
        go.transform.position = origin;
        go.transform.forward = direction;

        SniperShot shot = go.AddComponent<SniperShot>();
        shot.direction = direction.normalized;
        shot.damage = damage;
        shot.speed = speed;
        shot.remaining = range;
        shot.Build();
    }

    void Build()
    {
        GameObject visual = new GameObject("Cristal");
        visual.transform.SetParent(transform, false);
        visual.AddComponent<MeshFilter>().sharedMesh = CrystalMesh;
        MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = Solid;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        visual.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        crystal = visual.transform;

        trail = gameObject.AddComponent<TrailRenderer>();
        trail.sharedMaterial = Glow;
        trail.time = 0.28f;
        trail.startWidth = 0.2f;
        trail.endWidth = 0f;
        trail.startColor = new Color(1f, 0.95f, 0.3f, 0.9f);
        trail.endColor = new Color(1f, 0.7f, 0.05f, 0f);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.minVertexDistance = 0.05f;

        glow = gameObject.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = Electric;
        glow.intensity = 4f;
        glow.range = 7f;
    }

    void Update()
    {
        if (finished) return;

        float dt = Time.deltaTime;
        crystal.Rotate(0f, 0f, 900f * dt, Space.Self);
        float pulse = 1f + 0.25f * Mathf.Sin(Time.time * 50f);
        crystal.localScale = Vector3.one * 0.22f * pulse;

        float step = speed * dt;
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius, direction, step, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Collider other = hit.collider;
            if (other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null) continue;
            if (other.GetComponentInParent<BalaJefe>() != null || other.GetComponentInParent<BulletDamage>() != null) continue;

            bool damageable = other.CompareTag("Enemy") || other.CompareTag("Boss");
            IDamageable target = damageable ? other.GetComponentInParent<IDamageable>() : null;

            if (target != null)
            {
                if (alreadyHit.Add(target))
                {
                    target.TakeDamage(damage);
                    Sparks(hit.point == Vector3.zero ? transform.position : hit.point, 28);
                }
                // Atraviesa a los enemigos, pero el jefe lo detiene
                if (other.CompareTag("Boss")) { Finish(transform.position + direction * hit.distance); return; }
                continue;
            }

            // Los volúmenes de activación (pociones, portal...) no lo detienen
            if (other.isTrigger) continue;

            Finish(hit.point);
            return;
        }

        transform.position += direction * step;
        remaining -= step;
        if (remaining <= 0f) Finish(transform.position);
    }

    void Finish(Vector3 point)
    {
        finished = true;
        transform.position = point;
        Sparks(point, 40);
        crystal.gameObject.SetActive(false);
        glow.enabled = false;
        Destroy(gameObject, trail != null ? trail.time + 0.1f : 0.5f);
    }

    // Ráfaga de chispas eléctricas en el punto de impacto
    static void Sparks(Vector3 position, int count)
    {
        GameObject go = new GameObject("ChispasFrancotirador");
        go.transform.position = position;

        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 11f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.4f), new Color(1f, 0.75f, 0.1f));
        main.gravityModifier = 0.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = Glow;

        particles.Play();
        Destroy(go, 1.2f);
    }

    // ---------- Recursos generados por código ----------

    static Material Solid
    {
        get
        {
            if (solidMaterial != null) return solidMaterial;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            solidMaterial = new Material(shader);
            solidMaterial.color = new Color(1f, 0.95f, 0.45f);
            if (solidMaterial.HasProperty("_BaseColor")) solidMaterial.SetColor("_BaseColor", new Color(1f, 0.95f, 0.45f));
            return solidMaterial;
        }
    }

    static Material Glow
    {
        get
        {
            if (glowMaterial != null) return glowMaterial;
            glowMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            glowMaterial.SetColor("_TintColor", new Color(1f, 0.85f, 0.2f, 0.6f));
            return glowMaterial;
        }
    }

    // Cristal alargado de seis puntas (dos pirámides unidas por la base), apuntando hacia delante
    static Mesh CrystalMesh
    {
        get
        {
            if (crystalMesh != null) return crystalMesh;

            Vector3[] ring = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                ring[i] = new Vector3(Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.5f, 0f);
            }
            Vector3 front = new Vector3(0f, 0f, 1.5f), back = new Vector3(0f, 0f, -1.1f);

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                int i = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = ring[i], b = ring[(i + 1) % 4];
                Tri(a, b, front);
                Tri(b, a, back);
            }

            crystalMesh = new Mesh { name = "CristalFrancotirador" };
            crystalMesh.hideFlags = HideFlags.HideAndDontSave;
            crystalMesh.SetVertices(vertices);
            crystalMesh.SetTriangles(triangles, 0);
            crystalMesh.RecalculateNormals();
            crystalMesh.RecalculateBounds();
            return crystalMesh;
        }
    }
}
