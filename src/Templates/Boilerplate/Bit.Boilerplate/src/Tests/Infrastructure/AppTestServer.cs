//+:cnd:noEmit
using Bunit;
using Hangfire;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;

namespace Boilerplate.Tests.Infrastructure;

/// <summary>
/// Test server, capable of running backend API and Blazor Server UI for integration and UI tests using Playwright.
/// </summary>
public partial class AppTestServer(IBrowserContext? ClientBrowserContext = null) : IAsyncDisposable
{
    private WebApplication? webApp;
    private WebApplicationBuilder? webAppBuilder;
    //#if (api == "Standalone")
    //#if (IsInsideProjectTemplate)
    /*
    //#endif
    private WebApplication? apiApp;
    //#if (advancedTests == true)
    private WebApplicationBuilder? apiAppBuilder;
    //#endif
    //#if (IsInsideProjectTemplate)
    */
    //#endif
    //#endif

    public WebApplication WebApp => webApp ?? throw new InvalidOperationException($"{nameof(WebApp)} is null. Call {nameof(Build)} method first.");

    //#if (api == "Standalone")
    //#if (IsInsideProjectTemplate)
    /*
    //#endif
    /// <summary>
    /// Server.Api, a host of its own next to <see cref="WebApp"/>, which forwards /api and /hangfire to it - the way the
    /// two are deployed. The database, Hangfire and the captured identity e-mails live here, so resolve them from its services.
    /// </summary>
    public WebApplication ApiApp => apiApp ?? throw new InvalidOperationException($"{nameof(ApiApp)} is null. Call {nameof(Build)} method first.");
    //#if (IsInsideProjectTemplate)
    */
    //#endif
    //#else
    /// <summary>
    /// The host Server.Api's services live in - the database, Hangfire and the captured identity e-mails among them.
    /// The api is integrated into Server.Web, so that is <see cref="WebApp"/> itself.
    /// </summary>
    public WebApplication ApiApp => WebApp;
    //#endif

    //#if (advancedTests == true)
    public IServiceCollection WebAppServices => webAppBuilder?.Services ?? throw new InvalidOperationException($"{nameof(WebAppServices)} is null. Call {nameof(Build)} method first.");
    //#if (api == "Standalone")
    //#if (IsInsideProjectTemplate)
    /*
    //#endif
    public IServiceCollection ApiAppServices => apiAppBuilder?.Services ?? throw new InvalidOperationException($"{nameof(ApiAppServices)} is null. Call {nameof(Build)} method first.");
    //#if (IsInsideProjectTemplate)
    */
    //#endif
    //#else
    /// <summary>The registrations of the host Server.Api's services live in (See <see cref="ApiApp"/>).</summary>
    public IServiceCollection ApiAppServices => WebAppServices;
    //#endif
    //#endif
    public readonly Uri WebAppServerAddress = new(GenerateServerUrl());

    //#if (api == "Standalone")
    //#if (IsInsideProjectTemplate)
    /*
    //#endif
    public readonly Uri ApiServerAddress = new(GenerateServerUrl());
    //#if (IsInsideProjectTemplate)
    */
    //#endif
    //#else
    public Uri ApiServerAddress => WebAppServerAddress;
    //#endif

    /// <param name="configureTestWebAppServices">Overrides Server.Web's services, the client services it hosts included.</param>
    /// <param name="configureTestConfigurations">Overrides Server.Web's and Server.Api's configuration.</param>
    /// <param name="configureTestApiAppServices">Overrides Server.Api's services.</param>
    public AppTestServer Build(Action<IServiceCollection>? configureTestWebAppServices = null,
        Action<ConfigurationManager>? configureTestConfigurations = null,
        Action<IServiceCollection>? configureTestApiAppServices = null)
    {
        if (webApp != null)
            throw new InvalidOperationException("Server is already built.");

        //#if (api == "Standalone")
        //#if (IsInsideProjectTemplate)
        /*
        //#endif
        apiApp = BuildApi(configureTestConfigurations, configureTestApiAppServices);

        //#if (IsInsideProjectTemplate)
        */
        //#endif
        //#endif
        webAppBuilder = WebApplication.CreateBuilder(options: new()
        {
            EnvironmentName = Environments.Development,
            //#if (api == "Standalone")
            //#if (IsInsideProjectTemplate)
            /*
            //#endif
            ContentRootPath = ProjectDirectoryOf(typeof(Server.Web.Program).Assembly.GetName().Name!),
            //#if (IsInsideProjectTemplate)
            */
            //#endif
            //#endif
            ApplicationName = typeof(Server.Web.Program).Assembly.GetName().Name
        });

        //#if (api == "Standalone")
        // As in a real deployment, ServerAddress is Server.Api's own address: Server.Web's HttpClient calls it, JwtBearer
        // reads its discovery document there, and /api and /hangfire are forwarded to it.
        //#endif
        webAppBuilder.Configuration["ServerAddress"] = ApiServerAddress.ToString();
        webAppBuilder.WebHost.UseUrls(WebAppServerAddress.ToString());

        AppEnvironment.Set(webAppBuilder.Environment.EnvironmentName);

        webAppBuilder.Configuration.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Web");

        configureTestConfigurations?.Invoke(webAppBuilder.Configuration);

        webAppBuilder.AddTestProjectServices(WebAppServerAddress);

        configureTestWebAppServices?.Invoke(webAppBuilder.Services);
        //#if (api == "Integrated")
        configureTestApiAppServices?.Invoke(webAppBuilder.Services);
        //#endif

        var app = webApp = webAppBuilder.Build();

        app.ConfigureMiddlewares();

        return this;
    }

