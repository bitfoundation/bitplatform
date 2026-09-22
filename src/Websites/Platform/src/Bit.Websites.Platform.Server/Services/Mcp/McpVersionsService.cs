using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Bit.Websites.Platform.Server.Services.Mcp;

/// <summary>
/// Keeps one worktree per released version of the repository, indexes each with codebase-memory and runs that
/// version's documentation servers out of it, so /mcp?v=10.6.2 answers with the code and the docs of 10.6.2.
/// Runs in the background: startup never waits for it, and a version that cannot be prepared is only left out.
/// </summary>
public partial class McpVersionsService : BackgroundService
{
    /// <summary>Release tags only: v-1.6.2-pre-01 and the like are not served.</summary>
    [GeneratedRegex(@"^v-(?<version>\d+\.\d+\.\d+)$")]
    private static partial Regex ReleaseTagRegex();

    /// <summary>Enough for a first clone of this repository, and for the checkout each worktree does.</summary>
    private static readonly TimeSpan gitTransferTimeout = TimeSpan.FromMinutes(60);

    private static readonly TimeSpan gitQueryTimeout = TimeSpan.FromMinutes(2);

    private static readonly TimeSpan retryInterval = TimeSpan.FromMinutes(15);

    // IOptionsMonitor rather than the transient AppSettings: that one resolves through IOptionsSnapshot,
    // which is scoped and unavailable to root-scope services like this one.
    [AutoInject] private IOptionsMonitor<AppSettings> appSettings = default!;
    [AutoInject] private ILogger<McpVersionsService> logger = default!;
    [AutoInject] private ILoggerFactory loggerFactory = default!;
    [AutoInject] private CodebaseMemoryCli codebaseMemory = default!;
    [AutoInject] private McpVersions versions = default!;

    private readonly Dictionary<string, McpVersion> prepared = new(StringComparer.Ordinal);
    private readonly Dictionary<string, McpDocumentationSite[]> sites = new(StringComparer.Ordinal);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = appSettings.CurrentValue.Mcp;

        if (string.IsNullOrWhiteSpace(settings?.SourcesDirectoryPath) || string.IsNullOrWhiteSpace(settings.RepositoryUrl))
        {
            logger.LogInformation("No sources are configured, so /mcp serves no release: only the third party tools are exposed.");
            return;
        }

        // Everything below runs off the startup path.
        await Task.Yield();

        try
        {
            using PeriodicTimer timer = new(TimeSpan.FromMinutes(1));

            // A release that could not be prepared is retried long before the next look for new tags.
            var nextRefresh = DateTimeOffset.UtcNow + (await Prepare(settings, stoppingToken) ? settings.RefreshInterval : retryInterval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await Supervise(stoppingToken);

                if (DateTimeOffset.UtcNow < nextRefresh) continue;

                // Picks up a release tagged since the site started, without a redeploy.
                settings = appSettings.CurrentValue.Mcp!;

                nextRefresh = DateTimeOffset.UtcNow + (await Prepare(settings, stoppingToken) ? settings.RefreshInterval : retryInterval);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception exp)
        {
            // The endpoint still answers, with the third party tools alone until a release comes up.
            logger.LogError(exp, "Preparing the releases of /mcp failed.");
        }
    }

    /// <returns>Whether every release found is now being served.</returns>
    private async Task<bool> Prepare(McpSettings settings, CancellationToken cancellationToken)
    {
        var clonePath = Path.Combine(settings.SourcesDirectoryPath!, "repository");

        if (await EnsureClone(settings, clonePath, cancellationToken) is false) return false;

        // Three parts, since a configured 10.6 would otherwise carry a -1 build that no tag can match.
        var minimum = Version.TryParse(settings.MinimumVersion, out var parsed) ? new(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0)) : new Version(10, 6, 0);
        var releases = await Releases(clonePath, minimum, cancellationToken);

        if (releases.Length is 0)
        {
            logger.LogWarning("No release tag at or above {MinimumVersion} was found in {ClonePath}.", minimum.ToString(3), clonePath);
            return false;
        }

        var complete = true;

        // Newest first, so the version an unversioned caller gets is the first one to come up.
        foreach (var (number, tag) in releases)
        {
            if (prepared.ContainsKey(number.ToString(3))) continue;

            await PrepareRelease(settings, clonePath, number, tag, cancellationToken);

            complete &= prepared.ContainsKey(number.ToString(3));

            Publish();
        }

