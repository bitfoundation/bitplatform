using System.Drawing;
using System.Numerics;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class CanvasSizeTests
{
    [TestMethod]
    public void GrowingCentresTheImageAndFillsTheNewArea()
    {
        using var source = TestImages.Gradient(10, 6);
        var red = new BmageRgba32(255, 0, 0);
        using var output = new BmageCanvasSizeOperation(20, 10, BmageAnchor.Center, red).Apply(source, BmageOperationContext.Default);

        Assert.AreEqual(new Size(20, 10), output.Size);
        for (var y = 0; y < 10; y++)
            for (var x = 0; x < 20; x++)
            {
                var inside = x >= 5 && x < 15 && y >= 2 && y < 8;
                Assert.AreEqual(inside ? source[x - 5, y - 2] : red, output[x, y], $"pixel {x},{y}");
            }
    }

    [TestMethod]
    public void TheDefaultBackgroundIsTransparent()
    {
        using var source = TestImages.Gradient(4, 4);
        using var output = BmageImagePipeline.Create().CanvasSize(8, 8, BmageAnchor.TopLeft).Execute(source);
        Assert.AreEqual(BmageRgba32.Transparent, output[7, 7]);
        Assert.AreEqual(source[3, 3], output[3, 3]);
    }

    [TestMethod]
    public void ShrinkingCutsTheEdgesTheAnchorLeavesOut()
    {
        using var source = TestImages.Gradient(12, 9);
        foreach (var (anchor, origin) in new[] { (BmageAnchor.TopLeft, new Point(0, 0)), (BmageAnchor.BottomRight, new Point(7, 5)), (BmageAnchor.Center, new Point(3, 2)) })
        {
            using var output = new BmageCanvasSizeOperation(5, 4, anchor).Apply(source, BmageOperationContext.Default);
            using var expected = new BmageCropOperation(origin.X, origin.Y, 5, 4).Apply(source, BmageOperationContext.Default);
            Assert.AreEqual(0, TestImages.MaxChannelDifference(expected, output), anchor.ToString());
        }
    }

    [TestMethod]
    public void MarginsAddAndCutPerSide()
    {
        using var source = TestImages.Gradient(10, 8);
        var white = BmageRgba32.White;
        using var output = BmageCanvasSizeOperation.Margins(3, -2, 0, 4, white).Apply(source, BmageOperationContext.Default);

        Assert.AreEqual(new Size(13, 10), output.Size);
        Assert.AreEqual(white, output[0, 0]);
        Assert.AreEqual(source[0, 2], output[3, 0], "the top two rows are cut");
        Assert.AreEqual(source[9, 7], output[12, 5]);
        Assert.AreEqual(white, output[12, 6], "four rows are added at the bottom");
    }

    [TestMethod]
    public void ZeroMarginsAreAnIdentityTheOptimiserDrops()
    {
        var pad = BmageCanvasSizeOperation.Margins(0);
        Assert.IsTrue(pad.IsIdentity);
        Assert.AreEqual(0, BmageImagePipeline.Create().Apply(pad).Optimize(new Size(10, 10)).Count);
        Assert.IsFalse(new BmageCanvasSizeOperation(10, 10).IsIdentity, "an absolute size depends on the input");
    }

    [TestMethod]
    public void MarginsThatLeaveNoCanvasAreRejectedBeforeAndDuringExecution()
    {
        var op = BmageCanvasSizeOperation.Margins(-6, 0, -5, 0);
        Assert.ThrowsExactly<ArgumentException>(() => op.GetOutputSize(new Size(10, 10)));
        using var source = TestImages.Gradient(10, 10);
        Assert.ThrowsExactly<ArgumentException>(() => op.Apply(source, BmageOperationContext.Default).Dispose());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageCanvasSizeOperation(0, 10));
    }

    [TestMethod]
    public void TheTransformMapsSourcePixelsToWhereTheyLand()
    {
        var size = new Size(10, 6);
        foreach (var op in new[] { new BmageCanvasSizeOperation(25, 9, BmageAnchor.BottomRight), BmageCanvasSizeOperation.Margins(4, 1, 0, 0), new BmageCanvasSizeOperation(6, 4, BmageAnchor.Center) })
        {
            using var source = TestImages.Gradient(size.Width, size.Height);
            using var output = op.Apply(source, BmageOperationContext.Default);
            var m = op.GetTransform(size)!.Value;
            for (var y = 0; y < size.Height; y++)
                for (var x = 0; x < size.Width; x++)
                {
                    var p = Vector2.Transform(new Vector2(x + 0.5f, y + 0.5f), m);
                    var (dx, dy) = ((int)MathF.Floor(p.X), (int)MathF.Floor(p.Y));
                    if (dx < 0 || dy < 0 || dx >= output.Width || dy >= output.Height) continue;
                    Assert.AreEqual(source[x, y], output[dx, dy], $"{op} at {x},{y}");
                }
        }
    }

    [TestMethod]
    public void ForScaleExpressesTheSameCanvasForAProxy()
    {
        var absolute = (BmageCanvasSizeOperation)new BmageCanvasSizeOperation(1200, 800, BmageAnchor.Top, BmageRgba32.White).ForScale(0.25);
        Assert.AreEqual(new Size(300, 200), absolute.Size);
        Assert.AreEqual(BmageAnchor.Top, absolute.Anchor);
        Assert.AreEqual(BmageRgba32.White, absolute.Background);

        var margins = (BmageCanvasSizeOperation)BmageCanvasSizeOperation.Margins(40, -20, 10, 0).ForScale(0.5);
        Assert.AreEqual((20, -10, 5, 0), (margins.Left, margins.Top, margins.Right, margins.Bottom));
    }

    [TestMethod]
    public void TheSourceIsLeftAloneUnlessItMayBeMutated()
    {
        using var source = TestImages.Gradient(6, 6);
        var before = source.ToArray();
        using var output = BmageCanvasSizeOperation.Margins(2).Apply(source, BmageOperationContext.Default.WithMutation(false));
        CollectionAssert.AreEqual(before, source.ToArray());
        Assert.AreNotSame(source, output);
    }

    [TestMethod]
    public void APipelineEstimatesTheCanvasItWillProduce()
    {
        var pipeline = BmageImagePipeline.Create().Pad(10, BmageRgba32.White).CanvasSize(50, 50, BmageAnchor.TopLeft);
        Assert.AreEqual(new Size(50, 50), pipeline.GetOutputSize(new Size(20, 30)));
        using var source = TestImages.Gradient(20, 30);
        using var output = pipeline.Execute(source);
        Assert.AreEqual(new Size(50, 50), output.Size);
        Assert.AreEqual(BmageRgba32.White, output[0, 0]);
        Assert.AreEqual(source[0, 0], output[10, 10]);
        Assert.AreEqual(BmageRgba32.Transparent, output[45, 45]);
    }
}

