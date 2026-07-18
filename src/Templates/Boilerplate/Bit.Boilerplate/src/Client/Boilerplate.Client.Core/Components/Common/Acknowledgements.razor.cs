//+:cnd:noEmit
namespace Boilerplate.Client.Core.Components.Common;

/// <summary>
/// A tip of the hat to the open-source projects, tools, and services this app is built with.
/// It also doubles as the third-party attribution / license notice required by many of those licenses.
/// The lists below mirror the actual dependencies of the generated project: entries wrapped in the
/// template's conditional markers (e.g. //if (redis == true)) only ship when the matching option is selected.
/// No localization on purpose: product names and license identifiers are proper nouns that stay in English.
/// </summary>
public partial class Acknowledgements
{
    /// <param name="Name">Product/library display name.</param>
    /// <param name="Url">Official website (falls back to the repository when there is no distinct homepage).</param>
    /// <param name="Repo">Public source repository, or null for closed-source products.</param>
    /// <param name="License">SPDX license identifier (or a short human phrase for multi/every-non-SPDX licenses).</param>
    private record Dependency(string Name, string? Url, string? Repo, string License);

    /// <summary>
    /// Production NuGet packages (the app's runtime dependencies), grouped by product.
    /// </summary>
    private static readonly Dependency[] RuntimeDependencies =
    [
        new("bit platform ❤️", "https://bitplatform.dev", "https://github.com/bitfoundation/bitplatform", "MIT"),
        new(".NET", "https://dot.net", "https://github.com/dotnet/runtime", "MIT"),
        new("ASP.NET Core - Blazor", "https://asp.net", "https://github.com/dotnet/aspnetcore", "MIT"),
        //#if (aspire == true)
        new(".NET Aspire", "https://aspire.dev", "https://github.com/dotnet/aspire", "MIT"),
        //#endif
        new("Entity Framework Core", "https://learn.microsoft.com/ef", "https://github.com/dotnet/efcore", "MIT"),
        new(".NET MAUI", "https://dotnet.microsoft.com/apps/maui", "https://github.com/dotnet/maui", "MIT"),
        new("OpenTelemetry .NET", "https://opentelemetry.io", "https://github.com/open-telemetry/opentelemetry-dotnet", "Apache-2.0"),
        new("Hangfire", "https://www.hangfire.io", "https://github.com/HangfireIO/Hangfire", "LGPL-3.0 / Commercial"),
        new("Mapperly", "https://mapperly.riok.app", "https://github.com/riok/mapperly", "Apache-2.0"),
        new("FusionCache", "https://github.com/ZiggyCreatures/FusionCache", "https://github.com/ZiggyCreatures/FusionCache", "MIT"),
        new("ASP.NET Core OData", "https://www.odata.org", "https://github.com/OData/AspNetCoreOData", "MIT"),
        new("Humanizer", "https://github.com/Humanizr/Humanizer", "https://github.com/Humanizr/Humanizer", "MIT"),
        //#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
        new("Microsoft.Extensions.AI", "https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai", "https://github.com/dotnet/extensions", "MIT"),
        new("Semantic Kernel", "https://learn.microsoft.com/semantic-kernel", "https://github.com/microsoft/semantic-kernel", "MIT"),
        new("Microsoft Agent Framework", "https://learn.microsoft.com/agent-framework", "https://github.com/microsoft/agent-framework", "MIT"),
        //#endif
        //#if (redis == true)
        new("Redis", "https://redis.io", "https://github.com/redis/redis", "AGPL-3.0-only"),
        new("StackExchange.Redis", "https://stackexchange.github.io/StackExchange.Redis", "https://github.com/StackExchange/StackExchange.Redis", "MIT"),
        //#endif
        //#if (database == "SqlServer")
        new("Microsoft SQL Server (EF Core provider)", "https://learn.microsoft.com/ef/core", "https://github.com/dotnet/efcore", "MIT"),
        //#endif
        //#if (database == "PostgreSQL")
        new("Npgsql (PostgreSQL provider)", "https://www.npgsql.org", "https://github.com/npgsql/efcore.pg", "PostgreSQL"),
        new("pgvector-dotnet", "https://github.com/pgvector/pgvector-dotnet", "https://github.com/pgvector/pgvector-dotnet", "MIT"),
        //#endif
        //#if (database == "MySql")
        new("Pomelo (MySQL provider)", "https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql", "https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql", "MIT"),
        //#endif
        //#if (notification == true)
        new("Firebase Cloud Messaging", "https://firebase.google.com/docs/cloud-messaging", "https://github.com/dotnet/android-libraries", "MIT / Apache-2.0"),
        new("Plugin.LocalNotification", "https://github.com/thudugala/Plugin.LocalNotification", "https://github.com/thudugala/Plugin.LocalNotification", "MIT"),
        new("AdsPush", "https://github.com/adessoTurkey-dotNET/AdsPush", "https://github.com/adessoTurkey-dotNET/AdsPush", "BSD-3-Clause"),
        //#endif
        //#if (filesStorage == "S3")
        new("AWS SDK for .NET", "https://aws.amazon.com/sdk-for-net", "https://github.com/aws/aws-sdk-net", "Apache-2.0"),
        //#endif
        //#if (filesStorage == "AzureBlobStorage" || appInsights == true)
        new("Azure SDK for .NET", "https://learn.microsoft.com/dotnet/azure/sdk/azure-sdk-for-dotnet", "https://github.com/Azure/azure-sdk-for-net", "MIT"),
        //#endif
        //#if (sentry == true)
        new("Sentry", "https://sentry.io", "https://github.com/getsentry/sentry-dotnet", "MIT"),
        //#endif
        //#if (appInsights == true)
        new("BlazorApplicationInsights", "https://github.com/IvanJosipovic/BlazorApplicationInsights", "https://github.com/IvanJosipovic/BlazorApplicationInsights", "MIT"),
        //#endif
        //#if (offlineDb == true)
        new("Community Toolkit Datasync", "https://github.com/CommunityToolkit/Datasync", "https://github.com/CommunityToolkit/Datasync", "MIT"),
        //#endif
        new("SQLite", "https://sqlite.org", "https://github.com/sqlite/sqlite", "Public Domain"),
        new("Microsoft.Identity.Web", "https://github.com/AzureAD/microsoft-identity-web", "https://github.com/AzureAD/microsoft-identity-web", "MIT"),
        new("Twilio", "https://www.twilio.com", "https://github.com/twilio/twilio-csharp", "MIT"),
        new("Magick.NET", "https://github.com/dlemstra/Magick.NET", "https://github.com/dlemstra/Magick.NET", "Apache-2.0"),
        new("QRCoder", "https://github.com/Shane32/QRCoder", "https://github.com/Shane32/QRCoder", "MIT"),
        new("HtmlSanitizer", "https://github.com/mganss/HtmlSanitizer", "https://github.com/mganss/HtmlSanitizer", "MIT"),
        new("Scalar", "https://scalar.com", "https://github.com/scalar/scalar", "MIT"),
        new("ASP.NET API Versioning", "https://github.com/dotnet/aspnet-api-versioning", "https://github.com/dotnet/aspnet-api-versioning", "MIT"),
        new("AspNet.Security.OAuth.Providers", "https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers", "https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers", "Apache-2.0"),
        new("Xabaril HealthChecks", "https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks", "https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks", "Apache-2.0"),
        new("FIDO2 .NET Library", "https://github.com/passwordless-lib/fido2-net-lib", "https://github.com/passwordless-lib/fido2-net-lib", "MIT"),
        new("libphonenumber-csharp", "https://twcclegg.github.io/libphonenumber-csharp", "https://github.com/twcclegg/libphonenumber-csharp", "Apache-2.0"),
        new("FluentEmail", "https://github.com/lukencode/FluentEmail", "https://github.com/lukencode/FluentEmail", "MIT"),
        new("FluentStorage", "https://github.com/robinrodricks/FluentStorage", "https://github.com/robinrodricks/FluentStorage", "MIT"),
        new("NWebsec", "https://www.nwebsec.com", "https://github.com/NWebsec/NWebsec", "BSD-3-Clause"),
        new("DistributedLock", "https://github.com/madelson/DistributedLock", "https://github.com/madelson/DistributedLock", "MIT"),
        new("Velopack", "https://velopack.io", "https://github.com/velopack/velopack", "MIT"),
        new("Meziantou.Framework", "https://github.com/meziantou/Meziantou.Framework", "https://github.com/meziantou/Meziantou.Framework", "MIT"),
        new("EmbedIO", "https://unosquare.github.io/embedio", "https://github.com/unosquare/embedio", "MIT"),
        new("Microsoft Edge WebView2", "https://developer.microsoft.com/microsoft-edge/webview2", null, "BSD-3-Clause"),
        new("Oscore.Maui.AppStoreInfo", "https://github.com/oscoreio/Maui.AppStoreInfo", "https://github.com/oscoreio/Maui.AppStoreInfo", "MIT"),
        new("Plugin.Maui.AppRating", "https://github.com/FabriBertani/Plugin.Maui.AppRating", "https://github.com/FabriBertani/Plugin.Maui.AppRating", "MIT"),
        //#if (aspire == true)
        new("Keycloak", "https://www.keycloak.org", "https://github.com/keycloak/keycloak", "Apache-2.0"),
        new(".NET Aspire Community Toolkit", "https://github.com/CommunityToolkit/Aspire", "https://github.com/CommunityToolkit/Aspire", "MIT"),
        new("Mailpit", "https://mailpit.axllent.org", "https://github.com/axllent/mailpit", "MIT"),
        //#endif
        //#if (aspire == true && filesStorage == "S3")
        new("MinIO", "https://www.min.io", "https://github.com/minio/minio", "AGPL-3.0-only"),
        //#endif
        //#if (signalR == true)
        new("Model Context Protocol (C# SDK)", "https://modelcontextprotocol.io", "https://github.com/modelcontextprotocol/csharp-sdk", "MIT"),
        new("Azure SignalR Service", "https://azure.microsoft.com/products/signalr-service", "https://github.com/Azure/azure-signalr", "MIT"),
        //#endif
    ];

