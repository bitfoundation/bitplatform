using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Bit.BbScanner.Benchmarks;

/// <summary>
/// A performance gate quick enough for every pull request: each standard frame is decoded the way a live scanner
/// decodes it, and its time and allocations are compared with the baseline in <c>budgets.json</c>.
/// </summary>
/// <remarks>
/// <para>
/// BenchmarkDotNet's numbers are precise and take many minutes; this takes seconds and only has to catch what a
/// review would miss - a stage that became several times slower, or a frame that started allocating. A frame's time
/// is not compared in milliseconds, which depend on the machine, but as a multiple of a fixed piece of work timed on
/// the same machine in the same run, so a slower CI runner moves both alike. That ratio still varies between processors,
/// so a case fails only past <see cref="TimeTolerance"/> times its baseline. Allocations do not depend on the machine
/// and are held much closer, to <see cref="AllocationTolerance"/>.
/// </para>
/// <para>
/// <c>--budget</c> checks and exits with 1 on a regression; <c>--update-budgets</c> measures and rewrites the baseline,
/// after a change that is meant to move the numbers.
/// </para>
/// </remarks>
public static class BudgetCheck
{
    /// <summary>
    /// A case fails when its time, relative to the calibration work, exceeds its baseline this many times. The baseline
    /// is measured on a developer's machine and checked on CI runners with other processors, whose ratios differ by a
    /// fraction, so the line is drawn where only a real regression lands.
    /// </summary>
    public const double TimeTolerance = 3.0;

    /// <summary>A case is reported, without failing, when its time exceeds its baseline this many times.</summary>
    public const double TimeWarning = 2.0;

    /// <summary>A case fails when it allocates more than this many times its baseline per frame, plus <see cref="AllocationSlack"/>.</summary>
    public const double AllocationTolerance = 1.25;

    /// <summary>Bytes a frame may allocate beyond its scaled baseline, for runtime noise such as a resized collection.</summary>
    public const long AllocationSlack = 1024;

    // Long enough for tiered compilation to have replaced every method the frame runs with its optimized code: a
    // method is recompiled after 30 calls, in the background, after a 100 ms delay.
    private static readonly TimeSpan Warmup = TimeSpan.FromMilliseconds(600);
    private const int MinimumWarmupRuns = 60;
    private const int Samples = 61;

