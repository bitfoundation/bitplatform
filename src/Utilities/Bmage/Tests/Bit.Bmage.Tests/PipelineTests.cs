using System.Drawing;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class PipelineTests
{
    [TestMethod]
    public void IsImmutable()
    {
        var a = BmageImagePipeline.Create().Resize(100);
        var b = a.Grayscale();
        Assert.ContainsSingle(a);
        Assert.AreEqual(2, b.Count);
        Assert.AreNotSame(a, b);
    }

    [TestMethod]
    public void ComputesOutputSizeWithoutTouchingPixels()
    {
        var pipeline = BmageImagePipeline.Create().Resize(200, 100, BmageResizeMode.Stretch).Crop(10, 10, 50, 50).Orient(BmageOrientation.Rotate90);
        Assert.AreEqual(new Size(50, 50), pipeline.GetOutputSize(new Size(1000, 800)));
    }

    [TestMethod]
    public void ExecuteDoesNotModifyTheSource()
    {
        using var source = TestImages.Gradient(32, 32);
        using var copy = source.Clone();
        using var result = BmageImagePipeline.Create().Grayscale().Blur(2f).Execute(source);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(source, copy));
        Assert.IsTrue(TestImages.MaxChannelDifference(source, result) > 0);
    }

    [TestMethod]
    public void EmptyPipelineReturnsAnIndependentCopy()
    {
        using var source = TestImages.Gradient(16, 16);
        using var result = BmageImagePipeline.Empty.Execute(source);
        Assert.AreNotSame(source, result);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(source, result));
    }

    [TestMethod]
    public async Task ReportsMonotonicProgress()
    {
        using var source = TestImages.Gradient(64, 64);
        var values = new List<double>();
        var progress = new SynchronousProgress(v => values.Add(v));
        using var result = await BmageImagePipeline.Create().Resize(32, 32, BmageResizeMode.Stretch).Grayscale().Blur(1f)
            .ExecuteAsync(source, new BmagePipelineExecutionOptions { Progress = progress, YieldBetweenOperations = false });
        Assert.IsNotEmpty(values);
        Assert.AreEqual(1.0, values[^1], 0.5e-3);
        for (var i = 1; i < values.Count; i++)
            Assert.IsTrue(values[i] >= values[i - 1] - 1e-9, $"Progress went backwards: {values[i - 1]} → {values[i]}");
    }

    [TestMethod]
    public async Task HonoursCancellation()
    {
        using var source = TestImages.Gradient(512, 512);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            using var _ = await BmageImagePipeline.Create().Resize(256, 256, BmageResizeMode.Stretch).Blur(4f)
                .ExecuteAsync(source, new BmagePipelineExecutionOptions { CancellationToken = cts.Token });
        });
    }

    [TestMethod]
    public void WrapsOperationFailuresWithContext()
    {
        using var source = TestImages.Gradient(16, 16);
        var pipeline = BmageImagePipeline.Create().Add(new ThrowingOperation());
        var ex = Assert.ThrowsExactly<BmageImageException>(() => pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = false }));
        Assert.Contains("Boom", ex.Message, StringComparison.Ordinal);
        Assert.Contains("step 1 of 1", ex.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ForScaleAdjustsCoordinateDependentOperations()
    {
        var pipeline = BmageImagePipeline.Create().Crop(100, 100, 200, 200).Blur(8f).Pixelate(20);
        var half = pipeline.ForScale(0.5);
        Assert.AreEqual(new Rectangle(50, 50, 100, 100), ((BmageCropOperation)half[0]).Rectangle);
        Assert.AreEqual(4f, ((BmageBlurFilter)half[1]).Radius);
        Assert.AreEqual(10, ((BmagePixelateFilter)half[2]).BlockSize);
    }

    [TestMethod]
    public void ReplaceEnablesNonDestructiveReEditing()
    {
        var pipeline = BmageImagePipeline.Create().Crop(0, 0, 10, 10).Grayscale();
        var edited = pipeline.Replace(0, new BmageCropOperation(5, 5, 8, 8));
        Assert.AreEqual(new Rectangle(5, 5, 8, 8), ((BmageCropOperation)edited[0]).Rectangle);
        Assert.AreEqual(new Rectangle(0, 0, 10, 10), ((BmageCropOperation)pipeline[0]).Rectangle);
    }

    private sealed class ThrowingOperation : BmageImageOperation
    {
        public override string Name => "Throwing";
        public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context) => throw new InvalidOperationException("Boom");
    }

    private sealed class SynchronousProgress : IProgress<double>
    {
        private readonly Action<double> _action;
        public SynchronousProgress(Action<double> action) => _action = action;
        public void Report(double value) => _action(value);
    }
}

