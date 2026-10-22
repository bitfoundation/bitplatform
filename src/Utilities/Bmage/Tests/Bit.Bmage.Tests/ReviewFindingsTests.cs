using System.Collections.Concurrent;
using System.Drawing;
using System.Numerics;
using Bit.Bmage.Codecs;
using Bit.Bmage.Drawing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Layers;
using Bit.Bmage.Masks;
using Bit.Bmage.Memory;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Tests;

/// <summary>
/// Defects a review of the masks, layers, batches and budgets found, each pinned by the scenario that showed it.
/// </summary>
[TestClass]
public sealed class ReviewFindingsTests
{
    [TestMethod]
    public void ABrushStrokeTurningSharplyIsNotClippedByItsBounds()
    {
        // The smoothing curve overshoots the corner at (400, 100) upwards, beyond the box around the points.
        var brush = new BrushMask([new BrushStroke([new(100, 100), new(400, 100), new(400, 400)], 20)]);
        var size = new Size(500, 500);
        using var rendered = brush.Render(size);

        Assert.IsTrue(brush.GetAffectedBounds(size).Contains(rendered.GetBounds()),
            $"The stroke reaches {rendered.GetBounds()}, beyond the affected bounds {brush.GetAffectedBounds(size)}.");

        using var source = ImageBuffer.Create(500, 500, new Rgba32(100, 100, 100));
        using var masked = new MaskedOperation(new BrightnessAdjustment(0.5f), brush).Apply(source, OperationContext.Default);
        using var plain = new BrightnessAdjustment(0.5f).Apply(source, OperationContext.Default);
        for (var y = 0; y < 500; y++)
            for (var x = 0; x < 500; x++)
                if (rendered[x, y] == 255) Assert.AreEqual(plain[x, y], masked[x, y], $"({x},{y}) is covered but was left unedited.");
    }

    [TestMethod]
    public void ARotatedCopyLayerReplacesOnlyItsOwnOutline()
    {
        using var stamp = ImageBuffer.Create(40, 40, new Rgba32(0, 0, 255));
        using var image = ImageBuffer.Create(100, 100, Rgba32.White);
        new ImageLayer { Image = stamp, Bounds = new RectangleF(30, 30, 40, 40), Rotation = 45, BlendMode = BlendMode.Copy }.Composite(image);

        Assert.AreEqual(new Rgba32(0, 0, 255), image[50, 50]);
        // The corner of the rotated layer's bounding box lies outside the rotated square.
        Assert.AreEqual(Rgba32.White, image[24, 24], "Copy cleared the empty corner around the rotated layer.");
    }

    [TestMethod]
    public void CopyAtPartialStrengthMovesTowardsTheSource()
    {
        var mixed = Compositor.Blend(new Rgba32(200, 200, 200, 255), Rgba32.Transparent, BlendMode.Copy, 0.5f);
        Assert.IsTrue(mixed.A is >= 126 and <= 129, $"{mixed}");

        // A transparent fill through a feathered mask punches a soft hole, not a hard one.
        using var image = ImageBuffer.Create(100, 100, Rgba32.White);
        new FillLayer(Rgba32.Transparent) { BlendMode = BlendMode.Copy, Mask = Mask.Ellipse() with { Feather = 8 } }.Composite(image);
        Assert.AreEqual(0, image[50, 50].A);
        Assert.IsTrue(Enumerable.Range(0, 50).Any(x => image[x, 50].A is > 0 and < 255), "The feathered edge left no partly cleared pixels.");
    }

    [TestMethod]
    public void AnAbsurdFeatherFinishesAtOnce()
    {
        var mask = new RectangleMask(new RectangleF(10, 10, 50, 50)) { Feather = 1e9f };
        var watch = System.Diagnostics.Stopwatch.StartNew();
        using var rendered = mask.Render(new Size(300, 200));
        Assert.IsLessThan(2000, watch.ElapsedMilliseconds, $"Feathering took {watch.ElapsedMilliseconds} ms.");
        Assert.IsTrue(rendered.Values.ToArray().All(v => v < 255), "A blur wider than the image should leave no pixel fully covered.");
    }

    [TestMethod]
    public void TheBudgetNeverRefusesARequestThatFits()
    {
        var budget = new BudgetedPixelAllocator(1_000_000);
        // Many threads asking near the limit at once: whatever is refused must really not have fitted.
        var refusals = new ConcurrentBag<ImageMemoryBudgetExceededException>();
        Parallel.For(0, 4_000, _ =>
        {
            try
            {
                using var owner = budget.Rent(300_000);
            }
            catch (ImageMemoryBudgetExceededException ex)
            {
                refusals.Add(ex);
            }
        });
        Assert.AreEqual(0, budget.CurrentBytes);
        foreach (var refusal in refusals)
            Assert.IsGreaterThan(1_000_000, refusal.OutstandingBytes + refusal.RequestedBytes, "A request that fitted was refused.");
    }

