namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// The always-on demo apps that .github/workflows/{admin-sample,todo-sample,sales-module-demo}.cd.yml build from this
/// very template (dotnet new bit-bp) and deploy. Together they cover Client.Web, Server.Web, Client.Maui and
/// Client.Windows, so tests in this project run against real production deployments instead of a locally started server.
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

    /// <summary>Application id of the published Todo Android app, for <see cref="HybridAppConnector.LaunchAndroidApp"/>.</summary>
    public const string TodoAndroidAppId = "com.bitplatform.Todo.Template";

    /// <summary>Application id of the published AdminPanel Android app, for <see cref="HybridAppConnector.LaunchAndroidApp"/>.</summary>
    public const string AdminPanelAndroidAppId = "com.bitplatform.AdminPanel.Template";

    /// <summary>Velopack app id of the published Todo Windows app, for <see cref="HybridAppConnector.LaunchWindowsApp"/>.</summary>
    public const string TodoWindowsAppId = "TodoSample.Client.Windows";

    /// <summary>Velopack app id of the published AdminPanel Windows app, for <see cref="HybridAppConnector.LaunchWindowsApp"/>.</summary>
    public const string AdminPanelWindowsAppId = "AdminPanel.Client.Windows";

    /// <summary>Velopack app id of the published Sales Windows app, for <see cref="HybridAppConnector.LaunchWindowsApp"/>.</summary>
    public const string SalesWindowsAppId = "SalesModule.Client.Windows";

}
