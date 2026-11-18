using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A non-interactive heading inside a menu. Inside a <see cref="BitBhMenuGroup"/> it
/// labels the group; elsewhere it is presentational text that keyboard
/// navigation skips.
/// </summary>
public class BitBhMenuLabel : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhMenuGroupContext? Group { get; set; }

    /// <summary>The label text.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void OnInitialized() => Group?.RegisterLabel(true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Group?.LabelId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        // A label outside a group is presentational inside role="menu".
        if (Group is null)
            attrs["role"] = "presentation";
        return attrs;
    }

    public void Dispose() => Group?.RegisterLabel(false);
}