    /// <summary>
    /// Frames a sample times together. A live scanner does some work only every few frames - the enlarged second look a
    /// matrix symbol gets every fourth frame that held nothing, the angles MicroPDF417 tries in turn - and a sample of
    /// four consecutive frames always holds one of each, where the quartile of single frames would never see them.
    /// </summary>
    private const int FramesPerSample = 4;

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    /// <summary>Runs the check, or rewrites the baseline when <paramref name="update"/> is set.</summary>
    /// <returns>The process exit code: 0 when every case is within budget.</returns>
    public static int Run(bool update)
    {
        var calibration = Calibrate();
        var cases = Cases().ToDictionary(c => c.Name, StringComparer.Ordinal);
        var measured = new SortedDictionary<string, Budget>(StringComparer.Ordinal);

        // A baseline is the best of three rounds, so that one busy moment does not loosen a budget for good.
        for (var round = 0; round < (update ? 3 : 1); round++)
        {
            foreach (var (name, frame, options) in cases.Values)
            {
                var budget = Measure(frame, options, calibration);
                measured[name] = measured.TryGetValue(name, out var best) && best.RelativeTime < budget.RelativeTime
                    ? best with { BytesPerFrame = budget.BytesPerFrame }
                    : budget;
            }
        }

        var path = BaselinePath();
        if (update)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(new BudgetFile(
                "Each case: the lower quartile of the time a frame takes to decode, as a multiple of the calibration work, and the bytes it allocates. " +
                "Regenerate with: dotnet run -c Release --project Tests/Bit.BbScanner.Benchmarks -- --update-budgets",
                measured), Json) + Environment.NewLine);
            Console.WriteLine($"Wrote {measured.Count} budgets to {path}.");
            return 0;
        }

        var baseline = JsonSerializer.Deserialize<BudgetFile>(File.ReadAllText(path))
            ?? throw new InvalidOperationException($"{path} is empty.");

        Console.WriteLine($"Calibration work: {calibration * 1000:0.000} ms");
        Console.WriteLine($"{"Case",-44} {"time x",8} {"budget",8} {"bytes",8} {"budget",8}  ");
        Console.WriteLine(new string('-', 84));

        var failures = 0;
        foreach (var (name, first) in measured)
        {
            var budget = first;
            if (!baseline.Cases.TryGetValue(name, out var expected))
            {
                Console.WriteLine($"{name,-44} has no baseline; run --update-budgets.");
                failures++;
                continue;
            }

            var timeLimit = expected.RelativeTime * TimeTolerance;
            var byteLimit = (long)(expected.BytesPerFrame * AllocationTolerance) + AllocationSlack;

            // A case over its time budget is measured twice more, and only fails when it stays over: a regression is
            // slow every time, a busy machine only now and then.
            for (var retry = 0; retry < 2 && budget.RelativeTime > timeLimit; retry++)
            {
                var (_, frame, options) = cases[name];
                var again = Measure(frame, options, Calibrate());
                budget = again.RelativeTime < budget.RelativeTime ? again : budget;
            }

            var slow = budget.RelativeTime > timeLimit;
            var slower = !slow && budget.RelativeTime > expected.RelativeTime * TimeWarning;
            var heavy = budget.BytesPerFrame > byteLimit;
            failures += slow || heavy ? 1 : 0;

            Console.WriteLine(
                $"{name,-44} {budget.RelativeTime,8:0.00} {timeLimit,8:0.00} {budget.BytesPerFrame,8} {byteLimit,8}  " +
                (slow ? "SLOWER " : slower ? "slower (warning only) " : string.Empty) + (heavy ? "ALLOCATES MORE" : string.Empty));
        }

        foreach (var name in baseline.Cases.Keys.Except(measured.Keys))
        {
            Console.WriteLine($"{name,-44} is in the baseline but no longer measured; run --update-budgets.");
            failures++;
        }

        Console.WriteLine(new string('-', 84));
        Console.WriteLine(failures == 0 ? "Every case is within budget." : $"{failures} case(s) over budget.");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>Every standard frame with every symbology looked for, and the empty frame as a live scanner sees it.</summary>
    private static IEnumerable<(string Name, FrameFactory.Frame Frame, BbScannerOptions Options)> Cases()
    {
        foreach (var frame in FrameFactory.Standard())
        {
            yield return ($"{frame.Name}, every format", frame, new BbScannerOptions { Formats = BbBarcodeFormat.All });
        }

        var empty = FrameFactory.Empty();
        yield return ("Empty frame, live defaults", empty, new BbScannerOptions());
        yield return ("Empty frame, still image defaults", empty, BbScannerOptions.ForStillImages());

        var everything = BbScannerOptions.ForStillImages();
        everything.Formats = BbBarcodeFormat.All;
        yield return ("Empty frame, still image, every format", empty, everything);
    }

    private static Budget Measure(FrameFactory.Frame frame, BbScannerOptions options, double calibration)
    {
        options.DuplicateSuppressionWindow = TimeSpan.Zero;
        using var decoder = new BbBarcodeDecoder(options);

        WarmUp(() => Decode(decoder, frame));

        var times = new double[Samples];
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Samples; i++)
        {
            var start = Stopwatch.GetTimestamp();
            for (var f = 0; f < FramesPerSample; f++)
            {
                Decode(decoder, frame);
            }

            times[i] = Stopwatch.GetElapsedTime(start).TotalSeconds / FramesPerSample;
        }

        var bytes = (GC.GetAllocatedBytesForCurrentThread() - allocatedBefore) / (Samples * FramesPerSample);
        return new Budget(Math.Round(LowerQuartile(times) / calibration, 3), bytes);
    }

    private static void Decode(BbBarcodeDecoder decoder, FrameFactory.Frame frame)
    {
        var result = decoder.Decode(frame.Pixels, frame.Width, frame.Height, BbPixelFormat.Gray8, suppressDuplicates: false);
        if (result?.Text != frame.Expected)
        {
            // A frame that stopped decoding is measured on its failure path, which is no measurement at all.
            throw new InvalidOperationException($"{frame.Name} decoded as '{result?.Text}', not '{frame.Expected}'.");
        }
    }

    /// <summary>
    /// The lower quartile time of a fixed piece of work the decoder does not share code with: a pass over a frame's worth of
    /// bytes with data dependent branches, and a sort - the kind of integer work, memory traffic and branching the
    /// decoder's stages are made of.
    /// </summary>
    private static double Calibrate()
    {
        var random = new Random(42);
        var pixels = new byte[640 * 480];
        random.NextBytes(pixels);
        var keys = new int[16384];
        var source = new int[keys.Length];
        for (var i = 0; i < source.Length; i++)
        {
            source[i] = random.Next();
        }

        var sink = 0L;
        double Work()
        {
            var start = Stopwatch.GetTimestamp();
            var sum = 0L;
            for (var i = 1; i < pixels.Length; i++)
            {
                sum += pixels[i] > pixels[i - 1] ? pixels[i] - pixels[i - 1] : pixels[i] ^ 0x55;
            }

            source.CopyTo(keys, 0);
            Array.Sort(keys);
            sink += sum + keys[keys.Length / 2];
            return Stopwatch.GetElapsedTime(start).TotalSeconds;
        }

        WarmUp(() => Work());

        var times = new double[Samples * 2];
        for (var i = 0; i < times.Length; i++)
        {
            times[i] = Work();
        }

        GC.KeepAlive(sink);
        return LowerQuartile(times);
    }

    private static void WarmUp(Action run)
    {
        var start = Stopwatch.GetTimestamp();
        for (var runs = 0; runs < MinimumWarmupRuns || Stopwatch.GetElapsedTime(start) < Warmup; runs++)
        {
            run();
        }
    }

    // The lower quartile rather than the median or the mean: what a frame costs when nothing else - another process, a
    // collection, a timer interrupt - gets in its way, which is what a code change moves and noise does not.
    private static double LowerQuartile(double[] values)
    {
        Array.Sort(values);
        return values[values.Length / 4];
    }

    private static string BaselinePath([CallerFilePath] string source = "") => Path.Combine(Path.GetDirectoryName(source)!, "budgets.json");

    private sealed record Budget(double RelativeTime, long BytesPerFrame);

    private sealed record BudgetFile(string About, SortedDictionary<string, Budget> Cases);
}
