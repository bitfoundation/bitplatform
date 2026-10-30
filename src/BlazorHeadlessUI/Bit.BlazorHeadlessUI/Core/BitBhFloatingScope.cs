using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Cascaded by every floating surface (dialog, popover, menu, listbox …) to its
/// content. Nested surfaces use it to register as child layers — so a popover
/// opened inside a modal dialog may receive focus and clicks without dismissing
/// the dialog — and arrows use it to register themselves for positioning.
/// </summary>
public sealed class BitBhFloatingScope
{
    private readonly Func<int> _handle;

    internal BitBhFloatingScope(Func<int> handle)
    {
        _handle = handle;
    }

    /// <summary>The JS handle of the surface (0 while closed).</summary>
    public int Handle => _handle();

    /// <summary>The id of the registered arrow element, if any.</summary>
    public string? ArrowId { get; private set; }

    /// <summary>The id of the surface's content element.</summary>
    public string? Owner { get; internal set; }

    internal void RegisterArrow(string? id) => ArrowId = id;

    /// <summary>Wraps <paramref name="content"/> in a fixed cascading value of <paramref name="scope"/>.</summary>
    internal static void Render(RenderTreeBuilder builder, int sequence, BitBhFloatingScope scope, RenderFragment? content)
    {
        builder.OpenComponent<CascadingValue<BitBhFloatingScope>>(sequence);
        builder.AddComponentParameter(sequence + 1, "Value", scope);
        builder.AddComponentParameter(sequence + 2, "IsFixed", true);
        builder.AddComponentParameter(sequence + 3, "ChildContent", content);
        builder.CloseComponent();
    }
}

/// <summary>
/// An arrow that points from floating content (popover, tooltip, hover card,
/// menu) to its trigger. Place it inside the content; the positioning engine
/// moves and rotates it to follow the resolved side (<c>data-side</c>).
///
/// <para>The default rendering is a small SVG triangle pointing down — style its
/// <c>fill</c> with CSS, or provide your own markup as child content.</para>
/// </summary>
public class BitBhArrow : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhFloatingScope? Scope { get; set; }

    /// <summary>Width of the default SVG arrow in pixels. Default: 10.</summary>
    [Parameter]
    public int Width { get; set; } = 10;

    /// <summary>Height of the default SVG arrow in pixels. Default: 5.</summary>
    [Parameter]
    public int Height { get; set; } = 5;

    /// <summary>Optional custom content replacing the default SVG triangle.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    protected override void OnInitialized() => Scope?.RegisterArrow(ComponentId);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);

        if (ChildContent is not null)
        {
            builder.AddContent(40, ChildContent);
        }
        else
        {
            builder.OpenElement(50, "svg");
            builder.AddAttribute(51, "xmlns", "http://www.w3.org/2000/svg");
            builder.AddAttribute(52, "width", Width.ToString(CultureInfo.InvariantCulture));
            builder.AddAttribute(53, "height", Height.ToString(CultureInfo.InvariantCulture));
            builder.AddAttribute(54, "viewBox", FormattableString.Invariant($"0 0 {Width} {Height}"));
            builder.AddAttribute(55, "preserveAspectRatio", "none");
            builder.AddAttribute(56, "focusable", "false");
            builder.OpenElement(60, "polygon");
            builder.AddAttribute(61, "points", FormattableString.Invariant($"0,0 {Width},0 {Width / 2.0},{Height}"));
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-hidden"] = "true";
        attrs["data-bit-bh-arrow"] = "";
        return attrs;
    }

    public void Dispose()
    {
        if (Scope?.ArrowId == ComponentId)
            Scope.RegisterArrow(null);
    }
}
