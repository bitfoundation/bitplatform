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

    public const string UpdateMarker = "update-scheduled";

    public static readonly TimeSpan ProjectCopyLifetime = TimeSpan.FromDays(30);

    public static readonly TimeSpan LookupPause = TimeSpan.FromMinutes(15);

    public static readonly TimeSpan UpdatePause = TimeSpan.FromHours(1);

    public static readonly IReadOnlySet<string> CommandsWithoutAProject = new HashSet<string> { "new", "remove", "decode", "trust", "update", "about", "telemetry", Tools.AdministratorSession.CommandName };

    public static readonly TimeSpan NewerBitPause = TimeSpan.FromDays(1);

    public static readonly IReadOnlySet<string> CommandsOfTheNewestBit = new HashSet<string> { "diff" };

    private readonly HashSet<string> checkedProjects = new(cli.Environment.IsLinux ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

    public string CurrentVersion { get; init; } = BuildInfo.Version;

    public IReadOnlyList<OldestProject> OldestProjects { get; init; } = ProjectBit.OldestProjects;

    public bool IsOfficialBuild { get; init; } = BuildInfo.IsOfficialBuild;

    public string InstallMethod { get; init; } = CliTelemetry.InstallMethod();

    public string BaseDirectory { get; init; } = AppContext.BaseDirectory;

    public string CopiesDirectory => Path.Combine(cli.Environment.BitDirectory, "cli");

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

        await DeleteOldCopiesAsync(request.TemplateVersion);

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

        var executable = await GetCopyAsync(target, cli.Environment.CurrentDirectory, cancellationToken);

        if (executable is null)
        {
            cli.Console.Warn($"Couldn't get bit {target}, so bit {CurrentVersion} goes on.");
            return null;
        }

        if (updateInstalled)
        {
            ScheduleInstalledUpdate(target, cli.Environment.CurrentDirectory);
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

    public async Task<int?> HandOffToProjectBitAsync(string directory, IReadOnlyList<string> arguments, CancellationToken cancellationToken, string? command = null)
    {
        try
        {
            return await HandOffToFittingBitAsync(directory, arguments, command, cancellationToken);
        }
        catch (Exception exp) when (exp is not OperationCanceledException)
        {
            cli.Telemetry.RecordException(null, exp);
            cli.Log.Write(exp.ToString());
            return null;
        }
    }

    private async Task<int?> HandOffToFittingBitAsync(string directory, IReadOnlyList<string> arguments, string? command, CancellationToken cancellationToken)
    {
        if (IsOfficialBuild is false || cli.Environment.GetVariable(HandedOffVariable) is not null)
            return null;

        var project = ProjectBit.Find(directory, OldestProjects);
        var newestOfAll = command is not null && CommandsOfTheNewestBit.Contains(command);

        if (newestOfAll)
        {
            project = (project ?? new ProjectBit(directory, null, null)) with { MaxVersion = null, TooOldFor = null };
        }

        if (project is null || checkedProjects.Add(project.FilePath) is false)
            return null;

        var fits = project.Fits(CurrentVersion);
        var tooOld = project.NeedsNewerThan(CurrentVersion);
        var needs = tooOld ? $"needs bit {project.MinVersion} or newer"
            : project.IsTooOldFor(CurrentVersion) ? $"is on bit Boilerplate {project.TemplateVersion}, too old for bit {project.TooOldFor} and newer"
            : $"works with bit up to {project.MaxVersion}";

        var target = await FindOtherBitAsync(project, fits, tooOld, needs, newestOfAll, cancellationToken);
        var updateInstalled = target is not null && CompareVersions(target, CurrentVersion) > 0 && (IsPrerelease(CurrentVersion) || IsPrerelease(target) is false) && IsRecent(Path.Combine(CopiesDirectory, target, UpdateMarker), UpdatePause) is false;

        if (target is not null)
        {
            var andUpdates = updateInstalled ? ", and your installed bit updates to it when the run ends" : "";

            cli.Console.Text(fits ? $"bit {target} is out, so it runs this{andUpdates}." : $"This project {needs}, so bit {target} runs this{andUpdates}.");
        }

        var newest = target is not null && CompareVersions(target, CurrentVersion) > 0 ? target : CurrentVersion;

        if (newestOfAll is false && cli.Environment.IsCI is false && project.TemplateVersion is { } on && CompareVersions(on, newest) < 0)
        {
            cli.Console.Warn($"This project is on bit Boilerplate {on}, and {newest} is out. bit diff shows what changed.");
        }

        if (target is null)
            return null;

        var executable = await GetCopyAsync(target, CopiesDirectory, cancellationToken);

        if (executable is null)
        {
            Touch(Note("missed", project));
            cli.Console.Warn($"Couldn't get bit {target}, so bit {CurrentVersion} goes on.");
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        Touch(Path.Combine(CopiesDirectory, target, ProjectMarker));

        if (updateInstalled && await ClaimInstalledUpdateAsync(target, cancellationToken))
        {
            ScheduleInstalledUpdate(target, CopiesDirectory);
        }

        var result = await cli.Runner.RunAsync(new ProcessSpec
        {
            FileName = executable,
            Arguments = arguments,
            WorkingDirectory = cli.Environment.CurrentDirectory,
            Environment = new Dictionary<string, string?> { [HandedOffVariable] = CurrentVersion },
            Interactive = true,
            TakesOver = true
        }, CancellationToken.None);

        if (result.NotFound)
        {
            cli.Console.Warn($"Couldn't start bit {target}, so bit {CurrentVersion} goes on.");
            return null;
        }

        if (result.ExitCode != 0 && await IsBrokenAsync(executable))
        {
            DeleteCopy(Path.Combine(CopiesDirectory, target));
            cli.Console.Warn($"The copy of bit {target} that ran this is broken, so bit removed it. Run the command again.");
        }

        return result.ExitCode;
    }

    private async Task<string?> FindOtherBitAsync(ProjectBit project, bool fits, bool tooOld, string needs, bool newestOfAll, CancellationToken cancellationToken)
    {
        if (fits is false)
        {
            cli.Log.Write($"{project.FilePath}: this project {needs}, and this is bit {CurrentVersion}");
        }

        if (InstallMethod is not "tool" || cli.Environment.IsCI || cli.Environment.IsVariableTrue(OptOutVariable))
        {
            if (fits is false)
            {
                cli.Console.Warn($"This project {needs}, and this is bit {CurrentVersion}.");
            }

            return null;
        }

        try
        {
            Directory.CreateDirectory(CopiesDirectory);
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            if (fits is false)
            {
                cli.Console.Warn($"This project {needs}, and bit can't write to {CopiesDirectory}, so bit {CurrentVersion} goes on.");
            }

            return null;
        }

        var target = fits ? await FindNewerAsync(project, newestOfAll, cancellationToken) : await FindFittingAsync(project, tooOld, cancellationToken);

        if (target is null && fits is false)
        {
            cli.Console.Warn($"This project {needs}, and bit couldn't find {(tooOld ? "one" : "an older one")}, so bit {CurrentVersion} goes on.");
        }

        return target;
    }

    private async Task<string?> FindFittingAsync(ProjectBit project, bool tooOld, CancellationToken cancellationToken)
    {
        var comparer = Comparer<string>.Create(CompareVersions);
        var stableOnly = IsPrerelease(CurrentVersion) is false;

        string? Newest(IEnumerable<string> versions)
        {
            var fitting = versions.Where(project.Fits).OrderDescending(comparer).ToList();

            return (stableOnly ? fitting.FirstOrDefault(v => IsPrerelease(v) is false) : null) ?? fitting.FirstOrDefault();
        }

        var copies = Copies();
        var copy = Newest(copies);

        if (IsRecent(Note(copy is null ? "missed" : "newest", project), copy is null ? LookupPause : NewerBitPause))
            return copy;

        var found = await cli.Console.RunWithStatusAsync(tooOld ? "Looking for a newer bit" : "Looking for an older bit", _ => FindVersionsAsync(prerelease: true, CopiesDirectory, cancellationToken));
        var target = Newest(found.Concat(copies));

        Touch(Note(target is null ? "missed" : "newest", project));

        return target;
    }

    private async Task<string?> FindNewerAsync(ProjectBit project, bool now, CancellationToken cancellationToken)
    {
        var comparer = Comparer<string>.Create(CompareVersions);
        var prerelease = IsPrerelease(CurrentVersion);

        string? Newest(IEnumerable<string> versions) => versions
            .Where(v => project.Fits(v) && CompareVersions(v, CurrentVersion) > 0 && (prerelease || IsPrerelease(v) is false))
            .OrderDescending(comparer)
            .FirstOrDefault();

        var copies = Copies();
        var note = Note("newest", project);

        if (now is false && IsRecent(note, NewerBitPause))
            return Newest(copies);

        Touch(note);

        var found = await cli.Console.RunWithStatusAsync("Looking for a newer bit", _ => FindVersionsAsync(prerelease, CopiesDirectory, cancellationToken));

        return Newest(found.Concat(copies));
    }

    private string Note(string kind, ProjectBit project) => Path.Combine(CopiesDirectory, $".{kind}-{project.MinVersion ?? "any"}-to-{project.MaxVersion ?? "any"}{(project.TooOldFor is null ? "" : $"-before-{project.TooOldFor}")}");

    private async Task<bool> ClaimInstalledUpdateAsync(string version, CancellationToken cancellationToken)
    {
        using var claiming = await LockCopiesAsync(TimeSpan.FromSeconds(10), cancellationToken);
        var marker = Path.Combine(CopiesDirectory, version, UpdateMarker);

        if (claiming is null || IsRecent(marker, UpdatePause))
            return false;

        Touch(marker);
        return true;
    }

    private async Task<bool> IsBrokenAsync(string executable)
    {
        var result = await cli.Runner.RunAsync(new ProcessSpec
        {
            FileName = executable,
            Arguments = ["--version"],
            WorkingDirectory = CopiesDirectory,
            Environment = new Dictionary<string, string?> { [HandedOffVariable] = CurrentVersion },
            Timeout = TimeSpan.FromSeconds(30)
        }, CancellationToken.None);

        return result.TimedOut is false && result.Succeeded is false;
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

        ScheduleInstalledUpdate(latest, cli.Environment.CurrentDirectory);
        cli.Console.Text($"bit {latest} replaces {CurrentVersion} as soon as this command ends.");
        cli.Console.Dim($"Its log: {UpdateLog(latest)}");
        return CliApp.ExitOk;
    }

    public async Task<string?> LatestVersionAsync(CancellationToken cancellationToken)
    {
        var prerelease = IsPrerelease(CurrentVersion);

        return (await FindVersionsAsync(prerelease, cli.Environment.CurrentDirectory, cancellationToken))
            .Where(v => prerelease || IsPrerelease(v) is false)
            .OrderDescending(Comparer<string>.Create(CompareVersions))
            .FirstOrDefault();
    }

    private async Task<IReadOnlyList<string>> FindVersionsAsync(bool prerelease, string workingDirectory, CancellationToken cancellationToken)
    {
        var result = await cli.Runner.RunAsync(new ProcessSpec
        {
            FileName = "dotnet",
            Arguments = ["package", "search", BuildInfo.PackageId, "--exact-match", "--format", "json", .. prerelease ? new[] { "--prerelease" } : []],
            WorkingDirectory = workingDirectory,
            Timeout = TimeSpan.FromSeconds(20)
        }, cancellationToken);

        return ParseVersions(result.Output);
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

    private async Task<string?> GetCopyAsync(string version, string workingDirectory, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(CopiesDirectory, version);
        var executable = CopyExecutable(version);

        if (File.Exists(executable))
            return executable;

        var installed = await cli.Console.RunWithStatusAsync($"Getting bit {version}", async progress =>
        {
            using var installing = await LockCopiesAsync(TimeSpan.FromMinutes(5), cancellationToken);

            if (File.Exists(executable))
                return true;

            if (installing is null)
                return false;

            DeleteCopy(directory);

            var result = await cli.Runner.RunAsync(new ProcessSpec
            {
                FileName = "dotnet",
                Arguments = ["tool", "install", BuildInfo.PackageId, "--version", version, "--tool-path", directory],
                WorkingDirectory = workingDirectory,
                OnOutputLine = progress,
                Timeout = TimeSpan.FromMinutes(5)
            }, cancellationToken);

            if (result.Succeeded && File.Exists(executable))
                return true;

            DeleteCopy(directory);
            return false;
        });

        return installed ? executable : null;
    }

    private Task<FileStream?> LockCopiesAsync(TimeSpan patience, CancellationToken cancellationToken) => LockAsync(Path.Combine(CopiesDirectory, ".lock"), patience, cancellationToken);

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

    private static void Touch(string file)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllBytes(file, []);
            File.SetLastWriteTimeUtc(file, DateTime.UtcNow);
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static bool IsRecent(string file, TimeSpan age)
    {
        try
        {
            return DateTime.UtcNow - File.GetLastWriteTimeUtc(file) < age;
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void ScheduleInstalledUpdate(string version, string workingDirectory)
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
                WorkingDirectory = workingDirectory
            }
            : new ProcessSpec
            {
                FileName = "/bin/sh",
                Arguments = ["-c", $"trap '' HUP INT; while kill -0 {processId} 2>/dev/null; do sleep 1; done; {ShellQuote(dotnet)} tool update --global {BuildInfo.PackageId} --version {version} > {ShellQuote(log)} 2>&1"],
                WorkingDirectory = workingDirectory
            });
    }

    private string UpdateLog(string version) => Path.Combine(cli.Environment.LogsDirectory, $"update-{version}.log");

    private async Task DeleteOldCopiesAsync(string? keep)
    {
        if (Directory.Exists(CopiesDirectory) is false)
            return;

        using var cleaning = await LockCopiesAsync(TimeSpan.Zero, CancellationToken.None);

        if (cleaning is null)
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
            else if (CompareVersions(version, CurrentVersion) <= 0 && IsRecent(Path.Combine(directory, ProjectMarker), ProjectCopyLifetime) is false)
            {
                DeleteCopy(directory);
            }
        }

        foreach (var note in Directory.EnumerateFiles(CopiesDirectory, ".*-to-*").Where(file => IsRecent(file, LookupPause) is false).ToList())
        {
            try
            {
                File.Delete(note);
            }
            catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
            {
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
