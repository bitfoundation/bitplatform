namespace Boilerplate.Client.Core.Components;

public class Parameters
{
    public const string CurrentDir = nameof(CurrentDir);
    public const string CurrentUrl = nameof(CurrentUrl);
    public const string CurrentTheme = nameof(CurrentTheme);
    public const string IsAuthenticated = nameof(IsAuthenticated);
    public const string CurrentRouteData = nameof(CurrentRouteData);

    /// <summary>
    /// If the current page is part of the cross-layout pages that are rendered in multiple layouts.
    /// </summary>
    public const string IsCrossLayoutPage = nameof(IsCrossLayoutPage);

    /// <summary>
    /// Determines the connection status, with default behavior based on SignalR connection status.
    /// If SignalR is not added to the project during initial project creation, this value will always be true by default.
    /// Alternatively, you can implement custom logic to control this value.
    /// </summary>
    public const string IsOnline = nameof(IsOnline);
}