[TestClass]
public sealed class PipelineOptimizerTests
{
    [TestMethod]
    public void DropsIdentityOperations()
    {
        var pipeline = BmageImagePipeline.Create().Brightness(0).Grayscale(0).Resize(100, 100, BmageResizeMode.Stretch).Contrast(0);
        var optimized = pipeline.Optimize(new Size(200, 200));
        Assert.ContainsSingle(optimized);
        Assert.IsExactInstanceOfType<BmageResizeOperation>(optimized[0]);
    }

    [TestMethod]
    public void MergesConsecutiveCrops()
    {
        var optimized = BmageImagePipeline.Create().Crop(10, 10, 50, 50).Crop(5, 5, 20, 20).Optimize(new Size(200, 200));
        Assert.ContainsSingle(optimized);
        Assert.AreEqual(new Rectangle(15, 15, 20, 20), ((BmageCropOperation)optimized[0]).Rectangle);
    }

    [TestMethod]
    public void MergesConsecutiveOrientations()
    {
        var optimized = BmageImagePipeline.Create().Orient(BmageOrientation.Rotate90).Orient(BmageOrientation.Rotate90).Optimize(new Size(50, 50));
        Assert.ContainsSingle(optimized);
        Assert.AreEqual(BmageOrientation.Rotate180, ((BmageOrientationOperation)optimized[0]).Orientation);
    }

    [TestMethod]
    public void CancellingOrientationsDisappearEntirely()
    {
        var optimized = BmageImagePipeline.Create().FlipHorizontal().FlipHorizontal().Optimize(new Size(20, 20));
        Assert.IsEmpty(optimized);
    }

    [TestMethod]
    public void FusesConsecutivePointOperations()
    {
        var optimized = BmageImagePipeline.Create().Brightness(0.1f).Contrast(0.1f).Saturation(0.1f).Optimize(new Size(50, 50));
        Assert.ContainsSingle(optimized);
    }

    [TestMethod]
    public void DoesNotFuseAcrossGeometricOperations()
    {
        var optimized = BmageImagePipeline.Create().Brightness(0.1f).Crop(0, 0, 10, 10).Contrast(0.1f).Optimize(new Size(50, 50));
        Assert.AreEqual(3, optimized.Count);
    }

