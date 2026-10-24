using System.Drawing;
using Bit.Bmage.Drawing;
using Bit.Bmage.Editing;
using Bit.Bmage.Layers;
using Bit.Bmage.Masks;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;
using Bit.Bmage.Serialization;

namespace Bit.Bmage.Tests;

/// <summary>
/// Layer groups composite their layers as if ungrouped, or on their own when isolated, and clipped layers show only
/// where their base does; the document's layer edits reach layers inside groups.
/// </summary>
[TestClass]
public sealed class LayerGroupTests
{
    private static readonly Rgba32 Red = new(255, 0, 0);
    private static readonly Rgba32 Blue = new(0, 0, 255);

    private static ImageBuffer Composite(ImageBuffer background, params Layer[] layers)
    {
        var target = background.Clone();
        LayerCompositor.Composite(target, layers);
        return target;
    }

    /// <summary>Something of everything: a placed image, a masked fill blended by multiplying, an edit of everything below.</summary>
    private static Layer[] Mixed(ImageBuffer stamp) =>
    [
        new ImageLayer { Image = stamp, Bounds = new RectangleF(8, 6, 30, 20) },
        new FillLayer(new Rgba32(20, 160, 90, 200)) { Mask = new EllipseMask(new RectangleF(20, 10, 30, 30)), BlendMode = BlendMode.Multiply },
        new AdjustmentLayer(ImagePipeline.Create().Brightness(0.2f)) { Mask = new RectangleMask(new RectangleF(0, 0, 32, 48)) },
    ];

    /// <summary>A disc of <paramref name="color"/> on transparency, the shape a clipping base usually has.</summary>
    private static ImageBuffer Disc(int size, Rgba32 color)
    {
        var image = ImageBuffer.Create(size, size);
        var r = size / 2f;
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                if ((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r) <= r * r) image[x, y] = color;
        return image;
    }

    private static void AssertNear(Rgba32 expected, Rgba32 actual, int tolerance, string? message = null)
    {
        var worst = Math.Max(Math.Max(Math.Abs(expected.R - actual.R), Math.Abs(expected.G - actual.G)), Math.Max(Math.Abs(expected.B - actual.B), Math.Abs(expected.A - actual.A)));
        Assert.IsLessThanOrEqualTo(tolerance, worst, $"{message} expected {expected}, got {actual}");
    }

    // ------------------------------------------------------------------ groups

    [TestMethod]
    public void APassThroughGroupAtFullStrengthIsItsLayersAsIfUngrouped()
    {
        using var background = ParallelProcessingTests.Noise(64, 48, seed: 4);
        using var stamp = ParallelProcessingTests.Noise(16, 16, seed: 5);
        using var flat = Composite(background, Mixed(stamp));
        using var grouped = Composite(background, new GroupLayer(Mixed(stamp)));
        Assert.AreEqual(0, TestImages.MaxChannelDifference(flat, grouped));
    }

