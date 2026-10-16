using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>Builders for the drawings used across the test suite.</summary>
internal static class TestData
{
    public static BinkStrokePoints Line(Vector2 from, Vector2 to, int count = 20, float pressure = 0.5f)
    {
        var buffer = new BinkStrokePointBuffer(count);
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0 : i / (float)(count - 1);
            Vector2 p = Vector2.Lerp(from, to, t);
            buffer.Add(new BinkPoint(p.X, p.Y, pressure, i * 8f));
        }
        return buffer.ToStrokePoints();
    }

    public static BinkStrokePoints Circle(Vector2 center, float radius, int count = 64)
    {
        var buffer = new BinkStrokePointBuffer(count);
        for (int i = 0; i < count; i++)
        {
            float a = i / (float)count * MathF.Tau;
            buffer.Add(new BinkPoint(center.X + MathF.Cos(a) * radius, center.Y + MathF.Sin(a) * radius, 0.5f, i * 8f));
        }
        return buffer.ToStrokePoints();
    }

    /// <summary>Points with a pressure ramp from light to heavy and back, as a real signature has.</summary>
    public static BinkStrokePoints PressureRamp(Vector2 from, Vector2 to, int count = 40)
    {
        var buffer = new BinkStrokePointBuffer(count);
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            Vector2 p = Vector2.Lerp(from, to, t);
            float pressure = 0.1f + 0.9f * MathF.Sin(t * MathF.PI);
            buffer.Add(new BinkPoint(p.X, p.Y, pressure, i * 6f));
        }
        return buffer.ToStrokePoints();
    }

    public static BinkStroke Stroke(Vector2 from, Vector2 to, BinkBrush? brush = null, int count = 20)
        => new(Line(from, to, count), brush ?? new BinkPenBrush());

    /// <summary>A document with a handful of strokes, a shape, text and two layers.</summary>
    public static BinkDocument SampleDocument()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 400, Height = 300, Background = BinkColor.White });
        Guid drawing = document.Layers[0].Id;
        document.AddElement(new BinkStroke(Line(new Vector2(20, 20), new Vector2(180, 120), 30), new BinkPenBrush { Width = 5 }), drawing);
        document.AddElement(new BinkStroke(Circle(new Vector2(100, 200), 40), new BinkMarkerBrush { Color = BinkColor.Parse("#c62828") }), drawing);
        document.AddElement(new BinkStroke(Line(new Vector2(200, 40), new Vector2(380, 60), 12), new BinkHighlighterBrush()), drawing);

        BinkLayer annotations = document.AddLayer("Annotations");
        document.AddElement(new BinkRectangleShape(new BinkRect(220, 150, 120, 80)) { Style = new BinkShapeStyle { StrokeColor = BinkColor.Parse("#1565c0"), StrokeWidth = 3 } }, annotations.Id);
        document.AddElement(new BinkLineShape(new Vector2(210, 260), new Vector2(330, 250)) { EndArrow = true, Style = new BinkShapeStyle { StrokeColor = BinkColor.Parse("#2e7d32"), StrokeWidth = 2 } }, annotations.Id);
        document.AddElement(new BinkTextElement(new Vector2(230, 165), "Review") { Style = BinkTextStyle.Default with { FontSize = 18 } }, annotations.Id);
        return document;
    }

    /// <summary>An in-memory PNG usable as a <see cref="BinkImageSource"/>.</summary>
    public static byte[] CreatePng(int width, int height, Func<int, int, BinkColor> shader)
    {
        byte[] rgba = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                BinkColor c = shader(x, y);
                int o = (y * width + x) * 4;
                rgba[o] = c.R;
                rgba[o + 1] = c.G;
                rgba[o + 2] = c.B;
                rgba[o + 3] = c.A;
            }
        }
        return BinkPngCodec.Encode(rgba, width, height);
    }
}
