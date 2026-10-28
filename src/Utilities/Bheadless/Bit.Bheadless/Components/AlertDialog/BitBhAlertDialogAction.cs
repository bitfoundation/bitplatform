using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The button that confirms the alert dialog's action. Invokes <see cref="OnClick"/>
/// and then closes the dialog.
/// </summary>
public class BitBhAlertDialogAction : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhAlertDialogContext? AlertDialogContext { get; set; }

    /// <summary>The action to run. The dialog closes after it completes.</summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>Button content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            On<MouseEventArgs>("onclick", HandleClick));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
            attrs["type"] = "button";
        return attrs;
    }

    private async Task HandleClick(MouseEventArgs _)
    {
        await OnClick.InvokeAsync();
        if (AlertDialogContext is not null)
            await AlertDialogContext.CloseAsync();
    }
}