    /// <summary>
    /// Optimization must not change results structurally. Per-pixel fusion removes intermediate 8-bit rounding, so a
    /// few units of difference per channel are expected and allowed; anything larger indicates a rewrite bug.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(EquivalencePipelines))]
    public void OptimizationPreservesTheResultWithinQuantizationError(string name, BmageImagePipeline pipeline)
    {
        using var source = TestImages.Gradient(96, 72);
        using var direct = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = false });
        using var optimized = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = true });
        Assert.AreEqual(direct.Size, optimized.Size);
        var diff = TestImages.MaxChannelDifference(direct, optimized);
        var mean = TestImages.MeanChannelDifference(direct, optimized, includeAlpha: true);
        Assert.IsTrue(diff <= 4, $"'{name}' differed by max {diff} after optimization.");
        Assert.IsTrue(mean <= 0.75, $"'{name}' had mean difference {mean:0.####} after optimization.");
    }

    /// <summary>
    /// Fusion is not merely different from stepwise execution, it is closer to the mathematically exact answer.
    /// Compressing contrast to a third and then expanding it back is the identity, but stepwise execution quantises the
    /// compressed intermediate to 8 bits and then multiplies that error by three. The fused pass round-trips exactly.
    /// </summary>
    [TestMethod]
    public void FusionIsMoreAccurateThanStepwiseExecution()
    {
        using var source = TestImages.Gradient(64, 64);
        var pipeline = BmageImagePipeline.Create()
            .ColorMatrix(Bit.Bmage.BmageColorMatrix.Contrast(1f / 3f), "compress")
            .ColorMatrix(Bit.Bmage.BmageColorMatrix.Contrast(3f), "expand");
        using var stepwise = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = false });
        using var fused = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = true });
        var stepwiseError = TestImages.MaxChannelDifference(source, stepwise);
        var fusedError = TestImages.MaxChannelDifference(source, fused);
        Assert.IsTrue(fusedError < stepwiseError, $"fused error {fusedError} was not better than stepwise error {stepwiseError}");
        Assert.IsTrue(fusedError <= 1, $"fused round trip lost {fusedError} levels");
    }

    /// <summary>
    /// Curve (lookup table) fusion composes 256-entry tables, so it stays bit-identical to stepwise execution rather
    /// than gaining precision. That keeps gamma and levels edits reproducible whether or not the optimiser ran.
    /// </summary>
    [TestMethod]
    public void CurveFusionIsBitIdenticalToStepwiseExecution()
    {
        using var source = TestImages.Gradient(64, 64);
        var pipeline = BmageImagePipeline.Create().Gamma(2.2f).Gamma(1f / 1.8f).Posterize(64);
        using var stepwise = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = false });
        using var fused = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = true });
        Assert.AreEqual(0, TestImages.MaxChannelDifference(stepwise, fused));
    }

    /// <summary>Clipping behaviour must survive fusion: a blown-out highlight stays blown out.</summary>
    [TestMethod]
    public void FusionPreservesClippingBehaviour()
    {
        using var source = BmageImageBuffer.Create(4, 4, new BmageRgba32(200, 200, 200));
        var pipeline = BmageImagePipeline.Create().Brightness(0.8f).Brightness(-0.8f);
        using var stepwise = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = false });
        using var fused = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = true });
        // Both must clip to white and then darken to the same value, not return to the original 200.
        Assert.AreEqual(stepwise[0, 0].R, fused[0, 0].R);
        Assert.IsTrue(fused[0, 0].R < 100, $"Highlight was not clipped: {fused[0, 0]}");
    }

    public static IEnumerable<object[]> EquivalencePipelines() =>
    [
        ["adjust chain", BmageImagePipeline.Create().Brightness(0.1f).Contrast(0.15f).Saturation(-0.2f).Gamma(1.1f)],
        ["clipping adjust chain", BmageImagePipeline.Create().Brightness(0.6f).Contrast(0.5f).Exposure(1.5f).Brightness(-0.4f)],
        ["resize then crop", BmageImagePipeline.Create().Resize(48, 36, BmageResizeMode.Stretch).Crop(4, 4, 30, 20)],
        ["crop chain", BmageImagePipeline.Create().Crop(5, 5, 60, 50).Crop(2, 2, 30, 30)],
        ["orient chain", BmageImagePipeline.Create().Orient(BmageOrientation.Rotate90).FlipHorizontal().Orient(BmageOrientation.Rotate180)],
        ["identities", BmageImagePipeline.Create().Brightness(0).Grayscale(0.5f).Contrast(0)],
        ["mixed", BmageImagePipeline.Create().AutoOrient().Crop(4, 4, 80, 60).Brightness(0.05f).Contrast(0.05f).Resize(40, 30, BmageResizeMode.Stretch)],
        ["filters", BmageImagePipeline.Create().Blur(1.5f).Sharpen(0.4f).Vignette(0.3f)],
    ];

    [TestMethod]
    [DataRow(BmageResamplingFilter.Bilinear)]
    [DataRow(BmageResamplingFilter.Bicubic)]
    [DataRow(BmageResamplingFilter.Lanczos3)]
    [DataRow(BmageResamplingFilter.Box)]
    [DataRow(BmageResamplingFilter.NearestNeighbor)]
    public void ResizeThenCropFusesToIdenticalPixels(BmageResamplingFilter filter)
    {
        using var source = TestImages.Gradient(400, 300);
        var pipeline = BmageImagePipeline.Create().Resize(200, 150, BmageResizeMode.Stretch, filter).Crop(50, 40, 60, 50);
        using var direct = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = false });
        using var optimized = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = true });
        Assert.AreEqual(direct.Size, optimized.Size);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(direct, optimized));
    }

    [TestMethod]
    public void ResizeThenCropIsActuallyFused()
    {
        var optimized = BmageImagePipeline.Create().Resize(200, 150, BmageResizeMode.Stretch).Crop(50, 40, 60, 50).Optimize(new Size(400, 300));
        Assert.ContainsSingle(optimized);
        Assert.IsExactInstanceOfType<BmageResizeCropOperation>(optimized[0]);
        Assert.AreEqual(new Size(60, 50), optimized.GetOutputSize(new Size(400, 300)));
    }

    [TestMethod]
    public void AFusedResizeAndCropRefusesTheCanvasTheResizeAloneWouldRefuse()
    {
        using var source = TestImages.Gradient(40, 30);
        var pipeline = BmageImagePipeline.Create().Resize(20000, 20000, BmageResizeMode.Stretch).Crop(0, 0, 10, 10);
        Assert.IsExactInstanceOfType<BmageResizeCropOperation>(pipeline.Optimize(source.Size)[0]);
        Assert.ThrowsExactly<BmageImageLimitExceededException>(() => pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = false }));
        Assert.ThrowsExactly<BmageImageLimitExceededException>(() => pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = true }));

        using var allowed = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = true, Limits = BmageImageLimits.Unrestricted });
        Assert.AreEqual(new Size(10, 10), allowed.Size);
    }

    [TestMethod]
    public void ResizeWithPaddingIsNotFusedWithACrop()
    {
        // Contain letterboxes, so part of the output is background the resampler never writes.
        var optimized = BmageImagePipeline.Create().Resize(200, 200, BmageResizeMode.Contain).Crop(0, 0, 50, 50).Optimize(new Size(400, 200));
        Assert.AreEqual(2, optimized.Count);
    }

    [TestMethod]
    public void IsStableWhenRunTwice()
    {
        var pipeline = BmageImagePipeline.Create().Crop(1, 1, 30, 30).Crop(1, 1, 20, 20).Brightness(0.1f).Contrast(0.1f);
        var once = pipeline.Optimize(new Size(64, 64));
        var twice = once.Optimize(new Size(64, 64));
        Assert.AreEqual(once.Count, twice.Count);
    }
}

