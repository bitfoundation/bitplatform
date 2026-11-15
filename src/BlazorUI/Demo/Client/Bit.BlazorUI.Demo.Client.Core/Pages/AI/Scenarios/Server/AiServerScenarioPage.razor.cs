namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Server;

public partial class AiServerScenarioPage
{
    private const string Sse = "sse";
    private const string WebSocket = "websocket";

    private string _transport = Sse;
    private BitAiChatSession? _sseSession;
    private BitAiChatSession? _webSocketSession;

    private readonly string[] _suggestions =
    [
        "What time is it on the server?",
        "Where does this reply come from?",
    ];

    private BitAiChatSession? Session => _transport == WebSocket ? _webSocketSession : _sseSession;

    private string EmptyDescription => _transport == WebSocket
        ? "The reply is generated on the server and streamed here over a WebSocket."
        : "The reply is generated on the server and streamed here as server-sent events.";

    protected override Task OnInitAsync()
    {
        // The demo's server maps both endpoints over a scripted IChatClient (Startup/Middlewares.cs of the server).
        var sse = new BitAiSseTransport(HttpClient, new BitAiSseTransportOptions { Endpoint = "api/ai/chat" });

        // A WebSocket needs an absolute address: the app's base address, whose http(s) the transport turns into ws(s).
        var webSocket = new BitAiWebSocketTransport(new Uri(new Uri(NavigationManager.BaseUri), "api/ai/ws"));

        // Tools off: the one tool of this reply runs on the server, and the site's own demo tools are for other scenarios.
        // A session per transport, so switching keeps each conversation.
        var options = new BitAiChatSessionOptions { ExecuteTools = false };
        _sseSession = new BitAiChatSession(sse, null, options);
        _webSocketSession = new BitAiChatSession(webSocket, null, options);

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (_sseSession is not null) await _sseSession.DisposeAsync();
        if (_webSocketSession is not null) await _webSocketSession.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
