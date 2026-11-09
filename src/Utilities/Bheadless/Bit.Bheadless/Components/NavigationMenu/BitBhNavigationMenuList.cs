using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// The list of a <see cref="BitBhNavigationMenu"/>'s top-level items - a <c>&lt;ul&gt;</c> of
/// <see cref="BitBhNavigationMenuItem"/>s. Its triggers and links are all in the Tab order, and the arrow keys of
/// the menu's orientation (mirrored in RTL), Home and End move between them too.
/// </summary>
public class BitBhNavigationMenuList : BitBhComponentBase, IDisposable
{
    private BitBhNavigationMenuFocusGroup? _group;

    [CascadingParameter]
    private BitBhNavigationMenuContext? MenuContext { get; set; }

    /// <summary>The <see cref="BitBhNavigationMenuItem"/>s (and, optionally, a <see cref="BitBhNavigationMenuIndicator"/>).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "ul";

    private BitBhNavigationMenuFocusGroup Group => _group ??= new BitBhNavigationMenuFocusGroup(Interop, isList: true);

    protected override void OnInitialized() => MenuContext?.Root.RegisterList(ComponentId);

    protected override void OnParametersSet() => Group.Menu = MenuContext;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhNavigationMenuFocusGroup>>(40);
        builder.AddComponentParameter(41, "Value", Group);
        builder.AddComponentParameter(42, "IsFixed", true);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataValue(attrs, "orientation", (MenuContext?.Orientation ?? BitBhOrientation.Horizontal).ToAttribute());
        return attrs;
    }

    public void Dispose()
    {
        if (MenuContext?.ListId == ComponentId) MenuContext.Root.RegisterList(null);
    }
}
