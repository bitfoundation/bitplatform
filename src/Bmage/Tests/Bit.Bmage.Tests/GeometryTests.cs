using System.Drawing;
using Bit.Bmage.Geometry;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class OrientationTests
{
    [TestMethod]
    public void EveryOrientationComposedWithItsInverseIsNormal()
    {
        foreach (Orientation o in Enum.GetValues<Orientation>())
        {
            if (o == Orientation.Unspecified) continue;
            Assert.AreEqual(Orientation.Normal, o.Then(o.Inverse()));
            Assert.AreEqual(Orientation.Normal, o.Inverse().Then(o));
        }
    }

    [TestMethod]
    public void CompositionMatchesPixelTransformForAllPairs()
    {
        // The algebraic composition must agree with actually applying both transforms to pixels.
        var values = Enum.GetValues<Orientation>().Where(o => o != Orientation.Unspecified).ToArray();
        foreach (var a in values)
        {
            foreach (var b in values)
            {
                using var source = TestImages.Gradient(7, 5);
                using var stepwise = Apply(Apply(source, a), b);
                using var combined = Apply(source, a.Then(b));
                Assert.AreEqual(combined.Width, stepwise.Width);
                Assert.AreEqual(combined.Height, stepwise.Height);
                Assert.AreEqual(0, TestImages.MaxChannelDifference(combined, stepwise));
            }
        }

        static ImageBuffer Apply(ImageBuffer src, Orientation o)
            => new Operations.Transforms.OrientationOperation(o).Apply(src, Operations.OperationContext.Default);
    }

    [TestMethod]
    public void MatrixAgreesWithPixelMapping()
    {
        var size = new Size(7, 5);
        foreach (var o in Enum.GetValues<Orientation>().Where(v => v != Orientation.Unspecified))
        {
            using var source = TestImages.Gradient(size.Width, size.Height);
            using var rotated = new Operations.Transforms.OrientationOperation(o).Apply(source, Operations.OperationContext.Default);
            for (var y = 0; y < size.Height; y++)
            {
                for (var x = 0; x < size.Width; x++)
                {
                    // Map the pixel centre and check the destination pixel matches.
                    var p = o.Transform(new PointF(x + 0.5f, y + 0.5f), size);
                    var dx = (int)Math.Floor(p.X);
                    var dy = (int)Math.Floor(p.Y);
                    Assert.IsInRange(0, rotated.Width - 1, dx);
                    Assert.IsInRange(0, rotated.Height - 1, dy);
                    Assert.AreEqual(source[x, y], rotated[dx, dy]);
                }
            }
        }
    }

    [TestMethod]
    [DataRow(Orientation.Rotate90, true)]
    [DataRow(Orientation.Rotate270, true)]
    [DataRow(Orientation.Transpose, true)]
    [DataRow(Orientation.Transverse, true)]
    [DataRow(Orientation.Normal, false)]
    [DataRow(Orientation.Rotate180, false)]
    [DataRow(Orientation.FlipHorizontal, false)]
    public void ReportsDimensionSwaps(Orientation o, bool swaps) => Assert.AreEqual(swaps, o.SwapsDimensions());

    [TestMethod]
    public void FromRotationRoundTrips()
    {
        Assert.AreEqual(Orientation.Rotate90, OrientationMath.FromRotation(90));
        Assert.AreEqual(Orientation.Rotate180, OrientationMath.FromRotation(180));
        Assert.AreEqual(Orientation.Rotate270, OrientationMath.FromRotation(-90));
        Assert.AreEqual(Orientation.Normal, OrientationMath.FromRotation(360));
    }
}

[TestClass]
public sealed class ResizePlanTests
{
    private static readonly Size Landscape = new(1000, 500);

    [TestMethod]
    public void FitKeepsAspectRatioInsideBounds()
    {
        var plan = ResizePlan.Compute(Landscape, 400, 400, ResizeMode.Fit);
        Assert.AreEqual(new Size(400, 200), plan.CanvasSize);
    }

    [TestMethod]
    public void ContainPadsToExactTarget()
    {
        var plan = ResizePlan.Compute(Landscape, 400, 400, ResizeMode.Contain);
        Assert.AreEqual(new Size(400, 400), plan.CanvasSize);
        Assert.AreEqual(new Rectangle(0, 100, 400, 200), plan.DestinationRect);
    }

