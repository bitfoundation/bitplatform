using Bit.BbScanner.TestKit;

namespace Bit.BbScanner.Benchmarks;

/// <summary>
/// Checks that every benchmark frame actually decodes before any timing is taken.
/// </summary>
/// <remarks>
/// A benchmark that silently stops finding the symbol turns into a measurement of the failure
/// path, which is both faster and meaningless. Verifying first makes that impossible to miss.
/// </remarks>
public static class Verification
{
    /// <summary>Decodes each benchmark frame once and reports the outcome.</summary>
    public static void Run()
    {
        var frames = new List<FrameFactory.Frame>
        {
            FrameFactory.QrCode("https://example.com/p/12345", scale: 3),
            FrameFactory.QrCode(new string('A', 300), scale: 3),
            FrameFactory.QrCode("https://example.com/p/12345", scale: 4, noise: 35),
            FrameFactory.DataMatrix("BIT-PLATFORM-BBSCANNER-DM-26"),
            FrameFactory.Linear("Code 128", BbBarcodeFormat.Code128, LinearEncoders.Code128("BARCODE-SCANNER-128"), "BARCODE-SCANNER-128"),
            FrameFactory.Linear("EAN-13", BbBarcodeFormat.Ean13, LinearEncoders.Ean13("4006381333931"), "4006381333931"),
            FrameFactory.Empty(),
        };

        using var decoder = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.All,
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        var failures = 0;
        Console.WriteLine("Verifying benchmark frames");
        Console.WriteLine(new string('-', 72));

        foreach (var frame in frames)
        {
            var result = decoder.Decode(
                frame.Pixels, frame.Width, frame.Height, BbPixelFormat.Gray8, suppressDuplicates: false);

            var ok = frame.Expected is null ? result is null : result?.Text == frame.Expected;
            if (!ok)
            {
                failures++;
            }

            Console.WriteLine(
                $"{(ok ? "ok  " : "FAIL")}  {frame.Name,-28}  {decoder.LastFrame.Total.TotalMilliseconds,7:0.00} ms  {result?.Text ?? "(no symbol)"}");
        }

        Console.WriteLine(new string('-', 72));
        if (failures > 0)
        {
            Console.WriteLine($"{failures} benchmark frames did not decode as expected; timings below are not meaningful.");
        }

        Console.WriteLine();
    }
}
