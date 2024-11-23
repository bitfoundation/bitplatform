namespace Microsoft.AspNetCore.SignalR.Client;

public static class HubConnectionExtensions
{
    /// <summary>
    /// <inheritdoc cref="SignalREvents.PONG"/>"/>
    /// </summary>
    public static async Task<bool> IsUserSessionUnique(this HubConnection hubConnection, CancellationToken cancellationToken)
    {
        IDisposable? disposable = null; TaskCompletionSource? pongTcs = new();

        try
        {
            if (hubConnection.State is not HubConnectionState.Connected)
                throw new ServerConnectionException(AppStrings.ServerConnectionException);

            disposable = hubConnection.On(SignalREvents.PONG, () =>
            {
                pongTcs!.SetResult();
            });

            await hubConnection.InvokeAsync("Ping", cancellationToken);

            return await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(3), cancellationToken), pongTcs!.Task) != pongTcs.Task;
        }
        finally
        {
            disposable?.Dispose();
        }
    }
}
