using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using Bit.Bmage;
using Bit.Bmage.Codecs.Jpeg;
using Bit.Bmage.Codecs.Png;
using Bit.Bmage.Drawing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Operations.Filters;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;
using System.Numerics;

namespace Bit.Bmage.Benchmarks;

/// <summary>
/// Measures the operations that dominate real workloads. These run on desktop .NET rather than in a browser, so treat
/// them as a relative guide to where time goes. The ordering holds on WebAssembly; the absolute numbers are 1.2 to 2.4
/// times larger there with AOT compilation and 10 to 35 times larger on the default interpreter.
/// </summary>
public static class Images
{
    /// <summary>Builds a deterministic photo-like image: smooth gradients plus high frequency detail.</summary>
    public static ImageBuffer Create(int width, int height)
    {
        var image = ImageBuffer.Create(width, height, clear: false);
        var rng = new Random(12345);
        for (var y = 0; y < height; y++)
        {
            var row = image.GetRow(y);
            for (var x = 0; x < width; x++)
            {
                var r = (byte)Math.Clamp(x * 255 / Math.Max(1, width - 1) + rng.Next(-12, 12), 0, 255);
                var g = (byte)Math.Clamp(y * 255 / Math.Max(1, height - 1) + rng.Next(-12, 12), 0, 255);
                var b = (byte)Math.Clamp(((x >> 3) + (y >> 3)) % 2 == 0 ? 60 : 200, 0, 255);
                row[x] = new Rgba32(r, g, b);
            }
        }
        return image;
    }
}

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ResizeBenchmarks
{
    private ImageBuffer _source = null!;

    /// <summary>1080p, 4K and a 24 megapixel camera file: the sizes users actually upload.</summary>
    [Params("1920x1080", "3840x2160", "6000x4000")]
    public string Size { get; set; } = "1920x1080";

    [GlobalSetup]
    public void Setup()
    {
        var parts = Size.Split('x');
        _source = Images.Create(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    [GlobalCleanup]
    public void Cleanup() => _source.Dispose();

    [Benchmark(Baseline = true, Description = "Downscale to 1280 (bilinear)")]
    public int Bilinear() => Run(ResamplingFilter.Bilinear);

    [Benchmark(Description = "Downscale to 1280 (bicubic)")]
    public int Bicubic() => Run(ResamplingFilter.Bicubic);

    [Benchmark(Description = "Downscale to 1280 (lanczos3)")]
    public int Lanczos() => Run(ResamplingFilter.Lanczos3);

    [Benchmark(Description = "Downscale to 1280 (box)")]
    public int Box() => Run(ResamplingFilter.Box);

    [Benchmark(Description = "Thumbnail to 200 (auto)")]
    public int Thumbnail()
    {
        using var result = new ResizeOperation(200, 200, ResizeMode.Fit).Apply(_source, OperationContext.Default);
        return result.Width;
    }

    private int Run(ResamplingFilter filter)
    {
        using var result = new ResizeOperation(1280, 1280, ResizeMode.Fit, filter).Apply(_source, OperationContext.Default);
        return result.Width;
    }
}

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class OperationBenchmarks
{
    private ImageBuffer _source = null!;

    [Params("1920x1080", "3840x2160")]
    public string Size { get; set; } = "1920x1080";

    [GlobalSetup]
    public void Setup()
    {
        var parts = Size.Split('x');
        _source = Images.Create(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    [GlobalCleanup]
    public void Cleanup() => _source.Dispose();

    [Benchmark(Baseline = true, Description = "Brightness (single point op)")]
    public int Brightness()
    {
        using var result = new BrightnessAdjustment(0.15f).Apply(_source, OperationContext.Default);
        return result.Width;
    }

    [Benchmark(Description = "Four adjustments, unfused")]
    public int AdjustmentsUnfused()
    {
        using var result = ImagePipeline.Create().Brightness(0.1f).Contrast(0.12f).Saturation(0.15f).Gamma(1.1f)
            .Execute(_source, new PipelineExecutionOptions { Optimize = false });
        return result.Width;
    }

    [Benchmark(Description = "Four adjustments, fused")]
    public int AdjustmentsFused()
    {
        using var result = ImagePipeline.Create().Brightness(0.1f).Contrast(0.12f).Saturation(0.15f).Gamma(1.1f)
            .Execute(_source, new PipelineExecutionOptions { Optimize = true });
        return result.Width;
    }

    [Benchmark(Description = "Gaussian blur, radius 8")]
    public int Blur()
    {
        using var result = new BlurFilter(8f).Apply(_source, OperationContext.Default.WithMutation(false));
        return result.Width;
    }

    [Benchmark(Description = "Unsharp mask")]
    public int Sharpen()
    {
        using var result = new SharpenFilter(0.5f, 1.5f).Apply(_source, OperationContext.Default);
        return result.Width;
    }

    [Benchmark(Description = "Rotate 90 (lossless)")]
    public int Rotate90()
    {
        using var result = OrientationOperation.Rotate90.Apply(_source, OperationContext.Default);
        return result.Width;
    }

    [Benchmark(Description = "Rotate 37 (resampled)")]
    public int RotateArbitrary()
    {
        using var result = new RotateOperation(37).Apply(_source, OperationContext.Default);
        return result.Width;
    }

    [Benchmark(Description = "Crop to centre quarter")]
    public int Crop()
    {
        var w = _source.Width / 2;
        var h = _source.Height / 2;
        using var result = new CropOperation(w / 2, h / 2, w, h).Apply(_source, OperationContext.Default);
        return result.Width;
    }
}

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PipelineBenchmarks
{
    private ImageBuffer _source = null!;

    [GlobalSetup]
    public void Setup() => _source = Images.Create(4000, 3000);

    [GlobalCleanup]
    public void Cleanup() => _source.Dispose();

    /// <summary>The upload pipeline most applications run.</summary>
    private static readonly ImagePipeline Upload = ImagePipeline.Create()
        .AutoOrient()
        .MaxSize(1920, 1920)
        .Sharpen(0.3f);

    /// <summary>A resize immediately followed by a crop, which the optimiser fuses into one resample.</summary>
    private static readonly ImagePipeline ResizeThenCrop = ImagePipeline.Create()
        .Resize(1600, 1200, ResizeMode.Stretch)
        .Crop(400, 300, 800, 600);

    [Benchmark(Baseline = true, Description = "Upload pipeline (optimised)")]
    public int UploadOptimised()
    {
        using var result = Upload.Execute(_source, new PipelineExecutionOptions { Optimize = true });
        return result.Width;
    }

    [Benchmark(Description = "Upload pipeline (unoptimised)")]
    public int UploadUnoptimised()
    {
        using var result = Upload.Execute(_source, new PipelineExecutionOptions { Optimize = false });
        return result.Width;
    }

    [Benchmark(Description = "Resize+crop fused")]
    public int FusedResizeCrop()
    {
        using var result = ResizeThenCrop.Execute(_source, new PipelineExecutionOptions { Optimize = true });
        return result.Width;
    }

    [Benchmark(Description = "Resize+crop separate")]
    public int SeparateResizeCrop()
    {
        using var result = ResizeThenCrop.Execute(_source, new PipelineExecutionOptions { Optimize = false });
        return result.Width;
    }
}

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class CodecBenchmarks
{
    private ImageBuffer _source = null!;
    private byte[] _png = [];
    private byte[] _jpeg = [];

    [Params("1024x768", "3840x2160")]
    public string Size { get; set; } = "1024x768";

    [GlobalSetup]
    public void Setup()
    {
        var parts = Size.Split('x');
        _source = Images.Create(int.Parse(parts[0]), int.Parse(parts[1]));
        _png = PngEncoder.Encode(_source, new Codecs.ImageExportOptions { Format = ImageFormat.Png, PngCompressionLevel = 6 }).Data;
        _jpeg = JpegEncoder.Encode(_source, new Codecs.ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.85 }).Data;
    }

    [GlobalCleanup]
    public void Cleanup() => _source.Dispose();

    [Benchmark(Description = "PNG encode (level 6)")]
    public int EncodePng() => PngEncoder.Encode(_source, new Codecs.ImageExportOptions { Format = ImageFormat.Png, PngCompressionLevel = 6 }).Data.Length;

    [Benchmark(Description = "PNG encode (level 1)")]
    public int EncodePngFast() => PngEncoder.Encode(_source, new Codecs.ImageExportOptions { Format = ImageFormat.Png, PngCompressionLevel = 1 }).Data.Length;

    [Benchmark(Description = "PNG decode")]
    public int DecodePng()
    {
        using var image = PngDecoder.Decode(_png);
        return image.Width;
    }

    [Benchmark(Baseline = true, Description = "JPEG encode (q85)")]
    public int EncodeJpeg() => JpegEncoder.Encode(_source, new Codecs.ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.85 }).Data.Length;

    [Benchmark(Description = "JPEG decode")]
    public int DecodeJpeg()
    {
        using var image = JpegDecoder.Decode(_jpeg);
        return image.Width;
    }
}

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class DrawingBenchmarks
{
    private ImageCanvas _canvas = null!;
    private VectorPath _complexPath = null!;

    [GlobalSetup]
    public void Setup()
    {
        _canvas = ImageCanvas.Create(1920, 1080, Rgba32.White);
        var builder = new PathBuilder();
        var rng = new Random(7);
        for (var i = 0; i < 40; i++)
        {
            builder.MoveTo(rng.Next(1920), rng.Next(1080));
            for (var j = 0; j < 12; j++)
                builder.CubicTo(
                    new Vector2(rng.Next(1920), rng.Next(1080)),
                    new Vector2(rng.Next(1920), rng.Next(1080)),
                    new Vector2(rng.Next(1920), rng.Next(1080)));
        }
        _complexPath = builder.Build();
    }

    [GlobalCleanup]
    public void Cleanup() => _canvas.Dispose();

    [Benchmark(Baseline = true, Description = "Fill 100 rectangles")]
    public int FillRectangles()
    {
        for (var i = 0; i < 100; i++)
            _canvas.FillRectangle(new System.Drawing.RectangleF(i * 5, i * 3, 200, 120), new Rgba32(20, 60, 200, 128));
        return 100;
    }

    [Benchmark(Description = "Fill 100 ellipses")]
    public int FillEllipses()
    {
        for (var i = 0; i < 100; i++)
            _canvas.FillEllipse(new System.Drawing.RectangleF(i * 5, i * 3, 200, 120), new Rgba32(200, 60, 20, 128));
        return 100;
    }

    [Benchmark(Description = "Stroke 100 arrows")]
    public int StrokeArrows()
    {
        for (var i = 0; i < 100; i++)
            _canvas.DrawArrow(new Vector2(i * 3, 40), new Vector2(i * 3 + 300, 700), Rgba32.Black, new StrokeStyle { Width = 5 });
        return 100;
    }

    [Benchmark(Description = "Fill a 480 segment bezier path")]
    public int FillComplexPath()
    {
        _canvas.FillPath(_complexPath, new Rgba32(10, 120, 90, 90));
        return 1;
    }
}

public static class Program
{
    public static void Main(string[] args)
        => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
            .Run(args, DefaultConfig.Instance.AddJob(Job.Default.WithWarmupCount(2).WithIterationCount(6)));
}
