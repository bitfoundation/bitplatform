//+:cnd:noEmit
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Boilerplate.Client.Core.Infrastructure.Services;

/// <summary>
/// The app's SignalR connection. In the browser SignalR's assemblies are lazy loaded, so the connection is built the first time it starts.
/// </summary>
public partial class AppHubConnection : IAsyncDisposable
{
    // The BlazorWebAssemblyLazyLoad items of Client.Web. SignalR.Client.Core is not one: the router reads every type of this project at startup, IRetryPolicy included.
    private static readonly string[] assemblies =
    [
        "Microsoft.AspNetCore.SignalR.Client.wasm",
        "Microsoft.AspNetCore.SignalR.Common.wasm",
        "Microsoft.AspNetCore.SignalR.Protocols.Json.wasm",
        "Microsoft.AspNetCore.Http.Connections.Client.wasm",
        "Microsoft.AspNetCore.Http.Connections.Common.wasm",
        "Microsoft.AspNetCore.Connections.Abstractions.wasm",
        "Microsoft.Extensions.Features.wasm",
        "System.Net.WebSockets.Client.wasm",
        "System.Net.WebSockets.wasm",
        "System.Net.ServerSentEvents.wasm"
    ];

    [AutoInject] private IServiceProvider serviceProvider = default!;
    [AutoInject] private LazyAssemblyLoader lazyAssemblyLoader = default!;

    private Connection? connection;
    private Task? starting;
    private readonly Task startupDelay = Task.Delay(TimeSpan.FromSeconds(5));
    private readonly List<Handler> handlers = [];

    public event Func<Exception?, Task>? Closed;
    public event Func<Exception?, Task>? Reconnecting;
    public event Func<string?, Task>? Reconnected;

    public bool IsConnected => connection?.IsConnected is true;

    public string? ConnectionId => connection?.ConnectionId;

    public bool IsHubException(Exception exception) => connection?.IsHubException(exception) is true;

    // Never connects sooner than 5 seconds after the app started, so SignalR stays out of the app's startup.
    // False when the connection was already up or being opened by another call, so it may carry an older access token.
    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        await startupDelay.WaitAsync(cancellationToken);

        if (starting is { IsCompleted: false } || IsConnected)
        {
            await Connect().WaitAsync(cancellationToken);
            return false;
        }

