//+:cnd:noEmit
using Bunit;
using Hangfire;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

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
    public IServiceCollection WebAppServiceCollection => webAppBuilder?.Services ?? throw new InvalidOperationException($"{nameof(WebAppServiceCollection)} is null. Call {nameof(Build)} method first.");
    //#if (api == "Standalone")
    //#if (IsInsideProjectTemplate)
    /*
    //#endif
    public IServiceCollection ApiAppServiceCollection => apiAppBuilder?.Services ?? throw new InvalidOperationException($"{nameof(ApiAppServiceCollection)} is null. Call {nameof(Build)} method first.");
    //#if (IsInsideProjectTemplate)
    */
    //#endif
    //#else
    /// <summary>The registrations of the host Server.Api's services live in (See <see cref="ApiApp"/>).</summary>
    public IServiceCollection ApiAppServiceCollection => WebAppServiceCollection;
    //#endif
    //#endif
    /// <summary>
    /// Under localhost rather than an IP: WebAuthn only takes a domain as its relying party id.
    /// </summary>
    public readonly Uri WebAppAddress = new(GenerateServerUrl("localhost"));

    //#if (api == "Standalone")
    //#if (IsInsideProjectTemplate)
    /*
    //#endif
    /// <summary>
    /// A host other than <see cref="WebAppAddress"/>'s, as in a real deployment: cookies ignore the port, so under
    /// the same host name the api's cookies would reach the web app as well.
    /// </summary>
    public readonly Uri ApiAppAddress = new(GenerateServerUrl("127.0.0.1"));
    //#if (IsInsideProjectTemplate)
    */
    //#endif
    //#else
    public Uri ApiAppAddress => WebAppAddress;
    //#endif

    /// <param name="configureTestServices">
    /// Overrides the services of Server.Web and Server.Api alike, so the test does not have to know which of the two a
    /// service lives in. Prefer it to the two below.
    /// </param>
    /// <param name="configureTestWebAppServices">Overrides Server.Web's services, the client services it hosts included.</param>
    /// <param name="configureTestApiAppServices">Overrides Server.Api's services.</param>
    /// <param name="configureTestConfiguration">Overrides Server.Web's and Server.Api's configuration.</param>
    public AppTestServer Build(Action<IServiceCollection>? configureTestServices = null,
        Action<IServiceCollection>? configureTestWebAppServices = null,
        Action<IServiceCollection>? configureTestApiAppServices = null,
        Action<ConfigurationManager>? configureTestConfiguration = null)
    {
        if (webApp != null)
            throw new InvalidOperationException("Server is already built.");

        //#if (api == "Standalone")
        //#if (IsInsideProjectTemplate)
        /*
        //#endif
        apiApp = BuildApi(configureTestServices, configureTestApiAppServices, configureTestConfiguration);

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
            // The build output's wwwroot, not the source one: the build adds files there that the project folder lacks,
            // such as the _framework/bit.blazor.*.es2019.js scripts every page loads Blazor with.
            WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot"),
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
        webAppBuilder.Configuration["ServerAddress"] = ApiAppAddress.ToString();
        webAppBuilder.Configuration["WebAppUrl"] = WebAppAddress.ToString();
        webAppBuilder.WebHost.UseUrls(WebAppAddress.ToString());

        AppEnvironment.Set(webAppBuilder.Environment.EnvironmentName);

        webAppBuilder.Configuration.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Web");

        configureTestConfiguration?.Invoke(webAppBuilder.Configuration);

        webAppBuilder.AddTestWebProjectServices();

        // The HttpClient the app's services get here, the typed api clients among them (See AppClient.HttpClient).
        webAppBuilder.Services.AddTransient(BuildAppHttpClient);

        if (ClientBrowserContext is null)
        {
            // No browser signs in on this server, so the test does it through AuthManager (See AddBrowserlessClientServices).
            webAppBuilder.Services.AddBrowserlessClientServices();
        }

        configureTestServices?.Invoke(webAppBuilder.Services);
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
    private WebApplication BuildApi(Action<IServiceCollection>? configureTestServices,
        Action<IServiceCollection>? configureTestApiAppServices,
        Action<ConfigurationManager>? configureTestConfiguration)
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
        apiAppBuilder.Configuration["WebAppUrl"] = WebAppAddress.ToString();
        apiAppBuilder.WebHost.UseUrls(ApiAppAddress.ToString());

        AppEnvironment.Set(apiAppBuilder.Environment.EnvironmentName);

        apiAppBuilder.Configuration.AddSharedConfigurations();

        configureTestConfiguration?.Invoke(apiAppBuilder.Configuration);

        apiAppBuilder.Services.AddSharedProjectServices(apiAppBuilder.Configuration);
        Server.Api.Program.AddServerApiProjectServices(apiAppBuilder);
        apiAppBuilder.AddTestApiProjectServices();

        configureTestServices?.Invoke(apiAppBuilder.Services);
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
            // Points the app in the browser at this server's api.
            await ClientBrowserContext.AddInitScriptAsync($"window.startupParams = function() {{ return [ 'ServerAddress={ApiAppAddress}' ]; }};");
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
    /// A new client of the api, with a user of its own (See <see cref="AppClient"/>).
    /// </summary>
    public AppClient CreateAppClient()
    {
        if (ClientBrowserContext is not null)
            throw new InvalidOperationException("A browser drives this server, so the app's services here are the browser's, not the in-memory ones an AppClient keeps its user's tokens in (See AddBrowserlessClientServices). Sign in through the browser's pages instead.");

        return new(this);
    }

    /// <summary>
    /// An HttpClient that sends its requests straight to Server.Api, with no handler of the app's in between: an error
    /// response stays a response, nothing is retried and no header is added. The caller disposes it.
    /// </summary>
    /// <param name="handler">
    /// The transport, such as an <see cref="HttpClientHandler"/> that does not follow redirects. A plain one when null.
    /// </param>
    public HttpClient CreateRawHttpClient(HttpMessageHandler? handler = null)
    {
        return new HttpClient(handler ?? new HttpClientHandler()) { BaseAddress = ApiAppAddress };
    }

    private HttpClient BuildAppHttpClient(IServiceProvider services)
    {
        var httpClient = new HttpClient(services.GetRequiredService<HttpMessageHandlersChainFactory>().Invoke())
        {
            BaseAddress = WebAppAddress
        };

        // The origin the api builds its links for (e-mail confirmation and the rest), which the app's clients send as well.
        httpClient.DefaultRequestHeaders.Add("X-Origin", WebAppAddress.ToString());

        return httpClient;
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
    internal static string ProjectDirectoryOf(string projectName)
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
    /// <summary>
    /// A free port under <paramref name="host"/>. Kestrel binds localhost to both 127.0.0.1 and ::1 and fails when either
    /// is taken, so for localhost a port is only handed out once it is free on both.
    /// </summary>
    private static string GenerateServerUrl(string host)
    {
        while (true)
        {
            using var ipv4Listener = new TcpListener(IPAddress.Loopback, 0);
            ipv4Listener.Start();
            var port = ((IPEndPoint)ipv4Listener.LocalEndpoint).Port;

            if (host is "localhost" && Socket.OSSupportsIPv6)
            {
                using var ipv6Listener = new TcpListener(IPAddress.IPv6Loopback, port);
                try
                {
                    ipv6Listener.Start();
                }
                catch (SocketException exception) when (exception.SocketErrorCode is SocketError.AddressAlreadyInUse)
                {
                    continue;
                }
                catch (SocketException)
                {
                    // No IPv6 loopback on this machine, which Kestrel skips as well.
                }
            }

            return $"http://{host}:{port}/";
        }
    }
}
