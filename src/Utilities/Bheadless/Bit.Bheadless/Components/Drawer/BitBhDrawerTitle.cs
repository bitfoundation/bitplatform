using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// The title of a <see cref="BitBhDrawer"/>. Renders an <c>&lt;h2&gt;</c> by default and names the panel through <c>aria-labelledby</c>.
/// </summary>
public class BitBhDrawerTitle : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhDrawerContext? DrawerContext { get; set; }

    /// <summary>Title text or rich content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "h2";

    protected override void OnInitialized() => DrawerContext?.Register(BitBhDrawerPart.Title, true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", DrawerContext?.TitleId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    public void Dispose() => DrawerContext?.Register(BitBhDrawerPart.Title, false);
}
