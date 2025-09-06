namespace Bit.Websites.Platform.Server;

public class AppSettings
{
    public HealthCheckSettings HealthCheckSettings { get; set; } = default!;

    public TelegramBotSettings TelegramBotSettings { get; set; } = default!;

    public AzureOpenAIOptions AzureOpenAI { get; set; } = default!;
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
