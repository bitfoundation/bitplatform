using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A horizontal application menu bar (<c>role="menubar"</c>) implementing the
/// WAI-ARIA Menubar pattern. Place <see cref="BitBhMenu"/> components inside it; their
/// <see cref="BitBhMenuButton"/>s become menubar items with a single tab stop.
///
/// <para><b>Keyboard:</b> ArrowLeft / ArrowRight move between menus (opening the
/// neighbour when a menu is already open), Home / End jump to the ends, Enter /
/// Space / ArrowDown open the focused menu, ArrowRight / ArrowLeft inside an open
/// menu move to the adjacent one (unless the item opens a submenu).</para>
/// <para><b>Pointer:</b> once a menu is open, hovering another trigger switches to it. Menus with
/// <see cref="BitBhMenu.OpenOnHover"/> open on hover even when none is (menubar-style navigation); one the
/// pointer opened hands over to the next without moving the focus.</para>
///
/// <code>
/// &lt;BitBhMenubar&gt;
///     &lt;BitBhMenu&gt;
///         &lt;BitBhMenuButton&gt;File&lt;/BitBhMenuButton&gt;
///         &lt;BitBhMenuItems Anchor="@("bottom start")"&gt;…&lt;/BitBhMenuItems&gt;
///     &lt;/BitBhMenu&gt;
///     &lt;BitBhMenu&gt;
///         &lt;BitBhMenuButton&gt;Edit&lt;/BitBhMenuButton&gt;
///         &lt;BitBhMenuItems Anchor="@("bottom start")"&gt;…&lt;/BitBhMenuItems&gt;
///     &lt;/BitBhMenu&gt;
/// &lt;/BitBhMenubar&gt;
/// </code>
/// </summary>
public class BitBhMenubar : BitBhComponentBase
{
    private BitBhMenubarContext? _context;

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>Whether arrow keys wrap from the last menu to the first. Default: true.</summary>
    [Parameter]
    public bool Loop { get; set; } = true;

    /// <summary>Reading direction. Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>The menus.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void OnParametersSet()
    {
        _context ??= new BitBhMenubarContext(Interop);
        _context.Loop = Loop;
        _context.Dir = Dir ?? CascadedDir;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhMenubarContext>>(40);
        builder.AddComponentParameter(41, "Value", _context);
        builder.AddComponentParameter(42, "IsFixed", true);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "menubar";
        attrs["aria-orientation"] = "horizontal";
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        return attrs;
    }
}