        return complete;
    }

    private async Task<bool> EnsureClone(McpSettings settings, string clonePath, CancellationToken cancellationToken)
    {
        if (Directory.Exists(Path.Combine(clonePath, ".git")) is false)
        {
            Directory.CreateDirectory(settings.SourcesDirectoryPath!);

            logger.LogInformation("Cloning {RepositoryUrl} into {ClonePath}.", settings.RepositoryUrl, clonePath);

            // No working tree of its own: only tags are served, and each of them gets a worktree instead.
            return await Git(null, cancellationToken, "clone", "--no-checkout", settings.RepositoryUrl!, clonePath) is not null;
        }

        // Prunes the worktree registrations of directories that are no longer there, from an earlier run
        // whose data directory was cleaned out.
        await Git(clonePath, cancellationToken, "worktree", "prune");

        return await Git(clonePath, cancellationToken, "fetch", "--prune", "--prune-tags", "--tags", "--force", "origin") is not null;
    }

    private async Task<(Version Number, string Tag)[]> Releases(string clonePath, Version minimum, CancellationToken cancellationToken)
    {
        var listed = await Git(clonePath, cancellationToken, "tag", "--list", "v-*");

        if (listed is null) return [];

        return [.. listed.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         .Select(tag => (Tag: tag, Match: ReleaseTagRegex().Match(tag)))
                         .Where(listing => listing.Match.Success)
                         .Select(listing => (Number: Version.Parse(listing.Match.Groups["version"].Value), listing.Tag))
                         .Where(release => release.Number >= minimum)
                         .OrderByDescending(release => release.Number)];
    }

    private async Task PrepareRelease(McpSettings settings, string clonePath, Version number, string tag, CancellationToken cancellationToken)
    {
        var name = number.ToString(3);
        var worktreePath = Path.Combine(settings.SourcesDirectoryPath!, tag);

        if (await EnsureWorktree(clonePath, worktreePath, tag, cancellationToken) is false) return;

        var project = await codebaseMemory.Index(worktreePath, cancellationToken);

        if (project is null) return;

        var released = McpDocumentationSite.Projects
            .Select(documentation => new McpDocumentationSite(documentation.UpstreamName, name,
                                                              Path.Combine(worktreePath, documentation.ProjectPath.Replace('/', Path.DirectorySeparatorChar)),
                                                              loggerFactory.CreateLogger<McpDocumentationSite>()))
            .ToArray();

        // One at a time: they share referenced projects, so parallel builds fight over the same obj folders.
        foreach (var site in released)
        {
            if (await site.EnsureRunning(cancellationToken)) continue;

            foreach (var started in released) started.Dispose();

            logger.LogError("Version {VersionName} is not served: its {UpstreamName} site could not be started.", name, site.UpstreamName);

            return;
        }

        sites[name] = released;
        prepared[name] = new(number, tag, worktreePath, project, EndpointsOf(released));

        logger.LogInformation("Version {VersionName} is served from {WorktreePath}.", name, worktreePath);
    }

    private async Task<bool> EnsureWorktree(string clonePath, string worktreePath, string tag, CancellationToken cancellationToken)
    {
        if (Directory.Exists(worktreePath))
        {
            var head = await Git(worktreePath, cancellationToken, "rev-parse", "HEAD");
            var tagged = await Git(clonePath, cancellationToken, "rev-list", "-n", "1", tag);

            // Only a worktree known to sit on another commit is replaced. A git command that merely failed
            // must not cost the half hour of build output inside it; the next refresh asks again.
            if (head is null || tagged is null)
            {
                logger.LogWarning("Could not confirm that {WorktreePath} is at {Tag}, so it is left alone and retried later.", worktreePath, tag);
                return false;
            }

            if (head.Trim() == tagged.Trim()) return true;

            logger.LogWarning("{WorktreePath} is not at {Tag}, replacing it.", worktreePath, tag);

            await Git(clonePath, cancellationToken, "worktree", "remove", "--force", worktreePath);

            if (Directory.Exists(worktreePath))
            {
                Directory.Delete(worktreePath, recursive: true);
            }
        }

        return await Git(clonePath, cancellationToken, "worktree", "add", "--detach", "--force", worktreePath, tag) is not null;
    }

    /// <summary>Restarts the sites that died, and drops a version off the endpoint while its sites are down.</summary>
    private async Task Supervise(CancellationToken cancellationToken)
    {
        var changed = false;

        foreach (var (name, released) in sites)
        {
            var restarted = false;

            foreach (var site in released)
            {
                if (site.IsRunning) continue;

                restarted = true;

                await site.EnsureRunning(cancellationToken);
            }

            if (restarted is false) continue;

            // A restarted site binds a new port, so the version the proxy holds is replaced rather than edited.
            prepared[name] = prepared[name] with { DocumentationEndpoints = EndpointsOf(released) };
            changed = true;
        }

        if (changed)
        {
            Publish();
        }
    }

    private static Dictionary<string, Uri> EndpointsOf(McpDocumentationSite[] released)
        => released.Where(site => site.Endpoint is not null).ToDictionary(site => site.UpstreamName, site => site.Endpoint!, StringComparer.Ordinal);

    // A documentation server that outlives this site is not possible to leave behind: ChildProcessJob ties
    // every one of them to the site's own lifetime, so nothing has to be recorded or cleaned up here.
    private void Publish()
        => versions.Publish(prepared.Values.Where(version => sites[version.Name].All(site => site.IsRunning)));

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        foreach (var site in sites.Values.SelectMany(released => released))
        {
            site.Dispose();
        }
    }

    /// <returns>The standard output of the git command, or null when it failed.</returns>
    private async Task<string?> Git(string? repositoryPath, CancellationToken cancellationToken, params string[] arguments)
    {
        var timeout = arguments[0] is "clone" or "fetch" || arguments is ["worktree", "add", ..] ? gitTransferTimeout : gitQueryTimeout;

        var result = await ProcessRunner.Run(ProcessRunner.For("git", arguments, repositoryPath), timeout, cancellationToken);

        if (result.Succeeded) return result.Output;

        logger.LogError("git {Arguments} in {RepositoryPath} {Outcome}: {Diagnostics}",
            string.Join(' ', arguments), repositoryPath, result.TimedOut ? $"hit its {timeout.TotalMinutes:0} minute limit" : $"exited with {result.ExitCode}", result.Diagnostics);

        return null;
    }
}
