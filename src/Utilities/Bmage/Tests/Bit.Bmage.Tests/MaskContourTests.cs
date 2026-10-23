using System.Drawing;
using System.Numerics;
using Bit.Bmage.Masks;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class MaskContourTests
{
    private static MaskBuffer Square(int size, Rectangle covered)
    {
        var mask = MaskBuffer.Create(size, size);
        for (var y = covered.Top; y < covered.Bottom; y++)
            for (var x = covered.Left; x < covered.Right; x++) mask[x, y] = 255;
        return mask;
    }

    /// <summary>Twice the signed area: positive for a curve turning clockwise in image coordinates, where y grows down.</summary>
    private static float SignedArea(Vector2[] curve)
    {
        var area = 0f;
        for (var i = 0; i < curve.Length; i++)
        {
            var a = curve[i];
            var b = curve[(i + 1) % curve.Length];
            area += a.X * b.Y - b.X * a.Y;
        }
        return area;
    }

    [TestMethod]
    public void ASquareHasOneClosedOutlineAlongItsEdge()
    {
        using var mask = Square(20, new Rectangle(5, 5, 10, 10));
        var curves = MaskContours.Trace(mask);
        Assert.HasCount(1, curves);
        var outline = curves[0];
        // Half way between the last covered pixel centre and the first uncovered one: the pixel edge.
        Assert.AreEqual(5f, outline.Min(p => p.X), 0.01f);
        Assert.AreEqual(15f, outline.Max(p => p.X), 0.01f);
        Assert.AreEqual(5f, outline.Min(p => p.Y), 0.51f);
        Assert.AreEqual(15f, outline.Max(p => p.Y), 0.51f);
        Assert.IsGreaterThan(0f, SignedArea(outline), "covered on the right: clockwise in image coordinates");
    }

    [TestMethod]
    public void AnAntiAliasedEdgeIsTracedToAFractionOfAPixel()
    {
        // Full coverage to the left of x = 10, a quarter at x = 10, nothing beyond: the half crossing lies between them.
        using var mask = MaskBuffer.Create(20, 4);
        for (var y = 0; y < 4; y++)
        {
            for (var x = 0; x < 10; x++) mask[x, y] = 255;
            mask[10, y] = 64;
        }
        var curve = MaskContours.Trace(mask).Single();
        var right = curve.Where(p => p.Y > 1 && p.Y < 3).Max(p => p.X);
        // From 255 at the centre 9.5 to 64 at 10.5, coverage reaches 128 two thirds of the way.
        Assert.AreEqual(9.5f + (255 - 128) / (255f - 64), right, 0.01f);
    }

    [TestMethod]
    public void AHoleIsASecondOutlineTurningTheOtherWay()
    {
        using var mask = Square(30, new Rectangle(5, 5, 20, 20));
        for (var y = 12; y < 18; y++) for (var x = 12; x < 18; x++) mask[x, y] = 0;
        var curves = MaskContours.Trace(mask);
        Assert.HasCount(2, curves);
        var areas = curves.Select(SignedArea).OrderBy(a => a).ToArray();
        Assert.IsLessThan(0f, areas[0], "the hole turns anticlockwise");
        Assert.IsGreaterThan(0f, areas[1]);
    }

    [TestMethod]
    public void DiagonalNeighboursFormNeitherABridgeNorACrossing()
    {
        // Two pixels touching only at a corner: the saddle between them must not tangle the outlines. The saddle's
        // centre, the average of its corners, is 127.5, below the threshold, so the two stay apart.
        using var mask = MaskBuffer.Create(6, 6);
        mask[2, 2] = 255;
        mask[3, 3] = 255;
        var curves = MaskContours.Trace(mask);
        Assert.HasCount(2, curves);
        foreach (var curve in curves) Assert.IsGreaterThan(0f, SignedArea(curve));
    }

    [TestMethod]
    public void AnEmptyOrFullMaskHasTheObviousOutlines()
    {
        using var empty = MaskBuffer.Create(10, 10);
        Assert.IsEmpty(MaskContours.Trace(empty));
        using var full = MaskBuffer.Create(10, 10, 255);
        var curve = MaskContours.Trace(full).Single();
        Assert.AreEqual(0.5f, curve.Min(p => p.X), 0.51f, "the image's edge closes the outline");
    }

    [TestMethod]
    public void NoiseIsCappedRatherThanTracedEndlessly()
    {
        using var noise = MaskBuffer.Create(200, 200);
        var random = new Random(3);
        for (var i = 0; i < noise.Values.Length; i++) noise.Values[i] = (byte)(random.Next(2) * 255);
        var curves = MaskContours.Trace(noise, maxPoints: 5000);
        Assert.IsLessThanOrEqualTo(5000, curves.Sum(c => c.Length));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => MaskContours.Trace(noise, threshold: 0));
    }
}