        await Connect().WaitAsync(cancellationToken);
        return true;
    }

    public async Task InvokeAsync(string methodName, object? arg1, CancellationToken cancellationToken = default)
    {
        await (await GetStartedConnection(cancellationToken)).InvokeAsync<object?>(methodName, [arg1], cancellationToken);
    }

    public async Task<TResult> InvokeAsync<TResult>(string methodName, CancellationToken cancellationToken = default)
    {
        return await (await GetStartedConnection(cancellationToken)).InvokeAsync<TResult>(methodName, [], cancellationToken);
    }

    public async IAsyncEnumerable<TResult> StreamAsync<TResult>(string methodName, object? arg1, object? arg2, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in (await GetStartedConnection(cancellationToken)).StreamAsync<TResult>(methodName, [arg1, arg2], cancellationToken))
        {
            yield return item;
        }
    }

    public IDisposable On<T1>(string methodName, Func<T1, Task> handler) => Add(methodName, c => c.On(methodName, handler));

    public IDisposable On<TResult>(string methodName, Func<Task<TResult>> handler) => Add(methodName, c => c.On(methodName, handler));

    public IDisposable On<T1, TResult>(string methodName, Func<T1, Task<TResult>> handler) => Add(methodName, c => c.On(methodName, handler));

    public IDisposable On<T1, T2, TResult>(string methodName, Func<T1, T2, Task<TResult>> handler) => Add(methodName, c => c.On(methodName, handler));

    public void Remove(string methodName)
    {
        handlers.RemoveAll(h => h.MethodName == methodName);
        connection?.Remove(methodName);
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
        }
    }

    private Task Connect() => starting is { IsCompleted: false } ? starting : starting = Start();

    private async Task Start()
    {
        if (connection is null)
        {
            if (AppPlatform.IsBrowser && AppPlatform.IsAot is false)
            {
                await lazyAssemblyLoader.LoadAssembliesAsync(assemblies);
            }

            connection = Connection.Create(this);
        }

        if (connection.IsConnected is false)
        {
            await connection.StartAsync(CancellationToken.None);
        }
    }

    private async Task<Connection> GetStartedConnection(CancellationToken cancellationToken)
    {
        // A call that needs the connection starts it at once, without StartAsync's 5 seconds.
        if (connection is null || starting is { IsCompleted: false })
        {
            await Connect().WaitAsync(cancellationToken);
        }

        return connection!;
    }

    private IDisposable Add(string methodName, Func<Connection, IDisposable> register)
    {
        var handler = new Handler(this, methodName, register);
        handlers.Add(handler);

        if (connection is not null)
        {
            handler.Register(connection);
        }

        return handler;
    }

    // A handler added before the connection is built gets registered on it once it is.
    private sealed class Handler(AppHubConnection owner, string methodName, Func<Connection, IDisposable> register) : IDisposable
    {
        private IDisposable? registration;

        public string MethodName => methodName;

        public void Register(Connection connection) => registration = register(connection);

        public void Dispose()
        {
            owner.handlers.Remove(this);
            registration?.Dispose();
        }
    }

    // Every SignalR type stays in here: one in a field or a signature of AppHubConnection would be loaded before the lazy assemblies are.
    private sealed class Connection(HubConnection hubConnection)
    {
        public static Connection Create(AppHubConnection owner)
        {
            var sp = owner.serviceProvider;
            var authManager = sp.GetRequiredService<AuthManager>();
            var absoluteServerAddressProvider = sp.GetRequiredService<AbsoluteServerAddressProvider>();

            // The X-Origin is already has been set while Client.Web, Client.Maui, Client.Windows and Server.Web
            var xOrigin = sp.GetRequiredService<HttpClient>().DefaultRequestHeaders.Single(h => h.Key is "X-Origin").Value.Single();

            var hubConnection = new HubConnectionBuilder()
                .WithStatefulReconnect()
                .AddJsonProtocol(options =>
                {
                    foreach (var chain in sp.GetRequiredService<JsonSerializerOptions>().TypeInfoResolverChain)
                    {
                        options.PayloadSerializerOptions.TypeInfoResolverChain.Add(chain);
                    }
                })
                .WithAutomaticReconnect(sp.GetRequiredService<IRetryPolicy>())
                .WithAuthenticationRefresh(options =>
                {
                    options.EnableAutoRefresh = true;
                    options.RefreshBeforeExpiration = TimeSpan.FromMinutes(1);
                })
                .WithUrl(new Uri(absoluteServerAddressProvider.GetAddress(), "app-hub?origin=" + Uri.EscapeDataString(xOrigin)), options =>
                {
                    //#if (redis == true)
                    options.SkipNegotiation = true; // No negotiate request, so no sticky sessions behind the Redis backplane.
                    //#else
                    options.SkipNegotiation = false; // Required for Azure SignalR.
                    //#endif
                    options.Transports = HttpTransportType.WebSockets;
                    // Avoid enabling long polling or Server-Sent Events. Focus on resolving the issue with WebSockets instead.
                    // WebSockets should be enabled on services like IIS or Cloudflare CDN, offering significantly better performance.
                    options.HttpMessageHandlerFactory = httpClientHandler => sp.GetRequiredService<HttpMessageHandlersChainFactory>().Invoke(httpClientHandler);
                    options.AccessTokenProvider = async () =>
                    {
                        return await authManager.GetFreshAccessToken(requestedBy: nameof(HubConnection),
                            ignoreTransientException: true); // ignoreTransientException: If the client is disconnected and the access token is expired, this code will execute repeatedly every few seconds, causing an annoying error message to be displayed to the user.
                    };
                })
                .Build();

            hubConnection.Closed += exception => owner.Closed?.Invoke(exception) ?? Task.CompletedTask;
            hubConnection.Reconnecting += exception => owner.Reconnecting?.Invoke(exception) ?? Task.CompletedTask;
            hubConnection.Reconnected += connectionId => owner.Reconnected?.Invoke(connectionId) ?? Task.CompletedTask;

            var connection = new Connection(hubConnection);

            foreach (var handler in owner.handlers)
            {
                handler.Register(connection);
            }

            return connection;
        }

        public bool IsConnected => hubConnection.State is HubConnectionState.Connected;

        public string? ConnectionId => IsConnected ? hubConnection.ConnectionId : null;

        // HubException is in a lazy assembly. Mono never retries a reference it failed to resolve, and inlining this before the load would resolve it.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool IsHubException(Exception exception) => exception is HubException;

        public Task StartAsync(CancellationToken cancellationToken) => hubConnection.StartAsync(cancellationToken);

        public Task<TResult> InvokeAsync<TResult>(string methodName, object?[] args, CancellationToken cancellationToken) => hubConnection.InvokeCoreAsync<TResult>(methodName, args, cancellationToken);

        public IAsyncEnumerable<TResult> StreamAsync<TResult>(string methodName, object?[] args, CancellationToken cancellationToken) => hubConnection.StreamAsyncCore<TResult>(methodName, args, cancellationToken);

        public IDisposable On<T1>(string methodName, Func<T1, Task> handler) => hubConnection.On(methodName, handler);

        public IDisposable On<TResult>(string methodName, Func<Task<TResult>> handler) => hubConnection.On(methodName, handler);

        public IDisposable On<T1, TResult>(string methodName, Func<T1, Task<TResult>> handler) => hubConnection.On(methodName, handler);

        public IDisposable On<T1, T2, TResult>(string methodName, Func<T1, T2, Task<TResult>> handler) => hubConnection.On(methodName, handler);

        public void Remove(string methodName) => hubConnection.Remove(methodName);

        public ValueTask DisposeAsync() => hubConnection.DisposeAsync();
    }
}
