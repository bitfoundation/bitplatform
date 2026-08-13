namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitGridItem"/> component.
/// </summary>
public class BitGridItemParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitGridItem"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitGridItem value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitGridItem)}";



    public string Name => ParamName;



    /// <summary>
    /// Defines the vertical alignment of the item within its row.
    /// </summary>
    public BitAlignment? AlignSelf { get; set; }

    /// <summary>
    /// Sizes the item to its own content instead of to a number of columns.
    /// </summary>
    public bool? Auto { get; set; }

    /// <summary>
    /// Sizes the item to its own content from the extra small breakpoint upwards.
    /// </summary>
    public bool? AutoXs { get; set; }

    /// <summary>
    /// Sizes the item to its own content from the small breakpoint upwards.
    /// </summary>
    public bool? AutoSm { get; set; }

    /// <summary>
    /// Sizes the item to its own content from the medium breakpoint upwards.
    /// </summary>
    public bool? AutoMd { get; set; }

    /// <summary>
    /// Sizes the item to its own content from the large breakpoint upwards.
    /// </summary>
    public bool? AutoLg { get; set; }

    /// <summary>
    /// Sizes the item to its own content from the extra large breakpoint upwards.
    /// </summary>
    public bool? AutoXl { get; set; }

    /// <summary>
    /// Sizes the item to its own content from the extra extra large breakpoint upwards.
    /// </summary>
    public bool? AutoXxl { get; set; }

    /// <summary>
    /// Pushes the item to the end edge of its row by turning every column left over before it into the offset.
    /// </summary>
    public bool? AutoOffset { get; set; }

    /// <summary>
    /// Number of columns the item should fill.
    /// </summary>
    public int? ColumnSpan { get; set; }

    /// <summary>
    /// The custom html element used for the root node.
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// Lets the item grow into whatever width is left over in its row.
    /// </summary>
    public bool? Grow { get; set; }

    /// <summary>
    /// Lets the item grow into whatever width is left over in its row from the extra small breakpoint upwards.
    /// </summary>
    public bool? GrowXs { get; set; }

    /// <summary>
    /// Lets the item grow into whatever width is left over in its row from the small breakpoint upwards.
    /// </summary>
    public bool? GrowSm { get; set; }

    /// <summary>
    /// Lets the item grow into whatever width is left over in its row from the medium breakpoint upwards.
    /// </summary>
    public bool? GrowMd { get; set; }

    /// <summary>
    /// Lets the item grow into whatever width is left over in its row from the large breakpoint upwards.
    /// </summary>
    public bool? GrowLg { get; set; }

    /// <summary>
    /// Lets the item grow into whatever width is left over in its row from the extra large breakpoint upwards.
    /// </summary>
    public bool? GrowXl { get; set; }

    /// <summary>
    /// Lets the item grow into whatever width is left over in its row from the extra extra large breakpoint upwards.
    /// </summary>
    public bool? GrowXxl { get; set; }

    /// <summary>
    /// Number of columns to leave empty before the item.
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    /// Number of columns to leave empty before the item in the extra small breakpoint.
    /// </summary>
    public int? OffsetXs { get; set; }

    /// <summary>
    /// Number of columns to leave empty before the item in the small breakpoint.
    /// </summary>
    public int? OffsetSm { get; set; }

    /// <summary>
    /// Number of columns to leave empty before the item in the medium breakpoint.
    /// </summary>
    public int? OffsetMd { get; set; }

    /// <summary>
    /// Number of columns to leave empty before the item in the large breakpoint.
    /// </summary>
    public int? OffsetLg { get; set; }

    /// <summary>
    /// Number of columns to leave empty before the item in the extra large breakpoint.
    /// </summary>
    public int? OffsetXl { get; set; }

    /// <summary>
    /// Number of columns to leave empty before the item in the extra extra large breakpoint.
    /// </summary>
    public int? OffsetXxl { get; set; }

    /// <summary>
    /// Defines the position of the item among its siblings.
    /// </summary>
    public int? Order { get; set; }

    /// <summary>
    /// Defines the position of the item among its siblings in the extra small breakpoint.
    /// </summary>
    public int? OrderXs { get; set; }

    /// <summary>
    /// Defines the position of the item among its siblings in the small breakpoint.
    /// </summary>
    public int? OrderSm { get; set; }

    /// <summary>
    /// Defines the position of the item among its siblings in the medium breakpoint.
    /// </summary>
    public int? OrderMd { get; set; }

    /// <summary>
    /// Defines the position of the item among its siblings in the large breakpoint.
    /// </summary>
    public int? OrderLg { get; set; }

    /// <summary>
    /// Defines the position of the item among its siblings in the extra large breakpoint.
    /// </summary>
    public int? OrderXl { get; set; }

    /// <summary>
    /// Defines the position of the item among its siblings in the extra extra large breakpoint.
    /// </summary>
    public int? OrderXxl { get; set; }

    /// <summary>
    /// Number of columns the item should fill in the extra small breakpoint.
    /// </summary>
    public int? Xs { get; set; }

    /// <summary>
    /// Number of columns the item should fill in the small breakpoint.
    /// </summary>
    public int? Sm { get; set; }

    /// <summary>
    /// Number of columns the item should fill in the medium breakpoint.
    /// </summary>
    public int? Md { get; set; }

    /// <summary>
    /// Number of columns the item should fill in the large breakpoint.
    /// </summary>
    public int? Lg { get; set; }

    /// <summary>
    /// Number of columns the item should fill in the extra large breakpoint.
    /// </summary>
    public int? Xl { get; set; }

    /// <summary>
    /// Number of columns the item should fill in the extra extra large breakpoint.
    /// </summary>
    public int? Xxl { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitGridItem"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitGridItem"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitGridItem"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitGridItem"/>.
    /// </remarks>
    /// <param name="bitGridItem">
    /// The <see cref="BitGridItem"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitGridItem bitGridItem)
    {
        if (bitGridItem is null) return;

        UpdateBaseParameters(bitGridItem);

        if (AlignSelf.HasValue && bitGridItem.HasNotBeenSet(nameof(AlignSelf)))
        {
            bitGridItem.AlignSelf = AlignSelf.Value;

            bitGridItem.StyleBuilder.Reset();
        }

        if (Auto.HasValue && bitGridItem.HasNotBeenSet(nameof(Auto)))
        {
            bitGridItem.Auto = Auto.Value;

            bitGridItem.ClassBuilder.Reset();
        }

        if (AutoOffset.HasValue && bitGridItem.HasNotBeenSet(nameof(AutoOffset)))
        {
            bitGridItem.AutoOffset = AutoOffset.Value;

            bitGridItem.ClassBuilder.Reset();
        }

        if (ColumnSpan.HasValue && bitGridItem.HasNotBeenSet(nameof(ColumnSpan)))
        {
            bitGridItem.ColumnSpan = ColumnSpan.Value;

            bitGridItem.StyleBuilder.Reset();
        }

        if (Element.HasValue() && bitGridItem.HasNotBeenSet(nameof(Element)))
        {
            bitGridItem.Element = Element;
        }

        if (Grow.HasValue && bitGridItem.HasNotBeenSet(nameof(Grow)))
        {
            bitGridItem.Grow = Grow.Value;

            bitGridItem.ClassBuilder.Reset();
        }

        UpdateSizingParameters(bitGridItem);

        UpdateOffsetParameters(bitGridItem);

        UpdateOrderParameters(bitGridItem);

        UpdateSpanParameters(bitGridItem);
    }



    private void UpdateSizingParameters(BitGridItem bitGridItem)
    {
        if (AutoXs.HasValue && bitGridItem.HasNotBeenSet(nameof(AutoXs)))
        {
            bitGridItem.AutoXs = AutoXs.Value;

            bitGridItem.ClassBuilder.Reset();
        }

        if (AutoSm.HasValue && bitGridItem.HasNotBeenSet(nameof(AutoSm)))
        {
            bitGridItem.AutoSm = AutoSm.Value;

            bitGridItem.ClassBuilder.Reset();
        }

        if (AutoMd.HasValue && bitGridItem.HasNotBeenSet(nameof(AutoMd)))
        {
            bitGridItem.AutoMd = AutoMd.Value;

            bitGridItem.ClassBuilder.Reset();
        }

        if (AutoLg.HasValue && bitGridItem.HasNotBeenSet(nameof(AutoLg)))
        {
            bitGridItem.AutoLg = AutoLg.Value;

            bitGridItem.ClassBuilder.Reset();
        }

        if (AutoXl.HasValue && bitGridItem.HasNotBeenSet(nameof(AutoXl)))
        {
            bitGridItem.AutoXl = AutoXl.Value;

            bitGridItem.ClassBuilder.Reset();
        }

        if (AutoXxl.HasValue && bitGridItem.HasNotBeenSet(nameof(AutoXxl)))
        {
            bitGridItem.AutoXxl = AutoXxl.Value;

            bitGridItem.ClassBuilder.Reset();
        }

        if (GrowXs.HasValue && bitGridItem.HasNotBeenSet(nameof(GrowXs)))
        {
            bitGridItem.GrowXs = GrowXs.Value;

            bitGridItem.ClassBuilder.Reset();
        }

        if (GrowSm.HasValue && bitGridItem.HasNotBeenSet(nameof(GrowSm)))
        {
            bitGridItem.GrowSm = GrowSm.Value;

            bitGridItem.ClassBuilder.Reset();
        }

        if (GrowMd.HasValue && bitGridItem.HasNotBeenSet(nameof(GrowMd)))
        {
            bitGridItem.GrowMd = GrowMd.Value;

            bitGridItem.ClassBuilder.Reset();
        }

        if (GrowLg.HasValue && bitGridItem.HasNotBeenSet(nameof(GrowLg)))
        {
            bitGridItem.GrowLg = GrowLg.Value;

            bitGridItem.ClassBuilder.Reset();
        }

        if (GrowXl.HasValue && bitGridItem.HasNotBeenSet(nameof(GrowXl)))
        {
            bitGridItem.GrowXl = GrowXl.Value;

            bitGridItem.ClassBuilder.Reset();
        }

        if (GrowXxl.HasValue && bitGridItem.HasNotBeenSet(nameof(GrowXxl)))
        {
            bitGridItem.GrowXxl = GrowXxl.Value;

            bitGridItem.ClassBuilder.Reset();
        }
    }

    private void UpdateOffsetParameters(BitGridItem bitGridItem)
    {
        if (Offset.HasValue && bitGridItem.HasNotBeenSet(nameof(Offset)))
        {
            bitGridItem.Offset = Offset.Value;

            bitGridItem.ClassBuilder.Reset();
            bitGridItem.StyleBuilder.Reset();
        }

        if (OffsetXs.HasValue && bitGridItem.HasNotBeenSet(nameof(OffsetXs)))
        {
            bitGridItem.OffsetXs = OffsetXs.Value;

            bitGridItem.ClassBuilder.Reset();
            bitGridItem.StyleBuilder.Reset();
        }

        if (OffsetSm.HasValue && bitGridItem.HasNotBeenSet(nameof(OffsetSm)))
        {
            bitGridItem.OffsetSm = OffsetSm.Value;

            bitGridItem.ClassBuilder.Reset();
            bitGridItem.StyleBuilder.Reset();
        }

        if (OffsetMd.HasValue && bitGridItem.HasNotBeenSet(nameof(OffsetMd)))
        {
            bitGridItem.OffsetMd = OffsetMd.Value;

            bitGridItem.ClassBuilder.Reset();
            bitGridItem.StyleBuilder.Reset();
        }

        if (OffsetLg.HasValue && bitGridItem.HasNotBeenSet(nameof(OffsetLg)))
        {
            bitGridItem.OffsetLg = OffsetLg.Value;

            bitGridItem.ClassBuilder.Reset();
            bitGridItem.StyleBuilder.Reset();
        }

        if (OffsetXl.HasValue && bitGridItem.HasNotBeenSet(nameof(OffsetXl)))
        {
            bitGridItem.OffsetXl = OffsetXl.Value;

            bitGridItem.ClassBuilder.Reset();
            bitGridItem.StyleBuilder.Reset();
        }

        if (OffsetXxl.HasValue && bitGridItem.HasNotBeenSet(nameof(OffsetXxl)))
        {
            bitGridItem.OffsetXxl = OffsetXxl.Value;

            bitGridItem.ClassBuilder.Reset();
            bitGridItem.StyleBuilder.Reset();
        }
    }

    private void UpdateOrderParameters(BitGridItem bitGridItem)
    {
        if (Order.HasValue && bitGridItem.HasNotBeenSet(nameof(Order)))
        {
            bitGridItem.Order = Order.Value;

            bitGridItem.ClassBuilder.Reset();
            bitGridItem.StyleBuilder.Reset();
        }

        if (OrderXs.HasValue && bitGridItem.HasNotBeenSet(nameof(OrderXs)))
        {
            bitGridItem.OrderXs = OrderXs.Value;

            bitGridItem.ClassBuilder.Reset();
            bitGridItem.StyleBuilder.Reset();
        }

        if (OrderSm.HasValue && bitGridItem.HasNotBeenSet(nameof(OrderSm)))
        {
            bitGridItem.OrderSm = OrderSm.Value;

            bitGridItem.ClassBuilder.Reset();
            bitGridItem.StyleBuilder.Reset();
        }

        if (OrderMd.HasValue && bitGridItem.HasNotBeenSet(nameof(OrderMd)))
        {
            bitGridItem.OrderMd = OrderMd.Value;

            bitGridItem.ClassBuilder.Reset();
            bitGridItem.StyleBuilder.Reset();
        }

        if (OrderLg.HasValue && bitGridItem.HasNotBeenSet(nameof(OrderLg)))
        {
            bitGridItem.OrderLg = OrderLg.Value;

            bitGridItem.ClassBuilder.Reset();
            bitGridItem.StyleBuilder.Reset();
        }

        if (OrderXl.HasValue && bitGridItem.HasNotBeenSet(nameof(OrderXl)))
        {
            bitGridItem.OrderXl = OrderXl.Value;

            bitGridItem.ClassBuilder.Reset();
            bitGridItem.StyleBuilder.Reset();
        }

        if (OrderXxl.HasValue && bitGridItem.HasNotBeenSet(nameof(OrderXxl)))
        {
            bitGridItem.OrderXxl = OrderXxl.Value;

            bitGridItem.ClassBuilder.Reset();
            bitGridItem.StyleBuilder.Reset();
        }
    }

    private void UpdateSpanParameters(BitGridItem bitGridItem)
    {
        if (Xs.HasValue && bitGridItem.HasNotBeenSet(nameof(Xs)))
        {
            bitGridItem.Xs = Xs.Value;

            bitGridItem.StyleBuilder.Reset();
        }

        if (Sm.HasValue && bitGridItem.HasNotBeenSet(nameof(Sm)))
        {
            bitGridItem.Sm = Sm.Value;

            bitGridItem.StyleBuilder.Reset();
        }

        if (Md.HasValue && bitGridItem.HasNotBeenSet(nameof(Md)))
        {
            bitGridItem.Md = Md.Value;

            bitGridItem.StyleBuilder.Reset();
        }

        if (Lg.HasValue && bitGridItem.HasNotBeenSet(nameof(Lg)))
        {
            bitGridItem.Lg = Lg.Value;

            bitGridItem.StyleBuilder.Reset();
        }

        if (Xl.HasValue && bitGridItem.HasNotBeenSet(nameof(Xl)))
        {
            bitGridItem.Xl = Xl.Value;

            bitGridItem.StyleBuilder.Reset();
        }

        if (Xxl.HasValue && bitGridItem.HasNotBeenSet(nameof(Xxl)))
        {
            bitGridItem.Xxl = Xxl.Value;

            bitGridItem.StyleBuilder.Reset();
        }
    }
}
