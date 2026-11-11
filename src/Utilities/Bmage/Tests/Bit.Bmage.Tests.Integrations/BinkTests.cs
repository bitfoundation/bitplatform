using System.Drawing;
using Bit.Bink;
using Bit.Bmage.Bink;
using Bit.Bmage.Codecs;
using Bit.Bmage.Layers;

namespace Bit.Bmage.Tests.Integrations;

[TestClass]
public sealed class BinkTests
{
    /// <summary>Four coloured quadrants with a translucent band: every pixel distinct enough to show a shift.</summary>
    private static ImageBuffer Photo(int width = 120, int height = 80)
    {
        var image = ImageBuffer.Create(width, height);
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                image[x, y] = new Rgba32((byte)(x * 255 / width), (byte)(y * 255 / height), (byte)((x + y) % 256), (byte)(y < 10 ? 128 : 255));
        return image;
    }

    /// <summary>A filled red square on the drawing layer.</summary>
    private static void DrawSquare(BinkDocument document, BinkRect rect)
        => document.AddElement(new BinkRectangleShape(rect) { Style = BinkShapeStyle.Default with { FillColor = new BinkColor(255, 0, 0), StrokeWidth = 0 } });

    [TestMethod]
    public void ADocumentHoldsTheImageLockedBeneathALayerToDrawOn()
    {
        using var photo = Photo();
        var document = BmageBink.CreateDocument(photo);
        Assert.AreEqual(120f, document.Settings.Width);
        Assert.AreEqual(80f, document.Settings.Height);
        Assert.HasCount(2, document.Layers);
        Assert.AreEqual(BmageBink.ImageLayerName, document.Layers[0].Properties.Name);
        Assert.IsTrue(document.Layers[0].IsLocked);
        Assert.AreEqual("Drawing", document.Layers[1].Properties.Name);
        Assert.IsFalse(document.Layers[1].IsLocked);
        Assert.AreEqual(document.Layers[1].Id, document.ActiveLayerCandidate.Id, "New drawing lands above the image.");

        // Rendered back at one pixel a unit, the image is what went in.
        using var rendered = document.RenderToImage().Image;
        Assert.AreEqual(photo.Size, rendered.Size);
        Assert.IsLessThanOrEqualTo(1, BarcodeTests.MaxDifference(photo, rendered));
    }

    [TestMethod]
    public void FlatteningPutsTheDrawingOnTheImageAndLeavesTheRestAlone()
    {
        using var photo = Photo();
        var document = BmageBink.CreateDocument(photo);
        DrawSquare(document, new BinkRect(40, 30, 20, 20));
        using var flat = BmageBink.Flatten(photo, document);
        for (var y = 0; y < 80; y++)
            for (var x = 0; x < 120; x++)
            {
                if (x >= 41 && x < 59 && y >= 31 && y < 49) Assert.AreEqual(new Rgba32(255, 0, 0), flat[x, y], $"({x},{y}) is inside the square");
                else if (x < 39 || x >= 61 || y < 29 || y >= 51) Assert.AreEqual(photo[x, y], flat[x, y], $"({x},{y}) is outside it");
            }
    }

