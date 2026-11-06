using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ---------- Enredaderas con espinas ----------

[RequireComponent(typeof(CanvasRenderer))]
[ExecuteAlways]
public class MenuVines : MaskableGraphic
{
    public class Vine
    {
        public Vine parent;
        public int parentIndex;
        public Vector2 origin, target;   // fracciones de la pantalla; la enredadera trepa desde el origen hacia el objetivo
        public float reach, branchFactor;
        public float zigMix, wobbleAmp, wobbleFreq, wobblePhase, leafScale;
        public int leafEvery, gripEvery;
        public float baseX, length, width, theta, lean, amplitude, frequency, phase;
        public float thornLength, pulsePhase;
        public int thornEvery;
        public bool back;
        public Vector2[] points;
        public float[] angles;
        public int visible;
        public float visibleFraction;
    }

    const int Segments = 60;

    public struct GlowPoint
    {
        public Vector2 position;
        public float size, alpha;
    }

    public readonly List<GlowPoint> glowPoints = new List<GlowPoint>();

    private readonly List<Vine> vines = new List<Vine>();

    protected override void OnEnable()
    {
        base.OnEnable();
        if (vines.Count == 0) Build();
    }

    static float Rand(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

    // Enredaderas pegadas al muro: nacen en los costados y en la base y trepan en zigzag hacia el centro,
    // gruesas al principio y cada vez más finas. No se mueven; solo laten las espinas.
    void Build()
    {
        vines.Clear();
        System.Random rng = new System.Random(311);

        // Del fondo: más finas y oscuras
        AddRoot(rng, new Vector2(0.34f, -0.04f), 0.7f, 38f, true, 2);
        AddRoot(rng, new Vector2(0.68f, -0.04f), 0.7f, 38f, true, 2);
        AddRoot(rng, new Vector2(-0.03f, 0.22f), 0.62f, 40f, true, 2);
        AddRoot(rng, new Vector2(1.03f, 0.42f), 0.62f, 40f, true, 2);

        // Principales: desde la base y desde ambos costados, todas buscando el centro del muro
        AddRoot(rng, new Vector2(0.17f, -0.04f), 0.8f, 66f, false, 4);
        AddRoot(rng, new Vector2(0.5f, -0.04f), 0.72f, 62f, false, 4);
        AddRoot(rng, new Vector2(0.83f, -0.04f), 0.8f, 66f, false, 4);
        float[] sideHeights = { 0.06f, 0.3f, 0.56f };
        foreach (float h in sideHeights)
        {
            AddRoot(rng, new Vector2(-0.03f, h), 0.8f, 58f, false, 4);
            AddRoot(rng, new Vector2(1.03f, Mathf.Clamp01(h + 0.04f)), 0.8f, 58f, false, 4);
        }
    }

    void AddRoot(System.Random rng, Vector2 origin, float reach, float width, bool back, int branches)
    {
        Vine vine = NewVine(rng, 0f, 0f, width * Rand(rng, 0.75f, 1.3f), back);
        vine.origin = origin;
        vine.target = new Vector2(0.5f + Rand(rng, -0.07f, 0.07f), 0.58f + Rand(rng, -0.08f, 0.1f));
        vine.reach = reach * Rand(rng, 0.8f, 1.08f);
        vines.Add(vine);
        AddBranches(rng, vine, branches);
    }

    Vine NewVine(System.Random rng, float x, float length, float width, bool back)
    {
        return new Vine
        {
            baseX = x,
            length = length,
            width = width,
            lean = Rand(rng, -0.1f, 0.1f),
            // Cada enredadera tiene su carácter: unas en zigzag marcado, otras ondulantes, otras torcidas
            amplitude = Rand(rng, 0.35f, 0.95f),
            frequency = Rand(rng, 1.7f, 4.4f),
            phase = Rand(rng, 0f, 1f),
            zigMix = Rand(rng, 0f, 1f),
            wobbleAmp = Rand(rng, 0f, 0.38f),
            wobbleFreq = Rand(rng, 3f, 8f),
            wobblePhase = Rand(rng, 0f, 6.28f),
            leafEvery = rng.Next(5, 13),
            gripEvery = rng.Next(7, 16),
            leafScale = Rand(rng, 0.65f, 1.45f),
            thornLength = Rand(rng, 30f, 66f) * (back ? 0.75f : 1f),
            pulsePhase = Rand(rng, 0f, 6.28f),
            thornEvery = rng.Next(3, 7),
            back = back,
            points = new Vector2[Segments + 1],
            angles = new float[Segments + 1]
        };
    }

    static float Taper(float u) => Mathf.Pow(1f - u, 1.25f);

    void AddBranches(System.Random rng, Vine parent, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float at = 0.1f + (i + Rand(rng, 0.1f, 0.9f)) / count * 0.55f;
            float side = (i % 2 == 0 ? 1f : -1f);
            Vine branch = NewVine(rng, 0f, 0f, parent.width * Taper(at) * 0.55f, parent.back);
            branch.branchFactor = Rand(rng, 0.3f, 0.46f) * (1f - at * 0.6f);
            branch.parent = parent;
            branch.parentIndex = Mathf.RoundToInt(at * Segments);
            branch.theta = side * Rand(rng, 0.75f, 1.05f);
            branch.amplitude = Rand(rng, 0.4f, 0.55f);
            branch.frequency = Rand(rng, 2f, 2.8f);
            branch.thornLength = parent.thornLength * 0.8f;
            vines.Add(branch);
        }
    }

