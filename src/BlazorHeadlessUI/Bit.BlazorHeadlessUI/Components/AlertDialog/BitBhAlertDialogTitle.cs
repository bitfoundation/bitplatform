using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The alert dialog's title. Renders as <c>&lt;h2&gt;</c> by default and names the
/// content through <c>aria-labelledby</c>.
/// </summary>
public class BitBhAlertDialogTitle : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhAlertDialogContext? AlertDialogContext { get; set; }

    /// <summary>Title text or rich content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "h2";

    protected override void OnInitialized() => AlertDialogContext?.Register(BitBhAlertDialogPart.Title, true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", AlertDialogContext?.TitleId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    public void Dispose() => AlertDialogContext?.Register(BitBhAlertDialogPart.Title, false);
}
