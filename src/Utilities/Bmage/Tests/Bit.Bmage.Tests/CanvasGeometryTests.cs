using System.Drawing;
using System.Numerics;
using Bit.Bmage.Analysis;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class CanvasSizeTests
{
    [TestMethod]
    public void GrowingCentresTheImageAndFillsTheNewArea()
    {
        using var source = TestImages.Gradient(10, 6);
        var red = new Rgba32(255, 0, 0);
        using var output = new CanvasSizeOperation(20, 10, Anchor.Center, red).Apply(source, OperationContext.Default);

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
        using var output = ImagePipeline.Create().CanvasSize(8, 8, Anchor.TopLeft).Execute(source);
        Assert.AreEqual(Rgba32.Transparent, output[7, 7]);
        Assert.AreEqual(source[3, 3], output[3, 3]);
    }

    [TestMethod]
    public void ShrinkingCutsTheEdgesTheAnchorLeavesOut()
    {
        using var source = TestImages.Gradient(12, 9);
        foreach (var (anchor, origin) in new[] { (Anchor.TopLeft, new Point(0, 0)), (Anchor.BottomRight, new Point(7, 5)), (Anchor.Center, new Point(3, 2)) })
        {
            using var output = new CanvasSizeOperation(5, 4, anchor).Apply(source, OperationContext.Default);
            using var expected = new CropOperation(origin.X, origin.Y, 5, 4).Apply(source, OperationContext.Default);
            Assert.AreEqual(0, TestImages.MaxChannelDifference(expected, output), anchor.ToString());
        }
    }

    [TestMethod]
    public void MarginsAddAndCutPerSide()
    {
        using var source = TestImages.Gradient(10, 8);
        var white = Rgba32.White;
        using var output = CanvasSizeOperation.Margins(3, -2, 0, 4, white).Apply(source, OperationContext.Default);

        Assert.AreEqual(new Size(13, 10), output.Size);
        Assert.AreEqual(white, output[0, 0]);
        Assert.AreEqual(source[0, 2], output[3, 0], "the top two rows are cut");
        Assert.AreEqual(source[9, 7], output[12, 5]);
        Assert.AreEqual(white, output[12, 6], "four rows are added at the bottom");
    }

    [TestMethod]
    public void ZeroMarginsAreAnIdentityTheOptimiserDrops()
    {
        var pad = CanvasSizeOperation.Margins(0);
        Assert.IsTrue(pad.IsIdentity);
        Assert.AreEqual(0, ImagePipeline.Create().Apply(pad).Optimize(new Size(10, 10)).Count);
        Assert.IsFalse(new CanvasSizeOperation(10, 10).IsIdentity, "an absolute size depends on the input");
    }

    [TestMethod]
    public void MarginsThatLeaveNoCanvasAreRejectedBeforeAndDuringExecution()
    {
        var op = CanvasSizeOperation.Margins(-6, 0, -5, 0);
        Assert.ThrowsExactly<ArgumentException>(() => op.GetOutputSize(new Size(10, 10)));
        using var source = TestImages.Gradient(10, 10);
        Assert.ThrowsExactly<ArgumentException>(() => op.Apply(source, OperationContext.Default).Dispose());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CanvasSizeOperation(0, 10));
    }

    [TestMethod]
    public void TheTransformMapsSourcePixelsToWhereTheyLand()
    {
        var size = new Size(10, 6);
        foreach (var op in new[] { new CanvasSizeOperation(25, 9, Anchor.BottomRight), CanvasSizeOperation.Margins(4, 1, 0, 0), new CanvasSizeOperation(6, 4, Anchor.Center) })
        {
            using var source = TestImages.Gradient(size.Width, size.Height);
            using var output = op.Apply(source, OperationContext.Default);
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
        var absolute = (CanvasSizeOperation)new CanvasSizeOperation(1200, 800, Anchor.Top, Rgba32.White).ForScale(0.25);
        Assert.AreEqual(new Size(300, 200), absolute.Size);
        Assert.AreEqual(Anchor.Top, absolute.Anchor);
        Assert.AreEqual(Rgba32.White, absolute.Background);

        var margins = (CanvasSizeOperation)CanvasSizeOperation.Margins(40, -20, 10, 0).ForScale(0.5);
        Assert.AreEqual((20, -10, 5, 0), (margins.Left, margins.Top, margins.Right, margins.Bottom));
    }

    [TestMethod]
    public void TheSourceIsLeftAloneUnlessItMayBeMutated()
    {
        using var source = TestImages.Gradient(6, 6);
        var before = source.ToArray();
        using var output = CanvasSizeOperation.Margins(2).Apply(source, OperationContext.Default.WithMutation(false));
        CollectionAssert.AreEqual(before, source.ToArray());
        Assert.AreNotSame(source, output);
    }

    [TestMethod]
    public void APipelineEstimatesTheCanvasItWillProduce()
    {
        var pipeline = ImagePipeline.Create().Pad(10, Rgba32.White).CanvasSize(50, 50, Anchor.TopLeft);
        Assert.AreEqual(new Size(50, 50), pipeline.GetOutputSize(new Size(20, 30)));
        using var source = TestImages.Gradient(20, 30);
        using var output = pipeline.Execute(source);
        Assert.AreEqual(new Size(50, 50), output.Size);
        Assert.AreEqual(Rgba32.White, output[0, 0]);
        Assert.AreEqual(source[0, 0], output[10, 10]);
        Assert.AreEqual(Rgba32.Transparent, output[45, 45]);
    }
}