[TestClass]
public sealed class StraightenTests
{
    [TestMethod]
    public void ZeroDegreesChangesNothing()
    {
        var op = new BmageStraightenOperation(0);
        Assert.IsTrue(op.IsIdentity);
        using var source = TestImages.Gradient(9, 7);
        using var output = op.Apply(source, BmageOperationContext.Default.WithMutation(false));
        Assert.AreEqual(0, TestImages.MaxChannelDifference(source, output));
        Assert.AreEqual(source.Size, op.GetOutputSize(source.Size));
    }

    [TestMethod]
    [DataRow(1.0, 200, 150)]
    [DataRow(-7.5, 200, 150)]
    [DataRow(12.0, 90, 160)]
    [DataRow(30.0, 120, 120)]
    [DataRow(-45.0, 160, 90)]
    [DataRow(3.0, 5, 3)]
    public void AnOpaqueImageStaysOpaqueToItsVeryEdges(double degrees, int width, int height)
    {
        using var source = ParallelProcessingTests.Noise(width, height, seed: 11);
        for (var y = 0; y < height; y++)
            foreach (ref var p in source.GetRow(y)) p = new BmageRgba32(p.R, p.G, p.B, 255);

        foreach (var sampling in Enum.GetValues<BmageWarpSampling>())
        {
            using var output = new BmageStraightenOperation(degrees, sampling).Apply(source, BmageOperationContext.Default);
            AssertOpaque(output, $"{sampling} at {degrees} degrees");
        }
    }

    [TestMethod]
    public void NoAngleLeavesATranslucentCorner()
    {
        // Where the crop fits the rotated image exactly, the outermost samples lie on the image's very edge, half of a
        // bilinear sample's taps beyond it. Rounding usually leaves some slack; sweeping the angles finds the exact fits.
        foreach (var (width, height) in new[] { (64, 48), (48, 64), (50, 50), (97, 31) })
        {
            using var source = BmageImageBuffer.Create(width, height, new BmageRgba32(200, 120, 40));
            for (var degrees = -45.0; degrees <= 45; degrees += 0.75)
                foreach (var sampling in Enum.GetValues<BmageWarpSampling>())
                {
                    using var output = new BmageStraightenOperation(degrees, sampling).Apply(source, BmageOperationContext.Default);
                    AssertOpaque(output, $"{sampling} at {degrees} degrees on {width}x{height}");
                }
        }
    }

