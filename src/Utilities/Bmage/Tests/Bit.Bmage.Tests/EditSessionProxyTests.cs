using System.Drawing;
using Bit.Bmage.Editing;

namespace Bit.Bmage.Tests;

/// <summary>
/// The editor hands a session the browser's resampler for building its working proxy on WebAssembly. The session must
/// use it once, fall back to its own resampler when it cannot help, and never keep a proxy built for an old source.
/// </summary>
[TestClass]
public sealed class EditSessionProxyTests
{
    [TestMethod]
    public async Task TheProxyScalerBuildsTheProxyOnce()
    {
        var requests = new List<Size>();
        using var session = new ImageEditSession(FastPathEquivalenceTests.Noise(1000, 500, 61), new ImageEditSessionOptions
        {
            MaxProxyDimension = 100,
            ProxyScaler = (_, size, _) =>
            {
                requests.Add(size);
                return ValueTask.FromResult<ImageBuffer?>(ImageBuffer.Create(size.Width, size.Height, new Rgba32(10, 20, 30)));
            },
        });

        await session.RenderPreviewAsync();
        session.AddOperation(new Operations.Adjustments.BrightnessAdjustment(0f));
        var preview = await session.RenderPreviewAsync();

        CollectionAssert.AreEqual(new[] { new Size(100, 50) }, requests);
        Assert.AreEqual(new Size(100, 50), preview.Size);
        Assert.AreEqual(new Rgba32(10, 20, 30), preview[50, 25]);
    }

    [TestMethod]
    public async Task TheManagedResamplerTakesOverWhenTheScalerCannotHelp()
    {
        using var reference = new ImageEditSession(FastPathEquivalenceTests.Noise(1000, 500, 67), new ImageEditSessionOptions { MaxProxyDimension = 100 });
        var expected = (await reference.RenderPreviewAsync()).Clone();
        var wrongSize = ImageBuffer.Create(10, 10);

        foreach (var scaled in new ImageBuffer?[] { null, wrongSize })
        {
            using var session = new ImageEditSession(FastPathEquivalenceTests.Noise(1000, 500, 67), new ImageEditSessionOptions
            {
                MaxProxyDimension = 100,
                ProxyScaler = (_, _, _) => ValueTask.FromResult(scaled),
            });
            FastPathEquivalenceTests.AssertSamePixels(expected, await session.RenderPreviewAsync(), scaled is null ? "no proxy" : "wrong size");
        }

        Assert.IsTrue(wrongSize.IsDisposed);
        expected.Dispose();
    }

    [TestMethod]
    public async Task AProxyFinishedAfterTheSourceWasReplacedIsDropped()
    {
        var pending = new TaskCompletionSource<ImageBuffer?>();
        var sources = new List<ImageBuffer>();
        using var session = new ImageEditSession(FastPathEquivalenceTests.Noise(1000, 500, 71), new ImageEditSessionOptions
        {
            MaxProxyDimension = 100,
            ProxyScaler = (source, size, _) =>
            {
                sources.Add(source);
                return sources.Count == 1
                    ? new ValueTask<ImageBuffer?>(pending.Task)
                    : ValueTask.FromResult<ImageBuffer?>(ImageBuffer.Create(size.Width, size.Height, new Rgba32(1, 2, 3)));
            },
        });

        var render = session.RenderPreviewAsync().AsTask();
        var replacement = FastPathEquivalenceTests.Noise(800, 800, 73);
        session.ReplaceSource(replacement);
        var stale = ImageBuffer.Create(100, 50);
        pending.SetResult(stale);

        await Assert.ThrowsAsync<OperationCanceledException>(() => render);
        Assert.IsTrue(stale.IsDisposed);

        var preview = await session.RenderPreviewAsync();
        Assert.AreSame(replacement, sources[^1]);
        Assert.AreEqual(new Size(100, 100), preview.Size);
        Assert.AreEqual(new Rgba32(1, 2, 3), preview[0, 0]);
    }
}
