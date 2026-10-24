using System.Drawing;
using System.Numerics;
using Bit.Bmage.Masks;
using Bit.Bmage.Operations;
using Bit.Bmage.Pipeline;
using Bit.Bmage.Serialization;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class FloodMaskTests
{
    /// <summary>White, with a red square and, separately, a red bar: two regions of the same colour.</summary>
    private static ImageBuffer TwoRedRegions()
    {
        var image = ImageBuffer.Create(40, 30, Rgba32.White);
        image.Fill(new Rectangle(4, 4, 10, 10), new Rgba32(220, 20, 20));
        image.Fill(new Rectangle(25, 2, 6, 25), new Rgba32(210, 30, 25));
        return image;
    }

    private static int Covered(MaskBuffer mask, Rectangle area)
    {
        var count = 0;
        for (var y = area.Top; y < area.Bottom; y++)
            for (var x = area.Left; x < area.Right; x++)
                if (mask[x, y] == 255) count++;
        return count;
    }

    [TestMethod]
    public void TheWandSelectsOnlyTheConnectedRegion()
    {
        using var image = TwoRedRegions();
        using var mask = new FloodMask([new Vector2(8, 8)]) { Softness = 0 }.Render(image.Size, image);
        Assert.AreEqual(100, Covered(mask, new Rectangle(4, 4, 10, 10)), "the whole square");
        Assert.AreEqual(100, Covered(mask, image.Bounds), "and nothing else: the bar is not connected to it");
    }

    [TestMethod]
    public void WithoutContiguityEveryMatchingColourIsSelected()
    {
        using var image = TwoRedRegions();
        using var mask = new FloodMask([new Vector2(8, 8)]) { Contiguous = false, Softness = 0 }.Render(image.Size, image);
        Assert.AreEqual(100 + 150, Covered(mask, image.Bounds));
    }

    [TestMethod]
    public void SeveralSeedsUnionTheirRegions()
    {
        using var image = TwoRedRegions();
        using var mask = new FloodMask([new Vector2(8, 8), new Vector2(27, 20)]) { Softness = 0 }.Render(image.Size, image);
        Assert.AreEqual(250, Covered(mask, image.Bounds));
    }

    [TestMethod]
    public void ToleranceDecidesHowFarTheColourMayDrift()
    {
        // A horizontal ramp: the wider the tolerance, the further along it the selection reaches.
        using var ramp = ImageBuffer.Create(100, 4);
        for (var x = 0; x < 100; x++) for (var y = 0; y < 4; y++) ramp[x, y] = new Rgba32((byte)(x * 2.55), 0, 0);
        int Reach(float tolerance)
        {
            using var mask = new FloodMask([new Vector2(0, 0)]) { Tolerance = tolerance, Softness = 0 }.Render(ramp.Size, ramp);
            return Covered(mask, ramp.Bounds) / 4;
        }
        Assert.IsLessThan(Reach(0.2f), Reach(0.05f));
        Assert.IsLessThan(Reach(0.5f), Reach(0.2f));
        Assert.AreEqual(1, Reach(0), "no tolerance selects the seed's own colour only");
    }

    [TestMethod]
    public void TheEdgeIsSoftenedJustBeyondTheTolerance()
    {
        using var image = ImageBuffer.Create(20, 1, Rgba32.White);
        image[10, 0] = new Rgba32(242, 242, 242);
        image.Fill(new Rectangle(11, 0, 9, 1), Rgba32.Black);
        using var mask = new FloodMask([new Vector2(0, 0)]) { Tolerance = 0.02f, Softness = 0.1f }.Render(image.Size, image);
        Assert.AreEqual(255, mask[9, 0]);
        Assert.IsTrue(mask[10, 0] is > 0 and < 255, $"the near-white pixel at the edge is partly covered ({mask[10, 0]})");
        Assert.AreEqual(0, mask[11, 0]);
    }

    [TestMethod]
    public void SeedingOnTransparencySelectsTheTransparentAreaWhateverItsStoredColour()
    {
        using var image = ImageBuffer.Create(10, 10);
        // Transparent pixels storing different colours, around an opaque centre.
        for (var y = 0; y < 10; y++) for (var x = 0; x < 10; x++) image[x, y] = new Rgba32((byte)(x * 25), (byte)(y * 25), 99, 0);
        image.Fill(new Rectangle(3, 3, 4, 4), Rgba32.Black);
        using var mask = new FloodMask([new Vector2(0, 0)]) { Softness = 0 }.Render(image.Size, image);
        Assert.AreEqual(100 - 16, Covered(mask, image.Bounds));
    }

    [TestMethod]
    public void RelativeSeedsAtTheFarCornersLandOnTheLastPixels()
    {
        using var image = TwoRedRegions();
        using var mask = new FloodMask([new Vector2(1, 1)]) { Units = MaskUnits.Relative, Softness = 0 }.Render(image.Size, image);
        Assert.AreEqual(255, mask[39, 29]);
        Assert.AreEqual(0, mask[8, 8]);
    }

    [TestMethod]
    public void RemovingAPlainBackgroundLeavesTheSubjectOnTransparency()
    {
        using var photo = ImageBuffer.Create(60, 40, new Rgba32(250, 250, 248));
        photo.Fill(new Rectangle(20, 10, 20, 20), new Rgba32(30, 90, 160));
        using var cutout = ImagePipeline.Create().RemoveBackground(feather: 0).Execute(photo);
        Assert.AreEqual(0, cutout[0, 0].A);
        Assert.AreEqual(0, cutout[59, 39].A);
        Assert.AreEqual(255, cutout[30, 20].A);
        Assert.AreEqual(new Rgba32(30, 90, 160), cutout[25, 15]);
    }

    [TestMethod]
    public void SeedsScaleWithAProxyAndTheColoursComeFromIt()
    {
        var mask = new FloodMask([new Vector2(80, 40)]) { Feather = 2 };
        var scaled = (FloodMask)mask.ForScale(0.5);
        Assert.AreEqual(new Vector2(40, 20), scaled.Seeds[0]);
        Assert.AreEqual(1f, scaled.Feather);
        var relative = new FloodMask([new Vector2(0.5f, 0.5f)]) { Units = MaskUnits.Relative };
        Assert.AreEqual(new Vector2(0.5f, 0.5f), ((FloodMask)relative.ForScale(0.25)).Seeds[0]);
    }

    [TestMethod]
    public void AFloodNeedsAnImageAndASeed()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new FloodMask([]));
        Assert.ThrowsExactly<ArgumentException>(() => new FloodMask([new Vector2(float.NaN, 0)]));
        Assert.ThrowsExactly<InvalidOperationException>(() => new FloodMask([Vector2.Zero]).Render(new Size(4, 4)).Dispose());
    }

    [TestMethod]
    public void ALargeFloodIsCancellable()
    {
        using var image = ImageBuffer.Create(800, 600, Rgba32.White);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() =>
            new FloodMask([Vector2.Zero]).Render(image.Size, image, OperationContext.Default.WithCancellation(cts.Token)).Dispose());
    }
}

