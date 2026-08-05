using System.Text;

namespace Boilerplate.Server.Web.Infrastructure.Services;

/// <summary>
/// Watches the source files of every project in the solution during development and runs the MSBuild targets
/// that own those files (SCSS to CSS, TypeScript to JavaScript, resx to C#, ...) as soon as they change.
/// </summary>
public sealed class FileWatcherService : IDisposable
{
    /// <summary>
    /// The file patterns to watch and the MSBuild targets to run for each of them.
    /// A project is watched for a pattern only when its csproj defines (or depends on) the corresponding target.
    /// </summary>
    private static readonly Dictionary<string, string[]> targetsByFilePattern = new()
    {
        { "*.scss", ["BuildCssFiles"] }, // Checkout <Target Name="BuildCssFiles" ... in Boilerplate.Client.Core.csproj
        { "*.ts", ["BuildJavaScript"] },
        { "*.resx", ["PrepareResources"] }
    };

    /// <summary>
    /// The targets in <see cref="targetsByFilePattern"/>'s declaration order, which is the order they're run in.
    /// </summary>
    private static readonly string[] orderedTargets = [.. targetsByFilePattern.Values.SelectMany(targets => targets).Distinct(StringComparer.Ordinal)];

    /// <summary>
    /// Directories that hold no hand written source file, but do hold plenty of files matching the patterns above.
    /// </summary>
    private static readonly string[] ignoredDirectories = ["node_modules", "bin", "obj", ".git", ".vs"];

    /// <summary>
    /// Editors save a single file with several write operations, so the events are coalesced before the targets are run.
    /// </summary>
    private static readonly TimeSpan debounceDelay = TimeSpan.FromMilliseconds(250);

    internal static async Task StartAsync(WebApplication app)
    {
        if (app.Environment.IsDevelopment() is false)
            return;

        var logger = app.Services.GetRequiredService<ILogger<FileWatcherService>>();

        // Environment.CurrentDirectory is the Server.Web project's directory during development, so ../../ is the solution's src directory.
        var srcDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../../"));

        if (Directory.Exists(srcDirectory) is false)
        {
            logger.LogWarning("{Directory} not found, no source file is watched", srcDirectory);
            return;
        }

        foreach (var projectPath in Directory.EnumerateFiles(srcDirectory, "*.csproj", SearchOption.AllDirectories))
        {
            if (IsIgnored(srcDirectory, projectPath))
                continue;

            var projectContent = await File.ReadAllTextAsync(projectPath);

            var watchList = targetsByFilePattern
                .Select(item => (Pattern: item.Key, Targets: item.Value.Where(target => projectContent.Contains(target, StringComparison.Ordinal)).ToArray()))
                .Where(item => item.Targets.Length > 0)
                .ToArray();

            if (watchList.Length is 0)
                continue;

            var watcherService = new FileWatcherService(logger, projectPath);
            app.Lifetime.ApplicationStopping.Register(watcherService.Dispose);

            foreach (var (pattern, targets) in watchList)
            {
                watcherService.Watch(pattern, targets);
            }

            logger.LogInformation("Watching {Patterns} of {Project}", string.Join(", ", watchList.Select(item => item.Pattern)), projectPath);
        }
    }

    private readonly ILogger<FileWatcherService> logger;
    private readonly string projectPath;
    private readonly string projectDirectory;
    private readonly List<FileSystemWatcher> watchers = [];
    private readonly HashSet<string> pendingTargets = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim runLock = new(1, 1);
    private readonly Lock pendingTargetsLock = new();
    private readonly Timer debounceTimer;
    private bool disposed;

    private FileWatcherService(ILogger<FileWatcherService> logger, string projectPath)
    {
        this.logger = logger;
        this.projectPath = projectPath;
        projectDirectory = Path.GetDirectoryName(projectPath)!;
        debounceTimer = new Timer(_ => _ = RunPendingTargetsAsync(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    private void Watch(string pattern, string[] targets)
    {
        var watcher = new FileSystemWatcher(projectDirectory, pattern)
        {
            IncludeSubdirectories = true,
            InternalBufferSize = 64 * 1024,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
        };

        watcher.Changed += (_, e) => OnFileChanged(e.FullPath, targets);
        watcher.Created += (_, e) => OnFileChanged(e.FullPath, targets);
        watcher.Deleted += (_, e) => OnFileChanged(e.FullPath, targets);
        watcher.Renamed += (_, e) => OnFileChanged(e.FullPath, targets);
        watcher.Error += (_, e) => logger.LogError(e.GetException(), "Watching {Pattern} of {Project} failed", pattern, projectPath);

        watcher.EnableRaisingEvents = true;

        watchers.Add(watcher);
    }

    private void OnFileChanged(string filePath, string[] targets)
    {
        if (IsIgnored(projectDirectory, filePath))
            return;

        lock (pendingTargetsLock)
        {
            if (disposed)
                return;

            foreach (var target in targets)
            {
                pendingTargets.Add(target);
            }

            debounceTimer.Change(debounceDelay, Timeout.InfiniteTimeSpan);
        }
    }

    private async Task RunPendingTargetsAsync()
    {
        string[] targets;

        lock (pendingTargetsLock)
        {
            if (pendingTargets.Count is 0)
                return;

            targets = [.. orderedTargets.Where(pendingTargets.Contains)];
            pendingTargets.Clear();
        }

        // Files changing while a previous run is still in progress must not run two MSBuild processes against the same project.
        await runLock.WaitAsync();

        try
        {
            foreach (var target in targets)
            {
                await RunTargetAsync(target);
            }
        }
        finally
        {
            runLock.Release();
        }
    }

    private async Task RunTargetAsync(string target)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = projectDirectory,
                    FileName = "dotnet",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };
            process.StartInfo.ArgumentList.Add("build");
            process.StartInfo.ArgumentList.Add(projectPath);
            process.StartInfo.ArgumentList.Add($"-t:{target}");
            process.StartInfo.ArgumentList.Add("--no-restore");
            process.StartInfo.ArgumentList.Add("--no-dependencies");

            StringBuilder outputResult = new();
            StringBuilder errorResult = new();

            process.OutputDataReceived += (_, e) => { if (string.IsNullOrEmpty(e.Data) is false) outputResult.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (string.IsNullOrEmpty(e.Data) is false) errorResult.AppendLine(e.Data); };

            if (process.Start() is false)
            {
                logger.LogError("Failed to run the {Target} target of {Project}", target, projectPath);
                return;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            if (process.ExitCode is 0)
            {
                logger.LogInformation("Ran the {Target} target of {Project}:\n{Output}", target, projectPath, outputResult.ToString());
            }
            else
            {
                logger.LogError("The {Target} target of {Project} failed with exit code {ExitCode}:\n{Output}{Errors}", target, projectPath, process.ExitCode, outputResult.ToString(), errorResult.ToString());
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to run the {Target} target of {Project}", target, projectPath);
        }
    }

    private static bool IsIgnored(string rootDirectory, string filePath)
    {
        return Path.GetRelativePath(rootDirectory, filePath)
                   .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                   .Any(segment => ignoredDirectories.Contains(segment, StringComparer.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        lock (pendingTargetsLock)
        {
            if (disposed)
                return;

            disposed = true;
        }

        debounceTimer.Dispose();

        foreach (var watcher in watchers)
        {
            watcher.Dispose();
        }

        watchers.Clear();
    }
}
