namespace Boilerplate.Client.Core.Components.Layout;

public partial class RootContainer
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// <inheritdoc cref="Parameters.IsOnline"/>
    /// </summary>
    [Parameter] public bool? IsOnline { get; set; }
    [Parameter] public BitDir? CurrentDir { get; set; }
    [Parameter] public string? CurrentUrl { get; set; }
    [Parameter] public bool? IsAuthenticated { get; set; }
    /// <summary>
    /// <inheritdoc cref="Parameters.IsCrossLayoutPage"/>
    /// </summary>
    [Parameter] public bool? IsCrossLayoutPage { get; set; }
    [Parameter] public AppThemeType? CurrentTheme { get; set; }
    [Parameter] public RouteData? CurrentRouteData { get; set; }
}
