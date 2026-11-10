namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Server;

public partial class AiServerScenarioPage
{
    private readonly string example1RazorCode = """
<div style="height: 560px">
    <BitAiChat EmptyTitle="Ask the server"
               EmptyBody="The reply is generated on the server and streamed here as server-sent events." />
</div>
""";

    private readonly string example1CsharpCode = """
// Nothing: BitAiChat creates its session from the transport registered in the browser (Program.cs).
""";

    private const string example1ProgramCode = """
// Program.cs of the browser (WebAssembly, Server or Hybrid): the reply streams from the server's endpoint.
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseSseTransport(sp => sp.GetRequiredService<HttpClient>(), new() { Endpoint = "api/ai/chat" });
});
""";

    private const string example1ServerCode = """
using Microsoft.Extensions.AI;

// Program.cs of the server: the provider's chat client, and the endpoint streaming its replies.
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
""";

    private const string example1HubCode = """
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
