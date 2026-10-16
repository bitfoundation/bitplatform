using System.Drawing;
using System.Numerics;
using Bit.Bmage.Drawing;

namespace Bit.Bmage.Demo.Client.Shared;

/// <summary>
/// Draws a photo-like scene with the library's own canvas, so every demo can be tried without an image to hand and
/// without the repository shipping a photograph.
/// </summary>
public static class SampleImages
{
    /// <summary>A sunset landscape with a person standing off-centre, which gives smart cropping a subject to find.</summary>
    public static ImageBuffer Landscape(int width = 1200, int height = 800)
    {
        using var canvas = ImageCanvas.Create(width, height);
        float W(double f) => (float)(width * f);
        float H(double f) => (float)(height * f);

        // Sky, fading from deep blue to a warm horizon.
        canvas.FillPath(new PathBuilder().AddRectangle(new RectangleF(0, 0, width, height)).Build(), new LinearGradientPaint(
            new Vector2(0, 0), new Vector2(0, H(0.62)),
            [(0f, new Rgba32(28, 52, 120)), (0.55f, new Rgba32(226, 128, 102)), (1f, new Rgba32(252, 206, 132))]));
        canvas.FillCircle(new Vector2(W(0.70), H(0.50)), H(0.10), new Rgba32(255, 238, 186));

        // Two mountain ranges, the nearer one darker.
        canvas.FillPolygon(
        [
            new(0, H(0.62)), new(W(0.12), H(0.40)), new(W(0.24), H(0.52)), new(W(0.40), H(0.32)), new(W(0.55), H(0.50)),
            new(W(0.72), H(0.36)), new(W(0.88), H(0.48)), new(width, H(0.42)), new(width, H(0.62)),
        ], new Rgba32(104, 86, 132));
        canvas.FillPolygon(
        [
            new(0, H(0.66)), new(W(0.18), H(0.52)), new(W(0.33), H(0.62)), new(W(0.50), H(0.50)), new(W(0.66), H(0.64)),
            new(W(0.84), H(0.54)), new(width, H(0.62)), new(width, H(0.70)), new(0, H(0.70)),
        ], new Rgba32(58, 52, 86));

        // A lake reflecting the sky, with a glint under the sun.
        canvas.FillPath(new PathBuilder().AddRectangle(new RectangleF(0, H(0.68), width, H(0.32))).Build(), new LinearGradientPaint(
            new Vector2(0, H(0.68)), new Vector2(0, height),
            [(0f, new Rgba32(236, 160, 120)), (0.4f, new Rgba32(96, 92, 140)), (1f, new Rgba32(24, 36, 70))]));
        for (var i = 0; i < 6; i++)
        {
            var y = H(0.71 + i * 0.035);
            var half = W(0.06 - i * 0.008);
            canvas.DrawLine(new Vector2(W(0.70) - half, y), new Vector2(W(0.70) + half, y), new Rgba32(255, 232, 190, 170),
                new StrokeStyle { Width = H(0.006) });
        }

        // A figure on the shore, a third of the way in: skin tones and detail for smart framing to find.
        var cx = W(0.30);
        canvas.FillPolygon([new(cx - W(0.05), height), new(cx - W(0.035), H(0.60)), new(cx + W(0.035), H(0.60)), new(cx + W(0.05), height)],
            new Rgba32(186, 52, 60));
        canvas.FillCircle(new Vector2(cx, H(0.53)), H(0.065), new Rgba32(224, 172, 140));
        canvas.FillEllipse(new RectangleF(cx - H(0.07), H(0.455), H(0.14), H(0.06)), new Rgba32(60, 40, 32));
        canvas.FillCircle(new Vector2(cx - H(0.022), H(0.53)), H(0.008), new Rgba32(40, 30, 30));
        canvas.FillCircle(new Vector2(cx + H(0.022), H(0.53)), H(0.008), new Rgba32(40, 30, 30));

        return canvas.Snapshot();
    }
}
