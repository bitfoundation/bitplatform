namespace Bit.BlazorUI;

/// <summary>
/// A render boundary of the <see cref="BitResourceTimeline"/>: renders its content again only when the timeline changed
/// the key it is given.
/// </summary>
/// <remarks>
/// The timeline renders itself for state no row of the resource column depends on - the bar the pointer hovers for the
/// TooltipTemplate, the loading overlay, the layout of the corner - and a render of the timeline runs every row's
/// ResourceTemplate again. Behind this gate the rows render only when the timeline says they may have changed: on
/// parameters from its parent (a template, a class, a text or the disabled state can have changed there), on rows the
/// engine reports, and on a new layout of the column.
/// </remarks>
internal sealed class _BitResourceTimelineRenderGate : ComponentBase
{
    private long? _renderedKey;

    /// <summary>The version of what the content depends on; the content renders again when it differs.</summary>
    [Parameter] public long RenderKey { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override bool ShouldRender() => _renderedKey != RenderKey;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _renderedKey = RenderKey;

        builder.AddContent(0, ChildContent);
    }
}
