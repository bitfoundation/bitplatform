using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The dialog's description. Renders as <c>&lt;p&gt;</c> by default and describes
/// the panel through <c>aria-describedby</c>.
/// </summary>
public class BitBhDialogDescription : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhDialogContext? DialogContext { get; set; }

    /// <summary>Description text or rich content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "p";

    protected override void OnInitialized() => DialogContext?.RegisterDescription(true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", DialogContext?.DescriptionId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    public void Dispose() => DialogContext?.RegisterDescription(false);
}
