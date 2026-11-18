using System.Drawing;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class OrientationTests
{
    [TestMethod]
    public void EveryOrientationComposedWithItsInverseIsNormal()
    {
        foreach (BmageOrientation o in Enum.GetValues<BmageOrientation>())
        {
            if (o == BmageOrientation.Unspecified) continue;
            Assert.AreEqual(BmageOrientation.Normal, o.Then(o.Inverse()));
            Assert.AreEqual(BmageOrientation.Normal, o.Inverse().Then(o));
        }
    }

    [TestMethod]
    public void CompositionMatchesPixelTransformForAllPairs()
    {
        // The algebraic composition must agree with actually applying both transforms to pixels.
        var values = Enum.GetValues<BmageOrientation>().Where(o => o != BmageOrientation.Unspecified).ToArray();
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

        static BmageImageBuffer Apply(BmageImageBuffer src, BmageOrientation o)
            => new BmageOrientationOperation(o).Apply(src, BmageOperationContext.Default);
    }

    [TestMethod]
    public void MatrixAgreesWithPixelMapping()
    {
        var size = new Size(7, 5);
        foreach (var o in Enum.GetValues<BmageOrientation>().Where(v => v != BmageOrientation.Unspecified))
        {
            using var source = TestImages.Gradient(size.Width, size.Height);
            using var rotated = new BmageOrientationOperation(o).Apply(source, BmageOperationContext.Default);
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
    [DataRow(BmageOrientation.Rotate90, true)]
    [DataRow(BmageOrientation.Rotate270, true)]
    [DataRow(BmageOrientation.Transpose, true)]
    [DataRow(BmageOrientation.Transverse, true)]
    [DataRow(BmageOrientation.Normal, false)]
    [DataRow(BmageOrientation.Rotate180, false)]
    [DataRow(BmageOrientation.FlipHorizontal, false)]
    public void ReportsDimensionSwaps(BmageOrientation o, bool swaps) => Assert.AreEqual(swaps, o.SwapsDimensions());

    [TestMethod]
    public void FromRotationRoundTrips()
    {
        Assert.AreEqual(BmageOrientation.Rotate90, BmageOrientationMath.FromRotation(90));
        Assert.AreEqual(BmageOrientation.Rotate180, BmageOrientationMath.FromRotation(180));
        Assert.AreEqual(BmageOrientation.Rotate270, BmageOrientationMath.FromRotation(-90));
        Assert.AreEqual(BmageOrientation.Normal, BmageOrientationMath.FromRotation(360));
    }
}

[TestClass]
public sealed class ResizePlanTests
{
    private static readonly Size Landscape = new(1000, 500);

    [TestMethod]
    public void FitKeepsAspectRatioInsideBounds()
    {
        var plan = BmageResizePlan.Compute(Landscape, 400, 400, BmageResizeMode.Fit);
        Assert.AreEqual(new Size(400, 200), plan.CanvasSize);
    }

    [TestMethod]
    public void ContainPadsToExactTarget()
    {
        var plan = BmageResizePlan.Compute(Landscape, 400, 400, BmageResizeMode.Contain);
        Assert.AreEqual(new Size(400, 400), plan.CanvasSize);
        Assert.AreEqual(new Rectangle(0, 100, 400, 200), plan.DestinationRect);
    }

    [TestMethod]
    public void CoverCropsSourceToExactTarget()
    {
        var plan = BmageResizePlan.Compute(Landscape, 400, 400, BmageResizeMode.Cover);
        Assert.AreEqual(new Size(400, 400), plan.CanvasSize);
        Assert.AreEqual(new Rectangle(0, 0, 400, 400), plan.DestinationRect);
        Assert.AreEqual(500, plan.SourceRect.Width);
        Assert.AreEqual(500, plan.SourceRect.Height);
        Assert.AreEqual(250, plan.SourceRect.X);
    }

    [TestMethod]
    public void MaxNeverEnlarges()
    {
        var plan = BmageResizePlan.Compute(new Size(100, 50), 400, 400, BmageResizeMode.Max);
        Assert.AreEqual(new Size(100, 50), plan.CanvasSize);
    }

    [TestMethod]
    public void MinNeverShrinks()
    {
        var plan = BmageResizePlan.Compute(new Size(100, 50), 20, 20, BmageResizeMode.Min);
        Assert.AreEqual(new Size(100, 50), plan.CanvasSize);
    }

    [TestMethod]
    public void SingleDimensionDerivesTheOtherProportionally()
    {
        var plan = BmageResizePlan.Compute(Landscape, 250, null);
        Assert.AreEqual(new Size(250, 125), plan.CanvasSize);
        plan = BmageResizePlan.Compute(Landscape, null, 125);
        Assert.AreEqual(new Size(250, 125), plan.CanvasSize);
    }

    [TestMethod]
    public void StretchIgnoresAspectRatio()
    {
        var plan = BmageResizePlan.Compute(Landscape, 300, 300, BmageResizeMode.Stretch);
        Assert.AreEqual(new Size(300, 300), plan.CanvasSize);
    }

    [TestMethod]
    public void CoverRespectsAnchor()
    {
        var top = BmageResizePlan.Compute(Landscape, 400, 400, BmageResizeMode.Cover, BmageAnchor.Left);
        Assert.AreEqual(0, top.SourceRect.X);
        var right = BmageResizePlan.Compute(Landscape, 400, 400, BmageResizeMode.Cover, BmageAnchor.Right);
        Assert.AreEqual(500, right.SourceRect.X);
    }

    [TestMethod]
    public void RejectsInvalidArguments()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BmageResizePlan.Compute(Landscape, null, null));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BmageResizePlan.Compute(Landscape, 0, 10));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BmageResizePlan.Compute(new Size(0, 10), 10, 10));
    }
}

[TestClass]
public sealed class AspectRatioTests
{
    [TestMethod]
    public void FitsLargestRectangleInsideBounds()
    {
        var r = BmageAspectRatio.Ratio16x9.FitInside(new Rectangle(0, 0, 100, 100));
        Assert.AreEqual(100, r.Width);
        Assert.AreEqual(56, r.Height);
        Assert.AreEqual(22, r.Y); // centred
    }

    [TestMethod]
    public void SquareInsideWideBoundsIsLimitedByHeight()
    {
        var r = BmageAspectRatio.Square.FitInside(new Rectangle(0, 0, 200, 50));
        Assert.AreEqual(new Size(50, 50), r.Size);
        Assert.AreEqual(75, r.X);
    }

    [TestMethod]
    public void AnchorPositionsTheResult()
    {
        var r = BmageAspectRatio.Square.FitInside(new Rectangle(0, 0, 200, 50), BmageAnchor.TopLeft);
        Assert.AreEqual(new Point(0, 0), r.Location);
    }
}

[TestClass]
public sealed class AnchorTests
{
    [TestMethod]
    [DataRow(BmageAnchor.TopLeft, 0, 0)]
    [DataRow(BmageAnchor.TopRight, 8, 0)]
    [DataRow(BmageAnchor.BottomLeft, 0, 6)]
    [DataRow(BmageAnchor.BottomRight, 8, 6)]
    [DataRow(BmageAnchor.Center, 4, 3)]
    [DataRow(BmageAnchor.Top, 4, 0)]
    [DataRow(BmageAnchor.Left, 0, 3)]
    public void PlacesContentInsideArea(BmageAnchor anchor, int expectedX, int expectedY)
    {
        var r = anchor.Place(new Size(2, 4), new Size(10, 10));
        Assert.AreEqual(new Point(expectedX, expectedY), r.Location);
    }
}