    /// <summary>
    /// NuGet packages and npm packages used only while developing and testing the app.
    /// </summary>
    private static readonly Dependency[] DevelopmentDependencies =
    [
        new("C#", "https://dotnet.microsoft.com/en-us/languages/csharp", "https://github.com/dotnet/csharplang", "MIT"),
        new("TypeScript", "https://www.typescriptlang.org", "https://github.com/microsoft/TypeScript", "Apache-2.0"),
        new("MSTest", "https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro", "https://github.com/microsoft/testfx", "MIT"),
        new("Playwright for .NET", "https://playwright.dev/dotnet", "https://github.com/microsoft/playwright-dotnet", "MIT"),
        new("esbuild", "https://esbuild.github.io", "https://github.com/evanw/esbuild", "MIT"),
        new("Dart Sass", "https://sass-lang.com/dart-sass", "https://github.com/sass/dart-sass", "MIT"),
        new("FakeItEasy", "https://fakeiteasy.github.io", "https://github.com/FakeItEasy/FakeItEasy", "MIT"),
        new("EF Core Tools (dotnet-ef)", "https://learn.microsoft.com/ef/core/cli/dotnet", "https://github.com/dotnet/efcore", "MIT"),
        new("Microsoft.Extensions.TimeProvider.Testing", "https://github.com/dotnet/extensions", "https://github.com/dotnet/extensions", "MIT"),
        //#if (advancedTests == true)
        new("Otp.NET", "https://github.com/kspearrin/Otp.NET", "https://github.com/kspearrin/Otp.NET", "MIT"),
        //#endif
    ];

