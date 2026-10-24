namespace Bit.BbScanner.Tests;

[TestClass]
public class ScratchCompact
{
    [TestMethod]
    public void Trace()
    {
        var log = new List<string>();
        foreach (var symbol in ReferenceSymbols.All.Where(s => s.Id.StartsWith("pdf417compact", StringComparison.Ordinal)))
        {
            foreach (var scale in new[] { 2, 3, 4 })
            {
                using var rendered = symbol.Render(scale);
                var line = $"{symbol.Id} {scale}px:";
                var (still, noisy, live, n) = (0, 0, 0, 0);
                for (var degrees = 0; degrees < 360; degrees += 10)
                {
                    n++;
                    using var rotated = ImageTransforms.Rotate(rendered, degrees);
                    using var blurred = ImageTransforms.Blur(rotated, 0.7);
                    SyntheticImage.AddNoise(blurred, 12, seed: degrees);
                    var options = BbScannerOptions.ForStillImages();
                    options.Formats = BbBarcodeFormat.Pdf417;
                    using var decoder = new BbBarcodeDecoder(options);
                    bool Ok(BbBarcodeResult? r) => r is not null && (symbol.Expected is null || r.Text == symbol.Expected);
                    still += Ok(decoder.Decode(rotated.View, suppressDuplicates: false)) ? 1 : 0;
                    noisy += Ok(decoder.Decode(blurred.View, suppressDuplicates: false)) ? 1 : 0;
                    using var frame = ImageTransforms.Frame(rotated, Math.Max(640, rotated.Width), Math.Max(480, rotated.Height));
                    using var liveDecoder = new BbBarcodeDecoder(new BbScannerOptions { Formats = BbBarcodeFormat.Pdf417, DuplicateSuppressionWindow = TimeSpan.Zero });
                    live += Enumerable.Range(0, 4).Any(_ => Ok(liveDecoder.Decode(frame.View, suppressDuplicates: false))) ? 1 : 0;
                }

                log.Add($"{line} still {still}/{n} noisy {noisy}/{n} live {live}/{n}");
            }
        }

        Assert.Fail(string.Join("\n", log));
    }
}
