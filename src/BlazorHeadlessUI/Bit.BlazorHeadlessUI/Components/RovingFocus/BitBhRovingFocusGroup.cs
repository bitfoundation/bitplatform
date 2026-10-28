using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A generic roving-tabindex container: the whole group is one tab stop and the
/// arrow keys move between <see cref="BitBhRovingFocusItem"/>s (and any other
/// <see cref="BitBhRovingItemBase"/>). Use it to build custom composite widgets —
/// card grids, chip lists, segmented controls.
/// </summary>
public class BitBhRovingFocusGroup : BitBhComponentBase
{
    private BitBhRovingFocusContext? _context;

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>Which arrow keys move focus. Default: horizontal.</summary>
    [Parameter]
    public BitBhRovingOrientation Orientation { get; set; } = BitBhRovingOrientation.Horizontal;

    /// <summary>Whether navigation wraps. Default: true.</summary>
    [Parameter]
    public bool Loop { get; set; } = true;

    /// <summary>Reading direction. Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>The items.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The group's state.</summary>
    protected BitBhRovingFocusContext Context => _context ??= new BitBhRovingFocusContext(Interop, BitBhId.New("bit-bh-rove"));

    protected override void OnParametersSet()
    {
        Context.Orientation = Orientation;
        Context.Loop = Loop;
        Context.Dir = Dir ?? CascadedDir;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhRovingFocusContext>>(40);
        builder.AddComponentParameter(41, "Value", Context);
        builder.AddComponentParameter(42, "IsFixed", true);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Orientation != BitBhRovingOrientation.Both)
            SetDataValue(attrs, "orientation", Orientation == BitBhRovingOrientation.Vertical ? "vertical" : "horizontal");
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        return attrs;
    }
}
