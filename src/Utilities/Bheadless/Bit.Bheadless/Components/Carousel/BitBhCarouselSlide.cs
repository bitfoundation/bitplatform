using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// One slide of a <see cref="BitBhCarousel"/> (<c>role="group"</c>, <c>aria-roledescription="slide"</c>, named "2 of 5").
/// Slides out of view are <c>inert</c> and <c>aria-hidden</c>; <c>data-state="active|inactive"</c>, <c>data-index</c>.
/// </summary>
public class BitBhCarouselSlide : BitBhComponentBase, IBitBhCollectionItem, IDisposable
{
    [CascadingParameter]
    private BitBhCarouselContext? Context { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private BitBhCarousel? Owner => Context?.Owner;

    string IBitBhCollectionItem.ItemId => ComponentId;
    bool IBitBhCollectionItem.IsItemDisabled => false;
    string? IBitBhCollectionItem.ItemText => null;

    protected override void OnInitialized() => Owner?.Register(this);

    public void Dispose() => Owner?.Unregister(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Owner is not { } owner) return attrs;
        var index = owner.IndexOf(this);
        var inView = owner.IsInView(index);
        attrs["role"] = "group";
        attrs["aria-roledescription"] = "slide";
        attrs["data-bit-bh-item"] = owner.SlideKey;
        attrs["aria-label"] = Strings.CarouselSlide(index, owner.Count);
        if (!inView)
        {
            attrs["aria-hidden"] = "true";
            attrs["inert"] = true;
        }
        SetDataValue(attrs, "index", index.ToString(CultureInfo.InvariantCulture));
        SetDataState(attrs, inView, "active", "inactive");
        return attrs;
    }
}
