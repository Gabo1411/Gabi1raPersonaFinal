using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Halos violetas sobre las puntas de las espinas; leen los puntos que calculó MenuVines en el mismo fotograma
[RequireComponent(typeof(CanvasRenderer))]
[ExecuteAlways]
public class VineGlow : MaskableGraphic
{
    public MenuVines source;

    public override Texture mainTexture => BuffIcons.Glow.texture;

    void LateUpdate()
    {
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (source == null) return;

        foreach (MenuVines.GlowPoint point in source.glowPoints)
        {
            float h = point.size * 0.5f;
            Color c = new Color(0.72f, 0.38f, 1f, Mathf.Clamp01(point.alpha));
            int b = vh.currentVertCount;
            vh.AddVert(point.position + new Vector2(-h, -h), c, new Vector2(0f, 0f));
            vh.AddVert(point.position + new Vector2(-h, h), c, new Vector2(0f, 1f));
            vh.AddVert(point.position + new Vector2(h, h), c, new Vector2(1f, 1f));
            vh.AddVert(point.position + new Vector2(h, -h), c, new Vector2(1f, 0f));
            vh.AddTriangle(b, b + 1, b + 2);
            vh.AddTriangle(b + 2, b + 3, b);
        }
    }
}
