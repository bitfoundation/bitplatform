namespace AdminPanel.Client.Core.Components.Layout;

public partial class RootContainer
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public BitDir? CurrentDir { get; set; }
    [Parameter] public string? CurrentUrl { get; set; }
    [Parameter] public bool? IsAuthenticated { get; set; }
    [Parameter] public bool? IsCrossLayoutPage { get; set; }
    [Parameter] public AppThemeType? CurrentTheme { get; set; }
    [Parameter] public RouteData? CurrentRouteData { get; set; }
}
