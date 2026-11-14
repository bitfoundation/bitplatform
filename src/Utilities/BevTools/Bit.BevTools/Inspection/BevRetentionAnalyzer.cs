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
/// unsubscribed), the objects those hold (a list of steps, each with its callback), a field of a component that is
/// still on the page (an <c>@ref</c> to one it stopped rendering), the JS runtime's table of
/// <see cref="DotNetObjectReference"/>s and what their targets hold (created and never disposed), and its own timers
/// (scheduled and never stopped). Only reference identity is checked - no value is read or shown - and every hit is a
/// reference that exists, not a guess. The search is bounded (<see cref="MaxDepth"/> objects below each holder, and
/// only into application and library types, never the framework's): anything held elsewhere (a static, a cache, a
/// longer path) is not found, and the finding says what was searched.
/// </summary>
internal static class BevRetentionAnalyzer
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    private const int MaxItemsPerCollection = 1000;

    /// <summary>
    /// How many objects and collections below a holder the search follows: a service → its object → that object's list
    /// → an item, whose callback is then read.
    /// </summary>
    internal const int MaxDepth = 4;

    /// <summary>The objects one holder's search may read, whatever their depth: a service that holds a large graph is read in part.</summary>
    private const int MaxObjectsPerHolder = 20_000;

    private static readonly ConcurrentDictionary<Type, FieldInfo[]> Fields = new();

    /// <summary>What a finding says when no reference was found: what was searched, which is not everywhere.</summary>
    internal static readonly string NotFound = "Not held by its injected services, its cascading values, a field of a component still on the page, the JS runtime's object references or its own timers"
        + " (followed up to " + MaxDepth + " objects deep into the application's and its libraries' own types). What holds it is outside that: a static field, a cache, a framework object, or a longer path.";

    /// <summary>How to find what BevTools did not.</summary>
    internal const string HeapSnapshot = "Take a heap snapshot and follow the path from it to a root: dotnet-gcdump for a server process, the runtime's GC dump (collectGcDump, in a build with diagnostics) on WebAssembly.";

    /// <summary>"BitText 200, BitIcon 111, BitButton 80 and 12 more types".</summary>
    internal static string Describe(IReadOnlyList<BevRetainedTypeCount> types, int shown = 5) =>
        string.Join(", ", types.Take(shown).Select(t => t.DisplayName + " " + t.Count))
        + (types.Count > shown ? " and " + (types.Count - shown) + " more type" + (types.Count - shown == 1 ? "" : "s") : "");

    /// <summary>
    /// What one check has read: each holder's references indexed once, however many retained components share it. A
    /// list row's leak is a thousand rows on one service's event; reading that event a thousand times was the cost.
    /// </summary>
    internal sealed class Scan(BevToolsSession session, IEnumerable<IComponent>? disposed = null)
    {
        public BevToolsSession Session { get; } = session;

        /// <summary>
        /// The disposed components still alive. One of them holding another is not what keeps either: both go when
        /// whatever holds the first lets go (a disposed carousel's list of its disposed items).
        /// </summary>
        internal HashSet<IComponent> Disposed { get; } = new(disposed ?? [], ReferenceEqualityComparer.Instance);

        internal Dictionary<object, Dictionary<object, List<(string Member, string Detail, string Suggestion)>>> Holders { get; } = new(ReferenceEqualityComparer.Instance);

        internal Dictionary<object, List<BevRetentionCause>>? JSReferences { get; set; }

        internal Dictionary<object, List<BevRetentionCause>>? LiveComponents { get; set; }

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

            if ((scan.LiveComponents ??= IndexLiveComponents(scan)).TryGetValue(component, out var fields))
            {
                causes.AddRange(fields);
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
    /// So is a cascaded component that is disposed itself.
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

            if (value is null || value is string || value.GetType().IsPrimitive || ReferenceEquals(value, component) || !seen.Add(value) || IsNeverAHolder(value.GetType())
                || (value is IComponent holder && scan.Disposed.Contains(holder)))
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

    /// <summary>
    /// Whether the search follows a reference into an object of this type below a holder: the application's and its
    /// libraries' own types, where a component's callback is kept (a tour's steps, an animation engine's listeners).
    /// The framework's types are not followed - their internals are not where an application leaks, and a renderer or a
    /// container reaches everything - nor BevTools' own, which never holds an application component strongly.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, bool> Walkable = new();

    private static bool IsWalkable(Type type) => Walkable.GetOrAdd(type, static t =>
    {
        if (t.IsPrimitive || t.IsEnum || t.IsPointer || t == typeof(string) || typeof(Delegate).IsAssignableFrom(t) || typeof(IComponent).IsAssignableFrom(t)
            || typeof(MemberInfo).IsAssignableFrom(t) || IsNeverAHolder(t))
        {
            return false;
        }

        if (t.IsDefined(typeof(CompilerGeneratedAttribute), false))
        {
            return true;
        }

        // BevTools' own types all live directly in its namespace (exactly: an application under "Bit.BevTools.Demo" is
        // an application).
        var assembly = t.Assembly.GetName().Name ?? "";
        return !(assembly.StartsWith("System.", StringComparison.Ordinal) || assembly.StartsWith("Microsoft.", StringComparison.Ordinal)
            || assembly is "System" or "mscorlib" or "netstandard"
            || t.Namespace == "Bit.BevTools");
    });

    /// <summary>
    /// Every component the holder reaches - by a handler, a closure, a field, a collection, or an object of the
    /// application's that holds one of those, up to <see cref="MaxDepth"/> objects down - with the path.
    /// </summary>
    private static Dictionary<object, List<(string Member, string Detail, string Suggestion)>> IndexHolder(object holder, bool delegatesOnly)
    {
        var walker = new Walker(delegatesOnly);
        walker.Walk(holder, "", 0);
        return walker.Index;
    }

    /// <summary>A bounded walk of an object graph by reference, from one holder, that notes the components it reaches.</summary>
    private sealed class Walker(bool delegatesOnly, int budget = MaxObjectsPerHolder)
    {
        private readonly HashSet<object> _visited = new(ReferenceEqualityComparer.Instance);
        private int _budget = budget;

        public Dictionary<object, List<(string Member, string Detail, string Suggestion)>> Index { get; } = new(ReferenceEqualityComparer.Instance);

        /// <summary>What is left of the budget it was given.</summary>
        public int Remaining => Math.Max(0, _budget);

        /// <summary>The fields of <paramref name="value"/>, which is <paramref name="depth"/> objects below the holder (the holder is 0).</summary>
        public void Walk(object value, string path, int depth)
        {
            if (--_budget < 0 || !_visited.Add(value))
            {
                return;
            }

            for (var type = value.GetType(); type is not null && type != typeof(object); type = type.BaseType)
            {
                foreach (var field in Fields.GetOrAdd(type, static t => t.GetFields(InstanceFields)))
                {
                    var fieldType = field.FieldType;
                    if (fieldType.IsPrimitive || fieldType.IsEnum || fieldType.IsPointer || fieldType == typeof(string) || fieldType.IsByRefLike)
                    {
                        continue;
                    }

                    object? member;
                    try
                    {
                        member = field.GetValue(value);
                    }
                    catch
                    {
                        continue;
                    }

                    Value(member, path.Length == 0 ? MemberName(field) : path + "." + MemberName(field), depth + 1, inCollection: false);
                }
            }
        }

        private void Value(object? value, string path, int depth, bool inCollection)
        {
            switch (value)
            {
                case null:
                    return;
                case Delegate handlers:
                    IndexDelegate(handlers, path, Index);
                    return;
                case IComponent component when !delegatesOnly:
                    Note(Index, component, path, inCollection ? "a collection that still contains the component" : "a field that holds the component itself",
                        inCollection ? "Remove the component from it in Dispose." : "Clear the field when the component is disposed, or hold it weakly.");
                    return;
                case IComponent:
                    return;
            }

            var type = value.GetType();
            if (type == typeof(EventCallback) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>)))
            {
                if (BlazorReflection.EventCallbackDelegate(value) is { } callback)
                {
                    IndexDelegate(callback, path, Index);
                }

                return;
            }

            // A collection counts as a level, and is visited once, like an object: one that contains itself, or a chain
            // of nested ones, ends at the depth.
            if (delegatesOnly || depth > MaxDepth)
            {
                return;
            }

            if (value is IDictionary dictionary && IsFrameworkCollection(type))
            {
                if (_visited.Add(value))
                {
                    Items(DictionaryValues(dictionary), path + "[]", depth);
                }
            }
            else if (value is IEnumerable items && IsFrameworkCollection(type))
            {
                if (_visited.Add(value))
                {
                    Items(items, path + "[]", depth);
                }
            }
            else if (IsWalkable(type))
            {
                Walk(value, path, depth);
            }
        }

        private void Items(IEnumerable items, string path, int depth)
        {
            var count = 0;
            try
            {
                foreach (var item in items)
                {
                    if (++count > MaxItemsPerCollection || --_budget < 0)
                    {
                        return;
                    }

                    Value(item, path, depth + 1, inCollection: true);
                }
            }
            catch
            {
                // A collection modified while it is read, or one that throws when enumerated: nothing to report from it.
            }
        }
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

    /// <summary>
    /// The components still on the page that reference a removed one from a field of their own: an <c>@ref</c> (or a
    /// list of them) to a child the markup no longer renders keeps that child for as long as the page lives. Only the
    /// component's own declared fields are read, directly or as a framework collection's items.
    /// </summary>
    private static Dictionary<object, List<BevRetentionCause>> IndexLiveComponents(Scan scan)
    {
        var index = new Dictionary<object, List<BevRetentionCause>>(ReferenceEqualityComparer.Instance);
        if (scan.Disposed.Count == 0)
        {
            return index;
        }

        foreach (var record in scan.Session.Components.All)
        {
            if (record.IsDisposed || record.IsBevTools || record.Component is not { } live)
            {
                continue;
            }

            for (var type = live.GetType(); type is not null && type != typeof(ComponentBase) && type != typeof(object); type = type.BaseType)
            {
                foreach (var field in Fields.GetOrAdd(type, static t => t.GetFields(InstanceFields)))
                {
                    if (field.FieldType.IsValueType || field.FieldType == typeof(string) || typeof(Delegate).IsAssignableFrom(field.FieldType))
                    {
                        continue;
                    }

                    object? value;
                    try
                    {
                        value = field.GetValue(live);
                    }
                    catch
                    {
                        continue;
                    }

                    switch (value)
                    {
                        case IComponent held:
                            Add(held, record, field, "a field that still references the removed component, as an @ref does");
                            break;
                        case IDictionary dictionary when IsFrameworkCollection(value.GetType()):
                            Items(DictionaryValues(dictionary), record, field);
                            break;
                        case IEnumerable items when IsFrameworkCollection(value.GetType()):
                            Items(items, record, field);
                            break;
                    }
                }
            }
        }

        return index;

        void Items(IEnumerable items, BevComponentRecord record, FieldInfo field)
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

                    if (item is IComponent held)
                    {
                        Add(held, record, field, "a collection that still contains the removed component");
                    }
                }
            }
            catch
            {
                // Modified while it is read: nothing to report from it.
            }
        }

        void Add(IComponent held, BevComponentRecord record, FieldInfo field, string detail)
        {
            if (!scan.Disposed.Contains(held))
            {
                return;
            }

            if (!index.TryGetValue(held, out var causes))
            {
                index[held] = causes = [];
            }

            if (causes.Count < 10)
            {
                causes.Add(new BevRetentionCause(record.DisplayName + " (still rendered) → " + MemberName(field), detail,
                    "Clear the field when the markup stops rendering the component (or keep its data instead of the component): Blazor never clears an @ref."));
            }
        }
    }

    /// <summary>
    /// A DotNetObjectReference passed to JS is kept in the runtime's table until it is disposed - for a circuit, until
    /// the circuit ends - and with it whatever it wraps, and whatever that holds: a library's object (an animation
    /// engine, an observer) that keeps the component's callback. Indexed once per check by the component each one reaches.
    /// </summary>
    private static Dictionary<object, List<BevRetentionCause>> IndexJSRuntime(BevToolsSession session)
    {
        var index = new Dictionary<object, List<BevRetentionCause>>(ReferenceEqualityComparer.Instance);
        if (BevJSObjectReferences.Table(session) is not { } table)
        {
            return index;
        }

        // One budget for every target together: a page can hand JavaScript hundreds of references.
        var count = 0;
        var budget = 2 * MaxObjectsPerHolder;
        foreach (var reference in DictionaryValues(table))
        {
            if (++count > 10 * MaxItemsPerCollection)
            {
                break;
            }

            var target = BevJSObjectReferences.Target(reference);
            if (target is not null && target.GetType().Namespace == "Bit.BevTools")
            {
                // BevTools' own references (each panel's bridge): never what holds an application component.
                continue;
            }

            var description = "a " + TypeNames.Short(reference!.GetType()) + " passed to JavaScript and never disposed";
            if (target is IComponent component)
            {
                Add(component, description, "Dispose the DotNetObjectReference in Dispose/DisposeAsync (and tell the JS side to drop it).");
            }
            else if (target is not null)
            {
                if (budget <= 0)
                {
                    continue;
                }

                var walker = new Walker(delegatesOnly: false, budget);
                walker.Walk(target, "", 0);
                budget = walker.Remaining;
                // A lambda's closure has compiler-made field names: its path would say nothing.
                var named = !target.GetType().IsDefined(typeof(CompilerGeneratedAttribute), false);
                foreach (var (held, paths) in walker.Index)
                {
                    Add(held, description + (named ? ", whose target holds it (" + TypeNames.Short(target.GetType()) + "." + paths[0].Member + ")" : ""),
                        "Dispose the DotNetObjectReference, or what the component got from the library that created it, in Dispose/DisposeAsync.");
                }
            }
        }

        return index;

        void Add(object held, string detail, string suggestion)
        {
            if (!index.ContainsKey(held))
            {
                index[held] = [new BevRetentionCause("the JS runtime", detail, suggestion)];
            }
        }
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
    /// The components the renderers themselves still reference from their frame buffers, of every renderer the session
    /// saw (a page can have more than one): a component its parent stopped rendering is kept in the parent's previous
    /// render tree until the parent renders again, and in buffers reused without clearing. Held only there, a disposed
    /// component is not leaked: it goes with the parent's next render.
    /// </summary>
    public static HashSet<IComponent> HeldByRenderer(BevToolsSession session)
    {
        var held = new HashSet<IComponent>(ReferenceEqualityComparer.Instance);
        foreach (var renderer in session.Components.Renderers)
        {
            if (BlazorReflection.ComponentStates(renderer) is not { } states)
            {
                continue;
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
