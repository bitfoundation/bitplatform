using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;

namespace Bit.BevTools;

/// <summary>
/// Public <see cref="IComponentActivator"/> hook: every component instance the renderer creates passes through here,
/// which is how BevTools learns about components without touching application source.
/// </summary>
internal sealed class BevToolsComponentActivator : IComponentActivator
{
    private static readonly System.Reflection.Assembly BevToolsAssembly = typeof(BevToolsComponentActivator).Assembly;
    private readonly ConcurrentDictionary<Type, (bool Ignore, bool Hidden, bool BevTools)> _typeFlags = new();
    private readonly IServiceProvider _services;
    private readonly BevToolsSession _session;
    private readonly IComponentActivator? _inner;
    private readonly BitBevToolsOptions _options;

    // The holder of the activator registered before BevTools is Bit.BevTools.Loader's, which its hook uses too.
    public BevToolsComponentActivator(IServiceProvider services, BevToolsSession session, BitBevToolsOptions options, BevToolsInnerActivator? inner = null)
    {
        _services = services;
        _session = session;
        _options = options;
        _inner = inner?.Activator;
    }

    public IComponent CreateInstance(Type componentType)
    {
        var instance = _inner is not null ? _inner.CreateInstance(componentType) : BevToolsActivatorHook.CreateDefault(_services, componentType);
        if (!_session.IsEnabled)
        {
            return instance;
        }

        try
        {
            Track(instance, componentType);
        }
        catch (Exception ex)
        {
            _session.Errors.Record(ex, "bevtools", "Component instrumentation failed for " + componentType.Name);
        }

        return instance;
    }

    internal void TrackExisting(IComponent instance)
    {
        if (!_session.IsEnabled || _session.Components.TryGet(instance, out _))
        {
            return;
        }

        try
        {
            Track(instance, instance.GetType(), existing: true);
        }
        catch (Exception ex)
        {
            _session.Errors.Record(ex, "bevtools", "Component instrumentation failed for " + instance.GetType().Name);
        }
    }

    private void Track(IComponent instance, Type componentType, bool existing = false)
    {
        // Component activation is the earliest point that runs on the renderers synchronization context, and
        // parameter-update metrics fire before any component has rendered; map the context here so those
        // measurements can already be attributed to this session.
        SessionResolver.NoteCurrentContext(_session);

        var flags = _typeFlags.GetOrAdd(componentType, ComputeFlags);
        if (flags.Ignore)
        {
            return;
        }

        var record = _session.Components.Register(instance, instance.GetType(), flags.BevTools, flags.Hidden);
        if (record is null)
        {
            return; // tracking budget exhausted; the application keeps working untouched.
        }

        if (existing)
        {
            record.RenderedBeforeAttach = instance is not ComponentBase component || BlazorReflection.HasRenderedOrQueued(component);
        }

        _session.RenderTracker.Instrument(record);
        if (!existing && !flags.BevTools && !flags.Hidden)
        {
            _session.Timeline.Record(new BevToolsEvent
            {
                Kind = BevToolsEventKind.Lifecycle,
                Category = "lifecycle",
                Title = record.DisplayName + " created",
                ComponentInstanceId = record.InstanceId,
                ComponentName = record.DisplayName,
                ParentEventId = ActivityObserver.CurrentTriggerEvent(_session)?.Id,
            });
        }
    }

    private (bool Ignore, bool Hidden, bool BevTools) ComputeFlags(Type type)
    {
        if (type.IsDefined(typeof(BevToolsIgnoreAttribute), true))
        {
            return (true, false, false);
        }

        foreach (var predicate in _options.IgnoreComponentPredicates)
        {
            if (predicate(type))
            {
                return (true, false, false);
            }
        }

        var ns = type.Namespace ?? string.Empty;
        // BevTools' own UI: the core assembly, panel components shipped by the BevTools extension packages, or a panel
        // an extension contributed from its own assembly. Framework components the panels render (Virtualize,
        // CascadingValue, DynamicComponent) are indistinguishable from the application's by type; they inherit
        // ownership from their parent once the hierarchy resolves.
        var isBevTools = type.Assembly == BevToolsAssembly
            || ns == "Bit.BevTools"
            || IsExtensionPanel(type);
        var hidden = false;
        foreach (var prefix in _options.HiddenComponentNamespaces)
        {
            if (ns.StartsWith(prefix, StringComparison.Ordinal))
            {
                hidden = true;
                break;
            }
        }

        if (!hidden)
        {
            foreach (var prefix in _session.Registry.HiddenNamespaces)
            {
                if (ns.StartsWith(prefix, StringComparison.Ordinal))
                {
                    hidden = true;
                    break;
                }
            }
        }

        return (false, hidden, isBevTools);
    }

    private bool IsExtensionPanel(Type type)
    {
        foreach (var panel in _session.Registry.Panels)
        {
            if (panel.ComponentType == type)
            {
                return true;
            }
        }

        return false;
    }
}
