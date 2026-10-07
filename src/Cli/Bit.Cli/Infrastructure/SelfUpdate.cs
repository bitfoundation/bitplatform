using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Cli.Commands;
using Bit.Cli.Telemetry;

namespace Bit.Cli.Infrastructure;

public sealed partial class SelfUpdate(CliServices cli)
{
    public const string HandedOffVariable = "BIT_CLI_HANDED_OFF_FROM";

    public const string OptOutVariable = "BIT_CLI_NO_UPDATE";

    public string CurrentVersion { get; init; } = BuildInfo.Version;

    public bool IsOfficialBuild { get; init; } = BuildInfo.IsOfficialBuild;

    public string InstallMethod { get; init; } = CliTelemetry.InstallMethod();

    private string CopiesDirectory => Path.Combine(cli.Environment.BitDirectory, "cli");

    public bool CanHandOff(NewRequest request)
    {
        return IsOfficialBuild
            && InstallMethod is "tool"
            && cli.Environment.IsCI is false
            && request.TemplatePackage is null
            && request.NoUpdate is false
            && cli.Environment.IsVariableTrue(OptOutVariable) is false
            && cli.Environment.GetVariable(HandedOffVariable) is null;
    }

    public async Task<string?> FindTargetAsync(NewRequest request, CancellationToken cancellationToken)
    {
        if (CanHandOff(request) is false)
            return null;

        DeleteOldCopies(request.TemplateVersion);

        if (request.TemplateVersion is { } pinned)
            return IsValidVersion(pinned) && CompareVersions(pinned, CurrentVersion) != 0 ? pinned : null;

        var latest = await cli.Console.RunWithStatusAsync("Looking for a newer bit", _ => LatestVersionAsync(cancellationToken));
        return latest is not null && CompareVersions(latest, CurrentVersion) > 0 ? latest : null;
    }

    public async Task<int?> HandOffAsync(string target, NewRequest request, CancellationToken cancellationToken)
    {
        var updateInstalled = request.TemplateVersion is null;

        cli.Console.Text(updateInstalled
            ? $"bit {target} is out: this run continues with it, and your installed bit updates to it when the run ends."
            : $"bit {target} creates it, since it's the bit of --template-version {target}.");

        var executable = await GetCopyAsync(target, cancellationToken);

        if (executable is null)
        {
            cli.Console.Warn($"Couldn't get bit {target}, so bit {CurrentVersion} goes on.");
            return null;
        }

        if (updateInstalled)
        {
            ScheduleInstalledUpdate(target);
        }

        var result = await cli.Runner.RunAsync(new ProcessSpec
        {
            FileName = executable,
            Arguments = request.Arguments,
            WorkingDirectory = cli.Environment.CurrentDirectory,
            Environment = new Dictionary<string, string?> { [HandedOffVariable] = CurrentVersion },
            Interactive = true
        }, CancellationToken.None);

        return result.NotFound ? null : result.ExitCode;
    }

    public async Task<int> UpdateAsync(CancellationToken cancellationToken)
    {
        if (IsOfficialBuild is false)
        {
            cli.Console.Text("This bit was built from source, so it updates when you build it again.");
            return CliApp.ExitOk;
        }

        var latest = await cli.Console.RunWithStatusAsync("Looking for a newer bit", _ => LatestVersionAsync(cancellationToken));

        if (latest is null)
        {
            cli.Console.Warn($"Couldn't look up {BuildInfo.PackageId} on this machine's NuGet sources.");
            return CliApp.ExitFailed;
        }

        if (CompareVersions(latest, CurrentVersion) <= 0)
        {
            cli.Console.Text($"bit {CurrentVersion} is the newest.");
            return CliApp.ExitOk;
        }

        if (InstallMethod is not "tool")
        {
            cli.Console.Text($"bit {latest} is out. Run it with: dnx {BuildInfo.PackageId}@{latest}");
            return CliApp.ExitOk;
        }

        ScheduleInstalledUpdate(latest);
        cli.Console.Text($"bit {latest} replaces {CurrentVersion} as soon as this command ends.");
        cli.Console.Dim($"Its log: {UpdateLog(latest)}");
        return CliApp.ExitOk;
    }

    public async Task<string?> LatestVersionAsync(CancellationToken cancellationToken)
    {
        var prerelease = CurrentVersion.Contains('-', StringComparison.Ordinal);

        var result = await cli.Runner.RunAsync(new ProcessSpec
        {
            FileName = "dotnet",
            Arguments = ["package", "search", BuildInfo.PackageId, "--exact-match", "--format", "json", .. prerelease ? new[] { "--prerelease" } : []],
            WorkingDirectory = cli.Environment.CurrentDirectory,
            Timeout = TimeSpan.FromSeconds(20)
        }, cancellationToken);

        return ParseVersions(result.Output)
            .Where(v => prerelease || v.Contains('-', StringComparison.Ordinal) is false)
            .OrderDescending(Comparer<string>.Create(CompareVersions))
            .FirstOrDefault();
    }

