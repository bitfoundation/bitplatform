namespace Boilerplate.Client.Core.Components.Common;

public partial class ProductImage
{
    [CascadingParameter] public AppThemeType? CurrentTheme { get; set; }

    [Parameter] public string? Src { get; set; }
    [Parameter] public string? Alt { get; set; }
    [Parameter] public string? Width { get; set; }
    [Parameter] public string? Class { get; set; }
}
