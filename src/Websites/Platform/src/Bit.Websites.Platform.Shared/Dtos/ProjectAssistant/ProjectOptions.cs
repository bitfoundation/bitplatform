using System.ComponentModel;

namespace Bit.Websites.Platform.Shared.Dtos.ProjectAssistant;

public class ProjectOptions
{
    [Description("The project name: letters, digits, underscores and dots, each dot separated part starting with a letter, under 50 characters, e.g. Contoso.Clinic")]
    public string? Name { get; set; }

    [Description("Sqlite, SqlServer, PostgreSQL, MySql or Other")]
    public string? Database { get; set; }

    [Description("Local, S3, AzureBlobStorage or Other")]
    public string? FilesStorage { get; set; }

    [Description("Integrated or Standalone")]
    public string? Api { get; set; }

    [Description("GitHub, Azure or None")]
    public string? Pipeline { get; set; }

    [Description("None, Admin or Sales")]
    public string? Module { get; set; }

    [Description("None or reCaptcha")]
    public string? Captcha { get; set; }

    [Description("Fluent2, Fluent, Cupertino or Material")]
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

    [Description("The native apps to set up and build on this machine now: android, ios, macos and windows. The web app is always set up.")]
    public List<string>? Platforms { get; set; }

    [Description("code, vs, rider or none")]
    public string? Ide { get; set; }
}
