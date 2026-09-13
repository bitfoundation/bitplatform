namespace Bit.Websites.Platform.Server;

public class AppSettings
{
    public HealthCheckSettings HealthCheckSettings { get; set; } = default!;

    public TelegramBotSettings TelegramBotSettings { get; set; } = default!;

    public AzureOpenAIOptions AzureOpenAI { get; set; } = default!;

    public OpenAIOptions OpenAI { get; set; } = default!;

    public CodebaseMemorySettings CodebaseMemory { get; set; } = default!;
}

public class CodebaseMemorySettings
{
    /// <summary>
    /// Git checkout to index for the chatbot, or empty to skip indexing on this machine.
    /// </summary>
    public string? SourceRepositoryPath { get; set; }

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
