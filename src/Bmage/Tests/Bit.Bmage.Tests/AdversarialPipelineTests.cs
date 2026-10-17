using System.Drawing;
using Bit.Bmage.Codecs;
using Bit.Bmage.Editing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Tests;

/// <summary>
/// Differential tests over pipelines: the optimiser, proxy replay, history, sessions, cancellation and batching.
/// </summary>
[TestClass]
public sealed class AdversarialPipelineTests
{
    private static ImageBuffer Noise(int w, int h, int seed = 1) => AdversarialGeometryTests.Noise(w, h, seed);

    public static IEnumerable<object[]> PipelineNames() =>
        new[]
        {
            "resize+crop", "crop+crop", "orient+orient", "fit+cover", "max+stretch",
            "adjust-chain", "gamma+posterize", "matrix-chain", "resize+adjust+crop",
            "autoorient+cover+sharpen", "rotate+crop+flip", "tone-chain", "empty", "single",
        }.Select(name => new object[] { name });

    private static ImagePipeline Build(string name) => name switch
    {
        "resize+crop" => ImagePipeline.Create().Resize(40, 30, ResizeMode.Stretch).Crop(5, 5, 20, 15),
        "crop+crop" => ImagePipeline.Create().Crop(2, 2, 50, 40).Crop(3, 3, 20, 20),
        "orient+orient" => ImagePipeline.Create().Orient(Orientation.Rotate90).Orient(Orientation.FlipHorizontal),
        "fit+cover" => ImagePipeline.Create().Resize(50, 50, ResizeMode.Fit).Resize(20, 20, ResizeMode.Cover),
        "max+stretch" => ImagePipeline.Create().MaxSize(50, 50).Resize(20, 20, ResizeMode.Stretch),
        "adjust-chain" => ImagePipeline.Create().Brightness(0.1f).Contrast(0.2f).Saturation(-0.3f),
        "gamma+posterize" => ImagePipeline.Create().Gamma(1.4f).Posterize(8).Brightness(0.05f),
        "matrix-chain" => ImagePipeline.Create().Grayscale().Sepia(0.5f).Invert(0.25f),
        "resize+adjust+crop" => ImagePipeline.Create().Resize(30, 30, ResizeMode.Stretch).Brightness(0.2f).Crop(1, 1, 20, 20),
        "autoorient+cover+sharpen" => ImagePipeline.Create().AutoOrient().Resize(48, 36, ResizeMode.Cover).Sharpen(),
        "rotate+crop+flip" => ImagePipeline.Create().Rotate(90).Crop(0, 0, 30, 30).Orient(Orientation.FlipVertical),
        "tone-chain" => ImagePipeline.Create().Exposure(0.5f).Vibrance(0.3f).ShadowsHighlights(0.2f, -0.2f),
        "empty" => ImagePipeline.Empty,
        "single" => ImagePipeline.Create().Brightness(0.25f),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    /// <summary>The optimiser must never change the framing or the output size, whatever rule it applies.</summary>
    [TestMethod]
    [DynamicData(nameof(PipelineNames))]
    public void OptimiserPreservesFraming(string name)
    {
        var pipeline = Build(name);
        using var src = Noise(64, 48, 17);
        using var unoptimised = pipeline.Execute(src, new PipelineExecutionOptions { Optimize = false });
        using var optimised = pipeline.Execute(src, new PipelineExecutionOptions { Optimize = true });

        Assert.AreEqual(unoptimised.Size, optimised.Size);
        // The resize-elision rule deliberately removes an intermediate resample, so pixels may differ; the framing
        // must not. A mean difference this small cannot hide a shifted or differently cropped image.
        var mean = TestImages.MeanChannelDifference(unoptimised, optimised);
        Assert.IsTrue(mean <= 12, $"optimiser moved the image (mean difference {mean:0.0}) for [{pipeline}]");
    }

    /// <summary>
    /// The rules documented as bit-identical must be exactly that. These are the rules users rely on when an
    /// optimised and an unoptimised run have to agree byte for byte.
    /// </summary>
    [TestMethod]
    [DataRow("resize+crop")]
    [DataRow("crop+crop")]
    [DataRow("orient+orient")]
    [DataRow("rotate+crop+flip")]
    [DataRow("empty")]
    [DataRow("single")]
    public void OptimiserIsBitIdenticalForGeometricRules(string name)
    {
        var pipeline = Build(name);
        using var src = Noise(64, 48, 17);
        using var unoptimised = pipeline.Execute(src, new PipelineExecutionOptions { Optimize = false });
        using var optimised = pipeline.Execute(src, new PipelineExecutionOptions { Optimize = true });

        Assert.AreEqual(unoptimised.Size, optimised.Size);
        Assert.IsTrue(unoptimised.Bytes.SequenceEqual(optimised.Bytes),
            $"[{pipeline}] is documented as bit-identical but differs by {TestImages.MaxChannelDifference(unoptimised, optimised)}");
    }

    /// <summary>GetOutputSize must predict the real output size, before and after optimisation.</summary>
    [TestMethod]
    [DynamicData(nameof(PipelineNames))]
    public void PredictedOutputSizeMatchesReality(string name)
    {
        var pipeline = Build(name);
        using var src = Noise(64, 48, 19);
        using var result = pipeline.Execute(src, new PipelineExecutionOptions { Optimize = false });
        Assert.AreEqual(result.Size, pipeline.GetOutputSize(src.Size));
        Assert.AreEqual(result.Size, pipeline.Optimize(src.Size).GetOutputSize(src.Size));
    }

    /// <summary>Optimising must be idempotent: optimising an optimised pipeline changes nothing further.</summary>
    [TestMethod]
    [DynamicData(nameof(PipelineNames))]
    public void OptimiseIsIdempotent(string name)
    {
        var size = new Size(64, 48);
        var once = Build(name).Optimize(size);
        var twice = once.Optimize(size);
        Assert.AreEqual(once.Count, twice.Count);
        Assert.AreEqual(once.ToString(), twice.ToString());
    }

    /// <summary>
    /// A pipeline replayed on a half-size proxy must produce an image the size of the full result scaled by a half.
    /// This is what makes the editor preview trustworthy.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(PipelineNames))]
    public void ForScaleMatchesFullResolutionDownscaled(string name)
    {
        const double scale = 0.5;
        var pipeline = Build(name);
        using var src = Noise(64, 48, 23);
        using var full = pipeline.Execute(src, new PipelineExecutionOptions { Optimize = false });

        using var proxy = new ResizeOperation(32, 24, ResizeMode.Stretch).Apply(src, OperationContext.Default);
        using var preview = pipeline.ForScale(scale).Execute(proxy, new PipelineExecutionOptions { Optimize = false });

        var expectedW = Math.Max(1, (int)Math.Round(full.Width * scale));
        var expectedH = Math.Max(1, (int)Math.Round(full.Height * scale));
        Assert.IsTrue(Math.Abs(preview.Width - expectedW) <= 1 && Math.Abs(preview.Height - expectedH) <= 1,
            $"proxy preview {preview.Size} is not approximately {expectedW}x{expectedH} for [{pipeline}]");
    }

    /// <summary>
    /// Every way of putting an operation into a pipeline must reject null at the point of the mistake, not leave a
    /// hole that fails later with a NullReferenceException from inside execution.
    /// </summary>
    [TestMethod]
    public void NullOperationsAreRejectedWhereTheyAreIntroduced()
    {
        var pipeline = ImagePipeline.Create().Brightness(0.1f);

        Assert.ThrowsExactly<ArgumentNullException>(() => pipeline.Add(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => pipeline.Insert(0, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => pipeline.Replace(0, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => pipeline.AddRange(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => ImagePipeline.From(null!));

        Assert.ThrowsExactly<ArgumentException>(() => ImagePipeline.From([new BrightnessAdjustment(0.1f), null!]));
        Assert.ThrowsExactly<ArgumentException>(() => pipeline.AddRange([null!]));

        // The original pipeline must be untouched by any rejected call: it is an immutable value.
        Assert.ContainsSingle(pipeline);
    }

    [TestMethod]
    public void EmptyPipelineReturnsAnIndependentCopy()
    {
        using var src = Noise(8, 8, 3);
        using var result = ImagePipeline.Empty.Execute(src);
        Assert.IsFalse(ReferenceEquals(src, result));
        Assert.IsTrue(src.Bytes.SequenceEqual(result.Bytes));
        result[0, 0] = new Rgba32(1, 2, 3);
        Assert.AreNotEqual(result[0, 0], src[0, 0]);
    }

    /// <summary>A failure part way through a pipeline must not leak the partially processed buffer or the source.</summary>
    [TestMethod]
    public void FailureMidPipelineLeavesSourceIntactAndReportsTheStep()
    {
        using var src = Noise(16, 16, 5);
        var before = src.ToArray();
        var pipeline = ImagePipeline.Create()
            .Brightness(0.2f)
            .Apply(new ThrowingOperation())
            .Brightness(0.2f);

        var ex = Assert.ThrowsExactly<ImageException>(() => pipeline.Execute(src));
        Assert.Contains("Boom", ex.Message, StringComparison.Ordinal);
        Assert.IsTrue(before.AsSpan().SequenceEqual(src.Bytes), "the source image was modified by a failing pipeline");
    }

    private sealed class ThrowingOperation : ImageOperation
    {
        public override string Name => "Throwing";
        public override ImageBuffer Apply(ImageBuffer source, OperationContext context) => throw new InvalidOperationException("Boom");
    }

    /// <summary>A very long pipeline must terminate and stay correct.</summary>
    [TestMethod]
    public void VeryLongPipelineTerminates()
    {
        var pipeline = ImagePipeline.Empty;
        for (var i = 0; i < 500; i++) pipeline = pipeline.Brightness(0.001f);
        using var src = Noise(32, 32, 7);
        using var result = pipeline.Execute(src);
        Assert.AreEqual(src.Size, result.Size);
    }

    /// <summary>Random operation sequences must never corrupt state, hang or throw anything unexpected.</summary>
    [TestMethod]
    public void RandomPipelinesNeverThrowUnexpectedly()
    {
        var rnd = new Random(12345);
        for (var iteration = 0; iteration < 200; iteration++)
        {
            var pipeline = ImagePipeline.Empty;
            var steps = rnd.Next(1, 7);
            for (var s = 0; s < steps; s++)
            {
                pipeline = rnd.Next(10) switch
                {
                    0 => pipeline.Crop(rnd.Next(0, 30), rnd.Next(0, 30), rnd.Next(1, 40), rnd.Next(1, 40)),
                    1 => pipeline.Resize(rnd.Next(1, 80), rnd.Next(1, 80), (ResizeMode)rnd.Next(0, 6)),
                    2 => pipeline.Rotate(rnd.NextDouble() * 720 - 360),
                    3 => pipeline.Orient((Orientation)rnd.Next(1, 9)),
                    4 => pipeline.Brightness((float)(rnd.NextDouble() * 2 - 1)),
                    5 => pipeline.Contrast((float)(rnd.NextDouble() * 2 - 1)),
                    6 => pipeline.Gamma((float)(rnd.NextDouble() * 3 + 0.1)),
                    7 => pipeline.Blur((float)(rnd.NextDouble() * 5)),
                    8 => pipeline.Grayscale((float)rnd.NextDouble()),
                    _ => pipeline.Sharpen((float)rnd.NextDouble()),
                };
            }

            using var src = Noise(48, 36, iteration);
            ImageBuffer? result = null;
            try
            {
                result = pipeline.Execute(src);
                Assert.IsTrue(result.Width > 0 && result.Height > 0);
                Assert.AreEqual(result.Size, pipeline.GetOutputSize(src.Size));
            }
            catch (ImageLimitExceededException)
            {
                // Acceptable: a random resize can exceed the configured limits.
            }
            catch (Exception ex) when (ex is ArgumentException || ex.InnerException is ArgumentException)
            {
                // Acceptable: a random crop can fall entirely outside the image the previous step produced. What is
                // not acceptable is disagreement, so the size query must also report the pipeline as inapplicable
                // rather than the optimiser quietly substituting a region the caller never asked for.
                Assert.IsFalse(pipeline.TryGetOutputSize(src.Size, out var predicted),
                    $"[{pipeline}] failed at execution but the size query reported {predicted}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"pipeline [{pipeline}] threw {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                result?.Dispose();
            }
        }
    }

    // ------------------------------------------------------------------ history

    [TestMethod]
    public void HistoryRespectsMaxDepthAndKeepsTheOriginal()
    {
        var doc = ImageDocument.Create(new Size(10, 10));
        var history = new EditHistory(doc, maxDepth: 5);
        for (var i = 0; i < 50; i++)
            history.Push(doc.AddOperation(new BrightnessAdjustment(i / 100f)), $"step {i}");

        Assert.IsTrue(history.Entries.Count <= 5, $"history grew to {history.Entries.Count} entries with maxDepth 5");
        Assert.AreEqual("Original", history.Entries[0].Label);
        Assert.AreEqual(history.Entries.Count - 1, history.Position);
    }

    [TestMethod]
    public void RedoIsDiscardedAfterANewEdit()
    {
        var doc = ImageDocument.Create(new Size(10, 10));
        var history = new EditHistory(doc);
        history.Push(doc.AddOperation(OrientationOperation.Rotate90), "a");
        history.Push(doc.AddOperation(OrientationOperation.Rotate180), "b");
        history.Undo();
        Assert.IsTrue(history.CanRedo);
        history.Push(doc.AddOperation(OrientationOperation.FlipHorizontal), "c");
        Assert.IsFalse(history.CanRedo);
        Assert.AreEqual("c", history.CurrentEntry.Label);
    }

    [TestMethod]
    public void HistoryWithDepthOfOneStillWorks()
    {
        var doc = ImageDocument.Create(new Size(10, 10));
        var history = new EditHistory(doc, maxDepth: 1);
        history.Push(doc.AddOperation(OrientationOperation.Rotate90), "a");
        Assert.IsNotNull(history.Current);
        Assert.IsNotEmpty(history.Entries);
    }

    /// <summary>A thousand edits must stay in bounded memory, because documents describe edits rather than pixels.</summary>
    [TestMethod]
    public void ThousandEditsStayInBoundedMemory()
    {
        var doc = ImageDocument.Create(new Size(4000, 3000));
        var history = new EditHistory(doc, maxDepth: 100);
        for (var i = 0; i < 1000; i++)
            history.Push(history.Current.AddOperation(new BrightnessAdjustment(0.001f)), $"e{i}");
        Assert.IsTrue(history.EstimatedMemory < 10_000_000, $"history estimated at {history.EstimatedMemory} bytes");
        Assert.IsTrue(history.Entries.Count <= 100);
    }

    // ------------------------------------------------------------------ session

    [TestMethod]
    public async Task ConcurrentPreviewRendersDoNotCorruptTheSession()
    {
        using var session = new ImageEditSession(Noise(600, 400, 41), new ImageEditSessionOptions { MaxProxyDimension = 128 });
        session.AddOperation(new BrightnessAdjustment(0.1f));

        var failures = new List<Exception>();
        var tasks = new List<Task>();
        for (var i = 0; i < 24; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try { await session.RenderPreviewAsync(); }
                catch (OperationCanceledException) { /* expected: superseded by a newer render */ }
                catch (Exception ex) { lock (failures) failures.Add(ex); }
            }));
        }
        await Task.WhenAll(tasks);
        Assert.IsTrue(failures.Count == 0, $"concurrent previews threw: {string.Join("; ", failures.Select(f => f.GetType().Name + ": " + f.Message))}");

        var final = await session.RenderPreviewAsync();
        Assert.IsFalse(final.IsDisposed);
        Assert.IsFalse(session.IsPreviewStale);
    }

    /// <summary>Coordinate mapping must be valid immediately: the editor maps pointer events before the first render.</summary>
    [TestMethod]
    public async Task ProxyScaleIsCorrectBeforeTheFirstRender()
    {
        using var session = new ImageEditSession(Noise(1000, 500, 43), new ImageEditSessionOptions { MaxProxyDimension = 100 });
        Assert.IsTrue(session.UsesProxy, "UsesProxy is false before the first render for a 1000px image with a 100px proxy limit");
        Assert.AreEqual(0.1, session.ProxyScale, 0.5e-3);
        await session.RenderPreviewAsync();
        Assert.IsTrue(session.UsesProxy);
        Assert.AreEqual(0.1, session.ProxyScale, 0.5e-3);
    }

    [TestMethod]
    public async Task CancelledPreviewLeavesTheSessionUsable()
    {
        using var session = new ImageEditSession(Noise(400, 300, 47), new ImageEditSessionOptions { MaxProxyDimension = 200 });
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await session.RenderPreviewAsync(cts.Token));

        var preview = await session.RenderPreviewAsync();
        Assert.IsFalse(preview.IsDisposed);
    }

    /// <summary>
    /// Every pixel buffer rented during a full load/edit/export/dispose cycle must be returned. Counting rentals is
    /// the sound way to test this: a heap measurement cannot distinguish a leak from the array pool holding buffers
    /// it fully intends to hand back out.
    /// </summary>
    [TestMethod]
    public async Task RepeatedSessionLifecyclesReleaseEveryBuffer()
    {
        var allocator = new CountingAllocator();
        var processor = new ImageProcessor(ImageCodecRegistry.CreateDefault(), allocator);
        using (var seed = Noise(400, 400, 51))
        {
            var encoded = (await processor.EncodeAsync(seed, ImageExportOptions.Png)).Data;

            for (var i = 0; i < 40; i++)
            {
                using var decoded = await processor.DecodeAsync(encoded);
                using var session = new ImageEditSession(decoded.Clone(allocator), new ImageEditSessionOptions
                {
                    MaxProxyDimension = 200,
                    Allocator = allocator,
                });
                session.AddOperation(new BrightnessAdjustment(0.05f));
                session.AddOperation(new CropOperation(10, 10, 100, 100));
                await session.RenderPreviewAsync();
                using var full = await session.RenderFullAsync();
                _ = await processor.EncodeAsync(full, ImageExportOptions.Jpeg);
            }
        }

        Assert.IsTrue(allocator.Rented > 100, $"the probe only exercised {allocator.Rented} rentals");
        Assert.AreEqual(0, allocator.Outstanding);
        Assert.IsTrue(allocator.Peak < 20, $"peak concurrent buffers was {allocator.Peak}, so intermediates are accumulating within a cycle");
    }

    /// <summary>Counts rentals against returns so buffer leaks are visible independently of pool retention.</summary>
    private sealed class CountingAllocator : Bit.Bmage.Memory.IPixelAllocator
    {
        private readonly Bit.Bmage.Memory.IPixelAllocator _inner = Bit.Bmage.Memory.PooledPixelAllocator.Shared;
        internal int OutstandingCount;
        public int Rented;
        public int Peak;
        public int Outstanding => Volatile.Read(ref OutstandingCount);

        public System.Buffers.IMemoryOwner<byte> Rent(int byteCount)
        {
            Interlocked.Increment(ref Rented);
            var n = Interlocked.Increment(ref OutstandingCount);
            if (n > Peak) Peak = n;
            return new Owner(this, _inner.Rent(byteCount));
        }

        private sealed class Owner(CountingAllocator owner, System.Buffers.IMemoryOwner<byte> inner) : System.Buffers.IMemoryOwner<byte>
        {
            private int _disposed;
            public Memory<byte> Memory => inner.Memory;
            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
                Interlocked.Decrement(ref owner.OutstandingCount);
                inner.Dispose();
            }
        }
    }

    [TestMethod]
    public async Task DisposingASessionMidRenderDoesNotThrow()
    {
        var session = new ImageEditSession(Noise(800, 600, 53), new ImageEditSessionOptions { MaxProxyDimension = 400 });
        session.AddOperation(new Bit.Bmage.Operations.Filters.BlurFilter(8f));
        var render = Task.Run(async () =>
        {
            try { await session.RenderPreviewAsync(); }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
        });
        session.Dispose();
        await render;
        Assert.ThrowsExactly<ObjectDisposedException>(() => session.RenderPreviewAsync().GetAwaiter().GetResult());
    }

    // ------------------------------------------------------------------ cancellation

    [TestMethod]
    public async Task CancellationDuringAPipelineIsObserved()
    {
        using var src = Noise(1200, 900, 59);
        using var cts = new CancellationTokenSource();
        var pipeline = ImagePipeline.Create().Resize(600, 450, ResizeMode.Stretch, ResamplingFilter.Lanczos3).Blur(6f).Sharpen();
        await cts.CancelAsync();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            (await pipeline.ExecuteAsync(src, new PipelineExecutionOptions { CancellationToken = cts.Token })).Dispose());
    }

    /// <summary>Cancellation part way through must be observed by every long running operation.</summary>
    [TestMethod]
    [DataRow("resize")]
    [DataRow("blur")]
    [DataRow("rotate")]
    [DataRow("median")]
    [DataRow("adjust")]
    public async Task EachSlowOperationObservesCancellation(string kind)
    {
        using var src = Noise(1400, 1100, 61);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var op = kind switch
        {
            "resize" => (IImageOperation)new ResizeOperation(700, 550, ResizeMode.Stretch, ResamplingFilter.Lanczos3),
            "blur" => new Bit.Bmage.Operations.Filters.BlurFilter(10f),
            "rotate" => new RotateOperation(37),
            "median" => new Bit.Bmage.Operations.Filters.MedianFilter(2),
            _ => new BrightnessAdjustment(0.2f),
        };
        Assert.Throws<OperationCanceledException>(
            () => op.Apply(src, OperationContext.Default.WithCancellation(cts.Token)).Dispose());
    }

    // ------------------------------------------------------------------ encoding

    [TestMethod]
    public async Task ImpossibleSizeTargetFailsInsteadOfHanging()
    {
        var processor = new ImageProcessor();
        using var img = Noise(256, 256, 63);
        var options = ImageExportOptions.Jpeg with { MaxFileSize = 10, MaxFileSizeStrategy = MaxFileSizeStrategy.QualityOnly };
        await Assert.ThrowsExactlyAsync<ImageEncodeException>(async () => await processor.EncodeAsync(img, options));
    }

    /// <summary>DimensionsOnly must keep the requested quality; it is the whole point of the strategy.</summary>
    [TestMethod]
    public async Task DimensionsOnlyStrategyKeepsTheRequestedQuality()
    {
        var processor = new ImageProcessor();
        using var img = Noise(512, 512, 67);
        var options = ImageExportOptions.Jpeg with
        {
            Quality = 0.95,
            MinQuality = 0.2,
            MaxFileSize = 20_000,
            MaxFileSizeStrategy = MaxFileSizeStrategy.DimensionsOnly,
        };
        var result = await processor.EncodeAsync(img, options);
        Assert.IsTrue(result.Length <= 20_000, $"produced {result.Length} bytes");
        Assert.AreEqual(0.95, result.Quality!.Value, 0.5e-3);
    }

    [TestMethod]
    public async Task AchievableSizeTargetsAreMet()
    {
        var processor = new ImageProcessor();
        using var img = Noise(400, 400, 71);
        foreach (var target in new long[] { 100_000, 50_000, 20_000 })
        {
            var result = await processor.EncodeAsync(img, ImageExportOptions.Jpeg with { MaxFileSize = target });
            Assert.IsTrue(result.Length <= target, $"target {target} produced {result.Length} bytes");
        }
    }

    [TestMethod]
    public async Task MissingEncoderFailsWithACapabilityError()
    {
        var processor = new ImageProcessor();
        using var img = Noise(16, 16, 73);
        await Assert.ThrowsExactlyAsync<ImageCapabilityException>(async () => await processor.EncodeAsync(img, ImageExportOptions.Avif));
        await Assert.ThrowsExactlyAsync<ImageCapabilityException>(async () => await processor.EncodeAsync(img, ImageExportOptions.WebP));
    }

    // ------------------------------------------------------------------ batch

    [TestMethod]
    public async Task BatchSurvivesCorruptItems()
    {
        var processor = new ImageProcessor();
        using var good = Noise(64, 64, 75);
        var encoded = (await processor.EncodeAsync(good, ImageExportOptions.Png)).Data;

        var items = new List<BatchItem>();
        for (var i = 0; i < 12; i++)
            items.Add(i % 3 == 0 ? new BatchItem(new byte[] { 0xFF, 0xD8, 0xFF, 9, 9, 9 }, $"bad{i}") : new BatchItem(encoded, $"good{i}"));

        var result = await processor.ProcessBatchAsync(items, ImagePipeline.Create().MaxSize(32, 32),
            new BatchOptions { ExportOptions = ImageExportOptions.Png });

        Assert.AreEqual(12, result.Results.Count);
        Assert.AreEqual(8, result.SuccessCount);
        Assert.AreEqual(4, result.FailureCount);
        foreach (var r in result.Results)
        {
            Assert.IsTrue(r.IsSuccess || r.Error is not null || r.WasCancelled);
        }
    }

    [TestMethod]
    public async Task BatchReportsProgressForEveryItem()
    {
        var processor = new ImageProcessor();
        using var good = Noise(48, 48, 77);
        var encoded = (await processor.EncodeAsync(good, ImageExportOptions.Png)).Data;
        var items = Enumerable.Range(0, 20).Select(i => new BatchItem(encoded, $"i{i}")).ToList();

        var seen = 0;
        var progress = new Progress<BatchProgress>(_ => Interlocked.Increment(ref seen));
        var result = await processor.ProcessBatchAsync(items, ImagePipeline.Create().MaxSize(24, 24),
            new BatchOptions { ExportOptions = ImageExportOptions.Png, Progress = progress });

        Assert.AreEqual(20, result.SuccessCount);
        // Progress<T> posts asynchronously; allow it to drain.
        for (var i = 0; i < 50 && Volatile.Read(ref seen) < 20; i++) await Task.Delay(10);
        Assert.AreEqual(20, Volatile.Read(ref seen));
    }

    [TestMethod]
    public async Task BatchCancellationPropagates()
    {
        var processor = new ImageProcessor();
        using var good = Noise(256, 256, 79);
        var encoded = (await processor.EncodeAsync(good, ImageExportOptions.Png)).Data;
        var items = Enumerable.Range(0, 50).Select(i => new BatchItem(encoded, $"i{i}")).ToList();

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await processor.ProcessBatchAsync(items, ImagePipeline.Create().MaxSize(64, 64), null, cts.Token));
    }

