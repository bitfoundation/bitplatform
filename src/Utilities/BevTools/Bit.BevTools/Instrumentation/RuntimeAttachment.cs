using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

internal sealed class BevRuntimeAttachment
{
    private WeakReference<Renderer>? _renderer;

    public bool LoggerProviderAdded { get; set; }

    public bool HttpClientWrapped { get; set; }

    public bool? ActivatorWrapped { get; set; }

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

internal static class RuntimeAttachment
{
    private static readonly ConditionalWeakTable<IServiceProvider, BevToolsSession> Sessions = new();
    private static readonly ConditionalWeakTable<ILoggerFactory, BevToolsLoggerProvider> LoggerProviders = new();
    private static readonly FieldInfo? HttpHandlerField = typeof(HttpMessageInvoker).GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly object Lock = new();

    public static BevToolsSession? Resolve(IServiceProvider services) => services.GetService<BevToolsSession>() ?? Find(services);

    private static BevToolsSession? Find(IServiceProvider services) => Sessions.TryGetValue(services, out var session) && !session.IsDisposed ? session : null;

    public static BevToolsSession Attach(IServiceProvider services, Action<BitBevToolsOptions>? configure)
    {
        lock (Lock)
        {
            if (Find(services) is { } existing)
            {
                return existing;
            }

            var options = new BitBevToolsOptions();
            configure?.Invoke(options);
            var enabled = options.Enabled ?? true;
            options.ResolvedEnabled = enabled;
            var reason = enabled
                ? "BitBevTools.Attach attached it to the running app."
                : "BitBevTools.Attach was called with BitBevToolsOptions.Enabled set to false.";
            var registry = new BevToolsRegistry(options, enabled, reason, services: null);
            var attachment = new BevRuntimeAttachment();
            var session = new BevToolsSession(services, options, registry) { Attachment = attachment };
            if (enabled)
            {
                attachment.LoggerProviderAdded = AddLoggerProvider(services, registry);
                attachment.HttpClientWrapped = options.TrackHttp && WrapHttpClient(services, options);
            }

            Sessions.AddOrUpdate(services, session);
            return session;
        }
    }

    public static void AttachToRenderer(BevToolsSession session, ComponentBase host)
    {
        if (session.Attachment is not { } attachment || !session.IsEnabled || BlazorReflection.ResolveRenderer(host, out _) is not { } renderer || !attachment.TryClaim(renderer))
        {
            return;
        }

        session.Components.NoteRenderer(renderer);
        SessionResolver.NoteCurrentContext(session);

        BevToolsComponentActivator? activator = null;
        attachment.ActivatorWrapped = BlazorReflection.TryWrapComponentActivator(renderer, current =>
            current is BevToolsComponentActivator ? null : activator = new BevToolsComponentActivator(session.Services, session, session.Options, new InnerComponentActivator(current)));

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

            if (!LoggerProviders.TryGetValue(factory, out _))
            {
                var provider = new BevToolsLoggerProvider(registry);
                factory.AddProvider(provider);
                LoggerProviders.Add(factory, provider);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool WrapHttpClient(IServiceProvider services, BitBevToolsOptions options)
    {
        if (HttpHandlerField is null)
        {
            return false;
        }

        try
        {
            if (services.GetService<HttpClient>() is not { } client)
            {
                return false;
            }

            lock (client)
            {
                if (HttpHandlerField.GetValue(client) is not HttpMessageHandler handler)
                {
                    return false;
                }

                if (handler is not BevToolsHttpMessageHandler)
                {
                    HttpHandlerField.SetValue(client, new BevToolsHttpMessageHandler(options) { InnerHandler = handler });
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
