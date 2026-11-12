using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

// Dibujo de depuración con mallas de líneas: hitboxes, NavMesh y destinos de los enemigos, y los puntos de impacto de los misiles.
// Usa un MeshRenderer normal (no GL), así se ve igual en cualquier pipeline y en las capturas.
public class DebugGizmos : MonoBehaviour
{
    const float Radius = 32f;           // distancia máxima al jugador de lo que se dibuja
    const int MaxColliders = 220;

    private Mesh dynamicMesh, navMesh;
    private MeshRenderer dynamicRenderer, navRenderer;
    private Material material;
    private readonly List<Collider> nearby = new List<Collider>();
    private float nextScan;
    private bool navBuilt;
    private int navSceneHandle = -1;

    // Listas reutilizadas cada fotograma
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Color> colors = new List<Color>();
    private readonly List<int> indices = new List<int>();

    void Awake()
    {
        material = new Material(RuntimeShaders.Find("Sprites/Default"));

        dynamicMesh = new Mesh { name = "DebugLineas" };
        dynamicMesh.MarkDynamic();
        dynamicMesh.indexFormat = IndexFormat.UInt32;
        dynamicRenderer = MakeRenderer("DebugLineas", dynamicMesh);

        navMesh = new Mesh { name = "DebugNavMesh" };
        navMesh.indexFormat = IndexFormat.UInt32;
        navRenderer = MakeRenderer("DebugNavMesh", navMesh);
    }

