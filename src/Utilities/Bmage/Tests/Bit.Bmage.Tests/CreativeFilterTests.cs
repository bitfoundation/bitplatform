using System.Drawing;
using System.Numerics;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Operations.Filters;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class RadialBlurTests
{
    /// <summary>Colour set by the angle around the centre alone: wedges, which a zoom streaks along and so leaves alone.</summary>
    private static ImageBuffer Wedges(int size, int count)
    {
        var image = ImageBuffer.Create(size, size);
        var c = size / 2f;
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var angle = MathF.Atan2(y + 0.5f - c, x + 0.5f - c) + MathF.PI;
                var wedge = (int)(angle / (2 * MathF.PI) * count) % count;
                image[x, y] = wedge % 2 == 0 ? new Rgba32(230, 40, 40) : new Rgba32(30, 30, 200);
            }
        return image;
    }

    /// <summary>Colour set by the distance from the centre alone: rings, which a spin streaks along and so leaves alone.</summary>
    private static ImageBuffer Rings(int size, float width)
    {
        var image = ImageBuffer.Create(size, size);
        var c = size / 2f;
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var d = MathF.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                image[x, y] = (int)(d / width) % 2 == 0 ? Rgba32.White : Rgba32.Black;
            }
        return image;
    }

    private static double Changed(ImageBuffer a, ImageBuffer b, int margin)
    {
        long sum = 0, count = 0;
        for (var y = margin; y < a.Height - margin; y++)
            for (var x = margin; x < a.Width - margin; x++)
            {
                sum += Math.Abs(a[x, y].R - b[x, y].R) + Math.Abs(a[x, y].B - b[x, y].B);
                count++;
            }
        return sum / (double)count;
    }

    [TestMethod]
    public void AZoomStreaksAcrossRingsButAlongWedges()
    {
        using var wedges = Wedges(200, 12);
        using var rings = Rings(200, 8);
        var zoom = new RadialBlurFilter(0.3f, RadialBlurMode.Zoom);
        using var zoomedWedges = zoom.Apply(wedges, OperationContext.Default);
        using var zoomedRings = zoom.Apply(rings, OperationContext.Default);
        // Only the wedges' edges, sampled between pixels, change at all.
        var alongWedges = Changed(wedges, zoomedWedges, 10);
        var acrossRings = Changed(rings, zoomedRings, 10);
        Assert.IsGreaterThan(4 * alongWedges, acrossRings, $"Wedges point at the centre, so a zoom runs along them ({alongWedges}); rings cross every streak ({acrossRings}).");
    }

    [TestMethod]
    public void ASpinStreaksAcrossWedgesButAlongRings()
    {
        using var wedges = Wedges(200, 12);
        using var rings = Rings(200, 8);
        var spin = new RadialBlurFilter(0.5f, RadialBlurMode.Spin);
        using var spunWedges = spin.Apply(wedges, OperationContext.Default);
        using var spunRings = spin.Apply(rings, OperationContext.Default);
        var alongRings = Changed(rings, spunRings, 10);
        var acrossWedges = Changed(wedges, spunWedges, 10);
        // The rings' stepped edges change a little wherever a sample falls between pixels, and each pass's bilinear samples
        // soften them a little more; across the wedges the change is still about twice that.
        Assert.IsGreaterThan(1.8 * alongRings, acrossWedges, $"A spin runs along the rings ({alongRings}) and crosses every wedge ({acrossWedges}).");
    }

    [TestMethod]
    public void TheCentreStaysSharpAndAPlainImageStaysPlain()
    {
        // Rings 12 pixels wide round the chosen centre: next to it a streak is under a pixel long and stays in its ring,
        // far from it a streak crosses several.
        using var rings = Rings(201, 12);
        using var zoomed = new RadialBlurFilter(0.5f).Apply(rings, OperationContext.Default);
        double Change(int cx, int cy)
        {
            long sum = 0;
            for (var y = cy - 1; y <= cy + 1; y++)
                for (var x = cx - 1; x <= cx + 1; x++) sum += Math.Abs(rings[x, y].R - zoomed[x, y].R);
            return sum / 9.0;
        }
        var centre = Change(100, 100);
        var edge = Change(190, 100);
        Assert.IsLessThan(edge / 10, centre, $"The centre stays sharp ({centre}); the far side does not ({edge}).");
        using var plain = ImageBuffer.Create(64, 48, new Rgba32(40, 120, 200, 180));
        foreach (var mode in new[] { RadialBlurMode.Zoom, RadialBlurMode.Spin })
        {
            using var blurred = new RadialBlurFilter(0.8f, mode).Apply(plain, OperationContext.Default);
            Assert.AreEqual(0, TestImages.MaxChannelDifference(plain, blurred), mode.ToString());
        }
    }

    [TestMethod]
    public void TransparencyDoesNotDarkenTheStreaks()
    {
        // Red on transparent black: averaging premultiplied keeps every visible pixel red, only fainter.
        using var image = ImageBuffer.Create(100, 100, Rgba32.Transparent);
        image.Fill(new Rectangle(60, 40, 30, 20), new Rgba32(255, 0, 0));
        using var blurred = new RadialBlurFilter(0.6f).Apply(image, OperationContext.Default);
        var visible = 0;
        foreach (var p in blurred.Pixels)
        {
            if (p.A < 8) continue;
            visible++;
            Assert.IsGreaterThan(245, (int)p.R, $"{p}");
        }
        Assert.IsGreaterThan(600, visible, "The streak spreads beyond the block.");
    }

    [TestMethod]
    public void TheSamePixelsComeOutOnOneThreadOrManyAndTheRegionLimitsIt()
    {
        using var noise = ParallelProcessingTests.Noise(160, 120, seed: 9);
        var filter = new RadialBlurFilter(0.4f, RadialBlurMode.Spin, region: new Rectangle(20, 10, 100, 80));
        using var one = filter.Apply(noise, OperationContext.Default);
        using var many = filter.Apply(noise, new OperationContext(maxDegreeOfParallelism: 4));
        Assert.AreEqual(0, TestImages.MaxChannelDifference(one, many));
        for (var y = 0; y < 120; y++)
            for (var x = 0; x < 160; x++)
                if (!filter.Region!.Value.Contains(x, y)) Assert.AreEqual(noise[x, y], one[x, y]);
    }

    [TestMethod]
    public void AProxyGivesWhatTheOriginalGivesReduced()
    {
        using var rings = Rings(400, 10);
        var filter = new RadialBlurFilter(0.3f);
        Assert.AreSame(filter, filter.ForScale(0.5), "Relative centre and amount need no scaling.");
        using var full = ImagePipeline.Create().Add(filter).Resize(100, 100).Execute(rings);
        using var proxy = ImagePipeline.Create().Resize(100, 100).Add(filter.ForScale(0.25)).Execute(rings);
        Assert.IsLessThan(12, TestImages.MeanChannelDifference(full, proxy));
    }

    [TestMethod]
    public void TheMemoryEstimateCountsTheWorkingCopies()
    {
        var size = new System.Drawing.Size(1000, 800);
        var plain = ImagePipeline.Create().Vibrance(0.2f).EstimateMemory(size).PeakBytes;
        var radial = ImagePipeline.Create().RadialBlur(0.2f).EstimateMemory(size).PeakBytes;
        var threshold = ImagePipeline.Create().AdaptiveThreshold().EstimateMemory(size).PeakBytes;
        Assert.IsGreaterThanOrEqualTo(plain + 16L * 1000 * 800, radial, "Two copies at 16 bits a channel.");
        Assert.IsGreaterThanOrEqualTo(plain + 1000L * 800, threshold, "A byte of luminance a pixel.");
    }

    [TestMethod]
    public void ArgumentsAreChecked()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RadialBlurFilter(-0.1f));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RadialBlurFilter(1.1f));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RadialBlurFilter(0.5f, (RadialBlurMode)5));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RadialBlurFilter(0.5f, center: new PointF(float.NaN, 0)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RadialBlurFilter(0.5f, center: new PointF(3, 0)));
        Assert.IsTrue(new RadialBlurFilter(0).IsIdentity);
        Assert.AreEqual("Spin blur", new RadialBlurFilter(0.2f, RadialBlurMode.Spin).Name);
    }
}

