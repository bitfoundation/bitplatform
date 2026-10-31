using System.Collections.Concurrent;
using System.Drawing;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bit.Bmage.Codecs;
using Bit.Bmage.Drawing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Interop;
using Bit.Bmage.Layers;
using Bit.Bmage.Masks;
using Bit.Bmage.Memory;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;
using Microsoft.JSInterop;

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
    public void TheEstimateCountsTheMaskACombinationHoldsBesideItsOwn()
    {
        var size = new Size(1000, 1000);
        var single = ImagePipeline.Create().Masked(Mask.Ellipse(), p => p.Brightness(0.1f)).EstimateMemory(size).PeakBytes;
        var combined = ImagePipeline.Create().Masked(Mask.Ellipse().Union(Mask.All()), p => p.Brightness(0.1f)).EstimateMemory(size).PeakBytes;

        // The first side is drawn into the combination's own mask, the second beside it.
        Assert.AreEqual(1L * 1000 * 1000, combined - single);
    }

    // ------------------------------------------------------------------ second review

    [TestMethod]
    public void AnImageLayerDrawsWithTheLimitsAllocatorAndCancellationOfItsContext()
    {
        using var logo = ParallelProcessingTests.Noise(100, 4, seed: 6);
        // Wider than the default limits allow: only the context the layer is composited with lifts them.
        using var target = ImageBuffer.Create(17_000, 4, allocator: null, clear: true, ImageLimits.Unrestricted);
        var budget = new BudgetedPixelAllocator(long.MaxValue);
        new ImageLayer { Image = logo, Bounds = new RectangleF(0, 0, 17_000, 4) }.Composite(target, new OperationContext(budget, ImageLimits.Unrestricted));

        // The resampled copy is the size of the area drawn, and counted against the budget.
        Assert.IsGreaterThanOrEqualTo(17_000L * 4 * 4, budget.PeakBytes);
        Assert.AreEqual(0, budget.CurrentBytes);

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        using var small = ImageBuffer.Create(200, 200, Rgba32.White);
        Assert.ThrowsExactly<OperationCanceledException>(() =>
            new ImageLayer { Image = logo, Bounds = new RectangleF(20, 20, 150, 150), Rotation = 30 }.Composite(small, new OperationContext(cancellationToken: cancelled.Token)));
    }

    [TestMethod]
    public void ALayerWhosePipelineCropsIsShrunkOnlyAsFarAsWhatTheCropLeaves()
    {
        using var photo = ParallelProcessingTests.Noise(800, 800, seed: 7);
        var crop = new Rectangle(200, 200, 100, 100);
        using var cropped = new CropOperation(crop).Apply(photo, OperationContext.Default);
        using var throughPipeline = ImageBuffer.Create(100, 100, Rgba32.White);
        using var direct = ImageBuffer.Create(100, 100, Rgba32.White);

        new ImageLayer { Image = photo, Bounds = new RectangleF(0, 0, 100, 100), Pipeline = ImagePipeline.Create().Crop(crop) }.Composite(throughPipeline);
        new ImageLayer { Image = cropped, Bounds = new RectangleF(0, 0, 100, 100) }.Composite(direct);

        CollectionAssert.AreEqual(direct.Pixels.ToArray(), throughPipeline.Pixels.ToArray(), "The cropped layer was shrunk before its crop and drawn blurred.");
    }

    [TestMethod]
    public async Task AModuleWithoutConfigureWorkersStillLoads_AndACancelledLoadReleasesIt()
    {
        var old = new FakeModule(identifier => { if (identifier == "configureWorkers") throw new JSException("configureWorkers is not a function"); });
        await using (var interop = new BrowserImageInterop(new ImportingJSRuntime(old), new BitBmageOptions { MaxCodecWorkers = 2 }))
        {
            Assert.AreSame(old, await interop.GetModuleAsync());
            Assert.IsTrue(interop.IsLoaded);
            Assert.IsFalse(old.Disposed);
        }

        using var cancel = new CancellationTokenSource();
        var module = new FakeModule(identifier =>
        {
            if (identifier != "configureWorkers") return;
            cancel.Cancel();
            throw new TaskCanceledException();
        });
        await using var cancelled = new BrowserImageInterop(new ImportingJSRuntime(module), new BitBmageOptions { MaxCodecWorkers = 2 });
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await cancelled.GetModuleAsync(cancel.Token));
        Assert.IsTrue(module.Disposed, "The module imported for a cancelled load was never released.");
        Assert.IsFalse(cancelled.IsLoaded);
    }

    [TestMethod]
    public async Task AUrlTheBrowserMayNotReadIsExplainedAsTheCorsProblemItUsuallyIs()
    {
        // What fetch reports when a server sends no CORS header: a bare TypeError that names neither cause nor cure.
        var module = new FakeModule(identifier => { if (identifier == "fetchImage") throw new JSException("TypeError: Failed to fetch"); });
        await using var interop = new BrowserImageInterop(new ImportingJSRuntime(module), new BitBmageOptions());
        var loader = new Input.ImageLoader(new Input.ImageProcessorFactory(interop, Capabilities.ManagedCapabilityProvider.Instance), interop, Capabilities.ManagedCapabilityProvider.Instance);

        var error = await Assert.ThrowsExactlyAsync<ImageDecodeException>(async () => await loader.LoadAsync(Input.ImageSource.FromUrl("https://photos.example/beach.jpg")));

        StringAssert.Contains(error.Message, "https://photos.example/beach.jpg");
        StringAssert.Contains(error.Message, "CORS header");
        StringAssert.Contains(error.Message, "Failed to fetch", "and what the browser said, for whoever debugs it");
        Assert.IsInstanceOfType<JSException>(error.InnerException);
    }

    [TestMethod]
    public async Task AUrlServingMoreThanTheSourceLimitIsRefused()
    {
        object? passed = null;
        var module = new FakeModule(_ => { }, (identifier, args) => { if (identifier == "fetchImage") passed = args?[2]; });
        await using var interop = new BrowserImageInterop(new ImportingJSRuntime(module), new BitBmageOptions());
        var loader = new Input.ImageLoader(new Input.ImageProcessorFactory(interop, Capabilities.ManagedCapabilityProvider.Instance), interop, Capabilities.ManagedCapabilityProvider.Instance) { MaxSourceBytes = 123_456 };

        await Assert.ThrowsExactlyAsync<ImageLimitExceededException>(async () => await loader.LoadAsync(Input.ImageSource.FromUrl("https://photos.example/huge.jpg")));
        Assert.AreEqual(123_456L, passed);
    }

    [TestMethod]
    public async Task AUrlServingNoMoreThanTheSourceLimitIsDecoded()
    {
        using var image = TestImages.Gradient(12, 9);
        var png = Codecs.Png.PngEncoder.Encode(image).Data;
        var module = new FakeModule(_ => { }, respond: (identifier, _) => identifier == "fetchImage"
            ? new FakeHandle(new JsonObject { ["mimeType"] = "image/png", ["byteLength"] = png.Length }, png)
            : null);
        await using var interop = new BrowserImageInterop(new ImportingJSRuntime(module), new BitBmageOptions());
        var loader = new Input.ImageLoader(new Input.ImageProcessorFactory(interop, Capabilities.ManagedCapabilityProvider.Instance), interop, Capabilities.ManagedCapabilityProvider.Instance) { MaxSourceBytes = png.Length };

        using var loaded = await loader.LoadAsync(Input.ImageSource.FromUrl("https://photos.example/small.png"));

        Assert.AreEqual(image.Size, loaded.Size);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(image, loaded));
    }

    [TestMethod]
    public async Task TheBrowserTextRasterizerHoldsTheBrowsersCanvasToItsLimits()
    {
        object?[]? passed = null;
        var module = new FakeModule(_ => { }, (identifier, args) => { if (identifier == "renderText") passed = args; },
            (identifier, _) => identifier == "renderText" ? new FakeHandle(new Text.BrowserTextRasterizer.TextRenderInfo { Width = 20_000, Height = 9_000, TooLarge = true }, []) : null);
        await using var interop = new BrowserImageInterop(new ImportingJSRuntime(module), new BitBmageOptions());
        using var rasterizer = new Text.BrowserTextRasterizer(interop) { Limits = ImageLimits.Default with { MaxPixels = 1_000_000, MaxWidth = 4_000, MaxHeight = 3_000 } };

        var error = await Assert.ThrowsExactlyAsync<ImageLimitExceededException>(async () => await rasterizer.MeasureAsync("Hello", new TextStyle()));
        StringAssert.Contains(error.Message, "20000x9000");
        Assert.AreEqual(1_000_000L, passed?[7]);
        Assert.AreEqual(4_000, passed?[8]);
        Assert.AreEqual(3_000, passed?[9]);
    }

    [TestMethod]
    public async Task TheBrowserTextRasterizersCacheIsBoundedInBytesAndEmptiedWhenItsLimitsChange()
    {
        const int side = 2048; // 16 MB a bitmap, so four fill the cache's 64 MB
        var rendered = new List<string>();
        var module = new FakeModule(_ => { }, (identifier, args) => { if (identifier == "renderText") rendered.Add((string)args![0]!); },
            (identifier, _) => identifier == "renderText" ? new FakeHandle(new Text.BrowserTextRasterizer.TextRenderInfo { Width = side, Height = side, LineCount = 1 }, new byte[side * side * 4]) : null);
        await using var interop = new BrowserImageInterop(new ImportingJSRuntime(module), new BitBmageOptions());
        using var rasterizer = new Text.BrowserTextRasterizer(interop);
        var style = new TextStyle();

        await rasterizer.MeasureAsync("a", style);
        await rasterizer.MeasureAsync("a", style);
        CollectionAssert.AreEqual(new[] { "a" }, rendered);

        // A bitmap cached under looser limits is not handed out under tighter ones.
        rasterizer.Limits = rasterizer.Limits with { MaxPixels = 32_000_000 };
        await rasterizer.MeasureAsync("a", style);
        CollectionAssert.AreEqual(new[] { "a", "a" }, rendered);

        // Past 64 MB the oldest goes first, however few the entries.
        foreach (var text in new[] { "b", "c", "d", "e" }) await rasterizer.MeasureAsync(text, style);
        rendered.Clear();
        foreach (var text in new[] { "b", "c", "d", "e", "a" }) await rasterizer.MeasureAsync(text, style);
        CollectionAssert.AreEqual(new[] { "a" }, rendered);
    }

    [TestMethod]
    public async Task TheBrowserDecoderChecksTheSizeTheBrowserDecodesAt()
    {
        var called = new List<string>();
        var module = new FakeModule(called.Add);
        await using var interop = new BrowserImageInterop(new ImportingJSRuntime(module), new BitBmageOptions());
        var decoder = new Codecs.Browser.BrowserImageDecoder(interop);

        // Browsers grow a GIF's canvas to take in a first frame larger than its logical screen of one pixel.
        byte[] gif =
        [
            .. "GIF89a"u8, 0x01, 0x00, 0x01, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF,
            0x2C, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x02, 0x02, 0x44, 0x01, 0x00, 0x3B,
        ];
        Assert.AreEqual(1, decoder.Identify(gif)!.Width);
        await Assert.ThrowsExactlyAsync<ImageLimitExceededException>(async () => await decoder.DecodeAsync(gif, DecodeOptions.Default, PooledPixelAllocator.Shared));

        // A file whose size cannot be read is refused, rather than handed to a browser that may read a larger one in it.
        byte[] noFrame = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, .. "JFIF\0"u8, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9];
        await Assert.ThrowsExactlyAsync<ImageDecodeException>(async () => await decoder.DecodeAsync(noFrame, DecodeOptions.Default, PooledPixelAllocator.Shared));
        CollectionAssert.DoesNotContain(called, "decode");

        // Unlimited, there is nothing to check the size against.
        var unlimited = DecodeOptions.Default with { Limits = ImageLimits.Unrestricted };
        await Assert.ThrowsExactlyAsync<ImageDecodeException>(async () => await decoder.DecodeAsync(noFrame, unlimited, PooledPixelAllocator.Shared));
        Assert.AreEqual(1, called.Count(identifier => identifier == "decode"));
    }

    /// <summary>
    /// With a MaxSize the browser decodes straight to the smaller size, so a 48 megapixel photo held to 8 megapixels
    /// still loads when it is asked for at 2048 pixels, as it did before the size was checked.
    /// </summary>
    [TestMethod]
    public async Task TheBrowserDecodersSizeCheckAllowsForTheResizeItAsksTheBrowserFor()
    {
        using var small = TestImages.Gradient(8, 8);
        var png = Codecs.Png.PngEncoder.Encode(small).Data;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(16), 8_000);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(20), 6_000);
        var called = new List<string>();
        var module = new FakeModule(called.Add);
        await using var interop = new BrowserImageInterop(new ImportingJSRuntime(module), new BitBmageOptions());
        var decoder = new Codecs.Browser.BrowserImageDecoder(interop);
        var options = DecodeOptions.Default with { Limits = ImageLimits.Default with { MaxPixels = 8_000_000 } };

        await Assert.ThrowsExactlyAsync<ImageLimitExceededException>(async () => await decoder.DecodeAsync(png, options, PooledPixelAllocator.Shared));
        await Assert.ThrowsExactlyAsync<ImageLimitExceededException>(async () => await decoder.DecodeAsync(png, options with { MaxSize = new Size(20_000, 20_000) }, PooledPixelAllocator.Shared));
        CollectionAssert.DoesNotContain(called, "decode");

        // These reach the browser, which the fake answers with nothing.
        await Assert.ThrowsExactlyAsync<ImageDecodeException>(async () => await decoder.DecodeAsync(png, options with { MaxSize = new Size(2_048, 2_048) }, PooledPixelAllocator.Shared));
        await Assert.ThrowsExactlyAsync<ImageDecodeException>(async () => await decoder.DecodeAsync(png, options with { MaxSize = new Size(2_048, 0) }, PooledPixelAllocator.Shared));
        Assert.AreEqual(2, called.Count(identifier => identifier == "decode"));
    }

    [TestMethod]
    public async Task TheBrowserDecoderRefusesAHeaderItCouldNotScaleIntoTheLimitsBeforeDecoding()
    {
        using var small = TestImages.Gradient(8, 8);
        byte[] Png(int width, int height)
        {
            var png = Codecs.Png.PngEncoder.Encode(small).Data;
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(16), (uint)width);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(20), (uint)height);
            return png;
        }
        var called = new List<string>();
        var module = new FakeModule(called.Add);
        await using var interop = new BrowserImageInterop(new ImportingJSRuntime(module), new BitBmageOptions());
        var decoder = new Codecs.Browser.BrowserImageDecoder(interop);

        await Assert.ThrowsExactlyAsync<ImageLimitExceededException>(async () => await decoder.DecodeAsync(Png(20_000, 20_000), DecodeOptions.Default, PooledPixelAllocator.Shared));
        CollectionAssert.DoesNotContain(called, "decode");

        await Assert.ThrowsExactlyAsync<ImageDecodeException>(async () => await decoder.DecodeAsync(Png(12_000, 9_000), DecodeOptions.Default, PooledPixelAllocator.Shared));
        await Assert.ThrowsExactlyAsync<ImageDecodeException>(async () => await decoder.DecodeAsync(Png(20_000, 1_000), DecodeOptions.Default with { MaxSize = new Size(1_000, 1_000) }, PooledPixelAllocator.Shared));
        Assert.AreEqual(2, called.Count(identifier => identifier == "decode"));
    }

    [TestMethod]
    public void ADocumentNestingMasksDeeplyIsRefused_AChainBuiltAStepAtATimeIsNot()
    {
        Mask nested = Mask.All();
        for (var i = 0; i < 20; i++) nested = Mask.Ellipse().Union(nested);
        var refused = Assert.ThrowsExactly<Serialization.ImageDocumentFormatException>(() => Serialization.BmageJson.DeserializeObject<Mask>(Serialization.BmageJson.SerializeObject(nested)));
        Assert.Contains("too deeply", refused.Message);

        Mask chain = Mask.All();
        for (var i = 0; i < 100; i++) chain = chain.Union(new EllipseMask(new RectangleF(i * 9, i * 7, 60, 40)));
        var back = Serialization.BmageJson.DeserializeObject<Mask>(Serialization.BmageJson.SerializeObject(chain));

        // However long the chain, drawing it holds its own mask and the one being combined in.
        var budget = new BudgetedPixelAllocator(long.MaxValue);
        using var rendered = back.Render(new Size(1000, 800), null, new OperationContext(budget));
        Assert.IsLessThanOrEqualTo(2L * 1000 * 800, budget.PeakBytes);
        using var expected = chain.Render(new Size(1000, 800));
        CollectionAssert.AreEqual(expected.Values.ToArray(), rendered.Values.ToArray());
    }

    [TestMethod]
    public void OperationsUseOneThreadUnlessToldOtherwise()
    {
        Assert.AreEqual(1, OperationContext.Default.MaxDegreeOfParallelism);
        Assert.AreEqual(1, new OperationContext().MaxDegreeOfParallelism);
        Assert.AreEqual(4, new OperationContext(maxDegreeOfParallelism: 4).MaxDegreeOfParallelism);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new DecodeOptions { MaxDegreeOfParallelism = 0 });
    }

    [TestMethod]
    public void ProgressIsReportedOnTheCallersThreadOnly()
    {
        using var source = ParallelProcessingTests.Noise(1600, 1200, seed: 8);
        var caller = Environment.CurrentManagedThreadId;
        var threads = new ConcurrentBag<int>();
        var values = new ConcurrentQueue<double>();
        var progress = new Inline(value =>
        {
            threads.Add(Environment.CurrentManagedThreadId);
            values.Enqueue(value);
        });
        using (new ResizeOperation(900, 700, ResizeMode.Stretch, ResamplingFilter.Lanczos3).Apply(source, new OperationContext(progress: progress, maxDegreeOfParallelism: 8))) { }

        Assert.IsTrue(threads.All(thread => thread == caller), "Progress was reported on a thread other than the caller's.");
        Assert.AreEqual(1.0, values.Last(), 1e-9);
    }

    [TestMethod]
    public void ANumberJsonHasNoLiteralForRoundTrips()
    {
        var mask = new RadialGradientMask(new Vector2(float.NaN, 0.5f), float.PositiveInfinity, 0.25f) { Rotation = float.NegativeInfinity, Units = MaskUnits.Relative };
        var json = Serialization.BmageJson.SerializeObject(mask);
        var back = (RadialGradientMask)Serialization.BmageJson.DeserializeObject<Mask>(json);

        Assert.IsTrue(float.IsNaN(back.Center.X));
        Assert.AreEqual(float.PositiveInfinity, back.RadiusX);
        Assert.AreEqual(float.NegativeInfinity, back.Rotation);
        Assert.AreEqual(json, Serialization.BmageJson.SerializeObject(back));
    }

    [TestMethod]
    public async Task ANullBatchItemFailsOnItsOwn()
    {
        var processor = new ImageProcessor();
        using var image = ParallelProcessingTests.Noise(16, 16, seed: 9);
        var png = (await processor.EncodeAsync(image, ImageExportOptions.Png)).Data;

        var result = await processor.ProcessBatchAsync([new BatchItem(png), null!, new BatchItem(png)], ImagePipeline.Create(), new BatchOptions { ExportOptions = ImageExportOptions.Png });

        Assert.AreEqual(2, result.SuccessCount);
        Assert.AreEqual(1, result.FailureCount);
        Assert.IsInstanceOfType<ArgumentNullException>(result.Results[1].Error);
    }

    [TestMethod]
    public void RenderingADocumentReportsTheLayersProgressToo()
    {
        using var source = ParallelProcessingTests.Noise(300, 200, seed: 10);
        var document = Editing.ImageDocument.Create(source)
            .AddOperation(new BrightnessAdjustment(0.1f))
            .AddLayer(new FillLayer(new Rgba32(255, 0, 0)) { Mask = Mask.Ellipse(), BlendMode = BlendMode.Multiply })
            .AddLayer(new AdjustmentLayer(ImagePipeline.Create().Saturation(0.3f)));
        var values = new ConcurrentQueue<double>();

        using (new Editing.DocumentRenderer().Render(document, source, new PipelineExecutionOptions { Progress = new Inline(values.Enqueue) })) { }

        var reported = values.ToArray();
        // One share for the pipeline's step, one for each layer.
        Assert.IsTrue(reported.Any(value => value > 0.34 && value < 0.99), $"The layers reported nothing: {string.Join(", ", reported)}.");
        Assert.AreEqual(1.0, reported[^1], 1e-9);
        for (var i = 1; i < reported.Length; i++) Assert.IsGreaterThanOrEqualTo(reported[i - 1], reported[i], $"Progress went from {reported[i - 1]} to {reported[i]}.");
    }

    [TestMethod]
    public void AStepOfANestedPipelineThatFailsIsNamed()
    {
        using var source = ImageBuffer.Create(8, 8, Rgba32.White);
        var nested = new PipelineOperation(ImagePipeline.Create().Brightness(0.1f).Apply(new Fails()));

        var failure = Assert.ThrowsExactly<ImageException>(() => nested.Apply(source, OperationContext.Default));

        Assert.Contains("'Fails' (step 2 of 2)", failure.Message);
    }

    [TestMethod]
    public void AWindowOfAMaskHoldsExactlyThoseValuesOfTheWholeMask()
    {
        var size = new Size(240, 180);
        using var source = ParallelProcessingTests.Noise(240, 180, seed: 11);
        using var matte = ParallelProcessingTests.Noise(60, 45, seed: 12);
        Mask[] masks =
        [
            new RectangleMask(new RectangleF(30.3f, 20.7f, 120, 90)) { CornerRadius = 12, Feather = 3 },
            Mask.Ellipse() with { Feather = 6.5f },
            new PolygonMask([new(10, 10), new(200, 40), new(90, 170)]) { Inverted = true, Feather = 1.2f },
            new PathMask(new PathBuilder().AddEllipse(new RectangleF(60, 40, 90, 70)).Build()) { Opacity = 0.7f },
            new BrushMask([new BrushStroke([new(20, 30), new(120, 90), new(220, 40)], 14)]) { Opacity = 0.6f },
            Mask.Gradient(new(0, 0), new(0.3f, 0.8f)) with { Feather = 2 },
            new RadialGradientMask(new Vector2(120, 90), 70, 40) { Rotation = 30, Feather = 0.5f },
            new LuminanceRangeMask(0.2f, 0.6f) { Feather = 4 },
            new ColorRangeMask(new Rgba32(128, 64, 200)) { Tolerance = 0.4f },
            new BitmapMask(matte) { Channel = MaskChannel.Luminance, Feather = 2 },
            new CheckerMask { Feather = 2 },
            Mask.Ellipse().Union(new RectangleMask(new RectangleF(0, 0, 60, 60))).Subtract(new EllipseMask(new RectangleF(100, 50, 50, 50)) { Feather = 5 }) with { Feather = 3 },
        ];
        Rectangle[] windows = [new(0, 0, 240, 180), new(37, 21, 50, 33), new(0, 100, 240, 80), new(200, 0, 40, 10), new(119, 89, 1, 1)];

        foreach (var mask in masks)
        {
            using var whole = mask.Render(size, source);
            foreach (var window in windows)
            {
                using var part = mask.RenderWindow(size, window, source, OperationContext.Default);
                for (var y = 0; y < window.Height; y++)
                    CollectionAssert.AreEqual(whole.GetRow(window.Y + y).Slice(window.X, window.Width).ToArray(), part.GetRow(y).ToArray(), $"{mask.Kind} in {window}, row {y}.");
            }
        }
    }

    [TestMethod]
    public void ASmallMaskedLayerDrawsOnlyItsOwnPartOfTheMask()
    {
        using var logo = ImageBuffer.Create(50, 50, new Rgba32(0, 0, 255));
        using var target = ImageBuffer.Create(2000, 1500, Rgba32.White);
        var budget = new BudgetedPixelAllocator(long.MaxValue);

        new ImageLayer { Image = logo, Bounds = new RectangleF(100, 100, 50, 50), Mask = new EllipseMask(new RectangleF(100, 100, 50, 50)) { Feather = 2 } }
            .Composite(target, new OperationContext(budget));

        // The whole image's mask alone would be 3 MB.
        Assert.IsLessThan(100_000L, budget.PeakBytes, $"Compositing held {budget.PeakBytes} bytes.");
        Assert.AreEqual(new Rgba32(0, 0, 255), target[125, 125]);
        Assert.AreEqual(Rgba32.White, target[101, 101]);
    }

    [TestMethod]
    public async Task FadingAnImageLayerInTheEditorPreparesItsImageOnce()
    {
        using var logo = ParallelProcessingTests.Noise(400, 400, seed: 13);
        var counter = new Counts();
        var budget = new BudgetedPixelAllocator(long.MaxValue);
        var layer = new ImageLayer { Image = logo, Bounds = new RectangleF(50, 50, 200, 200), Pipeline = ImagePipeline.Create().Apply(counter) };
        using (var session = new Editing.ImageEditSession(ParallelProcessingTests.Noise(600, 400, seed: 14), new Editing.ImageEditSessionOptions { MaxProxyDimension = 0, Allocator = budget }))
        {
            session.Apply(d => d.AddLayer(layer), "Add");
            await session.RenderPreviewAsync();
            foreach (var opacity in new[] { 0.8f, 0.6f, 0.4f })
            {
                session.ApplyTransient(d => d.UpdateLayer(d.FindLayer(layer.Id)! with { Opacity = opacity }));
                await session.RenderPreviewAsync();
            }
            Assert.AreEqual(1, counter.Runs, "Every frame prepared the layer's image again.");
        }
        Assert.AreEqual(0, budget.CurrentBytes, "The session kept a prepared layer image after it was disposed.");
    }

    private sealed class Fails : ImageOperation
    {
        public override string Name => "Fails";
        public override ImageBuffer Apply(ImageBuffer source, OperationContext context) => throw new InvalidOperationException("It always does.");
    }

    private sealed class Counts : ImageOperation
    {
        private int _runs;
        public int Runs => Volatile.Read(ref _runs);
        public override string Name => "Counts";
        public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
        {
            Interlocked.Increment(ref _runs);
            return GetMutableCopy(source, context);
        }
    }

    /// <summary>A mask from outside the library, which is always handed a target the size of the whole image.</summary>
    private sealed record CheckerMask : Mask
    {
        public override string Kind => "Checker";

        // Protected internal only because the tests see the library's internals; outside it this is protected.
        protected internal override void RenderCoverage(MaskBuffer target, MaskRenderContext context)
        {
            Assert.AreEqual(context.Size, target.Size, "A custom mask was handed part of the image.");
            for (var y = 0; y < target.Height; y++)
            {
                var row = target.GetRow(y);
                for (var x = 0; x < row.Length; x++) row[x] = (byte)(((x / 7 + y / 5) & 1) * 255);
            }
        }
    }

    private sealed class FakeModule(Action<string> invoke, Action<string, object?[]?>? inspect = null, Func<string, object?[]?, object?>? respond = null) : IJSObjectReference
    {
        public bool Disposed { get; private set; }
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            invoke(identifier);
            inspect?.Invoke(identifier, args);
            return ValueTask.FromResult(respond?.Invoke(identifier, args) is TValue value ? value : default(TValue)!);
        }
        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// A byte result as the module's functions return one: a descriptor from info() and a payload from bytes(). A
    /// descriptor of another type reaches the caller through JSON, as the browser's does.
    /// </summary>
    private sealed class FakeHandle(object info, byte[] bytes) : IJSObjectReference
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => identifier switch
        {
            "info" => ValueTask.FromResult(info is TValue typed ? typed : JsonSerializer.Deserialize<TValue>(JsonSerializer.Serialize(info))!),
            "bytes" => ValueTask.FromResult((TValue)(object)new FakeStream(bytes)),
            _ => ValueTask.FromResult(default(TValue)!),
        };
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeStream(byte[] bytes) : IJSStreamReference
    {
        public long Length => bytes.Length;
        public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => ValueTask.FromResult<Stream>(new MemoryStream(bytes));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class ImportingJSRuntime(IJSObjectReference module) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => identifier == "import" ? ValueTask.FromResult((TValue)(object)module) : throw new NotSupportedException();
    }

    private sealed class Inline(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }
}
