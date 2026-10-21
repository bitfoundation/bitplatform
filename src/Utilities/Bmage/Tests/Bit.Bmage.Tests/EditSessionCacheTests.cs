using System.Drawing;
using System.Numerics;
using Bit.Bmage.Annotations;
using Bit.Bmage.Editing;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Operations.Filters;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Tests;

/// <summary>
/// An edit session reuses the pixels of earlier renders. Every preview it produces that way must equal what a fresh
/// session renders for the same document, and the reuse must really skip the work.
/// </summary>
[TestClass]
public sealed class EditSessionCacheTests
{
    [TestMethod]
    public async Task EveryCachedPreviewMatchesAFreshRender()
    {
        using var source = FastPathEquivalenceTests.Noise(300, 200, seed: 21);
        using var session = new ImageEditSession(source.Clone(), new ImageEditSessionOptions { MaxProxyDimension = 160 });
        var blur = new BlurFilter(3f);
        var rectangle = new RectangleAnnotation { Rectangle = new RectangleF(20, 30, 120, 60) };

        // A slider dragged after a blur, a crop and an orientation, annotations added and moved, steps undone: every kind
        // of change that keeps part of what the previous render computed.
        var edits = new Func<ImageDocument, ImageDocument>[]
        {
            d => d.AddOperation(new BrightnessAdjustment(0.1f)),
            d => d.AddOperation(blur),
            d => d.AddOperation(new ContrastAdjustment(0.3f)),
            d => d.ReplaceOperation(2, new ContrastAdjustment(0.5f)),
            d => d.ReplaceOperation(2, new ContrastAdjustment(-0.2f)),
            d => d.AddAnnotation(rectangle),
            d => d.UpdateAnnotation(rectangle.Translate(15, 5)),
            d => d.AddOperation(new CropOperation(10, 10, 250, 170)),
            d => d.AddOperation(OrientationOperation.Rotate90),
            d => d.AddOperation(new SharpenFilter(0.8f, 2f)),
            d => d.AddOperation(new SaturationAdjustment(0.4f)),
            d => d.ReplaceOperation(0, new BrightnessAdjustment(-0.1f)),
            d => d.ReplaceOperation(1, new BlurFilter(0.01f)),
            d => d.AddAnnotation(new EllipseAnnotation { Rectangle = new RectangleF(5, 5, 50, 50) }),
        };

        foreach (var edit in edits)
        {
            session.Apply(edit, "edit");
            await AssertMatchesFreshRender(source, session, session.Document);
        }
        for (var i = 0; i < 4; i++)
        {
            session.Undo();
            await AssertMatchesFreshRender(source, session, session.Document);
        }
    }

    [TestMethod]
    public async Task AShapeDrawnOverThePreviewMatchesRenderingTheDocumentWithIt()
    {
        using var source = FastPathEquivalenceTests.Noise(240, 180, seed: 8);
        using var session = new ImageEditSession(source.Clone(), new ImageEditSessionOptions { MaxProxyDimension = 120 });
        session.AddOperation(new BlurFilter(2f));
        session.AddAnnotation(new RectangleAnnotation { Rectangle = new RectangleF(10, 10, 80, 40) });
        await session.RenderPreviewAsync();

        for (var step = 1; step <= 5; step++)
        {
            var shape = new LineAnnotation { Start = new Vector2(5, 5), End = new Vector2(20 * step, 15 * step) };
            var preview = await session.RenderPreviewAsync(d => d.AddAnnotation(shape));
            using var expected = await FreshRender(source, session.Document.AddAnnotation(shape), 120);
            FastPathEquivalenceTests.AssertSamePixels(expected, preview, $"overlay step {step}");
        }

        // A redaction is drawn below everything else, so it cannot simply go on top of the kept preview.
        var redaction = new RedactionAnnotation { Rectangle = new RectangleF(0, 0, 60, 60) };
        var redacted = await session.RenderPreviewAsync(d => d.AddAnnotation(redaction));
        using var expectedRedaction = await FreshRender(source, session.Document.AddAnnotation(redaction), 120);
        FastPathEquivalenceTests.AssertSamePixels(expectedRedaction, redacted, "redaction overlay");
    }

