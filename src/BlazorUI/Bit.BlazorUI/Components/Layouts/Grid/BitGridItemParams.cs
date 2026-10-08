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
    /// Pushes the item to the end edge of its row from the extra small breakpoint upwards.
    /// </summary>
    public bool? AutoOffsetXs { get; set; }

    /// <summary>
    /// Pushes the item to the end edge of its row from the small breakpoint upwards.
    /// </summary>
    public bool? AutoOffsetSm { get; set; }

    /// <summary>
    /// Pushes the item to the end edge of its row from the medium breakpoint upwards.
    /// </summary>
    public bool? AutoOffsetMd { get; set; }

    /// <summary>
    /// Pushes the item to the end edge of its row from the large breakpoint upwards.
    /// </summary>
    public bool? AutoOffsetLg { get; set; }

    /// <summary>
    /// Pushes the item to the end edge of its row from the extra large breakpoint upwards.
    /// </summary>
    public bool? AutoOffsetXl { get; set; }

    /// <summary>
    /// Pushes the item to the end edge of its row from the extra extra large breakpoint upwards.
    /// </summary>
    public bool? AutoOffsetXxl { get; set; }

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

        if (AlignSelf.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(AlignSelf), AlignSelf.Value, static g => g.AlignSelf, static (g, v) => g.AlignSelf = v);
        }

        if (Auto.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(Auto), Auto.Value, static g => g.Auto, static (g, v) => g.Auto = v);
        }

        if (AutoOffset.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(AutoOffset), AutoOffset.Value, static g => g.AutoOffset, static (g, v) => g.AutoOffset = v);
        }

        if (ColumnSpan.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(ColumnSpan), ColumnSpan.Value, static g => g.ColumnSpan, static (g, v) => g.ColumnSpan = v);
        }

        if (Element.HasValue())
        {
            bitGridItem.TakeFromCascade(nameof(Element), Element, static g => g.Element, static (g, v) => g.Element = v);
        }

        if (Grow.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(Grow), Grow.Value, static g => g.Grow, static (g, v) => g.Grow = v);
        }

        UpdateSizingParameters(bitGridItem);

        UpdateOffsetParameters(bitGridItem);

        UpdateOrderParameters(bitGridItem);

        UpdateSpanParameters(bitGridItem);
    }



    private void UpdateSizingParameters(BitGridItem bitGridItem)
    {
        if (AutoXs.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(AutoXs), AutoXs.Value, static g => g.AutoXs, static (g, v) => g.AutoXs = v);
        }

        if (AutoSm.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(AutoSm), AutoSm.Value, static g => g.AutoSm, static (g, v) => g.AutoSm = v);
        }

        if (AutoMd.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(AutoMd), AutoMd.Value, static g => g.AutoMd, static (g, v) => g.AutoMd = v);
        }

        if (AutoLg.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(AutoLg), AutoLg.Value, static g => g.AutoLg, static (g, v) => g.AutoLg = v);
        }

        if (AutoXl.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(AutoXl), AutoXl.Value, static g => g.AutoXl, static (g, v) => g.AutoXl = v);
        }

        if (AutoXxl.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(AutoXxl), AutoXxl.Value, static g => g.AutoXxl, static (g, v) => g.AutoXxl = v);
        }

        if (GrowXs.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(GrowXs), GrowXs.Value, static g => g.GrowXs, static (g, v) => g.GrowXs = v);
        }

        if (GrowSm.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(GrowSm), GrowSm.Value, static g => g.GrowSm, static (g, v) => g.GrowSm = v);
        }

        if (GrowMd.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(GrowMd), GrowMd.Value, static g => g.GrowMd, static (g, v) => g.GrowMd = v);
        }

        if (GrowLg.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(GrowLg), GrowLg.Value, static g => g.GrowLg, static (g, v) => g.GrowLg = v);
        }

        if (GrowXl.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(GrowXl), GrowXl.Value, static g => g.GrowXl, static (g, v) => g.GrowXl = v);
        }

        if (GrowXxl.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(GrowXxl), GrowXxl.Value, static g => g.GrowXxl, static (g, v) => g.GrowXxl = v);
        }
    }

    private void UpdateOffsetParameters(BitGridItem bitGridItem)
    {
        if (AutoOffsetXs.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(AutoOffsetXs), AutoOffsetXs.Value, static g => g.AutoOffsetXs, static (g, v) => g.AutoOffsetXs = v);
        }

        if (AutoOffsetSm.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(AutoOffsetSm), AutoOffsetSm.Value, static g => g.AutoOffsetSm, static (g, v) => g.AutoOffsetSm = v);
        }

        if (AutoOffsetMd.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(AutoOffsetMd), AutoOffsetMd.Value, static g => g.AutoOffsetMd, static (g, v) => g.AutoOffsetMd = v);
        }

        if (AutoOffsetLg.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(AutoOffsetLg), AutoOffsetLg.Value, static g => g.AutoOffsetLg, static (g, v) => g.AutoOffsetLg = v);
        }

        if (AutoOffsetXl.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(AutoOffsetXl), AutoOffsetXl.Value, static g => g.AutoOffsetXl, static (g, v) => g.AutoOffsetXl = v);
        }

        if (AutoOffsetXxl.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(AutoOffsetXxl), AutoOffsetXxl.Value, static g => g.AutoOffsetXxl, static (g, v) => g.AutoOffsetXxl = v);
        }

        if (Offset.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(Offset), Offset.Value, static g => g.Offset, static (g, v) => g.Offset = v);
        }

        if (OffsetXs.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(OffsetXs), OffsetXs.Value, static g => g.OffsetXs, static (g, v) => g.OffsetXs = v);
        }

        if (OffsetSm.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(OffsetSm), OffsetSm.Value, static g => g.OffsetSm, static (g, v) => g.OffsetSm = v);
        }

        if (OffsetMd.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(OffsetMd), OffsetMd.Value, static g => g.OffsetMd, static (g, v) => g.OffsetMd = v);
        }

        if (OffsetLg.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(OffsetLg), OffsetLg.Value, static g => g.OffsetLg, static (g, v) => g.OffsetLg = v);
        }

        if (OffsetXl.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(OffsetXl), OffsetXl.Value, static g => g.OffsetXl, static (g, v) => g.OffsetXl = v);
        }

        if (OffsetXxl.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(OffsetXxl), OffsetXxl.Value, static g => g.OffsetXxl, static (g, v) => g.OffsetXxl = v);
        }
    }

    private void UpdateOrderParameters(BitGridItem bitGridItem)
    {
        if (Order.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(Order), Order.Value, static g => g.Order, static (g, v) => g.Order = v);
        }

        if (OrderXs.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(OrderXs), OrderXs.Value, static g => g.OrderXs, static (g, v) => g.OrderXs = v);
        }

        if (OrderSm.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(OrderSm), OrderSm.Value, static g => g.OrderSm, static (g, v) => g.OrderSm = v);
        }

        if (OrderMd.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(OrderMd), OrderMd.Value, static g => g.OrderMd, static (g, v) => g.OrderMd = v);
        }

        if (OrderLg.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(OrderLg), OrderLg.Value, static g => g.OrderLg, static (g, v) => g.OrderLg = v);
        }

        if (OrderXl.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(OrderXl), OrderXl.Value, static g => g.OrderXl, static (g, v) => g.OrderXl = v);
        }

        if (OrderXxl.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(OrderXxl), OrderXxl.Value, static g => g.OrderXxl, static (g, v) => g.OrderXxl = v);
        }
    }

    private void UpdateSpanParameters(BitGridItem bitGridItem)
    {
        if (Xs.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(Xs), Xs.Value, static g => g.Xs, static (g, v) => g.Xs = v);
        }

        if (Sm.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(Sm), Sm.Value, static g => g.Sm, static (g, v) => g.Sm = v);
        }

        if (Md.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(Md), Md.Value, static g => g.Md, static (g, v) => g.Md = v);
        }

        if (Lg.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(Lg), Lg.Value, static g => g.Lg, static (g, v) => g.Lg = v);
        }

        if (Xl.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(Xl), Xl.Value, static g => g.Xl, static (g, v) => g.Xl = v);
        }

        if (Xxl.HasValue)
        {
            bitGridItem.TakeFromCascade(nameof(Xxl), Xxl.Value, static g => g.Xxl, static (g, v) => g.Xxl = v);
        }
    }
}