    [TestMethod]
    public void TheOverlayLinesUpWithACopyOfTheImageAtAnotherSize()
    {
        using var photo = Photo();
        var document = BmageBink.CreateDocument(photo);
        DrawSquare(document, new BinkRect(40, 30, 20, 20));
        using var overlay = document.RenderOverlay(new Size(240, 160)).Image;
        Assert.AreEqual(new Size(240, 160), overlay.Size);
        Assert.AreEqual(new Rgba32(255, 0, 0), overlay[100, 80], "The square, twice the size, in the middle of where it was.");
        Assert.AreEqual(0, overlay[20, 20].A, "No image layer and no background: only the drawing.");
        Assert.AreEqual(0, overlay[79, 59].A);
        Assert.ThrowsExactly<ArgumentException>(() => document.RenderOverlay(new Size(240, 80)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => document.RenderOverlay(Size.Empty));
    }

    [TestMethod]
    public void ADrawingBecomesALayerThatCompositesAsTheFlattenedImage()
    {
        using var photo = Photo();
        var document = BmageBink.CreateDocument(photo);
        DrawSquare(document, new BinkRect(10, 40, 30, 25));
        var layer = document.ToImageLayer(photo.Size, "Signature");
        try
        {
            Assert.AreEqual("Signature", layer.Name);
            Assert.AreEqual(new RectangleF(0, 0, 120, 80), layer.Bounds);
            using var composed = photo.Clone();
            LayerCompositor.Composite(composed, [layer]);
            using var flat = BmageBink.Flatten(photo, document);
            Assert.IsLessThanOrEqualTo(1, BarcodeTests.MaxDifference(flat, composed));
        }
        finally
        {
            layer.Image!.Dispose();
        }
    }

    [TestMethod]
    public void AnImageTooLargeForABinkSourceIsRefused()
    {
        // Noise does not compress: 3000 x 3000 is about 36 MB of PNG, over a data URI's 32 MB.
        using var noise = ImageBuffer.Create(3000, 3000);
        new Random(1).NextBytes(noise.Bytes);
        Assert.ThrowsExactly<ArgumentException>(() => noise.ToBinkImageSource());
        using var small = Photo(16, 16);
        var source = small.ToBinkImageSource();
        Assert.AreEqual(16, source.Width);
        Assert.AreEqual("image/png", source.MediaType);
        Assert.IsTrue(source.IsEmbedded);
    }

    [TestMethod]
    public void TextWithoutARasteriserIsReportedAsSkipped()
    {
        using var photo = Photo();
        var document = BmageBink.CreateDocument(photo);
        document.AddElement(new BinkTextElement(new System.Numerics.Vector2(10, 40), "Approved"));
        var rendered = document.RenderOverlay(photo.Size);
        using var _ = rendered.Image;
        Assert.AreEqual(1, rendered.SkippedTextElements);
        Assert.IsFalse(rendered.IsComplete);

        // The helpers that hand back only pixels refuse rather than pass off the drawing without its text as finished.
        var flatten = Assert.ThrowsExactly<InvalidOperationException>(() => BmageBink.Flatten(photo, document));
        StringAssert.Contains(flatten.Message, "1 text element");
        Assert.ThrowsExactly<InvalidOperationException>(() => document.ToImageLayer(photo.Size));
    }

    [TestMethod]
    public void TheImageLayerIsKnownByItsMarkNotItsName()
    {
        using var photo = Photo();
        var document = BmageBink.CreateDocument(photo);
        var image = document.Layers[0];
        document.UpdateLayer(image.Id, p => p with { Name = "Background", IsLocked = false });
        // A layer of the user's own, named and locked as the image's once was, is drawing like any other.
        var own = document.AddLayer(new BinkLayerProperties { Name = BmageBink.ImageLayerName });
        document.AddElement(new BinkRectangleShape(new BinkRect(40, 30, 20, 20)) { Style = BinkShapeStyle.Default with { FillColor = new BinkColor(255, 0, 0), StrokeWidth = 0 } }, own.Id);
        document.UpdateLayer(own.Id, p => p with { IsLocked = true });

        using var overlay = document.RenderOverlay(photo.Size).Image;
        Assert.AreEqual(0, overlay[5, 20].A, "The renamed, unlocked image layer is still left out.");
        Assert.AreEqual(new Rgba32(255, 0, 0), overlay[50, 40], "The layer named Image is drawn.");
        using var flat = BmageBink.Flatten(photo, document);
        Assert.AreEqual(photo[5, 20], flat[5, 20]);
        Assert.AreEqual(new Rgba32(255, 0, 0), flat[50, 40]);
    }

    [TestMethod]
    public void FlatteningAMultiplyLayerBlendsWithTheImageAsTheWholeDocumentDoes()
    {
        using var photo = Photo();
        var document = BmageBink.CreateDocument(photo);
        var drawing = document.Layers[1];
        document.UpdateLayer(drawing.Id, p => p with { BlendMode = BinkBlendMode.Multiply });
        document.AddElement(new BinkRectangleShape(new BinkRect(30, 20, 60, 50)) { Style = BinkShapeStyle.Default with { FillColor = new BinkColor(255, 200, 40), StrokeWidth = 0 } }, drawing.Id);

        using var flat = BmageBink.Flatten(photo, document);
        using var whole = document.RenderToImage().Image;
        Assert.IsLessThanOrEqualTo(2, BarcodeTests.MaxDifference(whole, flat));
        // Multiplied, the square darkens the image under it rather than covering it.
        Assert.AreNotEqual(new Rgba32(255, 200, 40), flat[60, 45]);
        Assert.IsLessThan((int)photo[60, 45].B, (int)flat[60, 45].B);
    }

    [TestMethod]
    public void AnIndexedPngOfFewColoursRendersInBink()
    {
        // Two, three and five colours are written at 1, 2 and 4 bits a pixel; an odd width leaves a part-filled byte.
        Rgba32[] colours = [new(250, 20, 20), new(20, 30, 240), new(10, 200, 60, 128), new(0, 0, 0), new(255, 255, 255)];
        foreach (var count in new[] { 2, 3, 5 })
        {
            using var few = ImageBuffer.Create(7, 5);
            for (var y = 0; y < 5; y++)
                for (var x = 0; x < 7; x++)
                    few[x, y] = colours[(x + 2 * y) % count];
            var source = few.ToBinkImageSource(ImageExportOptions.Png with { MaxColors = 16 });
            var document = new BinkDocument(BinkDocumentSettings.Default with { Width = 7, Height = 5, Background = BinkColor.Transparent });
            document.AddElement(new BinkImageElement(source, new BinkRect(0, 0, 7, 5)));
            var rendered = document.RenderToImage();
            using var _ = rendered.Image;
            Assert.IsTrue(rendered.IsComplete, $"{count} colours");
            Assert.IsLessThanOrEqualTo(1, BarcodeTests.MaxDifference(few, rendered.Image), $"{count} colours");
        }
    }
}