[TestClass]
public sealed class MorphedMaskTests
{
    private static readonly Size Canvas = new(60, 60);

    /// <summary>A hard-edged 20 pixel square in the middle of the canvas.</summary>
    private static Mask Square => new RectangleMask(new RectangleF(20, 20, 20, 20));

    [TestMethod]
    public void ExpandingGrowsEveryEdgeByTheDistance()
    {
        using var mask = Square.Expand(5).Render(Canvas);
        Assert.AreEqual(255, mask[15, 30], "five pixels left of the square");
        Assert.AreEqual(0, mask[13, 30]);
        Assert.AreEqual(255, mask[44, 30], "five pixels right of it");
        Assert.AreEqual(0, mask[46, 30]);
        // Corners round off: the diagonal reaches five pixels, not five along each axis.
        Assert.AreEqual(0, mask[15, 15], "the square's corner grows round");
        Assert.AreEqual(255, mask[17, 17]);
    }

    [TestMethod]
    public void ContractingShrinksEveryEdgeByTheDistance()
    {
        using var mask = Square.Contract(4).Render(Canvas);
        Assert.AreEqual(0, mask[22, 30]);
        Assert.AreEqual(255, mask[24, 30]);
        Assert.AreEqual(255, mask[35, 30]);
        Assert.AreEqual(0, mask[37, 30]);
        Assert.AreEqual(255, mask[30, 30]);
    }

