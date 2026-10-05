using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Bit.Websites.Platform.Server.Services.Mcp;

/// <summary>
/// The codebase-memory command line, which indexes one worktree per served version into a single data
/// directory. <see cref="McpProxyService"/> spawns the same command as an MCP server over those indexes.
/// </summary>
public partial class CodebaseMemoryCli
{
    /// <summary>
    /// The parts of the repository that get indexed, and so the only code the source tools reach.
    /// What is left out answers for itself elsewhere on this endpoint: BlazorUI, Bmotion, Brouter, Butil
    /// and Bswup each have their own MCP server here, written for the library rather than grepped out of it.
    /// </summary>
    internal static readonly string[] IndexedPaths =
    [
        "src/Besql",
        "src/BlazorES2019",
        "src/CodeAnalyzers",
        "src/Minifier",
        "src/ResxTranslator",
        "src/SourceGenerators",
        "src/Templates/Boilerplate/Bit.Boilerplate",
        "src/Websites"
    ];

    /// <summary>Marks a .cbmignore as one this site owns, so a repository that brings its own keeps it.</summary>
    private const string ignoreFileHeader = "# Written by the bit platform website. Edit CodebaseMemoryCli.IndexedPaths instead.";

    /// <summary>
    /// The indexer takes a single root and narrows it down by gitignore rules alone, so <see cref="IndexedPaths"/>
    /// becomes an allowlist: everything is ignored, then each directory on the way down to an indexed one is
    /// un-ignored and its siblings ignored again. A later rule wins, so the levels are written from the root down.
    /// </summary>
    internal static string BuildIgnoreFileContent()
    {
        var segments = IndexedPaths.Select(path => path.Split('/')).ToArray();

        List<string> lines = [ignoreFileHeader, "/*"];

        for (var depth = 1; segments.Any(segment => segment.Length >= depth); depth++)
        {
            var directories = segments.Where(segment => segment.Length >= depth)
                                      .Select(segment => string.Join('/', segment.Take(depth)))
                                      .Distinct(StringComparer.Ordinal)
                                      .Order(StringComparer.Ordinal)
                                      .ToArray();

            lines.AddRange(directories.Select(directory => $"!/{directory}/"));

            // Only a directory merely on the way to an indexed one has its children ignored again.
            lines.AddRange(directories.Where(directory => segments.Any(segment => segment.Length > depth && string.Join('/', segment.Take(depth)) == directory))
                                      .Select(directory => $"/{directory}/*"));
        }

        return string.Join('\n', lines) + '\n';
    }

    // IOptionsMonitor rather than the transient AppSettings: that one resolves through IOptionsSnapshot,
    // which is scoped and unavailable to root-scope services like this one.
    [AutoInject] private IOptionsMonitor<AppSettings> appSettings = default!;
    [AutoInject] private ILogger<CodebaseMemoryCli> logger = default!;

    private CodebaseMemorySettings? Settings => appSettings.CurrentValue.Mcp?.CodebaseMemory;

    /// <summary>npx by default, so no global install has to sit on the PATH of the account the site runs under.</summary>
    public string Command => string.IsNullOrWhiteSpace(Settings?.ExecutablePath) ? "npx" : Settings.ExecutablePath;

    public string[] Arguments => string.IsNullOrWhiteSpace(Settings?.ExecutablePath) ? ["-y", "codebase-memory-mcp@latest"] : Settings.ExecutableArguments ?? [];

