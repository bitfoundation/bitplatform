using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The surface of a <see cref="BitBhDrawer"/> - the <c>role="dialog"</c> element (<c>aria-modal</c> when modal) that
/// holds its content, named by the <see cref="BitBhDrawerTitle"/> and described by the
/// <see cref="BitBhDrawerDescription"/>. It is the focus-trap boundary and what a swipe drags. Rendered only while
/// open (and while its exit animation runs).
///
/// <para>Without a <c>class</c> or <c>style</c> it is fixed to its edge (full width for top / bottom, full height for
/// left / right) above the backdrop. Animate it from its side, and follow the swipe:</para>
/// <code>
/// .sheet { transform: translateY(var(--bit-bh-drawer-swipe-movement-y, 0px)); transition: transform 300ms; }
/// .sheet[data-swiping] { transition: none; }
/// .sheet[data-state="open"] { animation: sheet-in 300ms; }
/// .sheet[data-state="closed"] { transform: translateY(100%); }   /* the exit transitions from where a swipe left it */
/// </code>
///
/// <para><b>Styling hooks:</b> <c>data-state="open|closed"</c>, <c>data-side="top|right|bottom|left"</c>,
/// <c>data-swiping</c>, <c>--bit-bh-drawer-swipe-movement-x</c> / <c>-y</c> (px, towards the edge) and
/// <c>--bit-bh-drawer-swipe-progress</c> (0-1), <c>data-nested</c>, <c>data-nested-drawer-open</c> and
/// <c>--bit-bh-nested-drawers</c>.</para>
/// </summary>
public class BitBhDrawerPanel : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhDrawerContext? DrawerContext { get; set; }

    /// <summary>
    /// Optional element to receive focus when the drawer opens, e.g. <c>InitialFocus="() =&gt; searchInput"</c>. When
    /// omitted the first element marked <c>data-bit-bh-autofocus</c>, else the first focusable element, else the panel
    /// itself is focused. A function, read once the open drawer has rendered (see <see cref="BitBhDialogPanel.InitialFocus"/>).
    /// </summary>
    [Parameter]
    public Func<ElementReference>? InitialFocus { get; set; }

    /// <summary>Whether Escape closes the drawer. Default: true.</summary>
    [Parameter]
    public bool DismissOnEscape { get; set; } = true;

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhDrawerRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhDrawerRenderContext>? Template { get; set; }

    protected override string DefaultTag => "div";

    protected override void OnParametersSet() => DrawerContext?.RegisterPanel(InitialFocus, DismissOnEscape);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (DrawerContext is { IsPresent: false }) return;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", DrawerContext?.PanelId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (DrawerContext?.IsHidden == true)
            builder.AddAttribute(25, "hidden", true);
        builder.AddAttribute(30, "onkeydown", On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        // Keep Escape from reaching an enclosing drawer / dialog in the same DOM tree.
        builder.AddEventStopPropagationAttribute(31, "onkeydown", true);
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, new BitBhDrawerRenderContext
            {
                IsOpen = DrawerContext?.IsOpen ?? false,
                Close = () => _ = DrawerContext?.CloseAsync(),
            }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "dialog";
        if (DrawerContext?.Modal != false)
            attrs["aria-modal"] = "true";
        if (DrawerContext?.HasTitle == true)
            attrs["aria-labelledby"] = DrawerContext.TitleId;
        if (DrawerContext?.HasDescription == true)
            attrs["aria-describedby"] = DrawerContext.DescriptionId;
        // Focusable from code when it holds nothing focusable, never a tab stop.
        attrs["tabindex"] = "-1";
        SetDataState(attrs, DrawerContext?.IsOpen ?? false);
        var side = DrawerContext?.Side ?? "bottom";
        SetDataValue(attrs, "side", side);
        attrs["data-bit-bh-presence"] = "";

        // Fixed to its edge above the backdrop, unless the consumer styles it.
        if (GetAdditionalAttribute("style") is null && GetAdditionalAttribute("class") is null)
        {
            attrs["style"] = "position:fixed;z-index:51;" + side switch
            {
                "top" => "top:0;left:0;right:0;",
                "left" => "top:0;bottom:0;left:0;",
                "right" => "top:0;bottom:0;right:0;",
                _ => "bottom:0;left:0;right:0;",
            };
        }
        BitBhDrawer.AddNestingAttributes(attrs, DrawerContext);
        return attrs;
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
        => args.Key == "Escape" && DismissOnEscape && DrawerContext is not null
            ? DrawerContext.DismissAsync(BitBhDismissReason.EscapeKey)
            : Task.CompletedTask;
}
