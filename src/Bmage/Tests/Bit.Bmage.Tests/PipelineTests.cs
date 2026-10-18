using System.Drawing;
using Bit.Bmage.Codecs;
using Bit.Bmage.Codecs.Png;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class PipelineTests
{
    [TestMethod]
    public void IsImmutable()
    {
        var a = ImagePipeline.Create().Resize(100);
        var b = a.Grayscale();
        Assert.ContainsSingle(a);
        Assert.AreEqual(2, b.Count);
        Assert.AreNotSame(a, b);
    }

    [TestMethod]
    public void ComputesOutputSizeWithoutTouchingPixels()
    {
        var pipeline = ImagePipeline.Create().Resize(200, 100, ResizeMode.Stretch).Crop(10, 10, 50, 50).Orient(Orientation.Rotate90);
        Assert.AreEqual(new Size(50, 50), pipeline.GetOutputSize(new Size(1000, 800)));
    }

    [TestMethod]
    public void ExecuteDoesNotModifyTheSource()
    {
        using var source = TestImages.Gradient(32, 32);
        using var copy = source.Clone();
        using var result = ImagePipeline.Create().Grayscale().Blur(2f).Execute(source);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(source, copy));
        Assert.IsTrue(TestImages.MaxChannelDifference(source, result) > 0);
    }

    [TestMethod]
    public void EmptyPipelineReturnsAnIndependentCopy()
    {
        using var source = TestImages.Gradient(16, 16);
        using var result = ImagePipeline.Empty.Execute(source);
        Assert.AreNotSame(source, result);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(source, result));
    }

    [TestMethod]
    public async Task ReportsMonotonicProgress()
    {
        using var source = TestImages.Gradient(64, 64);
        var values = new List<double>();
        var progress = new SynchronousProgress(v => values.Add(v));
        using var result = await ImagePipeline.Create().Resize(32, 32, ResizeMode.Stretch).Grayscale().Blur(1f)
            .ExecuteAsync(source, new PipelineExecutionOptions { Progress = progress, YieldBetweenOperations = false });
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
            using var _ = await ImagePipeline.Create().Resize(256, 256, ResizeMode.Stretch).Blur(4f)
                .ExecuteAsync(source, new PipelineExecutionOptions { CancellationToken = cts.Token });
        });
    }

    [TestMethod]
    public void WrapsOperationFailuresWithContext()
    {
        using var source = TestImages.Gradient(16, 16);
        var pipeline = ImagePipeline.Create().Add(new ThrowingOperation());
        var ex = Assert.ThrowsExactly<ImageException>(() => pipeline.Execute(source, new PipelineExecutionOptions { Optimize = false }));
        Assert.Contains("Boom", ex.Message, StringComparison.Ordinal);
        Assert.Contains("step 1 of 1", ex.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ForScaleAdjustsCoordinateDependentOperations()
    {
        var pipeline = ImagePipeline.Create().Crop(100, 100, 200, 200).Blur(8f).Pixelate(20);
        var half = pipeline.ForScale(0.5);
        Assert.AreEqual(new Rectangle(50, 50, 100, 100), ((CropOperation)half[0]).Rectangle);
        Assert.AreEqual(4f, ((Operations.Filters.BlurFilter)half[1]).Radius);
        Assert.AreEqual(10, ((Operations.Filters.PixelateFilter)half[2]).BlockSize);
    }

    [TestMethod]
    public void ReplaceEnablesNonDestructiveReEditing()
    {
        var pipeline = ImagePipeline.Create().Crop(0, 0, 10, 10).Grayscale();
        var edited = pipeline.Replace(0, new CropOperation(5, 5, 8, 8));
        Assert.AreEqual(new Rectangle(5, 5, 8, 8), ((CropOperation)edited[0]).Rectangle);
        Assert.AreEqual(new Rectangle(0, 0, 10, 10), ((CropOperation)pipeline[0]).Rectangle);
    }

    private sealed class ThrowingOperation : ImageOperation
    {
        public override string Name => "Throwing";
        public override ImageBuffer Apply(ImageBuffer source, OperationContext context) => throw new InvalidOperationException("Boom");
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
        var pipeline = ImagePipeline.Create().Brightness(0).Grayscale(0).Resize(100, 100, ResizeMode.Stretch).Contrast(0);
        var optimized = pipeline.Optimize(new Size(200, 200));
        Assert.ContainsSingle(optimized);
        Assert.IsExactInstanceOfType<ResizeOperation>(optimized[0]);
    }

    [TestMethod]
    public void MergesConsecutiveCrops()
    {
        var optimized = ImagePipeline.Create().Crop(10, 10, 50, 50).Crop(5, 5, 20, 20).Optimize(new Size(200, 200));
        Assert.ContainsSingle(optimized);
        Assert.AreEqual(new Rectangle(15, 15, 20, 20), ((CropOperation)optimized[0]).Rectangle);
    }

    [TestMethod]
    public void MergesConsecutiveOrientations()
    {
        var optimized = ImagePipeline.Create().Orient(Orientation.Rotate90).Orient(Orientation.Rotate90).Optimize(new Size(50, 50));
        Assert.ContainsSingle(optimized);
        Assert.AreEqual(Orientation.Rotate180, ((OrientationOperation)optimized[0]).Orientation);
    }

    [TestMethod]
    public void CancellingOrientationsDisappearEntirely()
    {
        var optimized = ImagePipeline.Create().FlipHorizontal().FlipHorizontal().Optimize(new Size(20, 20));
        Assert.IsEmpty(optimized);
    }

    [TestMethod]
    public void FusesConsecutivePointOperations()
    {
        var optimized = ImagePipeline.Create().Brightness(0.1f).Contrast(0.1f).Saturation(0.1f).Optimize(new Size(50, 50));
        Assert.ContainsSingle(optimized);
    }

    [TestMethod]
    public void DoesNotFuseAcrossGeometricOperations()
    {
        var optimized = ImagePipeline.Create().Brightness(0.1f).Crop(0, 0, 10, 10).Contrast(0.1f).Optimize(new Size(50, 50));
        Assert.AreEqual(3, optimized.Count);
    }

    /// <summary>
    /// Optimization must not change results structurally. Per-pixel fusion removes intermediate 8-bit rounding, so a
    /// few units of difference per channel are expected and allowed; anything larger indicates a rewrite bug.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(EquivalencePipelines))]
    public void OptimizationPreservesTheResultWithinQuantizationError(string name, ImagePipeline pipeline)
    {
        using var source = TestImages.Gradient(96, 72);
        using var direct = pipeline.Execute(source, new PipelineExecutionOptions { Optimize = false });
        using var optimized = pipeline.Execute(source, new PipelineExecutionOptions { Optimize = true });
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
        var pipeline = ImagePipeline.Create()
            .ColorMatrix(Bit.Bmage.Operations.ColorMatrix.Contrast(1f / 3f), "compress")
            .ColorMatrix(Bit.Bmage.Operations.ColorMatrix.Contrast(3f), "expand");
        using var stepwise = pipeline.Execute(source, new PipelineExecutionOptions { Optimize = false });
        using var fused = pipeline.Execute(source, new PipelineExecutionOptions { Optimize = true });
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
        var pipeline = ImagePipeline.Create().Gamma(2.2f).Gamma(1f / 1.8f).Posterize(64);
        using var stepwise = pipeline.Execute(source, new PipelineExecutionOptions { Optimize = false });
        using var fused = pipeline.Execute(source, new PipelineExecutionOptions { Optimize = true });
        Assert.AreEqual(0, TestImages.MaxChannelDifference(stepwise, fused));
    }

    /// <summary>Clipping behaviour must survive fusion: a blown-out highlight stays blown out.</summary>
    [TestMethod]
    public void FusionPreservesClippingBehaviour()
    {
        using var source = ImageBuffer.Create(4, 4, new Rgba32(200, 200, 200));
        var pipeline = ImagePipeline.Create().Brightness(0.8f).Brightness(-0.8f);
        using var stepwise = pipeline.Execute(source, new PipelineExecutionOptions { Optimize = false });
        using var fused = pipeline.Execute(source, new PipelineExecutionOptions { Optimize = true });
        // Both must clip to white and then darken to the same value, not return to the original 200.
        Assert.AreEqual(stepwise[0, 0].R, fused[0, 0].R);
        Assert.IsTrue(fused[0, 0].R < 100, $"Highlight was not clipped: {fused[0, 0]}");
    }

    public static IEnumerable<object[]> EquivalencePipelines() =>
    [
        ["adjust chain", ImagePipeline.Create().Brightness(0.1f).Contrast(0.15f).Saturation(-0.2f).Gamma(1.1f)],
        ["clipping adjust chain", ImagePipeline.Create().Brightness(0.6f).Contrast(0.5f).Exposure(1.5f).Brightness(-0.4f)],
        ["resize then crop", ImagePipeline.Create().Resize(48, 36, ResizeMode.Stretch).Crop(4, 4, 30, 20)],
        ["crop chain", ImagePipeline.Create().Crop(5, 5, 60, 50).Crop(2, 2, 30, 30)],
        ["orient chain", ImagePipeline.Create().Orient(Orientation.Rotate90).FlipHorizontal().Orient(Orientation.Rotate180)],
        ["identities", ImagePipeline.Create().Brightness(0).Grayscale(0.5f).Contrast(0)],
        ["mixed", ImagePipeline.Create().AutoOrient().Crop(4, 4, 80, 60).Brightness(0.05f).Contrast(0.05f).Resize(40, 30, ResizeMode.Stretch)],
        ["filters", ImagePipeline.Create().Blur(1.5f).Sharpen(0.4f).Vignette(0.3f)],
    ];

    [TestMethod]
    [DataRow(ResamplingFilter.Bilinear)]
    [DataRow(ResamplingFilter.Bicubic)]
    [DataRow(ResamplingFilter.Lanczos3)]
    [DataRow(ResamplingFilter.Box)]
    [DataRow(ResamplingFilter.NearestNeighbor)]
    public void ResizeThenCropFusesToIdenticalPixels(ResamplingFilter filter)
    {
        using var source = TestImages.Gradient(400, 300);
        var pipeline = ImagePipeline.Create().Resize(200, 150, ResizeMode.Stretch, filter).Crop(50, 40, 60, 50);
        using var direct = pipeline.Execute(source, new PipelineExecutionOptions { Optimize = false });
        using var optimized = pipeline.Execute(source, new PipelineExecutionOptions { Optimize = true });
        Assert.AreEqual(direct.Size, optimized.Size);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(direct, optimized));
    }

    [TestMethod]
    public void ResizeThenCropIsActuallyFused()
    {
        var optimized = ImagePipeline.Create().Resize(200, 150, ResizeMode.Stretch).Crop(50, 40, 60, 50).Optimize(new Size(400, 300));
        Assert.ContainsSingle(optimized);
        Assert.IsExactInstanceOfType<ResizeCropOperation>(optimized[0]);
        Assert.AreEqual(new Size(60, 50), optimized.GetOutputSize(new Size(400, 300)));
    }

    [TestMethod]
    public void ResizeWithPaddingIsNotFusedWithACrop()
    {
        // Contain letterboxes, so part of the output is background the resampler never writes.
        var optimized = ImagePipeline.Create().Resize(200, 200, ResizeMode.Contain).Crop(0, 0, 50, 50).Optimize(new Size(400, 200));
        Assert.AreEqual(2, optimized.Count);
    }

    [TestMethod]
    public void IsStableWhenRunTwice()
    {
        var pipeline = ImagePipeline.Create().Crop(1, 1, 30, 30).Crop(1, 1, 20, 20).Brightness(0.1f).Contrast(0.1f);
        var once = pipeline.Optimize(new Size(64, 64));
        var twice = once.Optimize(new Size(64, 64));
        Assert.AreEqual(once.Count, twice.Count);
    }
}