    void Update()
    {
        // Solo las espinas cambian (el pulso), así que se redibuja cada fotograma
        SetVerticesDirty();
    }

    // Onda en zigzag con codos marcados pero algo redondeados
    static float Zig(float x, float mix)
    {
        float s = Mathf.Sin(6.2832f * x);
        float tri = Mathf.Asin(s) * 0.6366f;
        return mix * tri + (1f - mix) * s;
    }

    // La forma es fija: depende solo del tamaño de la pantalla
    void Shape(Vine vine, Rect rect, float scale)
    {
        Vector2 origin;
        float angle0;
        float heading = 0f;
        if (vine.parent == null)
        {
            origin = new Vector2(rect.xMin + vine.origin.x * rect.width, rect.yMin + vine.origin.y * rect.height);
            Vector2 target = new Vector2(rect.xMin + vine.target.x * rect.width, rect.yMin + vine.target.y * rect.height);
            Vector2 toTarget = target - origin;
            heading = Mathf.Atan2(toTarget.x, toTarget.y);
            vine.length = toTarget.magnitude * vine.reach / rect.height;
            angle0 = heading;
        }
        else
        {
            vine.length = vine.parent.length * vine.branchFactor;
            origin = vine.parent.points[vine.parentIndex];
            angle0 = vine.parent.angles[vine.parentIndex];
        }

        float step = vine.length * rect.height / Segments;
        Vector2 position = origin;
        vine.points[0] = position;
        vine.angles[0] = angle0;
        for (int i = 1; i <= Segments; i++)
        {
            float u = (float)i / Segments;
            float angle;
            if (vine.parent == null)
                angle = heading + vine.amplitude * Zig(u * vine.frequency + vine.phase, vine.zigMix) * (1f - 0.3f * u)
                        + vine.wobbleAmp * Mathf.Sin(6.2832f * vine.wobbleFreq * u + vine.wobblePhase);
            else
                angle = angle0 + vine.theta * (1f - 0.55f * u) + vine.amplitude * Zig(u * vine.frequency + vine.phase, vine.zigMix) * 0.8f
                        + vine.wobbleAmp * 0.7f * Mathf.Sin(6.2832f * vine.wobbleFreq * u + vine.wobblePhase);
            vine.points[i] = position += step * new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            vine.angles[i] = angle;
        }

        vine.visibleFraction = 1f;
        vine.visible = Segments;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        glowPoints.Clear();

        Rect rect = rectTransform.rect;
        float scale = rect.height / 1080f;
        float t = Application.isPlaying ? Time.unscaledTime : Time.realtimeSinceStartup;

        // Los padres se calculan antes que sus ramas (están en ese orden en la lista)
        foreach (Vine vine in vines) Shape(vine, rect, scale);

        // Primero las del fondo, luego las del frente
        // La sombra sobre el muro va debajo de cada capa: así las enredaderas se ven pegadas a la pared
        foreach (Vine vine in vines) if (vine.back) Draw(vh, vine, t, scale, true);
        foreach (Vine vine in vines) if (vine.back) Draw(vh, vine, t, scale, false);
        foreach (Vine vine in vines) if (!vine.back) Draw(vh, vine, t, scale, true);
        foreach (Vine vine in vines) if (!vine.back) Draw(vh, vine, t, scale, false);
    }

