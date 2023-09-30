namespace Bit.BlazorUI;

public partial class BitThemeProvider : ComponentBase
{
    /// <summary>
    /// The content of the ThemeProvider.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The element used for the root node.
    /// </summary>
    [Parameter] public string? RootElement { get; set; }

    /// <summary>
    /// The BitTheme instance used to customize the theme.
    /// </summary>
    [Parameter] public BitTheme? Theme { get; set; }

    /// <summary>
    /// The name of the cascading BitTheme value.
    /// </summary>
    [Parameter] public string? ThemeName { get; set; }


    [CascadingParameter] public BitTheme? ParentTheme { get; set; }



    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;

        if (Theme is null)
        {
            builder.AddContent(seq, ChildContent);
            return;
        }

        var mergedTheme = Theme!;

        if (ParentTheme is not null)
        {
            mergedTheme = BitThemeMapper.Merge(Theme, ParentTheme);
        }

        var cssVars = BitThemeMapper.MapToCssVariables(Theme);
        var cssVarStyle = string.Join(';', cssVars.Select(kv => $"{kv.Key}:{kv.Value}"));

        builder.OpenElement(seq++, RootElement ?? "div");
        builder.AddAttribute(seq++, "style", cssVarStyle);

        builder.OpenComponent<CascadingValue<BitTheme?>>(seq++);
        if (ThemeName is not null)
        {
            builder.AddAttribute(seq++, "Name", ThemeName);
            builder.AddAttribute(seq++, "Value", Theme);
        }
        else
        {
            builder.AddAttribute(seq++, "Value", mergedTheme);
        }
        builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(builder2 => builder2.AddContent(seq, ChildContent)));
        builder.CloseComponent();

        builder.CloseElement();

        base.BuildRenderTree(builder);
    }
}
