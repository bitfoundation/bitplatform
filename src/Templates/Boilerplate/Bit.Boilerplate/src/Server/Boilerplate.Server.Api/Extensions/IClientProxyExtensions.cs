namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Sending messages to the client's hub proxy from the server.
/// Client is listening to the hub proxy in Client/Core's ClientAppCoordinator.cs
/// </summary>
public static class IClientProxyExtensions
{
    /// <summary>
    /// Sends a message to the clients that will published through PubSubService.
    /// </summary>
    /// <param name="connections">All or specific group or specific user</param>
    /// <param name="messageKey">A key of <see cref="SharedPubSubMessages"/></param>
    public static async Task PublishMessage(this IClientProxy connections, string messageKey, CancellationToken cancellationToken)
    {
        await connections.SendAsync("PUBLISH_MESSAGE", messageKey, cancellationToken);
    }

    /// <summary>
    /// Shows message at client side.
    /// </summary>
    public static async Task ShowMessage(this IClientProxy connections, string messageKey, CancellationToken cancellationToken)
    {
        await connections.SendAsync("SHOW_MESSAGE", messageKey, cancellationToken);
    }
}
