using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Bit.BevTools;

/// <summary>
/// Cached reflection over Blazor internals. Every accessor is optional: when a member is missing (framework change)
/// the corresponding capability is reported as unavailable instead of failing. Only development builds load this.
/// </summary>
internal static class BlazorReflection
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    static BlazorReflection()
    {
        try
        {
            RenderFragmentField = typeof(ComponentBase).GetField("_renderFragment", Instance);
            RenderHandleField = typeof(ComponentBase).GetField("_renderHandle", Instance);
            InitializedField = typeof(ComponentBase).GetField("_initialized", Instance);
            RendererInfoProperty = typeof(ComponentBase).GetProperty("RendererInfo", Instance);
            HandleRendererField = typeof(RenderHandle).GetField("_renderer", Instance);
            HandleComponentIdField = typeof(RenderHandle).GetField("_componentId", Instance);

            if (RenderFragmentField is not null && RenderHandleField is not null)
            {
                var component = Expression.Parameter(typeof(ComponentBase), "c");
                GetRenderFragment = Expression.Lambda<Func<ComponentBase, RenderFragment?>>(Expression.Field(component, RenderFragmentField), component).Compile();
                GetRenderHandle = Expression.Lambda<Func<ComponentBase, RenderHandle>>(Expression.Field(component, RenderHandleField), component).Compile();
            }

            if (InitializedField is not null)
            {
                var component = Expression.Parameter(typeof(ComponentBase), "c");
                GetInitialized = Expression.Lambda<Func<ComponentBase, bool>>(Expression.Field(component, InitializedField), component).Compile();
            }

            if (HandleRendererField is not null && HandleComponentIdField is not null)
            {
                var handle = Expression.Parameter(typeof(RenderHandle), "h");
                GetRendererFromHandle = Expression.Lambda<Func<RenderHandle, Renderer?>>(Expression.Field(handle, HandleRendererField), handle).Compile();
                GetComponentIdFromHandle = Expression.Lambda<Func<RenderHandle, int>>(Expression.Field(handle, HandleComponentIdField), handle).Compile();
            }

            var stateByComponentField = typeof(Renderer).GetField("_componentStateByComponent", Instance);
            if (stateByComponentField is not null && stateByComponentField.FieldType.IsGenericType)
            {
                var renderer = Expression.Parameter(typeof(Renderer), "r");
                var component = Expression.Parameter(typeof(IComponent), "c");
                var dict = Expression.Field(renderer, stateByComponentField);
                var tryGetValue = stateByComponentField.FieldType.GetMethod("TryGetValue");
                if (tryGetValue is not null)
                {
                    var result = Expression.Variable(typeof(ComponentState), "state");
                    var body = Expression.Block(
                        [result],
                        Expression.Condition(
                            Expression.Call(dict, tryGetValue, component, result),
                            result,
                            Expression.Constant(null, typeof(ComponentState))));
                    TryGetComponentState = Expression.Lambda<Func<Renderer, IComponent, ComponentState?>>(body, renderer, component).Compile();
                }
            }

            var batchBuilderField = typeof(Renderer).GetField("_batchBuilder", Instance);
            var diffsProperty = batchBuilderField?.FieldType.GetProperty("UpdatedComponentDiffs", Instance);
            var countProperty = diffsProperty?.PropertyType.GetProperty("Count", Instance);
            if (batchBuilderField is not null && diffsProperty is not null && countProperty is not null)
            {
                var renderer = Expression.Parameter(typeof(Renderer), "r");
                var body = Expression.Property(Expression.Property(Expression.Field(renderer, batchBuilderField), diffsProperty), countProperty);
                GetUpdatedDiffCount = Expression.Lambda<Func<Renderer, int>>(body, renderer).Compile();
            }

            var batchInProgressField = typeof(Renderer).GetField("_isBatchInProgress", Instance);
            if (batchInProgressField is not null)
            {
                var renderer = Expression.Parameter(typeof(Renderer), "r");
                GetIsBatchInProgress = Expression.Lambda<Func<Renderer, bool>>(Expression.Field(renderer, batchInProgressField), renderer).Compile();
            }

            // The renders the renderer was asked for and has not started: RenderBatchBuilder.ComponentRenderQueue, a
            // queue of RenderQueueEntry structs whose ComponentState field names the component.
            var queueProperty = batchBuilderField?.FieldType.GetProperty("ComponentRenderQueue", Instance);
            var entryType = queueProperty?.PropertyType is { IsGenericType: true } queueType ? queueType.GetGenericArguments()[0] : null;
            var entryStateField = entryType?.GetField("ComponentState", Instance);
            if (batchBuilderField is not null && queueProperty is not null && typeof(IEnumerable).IsAssignableFrom(queueProperty.PropertyType)
                && entryStateField is not null && entryStateField.FieldType == typeof(ComponentState))
            {
                var renderer = Expression.Parameter(typeof(Renderer), "r");
                GetRenderQueue = Expression.Lambda<Func<Renderer, IEnumerable>>(Expression.Property(Expression.Field(renderer, batchBuilderField), queueProperty), renderer).Compile();
                var entry = Expression.Parameter(typeof(object), "e");
                GetQueueEntryState = Expression.Lambda<Func<object, ComponentState>>(Expression.Field(Expression.Convert(entry, entryType!), entryStateField), entry).Compile();
            }

            var componentFactoryField = typeof(Renderer).GetField("_componentFactory", Instance);
            var componentActivatorField = componentFactoryField?.FieldType.GetField("_componentActivator", Instance);
            if (componentFactoryField is not null && componentActivatorField?.FieldType == typeof(IComponentActivator))
            {
                ComponentFactoryField = componentFactoryField;
                ComponentActivatorField = componentActivatorField;
            }

            var stateByIdField = typeof(Renderer).GetField("_componentStateById", Instance);
            if (stateByIdField is not null && typeof(IEnumerable<KeyValuePair<int, ComponentState>>).IsAssignableFrom(stateByIdField.FieldType))
            {
                ComponentStateByIdField = stateByIdField;
            }

            RendererDisposedField = typeof(Renderer).GetField("_rendererIsDisposed", Instance) is { } disposedField && disposedField.FieldType == typeof(bool) ? disposedField : null;
            HasNeverRenderedField = typeof(ComponentBase).GetField("_hasNeverRendered", Instance) is { } neverRendered && neverRendered.FieldType == typeof(bool) ? neverRendered : null;
            HasPendingQueuedRenderField = typeof(ComponentBase).GetField("_hasPendingQueuedRender", Instance) is { } pending && pending.FieldType == typeof(bool) ? pending : null;
            HasCalledOnAfterRenderField = typeof(ComponentBase).GetField("_hasCalledOnAfterRender", Instance) is { } afterRender && afterRender.FieldType == typeof(bool) ? afterRender : null;

            // The frames a component produced on its latest render: what it bound (event handlers, child component
            // parameters), read from the renderer's own copy rather than inferred.
            CurrentRenderTreeProperty = typeof(ComponentState).GetProperty("CurrentRenderTree", Instance) is { } tree && tree.PropertyType == typeof(RenderTreeBuilder) ? tree : null;
            EventCallbackDelegateField = typeof(EventCallback).GetField("Delegate", Instance) is { } callbackDelegate && typeof(Delegate).IsAssignableFrom(callbackDelegate.FieldType) ? callbackDelegate : null;
            GetComponentRenderModeMethod = typeof(Renderer).GetMethod("GetComponentRenderMode", Instance, [typeof(IComponent)]) is { } renderMode && renderMode.ReturnType == typeof(IComponentRenderMode) ? renderMode : null;
        }
        catch (Exception ex)
        {
            InitializationError = ex;
        }
    }

    public static FieldInfo? RenderFragmentField { get; }

    public static FieldInfo? RenderHandleField { get; }

    public static FieldInfo? InitializedField { get; }

    public static PropertyInfo? RendererInfoProperty { get; }

    public static FieldInfo? HandleRendererField { get; }

    public static FieldInfo? HandleComponentIdField { get; }

    public static Func<ComponentBase, RenderFragment?>? GetRenderFragment { get; }

    public static Func<ComponentBase, RenderHandle>? GetRenderHandle { get; }

    public static Func<ComponentBase, bool>? GetInitialized { get; }

    public static Func<RenderHandle, Renderer?>? GetRendererFromHandle { get; }

    public static Func<RenderHandle, int>? GetComponentIdFromHandle { get; }

    public static Func<Renderer, IComponent, ComponentState?>? TryGetComponentState { get; }

    public static Func<Renderer, int>? GetUpdatedDiffCount { get; }

    public static Func<Renderer, bool>? GetIsBatchInProgress { get; }

    public static Func<Renderer, IEnumerable>? GetRenderQueue { get; }

    public static Func<object, ComponentState>? GetQueueEntryState { get; }

    public static FieldInfo? ComponentFactoryField { get; }

    public static FieldInfo? ComponentActivatorField { get; }

    public static FieldInfo? ComponentStateByIdField { get; }

    public static FieldInfo? RendererDisposedField { get; }

    public static FieldInfo? HasNeverRenderedField { get; }

    public static FieldInfo? HasPendingQueuedRenderField { get; }

    public static FieldInfo? HasCalledOnAfterRenderField { get; }

    public static PropertyInfo? CurrentRenderTreeProperty { get; }

    public static FieldInfo? EventCallbackDelegateField { get; }

    public static MethodInfo? GetComponentRenderModeMethod { get; }

    public static Exception? InitializationError { get; }

    public static bool CanReadRenderTree => CurrentRenderTreeProperty is not null;

    /// <summary>The frames the component produced on its latest render; null when they cannot be read.</summary>
    public static RenderTreeFrame[]? CurrentFrames(ComponentState state)
    {
        try
        {
            if (CurrentRenderTreeProperty?.GetValue(state) is not RenderTreeBuilder builder)
            {
                return null;
            }

            var frames = builder.GetFrames();
            // A copy: the builder's buffer is reused by the next render, and the caller reads it after this returns.
            return frames.Array.AsSpan(0, frames.Count).ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static readonly ConcurrentDictionary<Type, FieldInfo?> TypedCallbackDelegateFields = new();

    /// <summary>
    /// The delegate an <c>EventCallback</c> or <c>EventCallback&lt;T&gt;</c> invokes, which says what it is bound to
    /// ("ProductList.OnSelected"); the struct keeps it in an internal field. Null when unbound or unreadable.
    /// </summary>
    public static Delegate? EventCallbackDelegate(object callback)
    {
        try
        {
            if (callback is EventCallback untyped)
            {
                return EventCallbackDelegateField?.GetValue(untyped) as Delegate;
            }

            var field = TypedCallbackDelegateFields.GetOrAdd(callback.GetType(), static type =>
                type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>)
                    && type.GetField("Delegate", Instance) is { } f && typeof(Delegate).IsAssignableFrom(f.FieldType) ? f : null);
            return field?.GetValue(callback) as Delegate;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// The render mode the renderer assigned to a component (<c>@rendermode</c> on it or on an ancestor's component
    /// frame, or <c>[RenderMode*]</c> on its type). <c>ComponentBase.AssignedRenderMode</c> reads the same on .NET 9,
    /// but it is protected; this works on every version and for components that do not derive from ComponentBase.
    /// </summary>
    public static IComponentRenderMode? AssignedRenderMode(Renderer renderer, IComponent component)
    {
        try
        {
            return GetComponentRenderModeMethod?.Invoke(renderer, [component]) as IComponentRenderMode;
        }
        catch
        {
            return null;
        }
    }

    private static readonly PropertyInfo? JSObjectIdProperty = typeof(Microsoft.JSInterop.Implementation.JSObjectReference).GetProperty("Id", Instance) is { } id && id.PropertyType == typeof(long) ? id : null;

    /// <summary>
    /// The id the JS side knows a JS object reference by (protected internal on <c>JSObjectReference</c>). Every .NET → JS
    /// call on that object names it, which is how the page hub tells BevTools' own calls over a circuit from the app's.
    /// </summary>
    public static long? JSObjectId(object reference)
    {
        try
        {
            return reference is Microsoft.JSInterop.Implementation.JSObjectReference && JSObjectIdProperty?.GetValue(reference) is long value ? value : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Whether ComponentBase ran OnAfterRender at least once; null when the field is missing.</summary>
    public static bool? HasCalledOnAfterRender(ComponentBase component)
    {
        try
        {
            return HasCalledOnAfterRenderField?.GetValue(component) as bool?;
        }
        catch
        {
            return null;
        }
    }

    public static bool CanWrapRenderFragment => RenderFragmentField is not null && GetRenderFragment is not null;

    public static bool CanResolveRenderer => GetRenderHandle is not null && GetRendererFromHandle is not null;

    public static bool CanResolveHierarchy => CanResolveRenderer && TryGetComponentState is not null;

    public static bool CanDetectBatches => GetUpdatedDiffCount is not null;

    public static bool CanReadRenderQueue => GetRenderQueue is not null && GetQueueEntryState is not null;

    /// <summary>The components whose render is queued but not started, in queue order; null when the queue cannot be read.</summary>
    public static IComponent[]? QueuedComponents(Renderer renderer)
    {
        if (GetRenderQueue is null || GetQueueEntryState is null)
        {
            return null;
        }

        try
        {
            List<IComponent>? components = null;
            foreach (var entry in GetRenderQueue(renderer))
            {
                (components ??= []).Add(GetQueueEntryState(entry!).Component);
            }

            return components?.ToArray() ?? [];
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Replaces the component's render fragment. Works on the readonly field through FieldInfo (allowed by the runtime for instance fields).</summary>
    public static bool TrySetRenderFragment(ComponentBase component, RenderFragment fragment)
    {
        if (RenderFragmentField is null)
        {
            return false;
        }

        try
        {
            RenderFragmentField.SetValue(component, fragment);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static Renderer? ResolveRenderer(ComponentBase component, out int componentId)
    {
        componentId = -1;
        if (GetRenderHandle is null || GetRendererFromHandle is null)
        {
            return null;
        }

        var handle = GetRenderHandle(component);
        var renderer = GetRendererFromHandle(handle);
        if (renderer is not null && GetComponentIdFromHandle is not null)
        {
            componentId = GetComponentIdFromHandle(handle);
        }

        return renderer;
    }

    /// <summary>
    /// The renderer's name and interactivity from <c>ComponentBase.RendererInfo</c>, which .NET 9 introduced; null on
    /// .NET 8, where the property does not exist and the panel infers both from its JS runtime instead.
    /// </summary>
    public static (string Name, bool IsInteractive)? ResolveRendererInfo(ComponentBase component)
    {
        try
        {
#if NET9_0_OR_GREATER
            return RendererInfoProperty?.GetValue(component) is RendererInfo info ? (info.Name, info.IsInteractive) : null;
#else
            return null;
#endif
        }
        catch
        {
            return null;
        }
    }

    public static bool TryWrapComponentActivator(Renderer renderer, Func<IComponentActivator, IComponentActivator?> wrap)
    {
        if (ComponentFactoryField is null || ComponentActivatorField is null)
        {
            return false;
        }

        try
        {
            var factory = ComponentFactoryField.GetValue(renderer);
            if (factory is null || ComponentActivatorField.GetValue(factory) is not IComponentActivator current)
            {
                return false;
            }

            if (wrap(current) is { } wrapped)
            {
                ComponentActivatorField.SetValue(factory, wrapped);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static ComponentState[]? ComponentStates(Renderer renderer)
    {
        if (ComponentStateByIdField is null)
        {
            return null;
        }

        try
        {
            return ComponentStateByIdField.GetValue(renderer) is IEnumerable<KeyValuePair<int, ComponentState>> states
                ? states.OrderBy(s => s.Key).Select(s => s.Value).ToArray()
                : null;
        }
        catch
        {
            return null;
        }
    }

    public static bool IsRendererDisposed(Renderer renderer)
    {
        try
        {
            return RendererDisposedField?.GetValue(renderer) is true;
        }
        catch
        {
            return false;
        }
    }

    public static bool HasRenderedOrQueued(ComponentBase component)
    {
        try
        {
            return HasNeverRenderedField is null || HasPendingQueuedRenderField is null
                || HasNeverRenderedField.GetValue(component) is false
                || HasPendingQueuedRenderField.GetValue(component) is true;
        }
        catch
        {
            return true;
        }
    }

    private static readonly ConcurrentDictionary<Type, Func<object, bool>?> JSRuntimeNotReadyProbes = new();
    private static readonly ConcurrentDictionary<string, byte> JSRuntimeReadinessGapNames = new(StringComparer.Ordinal);

    /// <summary>
    /// Reads whether a JS runtime cannot take calls yet: a prerender's never can, a circuit's until the browser has
    /// connected (<c>RemoteJSRuntime.IsInitialized</c>), a WebView's until its IPC channel is attached
    /// (<c>WebViewJSRuntime._ipcSender</c>). Resolved per runtime type, as this package references neither host; null when
    /// the host's member is missing on this framework version. Every other runtime is always ready.
    /// </summary>
    public static Func<object, bool>? ResolveJSRuntimeNotReady(Type runtimeType) => runtimeType.Name switch
    {
        "UnsupportedJavaScriptRuntime" => static _ => true,
        "RemoteJSRuntime" => runtimeType.GetProperty("IsInitialized", Instance) is { } isInitialized && isInitialized.PropertyType == typeof(bool)
            ? js => isInitialized.GetValue(js) is false
            : null,
        "WebViewJSRuntime" => runtimeType.GetField("_ipcSender", Instance) is { } ipcSender
            ? js => ipcSender.GetValue(js) is null
            : null,
        _ => static _ => false,
    };

    /// <summary>
    /// True while the runtime cannot take calls. One whose readiness cannot be read counts as ready, which is what
    /// BevTools assumed before it looked, and is reported through <see cref="JSRuntimeReadinessGaps"/>.
    /// </summary>
    public static bool IsJSRuntimeNotReady(object runtime)
    {
        var probe = JSRuntimeNotReadyProbes.GetOrAdd(runtime.GetType(), ResolveJSRuntimeNotReady);
        if (probe is null)
        {
            JSRuntimeReadinessGapNames.TryAdd(runtime.GetType().Name, 0);
            return false;
        }

        try
        {
            return probe(runtime);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>The JS runtimes met so far whose readiness could not be read on this framework version.</summary>
    public static IReadOnlyCollection<string> JSRuntimeReadinessGaps => [.. JSRuntimeReadinessGapNames.Keys];

    /// <summary>Reads renderer, component id and parent for a non-ComponentBase component via the renderer's component-state map.</summary>
    public static ComponentState? ResolveState(Renderer renderer, IComponent component)
    {
        try
        {
            return TryGetComponentState?.Invoke(renderer, component);
        }
        catch
        {
            return null;
        }
    }
}
