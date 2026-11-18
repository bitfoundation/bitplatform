using System.Collections.Concurrent;
using System.Drawing;

namespace Bit.Bmage.Tests;

/// <summary>
/// Memory is a design constraint: a budget is never exceeded, an estimate is close to what a run really allocates,
/// and a batch holds only what its images in flight need.
/// </summary>
[TestClass]
public sealed class MemoryBudgetTests
{
    // ------------------------------------------------------------------ the budgeted allocator

    [TestMethod]
    public void TheBudgetTracksWhatIsOutstanding_AndItsPeak()
    {
        var budget = new BmageBudgetedPixelAllocator(10_000);
        var a = budget.Rent(4_000);
        var b = budget.Rent(3_000);
        Assert.AreEqual(7_000, budget.CurrentBytes);
        a.Dispose();
        a.Dispose();
        Assert.AreEqual(3_000, budget.CurrentBytes, "Disposing twice returned the memory twice.");
        b.Dispose();
        Assert.AreEqual(0, budget.CurrentBytes);
        Assert.AreEqual(7_000, budget.PeakBytes);

        budget.ResetPeak();
        Assert.AreEqual(0, budget.PeakBytes);
    }

    [TestMethod]
    public void ARequestPastTheBudgetIsRefusedBeforeAnythingIsAllocated()
    {
        var budget = new BmageBudgetedPixelAllocator(1_000_000);
        using var held = BmageImageBuffer.Create(400, 400, budget);
        var refused = Assert.ThrowsExactly<BmageImageMemoryBudgetExceededException>(() => BmageImageBuffer.Create(400, 400, budget));

        Assert.AreEqual(640_000, refused.RequestedBytes);
        Assert.AreEqual(640_000, budget.CurrentBytes, "The refused request was left counted.");
    }

    [TestMethod]
    public void ManyThreadsShareOneBudget()
    {
        var budget = new BmageBudgetedPixelAllocator(long.MaxValue);
        Parallel.For(0, 2_000, _ =>
        {
            using var owner = budget.Rent(1_024);
        });
        Assert.AreEqual(0, budget.CurrentBytes);
        Assert.IsGreaterThanOrEqualTo(1_024, budget.PeakBytes);
    }

    [TestMethod]
    public void APipelineRunsWithinABudget_AndFailsCleanlyBelowIt()
    {
        using var source = ParallelProcessingTests.Noise(800, 600, seed: 4);
        var pipeline = BmageImagePipeline.Create().Rotate(10).MaxSize(500, 500).Blur(2);

        var roomy = new BmageBudgetedPixelAllocator(64L * 1024 * 1024);
        using (pipeline.Execute(source, new BmagePipelineExecutionOptions { Allocator = roomy })) { }
        Assert.AreEqual(0, roomy.CurrentBytes, "The run leaked pixel memory.");

        var tight = new BmageBudgetedPixelAllocator(1024 * 1024);
        Assert.ThrowsExactly<BmageImageMemoryBudgetExceededException>(() => pipeline.Execute(source, new BmagePipelineExecutionOptions { Allocator = tight }));
        Assert.AreEqual(0, tight.CurrentBytes, "A failed run left memory outstanding.");
    }

    // ------------------------------------------------------------------ estimates

    [TestMethod]
    public void TheEstimateCoversWhatARunAllocates_WithoutOverstatingIt()
    {
        using var source = ParallelProcessingTests.Noise(900, 700, seed: 5);
        foreach (var pipeline in new[]
        {
            BmageImagePipeline.Create().Brightness(0.1f).Contrast(0.2f),
            BmageImagePipeline.Create().AutoOrient().MaxSize(600, 600).Sharpen(0.3f),
            BmageImagePipeline.Create().Rotate(25).Crop(10, 10, 500, 400).Saturation(0.2f),
            BmageImagePipeline.Create().Masked(BmageMask.Ellipse(), p => p.Blur(3)).DenoiseMedian(1),
            BmageImagePipeline.Create().Cover(400, 400).ApplyMask(BmageMask.Ellipse()),
        })
        {
            var budget = new BmageBudgetedPixelAllocator(long.MaxValue);
            using (pipeline.Execute(source, new BmagePipelineExecutionOptions { Allocator = budget, MaxDegreeOfParallelism = 1 })) { }
            var estimate = pipeline.EstimateMemory(source.Size);

            Assert.IsGreaterThanOrEqualTo(budget.PeakBytes, estimate.PeakBytes, $"{pipeline}: estimated {estimate.PeakBytes}, measured {budget.PeakBytes}.");
            Assert.IsLessThanOrEqualTo(budget.PeakBytes * 2, estimate.PeakBytes, $"{pipeline}: estimated {estimate.PeakBytes}, measured {budget.PeakBytes}.");
        }
    }

