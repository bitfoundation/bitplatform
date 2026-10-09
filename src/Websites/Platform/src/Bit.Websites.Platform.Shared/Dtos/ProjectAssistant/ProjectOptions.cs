namespace Bit.Websites.Platform.Shared.Dtos.ProjectAssistant;

public class ProjectOptions
{
    public string? Name { get; set; }

    public string? Database { get; set; }

    public string? FilesStorage { get; set; }

    public string? Api { get; set; }

    public string? Pipeline { get; set; }

    public string? Module { get; set; }

    public string? Captcha { get; set; }

    public string? Theme { get; set; }

    public bool? Aspire { get; set; }

    public bool? Multitenant { get; set; }

    public bool? Notification { get; set; }

    public bool? Cloudflare { get; set; }

    public bool? Redis { get; set; }

    public bool? SignalR { get; set; }

    public bool? OfflineDb { get; set; }

    public bool? Sentry { get; set; }

    public bool? AppInsights { get; set; }

    public bool? Ads { get; set; }

    public bool? Brouter { get; set; }

    public bool? Sample { get; set; }

    public List<string>? Platforms { get; set; }

    public string? Ide { get; set; }
}