    /// <summary>
    /// Recommended editor extensions (VS Code, from .devcontainer/.vscode) and Visual Studio components (from .vsconfig).
    /// </summary>
    private static readonly Dependency[] IdeExtensions =
    [
        new("GitHub Copilot Chat", "https://marketplace.visualstudio.com/items?itemName=GitHub.copilot-chat", "https://github.com/microsoft/vscode-copilot-chat", "MIT"),
        new("C# (ms-dotnettools.csharp)", "https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp", "https://github.com/dotnet/vscode-csharp", "MIT"),
        new("C# Dev Kit", "https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit", null, "Proprietary"),
        new(".NET MAUI (VS Code)", "https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui", null, "Proprietary"),
        new("Aspire (VS Code)", "https://marketplace.visualstudio.com/items?itemName=microsoft-aspire.aspire-vscode", "https://github.com/microsoft/aspire", "MIT"),
        new(".NET Install Tool", "https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.vscode-dotnet-runtime", "https://github.com/dotnet/vscode-dotnet-runtime", "MIT"),
        new("Container Tools (Docker)", "https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-docker", "https://github.com/microsoft/vscode-docker", "MIT"),
        new("Dev Containers", "https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers", null, "Proprietary"),
        new("Blazor WASM Companion", "https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.blazorwasm-companion", "https://github.com/dotnet/razor", "MIT"),
        new("Live Sass Compiler", "https://marketplace.visualstudio.com/items?itemName=glenn2223.live-sass", "https://github.com/glenn2223/vscode-live-sass-compiler", "MIT"),
        new("ResX Manager", "https://marketplace.visualstudio.com/items?itemName=TomEnglert.ResXManager", "https://github.com/dotnet/ResXResourceManager", "MIT"),
        new("VS \"ASP.NET & web development\" workload", "https://visualstudio.microsoft.com/vs/features/web", null, "Proprietary"),
        new("VS \".NET MAUI development\" workload", "https://dotnet.microsoft.com/apps/maui", null, "Proprietary"),
    ];

