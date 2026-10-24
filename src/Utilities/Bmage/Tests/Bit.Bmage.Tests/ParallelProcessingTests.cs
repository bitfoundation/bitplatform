using System.Collections.Concurrent;
using System.Drawing;
using System.Numerics;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Operations.Filters;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Tests;

/// <summary>
/// Spreading an operation's rows over several threads must change nothing but the time it takes: every test renders
/// the same operation on one thread and on many, and requires identical pixels.
/// </summary>
[TestClass]
public sealed class ParallelProcessingTests
{
    private static IEnumerable<object[]> Operations()
    {
        yield return [new BrightnessAdjustment(0.15f)];
        yield return [new SaturationAdjustment(0.4f)];
        yield return [new FusedPointOperation([new ContrastAdjustment(0.2f), new SaturationAdjustment(-0.3f), new GammaAdjustment(1.3f)])];
        yield return [new ResizeOperation(611, 433, ResizeMode.Stretch, ResamplingFilter.Lanczos3)];
        yield return [new ResizeOperation(1500, 1100, ResizeMode.Stretch, ResamplingFilter.Bicubic)];
        yield return [new ResizeOperation(97, 61, ResizeMode.Stretch, ResamplingFilter.Lanczos3)];
        yield return [new ResizeOperation(333, 222, ResizeMode.Stretch, ResamplingFilter.NearestNeighbor)];
        yield return [new RotateOperation(37, new Rgba32(10, 20, 30, 200))];
        yield return [new RotateOperation(-12.5, sampling: WarpSampling.Bilinear)];
        yield return [new RotateOperation(80, sampling: WarpSampling.NearestNeighbor)];
        yield return [new PerspectiveTransformOperation([new PointF(40, 10), new PointF(900, 60), new PointF(1000, 700), new PointF(0, 760)])];
        yield return [ConvolutionFilter.EdgeDetect()];
        yield return [new MedianFilter(1)];
        yield return [new MedianFilter(3, new Rectangle(100, 50, 600, 400))];
        yield return [new VignetteFilter(0.7f)];
        yield return [new OrientationOperation(Orientation.Rotate90)];
        yield return [new OrientationOperation(Orientation.Transverse)];
        yield return [new OrientationOperation(Orientation.FlipHorizontal)];
    }

    [TestMethod]
    [DynamicData(nameof(Operations))]
    public void ManyThreadsProduceExactlyThePixelsOfOne(IImageOperation operation)
    {
        using var source = Noise(1024, 768, seed: 11);
        using var sequential = operation.Apply(source, new OperationContext(maxDegreeOfParallelism: 1));
        using var parallel = operation.Apply(source, new OperationContext(maxDegreeOfParallelism: 8));

        Assert.AreEqual(sequential.Size, parallel.Size);
        Assert.IsTrue(sequential.Bytes.SequenceEqual(parallel.Bytes), $"{operation.Name} differs between one thread and eight.");
    }

    [TestMethod]
    public void APipelineRunOnManyThreadsMatchesOneThread()
    {
        using var source = Noise(1200, 900, seed: 5);
        var pipeline = ImagePipeline.Create()
            .Rotate(7)
            .MaxSize(800, 800)
            .Contrast(0.1f).Saturation(0.2f)
            .Vignette(0.3f);

        using var one = pipeline.Execute(source, new PipelineExecutionOptions { MaxDegreeOfParallelism = 1 });
        using var many = pipeline.Execute(source, new PipelineExecutionOptions { MaxDegreeOfParallelism = 6 });

        Assert.IsTrue(one.Bytes.SequenceEqual(many.Bytes));
    }

    [TestMethod]
    public void ACustomPointOperationStaysOnOneThreadUnlessItSaysOtherwise()
    {
        using var source = Noise(1024, 1024, seed: 3);

        var unsafeOperation = new ThreadRecordingOperation(concurrent: false);
        using (unsafeOperation.Apply(source, new OperationContext(maxDegreeOfParallelism: 8))) { }
        Assert.AreEqual(1, unsafeOperation.Threads.Count, "A custom operation that did not opt in ran on several threads.");

        var safeOperation = new ThreadRecordingOperation(concurrent: true);
        // Fused with a built-in stage, which may run concurrently, the run is only as concurrent as its least safe stage.
        var fused = new FusedPointOperation([new BrightnessAdjustment(0.1f), new ThreadRecordingOperation(concurrent: false)]);
        Assert.IsFalse(fused.CanProcessRowsConcurrently);
        Assert.IsTrue(safeOperation.CanProcessRowsConcurrently);
    }

