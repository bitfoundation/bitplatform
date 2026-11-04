using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Extras.ResourceTimeline;

/// <summary>
/// What the BitResourceTimeline tests stand the browser side in with. The engine answers <c>setup</c> and
/// <c>setOptions</c> with the layout it works with, an internal record struct of the Extras assembly, so the results of
/// those two calls are built and handed to bUnit by reflection: a planned result of any other type would not match the
/// <c>InvokeAsync&lt;BitResourceTimelineLayout?&gt;</c> the component makes.
/// </summary>
internal static class BitResourceTimelineTestInterop
{
    public const string Prefix = "BitBlazorUI.ResourceTimeline.";
    public const string SetupFn = Prefix + "setup";
    public const string SetOptionsFn = Prefix + "setOptions";
    public const string EnableResourceTemplateFn = Prefix + "enableResourceTemplate";
    public const string DisableResourceTemplateFn = Prefix + "disableResourceTemplate";
    public const string EnableTooltipTemplateFn = Prefix + "enableTooltipTemplate";
    public const string DisableTooltipTemplateFn = Prefix + "disableTooltipTemplate";
    public const string SetDataFn = Prefix + "setData";
    public const string BeginDataFn = Prefix + "beginData";
    public const string AppendAllocationsFn = Prefix + "appendAllocations";
    public const string EndDataFn = Prefix + "endData";
    public const string BeginWindowedFn = Prefix + "beginWindowed";
    public const string GetVisibleWindowFn = Prefix + "getVisibleWindow";
    public const string ApplyAllocationWindowFn = Prefix + "applyAllocationWindow";
    public const string WhenRenderedFn = Prefix + "whenRendered";
    public const string UpsertAllocationsFn = Prefix + "upsertAllocations";
    public const string RemoveAllocationsFn = Prefix + "removeAllocations";
    public const string ClearSelectionFn = Prefix + "clearSelection";
    public const string SelectBarsFn = Prefix + "selectBars";
    public const string ScrollToAllocationFn = Prefix + "scrollToAllocation";
    public const string ScrollToResourceFn = Prefix + "scrollToResource";
    public const string GetSelectedBarIdsFn = Prefix + "getSelectedBarIds";
    public const string GoToNowFn = Prefix + "goToNow";
    public const string ScrollToTimeFn = Prefix + "scrollToTime";
    public const string PanByDaysFn = Prefix + "panByDays";
    public const string ZoomInFn = Prefix + "zoomIn";
    public const string ZoomOutFn = Prefix + "zoomOut";
    public const string ZoomToDaysFn = Prefix + "zoomToDays";
    public const string SetPixelsPerHourFn = Prefix + "setPixelsPerHour";
    public const string ResetZoomFn = Prefix + "resetZoom";
    public const string GetPixelsPerHourFn = Prefix + "getPixelsPerHour";
    public const string ToggleGroupFn = Prefix + "toggleGroup";
    public const string DisposeFn = Prefix + "dispose";

    /// <summary>The JSON options the JS runtime of Blazor serializes the interop payloads with: camelCase names.</summary>
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    public static readonly Assembly Library = typeof(BitResourceTimeline).Assembly;

    public static readonly Type LayoutType = Library.GetType("Bit.BlazorUI.BitResourceTimelineLayout", throwOnError: true)!;

    private static readonly MethodInfo SetupLayoutResult = typeof(BunitJSInteropSetupExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(BunitJSInteropSetupExtensions.Setup)
                     && m.IsGenericMethodDefinition
                     && m.GetParameters().Select(p => p.ParameterType).SequenceEqual([typeof(BunitJSInterop), typeof(string), typeof(InvocationMatcher)]))
        .MakeGenericMethod(typeof(Nullable<>).MakeGenericType(LayoutType));

    /// <summary>A layout as the engine reports it: the width of the resource column, the height of the time axis, the
    /// minimum height of a row and the indent of a nesting level, in pixels.</summary>
    public static object Layout(int resourceAxisWidth = 150, int timeAxisHeight = 60, int resourceHeight = 40, int resourceIndent = 16)
    {
        return Activator.CreateInstance(LayoutType, resourceAxisWidth, timeAxisHeight, resourceHeight, resourceIndent)!;
    }

    /// <summary>
    /// Plans every call of <paramref name="identifier"/> (setup or setOptions) to wait for the layout the returned handle
    /// is given, the way bUnit plans any other result.
    /// </summary>
    public static LayoutCall PlanLayout(this BunitJSInterop interop, string identifier)
    {
        return new(SetupLayoutResult.Invoke(null, [interop, identifier, (InvocationMatcher)(_ => true)])!);
    }

    /// <summary>A resource and a one-hour range holding a single one-minute bar of it.</summary>
    public static BitResourceTimelineConfig SampleConfig() => new()
    {
        Resources = [new() { Id = "r1", Name = "Resource 1" }],
        StartDate = DateTimeOffset.FromUnixTimeMilliseconds(0),
        EndDate = DateTimeOffset.FromUnixTimeMilliseconds(3_600_000),
        Allocations =
        [
            new()
            {
                Id = "a1",
                ResourceId = "r1",
                StartTime = DateTimeOffset.FromUnixTimeMilliseconds(0),
                EndTime = DateTimeOffset.FromUnixTimeMilliseconds(60_000),
            },
        ],
    };

    public static BitResourceTimelineAllocation Allocation(string id, string resourceId, long startMs, long endMs) => new()
    {
        Id = id,
        ResourceId = resourceId,
        StartTime = DateTimeOffset.FromUnixTimeMilliseconds(startMs),
        EndTime = DateTimeOffset.FromUnixTimeMilliseconds(endMs),
    };

    /// <summary>A planned setup or setOptions call, completed with a layout (or with null, as a prerender answers).</summary>
    public sealed class LayoutCall(object handler)
    {
        private readonly MethodInfo _setResult = handler.GetType().GetMethod("SetResult")!;

        public void Return(object? layout) => _setResult.Invoke(handler, [layout]);
    }
}