    [TestMethod]
    public async Task WorkBeforeTheLastBarrierRunsOnceWhileLaterStepsAndAnnotationsChange()
    {
        using var source = TestImages.Gradient(200, 150);
        using var session = new ImageEditSession(source.Clone(), new ImageEditSessionOptions { MaxProxyDimension = 100 });
        var counting = new CountingOperation();
        session.AddOperation(counting);
        session.AddOperation(new BrightnessAdjustment(0.1f));
        await session.RenderPreviewAsync();
        Assert.AreEqual(1, counting.Applied);

        for (var i = 1; i <= 5; i++)
        {
            session.ApplyTransient(d => d.ReplaceOperation(1, new BrightnessAdjustment(0.05f * i)));
            await session.RenderPreviewAsync();
        }
        session.AddAnnotation(new RectangleAnnotation { Rectangle = new RectangleF(10, 10, 50, 50) });
        await session.RenderPreviewAsync();
        await session.RenderPreviewAsync(d => d.AddAnnotation(new LineAnnotation { Start = Vector2.Zero, End = new Vector2(40, 40) }));
        Assert.AreEqual(1, counting.Applied, "the barrier's output is reused");

        session.ApplyTransient(d => d.ReplaceOperation(0, new CountingOperation()));
        await session.RenderPreviewAsync();
        Assert.AreEqual(1, counting.Applied, "a different operation is not the cached one");
    }

    [TestMethod]
    public async Task InvalidatingMakesTheNextRenderRunTheWholeDocument()
    {
        using var source = TestImages.Gradient(200, 150);
        using var session = new ImageEditSession(source.Clone(), new ImageEditSessionOptions { MaxProxyDimension = 100 });
        var counting = new CountingOperation();
        session.AddOperation(counting);
        var first = await session.RenderPreviewAsync();
        Assert.AreSame(first, await session.RenderPreviewAsync(), "an unchanged document is not rendered again");
        Assert.AreEqual(1, counting.Applied);

        // Something outside the document, such as a font, changed what it renders to.
        session.Invalidate();
        Assert.IsTrue(session.IsPreviewStale);
        await session.RenderPreviewAsync();
        Assert.AreEqual(2, counting.Applied, "nothing from before the invalidation is reused");
        Assert.IsFalse(session.IsPreviewStale);
        await session.RenderPreviewAsync();
        Assert.AreEqual(2, counting.Applied, "the render after it is reused again");
    }

    [TestMethod]
    public async Task ANewSourceNeverReusesThePixelsOfTheOldOne()
    {
        using var first = TestImages.Gradient(120, 90);
        using var second = FastPathEquivalenceTests.Noise(120, 90, seed: 4);
        using var session = new ImageEditSession(first.Clone(), new ImageEditSessionOptions { MaxProxyDimension = 60 });
        var blur = new BlurFilter(2f);
        session.AddOperation(blur);
        await session.RenderPreviewAsync();

        // The same operation instance on another image must not hit the cache of the first.
        session.ReplaceSource(second.Clone());
        session.AddOperation(blur);
        var preview = await session.RenderPreviewAsync();
        using var expected = await FreshRender(second, session.Document, 60);
        FastPathEquivalenceTests.AssertSamePixels(expected, preview, "after ReplaceSource");
    }

    private static async Task AssertMatchesFreshRender(ImageBuffer source, ImageEditSession session, ImageDocument document)
    {
        var preview = await session.RenderPreviewAsync();
        using var expected = await FreshRender(source, document, session.MaxProxyDimension);
        FastPathEquivalenceTests.AssertSamePixels(expected, preview, string.Join(", ", document.Pipeline.Select(o => o.Name)));
    }

    /// <summary>A render with nothing cached: a new session given the document directly.</summary>
    private static async Task<ImageBuffer> FreshRender(ImageBuffer source, ImageDocument document, int maxProxyDimension)
    {
        using var fresh = new ImageEditSession(source.Clone(), new ImageEditSessionOptions { MaxProxyDimension = maxProxyDimension });
        fresh.Apply(_ => document, "same document");
        return (await fresh.RenderPreviewAsync()).Clone();
    }

    /// <summary>An operation the optimiser knows nothing about, which counts how often it runs.</summary>
    private sealed class CountingOperation : ImageOperation
    {
        public int Applied { get; private set; }
        public override string Name => "Counting";
        public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
        {
            Applied++;
            return GetMutableCopy(source, context);
        }
    }
}
