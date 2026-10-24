using System.Drawing;
using System.Numerics;
using Bit.Bmage.Drawing;

namespace Bit.Bmage.Samples.Core.Shared;

/// <summary>
/// Draws a sunset landscape with the library's own canvas, so every sample can run without an image to hand and
/// without the repository shipping a photograph.
/// </summary>
public static class SampleScene
{
    public static ImageBuffer Draw(int width = 1200, int height = 800)
    {
        using var canvas = ImageCanvas.Create(width, height);
        float W(double f) => (float)(width * f);
        float H(double f) => (float)(height * f);

        // Sky, fading from deep blue to a warm horizon, and the sun.
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

        // A lake reflecting the sky.
        canvas.FillPath(new PathBuilder().AddRectangle(new RectangleF(0, H(0.68), width, H(0.32))).Build(), new LinearGradientPaint(
            new Vector2(0, H(0.68)), new Vector2(0, height),
            [(0f, new Rgba32(236, 160, 120)), (0.4f, new Rgba32(96, 92, 140)), (1f, new Rgba32(24, 36, 70))]));

        return canvas.Snapshot();
    }
}
