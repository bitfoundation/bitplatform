namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Server;

public partial class AiServerScenarioPage
{
    private BitAiChatSession? _session;

    private readonly string[] _suggestions =
    [
        "What time is it on the server?",
        "Where does this reply come from?",
    ];

    protected override Task OnInitAsync()
    {
        // The demo's server maps this endpoint over a scripted IChatClient (Startup/Middlewares.cs of the server).
        var transport = new BitAiSseTransport(HttpClient, new BitAiSseTransportOptions { Endpoint = "api/ai/chat" });

        // Tools off: the one tool of this reply runs on the server, and the site's own demo tools are for other scenarios.
        _session = new BitAiChatSession(transport, null, new BitAiChatSessionOptions { ExecuteTools = false });

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (_session is not null) await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
