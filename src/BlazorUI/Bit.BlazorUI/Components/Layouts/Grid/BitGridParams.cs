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
    /// Measures the breakpoints of the grid against its own width instead of the width of the viewport.
    /// </summary>
    public bool? Container { get; set; }

    /// <summary>
    /// The custom html element used for the root node.
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// Lets every child of the grid grow into whatever width its row did not use.
    /// </summary>
    public bool? Grow { get; set; }

    /// <summary>
    /// Defines the horizontal distribution of the children of the grid.
    /// </summary>
    public BitAlignment? HorizontalAlign { get; set; }

    /// <summary>
    /// Defines the horizontal spacing between the children of the grid.
    /// </summary>
    public string? HorizontalSpacing { get; set; }

    /// <summary>
    /// Defines the horizontal spacing between the children of the grid from the extra small breakpoint upwards.
    /// </summary>
    public string? HorizontalSpacingXs { get; set; }

    /// <summary>
    /// Defines the horizontal spacing between the children of the grid from the small breakpoint upwards.
    /// </summary>
    public string? HorizontalSpacingSm { get; set; }

    /// <summary>
    /// Defines the horizontal spacing between the children of the grid from the medium breakpoint upwards.
    /// </summary>
    public string? HorizontalSpacingMd { get; set; }

    /// <summary>
    /// Defines the horizontal spacing between the children of the grid from the large breakpoint upwards.
    /// </summary>
    public string? HorizontalSpacingLg { get; set; }

    /// <summary>
    /// Defines the horizontal spacing between the children of the grid from the extra large breakpoint upwards.
    /// </summary>
    public string? HorizontalSpacingXl { get; set; }

    /// <summary>
    /// Defines the horizontal spacing between the children of the grid from the extra extra large breakpoint upwards.
    /// </summary>
    public string? HorizontalSpacingXxl { get; set; }

    /// <summary>
    /// Sizes the children of the grid to a width they may not go below instead of to a number of columns, and
    /// fits as many of them on a row as that width allows.
    /// </summary>
    public string? MinItemWidth { get; set; }

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
    /// Defines the spacing between the children of the grid on both axes from the extra small breakpoint upwards.
    /// </summary>
    public string? SpacingXs { get; set; }

    /// <summary>
    /// Defines the spacing between the children of the grid on both axes from the small breakpoint upwards.
    /// </summary>
    public string? SpacingSm { get; set; }

    /// <summary>
    /// Defines the spacing between the children of the grid on both axes from the medium breakpoint upwards.
    /// </summary>
    public string? SpacingMd { get; set; }

    /// <summary>
    /// Defines the spacing between the children of the grid on both axes from the large breakpoint upwards.
    /// </summary>
    public string? SpacingLg { get; set; }

    /// <summary>
    /// Defines the spacing between the children of the grid on both axes from the extra large breakpoint upwards.
    /// </summary>
    public string? SpacingXl { get; set; }

    /// <summary>
    /// Defines the spacing between the children of the grid on both axes from the extra extra large breakpoint upwards.
    /// </summary>
    public string? SpacingXxl { get; set; }

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
    /// Defines the vertical spacing between the rows of the grid from the extra small breakpoint upwards.
    /// </summary>
    public string? VerticalSpacingXs { get; set; }

    /// <summary>
    /// Defines the vertical spacing between the rows of the grid from the small breakpoint upwards.
    /// </summary>
    public string? VerticalSpacingSm { get; set; }

    /// <summary>
    /// Defines the vertical spacing between the rows of the grid from the medium breakpoint upwards.
    /// </summary>
    public string? VerticalSpacingMd { get; set; }

    /// <summary>
    /// Defines the vertical spacing between the rows of the grid from the large breakpoint upwards.
    /// </summary>
    public string? VerticalSpacingLg { get; set; }

    /// <summary>
    /// Defines the vertical spacing between the rows of the grid from the extra large breakpoint upwards.
    /// </summary>
    public string? VerticalSpacingXl { get; set; }

    /// <summary>
    /// Defines the vertical spacing between the rows of the grid from the extra extra large breakpoint upwards.
    /// </summary>
    public string? VerticalSpacingXxl { get; set; }



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

        if (AlignContent.HasValue)
        {
            bitGrid.TakeFromCascade(nameof(AlignContent), AlignContent.Value, static g => g.AlignContent, static (g, v) => g.AlignContent = v);
        }

        if (Alignment.HasValue)
        {
            bitGrid.TakeFromCascade(nameof(Alignment), Alignment.Value, static g => g.Alignment, static (g, v) => g.Alignment = v);
        }

        if (Columns.HasValue)
        {
            bitGrid.TakeFromCascade(nameof(Columns), Columns.Value, static g => g.Columns, static (g, v) => g.Columns = v);
        }

        if (ColumnsXs.HasValue)
        {
            bitGrid.TakeFromCascade(nameof(ColumnsXs), ColumnsXs.Value, static g => g.ColumnsXs, static (g, v) => g.ColumnsXs = v);
        }

        if (ColumnsSm.HasValue)
        {
            bitGrid.TakeFromCascade(nameof(ColumnsSm), ColumnsSm.Value, static g => g.ColumnsSm, static (g, v) => g.ColumnsSm = v);
        }

        if (ColumnsMd.HasValue)
        {
            bitGrid.TakeFromCascade(nameof(ColumnsMd), ColumnsMd.Value, static g => g.ColumnsMd, static (g, v) => g.ColumnsMd = v);
        }

        if (ColumnsLg.HasValue)
        {
            bitGrid.TakeFromCascade(nameof(ColumnsLg), ColumnsLg.Value, static g => g.ColumnsLg, static (g, v) => g.ColumnsLg = v);
        }

        if (ColumnsXl.HasValue)
        {
            bitGrid.TakeFromCascade(nameof(ColumnsXl), ColumnsXl.Value, static g => g.ColumnsXl, static (g, v) => g.ColumnsXl = v);
        }

        if (ColumnsXxl.HasValue)
        {
            bitGrid.TakeFromCascade(nameof(ColumnsXxl), ColumnsXxl.Value, static g => g.ColumnsXxl, static (g, v) => g.ColumnsXxl = v);
        }

        if (Container.HasValue)
        {
            bitGrid.TakeFromCascade(nameof(Container), Container.Value, static g => g.Container, static (g, v) => g.Container = v);
        }

        if (Element.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(Element), Element, static g => g.Element, static (g, v) => g.Element = v);
        }

        if (Grow.HasValue)
        {
            bitGrid.TakeFromCascade(nameof(Grow), Grow.Value, static g => g.Grow, static (g, v) => g.Grow = v);
        }

        if (HorizontalAlign.HasValue)
        {
            bitGrid.TakeFromCascade(nameof(HorizontalAlign), HorizontalAlign.Value, static g => g.HorizontalAlign, static (g, v) => g.HorizontalAlign = v);
        }

        if (HorizontalSpacing.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(HorizontalSpacing), HorizontalSpacing, static g => g.HorizontalSpacing, static (g, v) => g.HorizontalSpacing = v);
        }

        if (MinItemWidth.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(MinItemWidth), MinItemWidth, static g => g.MinItemWidth, static (g, v) => g.MinItemWidth = v);
        }

        if (NoWrap.HasValue)
        {
            bitGrid.TakeFromCascade(nameof(NoWrap), NoWrap.Value, static g => g.NoWrap, static (g, v) => g.NoWrap = v);
        }

        if (Reversed.HasValue)
        {
            bitGrid.TakeFromCascade(nameof(Reversed), Reversed.Value, static g => g.Reversed, static (g, v) => g.Reversed = v);
        }

        if (Spacing.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(Spacing), Spacing, static g => g.Spacing, static (g, v) => g.Spacing = v);
        }

        if (Span.HasValue)
        {
            bitGrid.TakeFromCascade(nameof(Span), Span.Value, static g => g.Span, static (g, v) => g.Span = v);
        }

        if (VerticalAlign.HasValue)
        {
            bitGrid.TakeFromCascade(nameof(VerticalAlign), VerticalAlign.Value, static g => g.VerticalAlign, static (g, v) => g.VerticalAlign = v);
        }

        if (VerticalSpacing.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(VerticalSpacing), VerticalSpacing, static g => g.VerticalSpacing, static (g, v) => g.VerticalSpacing = v);
        }

        UpdateResponsiveSpacingParameters(bitGrid);
    }



    private void UpdateResponsiveSpacingParameters(BitGrid bitGrid)
    {
        if (SpacingXs.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(SpacingXs), SpacingXs, static g => g.SpacingXs, static (g, v) => g.SpacingXs = v);
        }

        if (SpacingSm.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(SpacingSm), SpacingSm, static g => g.SpacingSm, static (g, v) => g.SpacingSm = v);
        }

        if (SpacingMd.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(SpacingMd), SpacingMd, static g => g.SpacingMd, static (g, v) => g.SpacingMd = v);
        }

        if (SpacingLg.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(SpacingLg), SpacingLg, static g => g.SpacingLg, static (g, v) => g.SpacingLg = v);
        }

        if (SpacingXl.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(SpacingXl), SpacingXl, static g => g.SpacingXl, static (g, v) => g.SpacingXl = v);
        }

        if (SpacingXxl.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(SpacingXxl), SpacingXxl, static g => g.SpacingXxl, static (g, v) => g.SpacingXxl = v);
        }

        if (HorizontalSpacingXs.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(HorizontalSpacingXs), HorizontalSpacingXs, static g => g.HorizontalSpacingXs, static (g, v) => g.HorizontalSpacingXs = v);
        }

        if (HorizontalSpacingSm.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(HorizontalSpacingSm), HorizontalSpacingSm, static g => g.HorizontalSpacingSm, static (g, v) => g.HorizontalSpacingSm = v);
        }

        if (HorizontalSpacingMd.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(HorizontalSpacingMd), HorizontalSpacingMd, static g => g.HorizontalSpacingMd, static (g, v) => g.HorizontalSpacingMd = v);
        }

        if (HorizontalSpacingLg.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(HorizontalSpacingLg), HorizontalSpacingLg, static g => g.HorizontalSpacingLg, static (g, v) => g.HorizontalSpacingLg = v);
        }

        if (HorizontalSpacingXl.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(HorizontalSpacingXl), HorizontalSpacingXl, static g => g.HorizontalSpacingXl, static (g, v) => g.HorizontalSpacingXl = v);
        }

        if (HorizontalSpacingXxl.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(HorizontalSpacingXxl), HorizontalSpacingXxl, static g => g.HorizontalSpacingXxl, static (g, v) => g.HorizontalSpacingXxl = v);
        }

        if (VerticalSpacingXs.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(VerticalSpacingXs), VerticalSpacingXs, static g => g.VerticalSpacingXs, static (g, v) => g.VerticalSpacingXs = v);
        }

        if (VerticalSpacingSm.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(VerticalSpacingSm), VerticalSpacingSm, static g => g.VerticalSpacingSm, static (g, v) => g.VerticalSpacingSm = v);
        }

        if (VerticalSpacingMd.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(VerticalSpacingMd), VerticalSpacingMd, static g => g.VerticalSpacingMd, static (g, v) => g.VerticalSpacingMd = v);
        }

        if (VerticalSpacingLg.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(VerticalSpacingLg), VerticalSpacingLg, static g => g.VerticalSpacingLg, static (g, v) => g.VerticalSpacingLg = v);
        }

        if (VerticalSpacingXl.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(VerticalSpacingXl), VerticalSpacingXl, static g => g.VerticalSpacingXl, static (g, v) => g.VerticalSpacingXl = v);
        }

        if (VerticalSpacingXxl.HasValue())
        {
            bitGrid.TakeFromCascade(nameof(VerticalSpacingXxl), VerticalSpacingXxl, static g => g.VerticalSpacingXxl, static (g, v) => g.VerticalSpacingXxl = v);
        }
    }
}
