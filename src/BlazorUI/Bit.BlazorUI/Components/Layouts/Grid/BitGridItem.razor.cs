namespace Bit.BlazorUI;

public partial class BitGridItem : BitComponentBase
{
    private int columnSpan = 1;


    [CascadingParameter] protected BitGrid? Parent { get; set; }


    /// <summary>
    /// The content of the Grid item.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Number of columns a grid item should fill.
    /// </summary>
    [Parameter]
    public int ColumnSpan
    {
        get => columnSpan;
        set
        {
            if (columnSpan == value) return;

            columnSpan = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Number of columns in the extra small breakpoint.
    /// </summary>
    [Parameter] public int? Xs { get; set; }

    /// <summary>
    ///Number of columns in the small breakpoint.
    /// </summary>
    [Parameter] public int? Sm { get; set; }

    /// <summary>
    /// Number of columns in the medium breakpoint.
    /// </summary>
    [Parameter] public int? Md { get; set; }

    /// <summary>
    /// Number of columns in the large breakpoint.
    /// </summary>
    [Parameter] public int? Lg { get; set; }

    /// <summary>
    /// Number of columns in the extra large breakpoint.
    /// </summary>
    [Parameter] public int? Xl { get; set; }

    /// <summary>
    /// Number of columns in the extra extra large breakpoint.
    /// </summary>
    [Parameter] public int? Xxl { get; set; }


    protected override string RootElementClass => "bit-grd-itm";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Xs.HasValue ? $"--xs:{Xs}" : string.Empty);
        StyleBuilder.Register(() => Sm.HasValue ? $"--sm:{Sm}" : string.Empty);
        StyleBuilder.Register(() => Md.HasValue ? $"--md:{Md}" : string.Empty);
        StyleBuilder.Register(() => Lg.HasValue ? $"--lg:{Lg}" : string.Empty);
        StyleBuilder.Register(() => Xl.HasValue ? $"--xl:{Xl}" : string.Empty);
        StyleBuilder.Register(() => Xxl.HasValue ? $"--xxl:{Xxl}" : string.Empty);

        StyleBuilder.Register(() => Parent!.ColumnsXs.HasValue ? $"--columns-xs:{Parent.ColumnsXs}" : string.Empty);
        StyleBuilder.Register(() => Parent!.ColumnsSm.HasValue ? $"--columns-sm:{Parent.ColumnsSm}" : string.Empty);
        StyleBuilder.Register(() => Parent!.ColumnsMd.HasValue ? $"--columns-md:{Parent.ColumnsMd}" : string.Empty);
        StyleBuilder.Register(() => Parent!.ColumnsLg.HasValue ? $"--columns-lg:{Parent.ColumnsLg}" : string.Empty);
        StyleBuilder.Register(() => Parent!.ColumnsXl.HasValue ? $"--columns-xl:{Parent.ColumnsXl}" : string.Empty);
        StyleBuilder.Register(() => Parent!.ColumnsXxl.HasValue ? $"--columns-xxl:{Parent.ColumnsXxl}" : string.Empty);

        StyleBuilder.Register(() => $"--span:{ColumnSpan}");
    }
}
