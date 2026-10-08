namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitTimeline{TItem}"/> component.
/// </summary>
/// <remarks>
/// It carries the parameters that say nothing about the item type, so one object fits every timeline under it whatever
/// each of them holds. The parameters that depend on the item type (Items, ItemTemplate, DotTemplate, NameSelectors and
/// OnItemClick) stay on the timeline itself.
/// </remarks>
public class BitTimelineParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitTimeline{TItem}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitTimeline value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// <br />
    /// The name is the bare name of the component, so a single cascade reaches every timeline whatever it is generic over.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitTimeline<object>)}";



    public string Name => ParamName;



    /// <summary>
    /// Alternates the side of the items, so each item sits on the opposite side of the line of the item before it.
    /// </summary>
    public bool? Alternate { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the timeline.
    /// </summary>
    public BitTimelineClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the timeline.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Where the dot of each item sits along its item, with the contents of the item aligned to it: at its middle
    /// (Center, the default), at its Start (the top in a vertical timeline) or at its End (the bottom in a vertical timeline).
    /// Only Center, Start and End are honoured; every other value renders the default Center.
    /// </summary>
    public BitPlacement? DotAlignment { get; set; }

    /// <summary>
    /// Renders the timeline horizontally.
    /// </summary>
    public bool? Horizontal { get; set; }

    /// <summary>
    /// The way the connecting line of the timeline is painted, which the items can override one by one.
    /// </summary>
    public BitLineStyle? LineStyle { get; set; }

    /// <summary>
    /// Where the connecting line runs: through the middle of the timeline (Center, the default), or along its Start
    /// edge (the top in a horizontal timeline) or its End edge (the bottom in a horizontal timeline).
    /// Only Center, Start and End are honoured; every other value renders the default Center.
    /// </summary>
    public BitPlacement? LinePlacement { get; set; }

    /// <summary>
    /// Renders the items in the reverse order, so the last item of the list is painted first.
    /// The reading and the focus order keep the order of the list.
    /// </summary>
    public bool? ReverseOrder { get; set; }

    /// <summary>
    /// Reverses all of the timeline items direction, so their contents swap sides of the line.
    /// </summary>
    public bool? Reversed { get; set; }

    /// <summary>
    /// The size of the timeline.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the timeline.
    /// </summary>
    public BitTimelineClassStyles? Styles { get; set; }

    /// <summary>
    /// Truncates the connecting line of the timeline at the first dot, the last dot, or both of them.
    /// </summary>
    public BitTimelineTruncateLine? TruncateLine { get; set; }

    /// <summary>
    /// The visual variant of the timeline.
    /// </summary>
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitTimeline{TItem}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitTimeline{TItem}"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitTimeline"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitTimeline"/>.
    /// </remarks>
    /// <param name="bitTimeline">
    /// The <see cref="BitTimeline{TItem}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TItem>(BitTimeline<TItem> bitTimeline) where TItem : class
    {
        if (bitTimeline is null) return;

        UpdateBaseParameters(bitTimeline);

        // This runs on every render of every timeline under the BitParams, so a value that drives the class or
        // the style of its root only resets the builder when it differs from the one it already holds: an
        // unchanged one would rebuild both strings on every render for nothing.
        if (Alternate.HasValue && bitTimeline.HasNotBeenSet(nameof(Alternate)) && bitTimeline.Alternate != Alternate)
        {
            bitTimeline.Alternate = Alternate.Value;

            bitTimeline.ClassBuilder.Reset();
        }

        if (Classes is not null && bitTimeline.HasNotBeenSet(nameof(Classes)) && ReferenceEquals(bitTimeline.Classes, Classes) is false)
        {
            bitTimeline.Classes = Classes;

            bitTimeline.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitTimeline.HasNotBeenSet(nameof(Color)) && bitTimeline.Color != Color)
        {
            bitTimeline.Color = Color.Value;

            bitTimeline.ClassBuilder.Reset();
        }

        if (DotAlignment.HasValue && bitTimeline.HasNotBeenSet(nameof(DotAlignment)) && bitTimeline.DotAlignment != DotAlignment)
        {
            bitTimeline.DotAlignment = DotAlignment.Value;

            bitTimeline.ClassBuilder.Reset();
        }

        if (Horizontal.HasValue && bitTimeline.HasNotBeenSet(nameof(Horizontal)) && bitTimeline.Horizontal != Horizontal)
        {
            bitTimeline.Horizontal = Horizontal.Value;

            bitTimeline.ClassBuilder.Reset();
        }

        if (LineStyle.HasValue && bitTimeline.HasNotBeenSet(nameof(LineStyle)) && bitTimeline.LineStyle != LineStyle)
        {
            bitTimeline.LineStyle = LineStyle.Value;

            bitTimeline.ClassBuilder.Reset();
        }

        if (LinePlacement.HasValue && bitTimeline.HasNotBeenSet(nameof(LinePlacement)) && bitTimeline.LinePlacement != LinePlacement)
        {
            bitTimeline.LinePlacement = LinePlacement.Value;

            bitTimeline.ClassBuilder.Reset();
        }

        if (ReverseOrder.HasValue && bitTimeline.HasNotBeenSet(nameof(ReverseOrder)) && bitTimeline.ReverseOrder != ReverseOrder)
        {
            bitTimeline.ReverseOrder = ReverseOrder.Value;

            bitTimeline.ClassBuilder.Reset();
        }

        if (Reversed.HasValue && bitTimeline.HasNotBeenSet(nameof(Reversed)) && bitTimeline.Reversed != Reversed)
        {
            bitTimeline.Reversed = Reversed.Value;

            bitTimeline.ClassBuilder.Reset();
        }

        if (Size.HasValue && bitTimeline.HasNotBeenSet(nameof(Size)) && bitTimeline.Size != Size)
        {
            bitTimeline.Size = Size.Value;

            bitTimeline.ClassBuilder.Reset();
        }

        if (Styles is not null && bitTimeline.HasNotBeenSet(nameof(Styles)) && ReferenceEquals(bitTimeline.Styles, Styles) is false)
        {
            bitTimeline.Styles = Styles;

            bitTimeline.StyleBuilder.Reset();
        }

        if (TruncateLine.HasValue && bitTimeline.HasNotBeenSet(nameof(TruncateLine)) && bitTimeline.TruncateLine != TruncateLine)
        {
            bitTimeline.TruncateLine = TruncateLine.Value;

            bitTimeline.ClassBuilder.Reset();
        }

        if (Variant.HasValue && bitTimeline.HasNotBeenSet(nameof(Variant)) && bitTimeline.Variant != Variant)
        {
            bitTimeline.Variant = Variant.Value;

            bitTimeline.ClassBuilder.Reset();
        }
    }
}