    [TestMethod]
    public void FadingOrMaskingAPassThroughGroupFadesItsLayersTogether()
    {
        // Opaque, so fading is a plain average of before and after.
        using var background = AdversarialGeometryTests.Noise(64, 48, seed: 4);
        using var stamp = ParallelProcessingTests.Noise(16, 16, seed: 5);
        using var flat = Composite(background, Mixed(stamp));
        using var faded = Composite(background, new GroupLayer(Mixed(stamp)) { Opacity = 0.5f });
        using var masked = Composite(background, new GroupLayer(Mixed(stamp)) { Mask = new RectangleMask(new RectangleF(0, 0, 32, 48)) });
        for (var y = 0; y < 48; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                var below = background[x, y];
                var full = flat[x, y];
                AssertNear(new Rgba32((byte)((below.R + full.R + 1) / 2), (byte)((below.G + full.G + 1) / 2), (byte)((below.B + full.B + 1) / 2)), faded[x, y], 1, $"({x}, {y}) at half opacity:");
                Assert.AreEqual(x < 32 ? full : below, masked[x, y], $"({x}, {y}) through the mask");
            }
        }
    }

    [TestMethod]
    public void AnAdjustmentInsideAnIsolatedGroupEditsOnlyTheGroupsLayers()
    {
        using var background = ImageBuffer.Create(40, 20, new Rgba32(200, 100, 50));
        Layer[] layers =
        [
            new FillLayer(Blue) { Mask = new RectangleMask(new RectangleF(0, 0, 20, 20)) },
            new AdjustmentLayer(ImagePipeline.Create().Invert()),
        ];
        using var isolated = Composite(background, new GroupLayer(layers) { Isolated = true });
        using var through = Composite(background, new GroupLayer(layers));

        Assert.AreEqual(new Rgba32(255, 255, 0), isolated[5, 5], "the group's fill, inverted");
        Assert.AreEqual(new Rgba32(200, 100, 50), isolated[30, 5], "beneath the group, untouched");
        Assert.AreEqual(new Rgba32(255, 255, 0), through[5, 5]);
        Assert.AreEqual(new Rgba32(55, 155, 205), through[30, 5], "passed through, the edit reaches beneath the group");
    }

    [TestMethod]
    public void AGroupWithABlendModeBlendsItsLayersAsOneImage()
    {
        using var background = ImageBuffer.Create(50, 10, new Rgba32(200, 200, 200));
        var red = new FillLayer(Red) { Mask = new RectangleMask(new RectangleF(0, 0, 25, 10)) };
        var blue = new FillLayer(Blue) { Mask = new RectangleMask(new RectangleF(15, 0, 25, 10)) };
        using var group = Composite(background, new GroupLayer([red, blue]) { BlendMode = BlendMode.Multiply });
        using var each = Composite(background, red with { BlendMode = BlendMode.Multiply }, blue with { BlendMode = BlendMode.Multiply });

        // Within the group the blue covers the red, and only the result multiplies what is beneath.
        Assert.AreEqual(new Rgba32(200, 0, 0), group[5, 5]);
        Assert.AreEqual(new Rgba32(0, 0, 200), group[20, 5], "the overlap is the group's blue, multiplied once");
        Assert.AreEqual(new Rgba32(200, 200, 200), group[45, 5]);
        Assert.AreEqual(new Rgba32(0, 0, 0), each[20, 5], "where each layer multiplies on its own, the overlap goes black");
    }

    // ------------------------------------------------------------------ clipping

    [TestMethod]
    public void ALayerClippedToAnotherShowsOnlyWhereThatOneDoes()
    {
        using var background = ImageBuffer.Create(40, 40, Rgba32.White);
        using var disc = Disc(20, Blue);
        var logo = new ImageLayer { Image = disc, Bounds = new RectangleF(10, 10, 20, 20) };
        using var result = Composite(background, logo, new FillLayer(Red) { ClipToLayerBelow = true });

        Assert.AreEqual(Red, result[20, 20], "inside the disc, the fill");
        Assert.AreEqual(Rgba32.White, result[11, 11], "in the layer's square but outside its disc, nothing");
        Assert.AreEqual(Rgba32.White, result[2, 2], "outside the layer, nothing");
    }

    [TestMethod]
    public void LayersClippedToABaseFadeWithIt_AndAllBelongToTheNearestBaseBelow()
    {
        using var background = ImageBuffer.Create(40, 40, Rgba32.White);
        using var disc = Disc(20, Blue);
        var logo = new ImageLayer { Image = disc, Bounds = new RectangleF(10, 10, 20, 20), Opacity = 0.5f };
        var lower = new FillLayer(Red) { ClipToLayerBelow = true };
        var upper = new FillLayer(new Rgba32(0, 255, 0)) { ClipToLayerBelow = true, Mask = new RectangleMask(new RectangleF(20, 0, 20, 40)) };
        using var result = Composite(background, logo, lower, upper);

        AssertNear(new Rgba32(255, 128, 128), result[15, 20], 1, "the red fades with its base:");
        AssertNear(new Rgba32(128, 255, 128), result[25, 20], 1, "the green above it is clipped to the same base:");
        Assert.AreEqual(Rgba32.White, result[35, 35], "and limited to it");
    }

    [TestMethod]
    public void AnAdjustmentClippedToALayerEditsThatLayerAlone()
    {
        using var background = ImageBuffer.Create(40, 40, new Rgba32(200, 100, 50));
        using var disc = Disc(20, Blue);
        using var result = Composite(background,
            new ImageLayer { Image = disc, Bounds = new RectangleF(10, 10, 20, 20) },
            new AdjustmentLayer(ImagePipeline.Create().Invert()) { ClipToLayerBelow = true });

        Assert.AreEqual(new Rgba32(255, 255, 0), result[20, 20], "the disc, inverted");
        Assert.AreEqual(new Rgba32(200, 100, 50), result[11, 11], "beside it, untouched");
    }

    [TestMethod]
    public void AnEditClippedToALayerDoesNotPullInWhatOtherClippedLayersDrewBesideIt()
    {
        using var background = ImageBuffer.Create(40, 40, Rgba32.White);
        using var disc = Disc(20, Blue);
        using var result = Composite(background,
            new ImageLayer { Image = disc, Bounds = new RectangleF(10, 10, 20, 20) },
            new FillLayer(Red) { ClipToLayerBelow = true, Mask = new RectangleMask(new RectangleF(10, 10, 20, 20)) },
            // Drawn beside the disc, where clipping hides it, and within reach of the blur.
            new FillLayer(new Rgba32(0, 255, 0)) { ClipToLayerBelow = true, Mask = new RectangleMask(new RectangleF(0, 0, 10, 40)) },
            new AdjustmentLayer(ImagePipeline.Create().Blur(4)) { ClipToLayerBelow = true });

        var edge = result[10, 20];
        Assert.IsLessThan(40, edge.G, $"the disc's edge took in the green beside it: {edge}");
        Assert.IsGreaterThan(150, edge.R, $"and is still the red clipped to it: {edge}");
        Assert.AreEqual(Rgba32.White, result[4, 20], "beside the disc, nothing");
    }

    [TestMethod]
    public void ASoftBaseLimitsWhatIsClippedToItToItsTransparency()
    {
        using var background = ImageBuffer.Create(10, 10, Rgba32.White);
        using var result = Composite(background, new FillLayer(new Rgba32(0, 0, 255, 128)), new FillLayer(Red) { ClipToLayerBelow = true });
        AssertNear(new Rgba32(255, 127, 127), result[5, 5], 1, "red where the base is half there:");
    }

    [TestMethod]
    public void HidingTheBaseHidesWhatIsClippedToIt_AndTheLowestLayerHasNothingToClipTo()
    {
        using var background = ImageBuffer.Create(20, 20, Rgba32.White);
        using var disc = Disc(20, Blue);
        using var hidden = Composite(background,
            new ImageLayer { Image = disc, Bounds = new RectangleF(0, 0, 20, 20), IsVisible = false },
            new FillLayer(Red) { ClipToLayerBelow = true });
        Assert.AreEqual(0, TestImages.MaxChannelDifference(background, hidden));

        using var lowest = Composite(background, new FillLayer(Red) { ClipToLayerBelow = true });
        Assert.AreEqual(Red, lowest[0, 0]);
    }

    // ------------------------------------------------------------------ document edits

    [TestMethod]
    public void GroupingAndUngroupingNeitherChangeTheImageNorLoseALayer()
    {
        using var source = ParallelProcessingTests.Noise(64, 48, seed: 6);
        using var stamp = ParallelProcessingTests.Noise(16, 16, seed: 7);
        var layers = Mixed(stamp);
        var document = layers.Aggregate(ImageDocument.Create(source), (d, l) => d.AddLayer(l));

        var grouped = document.GroupLayers([layers[1].Id, layers[0].Id], out var groupId, "Pair");
        Assert.IsNotNull(groupId);
        Assert.HasCount(2, grouped.Layers);
        var group = (GroupLayer)grouped.Layers[0];
        Assert.AreEqual(groupId, group.Id);
        Assert.AreEqual("Pair", group.Name);
        CollectionAssert.AreEqual(new[] { layers[0].Id, layers[1].Id }, group.Layers.Select(l => l.Id).ToArray(), "in the order they were");
        var renderer = new DocumentRenderer();
        using (var before = renderer.Render(document, source))
        using (var after = renderer.Render(grouped, source))
            Assert.AreEqual(0, TestImages.MaxChannelDifference(before, after));

        var ungrouped = grouped.Ungroup(groupId.Value);
        CollectionAssert.AreEqual(layers.Select(l => l.Id).ToArray(), ungrouped.Layers.Select(l => l.Id).ToArray());
        Assert.AreSame(document, document.GroupLayers([Guid.NewGuid()], out var none));
        Assert.IsNull(none);
    }

    [TestMethod]
    public void LayersInsideGroupsAreFoundEditedMovedAndRemovedByTheirIds()
    {
        var a = new FillLayer(Red);
        var b = new FillLayer(Blue);
        var c = new FillLayer(Rgba32.White);
        var group = new GroupLayer([b, c]);
        var document = ImageDocument.Create(new Size(8, 8)).AddLayer(a).AddLayer(group);

        Assert.AreSame(c, document.FindLayer(c.Id));
        Assert.IsTrue(document.TryLocateLayer(c.Id, out var parent, out var index));
        Assert.AreEqual(group.Id, parent!.Id);
        Assert.AreEqual(1, index);
        Assert.IsTrue(document.TryLocateLayer(a.Id, out var none, out _));
        Assert.IsNull(none, "a top-level layer has no group");

        var faded = document.UpdateLayer(c with { Opacity = 0.5f });
        Assert.AreEqual(0.5f, faded.FindLayer(c.Id)!.Opacity);
        Assert.AreSame(a, faded.Layers[0]);
        var moved = document.MoveLayer(c.Id, 0);
        CollectionAssert.AreEqual(new[] { c.Id, b.Id }, ((GroupLayer)moved.Layers[1]).Layers.Select(l => l.Id).ToArray(), "moved within its group");
        var removed = document.RemoveLayer(b.Id);
        Assert.HasCount(1, ((GroupLayer)removed.Layers[1]).Layers);
        Assert.AreSame(document, document.RemoveLayer(Guid.NewGuid()));
    }

    [TestMethod]
    public void ADuplicatedGroupHoldsCopiesUnderNewIds()
    {
        var group = new GroupLayer([new FillLayer(Red), new GroupLayer([new FillLayer(Blue)])]);
        var document = ImageDocument.Create(new Size(8, 8)).AddLayer(group).DuplicateLayer(group.Id, out var copyId);

        static IEnumerable<Guid> Ids(IEnumerable<Layer> layers) => layers.SelectMany(l => l is GroupLayer g ? Ids(g.Layers).Prepend(l.Id) : [l.Id]);
        var ids = Ids(document.Layers).ToArray();
        Assert.HasCount(8, ids);
        Assert.HasCount(8, ids.Distinct(), "every layer of the copy has an id of its own");
        Assert.AreEqual(copyId, document.Layers[1].Id);
    }

    [TestMethod]
    public void LayersMoveIntoAndOutOfGroups()
    {
        var a = new FillLayer(Red);
        var b = new FillLayer(Blue);
        var c = new FillLayer(Rgba32.White);
        var inner = new GroupLayer([b]);
        var document = ImageDocument.Create(new Size(8, 8)).AddLayer(a).AddLayer(inner).AddLayer(c);

        var into = document.MoveLayerIntoGroup(c.Id, inner.Id);
        CollectionAssert.AreEqual(new[] { a.Id, inner.Id }, into.Layers.Select(l => l.Id).ToArray());
        CollectionAssert.AreEqual(new[] { b.Id, c.Id }, ((GroupLayer)into.Layers[1]).Layers.Select(l => l.Id).ToArray(), "on top of the group's layers");

        var outOf = into.MoveLayerOutOfGroup(b.Id);
        CollectionAssert.AreEqual(new[] { a.Id, inner.Id, b.Id }, outOf.Layers.Select(l => l.Id).ToArray(), "directly above the group");
        Assert.AreSame(document, document.MoveLayerOutOfGroup(a.Id), "a layer in no group stays");

        var nested = document.MoveLayerIntoGroup(a.Id, inner.Id).GroupLayers([inner.Id], out var outerId);
        Assert.AreSame(nested, nested.MoveLayerIntoGroup(outerId!.Value, inner.Id), "a group cannot go inside itself");
        Assert.ThrowsExactly<ArgumentException>(() => into.GroupLayers([a.Id, c.Id], out _), "nor can layers from different lists be grouped");
    }

    [TestMethod]
    public void GroupsNestOnlySoDeep()
    {
        Layer layer = new FillLayer(Red);
        for (var i = 0; i < GroupLayer.MaxDepth; i++) layer = new GroupLayer([layer]);
        Assert.AreEqual(GroupLayer.MaxDepth, ((GroupLayer)layer).Depth);
        Assert.ThrowsExactly<ArgumentException>(() => new GroupLayer([layer]));

        var deeper = $$"""{"type":"group","layers":[{{BmageJson.SerializeObject(layer)}}]}""";
        var error = Assert.ThrowsExactly<ImageDocumentFormatException>(() => BmageJson.DeserializeObject<Layer>(deeper));
        StringAssert.Contains(error.Message, "nest");
    }

    [TestMethod]
    public void AGroupsLayersMoveWithTheImageAndScaleForAPreview()
    {
        using var stamp = ParallelProcessingTests.Noise(16, 16, seed: 8);
        var placed = new ImageLayer { Image = stamp, Bounds = new RectangleF(10, 10, 20, 20) };
        var document = ImageDocument.Create(new Size(64, 48)).AddLayer(new GroupLayer([placed]));

        var cropped = document.AddOperation(new CropOperation(5, 5, 40, 30), keepOverlaysAttached: true);
        Assert.AreEqual(new RectangleF(5, 5, 20, 20), ((ImageLayer)cropped.FindLayer(placed.Id)!).Bounds);
        var half = (GroupLayer)document.Layers[0].ForScale(0.5);
        Assert.AreEqual(new RectangleF(5, 5, 10, 10), ((ImageLayer)half.Layers[0]).Bounds);
    }

    [TestMethod]
    public void TheHistoryCountsTheStrokesInsideGroups()
    {
        var paint = new PaintLayer([new PaintStroke([.. Enumerable.Range(0, 500).Select(i => new System.Numerics.Vector2(i, i))], 4, Red)]);
        var empty = new HistoryEntry(ImageDocument.Create(new Size(8, 8)).AddLayer(new GroupLayer([])), "group", DateTimeOffset.UnixEpoch);
        var painted = new HistoryEntry(ImageDocument.Create(new Size(8, 8)).AddLayer(new GroupLayer([paint])), "group", DateTimeOffset.UnixEpoch);
        Assert.IsGreaterThan(empty.EstimatedSize + 500 * 12, painted.EstimatedSize);
    }

    [TestMethod]
    public async Task ASessionPreviewsGroupsAsTheRendererRendersThem()
    {
        using var source = ParallelProcessingTests.Noise(64, 48, seed: 9);
        using var stamp = ParallelProcessingTests.Noise(16, 16, seed: 10);
        using var disc = Disc(24, Blue);
        var inner = new ImageLayer { Image = stamp, Bounds = new RectangleF(4, 4, 24, 24), Pipeline = ImagePipeline.Create().Grayscale() };
        var group = new GroupLayer(
        [
            inner,
            new ImageLayer { Image = disc, Bounds = new RectangleF(20, 10, 24, 24) },
            new FillLayer(Red) { ClipToLayerBelow = true, BlendMode = BlendMode.Multiply },
        ]) { Isolated = true, Opacity = 0.8f };
        using var session = new ImageEditSession(source.Clone());
        var renderer = new DocumentRenderer();

        session.Apply(d => d.AddLayer(group), "group");
        var preview = await session.RenderPreviewAsync();
        using (var expected = renderer.Render(session.Document, session.Original))
            Assert.AreEqual(0, TestImages.MaxChannelDifference(expected, preview));

        // Through the session's caches, after a layer inside the group changes.
        session.Apply(d => d.UpdateLayer(inner with { Opacity = 0.4f }), "fade");
        preview = await session.RenderPreviewAsync();
        using (var expected = renderer.Render(session.Document, session.Original))
            Assert.AreEqual(0, TestImages.MaxChannelDifference(expected, preview));
    }
}
