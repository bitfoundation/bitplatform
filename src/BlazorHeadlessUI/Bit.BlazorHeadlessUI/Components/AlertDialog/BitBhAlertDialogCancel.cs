using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The button that dismisses the alert dialog without taking the action. It
/// receives focus when the dialog opens — the least destructive choice.
/// </summary>
public class BitBhAlertDialogCancel : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhAlertDialogContext? AlertDialogContext { get; set; }

    /// <summary>Optional callback invoked before the dialog closes.</summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>Button content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void OnInitialized() => AlertDialogContext?.Register(BitBhAlertDialogPart.Cancel, true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", AlertDialogContext?.CancelId ?? ComponentId);
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

    public void Dispose() => AlertDialogContext?.Register(BitBhAlertDialogPart.Cancel, false);
}
