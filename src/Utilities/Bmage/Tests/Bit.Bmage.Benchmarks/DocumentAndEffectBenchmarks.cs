using BenchmarkDotNet.Attributes;
using Bit.Bmage.Analysis;
using Bit.Bmage.Codecs;
using Bit.Bmage.Codecs.Gif;
using Bit.Bmage.Codecs.Png;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Operations.Filters;
using System.Numerics;

namespace Bit.Bmage.Benchmarks;

/// <summary>
/// The document tools, the radial blurs, colour lookup tables and palette encoding, one thread, beside vibrance, an
/// existing per-pixel adjustment measured in the same run, so the rows compare across machines.
/// </summary>
[MemoryDiagnoser]
public class DocumentAndEffectBenchmarks
{
    private ImageBuffer _photo = null!;
    private ImageBuffer _page = null!;
    private ColorLookupTable _lut = null!;

    [Params("1920x1080", "3840x2160")]
    public string Size { get; set; } = "1920x1080";

    [GlobalSetup]
    public void Setup()
    {
        var parts = Size.Split('x');
        int width = int.Parse(parts[0]), height = int.Parse(parts[1]);
        _photo = Images.Create(width, height);
        // A page: rows of dark words on paper, lit from the right.
        _page = ImageBuffer.Create(width, height, Rgba32.White);
        var rng = new Random(3);
        for (var top = height / 20; top < height - height / 20; top += height / 40)
        {
            var x = width / 20;
            while (x < width - width / 10)
            {
                var word = rng.Next(width / 60, width / 15);
                _page.Fill(new System.Drawing.Rectangle(x, top, word, height / 90), new Rgba32(30, 30, 40));
                x += word + width / 120;
            }
        }
        for (var y = 0; y < height; y++)
        {
            var row = _page.GetRow(y);
            for (var x = 0; x < width; x++)
            {
                var light = 0.5f + 0.5f * x / width;
                var p = row[x];
                row[x] = new Rgba32((byte)(p.R * light), (byte)(p.G * light), (byte)(p.B * light));
            }
        }
        var tilted = new Operations.Transforms.RotateOperation(-3, Rgba32.White, expandCanvas: false).Apply(_page, Images.OneThread.WithMutation(false));
        _page.Dispose();
        _page = tilted;
        _lut = ColorLookupTable.Create(33, c => new Vector3(MathF.Sqrt(c.X), c.Y * c.Y, 0.1f + 0.8f * c.Z));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _photo.Dispose();
        _page.Dispose();
    }

    private int Run(IImageOperation operation, ImageBuffer? source = null)
    {
        using var result = operation.Apply(source ?? _photo, Images.OneThread.WithMutation(false));
        return result.Width;
    }

    [Benchmark(Baseline = true, Description = "Vibrance (reference)")]
    public int Vibrance() => Run(new VibranceAdjustment(0.4f));

    [Benchmark(Description = "Adaptive threshold, Sauvola, radius 16")]
    public int AdaptiveThreshold() => Run(new AdaptiveThresholdFilter(16), _page);

    [Benchmark(Description = "Adaptive threshold, mean, radius 16")]
    public int AdaptiveThresholdMean() => Run(new AdaptiveThresholdFilter(16, 0.05f, AdaptiveThresholdMethod.Mean), _page);

    [Benchmark(Description = "Document skew estimate")]
    public double Skew() => DocumentSkew.Estimate(_page)?.Angle ?? 0;

    [Benchmark(Description = "Sharpness")]
    public double Sharpness() => ImageSharpness.Measure(_photo);

    [Benchmark(Description = "Zoom blur 0.2")]
    public int ZoomBlur() => Run(new RadialBlurFilter(0.2f));

    [Benchmark(Description = "Spin blur 0.3")]
    public int SpinBlur() => Run(new RadialBlurFilter(0.3f, RadialBlurMode.Spin));

    [Benchmark(Description = "3D LUT, 33 entries an axis")]
    public int Lut() => Run(new ColorLutAdjustment(_lut));

    [Benchmark(Description = "Indexed PNG, 256 colours, dithered")]
    public long IndexedPng() => PngEncoder.Encode(_photo, new ImageExportOptions { MaxColors = 256 }).Length;

    [Benchmark(Description = "1-bit PNG of the thresholded page")]
    public long OneBitPng()
    {
        using var scan = new AdaptiveThresholdFilter(16).Apply(_page, Images.OneThread.WithMutation(false));
        return PngEncoder.Encode(scan, new ImageExportOptions { MaxColors = 2 }).Length;
    }

    [Benchmark(Description = "GIF, 256 colours, dithered")]
    public long Gif() => GifEncoder.Encode(_photo).Length;
}