    void Draw(VertexHelper vh, Vine vine, float t, float scale, bool shadow)
    {
        if (vine.visibleFraction <= 0f || vine.visible < 1) return;

        float shade = vine.back ? 0.6f : 1f;
        int count = Mathf.Min(vine.visible, Segments);
        float fraction = vine.visibleFraction;
        int previous = -1;

        for (int i = 0; i <= count; i++)
        {
            // El último punto se interpola para que la punta crezca con suavidad
            float u = (float)i / Segments;
            Vector2 p = vine.points[i];
            float a = vine.angles[i];
            if (i == count)
            {
                float exact = fraction * Segments;
                float k = Mathf.Clamp01(exact - (count - 1));
                p = Vector2.Lerp(vine.points[Mathf.Max(0, count - 1)], vine.points[i], k);
                u = exact / Segments;
            }

            float w = Mathf.Max(0.7f, vine.width * scale * Taper(u)) * 0.5f;
            if (i == count) w *= 0.35f;

            Vector2 dir = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
            Vector2 normal = new Vector2(dir.y, -dir.x);

            float pulse = Pulse(t, u, vine.pulsePhase);
            Color body = Color.Lerp(new Color(0.14f, 0.25f, 0.16f), new Color(0.27f, 0.17f, 0.36f), Mathf.Clamp01(u * 1.3f + pulse * 0.18f)) * shade;
            body.a = 1f;
            Color edgeColor = body * 0.55f; edgeColor.a = 1f;
            Color centerColor = Color.Lerp(body * 1.45f, new Color(0.35f, 0.18f, 0.5f) * shade, pulse * 0.25f); centerColor.a = 1f;

            if (shadow)
            {
                // Sombra proyectada hacia abajo y a la derecha, algo más ancha
                p += new Vector2(8f, -10f) * scale * (vine.back ? 0.6f : 1f);
                w *= 1.18f;
                edgeColor = centerColor = new Color(0f, 0f, 0f, vine.back ? 0.3f : 0.45f);
            }

            AddVertex(vh, p - normal * w, edgeColor);
            AddVertex(vh, p, centerColor);
            AddVertex(vh, p + normal * w, edgeColor);

            int current = vh.currentVertCount - 3;
            if (previous >= 0)
            {
                int b = previous;
                int c = current;
                vh.AddTriangle(b, b + 1, c);
                vh.AddTriangle(b + 1, c + 1, c);
                vh.AddTriangle(b + 1, b + 2, c + 1);
                vh.AddTriangle(b + 2, c + 2, c + 1);
            }
            previous = current;

            if (shadow) continue;

            if (i > 1 && i < count && i % vine.thornEvery == 0)
                Thorn(vh, vine, i, p, dir, normal, w, u, t, scale, shade, fraction);

            // Hojas pegadas a la pared y zarcillos que se agarran a ella con una ventosa
            if (i > 2 && i < count - 1)
            {
                if (i % vine.leafEvery == 2) Leaf(vh, vine, p, dir, normal, w, u, scale * vine.leafScale, shade, (i / vine.leafEvery) % 2 == 0 ? -1f : 1f);
                if (i % vine.gripEvery == 5 && u < 0.75f) Grip(vh, vine, p, dir, normal, w, scale, shade, (i / vine.gripEvery) % 2 == 0 ? 1f : -1f);
            }
        }
    }

    void Leaf(VertexHelper vh, Vine vine, Vector2 p, Vector2 dir, Vector2 normal, float w, float u, float scale, float shade, float side)
    {
        float length = (54f - 26f * u) * scale * (vine.back ? 0.8f : 1f);
        Vector2 leafDir = (normal * side * 0.75f + dir * 0.65f).normalized;
        Vector2 perp = new Vector2(leafDir.y, -leafDir.x);
        Vector2 root = p + normal * side * w * 0.5f;
        Vector2 mid = root + leafDir * length * 0.42f;

        Color dark = new Color(0.06f, 0.13f, 0.09f, 1f) * shade; dark.a = 1f;
        Color side1 = new Color(0.1f, 0.22f, 0.14f, 1f) * shade; side1.a = 1f;
        Color tip = new Color(0.2f, 0.3f, 0.24f, 1f) * shade; tip.a = 1f;
        int b = vh.currentVertCount;
        AddVertex(vh, root, dark);
        AddVertex(vh, mid + perp * length * 0.2f, side1);
        AddVertex(vh, root + leafDir * length, tip);
        AddVertex(vh, mid - perp * length * 0.2f, side1);
        vh.AddTriangle(b, b + 1, b + 2);
        vh.AddTriangle(b, b + 2, b + 3);
    }

