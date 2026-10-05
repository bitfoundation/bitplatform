namespace Bit.Websites.Platform.Server;

public class AppSettings
{
    public HealthCheckSettings HealthCheckSettings { get; set; } = default!;

    public TelegramBotSettings TelegramBotSettings { get; set; } = default!;

    public AzureOpenAIOptions AzureOpenAI { get; set; } = default!;

    public OpenAIOptions OpenAI { get; set; } = default!;

    public McpSettings Mcp { get; set; } = default!;
}

public class McpSettings
{
    /// <summary>
    /// The repository whose release tags become the versions /mcp answers for. Only its tags are used, so it
    /// is cloned without a working tree, then fetched every <see cref="RefreshInterval"/>.
    /// </summary>
    public string? RepositoryUrl { get; set; }

    /// <summary>
    /// Holds the clone and one worktree per served version, each with an index and a documentation server per
    /// library of its own. Empty leaves versioning off, and /mcp then serves only the third party tools.
    /// </summary>
    public string? SourcesDirectoryPath { get; set; }

    /// <summary>
    /// The lowest release served. Every non pre-release tag at or above it gets a version of its own, and a
    /// caller that names no version, or one that is not served, gets the newest of them.
    /// </summary>
    public string MinimumVersion { get; set; } = "10.6.0";

    /// <summary>How often a release tagged since the site started is picked up.</summary>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(6);

    public CodebaseMemorySettings CodebaseMemory { get; set; } = new();
}

public class CodebaseMemorySettings
{
    /// <summary>
    /// The site's own index and daemon directory, defaulting to one under the local application data.
    /// codebase-memory admits one client per data directory and refuses any whose path, or any parent
    /// of it, a broad group such as Authenticated Users may write to.
    /// </summary>
    public string? DataDirectoryPath { get; set; }

    /// <summary>
    /// An absolute path to the executable or a wrapper script, for hosts where npx is not reachable.
    /// </summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// The arguments <see cref="ExecutablePath"/> is started with, before the ones the site appends.
    /// </summary>
    public string[]? ExecutableArguments { get; set; }
}

public class HealthCheckSettings
{
    public bool EnableHealthChecks { get; set; }
}

public class TelegramBotSettings
{
    public string? Token { get; set; }
    public string[] ChatIds { get; set; } = [];
}

public class AzureOpenAIOptions
{
    public string? ChatModel { get; set; }
    public Uri? ChatEndpoint { get; set; }
    public string? ChatApiKey { get; set; }
}

public class OpenAIOptions
{
    public string? ChatModel { get; set; }
    public Uri? ChatEndpoint { get; set; }
    public string? ChatApiKey { get; set; }
}
