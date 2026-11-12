using System.Collections.Generic;
using UnityEngine;

// Límites de la sala del jefe: las paredes planas se visten con pilastras de piedra a intervalos regulares,
// una cornisa y un zócalo que las unen, y una bruma baja que se arrastra pegada al muro.
// Todo sigue la forma real de la sala: se mide con rayos desde el interior hasta el muro.
public class ArenaWalls : MonoBehaviour
{
    const float Spacing = 6f;          // distancia entre pilastras
    const float FogHeight = 0.5f;

    private Material stone, stoneDark;
    private readonly List<GameObject> built = new List<GameObject>();

    public static ArenaWalls Create(Transform arena)
    {
        Transform room = arena.Find("Sala");
        if (room == null) return null;

        GameObject go = new GameObject("DecoracionMuros");
        go.transform.SetParent(arena, false);
        ArenaWalls walls = go.AddComponent<ArenaWalls>();
        walls.Build(room);
        return walls;
    }

    void Build(Transform room)
    {
        // Medidas de la sala: unión de todos sus renderers (la propia malla de la sala y sus hijos)
        Bounds fb = new Bounds(room.position, Vector3.zero);
        Renderer firstRenderer = null;
        foreach (Renderer r in room.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            if (firstRenderer == null) { firstRenderer = r; fb = r.bounds; }
            else fb.Encapsulate(r.bounds);
        }
        if (firstRenderer == null) return;

        // Piedra lisa y oscura (el material de las paredes es un atlas de colores que se vería estirado en estos bloques)
        Shader lit = RuntimeShaders.Find("Universal Render Pipeline/Lit");
        stone = new Material(lit);
        RuntimeShaders.SetColor(stone, new Color(0.30f, 0.22f, 0.19f));
        stoneDark = new Material(lit);
        RuntimeShaders.SetColor(stoneDark, new Color(0.19f, 0.14f, 0.13f));
        foreach (Material m in new[] { stone, stoneDark })
        {
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.12f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        }

        Vector3 center = fb.center;

        // Altura del suelo: se mide un poco a un lado del centro (donde está el jefe)
        float floorY = fb.min.y;
        if (Physics.Raycast(center + new Vector3(7f, 6f, 7f), Vector3.down, out RaycastHit ground, 40f, ~0, QueryTriggerInteraction.Ignore)) floorY = ground.point.y;

        // Cuatro lados de la sala: dirección hacia el muro y eje a lo largo de él
        Vector3[] outward = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        for (int side = 0; side < 4; side++)
        {
            Vector3 dir = outward[side];
            Vector3 along = Vector3.Cross(Vector3.up, dir);
            float halfAcross = Mathf.Abs(Vector3.Dot(fb.extents, dir));          // distancia del centro al borde de la sala
            float halfAlong = Mathf.Abs(Vector3.Dot(fb.extents, along)) * 0.8f;  // se deja libre el margen de las esquinas

            int count = Mathf.Max(2, Mathf.RoundToInt(halfAlong * 2f / Spacing));
            float step = halfAlong * 2f / count;

            List<RaycastHit> hits = new List<RaycastHit>();
            List<bool> valid = new List<bool>();
            for (int i = 0; i <= count; i++)
            {
                float offset = -halfAlong + step * i;
                // Se lanza un rayo desde el interior hacia el muro y se toma el primer obstáculo que no sea un pilar ni el jefe
                Vector3 origin = center + along * offset;
                origin.y = floorY + 2.5f;

                RaycastHit hit = default;
                bool ok = false;
                RaycastHit[] all = Physics.RaycastAll(origin, dir, halfAcross + 10f, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(all, (x, y) => x.distance.CompareTo(y.distance));
                foreach (RaycastHit candidate in all)
                {
                    Transform t = candidate.collider.transform;
                    if (t.GetComponentInParent<Boss>() != null || t.GetComponentInParent<PlayerMovement>() != null) continue;
                    if (IsUnder(t, "Pilares") || candidate.distance < 2f) continue;
                    hit = candidate;
                    ok = Vector3.Dot(candidate.normal, -dir) > 0.85f;
                    break;
                }
                hits.Add(hit);
                valid.Add(ok);
            }

            // Pilastras en cada punto medido (se saltan las esquinas, donde los muros se juntan)
            for (int i = 1; i < count; i++)
            {
                if (valid[i]) BuildPilaster(hits[i].point, hits[i].normal, floorY);
            }

            // Zócalo y cornisa entre pilastras consecutivas
            for (int i = 0; i < count; i++)
            {
                if (valid[i] && valid[i + 1]) BuildBands(hits[i].point, hits[i + 1].point, hits[i].normal, floorY);
            }

            // Distancia real al muro (mediana de los rayos válidos) para colocar la bruma pegada a él
            float wallDistance = halfAcross;
            List<float> distances = new List<float>();
            for (int i = 0; i <= count; i++) if (valid[i]) distances.Add(Vector3.Dot(hits[i].point - center, dir));
            if (distances.Count > 0) { distances.Sort(); wallDistance = distances[distances.Count / 2]; }

            BuildFog(center, dir, wallDistance, halfAlong, floorY);
        }
    }

    // Pilastra: base ancha, fuste alto y capitel, hasta el techo que haya encima
    static bool IsUnder(Transform t, string ancestorName)
    {
        for (; t != null; t = t.parent) if (t.name == ancestorName) return true;
        return false;
    }

    void BuildPilaster(Vector3 wallPoint, Vector3 normal, float floorY)
    {
        float ceiling = floorY + 18f;
        Vector3 probe = wallPoint + normal * 0.5f;
        probe.y = floorY + 1f;
        foreach (RaycastHit hit in Physics.RaycastAll(probe, Vector3.up, 60f, ~0, QueryTriggerInteraction.Ignore))
        {
            ceiling = Mathf.Min(ceiling, hit.point.y);
            if (ceiling < floorY + 18f) break;
        }
        float height = Mathf.Clamp(ceiling - floorY, 5f, 40f);

        Quaternion rotation = Quaternion.LookRotation(normal, Vector3.up);
        Vector3 basePoint = new Vector3(wallPoint.x, floorY, wallPoint.z);

        GameObject root = new GameObject("Pilastra");
        root.transform.SetParent(transform, false);
        root.transform.SetPositionAndRotation(basePoint, rotation);
        built.Add(root);

        AddBlock(root.transform, "Base", new Vector3(1.7f, 1f, 0.7f), new Vector3(0f, 0.5f, 0.35f), stoneDark, true);
        AddBlock(root.transform, "Fuste", new Vector3(1f, height - 2.2f, 0.45f), new Vector3(0f, 1f + (height - 2.2f) * 0.5f, 0.225f), stone, true);
        AddBlock(root.transform, "Capitel", new Vector3(1.7f, 0.8f, 0.7f), new Vector3(0f, height - 0.9f, 0.35f), stoneDark, false);
        AddBlock(root.transform, "Remate", new Vector3(1.35f, 0.35f, 0.6f), new Vector3(0f, height - 1.45f, 0.3f), stone, false);
    }

    // Zócalo bajo y cornisa alta entre dos pilastras
    void BuildBands(Vector3 a, Vector3 b, Vector3 normal, float floorY)
    {
        Vector3 mid = (a + b) * 0.5f;
        mid.y = floorY;
        float length = Vector3.Distance(new Vector3(a.x, 0f, a.z), new Vector3(b.x, 0f, b.z)) - 1.4f;
        if (length < 0.5f) return;

        Quaternion rotation = Quaternion.LookRotation(normal, Vector3.up);
        GameObject root = new GameObject("Molduras");
        root.transform.SetParent(transform, false);
        root.transform.SetPositionAndRotation(mid, rotation);
        built.Add(root);

        AddBlock(root.transform, "Zocalo", new Vector3(length, 0.7f, 0.3f), new Vector3(0f, 0.35f, 0.15f), stoneDark, true);
        AddBlock(root.transform, "Friso", new Vector3(length, 0.18f, 0.22f), new Vector3(0f, 1.05f, 0.11f), stone, false);
    }

    void AddBlock(Transform parent, string name, Vector3 size, Vector3 localPosition, Material material, bool collider)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = localPosition;
        block.transform.localScale = size;

        MeshRenderer renderer = block.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        // Solo la parte baja tiene colisión (las balas y el jugador no atraviesan las pilastras)
        if (!collider) Destroy(block.GetComponent<Collider>());
    }

