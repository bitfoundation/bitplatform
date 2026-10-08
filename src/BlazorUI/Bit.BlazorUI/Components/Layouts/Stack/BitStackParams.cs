namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitStack"/> component.
/// </summary>
public class BitStackParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitStack"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitStack value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitStack)}";



    public string Name => ParamName;



    /// <summary>
    /// Gets or sets how the rows of a wrapping stack share out the room left over on the cross axis.
    /// </summary>
    public BitAlignment? AlignContent { get; set; }

    /// <summary>
    /// Gets or sets the alignment of the children of the stack on both axes at once.
    /// </summary>
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// Makes the height of the stack follow its content instead of filling its parent.
    /// </summary>
    public bool? AutoHeight { get; set; }

    /// <summary>
    /// Makes both the width and the height of the stack follow its content instead of filling its parent.
    /// </summary>
    public bool? AutoSize { get; set; }

    /// <summary>
    /// Makes the width of the stack follow its content instead of filling its parent.
    /// </summary>
    public bool? AutoWidth { get; set; }

    /// <summary>
    /// Gets or sets the size this stack starts from before the leftover space of its own container is shared out.
    /// </summary>
    public string? Basis { get; set; }

    /// <summary>
    /// Gets or sets the custom html element used for the root node. The default is "div".
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// Gives every direct child of the stack an equal share of the axis they are laid out along, whatever each of them holds.
    /// </summary>
    public bool? EqualContent { get; set; }

    /// <summary>
    /// Expands the direct children of the stack across the axis they are not laid out along.
    /// </summary>
    public bool? FillContent { get; set; }

    /// <summary>
    /// Sets the height of the stack to fit its content.
    /// </summary>
    public bool? FitHeight { get; set; }

    /// <summary>
    /// Sets the width and height of the stack to fit its content.
    /// </summary>
    public bool? FitSize { get; set; }

    /// <summary>
    /// Sets the width of the stack to fit its content.
    /// </summary>
    public bool? FitWidth { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack, using any CSS length value.
    /// </summary>
    public string? Gap { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack from the extra small breakpoint (from 0px) upwards.
    /// </summary>
    public string? GapXs { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack from the small breakpoint (from 600px) upwards.
    /// </summary>
    public string? GapSm { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack from the medium breakpoint (from 960px) upwards.
    /// </summary>
    public string? GapMd { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack from the large breakpoint (from 1280px) upwards.
    /// </summary>
    public string? GapLg { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack from the extra large breakpoint (from 1920px) upwards.
    /// </summary>
    public string? GapXl { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack from the extra extra large breakpoint (from 2560px) upwards.
    /// </summary>
    public string? GapXxl { get; set; }

    /// <summary>
    /// Gets or sets how much of the leftover space of its own container this stack takes compared to its siblings.
    /// </summary>
    public string? Grow { get; set; }

    /// <summary>
    /// Lets every direct child of the stack grow into the room the stack has left over along the axis they are laid out on.
    /// </summary>
    public bool? GrowContent { get; set; }

    /// <summary>
    /// Makes the stack take the space its own container has left over.
    /// </summary>
    public bool? Grows { get; set; }

    /// <summary>
    /// Renders the children of the stack side by side in a row instead of stacked in a column.
    /// </summary>
    public bool? Horizontal { get; set; }

    /// <summary>
    /// Gets or sets how the children of the stack are placed on the horizontal axis.
    /// </summary>
    public BitAlignment? HorizontalAlign { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the horizontal axis.
    /// </summary>
    public string? HorizontalGap { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the horizontal axis from the extra small breakpoint (from 0px) upwards.
    /// </summary>
    public string? HorizontalGapXs { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the horizontal axis from the small breakpoint (from 600px) upwards.
    /// </summary>
    public string? HorizontalGapSm { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the horizontal axis from the medium breakpoint (from 960px) upwards.
    /// </summary>
    public string? HorizontalGapMd { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the horizontal axis from the large breakpoint (from 1280px) upwards.
    /// </summary>
    public string? HorizontalGapLg { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the horizontal axis from the extra large breakpoint (from 1920px) upwards.
    /// </summary>
    public string? HorizontalGapXl { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the horizontal axis from the extra extra large breakpoint (from 2560px) upwards.
    /// </summary>
    public string? HorizontalGapXxl { get; set; }

    /// <summary>
    /// Gets or sets the direction of the stack from the extra small breakpoint (from 0px) upwards.
    /// </summary>
    public bool? HorizontalXs { get; set; }

    /// <summary>
    /// Gets or sets the direction of the stack from the small breakpoint (from 600px) upwards.
    /// </summary>
    public bool? HorizontalSm { get; set; }

    /// <summary>
    /// Gets or sets the direction of the stack from the medium breakpoint (from 960px) upwards.
    /// </summary>
    public bool? HorizontalMd { get; set; }

    /// <summary>
    /// Gets or sets the direction of the stack from the large breakpoint (from 1280px) upwards.
    /// </summary>
    public bool? HorizontalLg { get; set; }

    /// <summary>
    /// Gets or sets the direction of the stack from the extra large breakpoint (from 1920px) upwards.
    /// </summary>
    public bool? HorizontalXl { get; set; }

    /// <summary>
    /// Gets or sets the direction of the stack from the extra extra large breakpoint (from 2560px) upwards.
    /// </summary>
    public bool? HorizontalXxl { get; set; }

    /// <summary>
    /// Renders the stack as an inline box, so it flows with the text around it and is sized by its content.
    /// </summary>
    public bool? Inline { get; set; }

    /// <summary>
    /// Keeps the stack at its natural size when its own container runs out of room, instead of letting it be squeezed.
    /// </summary>
    public bool? NoShrink { get; set; }

    /// <summary>
    /// Keeps every direct child of the stack at its natural size when the stack runs out of room, instead of letting them be squeezed.
    /// </summary>
    public bool? NoShrinkContent { get; set; }

    /// <summary>
    /// Gets or sets the position of the stack among the children of its own container.
    /// </summary>
    public int? Order { get; set; }

    /// <summary>
    /// Gets or sets the inner padding of the stack, using any CSS padding value.
    /// </summary>
    public string? Padding { get; set; }

    /// <summary>
    /// Renders the children of the stack in the opposite direction.
    /// </summary>
    public bool? Reversed { get; set; }

    /// <summary>
    /// Gets or sets how the stack itself is placed across the axis of its own container.
    /// </summary>
    public BitAlignment? Self { get; set; }

    /// <summary>
    /// Gets or sets how much of what its own container is short of this stack gives up compared to its siblings.
    /// </summary>
    public string? Shrink { get; set; }

    /// <summary>
    /// Lets the stack shrink below the size of what it holds when its own container runs out of room.
    /// </summary>
    public bool? Shrinkable { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack, picked from the spacing scale of the theme.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Gets or sets how the children of the stack are placed on the vertical axis.
    /// </summary>
    public BitAlignment? VerticalAlign { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the vertical axis.
    /// </summary>
    public string? VerticalGap { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the vertical axis from the extra small breakpoint (from 0px) upwards.
    /// </summary>
    public string? VerticalGapXs { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the vertical axis from the small breakpoint (from 600px) upwards.
    /// </summary>
    public string? VerticalGapSm { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the vertical axis from the medium breakpoint (from 960px) upwards.
    /// </summary>
    public string? VerticalGapMd { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the vertical axis from the large breakpoint (from 1280px) upwards.
    /// </summary>
    public string? VerticalGapLg { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the vertical axis from the extra large breakpoint (from 1920px) upwards.
    /// </summary>
    public string? VerticalGapXl { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the vertical axis from the extra extra large breakpoint (from 2560px) upwards.
    /// </summary>
    public string? VerticalGapXxl { get; set; }

    /// <summary>
    /// Lets the children of the stack move onto more rows when they no longer fit on one.
    /// </summary>
    public bool? Wrap { get; set; }

    /// <summary>
    /// Gets or sets whether the children of the stack may move onto more rows, from the extra small breakpoint (from 0px) upwards.
    /// </summary>
    public bool? WrapXs { get; set; }

    /// <summary>
    /// Gets or sets whether the children of the stack may move onto more rows, from the small breakpoint (from 600px) upwards.
    /// </summary>
    public bool? WrapSm { get; set; }

    /// <summary>
    /// Gets or sets whether the children of the stack may move onto more rows, from the medium breakpoint (from 960px) upwards.
    /// </summary>
    public bool? WrapMd { get; set; }

    /// <summary>
    /// Gets or sets whether the children of the stack may move onto more rows, from the large breakpoint (from 1280px) upwards.
    /// </summary>
    public bool? WrapLg { get; set; }

    /// <summary>
    /// Gets or sets whether the children of the stack may move onto more rows, from the extra large breakpoint (from 1920px) upwards.
    /// </summary>
    public bool? WrapXl { get; set; }

    /// <summary>
    /// Gets or sets whether the children of the stack may move onto more rows, from the extra extra large breakpoint (from 2560px) upwards.
    /// </summary>
    public bool? WrapXxl { get; set; }

    /// <summary>
    /// Lets the children of the stack move onto more rows when they no longer fit on one, with the rows stacked in the opposite direction.
    /// </summary>
    public bool? WrapReverse { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitStack"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitStack"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitStack"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitStack"/>.
    /// </remarks>
    /// <param name="bitStack">
    /// The <see cref="BitStack"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitStack bitStack)
    {
        if (bitStack is null) return;

        UpdateBaseParameters(bitStack);

        if (AlignContent.HasValue)
        {
            bitStack.TakeFromCascade(nameof(AlignContent), AlignContent.Value, static s => s.AlignContent, static (s, v) => s.AlignContent = v);
        }

        if (Alignment.HasValue)
        {
            bitStack.TakeFromCascade(nameof(Alignment), Alignment.Value, static s => s.Alignment, static (s, v) => s.Alignment = v);
        }

        if (AutoHeight.HasValue)
        {
            bitStack.TakeFromCascade(nameof(AutoHeight), AutoHeight.Value, static s => s.AutoHeight, static (s, v) => s.AutoHeight = v);
        }

        if (AutoSize.HasValue)
        {
            bitStack.TakeFromCascade(nameof(AutoSize), AutoSize.Value, static s => s.AutoSize, static (s, v) => s.AutoSize = v);
        }

        if (AutoWidth.HasValue)
        {
            bitStack.TakeFromCascade(nameof(AutoWidth), AutoWidth.Value, static s => s.AutoWidth, static (s, v) => s.AutoWidth = v);
        }

        if (Basis.HasValue())
        {
            bitStack.TakeFromCascade(nameof(Basis), Basis, static s => s.Basis, static (s, v) => s.Basis = v);
        }

        if (Element.HasValue())
        {
            bitStack.TakeFromCascade(nameof(Element), Element, static s => s.Element, static (s, v) => s.Element = v);
        }

        if (EqualContent.HasValue)
        {
            bitStack.TakeFromCascade(nameof(EqualContent), EqualContent.Value, static s => s.EqualContent, static (s, v) => s.EqualContent = v);
        }

        if (FillContent.HasValue)
        {
            bitStack.TakeFromCascade(nameof(FillContent), FillContent.Value, static s => s.FillContent, static (s, v) => s.FillContent = v);
        }

        if (FitHeight.HasValue)
        {
            bitStack.TakeFromCascade(nameof(FitHeight), FitHeight.Value, static s => s.FitHeight, static (s, v) => s.FitHeight = v);
        }

        if (FitSize.HasValue)
        {
            bitStack.TakeFromCascade(nameof(FitSize), FitSize.Value, static s => s.FitSize, static (s, v) => s.FitSize = v);
        }

        if (FitWidth.HasValue)
        {
            bitStack.TakeFromCascade(nameof(FitWidth), FitWidth.Value, static s => s.FitWidth, static (s, v) => s.FitWidth = v);
        }

        if (Gap.HasValue())
        {
            bitStack.TakeFromCascade(nameof(Gap), Gap, static s => s.Gap, static (s, v) => s.Gap = v);
        }

        if (GapXs.HasValue())
        {
            bitStack.TakeFromCascade(nameof(GapXs), GapXs, static s => s.GapXs, static (s, v) => s.GapXs = v);
        }

        if (GapSm.HasValue())
        {
            bitStack.TakeFromCascade(nameof(GapSm), GapSm, static s => s.GapSm, static (s, v) => s.GapSm = v);
        }

        if (GapMd.HasValue())
        {
            bitStack.TakeFromCascade(nameof(GapMd), GapMd, static s => s.GapMd, static (s, v) => s.GapMd = v);
        }

        if (GapLg.HasValue())
        {
            bitStack.TakeFromCascade(nameof(GapLg), GapLg, static s => s.GapLg, static (s, v) => s.GapLg = v);
        }

        if (GapXl.HasValue())
        {
            bitStack.TakeFromCascade(nameof(GapXl), GapXl, static s => s.GapXl, static (s, v) => s.GapXl = v);
        }

        if (GapXxl.HasValue())
        {
            bitStack.TakeFromCascade(nameof(GapXxl), GapXxl, static s => s.GapXxl, static (s, v) => s.GapXxl = v);
        }

        if (Grow.HasValue())
        {
            bitStack.TakeFromCascade(nameof(Grow), Grow, static s => s.Grow, static (s, v) => s.Grow = v);
        }

        if (GrowContent.HasValue)
        {
            bitStack.TakeFromCascade(nameof(GrowContent), GrowContent.Value, static s => s.GrowContent, static (s, v) => s.GrowContent = v);
        }

        if (Grows.HasValue)
        {
            bitStack.TakeFromCascade(nameof(Grows), Grows.Value, static s => s.Grows, static (s, v) => s.Grows = v);
        }

        if (Horizontal.HasValue)
        {
            bitStack.TakeFromCascade(nameof(Horizontal), Horizontal.Value, static s => s.Horizontal, static (s, v) => s.Horizontal = v);
        }

        if (HorizontalAlign.HasValue)
        {
            bitStack.TakeFromCascade(nameof(HorizontalAlign), HorizontalAlign.Value, static s => s.HorizontalAlign, static (s, v) => s.HorizontalAlign = v);
        }

        if (HorizontalGap.HasValue())
        {
            bitStack.TakeFromCascade(nameof(HorizontalGap), HorizontalGap, static s => s.HorizontalGap, static (s, v) => s.HorizontalGap = v);
        }

        if (HorizontalGapXs.HasValue())
        {
            bitStack.TakeFromCascade(nameof(HorizontalGapXs), HorizontalGapXs, static s => s.HorizontalGapXs, static (s, v) => s.HorizontalGapXs = v);
        }

        if (HorizontalGapSm.HasValue())
        {
            bitStack.TakeFromCascade(nameof(HorizontalGapSm), HorizontalGapSm, static s => s.HorizontalGapSm, static (s, v) => s.HorizontalGapSm = v);
        }

        if (HorizontalGapMd.HasValue())
        {
            bitStack.TakeFromCascade(nameof(HorizontalGapMd), HorizontalGapMd, static s => s.HorizontalGapMd, static (s, v) => s.HorizontalGapMd = v);
        }

        if (HorizontalGapLg.HasValue())
        {
            bitStack.TakeFromCascade(nameof(HorizontalGapLg), HorizontalGapLg, static s => s.HorizontalGapLg, static (s, v) => s.HorizontalGapLg = v);
        }

        if (HorizontalGapXl.HasValue())
        {
            bitStack.TakeFromCascade(nameof(HorizontalGapXl), HorizontalGapXl, static s => s.HorizontalGapXl, static (s, v) => s.HorizontalGapXl = v);
        }

        if (HorizontalGapXxl.HasValue())
        {
            bitStack.TakeFromCascade(nameof(HorizontalGapXxl), HorizontalGapXxl, static s => s.HorizontalGapXxl, static (s, v) => s.HorizontalGapXxl = v);
        }

        if (HorizontalXs.HasValue)
        {
            bitStack.TakeFromCascade(nameof(HorizontalXs), HorizontalXs.Value, static s => s.HorizontalXs, static (s, v) => s.HorizontalXs = v);
        }

        if (HorizontalSm.HasValue)
        {
            bitStack.TakeFromCascade(nameof(HorizontalSm), HorizontalSm.Value, static s => s.HorizontalSm, static (s, v) => s.HorizontalSm = v);
        }

        if (HorizontalMd.HasValue)
        {
            bitStack.TakeFromCascade(nameof(HorizontalMd), HorizontalMd.Value, static s => s.HorizontalMd, static (s, v) => s.HorizontalMd = v);
        }

        if (HorizontalLg.HasValue)
        {
            bitStack.TakeFromCascade(nameof(HorizontalLg), HorizontalLg.Value, static s => s.HorizontalLg, static (s, v) => s.HorizontalLg = v);
        }

        if (HorizontalXl.HasValue)
        {
            bitStack.TakeFromCascade(nameof(HorizontalXl), HorizontalXl.Value, static s => s.HorizontalXl, static (s, v) => s.HorizontalXl = v);
        }

        if (HorizontalXxl.HasValue)
        {
            bitStack.TakeFromCascade(nameof(HorizontalXxl), HorizontalXxl.Value, static s => s.HorizontalXxl, static (s, v) => s.HorizontalXxl = v);
        }

        if (Inline.HasValue)
        {
            bitStack.TakeFromCascade(nameof(Inline), Inline.Value, static s => s.Inline, static (s, v) => s.Inline = v);
        }

        if (NoShrink.HasValue)
        {
            bitStack.TakeFromCascade(nameof(NoShrink), NoShrink.Value, static s => s.NoShrink, static (s, v) => s.NoShrink = v);
        }

        if (NoShrinkContent.HasValue)
        {
            bitStack.TakeFromCascade(nameof(NoShrinkContent), NoShrinkContent.Value, static s => s.NoShrinkContent, static (s, v) => s.NoShrinkContent = v);
        }

        if (Order.HasValue)
        {
            bitStack.TakeFromCascade(nameof(Order), Order.Value, static s => s.Order, static (s, v) => s.Order = v);
        }

        if (Padding.HasValue())
        {
            bitStack.TakeFromCascade(nameof(Padding), Padding, static s => s.Padding, static (s, v) => s.Padding = v);
        }

        if (Reversed.HasValue)
        {
            bitStack.TakeFromCascade(nameof(Reversed), Reversed.Value, static s => s.Reversed, static (s, v) => s.Reversed = v);
        }

        if (Self.HasValue)
        {
            bitStack.TakeFromCascade(nameof(Self), Self.Value, static s => s.Self, static (s, v) => s.Self = v);
        }

        if (Shrink.HasValue())
        {
            bitStack.TakeFromCascade(nameof(Shrink), Shrink, static s => s.Shrink, static (s, v) => s.Shrink = v);
        }

        if (Shrinkable.HasValue)
        {
            bitStack.TakeFromCascade(nameof(Shrinkable), Shrinkable.Value, static s => s.Shrinkable, static (s, v) => s.Shrinkable = v);
        }

        if (Size.HasValue)
        {
            bitStack.TakeFromCascade(nameof(Size), Size.Value, static s => s.Size, static (s, v) => s.Size = v);
        }

        if (VerticalAlign.HasValue)
        {
            bitStack.TakeFromCascade(nameof(VerticalAlign), VerticalAlign.Value, static s => s.VerticalAlign, static (s, v) => s.VerticalAlign = v);
        }

        if (VerticalGap.HasValue())
        {
            bitStack.TakeFromCascade(nameof(VerticalGap), VerticalGap, static s => s.VerticalGap, static (s, v) => s.VerticalGap = v);
        }

        if (VerticalGapXs.HasValue())
        {
            bitStack.TakeFromCascade(nameof(VerticalGapXs), VerticalGapXs, static s => s.VerticalGapXs, static (s, v) => s.VerticalGapXs = v);
        }

        if (VerticalGapSm.HasValue())
        {
            bitStack.TakeFromCascade(nameof(VerticalGapSm), VerticalGapSm, static s => s.VerticalGapSm, static (s, v) => s.VerticalGapSm = v);
        }

        if (VerticalGapMd.HasValue())
        {
            bitStack.TakeFromCascade(nameof(VerticalGapMd), VerticalGapMd, static s => s.VerticalGapMd, static (s, v) => s.VerticalGapMd = v);
        }

        if (VerticalGapLg.HasValue())
        {
            bitStack.TakeFromCascade(nameof(VerticalGapLg), VerticalGapLg, static s => s.VerticalGapLg, static (s, v) => s.VerticalGapLg = v);
        }

        if (VerticalGapXl.HasValue())
        {
            bitStack.TakeFromCascade(nameof(VerticalGapXl), VerticalGapXl, static s => s.VerticalGapXl, static (s, v) => s.VerticalGapXl = v);
        }

        if (VerticalGapXxl.HasValue())
        {
            bitStack.TakeFromCascade(nameof(VerticalGapXxl), VerticalGapXxl, static s => s.VerticalGapXxl, static (s, v) => s.VerticalGapXxl = v);
        }

        if (Wrap.HasValue)
        {
            bitStack.TakeFromCascade(nameof(Wrap), Wrap.Value, static s => s.Wrap, static (s, v) => s.Wrap = v);
        }

        if (WrapXs.HasValue)
        {
            bitStack.TakeFromCascade(nameof(WrapXs), WrapXs.Value, static s => s.WrapXs, static (s, v) => s.WrapXs = v);
        }

        if (WrapSm.HasValue)
        {
            bitStack.TakeFromCascade(nameof(WrapSm), WrapSm.Value, static s => s.WrapSm, static (s, v) => s.WrapSm = v);
        }

        if (WrapMd.HasValue)
        {
            bitStack.TakeFromCascade(nameof(WrapMd), WrapMd.Value, static s => s.WrapMd, static (s, v) => s.WrapMd = v);
        }

        if (WrapLg.HasValue)
        {
            bitStack.TakeFromCascade(nameof(WrapLg), WrapLg.Value, static s => s.WrapLg, static (s, v) => s.WrapLg = v);
        }

        if (WrapXl.HasValue)
        {
            bitStack.TakeFromCascade(nameof(WrapXl), WrapXl.Value, static s => s.WrapXl, static (s, v) => s.WrapXl = v);
        }

        if (WrapXxl.HasValue)
        {
            bitStack.TakeFromCascade(nameof(WrapXxl), WrapXxl.Value, static s => s.WrapXxl, static (s, v) => s.WrapXxl = v);
        }

        if (WrapReverse.HasValue)
        {
            bitStack.TakeFromCascade(nameof(WrapReverse), WrapReverse.Value, static s => s.WrapReverse, static (s, v) => s.WrapReverse = v);
        }
    }
}
