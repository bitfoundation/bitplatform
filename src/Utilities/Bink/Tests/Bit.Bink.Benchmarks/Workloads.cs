using System.Numerics;

namespace Bit.Bink.Benchmarks;

/// <summary>
/// Realistic inputs for the benchmarks. Synthetic strokes are shaped like handwriting (many short strokes with
/// pressure variation) rather than convenient straight lines, so the numbers reflect real documents.
/// </summary>
internal static class Workloads
{
    /// <summary>A handwriting-like stroke: a short curved run with a pressure envelope.</summary>
    public static BinkStrokePoints HandwritingStroke(Random random, int points = 60)
    {
        float x = (float)random.NextDouble() * 1400f + 50f;
        float y = (float)random.NextDouble() * 900f + 50f;
        float scale = 4f + (float)random.NextDouble() * 12f;
        float phase = (float)random.NextDouble() * MathF.Tau;

        var buffer = new BinkStrokePointBuffer(points);
        for (int i = 0; i < points; i++)
        {
            float t = i / (float)points;
            float angle = phase + t * MathF.Tau * 0.8f;
            buffer.Add(new BinkPoint(
                x + MathF.Cos(angle) * scale * (1f + t * 2f),
                y + MathF.Sin(angle * 1.7f) * scale,
                0.2f + 0.75f * MathF.Sin(t * MathF.PI),
                i * 6f));
        }
        return buffer.ToStrokePoints();
    }

    /// <summary>A long continuous stroke, the worst case for geometry generation.</summary>
    public static BinkStrokePoints LongStroke(int points)
    {
        var buffer = new BinkStrokePointBuffer(points);
        for (int i = 0; i < points; i++)
        {
            float t = i / (float)points;
            buffer.Add(new BinkPoint(
                50f + t * 1500f,
                500f + MathF.Sin(t * 40f) * 300f,
                0.3f + 0.6f * MathF.Sin(t * MathF.PI * 6f),
                i * 4f));
        }
        return buffer.ToStrokePoints();
    }

    /// <summary>Builds a document with the given number of handwriting strokes.</summary>
    public static BinkDocument Document(int strokes, int pointsPerStroke = 60, int seed = 20260908)
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 1600, Height = 1000, Background = BinkColor.White });
        var random = new Random(seed);
        var brush = new BinkPenBrush { Width = 3.5f };
        Guid layer = document.Layers[0].Id;

        using (document.BeginBatch("Load"))
        {
            for (int i = 0; i < strokes; i++)
            {
                document.AddElement(new BinkStroke(HandwritingStroke(random, pointsPerStroke), brush), layer);
            }
        }
        return document;
    }

    /// <summary>A batch of pointer samples as one animation frame would deliver, including coalesced ones.</summary>
    public static BinkInputSample[] Frame(int count, float startX = 100f, float startY = 100f, BinkPointerKind kind = BinkPointerKind.Pen)
    {
        var samples = new BinkInputSample[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            samples[i] = new BinkInputSample
            {
                PointerId = 1,
                Kind = kind,
                Phase = i == 0 ? BinkPointerPhase.Down : BinkPointerPhase.Move,
                X = startX + i * 2.5f,
                Y = startY + MathF.Sin(t * 8f) * 20f,
                Pressure = 0.25f + 0.7f * MathF.Sin(t * MathF.PI),
                Timestamp = i * 4.0,
                Buttons = 1,
                IsPrimary = true,
                IsCoalesced = i % 4 != 0,
            };
        }
        return samples;
    }
}
