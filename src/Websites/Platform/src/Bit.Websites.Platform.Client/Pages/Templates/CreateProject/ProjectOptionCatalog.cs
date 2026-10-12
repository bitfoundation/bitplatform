namespace Bit.Websites.Platform.Client.Pages.Templates.CreateProject;

public sealed record ProjectOptionChoice(string Value, string Text);

public sealed class ProjectOption
{
    private static readonly ProjectSetup defaults = new();

    public required string Key { get; init; }
    public required string Title { get; init; }
    public required string Summary { get; init; }
    public string? DemoUrl { get; init; }
    public string? Flag { get; init; }
    public ProjectOptionChoice[]? Choices { get; init; }
    public Func<ProjectSetup, bool>? GetBool { get; init; }
    public Action<ProjectSetup, bool>? SetBool { get; init; }
    public Func<ProjectSetup, string>? GetChoice { get; init; }
    public Action<ProjectSetup, string>? SetChoice { get; init; }
    public Func<ProjectSetup, bool>? IsAvailable { get; init; }

    public bool IsChoice => Choices is not null;

    public bool IsModified(ProjectSetup setup) => IsChoice
        ? GetChoice!(setup) != GetChoice!(defaults)
        : GetBool!(setup) != GetBool!(defaults);

    public bool Applies(ProjectSetup setup) => IsAvailable?.Invoke(setup) ?? true;
}

