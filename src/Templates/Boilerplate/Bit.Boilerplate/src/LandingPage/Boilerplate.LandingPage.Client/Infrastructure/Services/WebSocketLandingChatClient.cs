using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace Boilerplate.LandingPage.Client.Infrastructure.Services;

internal sealed partial class WebSocketLandingChatClient : ILandingChatClient, IAsyncDisposable
{
    [AutoInject] private NavigationManager navigationManager = default!;
    [AutoInject] private IStringLocalizer<LandingStrings> localizer = default!;

    private Connection? connection;

    public async IAsyncEnumerable<string> AskAsync(LandingChatAsk ask, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (connection?.IsOpen is not true)
        {
            if (connection is not null)
            {
                await connection.DisposeAsync();
            }

            var uri = new UriBuilder(navigationManager.ToAbsoluteUri("api/chat"));
            uri.Scheme = uri.Scheme == Uri.UriSchemeHttps ? "wss" : "ws";
            connection = await Connection.OpenAsync(uri.Uri, cancellationToken);
        }

        await connection.SendAsync(ask, cancellationToken);

        while (true)
        {
            var frame = await connection.ReceiveAsync(cancellationToken)
                ?? throw new LandingChatException(localizer[nameof(LandingStrings.ChatConnectionLost)], retryable: true);

            if (frame.Error is not null)
                throw new LandingChatException(frame.Error, frame.Retryable);

            if (frame.Delta is not null)
                yield return frame.Delta;

            if (frame.Done)
                yield break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
        }
    }

    private sealed class Connection(ClientWebSocket socket) : IAsyncDisposable
    {
        private readonly byte[] buffer = new byte[16 * 1024];

        public bool IsOpen => socket.State is WebSocketState.Open;

        public static async Task<Connection> OpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            var socket = new ClientWebSocket();
            await socket.ConnectAsync(uri, cancellationToken);
            return new(socket);
        }

        public async Task SendAsync(LandingChatAsk ask, CancellationToken cancellationToken)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(ask, LandingJsonContext.Default.LandingChatAsk);
            await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
        }

        public async Task<LandingChatFrame?> ReceiveAsync(CancellationToken cancellationToken)
        {
            using var stream = new MemoryStream();

            while (true)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType is WebSocketMessageType.Close)
                    return null;

                stream.Write(buffer, 0, result.Count);

                if (result.EndOfMessage)
                    break;
            }

            stream.Position = 0;
            return JsonSerializer.Deserialize(stream, LandingJsonContext.Default.LandingChatFrame);
        }

        public async ValueTask DisposeAsync()
        {
            if (socket.State is WebSocketState.Open)
            {
                try
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                }
                catch (WebSocketException) { }
            }

            socket.Dispose();
        }
    }
}
