namespace Microsoft.AspNetCore.SignalR.Client;

public static class HubConnectionExtensions
{
    extension(HubConnection hubConnection)
    {
        public async Task EnsureConnected(PubSubService pubSubService, CancellationToken cancellationToken)
        {
            if (hubConnection.State is HubConnectionState.Connected) return;

            TaskCompletionSource connectionAttempt = new(TaskCreationOptions.RunContinuationsAsynchronously);

            pubSubService.Publish(ClientAppMessages.CONNECT_SIGNALR, connectionAttempt, persistent: true);

            await connectionAttempt.Task.WaitAsync(cancellationToken);
        }
    }
}
