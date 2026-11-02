using System.Globalization;
using System.Text;

namespace Bit.BevTools;

/// <summary>
/// <c>bevtools check</c> and <c>bevtools compare</c>: the CI side of BevTools, over session exports only. It reads files
/// and writes Markdown; it never connects to a running app (that needs an authenticated transport the page hub
/// deliberately does not offer).
/// </summary>
internal static class BevToolsCli
{
    internal const int Passed = 0;
    internal const int Failed = 1;
    internal const int Unusable = 2;

    private const string Usage = """
        Checks bit BevTools session exports (About → Export session, or BevSessionExporter.ToJson).

        Usage:
          bevtools check <export.json> [--fail-on warning|error|info|none] [--ignore <rule-id>]... [--fail-on-errors] [--report <file.md>]
              Fails on the findings of BevTools' rules in the export: warnings and errors by default.
          bevtools compare <baseline.json> <current.json> [--min-render-change <n>] [--render-change-ratio <r>]
                           [--min-time-change-ms <ms>] [--time-change-ratio <r>] [--report <file.md>]
              Fails when a component type renders more often, or clearly longer, than in the baseline recording.

        Both print Markdown (--report also writes it to a file, such as $GITHUB_STEP_SUMMARY) and exit with
        0 when the check passes, 1 when it fails, 2 when an input cannot be read.
        """;

    public static int Main(string[] args)
    {
        // The report is Markdown with arrows and middle dots; a Windows console starts on its OEM code page.
        try
        {
            Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }
        catch (IOException)
        {
            // No console to set it on: output goes wherever it was redirected, as UTF-8 by default.
        }

        return Run(args, Console.Out, Console.Error);
    }

    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0)
        {
            error.WriteLine(Usage);
            return Unusable;
        }

        if (args[0] == "help" || args.Any(a => a is "-h" or "--help"))
        {
            output.WriteLine(Usage);
            return Passed;
        }

        try
        {
            var (positional, options) = Parse(args.Skip(1));
            if (options.TryGetValue("--report", out var reportPath) && reportPath[^1].Length == 0)
            {
                // An unset variable ($GITHUB_STEP_SUMMARY outside GitHub Actions): said before anything is checked.
                throw new UsageException("--report needs a file path; it was empty.");
            }

            var (markdown, failed) = args[0] switch
            {
                "check" => Check(positional, options),
                "compare" => Compare(positional, options),
                _ => throw new UsageException($"Unknown command '{args[0]}'."),
            };

            output.Write(markdown);
            if (options.TryGetValue("--report", out var report))
            {
                // Appended: $GITHUB_STEP_SUMMARY collects every step's summary in one file.
                File.AppendAllText(report[^1], markdown);
            }

            return failed ? Failed : Passed;
        }
        catch (UsageException ex)
        {
            error.WriteLine(ex.Message);
            error.WriteLine();
            error.WriteLine(Usage);
            return Unusable;
        }
        catch (Exception ex) when (ex is FormatException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            // Not a BevTools export, a section missing, a file that cannot be read or written, a path the OS refuses.
            error.WriteLine(ex.Message);
            return Unusable;
        }
    }

    private static (string Markdown, bool Failed) Check(List<string> files, Dictionary<string, List<string>> options)
    {
        Expect(files, 1, "check <export.json>");
        Allow(options, "--fail-on", "--ignore", "--fail-on-errors", "--report");
        var checkOptions = new BevSessionCheckOptions { FailOnErrors = options.ContainsKey("--fail-on-errors") };
        if (options.TryGetValue("--fail-on", out var failOn))
        {
            // By name: Enum.TryParse would also take "1" or "warning,error", and the latter as a level above every finding.
            checkOptions.FailOn = failOn[^1].ToLowerInvariant() switch
            {
                "none" => null,
                "info" => BevDiagnosticSeverity.Info,
                "warning" => BevDiagnosticSeverity.Warning,
                "error" => BevDiagnosticSeverity.Error,
                _ => throw new UsageException($"--fail-on takes info, warning, error or none, not '{failOn[^1]}'."),
            };
        }

        foreach (var rule in options.GetValueOrDefault("--ignore") ?? [])
        {
            checkOptions.IgnoredRules.Add(rule);
        }

        var result = BevSessionCheck.Check(Read(files[0]), checkOptions);
        return (result.ToMarkdown(), result.Failed);
    }

    private static (string Markdown, bool Failed) Compare(List<string> files, Dictionary<string, List<string>> options)
    {
        Expect(files, 2, "compare <baseline.json> <current.json>");
        Allow(options, "--min-render-change", "--render-change-ratio", "--min-time-change-ms", "--time-change-ratio", "--report");
        var compareOptions = new BevProfileComparisonOptions();
        if (options.ContainsKey("--min-render-change"))
        {
            compareOptions.MinRenderChange = (int)Number(options, "--min-render-change", integer: true);
        }

        if (options.ContainsKey("--render-change-ratio"))
        {
            compareOptions.RenderChangeRatio = Number(options, "--render-change-ratio");
        }

        if (options.ContainsKey("--min-time-change-ms"))
        {
            compareOptions.MinTimeChangeMs = Number(options, "--min-time-change-ms");
        }

        if (options.ContainsKey("--time-change-ratio"))
        {
            compareOptions.TimeChangeRatio = Number(options, "--time-change-ratio");
        }

        var result = BevProfileComparison.Compare(Read(files[0]), Read(files[1]), compareOptions);
        return (result.ToMarkdown(), result.HasRegressions);
    }

    private static (List<string> Positional, Dictionary<string, List<string>> Options) Parse(IEnumerable<string> args)
    {
        var positional = new List<string>();
        var options = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        using var e = args.GetEnumerator();
        while (e.MoveNext())
        {
            var arg = e.Current;
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                positional.Add(arg);
                continue;
            }

            if (!options.TryGetValue(arg, out var values))
            {
                options[arg] = values = [];
            }

            // The one switch; every other option takes a value, which is never the next option.
            if (arg != "--fail-on-errors")
            {
                values.Add(e.MoveNext() && !e.Current.StartsWith("--", StringComparison.Ordinal) ? e.Current : throw new UsageException($"{arg} needs a value."));
            }
        }

        return (positional, options);
    }

    private static void Expect(List<string> files, int count, string usage)
    {
        if (files.Count != count)
        {
            throw new UsageException($"Expected: bevtools {usage}.");
        }
    }

    private static void Allow(Dictionary<string, List<string>> options, params string[] known)
    {
        if (options.Keys.FirstOrDefault(o => !known.Contains(o)) is { } unknown)
        {
            throw new UsageException($"Unknown option {unknown}.");
        }
    }

    private static double Number(Dictionary<string, List<string>> options, string name, bool integer = false)
    {
        var text = options[name][^1];
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) && value >= 0
            && (!integer || (value == Math.Floor(value) && value <= int.MaxValue))
            ? value
            : throw new UsageException($"{name} takes a non-negative {(integer ? "whole number" : "number")}, not '{text}'.");
    }

    private static string Read(string path) =>
        File.Exists(path) ? File.ReadAllText(path) : throw new UsageException($"No such file: {path}");

    private sealed class UsageException(string message) : Exception(message);
}