    //#if (api == "Standalone")
    //#if (IsInsideProjectTemplate)
    /*
    //#endif
    /// <summary>
    /// Builds Server.Api the way its own Program.Main does, minus Sentry and the database initialization
    /// (See <see cref="TestsAssemblyInitializer"/>).
    /// </summary>
    private WebApplication BuildApi(Action<ConfigurationManager>? configureTestConfigurations, Action<IServiceCollection>? configureTestApiAppServices)
    {
        var apiAppBuilder = WebApplication.CreateBuilder(options: new()
        {
            EnvironmentName = Environments.Development,
            ApplicationName = typeof(Server.Api.Program).Assembly.GetName().Name,
            ContentRootPath = ProjectDirectoryOf(typeof(Server.Api.Program).Assembly.GetName().Name!)
        });
        //#if (advancedTests == true)
        this.apiAppBuilder = apiAppBuilder;
        //#endif

        // The links the api builds (e-mail confirmation, reset password, ...) point at the web app, which is another host here.
        apiAppBuilder.Configuration["WebAppUrl"] = WebAppServerAddress.ToString();
        apiAppBuilder.WebHost.UseUrls(ApiServerAddress.ToString());

        AppEnvironment.Set(apiAppBuilder.Environment.EnvironmentName);

        apiAppBuilder.Configuration.AddSharedConfigurations();

        configureTestConfigurations?.Invoke(apiAppBuilder.Configuration);

        apiAppBuilder.Services.AddSharedProjectServices(apiAppBuilder.Configuration);
        Server.Api.Program.AddServerApiProjectServices(apiAppBuilder);
        apiAppBuilder.AddTestApiProjectServices();

        configureTestApiAppServices?.Invoke(apiAppBuilder.Services);

        var app = apiAppBuilder.Build();

        Server.Api.Program.ConfigureMiddlewares(app);

        return app;
    }

    //#if (IsInsideProjectTemplate)
    */
    //#endif
    //#endif
    public async Task Start(CancellationToken cancellationToken)
    {
        //#if (api == "Standalone")
        //#if (IsInsideProjectTemplate)
        /*
        //#endif
        // The api first: the web app forwards to it and validates tokens against its discovery document.
        await ApiApp.StartAsync(cancellationToken);
        //#if (IsInsideProjectTemplate)
        */
        //#endif
        //#endif
        await WebApp.StartAsync(cancellationToken);
        if (ClientBrowserContext is not null)
        {
            await ClientBrowserContext.AddInitScriptAsync($"window.startupParams = function() {{ return [ 'ServerAddress={ApiServerAddress}' ]; }};");
        }
    }

    /// <summary>
    /// Waits until Hangfire reports no more background jobs waiting or running (enqueued + processing == 0).
    /// Actions like sending an e-mail are handled by Hangfire background jobs, so tests call this to deterministically
    /// wait for that work to finish instead of polling for its side effects (e.g. an e-mail being captured by CapturingBackgroundJobClient).
    /// </summary>
    public async Task WaitForBackgroundJobsToComplete(CancellationToken cancellationToken)
    {
        var monitoringApi = ApiApp.Services.GetRequiredService<JobStorage>().GetMonitoringApi();

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);

