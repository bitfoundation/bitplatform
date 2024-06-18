namespace Bit.BlazorUI;

public partial class BitGridItem : BitComponentBase
{
    private int columnSpan = 1;


    [CascadingParameter] protected BitGrid? Parent { get; set; }


    /// <summary>
    /// The content of the GridItem.
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

        StyleBuilder.Register(() => $"--span:{ColumnSpan}");
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;
        builder.OpenElement(seq++, "div");
        builder.AddMultipleAttributes(seq++, Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<IEnumerable<KeyValuePair<string, object>>>(HtmlAttributes));
        builder.AddAttribute(seq++, "id", _Id);
        builder.AddAttribute(seq++, "style", StyleBuilder.Value);
        builder.AddAttribute(seq++, "class", ClassBuilder.Value);
        builder.AddAttribute(seq++, "dir", Dir?.ToString().ToLower());
        builder.AddElementReferenceCapture(seq++, v => RootElement = v);

        builder.AddContent(seq++, ChildContent);

        builder.CloseElement();

        base.BuildRenderTree(builder);
    }
}
