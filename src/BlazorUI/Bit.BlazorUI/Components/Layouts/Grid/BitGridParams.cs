namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitGrid"/> component.
/// </summary>
public class BitGridParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitGrid"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitGrid value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitGrid)}";



    public string Name => ParamName;



    /// <summary>
    /// Defines how the rows of a wrapping grid share out the height left over.
    /// </summary>
    public BitAlignment? AlignContent { get; set; }

    /// <summary>
    /// Gets or sets the alignment of the children of the grid on both axes at once.
    /// </summary>
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// Defines the number of columns the width of the grid is divided into.
    /// </summary>
    public int? Columns { get; set; }

    /// <summary>
    /// Number of columns in the extra small breakpoint.
    /// </summary>
    public int? ColumnsXs { get; set; }

    /// <summary>
    /// Number of columns in the small breakpoint.
    /// </summary>
    public int? ColumnsSm { get; set; }

    /// <summary>
    /// Number of columns in the medium breakpoint.
    /// </summary>
    public int? ColumnsMd { get; set; }

    /// <summary>
    /// Number of columns in the large breakpoint.
    /// </summary>
    public int? ColumnsLg { get; set; }

    /// <summary>
    /// Number of columns in the extra large breakpoint.
    /// </summary>
    public int? ColumnsXl { get; set; }

    /// <summary>
    /// Number of columns in the extra extra large breakpoint.
    /// </summary>
    public int? ColumnsXxl { get; set; }

    /// <summary>
    /// The custom html element used for the root node.
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// Defines the horizontal distribution of the children of the grid.
    /// </summary>
    public BitAlignment? HorizontalAlign { get; set; }

    /// <summary>
    /// Defines the horizontal spacing between the children of the grid.
    /// </summary>
    public string? HorizontalSpacing { get; set; }

    /// <summary>
    /// Keeps the children of the grid on a single row instead of letting them wrap onto more rows.
    /// </summary>
    public bool? NoWrap { get; set; }

    /// <summary>
    /// Renders the children of the grid in the opposite direction.
    /// </summary>
    public bool? Reversed { get; set; }

    /// <summary>
    /// Defines the spacing between the children of the grid on both axes.
    /// </summary>
    public string? Spacing { get; set; }

    /// <summary>
    /// Defines the number of columns the children of the grid fill by default.
    /// </summary>
    public int? Span { get; set; }

    /// <summary>
    /// Defines the vertical alignment of the children of the grid within a row.
    /// </summary>
    public BitAlignment? VerticalAlign { get; set; }

    /// <summary>
    /// Defines the vertical spacing between the rows of the grid.
    /// </summary>
    public string? VerticalSpacing { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitGrid"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitGrid"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitGrid"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitGrid"/>.
    /// </remarks>
    /// <param name="bitGrid">
    /// The <see cref="BitGrid"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitGrid bitGrid)
    {
        if (bitGrid is null) return;

        UpdateBaseParameters(bitGrid);

        if (AlignContent.HasValue && bitGrid.HasNotBeenSet(nameof(AlignContent)))
        {
            bitGrid.AlignContent = AlignContent.Value;

            bitGrid.StyleBuilder.Reset();
        }

        if (Alignment.HasValue && bitGrid.HasNotBeenSet(nameof(Alignment)))
        {
            bitGrid.Alignment = Alignment.Value;

            bitGrid.StyleBuilder.Reset();
        }

        if (Columns.HasValue && bitGrid.HasNotBeenSet(nameof(Columns)))
        {
            bitGrid.Columns = Columns.Value;

            bitGrid.StyleBuilder.Reset();
        }

        if (ColumnsXs.HasValue && bitGrid.HasNotBeenSet(nameof(ColumnsXs)))
        {
            bitGrid.ColumnsXs = ColumnsXs.Value;

            bitGrid.StyleBuilder.Reset();
        }

        if (ColumnsSm.HasValue && bitGrid.HasNotBeenSet(nameof(ColumnsSm)))
        {
            bitGrid.ColumnsSm = ColumnsSm.Value;

            bitGrid.StyleBuilder.Reset();
        }

        if (ColumnsMd.HasValue && bitGrid.HasNotBeenSet(nameof(ColumnsMd)))
        {
            bitGrid.ColumnsMd = ColumnsMd.Value;

            bitGrid.StyleBuilder.Reset();
        }

        if (ColumnsLg.HasValue && bitGrid.HasNotBeenSet(nameof(ColumnsLg)))
        {
            bitGrid.ColumnsLg = ColumnsLg.Value;

            bitGrid.StyleBuilder.Reset();
        }

        if (ColumnsXl.HasValue && bitGrid.HasNotBeenSet(nameof(ColumnsXl)))
        {
            bitGrid.ColumnsXl = ColumnsXl.Value;

            bitGrid.StyleBuilder.Reset();
        }

        if (ColumnsXxl.HasValue && bitGrid.HasNotBeenSet(nameof(ColumnsXxl)))
        {
            bitGrid.ColumnsXxl = ColumnsXxl.Value;

            bitGrid.StyleBuilder.Reset();
        }

        if (Element.HasValue() && bitGrid.HasNotBeenSet(nameof(Element)))
        {
            bitGrid.Element = Element;
        }

        if (HorizontalAlign.HasValue && bitGrid.HasNotBeenSet(nameof(HorizontalAlign)))
        {
            bitGrid.HorizontalAlign = HorizontalAlign.Value;

            bitGrid.StyleBuilder.Reset();
        }

        if (HorizontalSpacing.HasValue() && bitGrid.HasNotBeenSet(nameof(HorizontalSpacing)))
        {
            bitGrid.HorizontalSpacing = HorizontalSpacing;

            bitGrid.StyleBuilder.Reset();
        }

        if (NoWrap.HasValue && bitGrid.HasNotBeenSet(nameof(NoWrap)))
        {
            bitGrid.NoWrap = NoWrap.Value;

            bitGrid.ClassBuilder.Reset();
        }

        if (Reversed.HasValue && bitGrid.HasNotBeenSet(nameof(Reversed)))
        {
            bitGrid.Reversed = Reversed.Value;

            bitGrid.ClassBuilder.Reset();
        }

        if (Spacing.HasValue() && bitGrid.HasNotBeenSet(nameof(Spacing)))
        {
            bitGrid.Spacing = Spacing!;

            bitGrid.StyleBuilder.Reset();
        }

        if (Span.HasValue && bitGrid.HasNotBeenSet(nameof(Span)))
        {
            bitGrid.Span = Span.Value;

            bitGrid.StyleBuilder.Reset();
        }

        if (VerticalAlign.HasValue && bitGrid.HasNotBeenSet(nameof(VerticalAlign)))
        {
            bitGrid.VerticalAlign = VerticalAlign.Value;

            bitGrid.StyleBuilder.Reset();
        }

        if (VerticalSpacing.HasValue() && bitGrid.HasNotBeenSet(nameof(VerticalSpacing)))
        {
            bitGrid.VerticalSpacing = VerticalSpacing;

            bitGrid.StyleBuilder.Reset();
        }
    }
}