[TestClass]
public sealed class ImageProcessorTests
{
    private static readonly BmageImageProcessor Processor = new();

    [TestMethod]
    public async Task RoundTripsThroughDecodeProcessEncode()
    {
        using var source = TestImages.Gradient(120, 90);
        var input = BmagePngEncoder.Encode(source).Data;
        var result = await Processor.ProcessAsync(input, BmageImagePipeline.Create().Resize(60, 45, BmageResizeMode.Stretch).Grayscale(), BmageImageExportOptions.Png);
        Assert.AreEqual(BmageImageFormat.Png, result.Format);
        Assert.AreEqual(60, result.Width);
        Assert.AreEqual(45, result.Height);
        using var decoded = BmagePngDecoder.Decode(result.Data);
        Assert.AreEqual(decoded[10, 10].R, decoded[10, 10].G);
    }

    [TestMethod]
    public async Task ThrowsAClearErrorForUnknownFormats()
    {
        var garbage = new byte[64];
        var ex = await Assert.ThrowsExactlyAsync<BmageImageDecodeException>(async () => await Processor.DecodeAsync(garbage));
        Assert.Contains("supported image format", ex.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task ThrowsACapabilityErrorForUnavailableEncoders()
    {
        using var source = TestImages.Gradient(8, 8);
        var ex = await Assert.ThrowsExactlyAsync<BmageImageCapabilityException>(async () => await Processor.EncodeAsync(source, BmageImageExportOptions.Avif));
        Assert.Contains("Avif", ex.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task MeetsAMaximumFileSizeByLoweringQuality()
    {
        using var source = TestImages.Gradient(400, 300);
        var target = 6000L;
        var result = await Processor.EncodeAsync(source, new BmageImageExportOptions { Format = BmageImageFormat.Jpeg, Quality = 0.95, MaxFileSize = target });
        Assert.IsTrue(result.Length <= target, $"Result was {result.Length} bytes, target {target}");
        Assert.IsTrue(result.Quality < 0.95);
    }

    [TestMethod]
    public async Task MeetsAVerySmallSizeByShrinkingDimensions()
    {
        using var source = TestImages.Gradient(800, 600);
        var target = 1200L;
        var result = await Processor.EncodeAsync(source, new BmageImageExportOptions { Format = BmageImageFormat.Jpeg, Quality = 0.9, MaxFileSize = target });
        Assert.IsTrue(result.Length <= target, $"Result was {result.Length} bytes");
        Assert.IsTrue(result.Width < 800);
    }

    [TestMethod]
    public async Task ReportsWhenASizeTargetIsImpossible()
    {
        using var source = TestImages.Gradient(400, 300);
        await Assert.ThrowsExactlyAsync<BmageImageEncodeException>(async () =>
            await Processor.EncodeAsync(source, new BmageImageExportOptions { Format = BmageImageFormat.Jpeg, Quality = 0.9, MaxFileSize = 300, MaxFileSizeStrategy = BmageMaxFileSizeStrategy.QualityOnly }));
    }

    [TestMethod]
    public async Task ProcessesABatchWithPerItemErrors()
    {
        using var a = TestImages.Gradient(40, 30);
        using var b = TestImages.Gradient(20, 20);
        var items = new List<BmageBatchItem>
        {
            new(BmagePngEncoder.Encode(a).Data, "a.png"),
            new(new byte[32], "broken.bin"),
            new(BmagePngEncoder.Encode(b).Data, "b.png"),
        };
        var updates = new List<BmageBatchProgress>();
        var result = await Processor.ProcessBatchAsync(items, BmageImagePipeline.Create().Resize(10, 10, BmageResizeMode.Stretch),
            new BmageBatchOptions { MaxConcurrency = 2, ExportOptions = BmageImageExportOptions.Png, Progress = new Progress<BmageBatchProgress>(p => { lock (updates) updates.Add(p); }) });

        Assert.AreEqual(3, result.Results.Count);
        Assert.AreEqual(2, result.SuccessCount);
        Assert.AreEqual(1, result.FailureCount);
        Assert.IsFalse(result.AllSucceeded);
        var failed = result.Failed.Single();
        Assert.AreEqual("broken.bin", failed.FileName);
        Assert.IsExactInstanceOfType<BmageImageDecodeException>(failed.Error);
        foreach (var r in result.Succeeded)
        {
            Assert.AreEqual(10, r.Output!.Width);
        }
    }

    [TestMethod]
    public async Task BatchRespectsCancellation()
    {
        using var img = TestImages.Gradient(64, 64);
        var data = BmagePngEncoder.Encode(img).Data;
        var items = Enumerable.Range(0, 8).Select(i => new BmageBatchItem(data, $"{i}.png")).ToList();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var stopped = await Assert.ThrowsExactlyAsync<BmageBatchCancelledException>(async () =>
            await Processor.ProcessBatchAsync(items, BmageImagePipeline.Create().Resize(32, 32, BmageResizeMode.Stretch), BmageBatchOptions.Default, cts.Token));
        var result = stopped.Result;
        Assert.IsTrue(result.WasCancelled);
        Assert.AreEqual(8, result.CancelledCount);
        Assert.IsTrue(result.Results.All(r => r.Output is null && r.Error is null));
    }

    [TestMethod]
    public void IdentifiesWithoutDecoding()
    {
        using var source = TestImages.Gradient(77, 55);
        var data = BmagePngEncoder.Encode(source).Data;
        var info = Processor.Identify(data);
        Assert.IsNotNull(info);
        Assert.AreEqual(77, info!.Width);
        Assert.AreEqual(BmageImageFormat.Png, info.Format);
    }
}