[TestClass]
public sealed class StraightenTests
{
    [TestMethod]
    public void ZeroDegreesChangesNothing()
    {
        var op = new StraightenOperation(0);
        Assert.IsTrue(op.IsIdentity);
        using var source = TestImages.Gradient(9, 7);
        using var output = op.Apply(source, OperationContext.Default.WithMutation(false));
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
            foreach (ref var p in source.GetRow(y)) p = new Rgba32(p.R, p.G, p.B, 255);

        foreach (var sampling in Enum.GetValues<WarpSampling>())
        {
            using var output = new StraightenOperation(degrees, sampling).Apply(source, OperationContext.Default);
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
            using var source = ImageBuffer.Create(width, height, new Rgba32(200, 120, 40));
            for (var degrees = -45.0; degrees <= 45; degrees += 0.75)
                foreach (var sampling in Enum.GetValues<WarpSampling>())
                {
                    using var output = new StraightenOperation(degrees, sampling).Apply(source, OperationContext.Default);
                    AssertOpaque(output, $"{sampling} at {degrees} degrees on {width}x{height}");
                }
        }
    }

    private static void AssertOpaque(ImageBuffer image, string what)
    {
        for (var y = 0; y < image.Height; y++)
            foreach (var p in image.GetRow(y))
                Assert.AreEqual(255, p.A, $"{what} left a translucent pixel on row {y}.");
    }

    [TestMethod]
    public void TheCropIsTheLargestRectangleOfTheSameAspectInsideTheRotatedImage()
    {
        var size = new Size(4000, 3000);
        var op = new StraightenOperation(5);
        var output = op.GetOutputSize(size);
        var k = StraightenOperation.GetCropScale(size, 5);
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
        var straighten = new StraightenOperation(-9);
        using var straightened = straighten.Apply(source, OperationContext.Default);
        using var rotated = new RotateOperation(-9, expandCanvas: false).Apply(source, OperationContext.Default);
        var x0 = (rotated.Width - straightened.Width) / 2;
        var y0 = (rotated.Height - straightened.Height) / 2;
        using var cropped = new CropOperation(x0, y0, straightened.Width, straightened.Height).Apply(rotated, OperationContext.Default);

        // Away from the border the two sample the same points of the same rotation, give or take the half pixel the
        // centred crop rounds by.
        using var inner = new CropOperation(4, 4, straightened.Width - 8, straightened.Height - 8).Apply(straightened, OperationContext.Default);
        using var innerExpected = new CropOperation(4, 4, straightened.Width - 8, straightened.Height - 8).Apply(cropped, OperationContext.Default);
        Assert.IsLessThan(6.0, TestImages.MeanChannelDifference(innerExpected, inner));
    }

    [TestMethod]
    public void TheProxyAndTheFullImageFrameTheSameCrop()
    {
        var op = new StraightenOperation(6.5);
        var full = op.GetOutputSize(new Size(6000, 4000));
        var proxy = op.ForScale(0.25).GetOutputSize(new Size(1500, 1000));
        Assert.IsLessThanOrEqualTo(1, Math.Abs(proxy.Width - full.Width / 4.0));
        Assert.IsLessThanOrEqualTo(1, Math.Abs(proxy.Height - full.Height / 4.0));
    }

    [TestMethod]
    public void AnglesBeyondFortyFiveDegreesAreRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new StraightenOperation(45.01));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new StraightenOperation(double.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new StraightenOperation(double.NegativeInfinity));
        _ = new StraightenOperation(-45);
    }

    [TestMethod]
    public void TheCentreStaysTheCentre()
    {
        var size = new Size(301, 201);
        var op = new StraightenOperation(17);
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
        Assert.ThrowsExactly<OperationCanceledException>(() => new StraightenOperation(4).Apply(source, OperationContext.Default.WithCancellation(cts.Token)).Dispose());
    }
}

