using System.Diagnostics;
using System.Globalization;

namespace ButilTests.Benchmarks;

/// <summary>
/// The download-weight half of the suite: how many bytes an app pays for each module it calls into.
/// </summary>
/// <remarks>
/// This is the benchmark that matters most to a page's first load, and the only one in the suite
/// whose numbers are exactly reproducible - no browser, no scheduler, no machine. It runs
/// <c>weigh-modules.mjs</c> (see that file for why the measurement lives in node) and holds the
/// results against <see cref="Budgets"/>.
/// <br/>
/// A module's figure is its <em>closure</em>: the module plus every dependency its lazy-loaded file
/// inlines. That is the unit an app downloads, so it is the unit worth budgeting - a two-line module
/// that pulls in a large dependency costs its callers the whole dependency.
/// </remarks>
internal static class ModuleWeight
{
    internal sealed record Row(string Name, int OwnMinified, int ClosureMinified, int Gzip, int Brotli, int DependencyCount);

    internal static void Run(Report report, string butilProjectDirectory)
    {
        Report.Section("Module download weight (minified, brotli)");

        var rows = Measure(butilProjectDirectory, out var total);
        if (rows.Count == 0)
        {
            report.Fail("weigh-modules.mjs produced no measurements.");
            return;
        }

        // Sorted heaviest first by the script, so the head of the list is the interesting part. Ten
        // is enough to see a shape without turning the report into a wall of 170 near-identical lines.
        Report.Line("  heaviest modules:");
        foreach (var row in rows.Take(10))
        {
            Report.Line($"    {row.Name,-28} {row.Brotli,6} B brotli  {row.ClosureMinified,7} B min  ({row.DependencyCount} deps)");
        }

        var brotliSorted = rows.Select(row => row.Brotli).OrderBy(value => value).ToArray();
        var p90 = brotliSorted[(int)(brotliSorted.Length * 0.9)];
        var median = brotliSorted[brotliSorted.Length / 2];

        Report.Line("");
        Report.Info("modules", rows.Count, "");
        Report.Info("median module closure", median, "B brotli");
        report.AtMost("p90 module closure", p90, Budgets.MaxP90ModuleClosureBrotli, "B brotli");
        report.AtMost($"heaviest module closure ({rows[0].Name})", rows[0].Brotli, Budgets.MaxModuleClosureBrotli, "B brotli");

        if (total is not null)
        {
            report.AtMost("full bundle", total.Brotli, Budgets.MaxBundleBrotli, "B brotli");
            report.AtMost("full bundle", total.ClosureMinified, Budgets.MaxBundleMinified, "B min");

            // Not a budget, a perspective: the whole point of lazy scripts is that a page pays the
            // median rather than the total, and printing the ratio is what makes that visible.
            Report.Info("bundle / median module", (double)total.Brotli / median, "x");
        }
    }

    private static List<Row> Measure(string butilProjectDirectory, out Row? total)
    {
        var script = Path.Combine(AppContext.BaseDirectory, "weigh-modules.mjs");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "node",
                ArgumentList = { script, butilProjectDirectory },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };

        process.Start();

        // Both pipes drained at once. Reading them one after the other deadlocks as soon as node
        // writes more to stderr than the pipe buffers while stdout is still open - a version-mismatch
        // notice per chunk is enough - because the child then blocks on stderr while this side
        // blocks on stdout.
        var errorTask = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        var error = errorTask.GetAwaiter().GetResult();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"weigh-modules.mjs failed ({process.ExitCode}): {error.Trim()}");

        var rows = new List<Row>();
        total = null;

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim().Split(',');
            if (parts.Length != 6 || parts[0] == "module") continue;

            var row = new Row(parts[0], Int(parts[1]), Int(parts[2]), Int(parts[3]), Int(parts[4]), Int(parts[5]));
            if (row.Name == "TOTAL") total = row;
            else rows.Add(row);
        }

        return rows;

        static int Int(string value) => int.Parse(value, CultureInfo.InvariantCulture);
    }
}
