using BenchmarkDotNet.Attributes;
using System.Numerics;

namespace Bit.Bmage.Benchmarks;

/// <summary>
/// The document tools, the radial blurs, colour lookup tables and palette encoding, one thread, beside vibrance, an
/// existing per-pixel adjustment measured in the same run, so the rows compare across machines.
/// </summary>
[MemoryDiagnoser]
public class DocumentAndEffectBenchmarks
{
    private BmageImageBuffer _photo = null!;
    private BmageImageBuffer _page = null!;
    private BmageColorLookupTable _lut = null!;

    [Params("1920x1080", "3840x2160")]
    public string Size { get; set; } = "1920x1080";

    [GlobalSetup]
    public void Setup()
    {
        var parts = Size.Split('x');
        int width = int.Parse(parts[0]), height = int.Parse(parts[1]);
        _photo = Images.Create(width, height);
        // A page: rows of dark words on paper, lit from the right.
        _page = BmageImageBuffer.Create(width, height, BmageRgba32.White);
        var rng = new Random(3);
        for (var top = height / 20; top < height - height / 20; top += height / 40)
        {
            var x = width / 20;
            while (x < width - width / 10)
            {
                var word = rng.Next(width / 60, width / 15);
                _page.Fill(new System.Drawing.Rectangle(x, top, word, height / 90), new BmageRgba32(30, 30, 40));
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
                row[x] = new BmageRgba32((byte)(p.R * light), (byte)(p.G * light), (byte)(p.B * light));
            }
        }
        var tilted = new BmageRotateOperation(-3, BmageRgba32.White, expandCanvas: false).Apply(_page, Images.OneThread.WithMutation(false));
        _page.Dispose();
        _page = tilted;
        _lut = BmageColorLookupTable.Create(33, c => new Vector3(MathF.Sqrt(c.X), c.Y * c.Y, 0.1f + 0.8f * c.Z));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _photo.Dispose();
        _page.Dispose();
    }

    private int Run(IBmageImageOperation operation, BmageImageBuffer? source = null)
    {
        using var result = operation.Apply(source ?? _photo, Images.OneThread.WithMutation(false));
        return result.Width;
    }

    [Benchmark(Baseline = true, Description = "Vibrance (reference)")]
    public int Vibrance() => Run(new BmageVibranceAdjustment(0.4f));

    [Benchmark(Description = "Adaptive threshold, Sauvola, radius 16")]
    public int AdaptiveThreshold() => Run(new BmageAdaptiveThresholdFilter(16), _page);

    [Benchmark(Description = "Adaptive threshold, mean, radius 16")]
    public int AdaptiveThresholdMean() => Run(new BmageAdaptiveThresholdFilter(16, 0.05f, BmageAdaptiveThresholdMethod.Mean), _page);

    [Benchmark(Description = "Document skew estimate")]
    public double Skew() => BmageDocumentSkew.Estimate(_page)?.Angle ?? 0;

    [Benchmark(Description = "Sharpness")]
    public double Sharpness() => BmageImageSharpness.Measure(_photo);

    [Benchmark(Description = "Zoom blur 0.2")]
    public int ZoomBlur() => Run(new BmageRadialBlurFilter(0.2f));

    [Benchmark(Description = "Spin blur 0.3")]
    public int SpinBlur() => Run(new BmageRadialBlurFilter(0.3f, BmageRadialBlurMode.Spin));

    [Benchmark(Description = "3D LUT, 33 entries an axis")]
    public int Lut() => Run(new BmageColorLutAdjustment(_lut));

    [Benchmark(Description = "Indexed PNG, 256 colours, dithered")]
    public long IndexedPng() => BmagePngEncoder.Encode(_photo, new BmageImageExportOptions { MaxColors = 256 }).Length;

    [Benchmark(Description = "1-bit PNG of the thresholded page")]
    public long OneBitPng()
    {
        using var scan = new BmageAdaptiveThresholdFilter(16).Apply(_page, Images.OneThread.WithMutation(false));
        return BmagePngEncoder.Encode(scan, new BmageImageExportOptions { MaxColors = 2 }).Length;
    }

    [Benchmark(Description = "GIF, 256 colours, dithered")]
    public long Gif() => BmageGifEncoder.Encode(_photo).Length;
}
