using Bit.Bmage.Editing;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Operations.Filters;

namespace Bit.Bmage.Tests;

/// <summary>
/// Drafts are what the editor shows while a control is dragged and a full preview is too slow: the same document at
/// half the proxy's resolution, replaced by the full preview when the interaction ends.
/// </summary>
[TestClass]
public sealed class EditSessionDraftTests
{
    [TestMethod]
    public async Task ADraftRendersAtHalfTheProxyResolutionAndCountsAsStale()
    {
        using var session = new ImageEditSession(FastPathEquivalenceTests.Noise(3000, 2000, 5), new ImageEditSessionOptions { MaxProxyDimension = 2000 });
        Assert.AreEqual(session.ProxyScale / 2, session.DraftScale, 1e-3);
        session.AddOperation(new BlurFilter(4f));

        var draft = await session.RenderDraftAsync();
        Assert.AreEqual(new System.Drawing.Size(1000, 667), draft.Size);
        Assert.AreEqual(session.DraftScale, session.PreviewScale);
        Assert.IsTrue(session.IsPreviewStale, "a draft is not the preview");

        var full = await session.RenderPreviewAsync();
        Assert.AreEqual(new System.Drawing.Size(2000, 1333), full.Size);
        Assert.AreEqual(session.ProxyScale, session.PreviewScale);
        Assert.IsFalse(session.IsPreviewStale);
    }

    [TestMethod]
    public async Task DraftsKeepTheirOwnCachedSteps()
    {
        using var session = new ImageEditSession(FastPathEquivalenceTests.Noise(1600, 1200, 9), new ImageEditSessionOptions { MaxProxyDimension = 1600 });
        var counting = new CountingBarrier();
        session.AddOperation(counting);
        session.AddOperation(new BrightnessAdjustment(0.1f));

        for (var i = 1; i <= 3; i++)
        {
            session.ApplyTransient(d => d.ReplaceOperation(1, new BrightnessAdjustment(0.05f * i)));
            await session.RenderDraftAsync();
        }
        session.Commit("brightness");
        await session.RenderPreviewAsync();
        await session.RenderDraftAsync(d => d.AddAnnotation(new Annotations.RectangleAnnotation { Rectangle = new System.Drawing.RectangleF(1, 1, 9, 9) }));

        // Once for the draft resolution and once for the full one.
        Assert.AreEqual(2, counting.Applied);
    }

    [TestMethod]
    public async Task ASmallImageHasNoDraft()
    {
        using var session = new ImageEditSession(FastPathEquivalenceTests.Noise(800, 600, 2));
        Assert.AreEqual(session.ProxyScale, session.DraftScale);
        session.AddOperation(new BrightnessAdjustment(0.2f));
        var draft = await session.RenderDraftAsync();
        Assert.AreEqual(new System.Drawing.Size(800, 600), draft.Size);
        Assert.IsFalse(session.IsPreviewStale, "without a draft resolution, the draft is the full preview");
    }

    private sealed class CountingBarrier : ImageOperation
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
