using UnityEngine;
using UnityEngine.UI;

// A texture-free clock face: remaining time is the clockwise arc from twelve o'clock.
public sealed class CountdownClockGraphic : MaskableGraphic
{
    private float fraction = 1f;
    private static readonly Color TrackColor = new Color(0.42f, 0.33f, 0.24f, 0.65f);
    private static readonly Color TickColor = new Color(0.98f, 0.88f, 0.65f, 0.8f);

    public float Fraction => fraction;

    public void SetFraction(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(value, fraction)) return;
        fraction = value;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        AddArc(vh, center, radius * 0.78f, radius * 0.94f, 1f, TrackColor);
        AddArc(vh, center, radius * 0.78f, radius * 0.94f, fraction, color);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI / 6f;
            Vector2 direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            Vector2 side = new Vector2(direction.y, -direction.x) * radius * 0.012f;
            Vector2 outer = center + direction * radius * 0.72f;
            Vector2 inner = center + direction * radius * (i % 3 == 0 ? 0.60f : 0.65f);
            AddQuad(vh, inner - side, inner + side, outer + side, outer - side, TickColor);
        }
    }

    private static void AddArc(VertexHelper vh, Vector2 center, float innerRadius,
        float outerRadius, float portion, Color tint)
    {
        int segments = Mathf.CeilToInt(portion * 96f);
        for (int i = 0; i < segments; i++)
        {
            float a = (float)i / segments * portion * Mathf.PI * 2f;
            float b = (float)(i + 1) / segments * portion * Mathf.PI * 2f;
            Vector2 first = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
            Vector2 next = new Vector2(Mathf.Sin(b), Mathf.Cos(b));
            AddQuad(vh, center + first * innerRadius, center + first * outerRadius,
                center + next * outerRadius, center + next * innerRadius, tint);
        }
    }

    private static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
    {
        int start = vh.currentVertCount;
        vh.AddVert(a, tint, Vector2.zero);
        vh.AddVert(b, tint, Vector2.zero);
        vh.AddVert(c, tint, Vector2.zero);
        vh.AddVert(d, tint, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}