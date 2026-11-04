using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitResourceTimelineJsRuntimeExtensions
{
    // What crosses the interop boundary is read and written by System.Text.Json through reflection, which the trimmer
    // cannot see through the params object[] of an interop call: without these the getters of a type only the serializer
    // reads (and the setters and constructors only it calls) are trimmed away, and a published app sends the options or
    // the bars as {} - the engine then silently keeps its defaults. Every type reachable from a payload is listed, the
    // nested ones too (InvokeAsync<T> preserves T alone). The time converters are not: the [JsonConverter] naming one
    // already keeps its constructor, and rooting a JsonConverter whole would keep the RequiresUnreferencedCode members of
    // its base class too, which fails the trimmed publish of an app (IL2026).
    private const DynamicallyAccessedMemberTypes All = DynamicallyAccessedMemberTypes.All;



    [DynamicDependency(All, typeof(BitResourceTimelineLayout))]
    [DynamicDependency(All, typeof(BitResourceTimelineOptions))]
    [DynamicDependency(All, typeof(BitResourceTimelineColors))]
    [DynamicDependency(All, typeof(BitResourceTimelineSurface))]
    [DynamicDependency(All, typeof(BitResourceTimelineSurfaceBand))]
    [DynamicDependency(All, typeof(BitResourceTimelineColumnStripes))]
    [DynamicDependency(All, typeof(BitResourceTimelineRowStripes))]
    [DynamicDependency(All, typeof(BitResourceTimelineAxisLines))]
    [DynamicDependency(All, typeof(BitResourceTimelineTexts))]
    public static ValueTask<BitResourceTimelineLayout?> BitResourceTimelineSetup(this IJSRuntime jsRuntime,
                                                                                    string id,
                                                                                    ElementReference viewport,
                                                                                    DotNetObjectReference<BitResourceTimeline> dotnetObj,
                                                                                    BitResourceTimelineOptions? options,
                                                                                    BitResourceTimelineTexts texts,
                                                                                    bool disabled)
    {
        return jsRuntime.Invoke<BitResourceTimelineLayout?>("BitBlazorUI.ResourceTimeline.setup", id, viewport, dotnetObj, options, texts, disabled);
    }

    [DynamicDependency(All, typeof(BitResourceTimelineLayout))]
    [DynamicDependency(All, typeof(BitResourceTimelineOptions))]
    [DynamicDependency(All, typeof(BitResourceTimelineColors))]
    [DynamicDependency(All, typeof(BitResourceTimelineSurface))]
    [DynamicDependency(All, typeof(BitResourceTimelineSurfaceBand))]
    [DynamicDependency(All, typeof(BitResourceTimelineColumnStripes))]
    [DynamicDependency(All, typeof(BitResourceTimelineRowStripes))]
    [DynamicDependency(All, typeof(BitResourceTimelineAxisLines))]
    [DynamicDependency(All, typeof(BitResourceTimelineTexts))]
    public static ValueTask<BitResourceTimelineLayout?> BitResourceTimelineSetOptions(this IJSRuntime jsRuntime,
                                                                                         string id,
                                                                                         BitResourceTimelineOptions? options,
                                                                                         BitResourceTimelineTexts texts,
                                                                                         bool disabled)
    {
        return jsRuntime.Invoke<BitResourceTimelineLayout?>("BitBlazorUI.ResourceTimeline.setOptions", id, options, texts, disabled);
    }

    public static ValueTask BitResourceTimelineEnableResourceTemplate(this IJSRuntime jsRuntime, string id, ElementReference overlayInner)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.enableResourceTemplate", id, overlayInner);
    }

    public static ValueTask BitResourceTimelineDisableResourceTemplate(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.disableResourceTemplate", id);
    }

    public static ValueTask BitResourceTimelineEnableTooltipTemplate(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.enableTooltipTemplate", id);
    }

    public static ValueTask BitResourceTimelineDisableTooltipTemplate(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.disableTooltipTemplate", id);
    }

    [DynamicDependency(All, typeof(BitResourceTimelineResource))]
    [DynamicDependency(All, typeof(BitResourceTimelineAllocation))]
    [DynamicDependency(All, typeof(BitResourceTimelineBarIcon))]
    [DynamicDependency(All, typeof(BitResourceTimelineEdgeBar))]
    [DynamicDependency(All, typeof(BitResourceTimelineBarSection))]
    public static ValueTask BitResourceTimelineSetData(this IJSRuntime jsRuntime,
                                                       string id,
                                                       IReadOnlyList<BitResourceTimelineResource> resources,
                                                       long start,
                                                       long end,
                                                       IReadOnlyList<BitResourceTimelineAllocation> allocations)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.setData", id, resources, start, end, allocations);
    }

    [DynamicDependency(All, typeof(BitResourceTimelineResource))]
    public static ValueTask BitResourceTimelineBeginData(this IJSRuntime jsRuntime,
                                                         string id,
                                                         IReadOnlyList<BitResourceTimelineResource> resources,
                                                         long start,
                                                         long end,
                                                         int expectedCount)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.beginData", id, resources, start, end, expectedCount);
    }

    [DynamicDependency(All, typeof(BitResourceTimelineAllocation))]
    [DynamicDependency(All, typeof(BitResourceTimelineBarIcon))]
    [DynamicDependency(All, typeof(BitResourceTimelineEdgeBar))]
    [DynamicDependency(All, typeof(BitResourceTimelineBarSection))]
    public static ValueTask BitResourceTimelineAppendAllocations(this IJSRuntime jsRuntime, string id, IReadOnlyList<BitResourceTimelineAllocation> allocations)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.appendAllocations", id, allocations);
    }

    public static ValueTask BitResourceTimelineEndData(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.endData", id);
    }

    [DynamicDependency(All, typeof(BitResourceTimelineResource))]
    public static ValueTask BitResourceTimelineBeginWindowed(this IJSRuntime jsRuntime,
                                                             string id,
                                                             IReadOnlyList<BitResourceTimelineResource> resources,
                                                             long start,
                                                             long end,
                                                             long session)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.beginWindowed", id, resources, start, end, session);
    }

    public static ValueTask<long[]?> BitResourceTimelineGetVisibleWindow(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<long[]?>("BitBlazorUI.ResourceTimeline.getVisibleWindow", id);
    }

    [DynamicDependency(All, typeof(BitResourceTimelineAllocation))]
    [DynamicDependency(All, typeof(BitResourceTimelineBarIcon))]
    [DynamicDependency(All, typeof(BitResourceTimelineEdgeBar))]
    [DynamicDependency(All, typeof(BitResourceTimelineBarSection))]
    public static ValueTask BitResourceTimelineApplyAllocationWindow(this IJSRuntime jsRuntime,
                                                                     string id,
                                                                     long requestId,
                                                                     IReadOnlyList<BitResourceTimelineAllocation> allocations,
                                                                     long start,
                                                                     long end,
                                                                     long session)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.applyAllocationWindow", id, requestId, allocations, start, end, session);
    }

    public static ValueTask BitResourceTimelineWhenRendered(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.whenRendered", id);
    }

    [DynamicDependency(All, typeof(BitResourceTimelineAllocation))]
    [DynamicDependency(All, typeof(BitResourceTimelineBarIcon))]
    [DynamicDependency(All, typeof(BitResourceTimelineEdgeBar))]
    [DynamicDependency(All, typeof(BitResourceTimelineBarSection))]
    public static ValueTask BitResourceTimelineUpsertAllocations(this IJSRuntime jsRuntime, string id, IReadOnlyList<BitResourceTimelineAllocation> allocations)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.upsertAllocations", id, allocations);
    }

    public static ValueTask BitResourceTimelineRemoveAllocations(this IJSRuntime jsRuntime, string id, IReadOnlyList<string> ids)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.removeAllocations", id, ids);
    }

    public static ValueTask BitResourceTimelineClearSelection(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.clearSelection", id);
    }

    public static ValueTask BitResourceTimelineSelectBars(this IJSRuntime jsRuntime, string id, IReadOnlyList<string> ids, bool additive)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.selectBars", id, ids, additive);
    }

    public static ValueTask<bool> BitResourceTimelineScrollToAllocation(this IJSRuntime jsRuntime, string id, string allocationId)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.ResourceTimeline.scrollToAllocation", id, allocationId);
    }

    public static ValueTask<bool> BitResourceTimelineScrollToResource(this IJSRuntime jsRuntime, string id, string resourceId)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.ResourceTimeline.scrollToResource", id, resourceId);
    }

    public static ValueTask<string[]?> BitResourceTimelineGetSelectedBarIds(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<string[]?>("BitBlazorUI.ResourceTimeline.getSelectedBarIds", id);
    }

    public static ValueTask<bool> BitResourceTimelineGoToNow(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.ResourceTimeline.goToNow", id);
    }

    public static ValueTask<bool> BitResourceTimelineScrollToTime(this IJSRuntime jsRuntime, string id, long unixMs)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.ResourceTimeline.scrollToTime", id, unixMs);
    }

    public static ValueTask<bool> BitResourceTimelinePanByDays(this IJSRuntime jsRuntime, string id, int days, bool? panToDayStart)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.ResourceTimeline.panByDays", id, days, panToDayStart);
    }

    public static ValueTask<double> BitResourceTimelineZoomIn(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<double>("BitBlazorUI.ResourceTimeline.zoomIn", id);
    }

    public static ValueTask<double> BitResourceTimelineZoomOut(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<double>("BitBlazorUI.ResourceTimeline.zoomOut", id);
    }

    public static ValueTask<double> BitResourceTimelineZoomToDays(this IJSRuntime jsRuntime, string id, double days)
    {
        return jsRuntime.Invoke<double>("BitBlazorUI.ResourceTimeline.zoomToDays", id, days);
    }

    public static ValueTask<double> BitResourceTimelineSetPixelsPerHour(this IJSRuntime jsRuntime, string id, double? pixelsPerHour)
    {
        return jsRuntime.Invoke<double>("BitBlazorUI.ResourceTimeline.setPixelsPerHour", id, pixelsPerHour);
    }

    public static ValueTask<double> BitResourceTimelineResetZoom(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<double>("BitBlazorUI.ResourceTimeline.resetZoom", id);
    }

    public static ValueTask<double> BitResourceTimelineGetPixelsPerHour(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<double>("BitBlazorUI.ResourceTimeline.getPixelsPerHour", id);
    }

    public static ValueTask BitResourceTimelineToggleGroup(this IJSRuntime jsRuntime, string id, string resourceId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.toggleGroup", id, resourceId);
    }

    public static ValueTask BitResourceTimelineDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ResourceTimeline.dispose", id);
    }
}
