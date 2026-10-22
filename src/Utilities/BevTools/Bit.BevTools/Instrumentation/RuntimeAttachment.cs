using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

/// <summary>How the requests of the <c>HttpClient</c> registered in DI reach an attached session.</summary>
internal enum BevHttpClientAttachment
{
    /// <summary>No <c>HttpClient</c> is registered in DI, or its handler could not be reached.</summary>
    None,

    /// <summary>Its pipeline already holds a BevTools handler, Bit.BevTools.Loader's <c>BevToolsHttpHookHandler</c> typically.</summary>
    InPipeline,

    /// <summary>BevTools put a <see cref="BevToolsHttpMessageHandler"/> in front of its handler through reflection.</summary>
    Wrapped,

    /// <summary>It is transient: wrapping one instance would leave every other one unrecorded, so none is.</summary>
    Transient,
}

/// <summary>What a session attached to a running app could and could not hook into. The capability report reads it.</summary>
internal sealed class BevRuntimeAttachment
{
    private WeakReference<Renderer>? _renderer;

    /// <summary>Attached through Bit.BevTools.Loader, whose startup hooks stand in for most of the reflection a bare attach needs.</summary>
    public bool ThroughLoader { get; init; }

    /// <summary>The loader's pass-through handlers sit in every <c>IHttpClientFactory</c> pipeline.</summary>
    public bool FactoryClientsHooked { get; init; }

    public bool LoggerProviderAdded { get; set; }

    public BevHttpClientAttachment HttpClient { get; set; }

    /// <summary>Null until a panel attaches to its renderer; then whether the components created after are seen.</summary>
    public bool? ActivatorAttached { get; set; }

    /// <summary>True when the loader's activator hook carries them, false when the renderer's activator was replaced through reflection.</summary>
    public bool ActivatorHooked { get; set; }

    public bool? ExistingComponentsRead { get; set; }

    public Renderer? Renderer => _renderer is not null && _renderer.TryGetTarget(out var renderer) ? renderer : null;

    public bool TryClaim(Renderer renderer)
    {
        if (ReferenceEquals(Renderer, renderer))
        {
            return false;
        }

        _renderer = new WeakReference<Renderer>(renderer);
        return true;
    }
}

/// <summary>
/// Turns BevTools on in an app that is already running (<see cref="BitBevTools.Attach"/>, Bit.BevTools.Loader). The
/// session is created outside DI and keyed by the scope's <see cref="IServiceProvider"/>; the first
/// <see cref="BevToolsPanel"/> of that scope attaches it to its renderer.
/// </summary>
internal static class RuntimeAttachment
{
    private static readonly ConditionalWeakTable<IServiceProvider, BevToolsSession> Sessions = new();
    private static readonly ConditionalWeakTable<ILoggerFactory, BevToolsLoggerProvider> LoggerProviders = new();
    private static readonly FieldInfo? HttpHandlerField = typeof(HttpMessageInvoker).GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly object Lock = new();

    /// <summary>The scope's session: registered at startup, or attached since.</summary>
    public static BevToolsSession? Resolve(IServiceProvider services) => services.GetService<BevToolsSession>() ?? Find(services);

    private static BevToolsSession? Find(IServiceProvider services) => Sessions.TryGetValue(services, out var session) && !session.IsDisposed ? session : null;

    public static BevToolsSession Attach(IServiceProvider services, Action<BitBevToolsOptions>? configure, BevToolsLoaderContext? loader)
    {
        if (services.GetService<BevToolsSession>() is { } registered)
        {
            return registered;
        }

        lock (Lock)
        {
            if (Find(services) is { } existing)
            {
                return existing;
            }

            var options = new BitBevToolsOptions();
            if (loader is not null)
            {
                BevToolsLoaderEntryPoint.Forward(loader.Options, options);
            }

            configure?.Invoke(options);
            var (enabled, reason) = ResolveEnabled(services, options, loader);
            options.ResolvedEnabled = enabled;

            // The loader kept the service collection from startup, which is what the Services panel is built from.
            var registry = new BevToolsRegistry(options, enabled, reason, loader?.Services);
            var attachment = new BevRuntimeAttachment
            {
                ThroughLoader = loader is not null,
                FactoryClientsHooked = loader is { RegisteredAtStartup: false },
            };
            var session = new BevToolsSession(services, options, registry) { Attachment = attachment };
            if (enabled)
            {
                attachment.LoggerProviderAdded = AddLoggerProvider(services, registry);
                if (options.TrackHttp)
                {
                    BevToolsHooks.Http = BevToolsHttpRecorder.Instance;
                    attachment.HttpClient = AttachHttpClient(services, options, loader);
                }
            }

            Sessions.AddOrUpdate(services, session);
            return session;
        }
    }