        while (true)
        {
            var statistics = monitoringApi.GetStatistics();
            if (statistics.Enqueued is 0 && statistics.Processing is 0)
                return;

            if (DateTimeOffset.UtcNow >= deadline)
                throw new TimeoutException("Hangfire background jobs did not complete within 30 seconds.");

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }
    }

    /// <summary>
    /// Returns the newest captured e-mail addressed to <paramref name="email"/> that satisfies <paramref name="predicate"/>,
    /// or throws after a timeout. E-mails are captured synchronously as they are requested (See
    /// <see cref="TestIdentityEmailService"/>), so the message is normally already present; the short poll only guards
    /// against a caller reading a hair before the triggering request finished.
    /// </summary>
    public async Task<CapturedEmail> WaitForCapturedEmail(string email, Func<CapturedEmail, bool> predicate, CancellationToken cancellationToken)
    {
        var store = ApiApp.Services.GetRequiredService<EmailCaptureStore>();

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);

        while (true)
        {
            // Newest-first so a freshly requested code wins over an earlier (now expired) one still in the capture.
            var match = store.Captured.Reverse().FirstOrDefault(capturedEmail => capturedEmail.IsTo(email) && predicate(capturedEmail));
            if (match is not null)
                return match;

            if (DateTimeOffset.UtcNow >= deadline)
            {
                var recipients = store.Captured.Select(capturedEmail => capturedEmail.ToEmailAddress).Distinct().ToArray();
                throw new InvalidOperationException(
                    $"No captured e-mail addressed to '{email}' matched within the timeout. " +
                    $"Captured {store.Captured.Count} e-mail(s) to: [{string.Join(", ", recipients)}].");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }
    }

    /// <summary>
    /// Creates a bUnit <see cref="BunitContext"/> for rendering the client app's pages/components in-memory
    /// (no browser and no WebAssembly runtime) while still talking to this running test server for real HTTP calls,
    /// which is dramatically faster than a full Playwright end-to-end run and lets a test focus on a single
    /// page/component.
    /// </summary>
    public BunitContext CreateBunitContext()
    {
        var ctx = new BunitContext();

        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        // These bUnit services will be replaced with the real server services below, so remove them first to avoid duplicates.
        ctx.Services.RemoveAll<HttpClient>();
        ctx.Services.RemoveAll<IAuthorizationService>();
        ctx.Services.RemoveAll<AuthenticationStateProvider>();
        ctx.Services.RemoveAll<IAuthorizationPolicyProvider>();

        var currentBunitServices = ctx.Services.ToList();

        // Share the whole server container (same as Blazor Server), except the services that only work with a
        // live Blazor circuit - bUnit already registered its own working test doubles for these.
        foreach (var descriptor in webAppBuilder!.Services)
        {
            if (currentBunitServices.Any(s => s.ServiceType == descriptor.ServiceType))
                continue;

            ctx.Services.Add(descriptor);
        }

        // There is no browser storage (nor HttpContext) in bUnit, so fake it in-memory (also used by the API
        // integration tests). Registered last so it wins over the server-side storage copied above.
        ctx.Services.AddScoped<IStorageService, TestStorageService>();

        ctx.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        return ctx;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAndDispose(webApp);
        //#if (api == "Standalone")
        //#if (IsInsideProjectTemplate)
        /*
        //#endif
        await StopAndDispose(apiApp);
        //#if (IsInsideProjectTemplate)
        */
        //#endif
        //#endif
    }

    private static async Task StopAndDispose(WebApplication? app)
    {
        if (app != null)
        {
            try
            {
                await app.StopAsync();
            }
            catch (OperationCanceledException) { }
            await app.DisposeAsync();
        }
    }

    //#if (api == "Standalone")
    //#if (IsInsideProjectTemplate)
    /*
    //#endif
    /// <summary>
    /// The source directory of a server project, which its host uses as the content root - as <c>dotnet run</c> does - so
    /// it reads that project's own appsettings.json and wwwroot: Server.Web and Server.Api both ship an appsettings.json,
    /// and only one of the two can land in this project's output. Found by walking up from <see cref="AppContext.BaseDirectory"/>
    /// (See Server.Web's FileWatcherService.FindSrcDirectory).
    /// </summary>
    private static string ProjectDirectoryOf(string projectName)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var projectDirectory = Path.Combine(directory.FullName, "Server", projectName);
            if (File.Exists(Path.Combine(projectDirectory, $"{projectName}.csproj")))
                return projectDirectory;
        }

        throw new DirectoryNotFoundException($"No Server/{projectName} directory was found above {AppContext.BaseDirectory}.");
    }

    //#if (IsInsideProjectTemplate)
    */
    //#endif
    //#endif
    private static string GenerateServerUrl()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return $"http://127.0.0.1:{port}/";
    }
}
