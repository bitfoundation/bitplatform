using Boilerplate.Client.Core.Services.HttpMessageHandlers;

namespace Boilerplate.Client.Core.Components;

public class Parameters
{
    public const string CurrentDir = nameof(CurrentDir);
    public const string CurrentTheme = nameof(CurrentTheme);
    public const string IsAuthenticated = nameof(IsAuthenticated);
    public const string CurrentRouteData = nameof(CurrentRouteData);

    /// <summary>
    /// The cross-layout pages are the pages that are getting rendered in multiple layouts (authenticated and unauthenticated).
    /// The Terms and Home pages are examples of cross-layout pages that.
    /// </summary>
    public const string IsCrossLayoutPage = nameof(IsCrossLayoutPage);

    /// <summary>
    /// Indicates the connection status, with default behavior tied to the SignalR connection status.
    /// <see cref="ExceptionDelegatingHandler"/> allows this value to be updated based on server responses:
    /// - When the first response is received from the server, this value becomes true (Online).
    /// - When a server connection exception occurs, it becomes false (Offline).
    /// By default, this value is null (Unknown).
    /// </summary>
    public const string IsOnline = nameof(IsOnline);
}