    /// <summary>
    /// Points every codebase-memory process of this site at a data directory of its own, rather than the
    /// default one a developer's editor on the same machine already holds. Created here: it must exist.
    /// </summary>
    public Dictionary<string, string?> Environment
    {
        get
        {
            var dataDirectory = string.IsNullOrWhiteSpace(Settings?.DataDirectoryPath)
                ? Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "bitplatform.dev", "codebase-memory")
                : Settings.DataDirectoryPath;

            var cacheDirectory = Path.Combine(dataDirectory, "cache");
            var runtimeDirectory = Path.Combine(dataDirectory, "runtime");

            Directory.CreateDirectory(cacheDirectory);
            Directory.CreateDirectory(runtimeDirectory);

            return new() { ["CBM_CACHE_DIR"] = cacheDirectory, ["CBM_RUNTIME_DIR"] = runtimeDirectory };
        }
    }

    /// <summary>
    /// Indexes one worktree and returns the name codebase-memory stored it under, which the proxy sends as
    /// the project argument of every source tool call. Null when the indexer failed.
    /// </summary>
    public async Task<string?> Index(string worktreePath, CancellationToken cancellationToken)
    {
        WriteIgnoreFile(worktreePath);

        // A first index of a large repository takes a minute or two; later runs reuse the persisted index.
        if (await Run(TimeSpan.FromMinutes(20), cancellationToken, "index_repository", "--repo-path", worktreePath) is null) return null;

        var projectName = await ResolveProjectName(worktreePath, cancellationToken);

        if (projectName is null)
        {
            logger.LogError("Codebase memory indexed {WorktreePath} but lists no project for it.", worktreePath);
        }

        return projectName;
    }

    /// <summary>
    /// Narrows the index down to <see cref="IndexedPaths"/>, through the only mechanism the indexer offers
    /// for it: a .cbmignore at the root of the worktree. A worktree that already has one of its own keeps it
    /// and is indexed whole, which is logged rather than left silent.
    /// </summary>
    private void WriteIgnoreFile(string worktreePath)
    {
        var ignoreFilePath = Path.Combine(worktreePath, ".cbmignore");

        if (File.Exists(ignoreFilePath) && File.ReadLines(ignoreFilePath).FirstOrDefault() != ignoreFileHeader)
        {
            logger.LogWarning("Codebase memory is indexing all of {WorktreePath}: {IgnoreFilePath} was written by someone else, so it is left as it is.", worktreePath, ignoreFilePath);
            return;
        }

        File.WriteAllText(ignoreFilePath, BuildIgnoreFileContent());
    }

    /// <summary>
    /// The server derives the project name from the path it indexed, so it is read back by matching the
    /// worktree against the root of each listed project rather than rebuilt from a naming convention.
    /// </summary>
    private async Task<string?> ResolveProjectName(string worktreePath, CancellationToken cancellationToken)
    {
        using var listed = ParseJson(await Run(TimeSpan.FromMinutes(2), cancellationToken, "list_projects", "--format", "json"));

        if (listed is null || listed.RootElement.TryGetProperty("projects", out var projects) is false) return null;

        foreach (var project in projects.EnumerateArray())
        {
            if (project.TryGetProperty("root_path", out var rootPath) &&
                project.TryGetProperty("name", out var name) &&
                IsSamePath(rootPath.GetString(), worktreePath))
            {
                return name.GetString();
            }
        }

        return null;
    }

    /// <returns>The standard output of the cli command, or null when it failed.</returns>
    private async Task<string?> Run(TimeSpan timeout, CancellationToken cancellationToken, params string[] arguments)
    {
        var startInfo = ProcessRunner.For(Command, [.. Arguments, "cli", .. arguments], shell: true);

        foreach (var (name, value) in Environment)
        {
            startInfo.Environment[name] = value;
        }

        var result = await ProcessRunner.Run(startInfo, timeout, cancellationToken);

        if (result.Succeeded) return result.Output;

        logger.LogError("Codebase memory cli {Arguments} {Outcome}: {Diagnostics}",
            string.Join(' ', arguments), result.TimedOut ? $"hit its {timeout.TotalMinutes:0} minute limit" : $"exited with {result.ExitCode}", result.Diagnostics);

        // A failure that says nothing at all means the command never ran: report the account's own view.
        if (string.IsNullOrWhiteSpace(result.Output) && string.IsNullOrWhiteSpace(result.Error))
        {
            LogHostDiagnostics();
        }

        return null;
    }

    private void LogHostDiagnostics()
    {
        try
        {
            var startInfo = OperatingSystem.IsWindows()
                ? ProcessRunner.For("cmd.exe", ["/c", "where", Command])
                : ProcessRunner.For("/usr/bin/which", [Command]);

            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;

            using var process = Process.Start(startInfo)!;
            var located = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit(milliseconds: 30_000);

            using var current = Process.GetCurrentProcess();

            logger.LogError("Codebase memory host: user {User}, session {SessionId}, cwd {WorkingDirectory}, '{Command}' resolved to '{Located}', PATH {Path}",
                System.Environment.UserName, current.SessionId, System.Environment.CurrentDirectory,
                Command, located.Trim(), System.Environment.GetEnvironmentVariable("PATH"));
        }
        catch (Exception exp)
        {
            logger.LogError(exp, "Codebase memory host diagnostics failed.");
        }
    }

    /// <summary>The cli prints hints and progress lines around its json result, so the outermost object is taken out.</summary>
    private static JsonDocument? ParseJson(string? output)
    {
        if (output is null) return null;

        var start = output.IndexOf('{');
        var end = output.LastIndexOf('}');

        if (start < 0 || end < start) return null;

        try
        {
            return JsonDocument.Parse(output[start..(end + 1)]);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>The listed root is the path the server normalized: separators, casing and a trailing slash differ.</summary>
    private static bool IsSamePath(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;

        return string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
                             Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
                             OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}
