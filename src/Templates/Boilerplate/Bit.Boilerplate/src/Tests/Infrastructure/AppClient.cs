//+:cnd:noEmit
using Boilerplate.Shared.Infrastructure.Services.Contracts;

namespace Boilerplate.Tests.Infrastructure;

/// <summary>
/// The app as a client of the api, without its UI: one per user a test needs (See
/// <see cref="AppTestServer.CreateAppClient"/>). What it creates calls the api anonymously until <see cref="AuthManager"/>
/// signs a user in, and as that user from then on.
/// </summary>
public sealed class AppClient : IAsyncDisposable
{
    private readonly AsyncServiceScope scope;

    internal AppClient(AppTestServer server)
    {
        Server = server;
        scope = server.WebApp.Services.CreateAsyncScope();
    }

    public AppTestServer Server { get; }

    /// <summary>
    /// The app's own services. What runs in Server.Api, <see cref="AppDbContext"/> among it, is resolved from
    /// <see cref="AppTestServer.ApiApp"/> instead.
    /// </summary>
    public IServiceProvider Services => scope.ServiceProvider;

    public AuthManager AuthManager => Services.GetRequiredService<AuthManager>();

    //#if (api == "Standalone")
    /// <summary>
    /// An HttpClient that works exactly like the one in the app: a test that calls the api through it tests not only the
    /// api's logic, but also the way the client connects to it, only without the UI. ExceptionDelegatingHandler turns an
    /// error response into the exception the app would get, for example, and the request goes to Server.Web first, which
    /// forwards it to Server.Api through YARP.
    /// </summary>
    //#else
    /// <summary>
    /// An HttpClient that works exactly like the one in the app: a test that calls the api through it tests not only the
    /// api's logic, but also the way the client connects to it, only without the UI. ExceptionDelegatingHandler turns an
    /// error response into the exception the app would get, for example, and the request goes to Server.Web, which
    /// hosts the api.
    /// </summary>
    //#endif
    public HttpClient HttpClient => field ??= Services.GetRequiredService<HttpClient>();

    /// <summary>
    /// The app's client of the api controller <typeparamref name="T"/> - <see cref="IUserController"/> and the rest -
    /// which calls the api the way <see cref="HttpClient"/> does.
    /// </summary>
    public T GetController<T>()
        where T : class, IAppController
    {
        return Services.GetRequiredService<T>();
    }

    public ValueTask DisposeAsync() => scope.DisposeAsync();
}