    [TestMethod]
    public void CoverCropsSourceToExactTarget()
    {
        var plan = ResizePlan.Compute(Landscape, 400, 400, ResizeMode.Cover);
        Assert.AreEqual(new Size(400, 400), plan.CanvasSize);
        Assert.AreEqual(new Rectangle(0, 0, 400, 400), plan.DestinationRect);
        Assert.AreEqual(500, plan.SourceRect.Width);
        Assert.AreEqual(500, plan.SourceRect.Height);
        Assert.AreEqual(250, plan.SourceRect.X);
    }

    [TestMethod]
    public void MaxNeverEnlarges()
    {
        var plan = ResizePlan.Compute(new Size(100, 50), 400, 400, ResizeMode.Max);
        Assert.AreEqual(new Size(100, 50), plan.CanvasSize);
    }

    [TestMethod]
    public void MinNeverShrinks()
    {
        var plan = ResizePlan.Compute(new Size(100, 50), 20, 20, ResizeMode.Min);
        Assert.AreEqual(new Size(100, 50), plan.CanvasSize);
    }

    [TestMethod]
    public void SingleDimensionDerivesTheOtherProportionally()
    {
        var plan = ResizePlan.Compute(Landscape, 250, null);
        Assert.AreEqual(new Size(250, 125), plan.CanvasSize);
        plan = ResizePlan.Compute(Landscape, null, 125);
        Assert.AreEqual(new Size(250, 125), plan.CanvasSize);
    }

    [TestMethod]
    public void StretchIgnoresAspectRatio()
    {
        var plan = ResizePlan.Compute(Landscape, 300, 300, ResizeMode.Stretch);
        Assert.AreEqual(new Size(300, 300), plan.CanvasSize);
    }

    [TestMethod]
    public void CoverRespectsAnchor()
    {
        var top = ResizePlan.Compute(Landscape, 400, 400, ResizeMode.Cover, Anchor.Left);
        Assert.AreEqual(0, top.SourceRect.X);
        var right = ResizePlan.Compute(Landscape, 400, 400, ResizeMode.Cover, Anchor.Right);
        Assert.AreEqual(500, right.SourceRect.X);
    }

    [TestMethod]
    public void RejectsInvalidArguments()
    {
        Assert.ThrowsExactly<ArgumentException>(() => ResizePlan.Compute(Landscape, null, null));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ResizePlan.Compute(Landscape, 0, 10));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ResizePlan.Compute(new Size(0, 10), 10, 10));
    }
}

[TestClass]
public sealed class AspectRatioTests
{
    [TestMethod]
    public void FitsLargestRectangleInsideBounds()
    {
        var r = AspectRatio.Ratio16x9.FitInside(new Rectangle(0, 0, 100, 100));
        Assert.AreEqual(100, r.Width);
        Assert.AreEqual(56, r.Height);
        Assert.AreEqual(22, r.Y); // centred
    }

    [TestMethod]
    public void SquareInsideWideBoundsIsLimitedByHeight()
    {
        var r = AspectRatio.Square.FitInside(new Rectangle(0, 0, 200, 50));
        Assert.AreEqual(new Size(50, 50), r.Size);
        Assert.AreEqual(75, r.X);
    }

    [TestMethod]
    public void AnchorPositionsTheResult()
    {
        var r = AspectRatio.Square.FitInside(new Rectangle(0, 0, 200, 50), Anchor.TopLeft);
        Assert.AreEqual(new Point(0, 0), r.Location);
    }
}

[TestClass]
public sealed class AnchorTests
{
    [TestMethod]
    [DataRow(Anchor.TopLeft, 0, 0)]
    [DataRow(Anchor.TopRight, 8, 0)]
    [DataRow(Anchor.BottomLeft, 0, 6)]
    [DataRow(Anchor.BottomRight, 8, 6)]
    [DataRow(Anchor.Center, 4, 3)]
    [DataRow(Anchor.Top, 4, 0)]
    [DataRow(Anchor.Left, 0, 3)]
    public void PlacesContentInsideArea(Anchor anchor, int expectedX, int expectedY)
    {
        var r = anchor.Place(new Size(2, 4), new Size(10, 10));
        Assert.AreEqual(new Point(expectedX, expectedY), r.Location);
    }
}
