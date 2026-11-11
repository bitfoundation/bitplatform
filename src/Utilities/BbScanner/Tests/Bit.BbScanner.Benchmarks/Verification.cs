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
        var frames = FrameFactory.Standard();

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
