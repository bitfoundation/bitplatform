//+:cnd:noEmit
using System.Reflection;
using Microsoft.Extensions.Options;

namespace Boilerplate.Tests.Features.ServerWeb;

/// <summary>
/// Pins what Server.Web's own <see cref="HttpClient"/> - the one pre-rendering and Blazor Server circuits call the api
/// with - is allowed to take from the caller, and how its transport is shared.
/// <para>
/// ⚠ <c>AddTestProjectServices</c> re-registers a transient, HttpContext-free <c>HttpClient</c> <i>after</i> calling
/// the real <c>AddServerWebProjectServices</c>, and last registration wins - so by default these tests would assert
/// against the harness' own client, which reproduces the very defects under test. Each test therefore drops that last
/// descriptor in <c>configureTestServices</c> (which runs after it), leaving Server.Web's real registration as the one
/// the container resolves.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest"), TestCategory("Security")]
public partial class ServerSideHttpClientTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// The client ip the api sees has to be the one the pipeline resolved, never the one the caller typed.
    /// <para>
    /// The factory copies every inbound <c>X-</c> header onto the outgoing client so a CDN's request context reaches
    /// the api. <c>X-Forwarded-For</c> starts with <c>X-</c> too, and an untrusted hop leaves it fully intact in
    /// <c>Request.Headers</c> (<c>ForwardedHeadersMiddleware</c> rejects rather than strips it), so a blanket copy
    /// hands the api an attacker-chosen address. That address is the partition key of the identity rate limiter and
    /// the value persisted as <c>UserSession.IP</c>, so rotating one header buys a fresh 30/min budget per forged ip
    /// and writes forged provenance into the user's own "where your account was accessed from" screen.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task ServerSideHttpClient_Should_SendTheResolvedClientIp_NotTheOneTheCallerSent()
    {
        await using var server = new AppTestServer();
        await server.Build(configureTestServices: services =>
                    {
                        services.AddIntegrationApiOnlyTestsServices();
                        UseTheRealServerWebHttpClient(services);
                    }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("app.example.com");
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.7";
        httpContext.Request.Headers["X-Forwarded-Proto"] = "http";
        httpContext.Request.Headers["X-App-Platform"] = "web"; // A header the api does consume; it must still pass through.
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.9");
        httpContextAccessor.HttpContext = httpContext;

        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        Assert.AreEqual("198.51.100.9", string.Join(',', httpClient.DefaultRequestHeaders.GetValues("X-Forwarded-For")),
            "The forwarded-for the server sends must be the address the pipeline resolved. Seeing 203.0.113.7 here means " +
            "the caller can choose the ip the identity rate limiter partitions by and the ip written to UserSession.IP.");

        Assert.IsFalse(httpClient.DefaultRequestHeaders.Contains("X-Forwarded-Proto"),
            "A caller supplied X-Forwarded-Proto would flip Request.IsHttps on the api side, which decides the scheme of " +
            "every reset-password / confirmation link the api generates.");

        // The control: the blanket copy still exists and still forwards the headers it is there for.
        Assert.AreEqual("web", string.Join(',', httpClient.DefaultRequestHeaders.GetValues("X-App-Platform")));
    }

    /// <summary>
    /// <c>X-Origin</c> is added by the factory itself, and <c>HttpHeaders.Add</c> appends rather than replaces - so a
    /// caller who sends one turns it into a two-valued header. <c>GetWebAppUrl()</c> then reads the comma-joined
    /// string, fails <c>Uri.TryCreate(..., Absolute)</c> and throws <c>BadRequestException</c> from the scoped Fido2
    /// factory that runs during controller construction, on anonymous endpoints included; and with <c>signalR</c> on,
    /// the <c>HubConnection</c> registration's <c>.Single()</c> throws while building the connection.
    /// </summary>
    [TestMethod]
    public async Task ServerSideHttpClient_Should_NotLetTheCallerAddASecondXOrigin()
    {
        await using var server = new AppTestServer();
        await server.Build(configureTestServices: services =>
                    {
                        services.AddIntegrationApiOnlyTestsServices();
                        UseTheRealServerWebHttpClient(services);
                    }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("app.example.com");
        httpContext.Request.Headers["X-Origin"] = "https://evil.example";
        httpContextAccessor.HttpContext = httpContext;

        var origins = scope.ServiceProvider.GetRequiredService<HttpClient>().DefaultRequestHeaders.GetValues("X-Origin").ToArray();

        Assert.HasCount(1, origins,
            "X-Origin must be single valued; a second value makes GetWebAppUrl() reject the comma-joined string and " +
            "makes the HubConnection registration's Single() throw.");
        Assert.AreEqual("https://app.example.com/", origins[0]);
    }

    /// <summary>
    /// One transport, and therefore one connection pool, for the whole process. A <c>SocketsHttpHandler</c> built per
    /// scope gives every pre-render request its own empty pool that is disposed again at the end of the request, so
    /// each one pays a fresh tcp connect and - against an https api - a fresh tls handshake, and
    /// <c>PooledConnectionLifetime</c> never has anything to amortise.
    /// </summary>
    [TestMethod]
    public async Task ServerSideHttpClient_Should_ShareOneTransportAcrossScopes()
    {
        await using var server = new AppTestServer();
        await server.Build(configureTestServices: services =>
                    {
                        services.AddIntegrationApiOnlyTestsServices();
                        UseTheRealServerWebHttpClient(services);
                    }).Start(TestContext.CancellationToken);

        var transports = new List<HttpMessageHandler>();

        for (var i = 0; i < 2; i++)
        {
            await using var scope = server.WebApp.Services.CreateAsyncScope();
            var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("app.example.com");
            httpContextAccessor.HttpContext = httpContext;

            transports.Add(FindTransport(scope.ServiceProvider.GetRequiredService<HttpClient>()));
        }

        Assert.AreSame(transports[0], transports[1],
            "Every DI scope built its own transport handler, so no connection is ever reused between pre-render requests.");
    }

    /// <summary>
    /// The persist/hydrate contract needs ONE dictionary and ONE RegisterOnPersisting subscription per scope. When the
    /// service was transient, every generated api client proxy got its own; two of them persisting the same key made
    /// <c>ComponentStatePersistenceManager</c> throw while pausing and then skip <c>PersistStateAsync</c> altogether,
    /// discarding the whole page's prerender state rather than just the duplicate.
    /// </summary>
    [TestMethod]
    public async Task PrerenderStateService_Should_BeOneInstancePerScope()
    {
        await using var server = new AppTestServer();
        await server.Build(configureTestServices: services =>
                    {
                        services.AddIntegrationApiOnlyTestsServices();
                        UseTheRealServerWebHttpClient(services);
                    }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        Assert.AreSame(scope.ServiceProvider.GetRequiredService<IPrerenderStateService>(),
                       scope.ServiceProvider.GetRequiredService<IPrerenderStateService>());
    }

    /// <summary>
    /// The settings instance every consumer injects has to be the one the options pipeline validated. Binding a second
    /// private copy is what let a missing <c>WebAppRender</c> section surface as a NullReferenceException from
    /// <c>ConfigureMiddlewares</c> - which runs before <c>app.RunAsync()</c>, and therefore before
    /// <c>ValidateOnStart</c> ever gets to report it.
    /// </summary>
    [TestMethod]
    public async Task ServerWebSettings_Should_BeTheValidatedOptionsInstance()
    {
        await using var server = new AppTestServer();
        await server.Build(configureTestServices: services =>
                    {
                        services.AddIntegrationApiOnlyTestsServices();
                        UseTheRealServerWebHttpClient(services);
                    }).Start(TestContext.CancellationToken);

        Assert.AreSame(server.WebApp.Services.GetRequiredService<IOptions<ServerWebSettings>>().Value,
                       server.WebApp.Services.GetRequiredService<ServerWebSettings>());
    }

    /// <summary>
    /// Drops the harness' own <c>HttpClient</c> registration so the container resolves Server.Web's real one.
    /// See the class remarks for why this is necessary.
    /// </summary>
    private static void UseTheRealServerWebHttpClient(IServiceCollection services)
    {
        services.Remove(services.Last(descriptor => descriptor.ServiceType == typeof(HttpClient)));
    }

    /// <summary>
    /// Walks to the innermost handler of the chain the factory built, skipping the non-disposing wrapper that exists
    /// so a scope's disposal cannot reach the shared transport.
    /// </summary>
    private static HttpMessageHandler FindTransport(HttpClient httpClient)
    {
        var handler = (HttpMessageHandler)typeof(HttpMessageInvoker)
            .GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(httpClient)!;

        while (handler is DelegatingHandler delegatingHandler && delegatingHandler.InnerHandler is not null)
        {
            handler = delegatingHandler.InnerHandler;
        }

        return handler;
    }
}