    /// <summary>
    /// SDKs, runtimes, and platform tooling used to build, containerize, and run the app
    /// (from .devcontainer features, .vsconfig, global.json, and native Apple/Android build requirements).
    /// </summary>
    private static readonly Dependency[] Tooling =
    [
        new(".NET SDK", "https://dotnet.microsoft.com", "https://github.com/dotnet/sdk", "MIT"),
        new("Node.js", "https://nodejs.org", "https://github.com/nodejs/node", "MIT"),
        new("Docker", "https://www.docker.com", "https://github.com/moby/moby", "Apache-2.0"),
        new("Kubernetes", "https://kubernetes.io", "https://github.com/kubernetes/kubernetes", "Apache-2.0"),
        new("Python", "https://www.python.org", "https://github.com/python/cpython", "PSF-2.0"),
        new("PowerShell", "https://learn.microsoft.com/powershell", "https://github.com/PowerShell/PowerShell", "MIT"),
        new("Helm", "https://helm.sh", "https://github.com/helm/helm", "Apache-2.0"),
        new("Minikube", "https://minikube.sigs.k8s.io", "https://github.com/kubernetes/minikube", "Apache-2.0"),
        new("Apple Xcode", "https://developer.apple.com/xcode", null, "Proprietary"),
        new("Android SDK", "https://developer.android.com/studio", null, "Proprietary"),
    ];

    /// <summary>
    /// CI/CD and deployment tooling taken from the pipeline definitions: GitHub Actions (.github/workflows) or
    /// Azure Pipelines (.azure-devops), plus the optional Cloudflare CDN integration. Everything here is build/deploy
    /// time only and never ships inside the app.
    /// </summary>
    private static readonly Dependency[] Deployment =
    [
        //#if (pipeline == "GitHub")
        new("GitHub Actions", "https://github.com/features/actions", "https://github.com/actions/runner", "MIT"),
        new("Azure WebApps Deploy", "https://github.com/Azure/webapps-deploy", "https://github.com/Azure/webapps-deploy", "MIT"),
        new("setup-xcode", "https://github.com/maxim-lobanov/setup-xcode", "https://github.com/maxim-lobanov/setup-xcode", "MIT"),
        new("Apple Actions", "https://github.com/Apple-Actions", "https://github.com/Apple-Actions", "MIT"),
        new("base64-to-file", "https://github.com/timheuer/base64-to-file", "https://github.com/timheuer/base64-to-file", "MIT"),
        new("variable-substitution", "https://github.com/devops-actions/variable-substitution", "https://github.com/devops-actions/variable-substitution", "MIT"),
        //#endif
        //#if (pipeline == "Azure")
        new("Azure Pipelines", "https://azure.microsoft.com/products/devops/pipelines", null, "Proprietary"),
        //#endif
        //#if (cloudflare == true)
        new("Cloudflare", "https://www.cloudflare.com", null, "Proprietary"),
        //#endif
        //#if (pipeline == "GitHub" && cloudflare == true)
        new("cloudflare-purge-action", "https://github.com/jakejarvis/cloudflare-purge-action", "https://github.com/jakejarvis/cloudflare-purge-action", "MIT"),
        //#endif
    ];
}
