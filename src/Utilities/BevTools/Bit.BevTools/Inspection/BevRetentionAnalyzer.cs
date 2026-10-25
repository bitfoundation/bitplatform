using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools;

/// <summary>
/// Finds what keeps a disposed component alive, in the places a Blazor component typically leaks through: an event or
/// callback list on a singleton or scoped service it injected or a value cascaded to it (subscribed to and never
/// unsubscribed), the JS runtime's table of <see cref="DotNetObjectReference"/>s (created and never disposed), and its
/// own timers (scheduled and never stopped). Only reference identity is checked - no value is read or shown - and every
/// hit is a reference that exists, not a guess; anything held elsewhere (a static, a cache) is not found, and the
/// finding says so.
/// </summary>
internal static class BevRetentionAnalyzer
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    private const int MaxItemsPerCollection = 1000;

    private static readonly ConcurrentDictionary<Type, FieldInfo[]> Fields = new();

    /// <summary>
    /// What one check has read: each holder's references indexed once, however many retained components share it. A
    /// list row's leak is a thousand rows on one service's event; reading that event a thousand times was the cost.
    /// </summary>
    internal sealed class Scan(BevToolsSession session)
    {
        public BevToolsSession Session { get; } = session;

        internal Dictionary<object, Dictionary<object, List<(string Member, string Detail, string Suggestion)>>> Holders { get; } = new(ReferenceEqualityComparer.Instance);

        internal Dictionary<object, List<BevRetentionCause>>? JSReferences { get; set; }

        internal Dictionary<Type, ServiceLifetime>? Lifetimes { get; set; }
    }

    public static IReadOnlyList<BevRetentionCause> Analyze(Scan scan, IComponent component)
    {
        var causes = new List<BevRetentionCause>();
        try
        {
            foreach (var (holder, name, delegatesOnly) in Holders(scan, component))
            {
                if (!scan.Holders.TryGetValue(holder, out var index))
                {
                    scan.Holders[holder] = index = IndexHolder(holder, delegatesOnly);
                }

                if (index.TryGetValue(component, out var found))
                {
                    causes.AddRange(found.Select(f => new BevRetentionCause(name + " → " + f.Member, f.Detail, f.Suggestion)));
                }
            }

            if ((scan.JSReferences ??= IndexJSRuntime(scan.Session)).TryGetValue(component, out var references))
            {
                causes.AddRange(references);
            }

            ScanOwnTimers(component, causes);
        }
        catch (Exception ex)
        {
            // An analysis that fails says so; it never takes the evaluation down with it.
            causes.Add(new BevRetentionCause("BevTools", "the analysis failed: " + ex.GetType().Name, "Take a heap snapshot to find what holds it."));
        }

        return causes.Distinct().ToArray();
    }

    /// <summary>
    /// The objects the component was handed that outlive it, and the scope's own event sources it may have subscribed to.
    /// A transient service is the component's own: it holding the component back is a cycle, not what keeps it alive.
    /// </summary>
    private static List<(object Holder, string Name, bool DelegatesOnly)> Holders(Scan scan, IComponent component)
    {
        var holders = new List<(object, string, bool)>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var (_, cascading, injected) = BevComponentMembers.Of(component.GetType());
        foreach (var property in injected)
        {
            if (Lifetime(scan, property.PropertyType) is not ServiceLifetime.Transient)
            {
                Add(Read(property, component), TypeNames.Short(property.PropertyType) + " (injected)");
            }
        }

        foreach (var property in cascading)
        {
            Add(Read(property, component), property.Name + " (cascading " + TypeNames.Short(property.PropertyType) + ")");
        }

        // Subscribing to these does not need an [Inject] property of the component's own: a base class or a helper
        // can do it with the component as the target.
        foreach (var name in (string[])["Microsoft.AspNetCore.Components.NavigationManager, Microsoft.AspNetCore.Components",
                     "Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, Microsoft.AspNetCore.Components.Authorization"])
        {
            if (Type.GetType(name, throwOnError: false) is { } type)
            {
                using (SessionResolver.BeginBevToolsWork())
                {
                    Add(GetService(scan.Session, type), TypeNames.Short(type));
                }
            }
        }

        return holders;

        void Add(object? value, string name)
        {
            if (value is BevTrackingJSRuntime tracking)
            {
                value = tracking.Inner;
            }

            if (value is null || value is string || value.GetType().IsPrimitive || ReferenceEquals(value, component) || !seen.Add(value) || IsNeverAHolder(value.GetType()))
            {
                return;
            }

            holders.Add((value, name, IsExtensionsService(value.GetType())));
        }
    }

    /// <summary>The lifetime the service is registered with; null when BevTools has no registration to read it from.</summary>
    private static ServiceLifetime? Lifetime(Scan scan, Type serviceType)
    {
        if (scan.Lifetimes is null)
        {
            scan.Lifetimes = [];
            if (scan.Session.Registry.ServiceGraph is { } graph)
            {
                foreach (var service in graph.Services.Where(s => !s.IsKeyed))
                {
                    // The last registration is the one the container resolves.
                    scan.Lifetimes[service.ServiceType] = service.Lifetime;
                }
            }
        }

        return scan.Lifetimes.TryGetValue(serviceType, out var lifetime) ? lifetime
            : serviceType.IsConstructedGenericType && scan.Lifetimes.TryGetValue(serviceType.GetGenericTypeDefinition(), out var open) ? open
            : null;
    }

    private static object? GetService(BevToolsSession session, Type type)
    {
        try
        {
            return session.Services.GetService(type);
        }
        catch
        {
            return null;
        }
    }

    private static object? Read(PropertyInfo property, object target)
    {
        try
        {
            return property.GetValue(target);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Containers and HTTP plumbing: never where a component subscribes, and not BevTools' to walk.</summary>
    private static bool IsNeverAHolder(Type type) =>
        typeof(IServiceProvider).IsAssignableFrom(type)
        || type.Namespace == "System.Net.Http";

    /// <summary>
    /// Options monitors, configuration and the like: a classic leak (<c>IOptionsMonitor.OnChange</c>), but their
    /// collections are configuration, so only their delegates - references, no values - are looked at.
    /// </summary>
    private static bool IsExtensionsService(Type type) => type.Namespace?.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal) ?? false;

    /// <summary>Every component the holder's fields reach - by a handler, a closure, the field itself or a collection - with how.</summary>
    private static Dictionary<object, List<(string Member, string Detail, string Suggestion)>> IndexHolder(object holder, bool delegatesOnly)
    {
        var index = new Dictionary<object, List<(string, string, string)>>(ReferenceEqualityComparer.Instance);
        for (var type = holder.GetType(); type is not null && type != typeof(object); type = type.BaseType)
        {
            foreach (var field in Fields.GetOrAdd(type, static t => t.GetFields(InstanceFields)))
            {
                object? value;
                try
                {
                    value = field.GetValue(holder);
                }
                catch
                {
                    continue;
                }

                var member = MemberName(field);
                switch (value)
                {
                    case null:
                        continue;
                    case Delegate handlers:
                        IndexDelegate(handlers, member, index);
                        break;
                    case IComponent component when !delegatesOnly:
                        Note(index, component, member, "a field that holds the component itself", "Clear the field when the component is disposed, or hold it weakly.");
                        break;
                    case IDictionary dictionary when !delegatesOnly && IsFrameworkCollection(value.GetType()):
                        IndexItems(DictionaryValues(dictionary), member, index);
                        break;
                    case IEnumerable items when !delegatesOnly && IsFrameworkCollection(value.GetType()):
                        IndexItems(items, member, index);
                        break;
                }
            }
        }

        return index;
    }

    /// <summary>A dictionary's values through its enumerator: a concurrent dictionary's Values copies it whole under every lock.</summary>
    private static IEnumerable DictionaryValues(IDictionary dictionary)
    {
        foreach (DictionaryEntry entry in dictionary)
        {
            yield return entry.Value;
        }
    }

    /// <summary>
    /// Arrays and the framework's own collections only: enumerating anything else can run application code (a lazy
    /// query, a database call), which looking must never do.
    /// </summary>
    private static bool IsFrameworkCollection(Type type) =>
        type.IsArray
        || (type.Namespace is "System.Collections.Generic" or "System.Collections.Concurrent" or "System.Collections.Immutable" or "System.Collections.ObjectModel"
            && type.Assembly.GetName().Name is { } assembly && (assembly.StartsWith("System.", StringComparison.Ordinal) || assembly == "mscorlib"));

    private static void IndexItems(IEnumerable items, string member, Dictionary<object, List<(string, string, string)>> index)
    {
        var count = 0;
        try
        {
            foreach (var item in items)
            {
                if (++count > MaxItemsPerCollection)
                {
                    return;
                }

                switch (item)
                {
                    case Delegate handlers:
                        IndexDelegate(handlers, member, index);
                        break;
                    case IComponent component:
                        Note(index, component, member, "a collection that still contains the component", "Remove the component from it in Dispose.");
                        break;
                }
            }
        }
        catch
        {
            // A collection modified while it is read, or one that throws when enumerated: nothing to report from it.
        }
    }

    private static void IndexDelegate(Delegate handlers, string member, Dictionary<object, List<(string, string, string)>> index)
    {
        var count = 0;
        foreach (var handler in handlers.GetInvocationList())
        {
            if (++count > MaxItemsPerCollection)
            {
                return;
            }

            if (handler.Target is IComponent component)
            {
                Note(index, component, member, "handler " + BevEventCallbacks.Method(handler), "Unsubscribe (-=) in Dispose: implement IDisposable and remove the handler there.");
            }
            else if (handler.Target is { } target && target.GetType().IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                foreach (var captured in Captured(target))
                {
                    Note(index, captured, member, BevEventCallbacks.Method(handler) + ", which captures the component",
                        "Keep the handler in a field and unsubscribe it in Dispose: a lambda cannot be removed with -= unless it is the same instance.");
                }
            }
            else if (handler.Target is { } wrapper)
            {
                IndexWrapped(wrapper, member, index);
            }
        }
    }

    /// <summary>
    /// A subscription object that holds the listener it was given (<c>IOptionsMonitor.OnChange</c> keeps a
    /// <c>ChangeTrackerDisposable</c> whose delegate is the component's handler): one level of its delegate fields.
    /// </summary>
    private static void IndexWrapped(object wrapper, string member, Dictionary<object, List<(string, string, string)>> index)
    {
        var through = TypeNames.Short(wrapper.GetType());
        for (var type = wrapper.GetType(); type is not null && type != typeof(object); type = type.BaseType)
        {
            foreach (var field in Fields.GetOrAdd(type, static t => t.GetFields(InstanceFields)))
            {
                if (!typeof(Delegate).IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                Delegate? inner;
                try
                {
                    inner = field.GetValue(wrapper) as Delegate;
                }
                catch
                {
                    continue;
                }

                foreach (var handler in inner?.GetInvocationList().Take(MaxItemsPerCollection) ?? [])
                {
                    const string suggestion = "Dispose the subscription in Dispose (the IDisposable the subscribe call returned), or unsubscribe there.";
                    if (handler.Target is IComponent component)
                    {
                        Note(index, component, member, "handler " + BevEventCallbacks.Method(handler) + ", through " + through, suggestion);
                    }
                    else if (handler.Target is { } closure)
                    {
                        foreach (var captured in Captured(closure))
                        {
                            Note(index, captured, member, BevEventCallbacks.Method(handler) + ", which captures the component, through " + through, suggestion);
                        }
                    }
                }
            }
        }
    }

    private static void Note(Dictionary<object, List<(string, string, string)>> index, object component, string member, string detail, string suggestion)
    {
        if (!index.TryGetValue(component, out var notes))
        {
            index[component] = notes = [];
        }

        if (notes.Count < 10)
        {
            notes.Add((member, detail, suggestion));
        }
    }

    /// <summary>The components a compiler-generated closure holds, directly or through the closure it nests in.</summary>
    private static List<IComponent> Captured(object closure)
    {
        var found = new List<IComponent>();
        Walk(closure, 0);
        return found;

        void Walk(object value, int depth)
        {
            var type = value.GetType();
            if (depth > 2 || !type.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                return;
            }

            foreach (var field in Fields.GetOrAdd(type, static t => t.GetFields(InstanceFields)))
            {
                object? captured;
                try
                {
                    captured = field.GetValue(value);
                }
                catch
                {
                    continue;
                }

                if (captured is IComponent component)
                {
                    found.Add(component);
                }
                else if (captured is not null)
                {
                    Walk(captured, depth + 1);
                }
            }
        }
    }

    private static readonly Lazy<FieldInfo?> TrackedReferences = new(() => typeof(JSRuntime).GetField("_trackedRefsById", BindingFlags.Instance | BindingFlags.NonPublic));

    /// <summary>
    /// A DotNetObjectReference passed to JS is kept in the runtime's table until it is disposed - for a circuit, until
    /// the circuit ends - and with it whatever it wraps. Indexed once per check by the component each one reaches.
    /// </summary>
    private static Dictionary<object, List<BevRetentionCause>> IndexJSRuntime(BevToolsSession session)
    {
        var index = new Dictionary<object, List<BevRetentionCause>>(ReferenceEqualityComparer.Instance);
        object? runtime;
        using (SessionResolver.BeginBevToolsWork())
        {
            runtime = GetService(session, typeof(IJSRuntime));
        }

        if (runtime is BevTrackingJSRuntime tracking)
        {
            runtime = tracking.Inner;
        }

        if (runtime is not JSRuntime || TrackedReferences.Value?.GetValue(runtime) is not IDictionary table)
        {
            return index;
        }

        var count = 0;
        foreach (var reference in DictionaryValues(table))
        {
            if (++count > 10 * MaxItemsPerCollection)
            {
                break;
            }

            // DotNetObjectReference<T>.Value: what the reference keeps alive.
            var target = reference?.GetType().GetProperty("Value")?.GetValue(reference);
            var reached = target is IComponent component ? [component] : target is null ? [] : Captured(target);
            foreach (var held in reached)
            {
                if (!index.ContainsKey(held))
                {
                    index[held] = [new BevRetentionCause("the JS runtime", "a " + TypeNames.Short(reference!.GetType()) + " passed to JavaScript and never disposed",
                        "Dispose the DotNetObjectReference in Dispose/DisposeAsync (and tell the JS side to drop it).")];
                }
            }
        }

        return index;
    }

    /// <summary>
    /// What a <see cref="System.Threading.Timer"/> will still run: its callback and state while it is scheduled, null when
    /// it is disposed, will not fire again (a one-shot that fired) or cannot be read. Optional reflection.
    /// </summary>
    private static readonly Lazy<Func<System.Threading.Timer, (Delegate? Callback, object? State)?>> ScheduledTimer = new(() =>
    {
        // Timer → TimerHolder (_timer) → TimerQueueTimer (_timer): _canceled, _dueTime (uint.MaxValue when not
        // scheduled), _timerCallback, _state.
        var holder = typeof(System.Threading.Timer).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic);
        var queued = holder?.FieldType.GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic);
        var queueType = queued?.FieldType;
        var canceled = queueType?.GetField("_canceled", BindingFlags.Instance | BindingFlags.NonPublic);
        var dueTime = queueType?.GetField("_dueTime", BindingFlags.Instance | BindingFlags.NonPublic);
        var callback = queueType?.GetField("_timerCallback", BindingFlags.Instance | BindingFlags.NonPublic);
        var state = queueType?.GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic);
        if (holder is null || queued is null || canceled?.FieldType != typeof(bool) || dueTime?.FieldType != typeof(uint) || callback is null || state is null)
        {
            return static _ => null;
        }

        return timer =>
        {
            try
            {
                if (holder.GetValue(timer) is not { } h || queued.GetValue(h) is not { } q || (bool)canceled.GetValue(q)! || (uint)dueTime.GetValue(q)! == uint.MaxValue)
                {
                    return null;
                }

                return (callback.GetValue(q) as Delegate, state.GetValue(q));
            }
            catch
            {
                return null;
            }
        };
    });

    private static readonly Lazy<FieldInfo?> ElapsedHandlers = new(() => typeof(System.Timers.Timer).GetField("_onIntervalElapsed", BindingFlags.Instance | BindingFlags.NonPublic));

    /// <summary>
    /// A timer the component started and never stopped roots its callback, and the callback the component: only a timer
    /// that will still fire, with a callback or state that reaches the component, is a cause.
    /// </summary>
    private static void ScanOwnTimers(IComponent component, List<BevRetentionCause> causes)
    {
        for (var type = component.GetType(); type is not null && type != typeof(ComponentBase) && type != typeof(object); type = type.BaseType)
        {
            foreach (var field in Fields.GetOrAdd(type, static t => t.GetFields(InstanceFields)))
            {
                object? value;
                try
                {
                    value = field.GetValue(component);
                }
                catch
                {
                    continue;
                }

                var reaches = value switch
                {
                    System.Timers.Timer timer => timer.Enabled && ElapsedHandlers.Value?.GetValue(timer) is Delegate elapsed && Reaches(elapsed, null, component),
                    System.Threading.Timer timer => ScheduledTimer.Value(timer) is { } scheduled && Reaches(scheduled.Callback, scheduled.State, component),
                    _ => false,
                };
                if (reaches)
                {
                    causes.Add(new BevRetentionCause("its own field " + MemberName(field), TypeNames.Short(value!.GetType()) + " still scheduled, with a callback that reaches the component",
                        "Stop and dispose the timer in Dispose."));
                }
            }
        }
    }

    private static bool Reaches(Delegate? callbacks, object? state, IComponent component)
    {
        if (ReferenceEquals(state, component) || (state is not null && Captured(state).Contains(component)))
        {
            return true;
        }

        if (callbacks is null)
        {
            return false;
        }

        foreach (var callback in callbacks.GetInvocationList())
        {
            if (ReferenceEquals(callback.Target, component) || (callback.Target is { } closure && Captured(closure).Contains(component)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The components the renderer itself still references from its frame buffers: a component its parent stopped
    /// rendering is kept in the parent's previous render tree until the parent renders again, and in buffers reused
    /// without clearing. Held only there, a disposed component is not leaked: it goes with the parent's next render.
    /// </summary>
    public static HashSet<IComponent> HeldByRenderer(BevToolsSession session)
    {
        var held = new HashSet<IComponent>(ReferenceEqualityComparer.Instance);
        if (session.Components.Renderer is not { } renderer || BlazorReflection.ComponentStates(renderer) is not { } states)
        {
            return held;
        }

        foreach (var state in states)
        {
            foreach (var frames in BlazorReflection.FrameBuffers(state))
            {
                Collect(frames);
            }
        }

        if (BlazorReflection.BatchReferenceFrames(renderer) is { } batch)
        {
            Collect(batch);
        }

        return held;

        void Collect(RenderTreeFrame[] frames)
        {
            for (var i = 0; i < frames.Length; i++)
            {
                if (frames[i].FrameType == RenderTreeFrameType.Component && frames[i].Component is { } component)
                {
                    held.Add(component);
                }
            }
        }
    }

    /// <summary>"Changed" for an event's backing field, "_subscribers" for an ordinary one; never "&lt;Name&gt;k__BackingField".</summary>
    private static string MemberName(FieldInfo field)
    {
        var name = field.Name;
        if (name.StartsWith('<') && name.IndexOf('>') is > 1 and var close)
        {
            return name[1..close];
        }

        return name;
    }
}
