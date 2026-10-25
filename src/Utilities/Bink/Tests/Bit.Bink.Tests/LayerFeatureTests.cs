using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>Layer blend modes and duplicating layers.</summary>
[TestClass]
public sealed class LayerFeatureTests
{
    private static BinkRectangleShape Box(BinkRect rect, BinkColor color) => new(rect) { Style = new BinkShapeStyle { FillColor = color, StrokeWidth = 0f } };

    /// <summary>A blue square below and a red square above on its own layer, overlapping in the middle.</summary>
    private static (BinkDocument Document, BinkLayer Top) Overlap(BinkBlendMode blend)
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 60, Height = 60, Background = BinkColor.White });
        document.AddElement(Box(new BinkRect(0, 0, 40, 40), new BinkColor(0, 0, 255)), document.Layers[0].Id);
        BinkLayer top = document.AddLayer(new BinkLayerProperties { Name = "Top", BlendMode = blend });
        document.AddElement(Box(new BinkRect(20, 20, 40, 40), new BinkColor(255, 0, 0)), top.Id);
        return (document, top);
    }

    [TestMethod]
    public void A_multiply_layer_darkens_what_is_below_it_and_only_there()
    {
        (BinkDocument normal, _) = Overlap(BinkBlendMode.Normal);
        (BinkDocument multiply, _) = Overlap(BinkBlendMode.Multiply);

        BinkRasterSurface plain = BinkPngExporter.Render(normal, BinkPngExportOptions.Default, out _, out _);
        BinkRasterSurface multiplied = BinkPngExporter.Render(multiply, BinkPngExportOptions.Default, out _, out _);

        Assert.AreEqual(new BinkColor(255, 0, 0), plain.GetPixel(30, 30), "Normal: red covers blue.");
        BinkColor overlap = multiplied.GetPixel(30, 30);
        Assert.IsLessThan(10, (int)overlap.R, "Red times blue has no red left.");
        Assert.IsLessThan(10, (int)overlap.B);
        Assert.AreEqual(new BinkColor(255, 0, 0), multiplied.GetPixel(50, 50), "Over the white page, multiplying red changes nothing.");
    }

    [TestMethod]
    public void The_blend_reaches_svg_and_survives_saving_and_synchronizing()
    {
        (BinkDocument document, BinkLayer top) = Overlap(BinkBlendMode.Screen);
        Assert.Contains("mix-blend-mode:screen", BinkSvgExporter.Export(document));

        BinkDocument loaded = BinkSerializer.Deserialize(BinkSerializer.Serialize(document));
        Assert.AreEqual(BinkBlendMode.Screen, loaded.GetLayer(top.Id).BlendMode);

        string wire = BinkOperationCodec.Serialize(new BinkUpdateLayerOperation(top.Id, top.Properties with { BlendMode = BinkBlendMode.Multiply }));
        var received = (BinkUpdateLayerOperation)BinkOperationCodec.Deserialize(wire);
        Assert.AreEqual(BinkBlendMode.Multiply, received.Properties.BlendMode);
    }

    [TestMethod]
    public void A_layer_cannot_erase_as_a_whole()
    {
        var document = new BinkDocument();
        BinkLayer layer = document.AddLayer(new BinkLayerProperties { BlendMode = BinkBlendMode.DestinationOut });
        Assert.AreEqual(BinkBlendMode.Normal, layer.BlendMode);
        document.UpdateLayer(layer.Id, p => p with { BlendMode = BinkBlendMode.DestinationOut, Opacity = float.NaN });
        Assert.AreEqual(BinkBlendMode.Normal, document.GetLayer(layer.Id).BlendMode);
        Assert.AreEqual(1f, document.GetLayer(layer.Id).Opacity);
    }

    [TestMethod]
    public void Duplicating_a_layer_copies_it_above_itself_as_one_undo_step()
    {
        var document = new BinkDocument();
        BinkLayer top = document.AddLayer(new BinkLayerProperties { Name = "Ink", Opacity = 0.5f, BlendMode = BinkBlendMode.Multiply });
        BinkStroke a = TestData.Stroke(Vector2.Zero, new Vector2(50, 50));
        BinkStroke b = TestData.Stroke(new Vector2(60, 0), new Vector2(90, 50));
        document.AddElement(a, top.Id);
        document.AddElement(b, top.Id);
        Guid group = document.GroupElements([a.Id, b.Id])!.Value;
        using var history = new BinkHistoryManager(document);

        BinkLayer copy = document.DuplicateLayer(top.Id);

        Assert.AreEqual(2, document.Layers.ToList().IndexOf(copy), "Directly above the original.");
        Assert.AreEqual("Ink copy", copy.Name);
        Assert.AreEqual(0.5f, copy.Opacity);
        Assert.AreEqual(BinkBlendMode.Multiply, copy.BlendMode);
        Assert.AreEqual(2, copy.Count);
        Assert.IsFalse(copy.Elements.Any(e => e.Id == a.Id || e.Id == b.Id), "The copies have new ids.");
        Guid copyGroup = copy.Elements[0].GroupIds.Single();
        Assert.AreNotEqual(group, copyGroup);
        Assert.AreEqual(copyGroup, copy.Elements[1].GroupIds.Single(), "The copies are grouped together, not with the originals.");
        Assert.AreEqual(1, history.UndoCount);

        Assert.IsTrue(history.Undo());
        Assert.AreEqual(2, document.Layers.Count);
        Assert.AreEqual(2, document.ElementCount);
    }
}
