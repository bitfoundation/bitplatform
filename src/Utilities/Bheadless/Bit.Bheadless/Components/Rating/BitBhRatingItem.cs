using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// One item (a star) of a <see cref="BitBhRating"/>: decorative (<c>aria-hidden</c>), its fill exposed as
/// <c>data-state="full|half|empty"</c> and to its <see cref="Template"/>. It stands for the value <see cref="Index"/> + 1
/// (and <see cref="Index"/> + 0.5 on its first half with <c>AllowHalf</c>).
/// </summary>
public class BitBhRatingItem : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhRating? Rating { get; set; }

    /// <summary>The item's position (0-based).</summary>
    [Parameter, EditorRequired]
    public int Index { get; set; }

    /// <summary>The content (a star icon).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Content that receives the item's state, e.g. to draw a half star.</summary>
    [Parameter]
    public RenderFragment<BitBhRatingItemContext>? Template { get; set; }

    protected override string DefaultTag => "span";

    private BitBhRatingItemState State => Rating?.StateOf(Index) ?? BitBhRatingItemState.Empty;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, BitBhContent.Pick(Template, ChildContent, new BitBhRatingItemContext(Index, State, Rating?.IsPreviewing ?? false)));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-hidden"] = "true";
        attrs["data-bit-bh-rating-item"] = Index.ToString(CultureInfo.InvariantCulture);
        SetDataValue(attrs, "index", Index.ToString(CultureInfo.InvariantCulture));
        SetDataValue(attrs, "state", State switch
        {
            BitBhRatingItemState.Full => "full",
            BitBhRatingItemState.Half => "half",
            _ => "empty",
        });
        SetDataFlag(attrs, "highlighted", Rating?.IsPreviewing == true && State != BitBhRatingItemState.Empty);
        return attrs;
    }
}
