### bit BlazorUI AI Server: the ASP.NET Core ends of the bit BlazorUI AI transports
[![NuGet Version](https://img.shields.io/nuget/v/Bit.BlazorUI.AI.Server.svg?style=flat)](https://www.nuget.org/packages/Bit.BlazorUI.AI.Server/) ![Nuget](https://img.shields.io/nuget/dt/Bit.BlazorUI.AI.Server.svg)

Streams a reply to [bit BlazorUI AI](https://www.nuget.org/packages/Bit.BlazorUI.AI/) components from an ASP.NET Core
server, in the packages' wire format. No component, stylesheet or script reaches the server through it.

```
dotnet add package Bit.BlazorUI.AI.Server
dotnet add package Bit.BlazorUI.AI.ChatClient   # to stream an IChatClient's reply
```

```csharp
// Server-sent events: the browser registers UseSseTransport.
app.MapBitAiSse("/api/ai/chat", (request, http, ct) =>
        http.RequestServices.GetRequiredService<IChatClient>().StreamBitAiEventsAsync(request, chatOptions, ct))
   .RequireAuthorization();

// A WebSocket per reply: the browser registers UseWebSocketTransport. A decision on a tool call the server holds
// comes back up the same socket. Authenticated by cookie, the endpoint lists the origins it serves.
app.MapBitAiWebSocket("/api/ai/ws", (request, http, ct) =>
        http.RequestServices.GetRequiredService<IChatClient>().StreamBitAiEventsAsync(request, chatOptions, ct),
        new() { WebSockets = new() { AllowedOrigins = { "https://my.app" } } })
   .RequireAuthorization();

// SignalR: derive a hub, map it, and the browser registers UseSignalRTransport (Bit.BlazorUI.AI.SignalR).
public sealed class AiHub(IChatClient chatClient) : BitAiHub
{
    protected override IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken ct) =>
        chatClient.StreamBitAiEventsAsync(request, cancellationToken: ct);
}
```

A tool call the server runs only after the user decides is held with `BitAiApprovalBroker`
(`services.AddSingleton<BitAiApprovalBroker>()`): stream a `BitAiToolApprovalRequestedEvent`, then
`await approvals.WaitAsync(toolCallId, ct)`. The decision arrives through `MapBitAiApprovals` (SSE), the WebSocket, or
the hub.
