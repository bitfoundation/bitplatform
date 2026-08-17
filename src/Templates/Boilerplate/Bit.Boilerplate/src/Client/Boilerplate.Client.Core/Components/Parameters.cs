namespace Boilerplate.Client.Core.Components;

public class Parameters
{
    /// <summary>
    /// Indicates the connection status. <see cref="ExceptionDelegatingHandler"/> drives it from server responses:
    /// - When the first response is received from the server, this value becomes true (Online).
    /// - When a <see cref="TransientException"/> occurs, it becomes false (Offline).
    /// By default, this value is null (Unknown), and it only changes when a request is actually made - nothing polls.
    /// When the app is built with SignalR, the hub connection state drives it as well (see AppClientCoordinator).
    /// </summary>
    public const string IsOnline = nameof(IsOnline);
}
