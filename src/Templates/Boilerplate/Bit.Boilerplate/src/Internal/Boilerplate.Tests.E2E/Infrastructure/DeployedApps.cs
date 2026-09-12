using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// The demo apps as platform-agnostic identities; which platform carries an app, and how to reach it there, is the
/// <see cref="IAppOpener"/>s' knowledge.
/// </summary>
public enum App
{
    AdminPanel,
    AdminPanelWasmStandalone,
    Todo,
    TodoAot,
    TodoSmall,
    TodoOffline,
    Sales,
}

/// <summary>
/// The always-on demo apps that .github/workflows/{admin-sample,todo-sample,sales-module-demo}.cd.yml build from this
/// very template and deploy, so these tests run against real deployments instead of a locally started server.
/// </summary>
public static class DeployedApps
{
    /// <summary>Admin module, prerendered PWA served by Server.Web with a standalone API (Azure Web App + Cloudflare CDN).</summary>
    public const string AdminPanel = "https://adminpanel.bitplatform.dev/";

    /// <summary>Admin module, Blazor WebAssembly Standalone (Azure Static Web App).</summary>
    public const string AdminPanelWasmStandalone = "https://adminpanel.bitplatform.cc/";

    /// <summary>Todo sample, prerendered PWA served by Server.Web with a standalone API (Azure Web App + Cloudflare CDN).</summary>
    public const string Todo = "https://todo.bitplatform.dev/";

    /// <summary>Todo sample, AOT compiled Blazor WebAssembly Standalone (Azure Static Web App).</summary>
    public const string TodoAot = "https://todo-aot.bitplatform.cc/";

    /// <summary>Todo sample, Blazor WebAssembly Standalone published for the smallest download footprint (Azure Static Web App).</summary>
    public const string TodoSmall = "https://todo-small.bitplatform.cc/";

    /// <summary>Todo sample with the offline in-browser SQLite database and sync, full-offline bswup mode (Azure Static Web App).</summary>
    public const string TodoOffline = "https://todo-offline.bitplatform.cc/";

    /// <summary>Sales module, prerendered PWA served by Server.Web with an integrated API (Azure Web App + Cloudflare CDN).</summary>
    public const string Sales = "https://sales.bitplatform.dev/";

    /// <summary>Standalone API of both admin panel apps (Azure Web App).</summary>
    public const string AdminPanelApi = "https://adminpanel-api.bitplatform.dev/";

    /// <summary>Standalone API of every todo sample app (Azure Web App).</summary>
    public const string TodoApi = "https://todo-api.bitplatform.dev/";

    /// <summary>Where an app is served, for a test that reaches it over http rather than through an opener.</summary>
    public static string AddressOf(App app) => app switch
    {
        App.AdminPanel => AdminPanel,
        App.AdminPanelWasmStandalone => AdminPanelWasmStandalone,
        App.Todo => Todo,
        App.TodoAot => TodoAot,
        App.TodoSmall => TodoSmall,
        App.TodoOffline => TodoOffline,
        App.Sales => Sales,
        _ => throw new ArgumentOutOfRangeException(nameof(app), app, "Unknown app"),
    };

    /// <summary>
    /// The API an app talks to, for the tests that call it directly through <see cref="DeployedApiClientProvider"/>. The
    /// admin and todo apps share one standalone API each, while Sales' API is integrated into the app itself.
    /// </summary>
    public static string ApiOf(App app) => app switch
    {
        App.AdminPanel or App.AdminPanelWasmStandalone => AdminPanelApi,
        App.Todo or App.TodoAot or App.TodoSmall or App.TodoOffline => TodoApi,
        App.Sales => Sales,
        _ => throw new ArgumentOutOfRangeException(nameof(app), app, "Unknown app"),
    };

    /// <summary>
    /// Where an API is deployed on this machine - its IIS site folder and the application pool it runs in - taken from
    /// the deploy inputs in .github/workflows/*.cd.yml. Sales has neither of its own: its API is part of the web app.
    /// </summary>
    public static (string SitePath, string AppPool) DeploymentOfApi(string apiAddress) => apiAddress switch
    {
        AdminPanelApi => (@"C:\inetpub\AdminPanelApi", "AdminPanelApi"),
        TodoApi => (@"C:\inetpub\TodoApi", "TodoApi"),
        Sales => (@"C:\inetpub\SalesModule", "SalesModule"),
        _ => throw new ArgumentOutOfRangeException(nameof(apiAddress), apiAddress, "Unknown API"),
    };

    public const string TodoAndroidAppId = "com.bitplatform.Todo.Template";
    public const string AdminPanelAndroidAppId = "com.bitplatform.AdminPanel.Template";

    /// <summary>Null for an app with no Android build.</summary>
    public static string? AndroidAppIdOf(App app) => app switch
    {
        App.Todo => TodoAndroidAppId,
        App.AdminPanel => AdminPanelAndroidAppId,
        _ => null,
    };

    /// <summary>Velopack app ids of the published Windows apps, used by <see cref="IPlaywrightExtensions.LaunchWindowsApp"/>.</summary>
    public const string TodoWindowsAppId = "TodoSample.Client.Windows";
    public const string AdminPanelWindowsAppId = "AdminPanel.Client.Windows";
    public const string SalesWindowsAppId = "SalesModule.Client.Windows";

    /// <summary>Null for an app with no Windows build.</summary>
    public static string? WindowsAppIdOf(App app) => app switch
    {
        App.Todo => TodoWindowsAppId,
        App.AdminPanel => AdminPanelWindowsAppId,
        App.Sales => SalesWindowsAppId,
        _ => null,
    };

    /// <summary>
    /// The Velopack release feed an installed Windows app updates itself from - the R2 bucket
    /// <c>vpk upload s3</c> publishes to, and the <c>WindowsUpdate.FilesUrl</c> the CD workflow builds the app with
    /// (See .github/workflows/*.cd.yml). Null for an app with no Windows build.
    /// </summary>
    public static string? WindowsUpdateFeedOf(App app) => app switch
    {
        App.Todo => "https://windows-todo.bitplatform.dev/",
        App.AdminPanel => "https://windows-adminpanel.bitplatform.dev/",
        App.Sales => "https://windows-sales.bitplatform.dev/",
        _ => null,
    };
}
