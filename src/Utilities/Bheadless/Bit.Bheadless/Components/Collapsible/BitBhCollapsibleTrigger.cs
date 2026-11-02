using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The button that toggles a <see cref="BitBhCollapsible"/>. Renders a native
/// <c>&lt;button&gt;</c> with <c>aria-expanded</c> and <c>aria-controls</c>.
/// </summary>
public class BitBhCollapsibleTrigger : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhCollapsibleContext? CollapsibleContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhCollapsibleRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhCollapsibleRenderContext>? Template { get; set; }

    protected override string DefaultTag => "button";

    private bool IsOpen => CollapsibleContext?.IsOpen ?? false;
    private bool IsDisabled => CollapsibleContext?.Disabled ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", CollapsibleContext?.TriggerId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            On<MouseEventArgs>("onclick", HandleClick));
        if (!Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
            builder.AddAttribute(31, "onkeydown", On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, new BitBhCollapsibleRenderContext
            {
                IsOpen = IsOpen,
                Close = CollapsibleContext?.Close ?? (() => { }),
            }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        BitBhToggleAttributes.ApplyTrigger(attrs, Tag, IsOpen, IsDisabled, CollapsibleContext?.ContentId);
        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", IsDisabled);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
        => IsDisabled ? Task.CompletedTask : CollapsibleContext?.ToggleAsync() ?? Task.CompletedTask;

    private Task HandleKeyDown(KeyboardEventArgs e)
        => e.Key is "Enter" or " " ? HandleClick(new MouseEventArgs()) : Task.CompletedTask;
}
