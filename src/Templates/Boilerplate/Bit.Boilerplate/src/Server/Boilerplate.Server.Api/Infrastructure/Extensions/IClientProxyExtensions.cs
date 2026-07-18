namespace Microsoft.AspNetCore.SignalR;

public static class IClientProxyExtensions
{
    extension(IClientProxy clientProxy)
    {
        /// <summary>
        /// Publishing a shared app message <see cref="SharedAppMessages"/> to the client through SignalR.
        /// </summary>
        public async Task Publish(string sharedAppMessage, CancellationToken cancellationToken)
        {
            await clientProxy.SendAsync(SharedAppMessages.PUBLISH_MESSAGE, sharedAppMessage, null, cancellationToken);
        }

        /// <summary>
        /// Publishing a shared app message <see cref="SharedAppMessages"/> to the client through SignalR.
        /// </summary>
        public async Task Publish(string sharedAppMessage, object? args, CancellationToken cancellationToken)
        {
            await clientProxy.SendAsync(SharedAppMessages.PUBLISH_MESSAGE, sharedAppMessage, args, cancellationToken);
        }
    }
}