    [TestMethod]
    public void TheNewEdgeIsAntiAliased()
    {
        using var mask = Square.Expand(2.5f).Render(Canvas);
        Assert.AreEqual(255, mask[18, 30]);
        Assert.IsTrue(mask[17, 30] is > 64 and < 192, $"half a pixel of coverage at the new edge ({mask[17, 30]})");
        Assert.AreEqual(0, mask[16, 30]);
    }

    [TestMethod]
    public void ABorderIsABandAlongTheEdge()
    {
        using var mask = Square.Border(6).Render(Canvas);
        Assert.AreEqual(255, mask[20, 30], "on the edge");
        Assert.AreEqual(255, mask[18, 30], "outside it by less than half the width");
        Assert.AreEqual(255, mask[21, 30], "inside it by less than half the width");
        Assert.AreEqual(0, mask[30, 30], "the middle is not part of the border");
        Assert.AreEqual(0, mask[10, 30], "nor is far outside");
    }

    [TestMethod]
    public void ZeroDistanceKeepsTheSourceAndTheImageEdgeIsNotAnEdge()
    {
        var soft = new EllipseMask(new RectangleF(5, 5, 40, 30)) { Feather = 2 };
        using var original = soft.Render(Canvas);
        using var same = soft.Expand(0).Render(Canvas);
        CollectionAssert.AreEqual(original.Values.ToArray(), same.Values.ToArray());

        // Everything selected: contracting does not shrink it away from the borders of the image.
        using var all = Mask.All().Contract(10).Render(Canvas);
        Assert.AreEqual(255, all[0, 0]);
        Assert.AreEqual(255, all[59, 59]);
    }

    [TestMethod]
    public void ContractingAWandSelectionTrimsItsHalo()
    {
        using var image = ImageBuffer.Create(50, 50, Rgba32.White);
        image.Fill(new Rectangle(10, 10, 30, 30), Rgba32.Black);
        var wand = new FloodMask([new Vector2(25, 25)]) { Softness = 0 };
        using var mask = wand.Contract(3).Render(image.Size, image);
        Assert.AreEqual(0, mask[11, 25]);
        Assert.AreEqual(255, mask[14, 25]);
    }

    [TestMethod]
    public void DistancesRescaleForAProxy()
    {
        var morphed = (MorphedMask)Square.Expand(8).ForScale(0.5);
        Assert.AreEqual(4f, morphed.Distance);
        Assert.AreEqual(new RectangleF(10, 10, 10, 10), ((RectangleMask)morphed.Source).Rectangle);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Square.Expand(-1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Square.Expand(MorphedMask.MaxDistance + 1));
    }

    [TestMethod]
    public void MorphedMasksRenderTheSameWindowsAsWholeImages()
    {
        // A masked edit draws only the window it changes; a morphed mask must give that window the whole image's values.
        using var image = ParallelProcessingTests.Noise(80, 60, seed: 6);
        var mask = new EllipseMask(new RectangleF(10, 10, 40, 30)).Expand(6).Union(new RectangleMask(new RectangleF(50, 5, 20, 20)).Border(4));
        using var whole = ImagePipeline.Create().Masked(mask, new Operations.Adjustments.InvertFilter()).Execute(image);
        using var reference = new Operations.Adjustments.InvertFilter().Apply(image, OperationContext.Default.WithMutation(false));
        using var coverage = mask.Render(image.Size, image);
        for (var y = 0; y < image.Height; y++)
            for (var x = 0; x < image.Width; x++)
            {
                if (coverage[x, y] == 255) Assert.AreEqual(reference[x, y], whole[x, y], $"{x},{y}");
                if (coverage[x, y] == 0) Assert.AreEqual(image[x, y], whole[x, y], $"{x},{y}");
            }
    }

    [TestMethod]
    public void AHostileDocumentCannotNestMorphedMasksWithoutLimit()
    {
        Mask mask = Square;
        for (var i = 0; i < 6; i++) mask = mask.Expand(1);
        var json = BmageJson.SerializeObject(mask);
        Assert.ThrowsExactly<ImageDocumentFormatException>(() => BmageJson.DeserializeObject<Mask>(json));

        var shallow = BmageJson.SerializeObject(Square.Expand(2).Border(3));
        Assert.IsInstanceOfType<MorphedMask>(BmageJson.DeserializeObject<Mask>(shallow));
    }
}