    // Bruma baja que se arrastra a lo largo del muro
    void BuildFog(Vector3 center, Vector3 dir, float wallDistance, float halfAlong, float floorY)
    {
        Vector3 position = center + dir * (wallDistance - 2.2f);
        position.y = floorY + FogHeight;

        ParticleSystem fog = GlowKit.NewParticles("Bruma", transform, GlowKit.Alpha, Vector3.zero);
        fog.transform.position = position;
        fog.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        float length = halfAlong * 2f;
        ParticleSystem.MainModule main = fog.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 14f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(4.5f, 8f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.48f, 0.5f, 0.10f), new Color(0.4f, 0.4f, 0.5f, 0.13f));
        main.maxParticles = Mathf.RoundToInt(length * 1.1f) + 10;
        main.prewarm = true;

        ParticleSystem.EmissionModule emission = fog.emission;
        emission.rateOverTime = Mathf.Max(3f, length / 11f);

        ParticleSystem.ShapeModule shape = fog.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(length, 0.8f, 3.2f);

        ParticleSystem.VelocityOverLifetimeModule velocity = fog.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);   // los tres ejes deben usar el mismo modo de curva
        velocity.z = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);

        ParticleSystem.RotationOverLifetimeModule rotation = fog.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);

        ParticleSystem.SizeOverLifetimeModule size = fog.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.8f, 1f, 1.35f));

        GlowKit.FadeOut(fog);
        fog.Play();
    }
}
