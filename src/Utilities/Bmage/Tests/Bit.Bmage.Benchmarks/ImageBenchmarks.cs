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
    /// <summary>
    /// One thread, so these benchmarks keep measuring what the published tables do, and what the browser runs by
    /// default. <see cref="ParallelBenchmarks"/> measures the same operations on every core.
    /// </summary>
    public static OperationContext OneThread { get; } = new(maxDegreeOfParallelism: 1);

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
        using var result = new ResizeOperation(200, 200, ResizeMode.Fit).Apply(_source, Images.OneThread);
        return result.Width;
    }

    private int Run(ResamplingFilter filter)
    {
        using var result = new ResizeOperation(1280, 1280, ResizeMode.Fit, filter).Apply(_source, Images.OneThread);
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
        using var result = new BrightnessAdjustment(0.15f).Apply(_source, Images.OneThread);
        return result.Width;
    }

    [Benchmark(Description = "Four adjustments, unfused")]
    public int AdjustmentsUnfused()
    {
        using var result = ImagePipeline.Create().Brightness(0.1f).Contrast(0.12f).Saturation(0.15f).Gamma(1.1f)
            .Execute(_source, new PipelineExecutionOptions { Optimize = false, MaxDegreeOfParallelism = 1 });
        return result.Width;
    }

    [Benchmark(Description = "Four adjustments, fused")]
    public int AdjustmentsFused()
    {
        using var result = ImagePipeline.Create().Brightness(0.1f).Contrast(0.12f).Saturation(0.15f).Gamma(1.1f)
            .Execute(_source, new PipelineExecutionOptions { Optimize = true, MaxDegreeOfParallelism = 1 });
        return result.Width;
    }

    [Benchmark(Description = "Gaussian blur, radius 8")]
    public int Blur()
    {
        using var result = new BlurFilter(8f).Apply(_source, Images.OneThread);
        return result.Width;
    }

    [Benchmark(Description = "Unsharp mask")]
    public int Sharpen()
    {
        using var result = new SharpenFilter(0.5f, 1.5f).Apply(_source, Images.OneThread);
        return result.Width;
    }

    [Benchmark(Description = "Rotate 90 (lossless)")]
    public int Rotate90()
    {
        using var result = OrientationOperation.Rotate90.Apply(_source, Images.OneThread);
        return result.Width;
    }

    [Benchmark(Description = "Rotate 37 (resampled)")]
    public int RotateArbitrary()
    {
        using var result = new RotateOperation(37).Apply(_source, Images.OneThread);
        return result.Width;
    }

    [Benchmark(Description = "Crop to centre quarter")]
    public int Crop()
    {
        var w = _source.Width / 2;
        var h = _source.Height / 2;
        using var result = new CropOperation(w / 2, h / 2, w, h).Apply(_source, Images.OneThread);
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
        using var result = Upload.Execute(_source, new PipelineExecutionOptions { Optimize = true, MaxDegreeOfParallelism = 1 });
        return result.Width;
    }

    [Benchmark(Description = "Upload pipeline (unoptimised)")]
    public int UploadUnoptimised()
    {
        using var result = Upload.Execute(_source, new PipelineExecutionOptions { Optimize = false, MaxDegreeOfParallelism = 1 });
        return result.Width;
    }

    [Benchmark(Description = "Resize+crop fused")]
    public int FusedResizeCrop()
    {
        using var result = ResizeThenCrop.Execute(_source, new PipelineExecutionOptions { Optimize = true, MaxDegreeOfParallelism = 1 });
        return result.Width;
    }

    [Benchmark(Description = "Resize+crop separate")]
    public int SeparateResizeCrop()
    {
        using var result = ResizeThenCrop.Execute(_source, new PipelineExecutionOptions { Optimize = false, MaxDegreeOfParallelism = 1 });
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

/// <summary>
/// The operations that spread their rows over threads, on one thread and on every core, plus what masks and layers
/// cost on top of the edit they shape. The pixels are identical either way; only the time changes.
/// </summary>
[MemoryDiagnoser]
public class ParallelBenchmarks
{
    private ImageBuffer _photo = null!;
    private ImageBuffer _uhd = null!;
    private ImageBuffer _hd = null!;
    private OperationContext _context = null!;
    private PipelineExecutionOptions _options = null!;
    private Layers.Layer[] _layers = null!;

    /// <summary>One thread, as in the browser by default, and every core this machine has.</summary>
    [Params(1, 0)]
    public int Threads { get; set; }

    private int Degree => Threads == 0 ? Environment.ProcessorCount : Threads;

    [GlobalSetup]
    public void Setup()
    {
        _photo = Images.Create(6000, 4000);
        _uhd = Images.Create(3840, 2160);
        _hd = Images.Create(1920, 1080);
        _context = new OperationContext(maxDegreeOfParallelism: Degree);
        _options = new PipelineExecutionOptions { MaxDegreeOfParallelism = Degree };
        _layers =
        [
            new Layers.FillLayer(new Rgba32(255, 120, 40)) { BlendMode = BlendMode.Screen, Opacity = 0.5f, Mask = Masks.Mask.Gradient(new(0, 0.75f), new(0, 0.3f)) },
            new Layers.AdjustmentLayer(ImagePipeline.Create().Contrast(0.3f).Saturation(0.2f)) { Mask = Masks.Mask.Ellipse() with { Feather = 20 } },
        ];
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _photo.Dispose();
        _uhd.Dispose();
        _hd.Dispose();
    }

    [Benchmark(Description = "Downscale 6000x4000 to 1280 (lanczos3)")]
    public int Downscale()
    {
        using var r = new ResizeOperation(1280, 1280, ResizeMode.Max, ResamplingFilter.Lanczos3).Apply(_photo, _context);
        return r.Width;
    }

    [Benchmark(Description = "Rotate 3840x2160 by 37 degrees (bicubic)")]
    public int Rotate()
    {
        using var r = new RotateOperation(37).Apply(_uhd, _context);
        return r.Width;
    }

    [Benchmark(Description = "Saturation and hue, 3840x2160")]
    public int Adjust()
    {
        using var r = new FusedPointOperation([new SaturationAdjustment(0.3f), new HueAdjustment(20)]).Apply(_uhd, _context);
        return r.Width;
    }

    [Benchmark(Description = "Median radius 1, 1920x1080")]
    public int Median()
    {
        using var r = new MedianFilter(1).Apply(_hd, _context);
        return r.Width;
    }

    [Benchmark(Description = "Exposure through a gradient mask, 3840x2160")]
    public int MaskedAdjustment()
    {
        using var r = ImagePipeline.Create().Masked(Masks.Mask.Gradient(new(0, 0), new(0, 0.5f)), p => p.Exposure(-0.7f)).Execute(_uhd, _options);
        return r.Width;
    }

    [Benchmark(Description = "Two masked layers composited, 3840x2160")]
    public int CompositeLayers()
    {
        using var target = _uhd.Clone();
        Layers.LayerCompositor.Composite(target, _layers, _context);
        return target.Width;
    }
}

/// <summary>
/// The editing tools: colour and tone, the directional filters, canvas geometry, selection and painting, on one thread at
/// the sizes the editor's proxy (1080p) and an export (4K) run them at.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class EditingToolBenchmarks
{
    private ImageBuffer _source = null!;
    private ImageBuffer _product = null!;
    private Layers.PaintLayer _paint = null!;
    private Layers.GroupLayer _group = null!;
    private Layers.Layer[] _clipped = null!;

    [Params("1920x1080", "3840x2160")]
    public string Size { get; set; } = "1920x1080";

    [GlobalSetup]
    public void Setup()
    {
        var parts = Size.Split('x');
        int width = int.Parse(parts[0]), height = int.Parse(parts[1]);
        _source = Images.Create(width, height);
        // A product shot: a subject on a near-white backdrop, for the wand, the background removal and trimming.
        _product = ImageBuffer.Create(width, height, new Rgba32(248, 248, 246));
        _product.Fill(new System.Drawing.Rectangle(width / 4, height / 5, width / 2, height * 3 / 5), new Rgba32(40, 90, 160));
        var strokes = new List<Layers.PaintStroke>();
        var rng = new Random(7);
        for (var i = 0; i < 20; i++)
        {
            var points = Enumerable.Range(0, 40).Select(j => new Vector2(rng.Next(width), rng.Next(height)) * 0.1f + new Vector2(j * width / 50f, i * height / 25f)).ToArray();
            strokes.Add(new Layers.PaintStroke(points, 24, new Rgba32(200, 40, 40)) { Hardness = 0.6f });
        }
        _paint = new Layers.PaintLayer(strokes);
        _group = new Layers.GroupLayer([_paint, new Layers.FillLayer(new Rgba32(250, 220, 160)) { BlendMode = Drawing.BlendMode.Multiply }]) { Isolated = true };
        _clipped = [_paint, new Layers.FillLayer(new Rgba32(40, 90, 200)) { ClipToLayerBelow = true }];
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _source.Dispose();
        _product.Dispose();
    }

    private int Run(IImageOperation operation, ImageBuffer? source = null)
    {
        using var result = operation.Apply(source ?? _source, Images.OneThread.WithMutation(false));
        return result.Width;
    }

    [Benchmark(Baseline = true, Description = "Whites/blacks (tables)")]
    public int WhitesBlacks() => Run(new WhitesBlacksAdjustment(0.3f, -0.2f));

    [Benchmark(Description = "Vibrance (an existing per-pixel adjustment, for reference)")]
    public int Vibrance() => Run(new VibranceAdjustment(0.4f));

    [Benchmark(Description = "HSL, two bands")]
    public int Hsl() => Run(new HslAdjustment().With(HueBand.Blues, new HslShift(-10, 0.3f, -0.2f)).With(HueBand.Greens, new HslShift(15, -0.2f, 0)));

    [Benchmark(Description = "Duotone")]
    public int Duotone() => Run(GradientMapOperation.Duotone(new Rgba32(20, 30, 90), new Rgba32(250, 210, 120)));

    [Benchmark(Description = "Colorize")]
    public int Colorize() => Run(new ColorizeAdjustment(30, 0.4f));

    [Benchmark(Description = "Auto white balance")]
    public int AutoWhiteBalance() => Run(new AutoWhiteBalanceOperation());

    [Benchmark(Description = "Straighten 5 degrees")]
    public int Straighten() => Run(new StraightenOperation(5));

    [Benchmark(Description = "Canvas: 40px border")]
    public int Border() => Run(CanvasSizeOperation.Margins(40, Rgba32.White));

    [Benchmark(Description = "Motion blur 20px, horizontal")]
    public int MotionBlurHorizontal() => Run(new MotionBlurFilter(20, 0));

    [Benchmark(Description = "Motion blur 20px, 30 degrees")]
    public int MotionBlurAngled() => Run(new MotionBlurFilter(20, 30));

    [Benchmark(Description = "High pass, radius 3")]
    public int HighPass() => Run(new HighPassFilter(3));

    [Benchmark(Description = "Remove plain background (corner flood)")]
    public int RemoveBackground() => Run(new Masks.AlphaMaskOperation(new Masks.FloodMask([new(0, 0), new(1, 0), new(0, 1), new(1, 1)]) { Units = Masks.MaskUnits.Relative, Inverted = true }), _product);

    [Benchmark(Description = "Mask expand by 10px")]
    public int Expand()
    {
        using var mask = new Masks.EllipseMask(new System.Drawing.RectangleF(100, 100, 800, 600)).Expand(10).Render(_source.Size, null, Images.OneThread);
        return mask.Width;
    }

    [Benchmark(Description = "Paint layer, 20 strokes")]
    public int Paint()
    {
        using var target = _source.Clone();
        _paint.Composite(target, Images.OneThread);
        return target.Width;
    }

    [Benchmark(Description = "Trim bounds")]
    public int Trim() => Analysis.ContentBounds.Find(_product)!.Value.Width;

    [Benchmark(Description = "Isolated group: paint and a multiplied fill")]
    public int Group()
    {
        using var target = _source.Clone();
        _group.Composite(target, Images.OneThread);
        return target.Width;
    }

    [Benchmark(Description = "A fill clipped to the paint layer")]
    public int Clipped()
    {
        using var target = _source.Clone();
        Layers.LayerCompositor.Composite(target, _clipped, Images.OneThread);
        return target.Width;
    }
}

public static class Program
{
    public static void Main(string[] args)
        => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
            .Run(args, DefaultConfig.Instance.AddJob(Job.Default.WithWarmupCount(2).WithIterationCount(6)));
}
