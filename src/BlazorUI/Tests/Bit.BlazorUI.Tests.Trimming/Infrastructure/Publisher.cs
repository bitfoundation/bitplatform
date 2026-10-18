using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Bit.BlazorUI.Tests.Trimming.Infrastructure;

/// <summary>
/// Publishes a project of the source tree with a set of properties, once per set: the tests asking about the same
/// scenario read the same publish.
/// </summary>
public static class Publisher
{
    private static readonly Dictionary<string, PublishResult> Results = [];
    private static readonly SemaphoreSlim Gate = new(1, 1);

    /// <summary>The src/BlazorUI folder: the first one above the test binaries holding Bit.BlazorUI/Bit.BlazorUI.csproj.</summary>
    public static string Root { get; } = FindRoot();

    public const string Fixture = "Tests/Bit.BlazorUI.Tests.Trimming.Fixture/Bit.BlazorUI.Tests.Trimming.Fixture.csproj";
    public const string Consumer = "Tests/Bit.BlazorUI.Tests.Consumer/Bit.BlazorUI.Tests.Consumer.csproj";
    public const string Hosted = "Tests/Bit.BlazorUI.Tests.Trimming.Hosted/Bit.BlazorUI.Tests.Trimming.Hosted.csproj";

    /// <summary>Publishes in Release with the given MSBuild properties ("Name=Value"), or hands back the publish already made.</summary>
    public static PublishResult Publish(string project, params string[] properties)
    {
        var key = project + "|" + string.Join("|", properties.OrderBy(p => p, StringComparer.Ordinal));

        Gate.Wait();
        try
        {
            if (Results.TryGetValue(key, out var cached)) return cached;

            var output = Path.Combine(Path.GetTempPath(), "bit-blazorui-trimming", Hash(key));
            if (Directory.Exists(output)) Directory.Delete(output, recursive: true);

            // No node reuse: a node kept from an earlier build holds the task assembly it loaded then, and the source
            // tree's task is rebuilt along with the packages.
            var arguments = new List<string> { "publish", Path.Combine(Root, project), "-c", "Release", "-nologo", "-nodeReuse:false", "-o", output };
            arguments.AddRange(properties.Select(p => $"-p:{p}"));

            var result = Run("dotnet", arguments, Root, TimeSpan.FromMinutes(20));
            var published = new PublishResult(output, result.ExitCode, result.Output);
            Results[key] = published;
            return published;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static (int ExitCode, string Output) Run(string fileName, IEnumerable<string> arguments, string workingDirectory, TimeSpan timeout, IDictionary<string, string>? environment = null)
    {
        var info = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        // A test run's own MSBuild settings (the SDK it was started from, a node it attached to) are not the child's.
        info.Environment.Remove("MSBuildExtensionsPath");
        info.Environment.Remove("MSBuildSDKsPath");
        info.Environment.Remove("MSBUILD_EXE_PATH");
        if (environment is not null) foreach (var (name, value) in environment) info.Environment[name] = value;

        using var process = Process.Start(info)!;
        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (output) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (output) output.AppendLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (process.WaitForExit(timeout) is false)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"{fileName} {string.Join(' ', arguments)} did not finish in {timeout}.\n{output}");
        }

        process.WaitForExit();
        return (process.ExitCode, output.ToString());
    }

    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..16].ToLowerInvariant();

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Bit.BlazorUI", "Bit.BlazorUI.csproj"))) return directory.FullName;
        }

        throw new InvalidOperationException($"No src/BlazorUI folder above {AppContext.BaseDirectory}; these tests publish the source tree's projects.");
    }
}
