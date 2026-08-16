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
    /// Lets the children of the stack move onto more rows when they no longer fit on one.
    /// </summary>
    public bool? Wrap { get; set; }

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

        if (AlignContent.HasValue && bitStack.HasNotBeenSet(nameof(AlignContent)))
        {
            bitStack.AlignContent = AlignContent.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (Alignment.HasValue && bitStack.HasNotBeenSet(nameof(Alignment)))
        {
            bitStack.Alignment = Alignment.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (AutoHeight.HasValue && bitStack.HasNotBeenSet(nameof(AutoHeight)))
        {
            bitStack.AutoHeight = AutoHeight.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (AutoSize.HasValue && bitStack.HasNotBeenSet(nameof(AutoSize)))
        {
            bitStack.AutoSize = AutoSize.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (AutoWidth.HasValue && bitStack.HasNotBeenSet(nameof(AutoWidth)))
        {
            bitStack.AutoWidth = AutoWidth.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (Basis.HasValue() && bitStack.HasNotBeenSet(nameof(Basis)))
        {
            bitStack.Basis = Basis;

            bitStack.StyleBuilder.Reset();
        }

        if (Element.HasValue() && bitStack.HasNotBeenSet(nameof(Element)))
        {
            bitStack.Element = Element;
        }

        if (EqualContent.HasValue && bitStack.HasNotBeenSet(nameof(EqualContent)))
        {
            bitStack.EqualContent = EqualContent.Value;

            bitStack.ClassBuilder.Reset();
        }

        if (FillContent.HasValue && bitStack.HasNotBeenSet(nameof(FillContent)))
        {
            bitStack.FillContent = FillContent.Value;

            bitStack.ClassBuilder.Reset();
        }

        if (FitHeight.HasValue && bitStack.HasNotBeenSet(nameof(FitHeight)))
        {
            bitStack.FitHeight = FitHeight.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (FitSize.HasValue && bitStack.HasNotBeenSet(nameof(FitSize)))
        {
            bitStack.FitSize = FitSize.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (FitWidth.HasValue && bitStack.HasNotBeenSet(nameof(FitWidth)))
        {
            bitStack.FitWidth = FitWidth.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (Gap.HasValue() && bitStack.HasNotBeenSet(nameof(Gap)))
        {
            bitStack.Gap = Gap;

            bitStack.StyleBuilder.Reset();
        }

        if (GapXs.HasValue() && bitStack.HasNotBeenSet(nameof(GapXs)))
        {
            bitStack.GapXs = GapXs;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (GapSm.HasValue() && bitStack.HasNotBeenSet(nameof(GapSm)))
        {
            bitStack.GapSm = GapSm;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (GapMd.HasValue() && bitStack.HasNotBeenSet(nameof(GapMd)))
        {
            bitStack.GapMd = GapMd;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (GapLg.HasValue() && bitStack.HasNotBeenSet(nameof(GapLg)))
        {
            bitStack.GapLg = GapLg;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (GapXl.HasValue() && bitStack.HasNotBeenSet(nameof(GapXl)))
        {
            bitStack.GapXl = GapXl;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (GapXxl.HasValue() && bitStack.HasNotBeenSet(nameof(GapXxl)))
        {
            bitStack.GapXxl = GapXxl;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (Grow.HasValue() && bitStack.HasNotBeenSet(nameof(Grow)))
        {
            bitStack.Grow = Grow;

            bitStack.StyleBuilder.Reset();
        }

        if (GrowContent.HasValue && bitStack.HasNotBeenSet(nameof(GrowContent)))
        {
            bitStack.GrowContent = GrowContent.Value;

            bitStack.ClassBuilder.Reset();
        }

        if (Grows.HasValue && bitStack.HasNotBeenSet(nameof(Grows)))
        {
            bitStack.Grows = Grows.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (Horizontal.HasValue && bitStack.HasNotBeenSet(nameof(Horizontal)))
        {
            bitStack.Horizontal = Horizontal.Value;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (HorizontalAlign.HasValue && bitStack.HasNotBeenSet(nameof(HorizontalAlign)))
        {
            bitStack.HorizontalAlign = HorizontalAlign.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (HorizontalGap.HasValue() && bitStack.HasNotBeenSet(nameof(HorizontalGap)))
        {
            bitStack.HorizontalGap = HorizontalGap;

            bitStack.StyleBuilder.Reset();
        }

        if (HorizontalXs.HasValue && bitStack.HasNotBeenSet(nameof(HorizontalXs)))
        {
            bitStack.HorizontalXs = HorizontalXs.Value;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (HorizontalSm.HasValue && bitStack.HasNotBeenSet(nameof(HorizontalSm)))
        {
            bitStack.HorizontalSm = HorizontalSm.Value;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (HorizontalMd.HasValue && bitStack.HasNotBeenSet(nameof(HorizontalMd)))
        {
            bitStack.HorizontalMd = HorizontalMd.Value;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (HorizontalLg.HasValue && bitStack.HasNotBeenSet(nameof(HorizontalLg)))
        {
            bitStack.HorizontalLg = HorizontalLg.Value;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (HorizontalXl.HasValue && bitStack.HasNotBeenSet(nameof(HorizontalXl)))
        {
            bitStack.HorizontalXl = HorizontalXl.Value;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (HorizontalXxl.HasValue && bitStack.HasNotBeenSet(nameof(HorizontalXxl)))
        {
            bitStack.HorizontalXxl = HorizontalXxl.Value;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (Inline.HasValue && bitStack.HasNotBeenSet(nameof(Inline)))
        {
            bitStack.Inline = Inline.Value;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (NoShrink.HasValue && bitStack.HasNotBeenSet(nameof(NoShrink)))
        {
            bitStack.NoShrink = NoShrink.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (NoShrinkContent.HasValue && bitStack.HasNotBeenSet(nameof(NoShrinkContent)))
        {
            bitStack.NoShrinkContent = NoShrinkContent.Value;

            bitStack.ClassBuilder.Reset();
        }

        if (Order.HasValue && bitStack.HasNotBeenSet(nameof(Order)))
        {
            bitStack.Order = Order.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (Padding.HasValue() && bitStack.HasNotBeenSet(nameof(Padding)))
        {
            bitStack.Padding = Padding;

            bitStack.StyleBuilder.Reset();
        }

        if (Reversed.HasValue && bitStack.HasNotBeenSet(nameof(Reversed)))
        {
            bitStack.Reversed = Reversed.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (Self.HasValue && bitStack.HasNotBeenSet(nameof(Self)))
        {
            bitStack.Self = Self.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (Shrink.HasValue() && bitStack.HasNotBeenSet(nameof(Shrink)))
        {
            bitStack.Shrink = Shrink;

            bitStack.StyleBuilder.Reset();
        }

        if (Shrinkable.HasValue && bitStack.HasNotBeenSet(nameof(Shrinkable)))
        {
            bitStack.Shrinkable = Shrinkable.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (Size.HasValue && bitStack.HasNotBeenSet(nameof(Size)))
        {
            bitStack.Size = Size.Value;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (VerticalAlign.HasValue && bitStack.HasNotBeenSet(nameof(VerticalAlign)))
        {
            bitStack.VerticalAlign = VerticalAlign.Value;

            bitStack.StyleBuilder.Reset();
        }

        if (VerticalGap.HasValue() && bitStack.HasNotBeenSet(nameof(VerticalGap)))
        {
            bitStack.VerticalGap = VerticalGap;

            bitStack.StyleBuilder.Reset();
        }

        if (Wrap.HasValue && bitStack.HasNotBeenSet(nameof(Wrap)))
        {
            bitStack.Wrap = Wrap.Value;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }

        if (WrapReverse.HasValue && bitStack.HasNotBeenSet(nameof(WrapReverse)))
        {
            bitStack.WrapReverse = WrapReverse.Value;

            bitStack.ClassBuilder.Reset();
            bitStack.StyleBuilder.Reset();
        }
    }
}
