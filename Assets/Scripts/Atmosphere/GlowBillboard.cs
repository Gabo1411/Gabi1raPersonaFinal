using UnityEngine;

// Halo luminoso: un cuadrado con brillo suave que mira siempre a la cámara (o tumbado en el suelo).
// Sirve para dar la sensación de resplandor (bloom) alrededor de llamas y focos de luz.
public class GlowBillboard : MonoBehaviour
{
    static Mesh quadMesh;

    public Color color = new Color(1f, 0.6f, 0.2f, 1f);
    public float size = 1.5f;
    [Range(0f, 3f)] public float intensity = 1f;
    public bool flatOnGround;

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock block;
    private Transform cam;
    private static readonly int TintColor = Shader.PropertyToID("_TintColor");

    public static GlowBillboard Create(Transform parent, Vector3 worldPosition, float size, Color color, bool flat = false)
    {
        GameObject go = new GameObject(flat ? "HaloSuelo" : "Halo");
        go.transform.SetParent(parent, true);
        go.transform.position = worldPosition;

        go.AddComponent<MeshFilter>().sharedMesh = Quad;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = GlowKit.Additive;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        GlowBillboard glow = go.AddComponent<GlowBillboard>();
        glow.size = size;
        glow.color = color;
        glow.flatOnGround = flat;
        glow.meshRenderer = renderer;
        if (flat) go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        return glow;
    }

    void Awake()
    {
        block = new MaterialPropertyBlock();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
    }

    void LateUpdate()
    {
        if (meshRenderer == null) return;
        if (cam == null && Camera.main != null) cam = Camera.main.transform;

        if (!flatOnGround && cam != null) transform.rotation = cam.rotation;
        // Se compensa la escala del padre (algunas antorchas están escaladas) para que el tamaño sea el pedido en metros
        float parentScale = transform.parent != null ? Mathf.Max(0.0001f, transform.parent.lossyScale.x) : 1f;
        transform.localScale = Vector3.one * (size / parentScale);

        // El material suma 2 × tinte: con 0.5 de base el color queda tal cual
        Color tint = color * (0.5f * intensity);
        tint.a = 0.5f;
        block.SetColor(TintColor, tint);
        meshRenderer.SetPropertyBlock(block);
    }

    static Mesh Quad
    {
        get
        {
            if (quadMesh != null) return quadMesh;
            quadMesh = new Mesh { name = "CuadradoHalo" };
            quadMesh.hideFlags = HideFlags.HideAndDontSave;
            quadMesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) };
            quadMesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            quadMesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            quadMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            quadMesh.RecalculateBounds();
            return quadMesh;
        }
    }
}
