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

    // Enredaderas pegadas al muro: troncos gruesos en la base que suben en zigzag y se afinan hacia la punta.
    // No se mueven; solo laten las espinas.
    void Build()
    {
        vines.Clear();
        System.Random rng = new System.Random(311);

        // Del fondo: más finas y oscuras, entre las principales
        float[] backX = { 0.15f, 0.37f, 0.63f, 0.85f };
        foreach (float x in backX)
        {
            Vine vine = NewVine(rng, x + Rand(rng, -0.03f, 0.03f), Rand(rng, 0.5f, 0.68f), Rand(rng, 34f, 46f), true);
            vines.Add(vine);
            AddBranches(rng, vine, 2);
        }

        // Principales: llegan por encima de la mitad de la pantalla
        float[] frontX = { 0.04f, 0.25f, 0.5f, 0.75f, 0.96f };
        float[] frontLean = { 0.1f, -0.06f, 0.04f, 0.06f, -0.1f };
        for (int i = 0; i < frontX.Length; i++)
        {
            Vine vine = NewVine(rng, frontX[i], Rand(rng, 0.66f, 0.9f), Rand(rng, 62f, 80f), false);
            vine.lean = frontLean[i];
            vines.Add(vine);
            AddBranches(rng, vine, 4);
        }
    }

    Vine NewVine(System.Random rng, float x, float length, float width, bool back)
    {
        return new Vine
        {
            baseX = x,
            length = length,
            width = width,
            lean = Rand(rng, -0.1f, 0.1f),
            amplitude = Rand(rng, 0.66f, 0.82f),
            frequency = Rand(rng, 2.6f, 3.6f),
            phase = Rand(rng, 0f, 1f),
            thornLength = Rand(rng, 40f, 56f) * (back ? 0.75f : 1f),
            pulsePhase = Rand(rng, 0f, 6.28f),
            thornEvery = 4,
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
            Vine branch = NewVine(rng, 0f, parent.length * Rand(rng, 0.3f, 0.46f) * (1f - at * 0.6f), parent.width * Taper(at) * 0.55f, parent.back);
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
    static float Zig(float x)
    {
        float s = Mathf.Sin(6.2832f * x);
        float tri = Mathf.Asin(s) * 0.6366f;
        return 0.88f * tri + 0.12f * s;
    }

    // La forma es fija: depende solo del tamaño de la pantalla
    void Shape(Vine vine, Rect rect, float scale)
    {
        Vector2 origin;
        float angle0;
        if (vine.parent == null)
        {
            origin = new Vector2(rect.xMin + vine.baseX * rect.width, rect.yMin - 40f * scale);
            angle0 = vine.lean;
        }
        else
        {
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
                angle = vine.lean + vine.amplitude * Zig(u * vine.frequency + vine.phase) * (1f - 0.35f * u);
            else
                angle = angle0 + vine.theta * (1f - 0.55f * u) + vine.amplitude * Zig(u * vine.frequency + vine.phase) * 0.8f;
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
        foreach (Vine vine in vines) if (vine.back) Draw(vh, vine, t, scale);
        foreach (Vine vine in vines) if (!vine.back) Draw(vh, vine, t, scale);
    }

    void Draw(VertexHelper vh, Vine vine, float t, float scale)
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

            if (i > 1 && i < count && i % vine.thornEvery == 0)
                Thorn(vh, vine, i, p, dir, normal, w, u, t, scale, shade, fraction);
        }
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
