using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// An optional expand / collapse indicator for a treegrid row (the chevron), placed in
/// one of the row's cells. Clicking it toggles the row without selecting it. It is
/// hidden from assistive tech: the row exposes <c>aria-expanded</c> and keyboard users
/// expand with ArrowRight / collapse with ArrowLeft on the row's first cell.
///
/// <code>
/// &lt;BitBhGridCell&gt;&lt;BitBhGridRowToggle class="chevron"&gt;▸&lt;/BitBhGridRowToggle&gt; @folder.Name&lt;/BitBhGridCell&gt;
/// </code>
/// </summary>
public class BitBhGridRowToggle : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhGridRow? Row { get; set; }

    [CascadingParameter]
    private BitBhGridRowState? RowState { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives whether the row is expanded. Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on it:
    /// <c>&lt;Template Context="expanded"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<bool>? Template { get; set; }

    protected override string DefaultTag => "span";

    private bool IsExpanded => RowState?.IsExpanded ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddAttribute(20, "onclick", On<MouseEventArgs>("onclick", HandleClick));
        builder.AddEventStopPropagationAttribute(21, "onclick", true);
        // The host it sits in acts on the owned click; a chevron press expands and nothing else.
        builder.AddEventStopPropagationAttribute(22, BitBhEventHandlers.Click, true);
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, BitBhContent.Pick(Template, ChildContent, IsExpanded));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-hidden"] = "true";
        if (RowState is { HasChildren: true }) SetDataState(attrs, IsExpanded);
        else attrs["data-leaf"] = "";
        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
        => Row?.Grid?.Grid is { } grid && Row.HasChildren ? grid.SetExpandedAsync(Row, !IsExpanded) : Task.CompletedTask;
}

/// <summary>The state of a <see cref="BitBhGridRow"/>, cascaded to its cells' content.</summary>
/// <param name="Key">The row key.</param>
/// <param name="Level">The hierarchy level (treegrid).</param>
/// <param name="HasChildren">Whether the row has child rows.</param>
/// <param name="IsExpanded">Whether the row is expanded.</param>
/// <param name="IsSelected">Whether the row is selected.</param>
public sealed record BitBhGridRowState(string? Key, int? Level, bool HasChildren, bool IsExpanded, bool IsSelected);
