using System.Drawing;
using Bit.Bink;
using Bit.Bmage.Bink;
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
    }
}
