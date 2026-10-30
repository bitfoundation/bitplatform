using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The alert dialog's description. Renders as <c>&lt;p&gt;</c> by default and
/// describes the content through <c>aria-describedby</c>.
/// </summary>
public class BitBhAlertDialogDescription : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhAlertDialogContext? AlertDialogContext { get; set; }

    /// <summary>Description text or rich content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "p";

    protected override void OnInitialized() => AlertDialogContext?.Register(BitBhAlertDialogPart.Description, true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", AlertDialogContext?.DescriptionId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    public void Dispose() => AlertDialogContext?.Register(BitBhAlertDialogPart.Description, false);
}