    [TestMethod]
    public void TheEstimateReportsTheResultSize()
    {
        var estimate = BmageImagePipeline.Create().MaxSize(1920, 1920).EstimateMemory(new Size(4000, 3000));

        Assert.AreEqual(new Size(1920, 1440), estimate.OutputSize);
        Assert.AreEqual(4L * 1920 * 1440, estimate.OutputBytes);
    }

    // ------------------------------------------------------------------ batches

    [TestMethod]
    public async Task DeferredItemsAreReadOnlyWhenTheirTurnComes()
    {
        var processor = new BmageImageProcessor();
        var encoded = await Png(64, 48);
        var reading = 0;
        var mostAtOnce = 0;
        var items = Enumerable.Range(0, 12).Select(i => BmageBatchItem.Deferred(async ct =>
        {
            var now = Interlocked.Increment(ref reading);
            InterlockedMax(ref mostAtOnce, now);
            await Task.Delay(5, ct);
            Interlocked.Decrement(ref reading);
            return encoded;
        }, $"{i}.png")).ToList();

        var result = await processor.ProcessBatchAsync(items, BmageImagePipeline.Create().MaxSize(32, 32), new BmageBatchOptions { MaxConcurrency = 3, ExportOptions = BmageImageExportOptions.Png });

        Assert.AreEqual(12, result.SuccessCount);
        Assert.IsLessThanOrEqualTo(3, mostAtOnce, $"{mostAtOnce} files were being read at once with three workers.");
        Assert.AreEqual(12L * encoded.Length, result.TotalInputBytes);
        Assert.IsTrue(result.Results.All(r => r.Item.IsDeferred));
    }

    [TestMethod]
    public async Task StreamingHandsOverEveryResult_InTheOrderTheyFinish()
    {
        var processor = new BmageImageProcessor();
        var encoded = await Png(40, 40);
        var items = Enumerable.Range(0, 15).Select(i => new BmageBatchItem(encoded, $"{i}.png"));

        var names = new List<string?>();
        await foreach (var result in processor.ProcessBatchStreamAsync(items, BmageImagePipeline.Create().Grayscale(), new BmageBatchOptions { MaxConcurrency = 4, ExportOptions = BmageImageExportOptions.Png }))
        {
            Assert.IsTrue(result.IsSuccess, result.Error?.Message);
            names.Add(result.FileName);
        }

        CollectionAssert.AreEquivalent(Enumerable.Range(0, 15).Select(i => $"{i}.png").ToList(), names);
    }

    [TestMethod]
    public async Task StreamingFromAnAsynchronousSourceWorks_AndReportsAnUnknownTotal()
    {
        var processor = new BmageImageProcessor();
        var encoded = await Png(32, 32);
        var totals = new ConcurrentBag<int>();

        var count = 0;
        await foreach (var result in processor.ProcessBatchStreamAsync(Produce(), BmageImagePipeline.Create().Invert(),
            new BmageBatchOptions { ExportOptions = BmageImageExportOptions.Png, Progress = new Inline<BmageBatchProgress>(p => totals.Add(p.Total)) }))
        {
            Assert.IsTrue(result.IsSuccess);
            count++;
        }

        Assert.AreEqual(7, count);
        Assert.IsTrue(totals.All(t => t == -1));

        async IAsyncEnumerable<BmageBatchItem> Produce()
        {
            for (var i = 0; i < 7; i++)
            {
                await Task.Yield();
                yield return new BmageBatchItem(encoded, $"{i}");
            }
        }
    }

    [TestMethod]
    public async Task AFailingSourceEndsTheStreamWithItsError()
    {
        var processor = new BmageImageProcessor();
        var encoded = await Png(32, 32);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(async () =>
        {
            await foreach (var _ in processor.ProcessBatchStreamAsync(Broken(), BmageImagePipeline.Create())) { }
        });

        async IAsyncEnumerable<BmageBatchItem> Broken()
        {
            yield return new BmageBatchItem(encoded);
            await Task.Yield();
            throw new InvalidDataException("The source broke.");
        }
    }