    MeshRenderer MakeRenderer(string name, Mesh mesh)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enabled = false;
        return renderer;
    }

    void LateUpdate()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        Vector3 center = player != null ? player.transform.position : Vector3.zero;

        // --- NavMesh (estático) ---
        navRenderer.enabled = DebugRuntime.ShowNavMesh;
        if (DebugRuntime.ShowNavMesh)
        {
            int handle = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
            if (!navBuilt || navSceneHandle != handle) BuildNavMesh(handle);
        }

        // --- Líneas dinámicas ---
        bool any = DebugRuntime.ShowHitboxes || DebugRuntime.ShowMissiles || DebugRuntime.ShowNavMesh;
        dynamicRenderer.enabled = any;
        if (!any) return;

        vertices.Clear();
        colors.Clear();
        indices.Clear();

        if (DebugRuntime.ShowHitboxes) DrawHitboxes(center);
        if (DebugRuntime.ShowNavMesh) DrawDestinations();
        if (DebugRuntime.ShowMissiles) DrawMissiles();

        dynamicMesh.Clear();
        if (vertices.Count == 0) return;
        dynamicMesh.SetVertices(vertices);
        dynamicMesh.SetColors(colors);
        dynamicMesh.SetIndices(indices, MeshTopology.Lines, 0);
        dynamicMesh.bounds = new Bounds(center, Vector3.one * 2000f);
    }

    // ---------- Primitivas ----------

    void Line(Vector3 a, Vector3 b, Color color)
    {
        int i = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        colors.Add(color);
        colors.Add(color);
        indices.Add(i);
        indices.Add(i + 1);
    }

    void Circle(Vector3 center, Vector3 axisA, Vector3 axisB, float radius, Color color, int segments = 24)
    {
        Vector3 previous = center + axisA * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = center + (axisA * Mathf.Cos(angle) + axisB * Mathf.Sin(angle)) * radius;
            Line(previous, next, color);
            previous = next;
        }
    }

    void WireBox(Matrix4x4 matrix, Vector3 center, Vector3 size, Color color)
    {
        Vector3 h = size * 0.5f;
        Vector3[] c = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = center + new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
            c[i] = matrix.MultiplyPoint3x4(corner);
        }
        int[,] edges = { { 0, 1 }, { 2, 3 }, { 4, 5 }, { 6, 7 }, { 0, 2 }, { 1, 3 }, { 4, 6 }, { 5, 7 }, { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };
        for (int i = 0; i < 12; i++) Line(c[edges[i, 0]], c[edges[i, 1]], color);
    }

    void WireCapsule(Vector3 center, float radius, float height, Color color)
    {
        float half = Mathf.Max(0f, height * 0.5f - radius);
        Vector3 top = center + Vector3.up * half, bottom = center - Vector3.up * half;
        Circle(top, Vector3.right, Vector3.forward, radius, color);
        Circle(bottom, Vector3.right, Vector3.forward, radius, color);
        Circle(top, Vector3.right, Vector3.up, radius, color, 12);
        Circle(bottom, Vector3.right, Vector3.up, radius, color, 12);
        foreach (Vector3 side in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
            Line(top + side * radius, bottom + side * radius, color);
    }

    // ---------- Hitboxes ----------

    static Color ColorOf(Collider col)
    {
        if (col.GetComponentInParent<PlayerMovement>() != null) return new Color(0.3f, 1f, 0.4f, 1f);
        if (col.GetComponentInParent<Boss>() != null) return new Color(1f, 0.3f, 0.9f, 1f);
        if (col.GetComponentInParent<EnemyBase>() != null) return new Color(1f, 0.25f, 0.2f, 1f);
        if (col.GetComponentInParent<BulletDamage>() != null || col.GetComponentInParent<BalaJefe>() != null) return new Color(1f, 0.95f, 0.2f, 1f);
        if (col.isTrigger) return new Color(1f, 0.6f, 0.15f, 1f);
        return new Color(0.3f, 0.85f, 1f, 0.8f);
    }

    void DrawHitboxes(Vector3 center)
    {
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 0.5f;
            nearby.Clear();
            foreach (Collider col in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
                if (col.transform.IsChildOf(transform)) continue;
                if ((col.bounds.center - center).sqrMagnitude > Radius * Radius) continue;
                nearby.Add(col);
                if (nearby.Count >= MaxColliders) break;
            }
        }

        foreach (Collider col in nearby)
        {
            if (col == null || !col.enabled) continue;
            Color color = ColorOf(col);

            switch (col)
            {
                case BoxCollider box:
                    WireBox(box.transform.localToWorldMatrix, box.center, box.size, color);
                    break;
                case SphereCollider sphere:
                {
                    Vector3 c = sphere.transform.TransformPoint(sphere.center);
                    float r = sphere.radius * Mathf.Max(sphere.transform.lossyScale.x, sphere.transform.lossyScale.y, sphere.transform.lossyScale.z);
                    Circle(c, Vector3.right, Vector3.up, r, color);
                    Circle(c, Vector3.right, Vector3.forward, r, color);
                    Circle(c, Vector3.up, Vector3.forward, r, color);
                    break;
                }
                case CapsuleCollider capsule:
                {
                    Vector3 s = capsule.transform.lossyScale;
                    float r = capsule.radius * Mathf.Max(s.x, s.z);
                    float h = capsule.height * s.y;
                    WireCapsule(capsule.transform.TransformPoint(capsule.center), r, h, color);
                    break;
                }
                case CharacterController controller:
                    WireCapsule(controller.bounds.center, controller.radius, controller.height, color);
                    break;
                default:
                {
                    // Mallas y otros: se dibuja su caja envolvente
                    Bounds b = col.bounds;
                    WireBox(Matrix4x4.identity, b.center, b.size, new Color(color.r, color.g, color.b, 0.35f));
                    break;
                }
            }
        }
    }

    // ---------- NavMesh ----------

    void BuildNavMesh(int sceneHandle)
    {
        navBuilt = true;
        navSceneHandle = sceneHandle;

        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        List<Vector3> v = new List<Vector3>(tri.vertices.Length);
        foreach (Vector3 p in tri.vertices) v.Add(p + Vector3.up * 0.12f);

        List<int> idx = new List<int>(tri.indices.Length * 2);
        for (int i = 0; i + 2 < tri.indices.Length; i += 3)
        {
            int a = tri.indices[i], b = tri.indices[i + 1], c = tri.indices[i + 2];
            idx.Add(a); idx.Add(b);
            idx.Add(b); idx.Add(c);
            idx.Add(c); idx.Add(a);
        }

        Color[] cols = new Color[v.Count];
        for (int i = 0; i < cols.Length; i++) cols[i] = new Color(0.2f, 0.9f, 0.5f, 0.55f);

        navMesh.Clear();
        navMesh.SetVertices(v);
        navMesh.SetColors(cols);
        navMesh.SetIndices(idx, MeshTopology.Lines, 0);
        navMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
    }

    // Camino y destino de cada enemigo
    void DrawDestinations()
    {
        foreach (EnemyBase enemy in EnemyBase.Active)
        {
            if (enemy == null || enemy.health <= 0) continue;
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled || !agent.isOnNavMesh || !agent.hasPath) continue;

            Vector3[] corners = agent.path.corners;
            Color color = new Color(0.35f, 0.6f, 1f, 1f);
            Vector3 previous = enemy.transform.position + Vector3.up * 0.3f;
            foreach (Vector3 corner in corners)
            {
                Line(previous, corner + Vector3.up * 0.3f, color);
                previous = corner + Vector3.up * 0.3f;
            }
            Circle(agent.destination + Vector3.up * 0.3f, Vector3.right, Vector3.forward, 0.5f, color, 12);
        }
    }

    // ---------- Misiles ----------

    void DrawMissiles()
    {
        foreach (BossMissile missile in BossMissile.Active)
        {
            if (missile == null || !missile.IsLocked) continue;

            Color color = new Color(1f, 0.35f, 0.15f, 1f);
            Vector3 ground = missile.MarkerPoint + Vector3.up * 0.15f;
            Circle(ground, Vector3.right, Vector3.forward, BossMissile.BlastRadiusValue, color);
            Line(missile.transform.position, missile.AimPoint, new Color(1f, 0.85f, 0.2f, 1f));
            Vector3 a = missile.AimPoint;
            Line(a + Vector3.left * 0.4f, a + Vector3.right * 0.4f, color);
            Line(a + Vector3.up * 0.4f, a + Vector3.down * 0.4f, color);
            Line(a + Vector3.forward * 0.4f, a + Vector3.back * 0.4f, color);
        }
    }
}
