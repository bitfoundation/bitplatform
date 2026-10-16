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

    public static Exception? InitializationError { get; }

    public static bool CanWrapRenderFragment => RenderFragmentField is not null && GetRenderFragment is not null;

    public static bool CanResolveRenderer => GetRenderHandle is not null && GetRendererFromHandle is not null;

    public static bool CanResolveHierarchy => CanResolveRenderer && TryGetComponentState is not null;

    public static bool CanDetectBatches => GetUpdatedDiffCount is not null;

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