    // Zarcillo corto que se curva sobre el muro y termina en un disco pegado a la piedra
    void Grip(VertexHelper vh, Vine vine, Vector2 p, Vector2 dir, Vector2 normal, float w, float scale, float shade, float side)
    {
        Vector2 position = p + normal * side * w * 0.7f;
        Vector2 heading = (normal * side - dir * 0.35f).normalized;
        float step = 15f * scale;
        Color col = new Color(0.12f, 0.2f, 0.14f, 1f) * shade; col.a = 1f;
        float half = 2.4f * scale;

        Vector2 previousLeft = position - new Vector2(heading.y, -heading.x) * half;
        Vector2 previousRight = position + new Vector2(heading.y, -heading.x) * half;
        for (int k = 1; k <= 4; k++)
        {
            float turn = 0.4f * side;
            heading = new Vector2(heading.x * Mathf.Cos(turn) - heading.y * Mathf.Sin(turn), heading.x * Mathf.Sin(turn) + heading.y * Mathf.Cos(turn));
            position += heading * step;
            float hw = half * (1f - k * 0.17f);
            Vector2 n = new Vector2(heading.y, -heading.x);
            int b = vh.currentVertCount;
            AddVertex(vh, previousLeft, col);
            AddVertex(vh, previousRight, col);
            AddVertex(vh, position - n * hw, col);
            AddVertex(vh, position + n * hw, col);
            vh.AddTriangle(b, b + 1, b + 2);
            vh.AddTriangle(b + 1, b + 3, b + 2);
            previousLeft = position - n * hw;
            previousRight = position + n * hw;
        }

        // Ventosa: disco con borde más oscuro y centro más claro
        float radius = 7f * scale;
        Color rim = new Color(0.08f, 0.07f, 0.1f, 1f) * shade; rim.a = 1f;
        Color mid = new Color(0.26f, 0.17f, 0.32f, 1f) * shade; mid.a = 1f;
        int c = vh.currentVertCount;
        AddVertex(vh, position, mid);
        const int sides = 8;
        for (int k = 0; k < sides; k++)
        {
            float ang = k * 6.2832f / sides;
            AddVertex(vh, position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius, rim);
        }
        for (int k = 0; k < sides; k++) vh.AddTriangle(c, c + 1 + k, c + 1 + (k + 1) % sides);
    }

    static float Pulse(float t, float u, float phase)
    {
        float p = 0.5f + 0.5f * Mathf.Sin(t * 1.7f - u * 8f + phase);
        return p * p;
    }

    void Thorn(VertexHelper vh, Vine vine, int index, Vector2 p, Vector2 dir, Vector2 normal, float w, float u, float t, float scale, float shade, float fraction)
    {
        float side = (index / vine.thornEvery) % 2 == 0 ? 1f : -1f;
        // Las espinas brotan cuando la enredadera ya ha pasado por ahí
        float sprout = Mathf.Clamp01((fraction - u) * 14f);
        if (sprout <= 0f) return;

        float taper = 1f - 0.55f * u;
        float length = vine.thornLength * scale * sprout * Mathf.Clamp(taper, 0.35f, 1f);
        Vector2 thornDir = (normal * side * 0.82f + dir * 0.57f).normalized;
        // Ligera curva hacia arriba en la punta
        Vector2 tip = p + normal * side * w * 0.6f + thornDir * length + dir * length * 0.12f;
        float baseWidth = Mathf.Max(2.2f, w * 0.8f);

        float pulse = Pulse(t, u, vine.pulsePhase);
        Color baseColor = new Color(0.25f, 0.16f, 0.32f) * shade; baseColor.a = 1f;
        Color tipColor = Color.Lerp(new Color(0.42f, 0.2f, 0.62f), new Color(0.82f, 0.48f, 1f), pulse) * (vine.back ? 0.75f : 1f);
        tipColor.a = 1f;

        AddVertex(vh, p - dir * baseWidth + normal * side * w * 0.4f, baseColor);
        AddVertex(vh, p + dir * baseWidth + normal * side * w * 0.4f, baseColor);
        AddVertex(vh, tip, tipColor);
        int b = vh.currentVertCount - 3;
        vh.AddTriangle(b, b + 1, b + 2);

        glowPoints.Add(new GlowPoint
        {
            position = tip,
            size = (15f + 15f * pulse) * scale * (vine.back ? 0.7f : 1f),
            alpha = (0.05f + 0.24f * pulse) * sprout * (vine.back ? 0.6f : 1f)
        });
    }

    static void AddVertex(VertexHelper vh, Vector2 position, Color color)
    {
        vh.AddVert(position, color, Vector2.zero);
    }
}
