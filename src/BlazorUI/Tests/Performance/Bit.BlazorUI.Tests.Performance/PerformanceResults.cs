using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// The measurements the performance tests take, written once the run is over to
/// performance-results_{browser}_{framework}_{time}.json - a file of its own for every run, so runs of two browsers sharing
/// a results directory keep both - in the run's results directory (--results-directory, which CI uploads with the TRX;
/// TestResults beside the test app otherwise), together with what they were measured on: the machine, the
/// browser, the SDK and runtime, the commit, the configuration the host was built in and its render mode. The tests'
/// thresholds stay generous, to catch what is catastrophic; a trend is read by comparing these files.
/// </summary>
internal static class PerformanceResults
{
    /// <summary>The name of the file a run written at <paramref name="generatedAt"/> writes: the browser, the framework and the time, in UTC.</summary>
    public static string FileName(DateTimeOffset generatedAt) =>
        FormattableString.Invariant($"performance-results_{PerformanceTestBase.BrowserName}_net{Environment.Version.Major}.{Environment.Version.Minor}_{generatedAt.UtcDateTime:yyyyMMdd-HHmmss}.json");

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static readonly List<Measurement> Measurements = [];

    private static string? _browser;

    /// <summary>One value a test measured. <paramref name="Count"/> is the size of what it measured (components, cycles).</summary>
    public sealed record Measurement(string Test, string Metric, double Value, string Unit, int? Count);

    public static void Add(Measurement measurement, string browser)
    {
        lock (Measurements)
        {
            Measurements.Add(measurement);
            _browser ??= browser;
        }
    }

    /// <summary>Writes what the run measured, while the host it measured is still up to say what it is; nothing when it measured nothing.</summary>
    public static async Task WriteAsync(string baseUrl)
    {
        List<Measurement> measurements;
        lock (Measurements)
        {
            if (Measurements.Count == 0) return;
            measurements = [.. Measurements];
        }

        var generatedAt = DateTimeOffset.UtcNow;
        var report = new
        {
            GeneratedAt = generatedAt,
            Commit = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? await Read("git", "rev-parse HEAD"),
            Machine = new
            {
                Os = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.OSArchitecture.ToString(),
                Cpu = await CpuModel(),
                Cores = Environment.ProcessorCount,
                MemoryGB = Math.Round(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024), 1),
                Ci = Environment.GetEnvironmentVariable("CI") is { Length: > 0 },
            },
            Browser = _browser,
            Sdk = await Read("dotnet", "--version"),
            Runner = new
            {
                Framework = RuntimeInformation.FrameworkDescription,
                Configuration = typeof(PerformanceResults).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration,
            },
            Host = await HostEnvironment(baseUrl),
            Measurements = measurements,
        };

        var directory = ResultsDirectory();
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, FileName(generatedAt)), JsonSerializer.Serialize(report, Json));
    }

    // The folder the run writes its TRX to: TestContext's own folders are the test app's, or with --report-trx a folder of
    // the run's the platform deletes once it is over.
    private static string ResultsDirectory()
    {
        var arguments = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(arguments, "--results-directory");

        return index >= 0 && index + 1 < arguments.Length ? Path.GetFullPath(arguments[index + 1]) : Path.Combine(AppContext.BaseDirectory, "TestResults");
    }

    // The host's own account of itself (/api/environment): a host started by hand, or left running from another run, is
    // measured as it was built, whatever this run's configuration.
    private static async Task<JsonNode?> HostEnvironment(string baseUrl)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            return JsonNode.Parse(await client.GetStringAsync($"{baseUrl}/api/environment"));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    private static async Task<string?> CpuModel()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                return (key?.GetValue("ProcessorNameString") as string)?.Trim();
            }

            if (OperatingSystem.IsLinux())
            {
                return File.ReadLines("/proc/cpuinfo").FirstOrDefault(l => l.StartsWith("model name", StringComparison.Ordinal))?.Split(':', 2)[1].Trim();
            }

            if (OperatingSystem.IsMacOS()) return await Read("sysctl", "-n machdep.cpu.brand_string");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
        }

        return null;
    }

    // The first line a command prints, or null where it cannot be run here or has not finished within 30 seconds: the
    // results are still written without it.
    private static async Task<string?> Read(string fileName, string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (process is null) return null;

            // Both pipes are drained, so neither can fill and stall the process, and the reads and the exit share one
            // deadline: a command that hangs, or leaves a pipe open, is killed rather than holding up the report and the
            // shutdown of the host and the browser after it.
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            try
            {
                await Task.WhenAll(output, error, process.WaitForExitAsync()).WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch (System.TimeoutException)
            {
                process.Kill(entireProcessTree: true);
                return null;
            }

            if (process.ExitCode != 0) return null;

            return (await output).Split('\n')[0].Trim() is { Length: > 0 } line ? line : null;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return null;
        }
    }
}
