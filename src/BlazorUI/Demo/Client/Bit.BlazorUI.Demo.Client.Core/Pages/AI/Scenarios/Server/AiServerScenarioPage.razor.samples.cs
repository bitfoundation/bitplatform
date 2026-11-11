namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Server;

public partial class AiServerScenarioPage
{
    private readonly string example1RazorCode = """
<div style="height: 560px">
    <BitAiChat EmptyTitle="Ask the server"
               EmptyBody="The reply is generated on the server and streamed here." />
</div>
""";

    private readonly string example1CsharpCode = """
// Nothing: BitAiChat creates its session from the transport registered in the browser (Program.cs).
""";

    private const string example1ProgramCode = """
// The Blazor app (WebAssembly, Server or Hybrid): dotnet add package Bit.BlazorUI.AI
// Program.cs - the reply streams from the server's endpoint. One transport, either of these:
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    // Server-sent events: a POST per reply, its events read from the response.
    ai.UseSseTransport(sp => sp.GetRequiredService<HttpClient>(), new() { Endpoint = "api/ai/chat" });

    // Or a WebSocket per reply, which also carries the decisions on the tool calls the server holds.
    // ai.UseWebSocketTransport(sp => new Uri(new Uri(sp.GetRequiredService<NavigationManager>().BaseUri), "api/ai/ws"));
});
""";

    private const string example1ServerCode = """
// The server: dotnet add package Bit.BlazorUI.AI.Server
//             dotnet add package Bit.BlazorUI.AI.ChatClient
using Microsoft.Extensions.AI;

// Program.cs - the provider's chat client, and the endpoints streaming its replies.
builder.Services.AddChatClient(sp => new OpenAI.Chat.ChatClient("gpt-5-mini", apiKey).AsIChatClient())
    .UseFunctionInvocation();

var app = builder.Build();

// Pinned by the server: a browser's request only fills in what these leave unset.
var chatOptions = new ChatOptions
{
    ModelId = "gpt-5-mini",
    MaxOutputTokens = 2048,
    Tools = [AIFunctionFactory.Create(() => DateTimeOffset.UtcNow.ToString("HH:mm:ss 'UTC'"), "get_server_time", "Reads the server's clock.")],
};

app.MapBitAiSse("/api/ai/chat", (request, http, cancellationToken) =>
        http.RequestServices.GetRequiredService<IChatClient>().StreamBitAiEventsAsync(request, chatOptions, cancellationToken))
   .RequireAuthorization();

// The same handler over a WebSocket. Authenticated by cookie, it lists the origins it serves.
app.MapBitAiWebSocket("/api/ai/ws", (request, http, cancellationToken) =>
        http.RequestServices.GetRequiredService<IChatClient>().StreamBitAiEventsAsync(request, chatOptions, cancellationToken),
        new() { WebSockets = new() { AllowedOrigins = { "https://my.app" } } })
   .RequireAuthorization();
""";

    private const string example1HubCode = """
// The server, as above. The browser adds: dotnet add package Bit.BlazorUI.AI.SignalR
using Microsoft.Extensions.AI;

// The same reply over SignalR: map the hub (app.MapHub<AiHub>("/hubs/ai")) and, in the browser,
// ai.UseSignalRTransport(sp => sp.GetRequiredService<HubConnection>()) with a connection to it.
public sealed class AiHub(IChatClient chatClient) : BitAiHub
{
    protected override IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken) =>
        chatClient.StreamBitAiEventsAsync(request, cancellationToken: cancellationToken);
}
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("Program.cs", example1ProgramCode),
        new("Server/Program.cs", example1ServerCode),
        new("AiHub.cs", example1HubCode),
    ];
}
