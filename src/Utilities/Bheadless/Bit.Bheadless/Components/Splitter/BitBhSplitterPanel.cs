using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A resizable panel of a <see cref="BitBhSplitter"/>. Its size, in percent of the space the handles leave, is
/// applied inline (<c>flex: &lt;size&gt; 1 0px; overflow: hidden</c> - pass your own <c>overflow</c> in <c>style</c>
/// for a scrolling panel). Sizes are bounded by <see cref="MinSize"/> / <see cref="MaxSize"/>; a
/// <see cref="Collapsible"/> panel also collapses to <see cref="CollapsedSize"/>. A panel collapsed to nothing is
/// <c>inert</c>, so the keyboard can't reach content nobody sees.
///
/// <para>
/// <b>Styling hooks:</b> <c>data-orientation</c>, <c>data-state="expanded|collapsed"</c>, <c>data-collapsed</c>, and
/// the variable <c>--bit-bh-splitter-panel-size</c> (the size in percent, unitless).
/// </para>
/// </summary>
public class BitBhSplitterPanel : BitBhComponentBase, IBitBhCollectionItem, IDisposable
{
    [CascadingParameter]
    private BitBhSplitter? Splitter { get; set; }

    /// <summary>
    /// Identifies the panel in a saved layout (<see cref="BitBhSplitter.AutoSaveId"/>); its <c>Id</c> or its position
    /// otherwise. Give conditionally rendered panels one, so each set of panels keeps its own layout.
    /// </summary>
    [Parameter]
    public string? Key { get; set; }

    /// <summary>The initial size in percent when the splitter has no <c>Sizes</c> / <c>DefaultSizes</c>.</summary>
    [Parameter]
    public double? DefaultSize { get; set; }

    /// <summary>The smallest size in percent. Default: 0.</summary>
    [Parameter]
    public double MinSize { get; set; }

    /// <summary>The largest size in percent. Default: 100.</summary>
    [Parameter]
    public double MaxSize { get; set; } = 100;

    /// <summary>
    /// Whether the panel collapses (to <see cref="CollapsedSize"/>) when it is dragged past half its
    /// <see cref="MinSize"/>, sent to its smallest size with Home, or toggled with Enter on a handle next to it.
    /// </summary>
    [Parameter]
    public bool Collapsible { get; set; }

    /// <summary>The size in percent of the collapsed panel (an icon rail, say). Default: 0.</summary>
    [Parameter]
    public double CollapsedSize { get; set; }

    /// <summary>
    /// The panel's position among the splitter's panels, for panels rendered conditionally: panels are ordered by
    /// it, then by document order.
    /// </summary>
    [Parameter]
    public int? Order { get; set; }

    /// <summary>Fires when the panel collapses (true) or expands (false).</summary>
    [Parameter]
    public EventCallback<bool> OnCollapsedChange { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhSplitterPanelRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhSplitterPanelRenderContext>? Template { get; set; }

    protected override string DefaultTag => "div";

    /// <inheritdoc />
    public string ItemId => ComponentId;

    /// <inheritdoc />
    public bool IsItemDisabled => false;

    /// <inheritdoc />
    public string? ItemText => null;

    /// <summary>The current size in percent.</summary>
    public double Size => Splitter?.SizeOf(this) ?? 100;

    /// <summary>Whether the panel is collapsed.</summary>
    public bool IsCollapsed => IsCollapsedAt(Size);

    /// <summary>The size an expanding panel goes back to: the one it had when it collapsed.</summary>
    internal double? RestoreSize { get; set; }

    internal double EffectiveMin => Math.Clamp(Finite(MinSize, 0), 0, 100);

    internal double EffectiveMax => Math.Clamp(Finite(MaxSize, 100), EffectiveMin, 100);

    internal double EffectiveCollapsedSize => Math.Clamp(Finite(CollapsedSize, 0), 0, EffectiveMin);

    private static double Finite(double value, double fallback) => double.IsFinite(value) ? value : fallback;

    internal bool IsCollapsedAt(double size) => Collapsible && size <= EffectiveCollapsedSize + 0.0001;

    /// <summary>Collapses the panel (when <see cref="Collapsible"/>).</summary>
    public Task CollapseAsync() => Splitter?.SetCollapsedAsync(this, true) ?? Task.CompletedTask;

    /// <summary>Expands a collapsed panel to the size it had, or its <see cref="DefaultSize"/> / <see cref="MinSize"/>.</summary>
    public Task ExpandAsync() => Splitter?.SetCollapsedAsync(this, false) ?? Task.CompletedTask;

    /// <summary>Resizes the panel to <paramref name="size"/> percent, as moving the handle after it (or, for the last panel, before it) would.</summary>
    public Task ResizeAsync(double size) => Splitter?.ResizePanelAsync(this, size) ?? Task.CompletedTask;

    protected override void OnInitialized() => Splitter?.PanelCollection.Register(this);

    /// <inheritdoc />
    public void Dispose() => Splitter?.PanelCollection.Unregister(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var size = Size;
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, BitBhContent.Pick(Template, ChildContent, new BitBhSplitterPanelRenderContext { Size = size, IsCollapsed = IsCollapsedAt(size) }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Splitter is null) return attrs;
        var size = Size;
        var collapsed = IsCollapsedAt(size);
        var grow = size.ToString("0.####", CultureInfo.InvariantCulture);
        attrs["data-bit-bh-item"] = Splitter.PanelCollection.Key;
        attrs["data-bit-bh-splitter-panel"] = Splitter.RootId;
        SetDataValue(attrs, "orientation", Splitter.SplitterOrientation.ToAttribute());
        SetDataState(attrs, collapsed, "collapsed", "expanded");
        SetDataFlag(attrs, "collapsed", collapsed);
        // Content squeezed to nothing is out of sight: it leaves the tab order and the accessibility tree.
        if (size <= 0.0001) attrs["inert"] = true;
        // Functional: the panels share the space the handles leave by their flex-grow (the sizes sum to 100).
        attrs["style"] = $"flex:{grow} 1 0px;overflow:hidden;--bit-bh-splitter-panel-size:{grow}";
        return attrs;
    }
}

/// <summary>Render-fragment context exposed by <see cref="BitBhSplitterPanel"/>.</summary>
public sealed record BitBhSplitterPanelRenderContext
{
    /// <summary>The panel's size in percent.</summary>
    public required double Size { get; init; }

    /// <summary>Whether the panel is collapsed.</summary>
    public required bool IsCollapsed { get; init; }
}
