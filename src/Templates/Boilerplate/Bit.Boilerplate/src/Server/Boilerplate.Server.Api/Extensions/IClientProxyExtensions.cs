namespace Microsoft.AspNetCore.SignalR;

public static class IClientProxyExtensions
{
    /// <summary>
    /// Publishing a shared app message <see cref="SharedAppMessages"/> to the client through SignalR.
    /// </summary>
    public static async Task Publish(this IClientProxy clientProxy, string sharedAppMessage, CancellationToken cancellationToken)
    {
        await clientProxy.SendAsync(SharedAppMessages.PUBLISH_MESSAGE, sharedAppMessage, null, cancellationToken);
    }

    /// <summary>
    /// Publishing a shared app message <see cref="SharedAppMessages"/> to the client through SignalR.
    /// </summary>
    public static async Task Publish(this IClientProxy clientProxy, string sharedAppMessage, object? args, CancellationToken cancellationToken)
    {
        await clientProxy.SendAsync(SharedAppMessages.PUBLISH_MESSAGE, sharedAppMessage, args, cancellationToken);
    }
}