[TestClass]
public sealed class ContentBoundsTests
{
    private static ImageBuffer Framed(int width, int height, Rectangle content, Rgba32 border, Rgba32 fill)
    {
        var image = ImageBuffer.Create(width, height, border);
        image.Fill(content, fill);
        return image;
    }

    [TestMethod]
    public void TransparentBordersAreFoundExactly()
    {
        using var image = Framed(40, 30, new Rectangle(7, 4, 20, 11), Rgba32.Transparent, new Rgba32(10, 200, 30));
        Assert.AreEqual(new Rectangle(7, 4, 20, 11), ContentBounds.Find(image));
    }

    [TestMethod]
    public void PaddingKeepsSomeBorderButNeverReachesOutsideTheImage()
    {
        using var image = Framed(40, 30, new Rectangle(2, 4, 20, 11), Rgba32.Transparent, Rgba32.Black);
        Assert.AreEqual(Rectangle.FromLTRB(0, 1, 25, 18), ContentBounds.Find(image, new TrimOptions { Padding = 3 }));
    }

    [TestMethod]
    public void AUniformBorderIsFoundWithinTheTolerance()
    {
        using var image = Framed(50, 50, new Rectangle(10, 12, 25, 20), Rgba32.White, new Rgba32(30, 60, 90));
        // JPEG-like noise in the border: within 4% it is still border.
        image[0, 49] = new Rgba32(250, 252, 249);
        image[49, 0] = new Rgba32(247, 255, 251);
        Assert.AreEqual(new Rectangle(10, 12, 25, 20), ContentBounds.Find(image));
        Assert.AreEqual(Rectangle.FromLTRB(0, 0, 50, 50), ContentBounds.Find(image, new TrimOptions { Tolerance = 0 }), "with no tolerance the noise is content");
    }

    [TestMethod]
    public void AnImageThatIsAllBackgroundHasNoContent()
    {
        using var image = ImageBuffer.Create(20, 10, Rgba32.White);
        Assert.IsNull(ContentBounds.Find(image));
        using var clear = ImageBuffer.Create(20, 10);
        Assert.IsNull(ContentBounds.Find(clear, new TrimOptions { Mode = TrimMode.Transparent }));
    }

    [TestMethod]
    public void AutoUsesTransparencyOnlyWhenACornerIsTransparent()
    {
        // An opaque white page with a translucent stamp in it: the page is the border, the stamp is content.
        using var page = Framed(30, 30, new Rectangle(5, 5, 10, 10), Rgba32.White, new Rgba32(0, 0, 0, 40));
        Assert.AreEqual(new Rectangle(5, 5, 10, 10), ContentBounds.Find(page));

        // A cut-out on transparency with a white subject: transparency is the border, the white is content.
        using var cutout = Framed(30, 30, new Rectangle(8, 3, 4, 20), Rgba32.Transparent, Rgba32.White);
        Assert.AreEqual(new Rectangle(8, 3, 4, 20), ContentBounds.Find(cutout));
    }

    [TestMethod]
    public void AnExplicitColourIsTheBorderWhateverTheCorners()
    {
        var green = new Rgba32(0, 255, 0);
        using var image = Framed(30, 20, new Rectangle(0, 0, 30, 20), green, green);
        image.Fill(new Rectangle(0, 0, 3, 3), Rgba32.Black);
        image.Fill(new Rectangle(20, 10, 5, 5), Rgba32.Black);
        Assert.AreEqual(Rectangle.FromLTRB(0, 0, 25, 15), ContentBounds.Find(image, new TrimOptions { Mode = TrimMode.Color, Color = green }));
    }

    [TestMethod]
    public void TheBoundsCropToTheContent()
    {
        using var image = Framed(64, 48, new Rectangle(13, 9, 30, 21), Rgba32.Transparent, new Rgba32(200, 100, 50));
        var bounds = ContentBounds.Find(image)!.Value;
        using var trimmed = ImagePipeline.Create().Crop(bounds).Execute(image);
        Assert.AreEqual(new Size(30, 21), trimmed.Size);
        Assert.IsFalse(trimmed.HasTransparency());
    }

    [TestMethod]
    public void InvalidOptionsAreRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TrimOptions { Padding = -1 });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TrimOptions { Tolerance = float.NaN });
        Assert.AreEqual(1f, new TrimOptions { Tolerance = 3 }.Tolerance);
    }
}
