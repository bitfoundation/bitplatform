using Boilerplate.Client.Core.Services.HttpMessageHandlers;

namespace Boilerplate.Client.Core.Components;

public class Parameters
{
    /// <summary>
    /// Indicates the connection status, with default behavior tied to the SignalR connection status.
    /// <see cref="ExceptionDelegatingHandler"/> allows this value to be updated based on server responses:
    /// - When the first response is received from the server, this value becomes true (Online).
    /// - When a <see cref="ServerConnectionException"/> occurs, it becomes false (Offline).
    /// By default, this value is null (Unknown).
    /// </summary>
    public const string IsOnline = nameof(IsOnline);
}