    /// <summary>
    /// Attaching is the app's decision, so it is enabled, except where that would expose other people's data: in a
    /// process that serves many users the panel shows whoever opens it the server side objects of their circuit,
    /// injected services and <c>IConfiguration</c> included. There it takes Development, an explicit
    /// <see cref="BitBevToolsOptions.Enabled"/>, or the loader's <c>AllowInMultiUserHost</c>.
    /// </summary>
    private static (bool Enabled, string Reason) ResolveEnabled(IServiceProvider services, BitBevToolsOptions options, BevToolsLoaderContext? loader)
    {
        var how = loader is null ? "BitBevTools.Attach" : "BevToolsLoader.OpenAsync (at startup: " + loader.StartupDecision + ")";
        if (options.Enabled is { } explicitValue)
        {
            return (explicitValue, how + " was called with BitBevToolsOptions.Enabled set to " + explicitValue.ToString().ToLowerInvariant() + ".");
        }

        if (!BevToolsSession.HostsAServer(services))
        {
            return (true, how + " attached it to the running app.");
        }

        if (loader?.Options.AllowInMultiUserHost == true)
        {
            return (true, how + " attached it to this server process, which BitBevToolsLoaderOptions.AllowInMultiUserHost allows.");
        }

        var environment = services.GetService<IHostEnvironment>();
        return environment?.IsDevelopment() == true
            ? (true, how + $" attached it to this server process, whose IHostEnvironment reports '{environment.EnvironmentName}'.")
            : (false, how + $" left it off: this process serves many users (IHostEnvironment reports '{environment?.EnvironmentName ?? "nothing"}'), and the panel would show whoever opens it the server side objects of their circuit. Set BitBevToolsOptions.Enabled to attach anyway.");
    }

    public static void AttachToRenderer(BevToolsSession session, ComponentBase host)
    {
        if (session.Attachment is not { } attachment || !session.IsEnabled || BlazorReflection.ResolveRenderer(host, out _) is not { } renderer || !attachment.TryClaim(renderer))
        {
            return;
        }

        session.Components.NoteRenderer(renderer);
        SessionResolver.NoteCurrentContext(session);

        // The renderer took its activator from this scope when it was created. When that is the loader's hook, BevTools
        // only has to step into it; otherwise the renderer's activator is replaced through reflection.
        BevToolsComponentActivator? activator = null;
        if (session.Services.GetService<IComponentActivator>() is BevToolsActivatorHook hook)
        {
            hook.Interceptor = activator = new BevToolsComponentActivator(session.Services, session, session.Options, new InnerComponentActivator(hook.Untracked));
            attachment.ActivatorAttached = true;
            attachment.ActivatorHooked = true;
        }
        else
        {
            attachment.ActivatorAttached = BlazorReflection.TryWrapComponentActivator(renderer, current =>
                current is BevToolsComponentActivator ? null : activator = new BevToolsComponentActivator(session.Services, session, session.Options, new InnerComponentActivator(current)));
        }

        var states = BlazorReflection.ComponentStates(renderer);
        attachment.ExistingComponentsRead = states is not null;
        if (states is not null)
        {
            activator ??= new BevToolsComponentActivator(session.Services, session, session.Options);
            foreach (var state in states)
            {
                activator.TrackExisting(state.Component);
            }
        }

        session.Touch();
    }

    /// <summary>
    /// A session DI did not create is not disposed by DI. Bit.BevTools.Loader disposes it with the scope; after a bare
    /// <see cref="BitBevTools.Attach"/> it ends with the renderer, which disposes its components, this panel included,
    /// after it marked itself disposed.
    /// </summary>
    public static void OnPanelDisposed(BevToolsSession session)
    {
        if (session.Attachment?.Renderer is { } renderer && BlazorReflection.IsRendererDisposed(renderer))
        {
            session.Dispose();
        }
    }

    private static bool AddLoggerProvider(IServiceProvider services, BevToolsRegistry registry)
    {
        try
        {
            if (services.GetService<ILoggerFactory>() is not { } factory)
            {
                return false;
            }

            // One per factory, which is a singleton: the provider attributes each log entry to the session of the
            // context it was written in, so it serves every session attached in this process.
            lock (LoggerProviders)
            {
                if (!LoggerProviders.TryGetValue(factory, out _))
                {
                    var provider = new BevToolsLoggerProvider(registry);
                    factory.AddProvider(provider);
                    LoggerProviders.Add(factory, provider);
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static BevHttpClientAttachment AttachHttpClient(IServiceProvider services, BitBevToolsOptions options, BevToolsLoaderContext? loader)
    {
        if (HttpHandlerField is null)
        {
            return BevHttpClientAttachment.None;
        }

        try
        {
            if (services.GetService<HttpClient>() is not { } client)
            {
                return BevHttpClientAttachment.None;
            }

            if (IsTransient(services, client, loader))
            {
                return BevHttpClientAttachment.Transient;
            }

            lock (client)
            {
                if (HttpHandlerField.GetValue(client) is not HttpMessageHandler handler)
                {
                    return BevHttpClientAttachment.None;
                }

                if (Records(handler))
                {
                    return BevHttpClientAttachment.InPipeline;
                }

                HttpHandlerField.SetValue(client, new BevToolsHttpMessageHandler(options) { InnerHandler = handler });
                return BevHttpClientAttachment.Wrapped;
            }
        }
        catch
        {
            return BevHttpClientAttachment.None;
        }
    }

    private static bool IsTransient(IServiceProvider services, HttpClient client, BevToolsLoaderContext? loader)
    {
        if (loader?.Services.LastOrDefault(d => d.ServiceType == typeof(HttpClient) && !d.IsKeyedService) is { } descriptor)
        {
            return descriptor.Lifetime == ServiceLifetime.Transient;
        }

        // Without the service collection, the container is asked twice; a transient client is the one that differs.
        return !ReferenceEquals(client, services.GetService<HttpClient>());
    }

    private static bool Records(HttpMessageHandler? handler)
    {
        for (var depth = 0; handler is DelegatingHandler delegating && depth < 64; depth++)
        {
            if (delegating is BevToolsHttpMessageHandler or BevToolsHttpHookHandler)
            {
                return true;
            }

            handler = delegating.InnerHandler;
        }

        return false;
    }
}