[TestClass]
public sealed class ColorLutTests
{
    [TestMethod]
    public void AnIdentityTableLeavesEveryPixelAlone()
    {
        using var noise = ParallelProcessingTests.Noise(120, 90, seed: 4);
        var identity = ColorLookupTable.Create(17, c => c);
        using var result = new ColorLutAdjustment(identity).Apply(noise, OperationContext.Default);
        Assert.IsLessThanOrEqualTo(1, TestImages.MaxChannelDifference(noise, result));
    }

    [TestMethod]
    public void ALinearMapIsReproducedExactlyBetweenEntries()
    {
        // Tetrahedral interpolation is exact for any map linear in the input, even with only two entries per axis.
        Vector3 Map(Vector3 c) => new(c.Z, 0.5f * c.X + 0.25f, 1 - c.Y);
        var table = ColorLookupTable.Create(2, Map);
        var random = new Random(3);
        for (var i = 0; i < 2000; i++)
        {
            var c = new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle());
            var expected = Map(c);
            var actual = table.Lookup(c);
            Assert.AreEqual(expected.X, actual.X, 1e-5);
            Assert.AreEqual(expected.Y, actual.Y, 1e-5);
            Assert.AreEqual(expected.Z, actual.Z, 1e-5);
        }
        // Inputs outside [0,1] are clamped onto the cube.
        Assert.AreEqual(0, Vector3.Distance(Map(new Vector3(1, 0, 0.5f)), table.Lookup(new Vector3(2, -1, 0.5f))), 1e-5);
    }

    [TestMethod]
    public void ACubeFileIsReadAndWrittenBack()
    {
        const string cube = """
            # A two-entry table that swaps red and blue
            TITLE "Swap"
            LUT_3D_SIZE 2

            0 0 0
            0 0 1
            0 1 0
            0 1 1
            1 0 0
            1 0 1
            1 1 0
            1 1 1
            """;
        var table = ColorLookupTable.ParseCube(cube);
        Assert.AreEqual(2, table.Size);
        Assert.AreEqual("Swap", table.Title);
        var looked = table.Lookup(new Vector3(0.9f, 0.5f, 0.2f));
        Assert.AreEqual(0.2f, looked.X, 1e-6);
        Assert.AreEqual(0.5f, looked.Y, 1e-6);
        Assert.AreEqual(0.9f, looked.Z, 1e-6);

        using var image = ImageBuffer.Create(2, 1, new Rgba32(200, 100, 10));
        using var swapped = ImagePipeline.Create().ColorLut(table).Execute(image);
        Assert.AreEqual(new Rgba32(10, 100, 200), swapped[0, 0]);
        using var half = ImagePipeline.Create().ColorLut(table, 0.5f).Execute(image);
        Assert.AreEqual(new Rgba32(105, 100, 105), half[0, 0]);

        var again = ColorLookupTable.ParseCube(table.ToCube());
        Assert.IsTrue(table.HasSameValues(again));
        Assert.AreEqual("Swap", again.Title);
    }

    [TestMethod]
    public void ADomainOtherThanZeroToOneIsFoldedIn()
    {
        // The table's domain is [0,2]: an input of 1 (the top of Bmage's range) is half way along each axis.
        var text = "LUT_3D_SIZE 3\nDOMAIN_MIN 0 0 0\nDOMAIN_MAX 2 2 2\n" + string.Concat(
            from b in Enumerable.Range(0, 3) from g in Enumerable.Range(0, 3) from r in Enumerable.Range(0, 3)
            select $"{r / 2.0} {g / 2.0} {b / 2.0}\n");
        var table = ColorLookupTable.ParseCube(text);
        var mid = table.Lookup(Vector3.One);
        Assert.AreEqual(0.5f, mid.X, 1e-5);
        Assert.AreEqual(0.25f, table.Lookup(new Vector3(0.5f, 0.5f, 0.5f)).Y, 1e-5);
    }

    [TestMethod]
    [DataRow("0 0 0\n", "before LUT_3D_SIZE")]
    [DataRow("LUT_3D_SIZE 1\n", "between")]
    [DataRow("LUT_3D_SIZE 99\n", "between")]
    [DataRow("LUT_1D_SIZE 16\n", "One-dimensional")]
    [DataRow("LUT_3D_SIZE 2\n0 0 0\n", "declares 8 entries but the text has 1")]
    [DataRow("LUT_3D_SIZE 2\n0 0\n", "three numbers")]
    [DataRow("LUT_3D_SIZE 2\n0 0 x\n", "three numbers")]
    [DataRow("LUT_3D_SIZE 2\n0 0 NaN\n", "three numbers")]
    [DataRow("TITLE \"no table\"\n", "no LUT_3D_SIZE")]
    [DataRow("LUT_3D_SIZE 2\nDOMAIN_MIN 1 0 0\nDOMAIN_MAX 1 1 1\n0 0 0\n0 0 1\n0 1 0\n0 1 1\n1 0 0\n1 0 1\n1 1 0\n1 1 1\n", "DOMAIN_MIN must be below")]
    public void AMalformedCubeIsRefusedWithAReason(string text, string reason)
    {
        var error = Assert.ThrowsExactly<FormatException>(() => ColorLookupTable.ParseCube(text));
        StringAssert.Contains(error.Message, reason);
    }

    [TestMethod]
    public void TooManyEntriesAreRefusedOnTheirLine()
    {
        var text = "LUT_3D_SIZE 2\n" + string.Concat(Enumerable.Repeat("0 0 0\n", 9));
        var error = Assert.ThrowsExactly<FormatException>(() => ColorLookupTable.ParseCube(text));
        StringAssert.Contains(error.Message, "Line 10");
    }

    [TestMethod]
    public void TheSamePixelsComeOutOnOneThreadOrManyAndAfterFusing()
    {
        using var noise = ParallelProcessingTests.Noise(300, 200, seed: 6);
        var table = ColorLookupTable.Create(9, c => new Vector3(MathF.Sqrt(c.X), c.Y * c.Y, 1 - c.Z));
        var pipeline = ImagePipeline.Create().Brightness(0.1f).ColorLut(table, 0.8f).Contrast(0.2f);
        using var one = pipeline.Execute(noise);
        using var many = pipeline.Execute(noise, new PipelineExecutionOptions { MaxDegreeOfParallelism = 4 });
        Assert.AreEqual(0, TestImages.MaxChannelDifference(one, many));
    }

    [TestMethod]
    public void ArgumentsAreChecked()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ColorLookupTable.Create(1, c => c));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ColorLookupTable.Create(66, c => c));
        Assert.ThrowsExactly<ArgumentException>(() => ColorLookupTable.Create(2, _ => new Vector3(float.NaN)));
        Assert.ThrowsExactly<ArgumentException>(() => ColorLookupTable.FromValues(2, new float[23]));
        Assert.ThrowsExactly<ArgumentNullException>(() => new ColorLutAdjustment(null!));
        Assert.IsTrue(new ColorLutAdjustment(ColorLookupTable.Create(2, c => c), 0).IsIdentity);
        Assert.AreEqual("LUT: Film", new ColorLutAdjustment(ColorLookupTable.Create(2, c => c, "Film")).Name);
    }
}