    private static void AssertOpaque(BmageImageBuffer image, string what)
    {
        for (var y = 0; y < image.Height; y++)
            foreach (var p in image.GetRow(y))
                Assert.AreEqual(255, p.A, $"{what} left a translucent pixel on row {y}.");
    }

    [TestMethod]
    public void TheCropIsTheLargestRectangleOfTheSameAspectInsideTheRotatedImage()
    {
        var size = new Size(4000, 3000);
        var op = new BmageStraightenOperation(5);
        var output = op.GetOutputSize(size);
        var k = BmageStraightenOperation.GetCropScale(size, 5);
        Assert.AreEqual((int)Math.Floor(4000 * k), output.Width);
        Assert.AreEqual((int)Math.Floor(3000 * k), output.Height);

        // The corners of the crop, turned back into the source, sit on or inside its edges, and one pair touches them.
        var inverse = Matrix3x2.Invert(op.GetTransform(size)!.Value, out var inv) ? inv : throw new InvalidOperationException();
        var corners = new[] { Vector2.Zero, new(output.Width, 0), new(0, output.Height), new(output.Width, output.Height) }.Select(c => Vector2.Transform(c, inverse)).ToArray();
        foreach (var c in corners)
        {
            Assert.IsTrue(c.X >= -0.01f && c.X <= size.Width + 0.01f && c.Y >= -0.01f && c.Y <= size.Height + 0.01f, $"corner {c} lies outside the source");
        }
        var slack = corners.Min(c => Math.Min(Math.Min(c.X, size.Width - c.X), Math.Min(c.Y, size.Height - c.Y)));
        Assert.IsLessThan(2f, slack, "the crop should touch the rotated image's edge, not leave a margin");
    }

    [TestMethod]
    public void TheInteriorMatchesARotationCroppedAboutTheCentre()
    {
        using var source = TestImages.Gradient(120, 80);
        var straighten = new BmageStraightenOperation(-9);
        using var straightened = straighten.Apply(source, BmageOperationContext.Default);
        using var rotated = new BmageRotateOperation(-9, expandCanvas: false).Apply(source, BmageOperationContext.Default);
        var x0 = (rotated.Width - straightened.Width) / 2;
        var y0 = (rotated.Height - straightened.Height) / 2;
        using var cropped = new BmageCropOperation(x0, y0, straightened.Width, straightened.Height).Apply(rotated, BmageOperationContext.Default);

        // Away from the border the two sample the same points of the same rotation, give or take the half pixel the
        // centred crop rounds by.
        using var inner = new BmageCropOperation(4, 4, straightened.Width - 8, straightened.Height - 8).Apply(straightened, BmageOperationContext.Default);
        using var innerExpected = new BmageCropOperation(4, 4, straightened.Width - 8, straightened.Height - 8).Apply(cropped, BmageOperationContext.Default);
        Assert.IsLessThan(6.0, TestImages.MeanChannelDifference(innerExpected, inner));
    }

    [TestMethod]
    public void TheProxyAndTheFullImageFrameTheSameCrop()
    {
        var op = new BmageStraightenOperation(6.5);
        var full = op.GetOutputSize(new Size(6000, 4000));
        var proxy = op.ForScale(0.25).GetOutputSize(new Size(1500, 1000));
        Assert.IsLessThanOrEqualTo(1, Math.Abs(proxy.Width - full.Width / 4.0));
        Assert.IsLessThanOrEqualTo(1, Math.Abs(proxy.Height - full.Height / 4.0));
    }

    [TestMethod]
    public void AnglesBeyondFortyFiveDegreesAreRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageStraightenOperation(45.01));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageStraightenOperation(double.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageStraightenOperation(double.NegativeInfinity));
        _ = new BmageStraightenOperation(-45);
    }

    [TestMethod]
    public void TheCentreStaysTheCentre()
    {
        var size = new Size(301, 201);
        var op = new BmageStraightenOperation(17);
        var output = op.GetOutputSize(size);
        var centre = Vector2.Transform(new Vector2(size.Width / 2f, size.Height / 2f), op.GetTransform(size)!.Value);
        Assert.AreEqual(output.Width / 2f, centre.X, 1e-3f);
        Assert.AreEqual(output.Height / 2f, centre.Y, 1e-3f);
    }

    [TestMethod]
    public void ItCanBeCancelled()
    {
        using var source = TestImages.Gradient(400, 300);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() => new BmageStraightenOperation(4).Apply(source, BmageOperationContext.Default.WithCancellation(cts.Token)).Dispose());
    }
}