    [TestMethod]
    public async Task AStreamedBatchWaitsForASlowConsumer()
    {
        var processor = new ImageProcessor();
        using var image = ParallelProcessingTests.Noise(32, 32, seed: 1);
        var png = (await processor.EncodeAsync(image, ImageExportOptions.Png)).Data;
        var loaded = 0;
        var items = Enumerable.Range(0, 40).Select(i => BatchItem.Deferred(_ =>
        {
            Interlocked.Increment(ref loaded);
            return new ValueTask<ReadOnlyMemory<byte>>(png);
        }));

        var consumed = 0;
        var mostAhead = 0;
        await foreach (var _ in processor.ProcessBatchStreamAsync(items, ImagePipeline.Create(), new BatchOptions { MaxConcurrency = 2, ExportOptions = ImageExportOptions.Png }))
        {
            consumed++;
            mostAhead = Math.Max(mostAhead, Volatile.Read(ref loaded) - consumed);
            await Task.Delay(10);
        }

        Assert.AreEqual(40, consumed);
        // Two in flight, two finished and waiting to be taken: never the whole batch read ahead.
        Assert.IsLessThanOrEqualTo(6, mostAhead, $"{mostAhead} items were read ahead of a slow consumer.");
    }

    [TestMethod]
    public async Task ASourceTimingOutIsTheBatchsError()
    {
        var processor = new ImageProcessor();
        using var image = ParallelProcessingTests.Noise(16, 16, seed: 2);
        var png = (await processor.EncodeAsync(image, ImageExportOptions.Png)).Data;

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await foreach (var _ in processor.ProcessBatchStreamAsync(TimesOut(), ImagePipeline.Create())) { }
        });

        async IAsyncEnumerable<BatchItem> TimesOut()
        {
            yield return new BatchItem(png);
            await Task.Yield();
            // What an HttpClient timeout looks like: a cancellation nobody asked the batch for.
            throw new TaskCanceledException("The source timed out.");
        }
    }

    [TestMethod]
    public async Task AnUnboundedConcurrencyIsClampedRatherThanAllocated()
    {
        var processor = new ImageProcessor();
        using var image = ParallelProcessingTests.Noise(16, 16, seed: 3);
        var png = (await processor.EncodeAsync(image, ImageExportOptions.Png)).Data;

        var result = await processor.ProcessBatchAsync([new BatchItem(png), new BatchItem(png)], ImagePipeline.Create(), new BatchOptions { MaxConcurrency = int.MaxValue });

        Assert.AreEqual(2, result.SuccessCount);
        Assert.AreEqual(2, ImageProcessor.WorkerCount(int.MaxValue, 2));
        Assert.AreEqual(256, ImageProcessor.WorkerCount(int.MaxValue, -1));
        Assert.AreEqual(1, ImageProcessor.WorkerCount(0, 10));
    }

    [TestMethod]
    public void ProgressDoesNotGoBackwardsThroughALargeReduction()
    {
        using var source = ParallelProcessingTests.Noise(1600, 1200, seed: 4);
        var values = new ConcurrentQueue<double>();
        var progress = new Inline(values.Enqueue);
        // Shrinking 8x runs a box reduction, then the resample: both on several threads.
        using (new ResizeOperation(200, 150, ResizeMode.Stretch, ResamplingFilter.Lanczos3).Apply(source, new OperationContext(progress: progress, maxDegreeOfParallelism: 8))) { }

        var reported = values.ToArray();
        for (var i = 1; i < reported.Length; i++) Assert.IsGreaterThanOrEqualTo(reported[i - 1], reported[i], $"Progress went from {reported[i - 1]} to {reported[i]}.");
    }

    [TestMethod]
    public void AnImageLayerNeverDisposesTheCallersImage()
    {
        using var logo = ImageBuffer.Create(200, 200, new Rgba32(0, 120, 0));
        var layer = new ImageLayer { Image = logo, Bounds = new RectangleF(0, 0, 199.6f, 199.6f), Pipeline = ImagePipeline.Create().Grayscale() };
        using var target = ImageBuffer.Create(220, 220, Rgba32.White);

        LayerCompositor.Composite(target, [layer], new OperationContext(canMutateSource: true));
        LayerCompositor.Composite(target, [layer], new OperationContext(canMutateSource: true));

        Assert.IsFalse(logo.IsDisposed);
    }

    [TestMethod]
    public void AKernelWhoseSizeOverflowsIsRefused()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new Operations.Filters.ConvolutionFilter(new float[131073], 65537, 65537));
    }

    [TestMethod]
    public void TheEstimateCountsBothSidesOfACombinedMask()
    {
        var size = new Size(1000, 1000);
        var single = ImagePipeline.Create().Masked(Mask.Ellipse(), p => p.Brightness(0.1f)).EstimateMemory(size).PeakBytes;
        var combined = ImagePipeline.Create().Masked(Mask.Ellipse().Union(Mask.All()), p => p.Brightness(0.1f)).EstimateMemory(size).PeakBytes;

        Assert.AreEqual(2L * 1000 * 1000, combined - single);
    }

    private sealed class Inline(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }
}