    [TestMethod]
    public void AnExceptionThrownInABandSurfacesAsItself()
    {
        using var source = Noise(1024, 1024, seed: 9);
        var failing = new FailingOperation();
        Assert.ThrowsExactly<InvalidDataException>(() => failing.Apply(source, new OperationContext(maxDegreeOfParallelism: 8)));
    }

    [TestMethod]
    public void CancellationStopsEveryBand()
    {
        using var source = Noise(1024, 1024, seed: 9);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() =>
            new RotateOperation(33).Apply(source, new OperationContext(cancellationToken: cts.Token, maxDegreeOfParallelism: 8)));
    }

    [TestMethod]
    public void ProgressNeverGoesBackwards_AndEndsAtOne()
    {
        using var source = Noise(1024, 1024, seed: 2);
        var values = new ConcurrentQueue<double>();
        using (new RotateOperation(21).Apply(source, new OperationContext(progress: new Inline(values.Enqueue), maxDegreeOfParallelism: 8))) { }

        var reported = values.ToArray();
        Assert.IsGreaterThan(1, reported.Length);
        for (var i = 1; i < reported.Length; i++)
            Assert.IsGreaterThanOrEqualTo(reported[i - 1], reported[i], $"Progress went from {reported[i - 1]} to {reported[i]}.");
        Assert.AreEqual(1.0, reported[^1]);
    }

    [TestMethod]
    public void SmallImagesAreNotSplitAtAll()
    {
        Assert.AreEqual(1, ParallelRows.BandCount(8, rows: 100, pixelsPerRow: 100));
        Assert.AreEqual(1, ParallelRows.BandCount(1, rows: 4000, pixelsPerRow: 4000));
        Assert.AreEqual(16, ParallelRows.BandCount(8, rows: 4000, pixelsPerRow: 4000));
        Assert.AreEqual(3, ParallelRows.BandCount(8, rows: 3, pixelsPerRow: 1_000_000));
    }

    [TestMethod]
    public void TheParallelismOptionRejectsZeroThreads()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new OperationContext(maxDegreeOfParallelism: 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PipelineExecutionOptions { MaxDegreeOfParallelism = 0 });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BitBmageOptions { MaxDegreeOfParallelism = -1 });
    }

    private sealed class Inline(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }

    private sealed class ThreadRecordingOperation(bool concurrent) : PointOperation
    {
        public ConcurrentDictionary<int, bool> Threads { get; } = new();
        public override string Name => "Records threads";
        public override bool CanProcessRowsConcurrently => concurrent;

        public override void ProcessRow(Span<Vector4> pixels)
        {
            Threads.TryAdd(Environment.CurrentManagedThreadId, true);
            // Slow enough that a second thread would get a share of the rows.
            for (var i = 0; i < pixels.Length; i++) pixels[i] = Vector4.SquareRoot(pixels[i]);
        }
    }

    private sealed class FailingOperation : PointOperation
    {
        private int _rows;
        public override string Name => "Fails";
        public override bool CanProcessRowsConcurrently => true;

        public override void ProcessRow(Span<Vector4> pixels)
        {
            if (Interlocked.Increment(ref _rows) == 300) throw new InvalidDataException("Row 300 is broken.");
        }
    }

    internal static ImageBuffer Noise(int width, int height, int seed)
    {
        var random = new Random(seed);
        var image = ImageBuffer.Create(width, height);
        var bytes = image.Bytes;
        random.NextBytes(bytes);
        // Mostly opaque, with some fully transparent and some translucent pixels, so alpha handling is exercised.
        for (var i = 3; i < bytes.Length; i += 4)
            bytes[i] = (byte)(bytes[i] < 30 ? 0 : bytes[i] < 90 ? bytes[i] : 255);
        return image;
    }
}