[TestClass]
public sealed class ImageProcessorTests
{
    private static readonly ImageProcessor Processor = new();

    [TestMethod]
    public async Task RoundTripsThroughDecodeProcessEncode()
    {
        using var source = TestImages.Gradient(120, 90);
        var input = PngEncoder.Encode(source).Data;
        var result = await Processor.ProcessAsync(input, ImagePipeline.Create().Resize(60, 45, ResizeMode.Stretch).Grayscale(), ImageExportOptions.Png);
        Assert.AreEqual(ImageFormat.Png, result.Format);
        Assert.AreEqual(60, result.Width);
        Assert.AreEqual(45, result.Height);
        using var decoded = PngDecoder.Decode(result.Data);
        Assert.AreEqual(decoded[10, 10].R, decoded[10, 10].G);
    }

    [TestMethod]
    public async Task ThrowsAClearErrorForUnknownFormats()
    {
        var garbage = new byte[64];
        var ex = await Assert.ThrowsExactlyAsync<ImageDecodeException>(async () => await Processor.DecodeAsync(garbage));
        Assert.Contains("supported image format", ex.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task ThrowsACapabilityErrorForUnavailableEncoders()
    {
        using var source = TestImages.Gradient(8, 8);
        var ex = await Assert.ThrowsExactlyAsync<ImageCapabilityException>(async () => await Processor.EncodeAsync(source, ImageExportOptions.WebP));
        Assert.Contains("WebP", ex.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task MeetsAMaximumFileSizeByLoweringQuality()
    {
        using var source = TestImages.Gradient(400, 300);
        var target = 6000L;
        var result = await Processor.EncodeAsync(source, new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.95, MaxFileSize = target });
        Assert.IsTrue(result.Length <= target, $"Result was {result.Length} bytes, target {target}");
        Assert.IsTrue(result.Quality < 0.95);
    }

    [TestMethod]
    public async Task MeetsAVerySmallSizeByShrinkingDimensions()
    {
        using var source = TestImages.Gradient(800, 600);
        var target = 1200L;
        var result = await Processor.EncodeAsync(source, new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.9, MaxFileSize = target });
        Assert.IsTrue(result.Length <= target, $"Result was {result.Length} bytes");
        Assert.IsTrue(result.Width < 800);
    }

    [TestMethod]
    public async Task ReportsWhenASizeTargetIsImpossible()
    {
        using var source = TestImages.Gradient(400, 300);
        await Assert.ThrowsExactlyAsync<ImageEncodeException>(async () =>
            await Processor.EncodeAsync(source, new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.9, MaxFileSize = 300, MaxFileSizeStrategy = MaxFileSizeStrategy.QualityOnly }));
    }

    [TestMethod]
    public async Task ProcessesABatchWithPerItemErrors()
    {
        using var a = TestImages.Gradient(40, 30);
        using var b = TestImages.Gradient(20, 20);
        var items = new List<BatchItem>
        {
            new(PngEncoder.Encode(a).Data, "a.png"),
            new(new byte[32], "broken.bin"),
            new(PngEncoder.Encode(b).Data, "b.png"),
        };
        var updates = new List<BatchProgress>();
        var result = await Processor.ProcessBatchAsync(items, ImagePipeline.Create().Resize(10, 10, ResizeMode.Stretch),
            new BatchOptions { MaxConcurrency = 2, ExportOptions = ImageExportOptions.Png, Progress = new Progress<BatchProgress>(p => { lock (updates) updates.Add(p); }) });

        Assert.AreEqual(3, result.Results.Count);
        Assert.AreEqual(2, result.SuccessCount);
        Assert.AreEqual(1, result.FailureCount);
        Assert.IsFalse(result.AllSucceeded);
        var failed = result.Failed.Single();
        Assert.AreEqual("broken.bin", failed.FileName);
        Assert.IsExactInstanceOfType<ImageDecodeException>(failed.Error);
        foreach (var r in result.Succeeded)
        {
            Assert.AreEqual(10, r.Output!.Width);
        }
    }

    [TestMethod]
    public async Task BatchRespectsCancellation()
    {
        using var img = TestImages.Gradient(64, 64);
        var data = PngEncoder.Encode(img).Data;
        var items = Enumerable.Range(0, 8).Select(i => new BatchItem(data, $"{i}.png")).ToList();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Processor.ProcessBatchAsync(items, ImagePipeline.Create().Resize(32, 32, ResizeMode.Stretch), BatchOptions.Default, cts.Token));
    }

    [TestMethod]
    public void IdentifiesWithoutDecoding()
    {
        using var source = TestImages.Gradient(77, 55);
        var data = PngEncoder.Encode(source).Data;
        var info = Processor.Identify(data);
        Assert.IsNotNull(info);
        Assert.AreEqual(77, info!.Width);
        Assert.AreEqual(ImageFormat.Png, info.Format);
    }
}
