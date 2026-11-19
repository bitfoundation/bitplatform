using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Cli.Commands;
using Bit.Cli.Projects;
using Bit.Cli.Telemetry;

namespace Bit.Cli.Infrastructure;

public sealed partial class SelfUpdate(CliServices cli)
{
    public const string HandedOffVariable = "BIT_CLI_HANDED_OFF_FROM";

    public const string OptOutVariable = "BIT_CLI_NO_UPDATE";

    public const string ProjectMarker = "used-by-a-project";

    public static readonly TimeSpan ProjectCopyLifetime = TimeSpan.FromDays(30);

    public static readonly IReadOnlySet<string> CommandsWithoutAProject = new HashSet<string> { "new", "remove", "decode", "update", "about", "telemetry", Tools.AdministratorSession.CommandName };

    private readonly HashSet<string> checkedProjects = new(cli.Environment.IsLinux ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

    public string CurrentVersion { get; init; } = BuildInfo.Version;

    public bool IsOfficialBuild { get; init; } = BuildInfo.IsOfficialBuild;

    public string InstallMethod { get; init; } = CliTelemetry.InstallMethod();

    public string BaseDirectory { get; init; } = AppContext.BaseDirectory;

    public TimeSpan HandOffGrace { get; init; } = TimeSpan.FromSeconds(8);

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

    public async Task<int?> HandOffToProjectBitAsync(string directory, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        try
        {
            return await HandOffToFittingBitAsync(directory, arguments, cancellationToken);
        }
        catch (Exception exp) when (exp is not OperationCanceledException)
        {
            cli.Telemetry.RecordException(null, exp);
            cli.Log.Write(exp.ToString());
            return null;
        }
    }

    private async Task<int?> HandOffToFittingBitAsync(string directory, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        if (IsOfficialBuild is false || cli.Environment.GetVariable(HandedOffVariable) is not null)
            return null;

        if (ProjectBit.Find(directory) is not { } project || project.Fits(CurrentVersion) || checkedProjects.Add(project.FilePath) is false)
            return null;

        var tooOld = project.NeedsNewerThan(CurrentVersion);
        var needs = tooOld ? $"needs bit {project.MinVersion} or newer" : $"works with bit up to {project.MaxVersion}";

        cli.Log.Write($"{project.FilePath}: this project {needs}, and this is bit {CurrentVersion}");

        if (InstallMethod is not "tool" || cli.Environment.IsCI || cli.Environment.IsVariableTrue(OptOutVariable))
        {
            cli.Console.Warn($"This project {needs}, and this is bit {CurrentVersion}.");
            return null;
        }

        var target = PickFitting(project, Copies());

        if (target is null)
        {
            var prerelease = new[] { CurrentVersion, project.MinVersion, project.MaxVersion }.Any(v => v is not null && IsPrerelease(v));
            target = PickFitting(project, await cli.Console.RunWithStatusAsync(tooOld ? "Looking for a newer bit" : "Looking for an older bit", _ => FindVersionsAsync(prerelease, cancellationToken)));
        }

        if (target is null)
        {
            cli.Console.Warn($"This project {needs}, and bit couldn't find one, so bit {CurrentVersion} goes on.");
            return null;
        }

        var updateInstalled = tooOld && File.Exists(CopyExecutable(target)) is false && (IsPrerelease(CurrentVersion) || IsPrerelease(target) is false);

        cli.Console.Text($"This project {needs}, so bit {target} runs this{(updateInstalled ? ", and your installed bit updates to it when the run ends" : "")}.");

        var executable = await GetCopyAsync(target, cancellationToken);

        if (executable is null)
        {
            cli.Console.Warn($"Couldn't get bit {target}, so bit {CurrentVersion} goes on.");
            return null;
        }

        MarkUsedByAProject(target);

        if (updateInstalled)
        {
            ScheduleInstalledUpdate(target);
        }

        using var stopIt = new CancellationTokenSource();
        using var whenCanceled = cancellationToken.Register(() => stopIt.CancelAfter(HandOffGrace));

        var result = await cli.Runner.RunAsync(new ProcessSpec
        {
            FileName = executable,
            Arguments = arguments,
            WorkingDirectory = cli.Environment.CurrentDirectory,
            Environment = new Dictionary<string, string?> { [HandedOffVariable] = CurrentVersion },
            Interactive = true
        }, stopIt.Token);

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

        if (InstallMethod is "tool-path")
        {
            var toolPath = BaseDirectory[..BaseDirectory.Replace('\\', '/').IndexOf("/.store/", StringComparison.OrdinalIgnoreCase)];
            cli.Console.Text($"bit {latest} is out. Update it with: dotnet tool update {BuildInfo.PackageId} --tool-path {ProcessSpec.Quote(toolPath)} --version {latest}");
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
        var prerelease = IsPrerelease(CurrentVersion);

        return (await FindVersionsAsync(prerelease, cancellationToken))
            .Where(v => prerelease || IsPrerelease(v) is false)
            .OrderDescending(Comparer<string>.Create(CompareVersions))
            .FirstOrDefault();
    }

    private async Task<IReadOnlyList<string>> FindVersionsAsync(bool prerelease, CancellationToken cancellationToken)
    {
        var result = await cli.Runner.RunAsync(new ProcessSpec
        {
            FileName = "dotnet",
            Arguments = ["package", "search", BuildInfo.PackageId, "--exact-match", "--format", "json", .. prerelease ? new[] { "--prerelease" } : []],
            WorkingDirectory = cli.Environment.CurrentDirectory,
            Timeout = TimeSpan.FromSeconds(20)
        }, cancellationToken);

        return ParseVersions(result.Output);
    }

    private string? PickFitting(ProjectBit project, IEnumerable<string> versions)
    {
        var fitting = versions.Where(project.Fits).OrderDescending(Comparer<string>.Create(CompareVersions)).ToList();

        return (IsPrerelease(CurrentVersion) ? null : fitting.FirstOrDefault(v => IsPrerelease(v) is false)) ?? fitting.FirstOrDefault();
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

    public static bool IsPrerelease(string version) => version.Split('+')[0].Contains('-', StringComparison.Ordinal);

    private string CopyExecutable(string version) => Path.Combine(CopiesDirectory, version, cli.Environment.IsWindows ? "bit.exe" : "bit");

    private IReadOnlyList<string> Copies()
    {
        try
        {
            return Directory.Exists(CopiesDirectory)
                ? [.. Directory.EnumerateDirectories(CopiesDirectory).Select(d => Path.GetFileName(d)).Where(v => IsValidVersion(v) && File.Exists(CopyExecutable(v)))]
                : [];
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private async Task<string?> GetCopyAsync(string version, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(CopiesDirectory, version);
        var executable = CopyExecutable(version);

        if (File.Exists(executable))
            return executable;

        using var installing = await LockCopiesAsync(cancellationToken);

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

        DeleteCopy(directory);
        return null;
    }

    private Task<FileStream?> LockCopiesAsync(CancellationToken cancellationToken) => LockAsync(Path.Combine(CopiesDirectory, ".lock"), TimeSpan.FromMinutes(5), cancellationToken);

    public static async Task<FileStream?> LockAsync(string file, TimeSpan patience, CancellationToken cancellationToken)
    {
        var giveUpAt = DateTime.UtcNow + patience;

        while (true)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                return new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException exp) when (IsHeldByAnotherProcess(exp) && DateTime.UtcNow < giveUpAt)
            {
                await Task.Delay(250, cancellationToken);
            }
            catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }
    }

    private static bool IsHeldByAnotherProcess(IOException exp) => OperatingSystem.IsWindows() ? (exp.HResult & 0xFFFF) is 32 or 33 : exp.HResult is 11 or 35;

    private void MarkUsedByAProject(string version)
    {
        try
        {
            File.WriteAllBytes(Path.Combine(CopiesDirectory, version, ProjectMarker), []);
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
        }
    }

    private bool IsUsedByAProject(string directory)
    {
        try
        {
            return DateTime.UtcNow - File.GetLastWriteTimeUtc(Path.Combine(directory, ProjectMarker)) < ProjectCopyLifetime;
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return true;
        }
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

        foreach (var directory in Directory.EnumerateDirectories(CopiesDirectory).ToList())
        {
            var version = Path.GetFileName(directory);

            if (string.Equals(version, keep, StringComparison.OrdinalIgnoreCase))
                continue;

            if (IsValidVersion(version) is false)
            {
                TryDelete(directory);
            }
            else if (CompareVersions(version, CurrentVersion) <= 0 && IsUsedByAProject(directory) is false)
            {
                DeleteCopy(directory);
            }
        }
    }

    private void DeleteCopy(string directory)
    {
        var removing = Path.Combine(CopiesDirectory, $".removing-{Guid.NewGuid():N}");

        try
        {
            Directory.Move(directory, removing);
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return;
        }

        TryDelete(removing);
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
