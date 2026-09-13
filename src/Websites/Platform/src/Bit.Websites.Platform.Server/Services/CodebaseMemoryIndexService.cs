using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Bit.Websites.Platform.Server.Services;

/// <summary>
/// Indexes the repository configured at AppSettings:CodebaseMemory:SourceRepositoryPath when the site
/// starts, so the CodebaseMemory upstream of <see cref="McpProxyService"/> answers from a ready index.
/// Runs in the background: startup never waits for it, and a missing executable or a failed run only logs.
/// </summary>
public partial class CodebaseMemoryIndexService : BackgroundService
{
    /// <summary>
    /// npx by default, so no global install has to sit on the PATH of the account the site runs under.
    /// </summary>
    internal static string ResolveCommand(CodebaseMemorySettings? settings)
        => string.IsNullOrWhiteSpace(settings?.ExecutablePath) ? "npx" : settings.ExecutablePath;

    internal static string[] ResolveArguments(CodebaseMemorySettings? settings)
        => string.IsNullOrWhiteSpace(settings?.ExecutablePath) ? ["-y", "codebase-memory-mcp@latest"] : settings.ExecutableArguments ?? [];

    // IOptionsMonitor rather than the transient AppSettings: that one resolves through IOptionsSnapshot,
    // which is scoped and unavailable to root-scope services like this one.
    [AutoInject] private IOptionsMonitor<AppSettings> appSettings = default!;
    [AutoInject] private ILogger<CodebaseMemoryIndexService> logger = default!;

    /// <summary>
    /// The name the indexed repository is stored under, which <see cref="McpProxyService"/> injects into
    /// every CodebaseMemory tool call. Null until the first successful index of this process.
    /// </summary>
    public static string? ProjectName { get; private set; }

    /// <summary>
    /// Points every codebase-memory process of this site at a data directory of its own, rather than the
    /// default one a developer's editor on the same machine already holds. Created here: it must exist.
    /// </summary>
    internal static Dictionary<string, string?> BuildEnvironment(CodebaseMemorySettings? settings)
    {
        var dataDirectory = string.IsNullOrWhiteSpace(settings?.DataDirectoryPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "bitplatform.dev", "codebase-memory")
            : settings.DataDirectoryPath;

        var cacheDirectory = Path.Combine(dataDirectory, "cache");
        var runtimeDirectory = Path.Combine(dataDirectory, "runtime");

        Directory.CreateDirectory(cacheDirectory);
        Directory.CreateDirectory(runtimeDirectory);

        return new() { ["CBM_CACHE_DIR"] = cacheDirectory, ["CBM_RUNTIME_DIR"] = runtimeDirectory };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var repositoryPath = appSettings.CurrentValue.CodebaseMemory?.SourceRepositoryPath;

        if (string.IsNullOrWhiteSpace(repositoryPath)) return;

        // Everything below runs off the startup path.
        await Task.Yield();

        if (Directory.Exists(repositoryPath) is false)
        {
            logger.LogWarning("Codebase memory indexing skipped: {RepositoryPath} does not exist.", repositoryPath);
            return;
        }

        try
        {
            // A first index of a large repository takes a minute or two; later runs reuse the persisted
            // index and only process what changed.
            if (await Run(stoppingToken, "index_repository", "--repo-path", repositoryPath) is null) return;

            ProjectName = await ResolveProjectName(repositoryPath, stoppingToken);

            if (ProjectName is null)
            {
                logger.LogError("Codebase memory indexed {RepositoryPath} but lists no project for it.", repositoryPath);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception exp)
        {
            // The chatbot still works; its CodebaseMemory tools report the repository as not indexed.
            logger.LogError(exp, "Codebase memory indexing could not run for {RepositoryPath}.", repositoryPath);
        }
    }

    /// <summary>
    /// The server derives the project name from the path it indexed, so it is read back by matching the
    /// configured <see cref="CodebaseMemorySettings.SourceRepositoryPath"/> against the root of each
    /// listed project, rather than rebuilt here from a naming convention that is the server's to change.
    /// </summary>
    private async Task<string?> ResolveProjectName(string repositoryPath, CancellationToken cancellationToken)
    {
        using var listed = ParseJson(await Run(cancellationToken, "list_projects"));

        if (listed is null || listed.RootElement.TryGetProperty("projects", out var projects) is false) return null;

        foreach (var project in projects.EnumerateArray())
        {
            if (project.TryGetProperty("root_path", out var rootPath) &&
                project.TryGetProperty("name", out var name) &&
                IsSamePath(rootPath.GetString(), repositoryPath))
            {
                return name.GetString();
            }
        }

        return null;
    }

    /// <returns>The standard output of the cli command, or null when it failed.</returns>
    private async Task<string?> Run(CancellationToken cancellationToken, params string[] arguments)
    {
        var settings = appSettings.CurrentValue.CodebaseMemory;

        // A .cmd shim on Windows cannot be started by CreateProcess, so it goes through the shell there.
        var command = ResolveCommand(settings);
        ProcessStartInfo startInfo = OperatingSystem.IsWindows() ? new("cmd.exe") { ArgumentList = { "/c", command } } : new(command);

        foreach (var argument in (string[])[.. ResolveArguments(settings), "cli", .. arguments])
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var (name, value) in BuildEnvironment(settings))
        {
            startInfo.Environment[name] = value;
        }

        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;

        using var process = Process.Start(startInfo)!;

        // Both pipes are drained at once: the command writes progress to standard error, and letting that
        // buffer fill blocks the child while standard output alone is being read to the end.
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await Task.WhenAll(outputTask, errorTask);
        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode is 0) return output;

        logger.LogError("Codebase memory cli {Arguments} exited with {ExitCode}: {Output}",
            string.Join(' ', arguments), process.ExitCode, string.IsNullOrWhiteSpace(error) ? output : error);

        // A failure that says nothing at all means the command never ran: report the account's own view.
        if (string.IsNullOrWhiteSpace(output) && string.IsNullOrWhiteSpace(error))
        {
            LogHostDiagnostics(command);
        }

        return null;
    }

    private void LogHostDiagnostics(string command)
    {
        try
        {
            ProcessStartInfo startInfo = OperatingSystem.IsWindows()
                ? new("cmd.exe") { ArgumentList = { "/c", "where", command } }
                : new("/usr/bin/which") { ArgumentList = { command } };

            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;

            using var process = Process.Start(startInfo)!;
            var located = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit();

            using var current = Process.GetCurrentProcess();

            logger.LogError("Codebase memory host: user {User}, session {SessionId}, cwd {WorkingDirectory}, '{Command}' resolved to '{Located}' with exit {ExitCode}, PATH {Path}",
                Environment.UserName, current.SessionId, Environment.CurrentDirectory,
                command, located.Trim(), process.ExitCode, Environment.GetEnvironmentVariable("PATH"));
        }
        catch (Exception exp)
        {
            logger.LogError(exp, "Codebase memory host diagnostics failed.");
        }
    }

    /// <summary>
    /// The cli prints hints and progress lines around its json result, so the outermost object is taken
    /// out of the output rather than the output parsed as a whole.
    /// </summary>
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

    /// <summary>
    /// The listed root is the path the server normalized, which differs from the configured one in
    /// separators, casing and a trailing slash.
    /// </summary>
    private static bool IsSamePath(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;

        return string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
                             Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
                             OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}