public static class ProjectOptionCatalog
{
    public static readonly ProjectOption[] All =
    [
        new()
        {
            Key = "module",
            Title = "Starting point",
            Summary = "Start from a blank app, or from a working admin panel or online store you then make your own.",
            Flag = "--module",
            Choices = [new("None", "Blank app"), new("Admin", "Admin panel"), new("Sales", "Online store")],
            GetChoice = s => s.Module,
            SetChoice = (s, v) => s.Module = v
        },
        new()
        {
            Key = "sample",
            Title = "Sample pages",
            Summary = "Adds a small todo feature that shows how a page, its API and its data fit together.",
            DemoUrl = "https://todo.bitplatform.dev/todo",
            Flag = "--sample",
            GetBool = s => s.Sample,
            SetBool = (s, v) => s.Sample = v
        },
        new()
        {
            Key = "multitenant",
            Title = "Many organizations",
            Summary = "One deployment serves many companies or teams, each with its own users and data.",
            DemoUrl = "https://adminpanel.bitplatform.dev",
            Flag = "--multitenant",
            GetBool = s => s.Multitenant,
            SetBool = (s, v) => s.Multitenant = v
        },
        new()
        {
            Key = "database",
            Title = "Database",
            Summary = "Where the server keeps its data. SQLite needs nothing installed; with Aspire the others run in a container for you.",
            Flag = "--database",
            Choices = [new("Sqlite", "SQLite"), new("PostgreSQL", "PostgreSQL"), new("SqlServer", "SQL Server"), new("MySql", "MySQL"), new("Other", "Other")],
            GetChoice = s => s.Database,
            SetChoice = (s, v) => s.Database = v
        },
        new()
        {
            Key = "filesStorage",
            Title = "File storage",
            Summary = "Where uploaded files such as profile pictures go.",
            Flag = "--filesStorage",
            Choices = [new("Local", "This server's disk"), new("S3", "S3 compatible"), new("AzureBlobStorage", "Azure Blob Storage"), new("Other", "Other")],
            GetChoice = s => s.FilesStorage,
            SetChoice = (s, v) => s.FilesStorage = v
        },
        new()
        {
            Key = "offlineDb",
            Title = "Works offline",
            Summary = "A database on the user's device, so the app keeps working without internet. Makes the app bigger and slower to start.",
            DemoUrl = "https://todo-offline.bitplatform.cc",
            Flag = "--offlineDb",
            GetBool = s => s.OfflineDb,
            SetBool = (s, v) => s.OfflineDb = v
        },
        new()
        {
            Key = "redis",
            Title = "Redis",
            Summary = "Shared caching, fast background jobs and running on more than one server.",
            Flag = "--redis",
            GetBool = s => s.Redis,
            SetBool = (s, v) => s.Redis = v
        },
        new()
        {
            Key = "api",
            Title = "API hosting",
            Summary = "One server for the website and the API, or two you deploy and scale separately.",
            Flag = "--api",
            Choices = [new("Integrated", "One server"), new("Standalone", "Separate API server")],
            GetChoice = s => s.Api,
            SetChoice = (s, v) => s.Api = v
        },
        new()
        {
            Key = "aspire",
            Title = "Aspire",
            Summary = "Runs the database, Redis, storage and a test mail server in containers while you develop, and helps you deploy.",
            Flag = "--aspire",
            GetBool = s => s.Aspire,
            SetBool = (s, v) => s.Aspire = v
        },
        new()
        {
            Key = "cloudflare",
            Title = "Cloudflare",
            Summary = "Puts the backend behind Cloudflare's CDN for speed and less load on your server.",
            Flag = "--cloudflare",
            GetBool = s => s.Cloudflare,
            SetBool = (s, v) => s.Cloudflare = v
        },
        new()
        {
            Key = "realProject",
            Title = "Fast restarts",
            Summary = "Keeps the project's containers running between runs, so debugging a real project starts fast.",
            Flag = "--realProject",
            GetBool = s => s.RealProject,
            SetBool = (s, v) => s.RealProject = v
        },
        new()
        {
            Key = "signalR",
            Title = "Live updates and AI chatbot",
            Summary = "The server pushes changes to open apps in real time, and the AI chatbot comes with it.",
            Flag = "--signalR",
            GetBool = s => s.SignalR,
            SetBool = (s, v) => s.SignalR = v
        },
        new()
        {
            Key = "notification",
            Title = "Push notifications",
            Summary = "Notifications on Android, iOS, macOS and in web browsers.",
            Flag = "--notification",
            GetBool = s => s.Notification,
            SetBool = (s, v) => s.Notification = v
        },
        new()
        {
            Key = "captcha",
            Title = "Bot protection",
            Summary = "Google reCAPTCHA on the sign-up page.",
            DemoUrl = "https://todo.bitplatform.dev/sign-up",
            Flag = "--captcha",
            Choices = [new("None", "None"), new("reCaptcha", "reCAPTCHA")],
            GetChoice = s => s.Captcha,
            SetChoice = (s, v) => s.Captcha = v
        },
        new()
        {
            Key = "ads",
            Title = "Ads",
            Summary = "Rewarded Google ads: people watch one in return for something you give them.",
            DemoUrl = "https://adminpanel.bitplatform.dev/settings/UpgradeAccount",
            Flag = "--ads",
            GetBool = s => s.Ads,
            SetBool = (s, v) => s.Ads = v
        },
        new()
        {
            Key = "brouter",
            Title = "bit Brouter",
            Summary = "Nested routes, route guards, data loaders and pages that keep their state. Off keeps Blazor's own router, for the smallest download.",
            DemoUrl = "https://brouter.bitplatform.dev",
            Flag = "--brouter",
            GetBool = s => s.Brouter,
            SetBool = (s, v) => s.Brouter = v
        },
        new()
        {
            Key = "sentry",
            Title = "Sentry",
            Summary = "Sends errors and crashes from every app to Sentry.",
            Flag = "--sentry",
            GetBool = s => s.Sentry,
            SetBool = (s, v) => s.Sentry = v
        },
        new()
        {
            Key = "appInsights",
            Title = "Application Insights",
            Summary = "Sends logs and errors from the server and the apps to Azure Application Insights.",
            Flag = "--appInsights",
            GetBool = s => s.AppInsights,
            SetBool = (s, v) => s.AppInsights = v
        },
        new()
        {
            Key = "pipeline",
            Title = "CI/CD",
            Summary = "Pipelines that build, test and deploy the project on every push.",
            Flag = "--pipeline",
            Choices = [new("GitHub", "GitHub Actions"), new("Azure", "Azure DevOps"), new("None", "None")],
            GetChoice = s => s.Pipeline,
            SetChoice = (s, v) => s.Pipeline = v
        },
        new()
        {
            Key = "githubRepo",
            Title = "GitHub repository",
            Summary = "Creates a private GitHub repository and pushes the project to it.",
            Flag = "--github-repo",
            GetBool = s => s.GitHubRepo,
            SetBool = (s, v) => s.GitHubRepo = v,
            IsAvailable = s => s.Pipeline is "GitHub"
        },
        new()
        {
            Key = "ide",
            Title = "Open in",
            Summary = "The editor bit opens the project in, with its recommended extensions.",
            Flag = "--ide",
            Choices = [new("code", "VS Code"), new("vs", "Visual Studio"), new("rider", "Rider"), new("none", "Don't open it")],
            GetChoice = s => s.Ide,
            SetChoice = (s, v) => s.Ide = v
        },
        new()
        {
            Key = "theme",
            Title = "Theme",
            Summary = "The design system the UI ships with. Every theme has light and dark modes.",
            Flag = "--theme",
            Choices = [new("Fluent2", "Fluent 2"), new("Fluent", "Fluent"), new("Cupertino", "Cupertino"), new("Material", "Material")],
            GetChoice = s => s.Theme,
            SetChoice = (s, v) => s.Theme = v
        },
        new()
        {
            Key = "android",
            Title = "Android app",
            Summary = "Installs what the Android app needs and builds it now.",
            Flag = "--platforms",
            GetBool = s => s.Android,
            SetBool = (s, v) => s.Android = v
        },
        new()
        {
            Key = "ios",
            Title = "iOS app",
            Summary = "Installs what the iOS app needs and builds it now.",
            Flag = "--platforms",
            GetBool = s => s.Ios,
            SetBool = (s, v) => s.Ios = v
        },
        new()
        {
            Key = "macOS",
            Title = "macOS app",
            Summary = "Installs what the macOS app needs and builds it now.",
            Flag = "--platforms",
            GetBool = s => s.MacOS,
            SetBool = (s, v) => s.MacOS = v
        },
        new()
        {
            Key = "windows",
            Title = "Windows app",
            Summary = "Installs what the Windows app needs and builds it now.",
            Flag = "--platforms",
            GetBool = s => s.Windows,
            SetBool = (s, v) => s.Windows = v
        }
    ];

    public static ProjectOption Get(string key) => All.Single(o => o.Key == key);
}
