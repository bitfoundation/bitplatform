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
/// The measurements the performance tests take, written once the run is over to performance-results_{framework}.json in
/// the run's results directory (--results-directory, which CI uploads with the TRX; TestResults beside the test app
/// otherwise), together with what they were measured on: the machine, the
/// browser, the SDK and runtime, the commit, the configuration the host was built in and its render mode. The tests'
/// thresholds stay generous, to catch what is catastrophic; a trend is read by comparing these files.
/// </summary>
internal static class PerformanceResults
{
    public static string FileName { get; } = $"performance-results_net{Environment.Version.Major}.{Environment.Version.Minor}.json";

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

        var report = new
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Commit = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? Read("git", "rev-parse HEAD"),
            Machine = new
            {
                Os = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.OSArchitecture.ToString(),
                Cpu = CpuModel(),
                Cores = Environment.ProcessorCount,
                MemoryGB = Math.Round(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024), 1),
                Ci = Environment.GetEnvironmentVariable("CI") is { Length: > 0 },
            },
            Browser = _browser,
            Sdk = Read("dotnet", "--version"),
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
        await File.WriteAllTextAsync(Path.Combine(directory, FileName), JsonSerializer.Serialize(report, Json));
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

    private static string? CpuModel()
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

            if (OperatingSystem.IsMacOS()) return Read("sysctl", "-n machdep.cpu.brand_string");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
        }

        return null;
    }

    // The first line a command prints, or null where it cannot be run here: the results are still written without it.
    private static string? Read(string fileName, string arguments)
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

            // Both pipes are drained, so neither can fill and stall the process.
            var error = process.StandardError.ReadToEndAsync();
            var output = process.StandardOutput.ReadToEnd();
            _ = error.Result;
            if (process.WaitForExit(TimeSpan.FromSeconds(30)) is false || process.ExitCode != 0) return null;

            return output.Split('\n')[0].Trim() is { Length: > 0 } line ? line : null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }
}