    public static IReadOnlyList<string> ParseVersions(string output)
    {
        var start = output.IndexOf('{', StringComparison.Ordinal);

        if (start < 0)
            return [];

        try
        {
            using var document = JsonDocument.Parse(output[start..]);

            if (document.RootElement.TryGetProperty("searchResult", out var sources) is false || sources.ValueKind is not JsonValueKind.Array)
                return [];

            return [.. sources.EnumerateArray()
                .Where(s => s.TryGetProperty("packages", out var packages) && packages.ValueKind is JsonValueKind.Array)
                .SelectMany(s => s.GetProperty("packages").EnumerateArray())
                .Where(p => p.TryGetProperty("id", out var id) && string.Equals(id.GetString(), BuildInfo.PackageId, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.TryGetProperty("version", out var version) ? version.GetString() : null)
                .OfType<string>()
                .Where(IsValidVersion)
                .Distinct(StringComparer.OrdinalIgnoreCase)];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static int CompareVersions(string left, string right)
    {
        var (leftCore, leftLabel) = Split(left);
        var (rightCore, rightLabel) = Split(right);
        var core = leftCore.CompareTo(rightCore);

        if (core != 0)
            return core;

        if (leftLabel is null || rightLabel is null)
            return (leftLabel is null).CompareTo(rightLabel is null);

        return string.Compare(leftLabel, rightLabel, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsValidVersion(string version) => VersionPattern().IsMatch(version);

    private async Task<string?> GetCopyAsync(string version, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(CopiesDirectory, version);
        var executable = Path.Combine(directory, cli.Environment.IsWindows ? "bit.exe" : "bit");

        if (File.Exists(executable))
            return executable;

        var result = await cli.Console.RunWithStatusAsync($"Getting bit {version}", progress => cli.Runner.RunAsync(new ProcessSpec
        {
            FileName = "dotnet",
            Arguments = ["tool", "install", BuildInfo.PackageId, "--version", version, "--tool-path", directory],
            WorkingDirectory = cli.Environment.CurrentDirectory,
            OnOutputLine = progress,
            Timeout = TimeSpan.FromMinutes(5)
        }, cancellationToken));

        if (result.Succeeded && File.Exists(executable))
            return executable;

        TryDelete(directory);
        return null;
    }

    private void ScheduleInstalledUpdate(string version)
    {
        var dotnet = cli.Runner.FindExecutable("dotnet") ?? "dotnet";
        var log = UpdateLog(version);
        var processId = Environment.ProcessId;

        Directory.CreateDirectory(cli.Environment.LogsDirectory);

        cli.Runner.StartDetached(cli.Environment.IsWindows
            ? new ProcessSpec
            {
                FileName = "powershell.exe",
                Arguments = ["-NoProfile", "-NonInteractive", "-WindowStyle", "Hidden", "-Command",
                    $"Wait-Process -Id {processId} -ErrorAction SilentlyContinue; & {PowerShellQuote(dotnet)} tool update --global {BuildInfo.PackageId} --version {version} *>&1 | Out-File -LiteralPath {PowerShellQuote(log)} -Encoding utf8"],
                WorkingDirectory = cli.Environment.CurrentDirectory
            }
            : new ProcessSpec
            {
                FileName = "/bin/sh",
                Arguments = ["-c", $"trap '' HUP; while kill -0 {processId} 2>/dev/null; do sleep 1; done; {ShellQuote(dotnet)} tool update --global {BuildInfo.PackageId} --version {version} > {ShellQuote(log)} 2>&1"],
                WorkingDirectory = cli.Environment.CurrentDirectory
            });
    }

    private string UpdateLog(string version) => Path.Combine(cli.Environment.LogsDirectory, $"update-{version}.log");

    private void DeleteOldCopies(string? keep)
    {
        if (Directory.Exists(CopiesDirectory) is false)
            return;

        foreach (var directory in Directory.EnumerateDirectories(CopiesDirectory))
        {
            var version = Path.GetFileName(directory);

            if (string.Equals(version, keep, StringComparison.OrdinalIgnoreCase) is false && (IsValidVersion(version) is false || CompareVersions(version, CurrentVersion) <= 0))
            {
                TryDelete(directory);
            }
        }
    }

    private static void TryDelete(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static (Version Core, string? Label) Split(string version)
    {
        var parts = version.Split('+')[0].Split('-', 2);
        var core = Version.TryParse(parts[0], out var parsed) ? parsed : new Version(0, 0);
        return (new Version(core.Major, core.Minor, Math.Max(core.Build, 0), Math.Max(core.Revision, 0)), parts.Length > 1 ? parts[1] : null);
    }

    private static string PowerShellQuote(string value) => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";

    private static string ShellQuote(string value) => $"'{value.Replace("'", "'\\''", StringComparison.Ordinal)}'";

    [GeneratedRegex(@"^\d+\.\d+\.\d+(\.\d+)?(-[0-9A-Za-z.-]+)?$")]
    private static partial Regex VersionPattern();
}
