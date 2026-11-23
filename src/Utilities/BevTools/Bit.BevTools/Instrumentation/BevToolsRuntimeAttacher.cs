using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

/// <summary>
/// Turns BevTools on in an app that is already running (<see cref="BitBevTools.Attach"/>, Bit.BevTools.Loader). The
/// session is created outside DI and keyed by the scope's <see cref="IServiceProvider"/>; the first
/// <see cref="BevToolsPanel"/> of that scope attaches it to its renderer.
/// </summary>
internal static class BevToolsRuntimeAttacher
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(30);
    private static readonly ConditionalWeakTable<IServiceProvider, BevToolsSession> Sessions = new();
    private static readonly ConditionalWeakTable<BevToolsLoaderContext, BevToolsRegistry> LoaderRegistries = new();
    private static readonly ConditionalWeakTable<IServiceCollection, Lazy<BevToolsServiceGraph?>> Graphs = new();
    private static readonly ConditionalWeakTable<ILoggerFactory, BevToolsLoggerProvider> LoggerProviders = new();
    private static readonly FieldInfo? HttpHandlerField = typeof(HttpMessageInvoker).GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly object Lock = new();

    /// <summary>The attached sessions that are not disposed yet; each ends with its scope, whatever else happens to it.</summary>
    private static readonly List<WeakReference<BevToolsSession>> Live = [];
    private static Timer? _sweep;

    /// <summary>
    /// The scope's session: registered at startup, or attached since. A registered one that was left off (outside
    /// Development, typically) gives way to one attached since.
    /// </summary>
    public static BevToolsSession? Resolve(IServiceProvider services)
    {
        var registered = services.GetService<BevToolsSession>();
        return registered is { IsEnabled: true } ? registered : Find(services) ?? registered;
    }

    private static BevToolsSession? Find(IServiceProvider services) => Sessions.TryGetValue(services, out var session) && !session.IsDisposed ? session : null;

    public static BevToolsSession Attach(IServiceProvider services, Action<BevToolsOptions>? configure, BevToolsLoaderContext? loader, bool openedByLoader)
    {
        // A session registered at startup and left off does not stand in the way: attaching is a decision of its own,
        // with its own rule (ResolveEnabled), and Bit.BevTools.Loader installed its hooks for exactly that.
        if (services.GetService<BevToolsSession>() is { IsEnabled: true } registered)
        {
            return registered;
        }

        SweepEndedScopes();
        if (Find(services) is { } existing)
        {
            return existing;
        }

        // The app's own code - the configure callback, its HttpClient factory - runs outside the lock, which every scope
        // of the process shares: a slow factory must not hold up every other circuit, nor one that waits on another
        // attach deadlock it.
        var session = Create(services, configure, loader, openedByLoader);

        // Decided before the session is published: another attach of this scope returns it as soon as it is, and its
        // panel may attach it to the renderer before this one goes on, which would find nothing to take over. Nothing is
        // taken here, so a session that loses the race below takes nothing with it.
        var perScope = session.IsEnabled && loader?.AutoStartLogs is { } autoStartLogs && autoStartLogs.KeepsPerScope(services);
        if (perScope)
        {
            session.Attachment!.PendingLogsBeforeAttach = loader;
        }

        lock (Lock)
        {
            if (Find(services) is { } raced)
            {
                session.Dispose();
                return raced;
            }

            Sessions.AddOrUpdate(services, session);
            Live.Add(new WeakReference<BevToolsSession>(session));
            _sweep ??= new Timer(static _ => SweepEndedScopes(), null, SweepInterval, SweepInterval);
        }

        if (session.IsEnabled)
        {
            AuditMultiUserAttach(services, session);
            var attachment = session.Attachment!;
            // Taken right when the session's own provider starts attributing the scope's entries to it: taking them stops
            // the loader keeping more, so none is kept twice, and only an entry another thread logs in the instant
            // between the two is kept by neither. In a process that serves one user that is now (the only session owns
            // everything; WebAssembly has no other thread); per circuit it is once the panel attaches the session to the
            // renderer and it knows the dispatcher, until when the loader keeps them (PendingLogsBeforeAttach, set above;
            // a panel may have taken it over already).
            var handover = perScope ? null : BevToolsAutoStartLogImporter.Take(session, loader);
            attachment.LoggerProviderAdded = AddLoggerProvider(services, session.Registry);
            if (!perScope)
            {
                attachment.LogsBeforeAttach = BevToolsAutoStartLogImporter.Complete(session, handover, out var handedOver);
                attachment.LogsBeforeAttachHandedOver = handedOver;
            }

            if (session.Options.TrackHttp)
            {
                BevToolsHooks.Http = BevToolsHttpRecorder.Instance;
                attachment.HttpClient = AttachHttpClient(services, session.Options, loader);
            }
        }

        return session;
    }

    /// <summary>
    /// Leaves a trace in the app's own logs whenever BevTools is opened outside Development in a process that serves
    /// many users: an inspection of production is an event an operator should be able to find afterwards. The
    /// category is BevTools' own, which its logger provider does not capture, so the line is not shown back to the panel.
    /// </summary>
    private static void AuditMultiUserAttach(IServiceProvider services, BevToolsSession session)
    {
        try
        {
            if (!session.IsInMultiUserHost || services.GetService<Microsoft.Extensions.Hosting.IHostEnvironment>() is not { } environment
                || Microsoft.Extensions.Hosting.HostEnvironmentEnvExtensions.IsDevelopment(environment))
            {
                return;
            }

            services.GetService<ILoggerFactory>()?.CreateLogger("Bit.BevTools").LogWarning(
                "BevTools was attached to a running {Environment} app that serves many users: session {SessionId}, value access {ValueAccess}. {Reason}",
                environment.EnvironmentName, session.Id, session.ValueAccess, session.Registry.EnabledReason);
        }
        catch
        {
            // An audit line that cannot be written must not stop the attach the app asked for.
        }
    }

    private static BevToolsSession Create(IServiceProvider services, Action<BevToolsOptions>? configure, BevToolsLoaderContext? loader, bool openedByLoader)
    {
        // Every scope the loader opens attaches with the same options and the same decision, so they share one registry,
        // as the sessions of a BevTools registered at startup do, instead of each analyzing the service collection again.
        var registry = openedByLoader
            ? LoaderRegistries.GetValue(loader!, context => CreateRegistry(services, configure: null, context, openedByLoader: true))
            : CreateRegistry(services, configure, loader, openedByLoader: false);

        return new BevToolsSession(services, registry.Options, registry)
        {
            Attachment = new BevToolsRuntimeAttachment
            {
                ThroughLoader = loader is not null,
                FactoryClientsHooked = loader is { RegisteredAtStartup: false },
            },
        };
    }

    private static BevToolsRegistry CreateRegistry(IServiceProvider services, Action<BevToolsOptions>? configure, BevToolsLoaderContext? loader, bool openedByLoader)
    {
        var options = new BevToolsOptions();
        if (loader is not null)
        {
            // The app's configuration from the loader, the one its startup registration had too: panels, extensions and
            // every option the loader does not forward. An explicit configure (BitBevTools.Attach) comes after it.
            BevToolsLoaderEntryPoint.Forward(loader.Options, options);
            BevToolsLoaderEntryPoint.Configure(loader.Options, options, skip: 0);
        }

        configure?.Invoke(options);
        var (enabled, reason) = ResolveEnabled(services, options, loader, openedByLoader);
        options.ResolvedEnabled = enabled;

        // The loader kept the service collection from startup, which is what the Services panel is built from.
        var graph = loader is null ? BevToolsRegistry.CreateGraph(null) : Graphs.GetValue(loader.Services, BevToolsRegistry.CreateGraph);
        return new BevToolsRegistry(options, enabled, reason, graph);
    }

    /// <summary>
    /// An explicit <see cref="BevToolsOptions.Enabled"/> decides; otherwise <see cref="BevToolsAttachPolicy"/> does,
    /// the rule <see cref="BevToolsLoader.CanOpen"/> applies too, so a loader never offers an attach it then refuses.
    /// </summary>
    private static (bool Enabled, string Reason) ResolveEnabled(IServiceProvider services, BevToolsOptions options, BevToolsLoaderContext? loader, bool openedByLoader)
    {
        var how = openedByLoader ? "BevToolsLoader.OpenAsync (at startup: " + loader!.StartupDecision + ")" : "BitBevTools.Attach";
        if (options.Enabled is { } explicitValue)
        {
            return (explicitValue, how + " was called with BevToolsOptions.Enabled set to " + explicitValue.ToString().ToLowerInvariant() + ".");
        }

        // A bare attach does not go by the loader's AllowInMultiUserHost: that allowance comes with an Authorize callback,
        // which only BevToolsLoader.OpenAsync runs. The app's own call vouches for itself with BevToolsOptions.Enabled.
        var (allowed, why) = openedByLoader
            ? loader!.ResolveAttach(services)
            : BevToolsAttachPolicy.Resolve(services, loader?.IsMultiUserHost ?? BevToolsSession.HostsAServer(services), allowInMultiUserHost: false, authorizes: false,
                "Set BevToolsOptions.Enabled to attach anyway, or open it through BevToolsLoader.OpenAsync with BevToolsLoaderOptions.AllowInMultiUserHost and Authorize.");

        return (allowed, how + (allowed ? " attached it: " : " left it off: ") + why);
    }

    public static void AttachToRenderer(BevToolsSession session, ComponentBase host)
    {
        if (session.Attachment is not { } attachment || !session.IsEnabled || BevToolsBlazorReflection.ResolveRenderer(host, out _) is not { } renderer || !attachment.TryClaim(renderer))
        {
            return;
        }

        session.Components.NoteRenderer(renderer);
        var pending = attachment.PendingLogsBeforeAttach;
        attachment.PendingLogsBeforeAttach = null;
        var handover = pending is null ? null : BevToolsAutoStartLogImporter.Take(session, pending);
        BevToolsSessionResolver.NoteCurrentContext(session);
        if (pending is not null)
        {
            attachment.LogsBeforeAttach = BevToolsAutoStartLogImporter.Complete(session, handover, out var handedOver);
            attachment.LogsBeforeAttachHandedOver = handedOver;
        }

        // The renderer took its activator from this scope when it was created. When that is the loader's hook, BevTools
        // only has to step into it; otherwise the renderer's activator is replaced through reflection.
        BevToolsComponentActivator? activator = null;
        if (session.Services.GetService<IComponentActivator>() is BevToolsActivatorHook hook)
        {
            hook.Interceptor = activator = new BevToolsComponentActivator(session.Services, session, session.Options, new BevToolsInnerActivator(hook.Untracked));
            attachment.ActivatorAttached = true;
            attachment.ActivatorHooked = true;
        }
        else
        {
            attachment.ActivatorAttached = BevToolsBlazorReflection.TryWrapComponentActivator(renderer, current =>
                current is BevToolsComponentActivator ? null : activator = new BevToolsComponentActivator(session.Services, session, session.Options, new BevToolsInnerActivator(current)));
        }

        var states = BevToolsBlazorReflection.ComponentStates(renderer);
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
    /// A session DI did not create is not disposed by DI. When the renderer is being disposed - a circuit closing - it
    /// ends now, rather than on the next sweep.
    /// </summary>
    public static void OnPanelDisposed(BevToolsSession session)
    {
        if (session.Attachment?.Renderer is { } renderer && BevToolsBlazorReflection.IsRendererDisposed(renderer))
        {
            session.Dispose();
        }
    }

    /// <summary>
    /// Disposes every attached session whose scope has ended. Nothing else is told when that happens: a panel can be
    /// gone long before its circuit closes, or never rendered, and Bit.BevTools.Loader disposes only what it attached.
    /// Until then <see cref="BevToolsSessionResolver"/> would keep the session, and with it the scope, alive for good. Run by
    /// every attach and, while any attached session is left, by a timer.
    /// </summary>
    internal static void SweepEndedScopes()
    {
        List<BevToolsSession>? ended = null;
        lock (Lock)
        {
            Live.RemoveAll(reference =>
            {
                if (!reference.TryGetTarget(out var session) || session.IsDisposed)
                {
                    return true;
                }

                if (!HasEnded(session.Services))
                {
                    return false;
                }

                (ended ??= []).Add(session);
                return true;
            });

            if (Live.Count == 0)
            {
                _sweep?.Dispose();
                _sweep = null;
            }
        }

        foreach (var session in ended ?? [])
        {
            session.Dispose();
        }
    }

    /// <summary>A disposed scope refuses every resolution; a live one resolves itself.</summary>
    private static bool HasEnded(IServiceProvider services)
    {
        try
        {
            _ = services.GetService(typeof(IServiceProvider));
            return false;
        }
        catch (ObjectDisposedException)
        {
            return true;
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

    private static BevToolsHttpClientAttachment AttachHttpClient(IServiceProvider services, BevToolsOptions options, BevToolsLoaderContext? loader)
    {
        if (HttpHandlerField is null)
        {
            return BevToolsHttpClientAttachment.None;
        }

        try
        {
            if (services.GetService<HttpClient>() is not { } client)
            {
                return BevToolsHttpClientAttachment.None;
            }

            if (IsTransient(services, client, loader))
            {
                return BevToolsHttpClientAttachment.Transient;
            }

            lock (client)
            {
                if (HttpHandlerField.GetValue(client) is not HttpMessageHandler handler)
                {
                    return BevToolsHttpClientAttachment.None;
                }

                if (Records(handler))
                {
                    return BevToolsHttpClientAttachment.InPipeline;
                }

                HttpHandlerField.SetValue(client, new BevToolsHttpMessageHandler(options) { InnerHandler = handler });
                return BevToolsHttpClientAttachment.Wrapped;
            }
        }
        catch
        {
            return BevToolsHttpClientAttachment.None;
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

    /// <summary>
    /// Whether the pipeline already records, so wrapping it would record every request twice. A client
    /// <c>IHttpClientFactory</c> built carries the loader's hook from its startup filter, typically below other handlers.
    /// </summary>
    private static bool Records(HttpMessageHandler? handler)
    {
        for (var depth = 0; handler is DelegatingHandler delegating && depth < 64; depth++)
        {
            if (delegating is BevToolsHttpMessageHandler or BevToolsHttpHookHandler or BevToolsHttpHookFilter.RecordHook)
            {
                return true;
            }

            handler = delegating.InnerHandler;
        }

        return false;
    }
}