    [TestMethod]
    public async Task BatchStopOnFirstErrorStopsEarlyButReturnsResults()
    {
        var processor = new ImageProcessor();
        using var good = Noise(64, 64, 81);
        var encoded = (await processor.EncodeAsync(good, ImageExportOptions.Png)).Data;
        var items = new List<BatchItem> { new(new byte[] { 0xFF, 0xD8, 0xFF, 1, 2, 3 }, "bad") };
        items.AddRange(Enumerable.Range(0, 30).Select(i => new BatchItem(encoded, $"g{i}")));

        var result = await processor.ProcessBatchAsync(items, ImagePipeline.Create().MaxSize(32, 32),
            new BatchOptions { StopOnFirstError = true, MaxConcurrency = 1, ExportOptions = ImageExportOptions.Png });

        Assert.AreEqual(31, result.Results.Count);
        Assert.IsTrue(result.FailureCount >= 1);
        foreach (var r in result.Results)
        {
            Assert.IsNotNull(r);
        }
    }

    // ------------------------------------------------------------------ decode robustness

    [TestMethod]
    public async Task DecodingGarbageThrowsImageDecodeException()
    {
        var processor = new ImageProcessor();
        await Assert.ThrowsExactlyAsync<ImageDecodeException>(async () => await processor.DecodeAsync(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }));
    }

    [TestMethod]
    public async Task DecodingEmptyDataThrowsImageDecodeException()
    {
        var processor = new ImageProcessor();
        await Assert.ThrowsExactlyAsync<ImageDecodeException>(async () => await processor.DecodeAsync(ReadOnlyMemory<byte>.Empty));
    }

    [TestMethod]
    [DataRow(ImageFormat.Png)]
    [DataRow(ImageFormat.Jpeg)]
    [DataRow(ImageFormat.Bmp)]
    public async Task TruncatedFilesFailCleanly(ImageFormat format)
    {
        var processor = new ImageProcessor();
        using var img = Noise(48, 48, 83);
        var encoded = await processor.EncodeAsync(img, new ImageExportOptions { Format = format });
        for (var cut = 4; cut < encoded.Data.Length; cut += Math.Max(1, encoded.Data.Length / 10))
        {
            var truncated = encoded.Data.AsMemory(0, cut);
            var ex = await Capture.ExceptionAsync(async () => (await processor.DecodeAsync(truncated)).Dispose());
            Assert.IsTrue(ex is null or ImageException,
                $"{format} truncated to {cut} of {encoded.Data.Length} bytes threw {ex?.GetType().Name}: {ex?.Message}");
        }
    }

    [TestMethod]
    [DataRow(ImageFormat.Png)]
    [DataRow(ImageFormat.Jpeg)]
    [DataRow(ImageFormat.Bmp)]
    public async Task CorruptedFilesFailCleanly(ImageFormat format)
    {
        var processor = new ImageProcessor();
        using var img = Noise(48, 48, 87);
        var encoded = await processor.EncodeAsync(img, new ImageExportOptions { Format = format });
        var rnd = new Random(4242);
        for (var trial = 0; trial < 60; trial++)
        {
            var copy = encoded.Data.ToArray();
            for (var flips = 0; flips < 6; flips++)
                copy[rnd.Next(copy.Length)] = (byte)rnd.Next(256);
            var ex = await Capture.ExceptionAsync(async () => (await processor.DecodeAsync(copy)).Dispose());
            Assert.IsTrue(ex is null or ImageException,
                $"{format} corruption trial {trial} threw {ex?.GetType().Name}: {ex?.Message}");
        }
    }

    [TestMethod]
    [DataRow(ImageFormat.Png)]
    [DataRow(ImageFormat.Jpeg)]
    [DataRow(ImageFormat.Bmp)]
    public async Task EncodeDecodeRoundTripPreservesDimensions(ImageFormat format)
    {
        var processor = new ImageProcessor();
        foreach (var (w, h) in new[] { (1, 1), (1, 17), (17, 1), (2, 2), (63, 65), (256, 129) })
        {
            using var src = Noise(w, h, w * 31 + h);
            var encoded = await processor.EncodeAsync(src, new ImageExportOptions { Format = format, Quality = 0.9 });
            Assert.AreEqual(format, encoded.Format);
            using var decoded = await processor.DecodeAsync(encoded.Data);
            Assert.AreEqual(new Size(w, h), decoded.Size);
        }
    }
}
