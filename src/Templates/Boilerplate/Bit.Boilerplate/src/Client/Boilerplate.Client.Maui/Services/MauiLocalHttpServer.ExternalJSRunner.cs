using EmbedIO.WebSockets;

namespace Boilerplate.Client.Maui.Services;

/// <summary>
/// Checkout external-js-runner.html
/// </summary>
public class ExternalJSRunnerWebSocketModule : WebSocketModule
{
    public static Func<JsonDocument, Task<JsonDocument>>? RequestToBeSent { get; private set; }

    TaskCompletionSource<JsonDocument>? responseTcs;
    TaskCompletionSource<IWebSocketContext> webSocketTcs = new();

    public ExternalJSRunnerWebSocketModule()
        : base("/external-js-runner", true)
    {
        RequestToBeSent += SendRequest;
    }

    private async Task<JsonDocument> SendRequest(JsonDocument document)
    {
        responseTcs = new();
        var webSocket = await webSocketTcs.Task;
        await SendAsync(webSocket, document.RootElement.GetRawText());
        return await responseTcs.Task;
    }

    protected override async Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result)
    {
        var response = JsonDocument.Parse(System.Text.Encoding.UTF8.GetString(buffer));
        responseTcs!.SetResult(response);
    }

    protected override async Task OnClientConnectedAsync(IWebSocketContext context)
    {
        await base.OnClientConnectedAsync(context);
        webSocketTcs.SetResult(context);
    }

    protected override async Task OnClientDisconnectedAsync(IWebSocketContext context)
    {
        webSocketTcs.TrySetCanceled();
        webSocketTcs = new();
        await base.OnClientDisconnectedAsync(context);
    }

    protected override void Dispose(bool disposing)
    {
        RequestToBeSent -= SendRequest;
        base.Dispose(disposing);
    }
}