[TestClass]
public sealed class ContentBoundsTests
{
    private static BmageImageBuffer Framed(int width, int height, Rectangle content, BmageRgba32 border, BmageRgba32 fill)
    {
        var image = BmageImageBuffer.Create(width, height, border);
        image.Fill(content, fill);
        return image;
    }

    [TestMethod]
    public void TransparentBordersAreFoundExactly()
    {
        using var image = Framed(40, 30, new Rectangle(7, 4, 20, 11), BmageRgba32.Transparent, new BmageRgba32(10, 200, 30));
        Assert.AreEqual(new Rectangle(7, 4, 20, 11), BmageContentBounds.Find(image));
    }

    [TestMethod]
    public void PaddingKeepsSomeBorderButNeverReachesOutsideTheImage()
    {
        using var image = Framed(40, 30, new Rectangle(2, 4, 20, 11), BmageRgba32.Transparent, BmageRgba32.Black);
        Assert.AreEqual(Rectangle.FromLTRB(0, 1, 25, 18), BmageContentBounds.Find(image, new BmageTrimOptions { Padding = 3 }));
    }

    [TestMethod]
    public void AUniformBorderIsFoundWithinTheTolerance()
    {
        using var image = Framed(50, 50, new Rectangle(10, 12, 25, 20), BmageRgba32.White, new BmageRgba32(30, 60, 90));
        // JPEG-like noise in the border: within 4% it is still border.
        image[0, 49] = new BmageRgba32(250, 252, 249);
        image[49, 0] = new BmageRgba32(247, 255, 251);
        Assert.AreEqual(new Rectangle(10, 12, 25, 20), BmageContentBounds.Find(image));
        Assert.AreEqual(Rectangle.FromLTRB(0, 0, 50, 50), BmageContentBounds.Find(image, new BmageTrimOptions { Tolerance = 0 }), "with no tolerance the noise is content");
    }

    [TestMethod]
    public void AnImageThatIsAllBackgroundHasNoContent()
    {
        using var image = BmageImageBuffer.Create(20, 10, BmageRgba32.White);
        Assert.IsNull(BmageContentBounds.Find(image));
        using var clear = BmageImageBuffer.Create(20, 10);
        Assert.IsNull(BmageContentBounds.Find(clear, new BmageTrimOptions { Mode = BmageTrimMode.Transparent }));
    }

    [TestMethod]
    public void AutoUsesTransparencyOnlyWhenACornerIsTransparent()
    {
        // An opaque white page with a translucent stamp in it: the page is the border, the stamp is content.
        using var page = Framed(30, 30, new Rectangle(5, 5, 10, 10), BmageRgba32.White, new BmageRgba32(0, 0, 0, 40));
        Assert.AreEqual(new Rectangle(5, 5, 10, 10), BmageContentBounds.Find(page));

        // A cut-out on transparency with a white subject: transparency is the border, the white is content.
        using var cutout = Framed(30, 30, new Rectangle(8, 3, 4, 20), BmageRgba32.Transparent, BmageRgba32.White);
        Assert.AreEqual(new Rectangle(8, 3, 4, 20), BmageContentBounds.Find(cutout));
    }

    [TestMethod]
    public void AnExplicitColourIsTheBorderWhateverTheCorners()
    {
        var green = new BmageRgba32(0, 255, 0);
        using var image = Framed(30, 20, new Rectangle(0, 0, 30, 20), green, green);
        image.Fill(new Rectangle(0, 0, 3, 3), BmageRgba32.Black);
        image.Fill(new Rectangle(20, 10, 5, 5), BmageRgba32.Black);
        Assert.AreEqual(Rectangle.FromLTRB(0, 0, 25, 15), BmageContentBounds.Find(image, new BmageTrimOptions { Mode = BmageTrimMode.Color, Color = green }));
    }

    [TestMethod]
    public void TheBoundsCropToTheContent()
    {
        using var image = Framed(64, 48, new Rectangle(13, 9, 30, 21), BmageRgba32.Transparent, new BmageRgba32(200, 100, 50));
        var bounds = BmageContentBounds.Find(image)!.Value;
        using var trimmed = BmageImagePipeline.Create().Crop(bounds).Execute(image);
        Assert.AreEqual(new Size(30, 21), trimmed.Size);
        Assert.IsFalse(trimmed.HasTransparency());
    }

    [TestMethod]
    public void InvalidOptionsAreRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageTrimOptions { Padding = -1 });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageTrimOptions { Tolerance = float.NaN });
        Assert.AreEqual(1f, new BmageTrimOptions { Tolerance = 3 }.Tolerance);
    }
}
