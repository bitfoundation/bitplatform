using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The dialog's title. Renders as <c>&lt;h2&gt;</c> by default and names the
/// panel through <c>aria-labelledby</c>.
/// </summary>
public class BitBhDialogTitle : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhDialogContext? DialogContext { get; set; }

    /// <summary>Title text or rich content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "h2";

    protected override void OnInitialized() => DialogContext?.RegisterTitle(true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", DialogContext?.TitleId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    public void Dispose() => DialogContext?.RegisterTitle(false);
}