    [TestMethod]
    public async Task StoppingEarlyStopsTheBatch()
    {
        var processor = new BmageImageProcessor();
        var encoded = await Png(64, 64);
        var loaded = 0;
        var items = Enumerable.Range(0, 100).Select(i => BmageBatchItem.Deferred(_ =>
        {
            Interlocked.Increment(ref loaded);
            return new ValueTask<ReadOnlyMemory<byte>>(encoded);
        }));

        await foreach (var _ in processor.ProcessBatchStreamAsync(items, BmageImagePipeline.Create(), new BmageBatchOptions { MaxConcurrency = 2 }))
            break;

        Assert.IsLessThan(10, Volatile.Read(ref loaded), "Breaking out of the stream left the batch running.");
    }

    [TestMethod]
    public async Task AMemoryBudgetLimitsTheImagesInFlight()
    {
        var processor = new BmageImageProcessor();
        // 400 x 300 decodes to 480 KB; with a pipeline buffer and an encoding allowance each image needs about 1.2 MB.
        var encoded = await Png(400, 300);
        var tracker = new ConcurrencyTracker();
        var pipeline = BmageImagePipeline.Create().Apply(tracker).Brightness(0.1f);
        var perImage = processor.EstimateBatchMemory(encoded, pipeline, BmageBatchOptions.Default);
        var items = Enumerable.Range(0, 12).Select(i => new BmageBatchItem(encoded, $"{i}")).ToList();

        var result = await processor.ProcessBatchAsync(items, pipeline, new BmageBatchOptions
        {
            MaxConcurrency = 8,
            MaxMemoryBytes = perImage * 2 + perImage / 2,
            ExportOptions = BmageImageExportOptions.Png,
        });

        Assert.AreEqual(12, result.SuccessCount);
        Assert.IsLessThanOrEqualTo(2, tracker.MostAtOnce, $"{tracker.MostAtOnce} images ran at once in a budget for two.");
    }

    [TestMethod]
    public async Task AnImageLargerThanTheWholeBudgetStillRunsAlone()
    {
        var processor = new BmageImageProcessor();
        var encoded = await Png(300, 300);
        var items = Enumerable.Range(0, 3).Select(i => new BmageBatchItem(encoded, $"{i}")).ToList();

        var result = await processor.ProcessBatchAsync(items, BmageImagePipeline.Create(), new BmageBatchOptions { MaxMemoryBytes = 1, MaxConcurrency = 3 });

        Assert.AreEqual(3, result.SuccessCount);
    }

    [TestMethod]
    public async Task TheMemoryGateAdmitsInOrder_AndReleasesWaitersOnCancellation()
    {
        var gate = new BmageMemoryGate(100);
        var first = await gate.AcquireAsync(60, CancellationToken.None);
        var second = gate.AcquireAsync(60, CancellationToken.None);
        var third = gate.AcquireAsync(10, CancellationToken.None);
        Assert.IsFalse(second.IsCompleted);
        Assert.IsFalse(third.IsCompleted, "A small request overtook a waiting large one.");

        gate.Release(first);
        Assert.AreEqual(60, await second);
        Assert.AreEqual(10, await third);
        Assert.AreEqual(70, gate.Used);

        using var cts = new CancellationTokenSource();
        var cancelled = gate.AcquireAsync(90, cts.Token);
        var behind = gate.AcquireAsync(20, CancellationToken.None);
        await cts.CancelAsync();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await cancelled);
        Assert.AreEqual(20, await behind, "Cancelling the head of the queue did not let the next one in.");
    }

    private static async Task<byte[]> Png(int width, int height)
    {
        using var image = ParallelProcessingTests.Noise(width, height, seed: width * 7 + height);
        return (await new BmageImageProcessor().EncodeAsync(image, BmageImageExportOptions.Png with { PngCompressionLevel = 1 })).Data;
    }

    private static void InterlockedMax(ref int target, int value)
    {
        var current = Volatile.Read(ref target);
        while (value > current)
        {
            var seen = Interlocked.CompareExchange(ref target, value, current);
            if (seen == current) return;
            current = seen;
        }
    }

    private sealed class Inline<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }

    /// <summary>An operation that records how many runs are inside it at once.</summary>
    private sealed class ConcurrencyTracker : BmageImageOperation
    {
        private int _inside;
        private int _most;
        public int MostAtOnce => Volatile.Read(ref _most);
        public override string Name => "Tracks concurrency";

        public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
        {
            InterlockedMax(ref _most, Interlocked.Increment(ref _inside));
            try
            {
                Thread.Sleep(15);
                return GetMutableCopy(source, context);
            }
            finally
            {
                Interlocked.Decrement(ref _inside);
            }
        }
    }
}
