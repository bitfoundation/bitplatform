using System.Text;

namespace Bit.Websites.Platform.Client.Pages.Templates.CreateProject;

public class ProjectSetup
{
    public string Name { get; set; } = "MyFirstProject";
    public string Database { get; set; } = "Sqlite";
    public string FilesStorage { get; set; } = "Local";
    public string Api { get; set; } = "Integrated";
    public string Pipeline { get; set; } = "GitHub";
    public bool GitHubRepo { get; set; } = true;
    public string Module { get; set; } = "None";
    public string Captcha { get; set; } = "None";
    public string Theme { get; set; } = "Fluent2";
    public bool Aspire { get; set; } = true;
    public bool Multitenant { get; set; } = true;
    public bool Notification { get; set; } = true;
    public bool Cloudflare { get; set; } = true;
    public bool Redis { get; set; }
    public bool SignalR { get; set; }
    public bool OfflineDb { get; set; }
    public bool Sentry { get; set; }
    public bool AppInsights { get; set; }
    public bool Ads { get; set; }
    public bool Brouter { get; set; } = true;
    public bool Sample { get; set; }
    public bool Android { get; set; }
    public bool Ios { get; set; }
    public bool MacOS { get; set; }
    public bool Windows { get; set; }
    public string Ide { get; set; } = "code";

    public ProjectSetup Clone() => (ProjectSetup)MemberwiseClone();

    public void CopyFrom(ProjectSetup other)
    {
        Name = other.Name;
        Database = other.Database;
        FilesStorage = other.FilesStorage;
        Api = other.Api;
        Pipeline = other.Pipeline;
        GitHubRepo = other.GitHubRepo;
        Module = other.Module;
        Captcha = other.Captcha;
        Theme = other.Theme;
        Aspire = other.Aspire;
        Multitenant = other.Multitenant;
        Notification = other.Notification;
        Cloudflare = other.Cloudflare;
        Redis = other.Redis;
        SignalR = other.SignalR;
        OfflineDb = other.OfflineDb;
        Sentry = other.Sentry;
        AppInsights = other.AppInsights;
        Ads = other.Ads;
        Brouter = other.Brouter;
        Sample = other.Sample;
        Android = other.Android;
        Ios = other.Ios;
        MacOS = other.MacOS;
        Windows = other.Windows;
        Ide = other.Ide;
    }

    public void ApplyPreset(ProjectSetup preset)
    {
        var name = Name;
        CopyFrom(preset);
        Name = name;
    }

    public string ToCommand()
    {
        var defaults = new ProjectSetup();
        StringBuilder command = new($"bit new {Name} ");

        Choice(command, "captcha", Captcha, defaults.Captcha);
        Choice(command, "pipeline", Pipeline, defaults.Pipeline);

        if (Pipeline is "GitHub" && GitHubRepo)
        {
            command.Append("--github-repo ");
        }

        Flag(command, "sample", Sample, defaults.Sample);
        Choice(command, "module", Module, defaults.Module);
        Flag(command, "cloudflare", Cloudflare, defaults.Cloudflare);
        Flag(command, "sentry", Sentry, defaults.Sentry);
        Flag(command, "appInsights", AppInsights, defaults.AppInsights);
        Flag(command, "signalR", SignalR, defaults.SignalR);
        Flag(command, "ads", Ads, defaults.Ads);
        Flag(command, "redis", Redis, defaults.Redis);
        Flag(command, "aspire", Aspire, defaults.Aspire);
        Choice(command, "filesStorage", FilesStorage, defaults.FilesStorage);
        Flag(command, "offlineDb", OfflineDb, defaults.OfflineDb);
        Choice(command, "database", Database, defaults.Database);
        Flag(command, "notification", Notification, defaults.Notification);
        Choice(command, "api", Api, defaults.Api);
        Flag(command, "multitenant", Multitenant, defaults.Multitenant);
        Flag(command, "brouter", Brouter, defaults.Brouter);
        Choice(command, "theme", Theme, defaults.Theme);

        if (Android || Ios || MacOS || Windows)
        {
            List<string> platforms = ["web"];
            if (Android) platforms.Add("android");
            if (Ios) platforms.Add("ios");
            if (MacOS) platforms.Add("macos");
            if (Windows) platforms.Add("windows");
            command.Append($"--platforms {string.Join(',', platforms)} ");
        }

        Choice(command, "ide", Ide, defaults.Ide);

        command.Append("--yes");

        return command.ToString();
    }

    private static void Flag(StringBuilder command, string name, bool value, bool defaultValue)
    {
        if (value == defaultValue)
            return;

        command.Append(value ? $"--{name} " : $"--{name} false ");
    }

    private static void Choice(StringBuilder command, string name, string value, string defaultValue)
    {
        if (value == defaultValue)
            return;

        command.Append($"--{name} {value} ");
    }

    public static ProjectSetup RealProduct() => new()
    {
        Database = "PostgreSQL",
        FilesStorage = "S3",
        Redis = true,
        SignalR = true
    };
}
