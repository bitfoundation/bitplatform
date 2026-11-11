### bit BlazorUI AI: The AI components package of bit BlazorUI
[![NuGet Version](https://img.shields.io/nuget/v/Bit.BlazorUI.AI.svg?style=flat)](https://www.nuget.org/packages/Bit.BlazorUI.AI/) ![Nuget](https://img.shields.io/nuget/dt/Bit.BlazorUI.AI.svg)


**bit BlazorUI AI** offers the UI of AI apps for Blazor: conversations, streaming, tool calling, agents, citations,
artifacts, structured output and generative UI - as one coherent set of components rather than a pile of them.

**Why choose bit BlazorUI AI components?**
- **Native C# and Blazor throughout**: no JavaScript AI library is wrapped, no provider SDK is referenced, and no
  component knows which model produced what it is drawing.
- **Built for streaming**: Markdown that is still being written, JSON that is not valid yet, tool calls that have to be
  shown, approved, run and answered, and transcripts of hundreds of messages that never re-render the world per token.
- **Part of bit BlazorUI**: built on its components, styled by its theme tokens - dark and light, the Fluent 2,
  Material and Cupertino presets and right-to-left come for free.
- **Universal Support**: Compatible with all interactive Blazor modes - **Server**, **WASM**, and **Hybrid**.

### Getting Started

To use bit BlazorUI AI components, follow these steps:

1. Install the `Bit.BlazorUI.AI` nuget package.

using command line:

```
dotnet add package Bit.BlazorUI.AI
```

or using Package Manager Console:

```
Install-Package Bit.BlazorUI.AI
```

2. Register its services, together with the transport that talks to your model (in every `Program.cs` that renders
components):

```csharp
builder.Services.AddBitBlazorUIAIServices(ai => ai.UseTransport<MyTransport>());
```

3. In the default document (`App.razor` or `index.html`), add the `Bit.BlazorUI.AI` CSS file reference after the core
one:

```
<link rel="stylesheet" href="_content/Bit.BlazorUI.AI/styles/bit.blazorui.ai.css" />
```

4. In the default document (`App.razor` or `index.html`), add the `Bit.BlazorUI.AI` JS file reference after the core
one:

```
<script src="_content/Bit.BlazorUI.AI/scripts/bit.blazorui.ai.js"></script>
```

5. In the `_Imports.razor`, add the using `Bit.BlazorUI` to make it available throughout the project.

```
@using Bit.BlazorUI;
```

6. Start using BlazorUI AI components following its documents: https://blazorui.bitplatform.dev

### Downloading it only when the chat opens

A Blazor WebAssembly app can lazy-load `Bit.BlazorUI.AI.wasm` and `Bit.BlazorUI.AI.Core.wasm`
(`BlazorWebAssemblyLazyLoad`) with the pages that render the components. Nothing of the package has to be registered
for them to render, so `Program.cs` calls `AddBitBlazorUIServices()` alone, and the chat's page hands it its session
(`<BitAiChat Session="..." />`) and, where it wants them, its options and renderers. The setup, and what each part costs
a trimmed app, are on the Trimming page: https://blazorui.bitplatform.dev/trimming#lazy-ai

### The one interface you implement

Everything the components do is downstream of a single boundary. Implement it over a provider SDK, an HTTP endpoint, a
local model, an orchestrator, or a fixture in a test:

```csharp
public sealed class MyTransport : IBitAiTransport
{
    public async IAsyncEnumerable<BitAiEvent> StreamAsync(
        BitAiRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new BitAiMessageStartedEvent();

        await foreach (var token in _model.StreamAsync(request, cancellationToken))
        {
            yield return new BitAiTextDeltaEvent(token);
        }

        yield return new BitAiUsageUpdatedEvent(new BitAiTokenUsage { OutputTokens = 128 });
        yield return new BitAiMessageCompletedEvent();
    }
}
```

Then a working chat - transcript, composer, streaming Markdown, tool cards, error recovery, keyboard and screen-reader
support - is one line:

```razor
<BitAiChat />
```

### The packages

The AI packages are split by what each end of an app runs, so an app takes on only the dependencies of what it installs:

| Package | What it holds | Install it in |
|---|---|---|
| `Bit.BlazorUI.AI` | The components. Brings `Bit.BlazorUI.AI.Core`. | The Blazor app |
| `Bit.BlazorUI.AI.Core` | The transport contract, the events and their wire format, the conversation model, the chat session, the SSE and WebSocket client transports. No UI, no ASP.NET Core. | Comes with the one above; a server or a plain .NET client on its own |
| `Bit.BlazorUI.AI.Server` | `MapBitAiSse`, `MapBitAiWebSocket`, `MapBitAiApprovals`, `BitAiHub`, `BitAiApprovalBroker`. | The ASP.NET Core server |
| `Bit.BlazorUI.AI.ChatClient` | The `Microsoft.Extensions.AI` adapter (`StreamBitAiEventsAsync`, `UseChatClient`). | The server, or a Blazor Server / Hybrid app calling its model directly |
| `Bit.BlazorUI.AI.SignalR` | The SignalR client transport (`UseSignalRTransport`). | The Blazor app, to stream from a `BitAiHub` |

### Streaming from a server with Microsoft.Extensions.AI

With the model behind an `IChatClient` on your server, you write no transport at all. The packages carry the reply to
the browser in their own wire format, and map the chat client's updates to events for you.

On the server (`Bit.BlazorUI.AI.Server` and `Bit.BlazorUI.AI.ChatClient`):

```csharp
builder.Services.AddChatClient(/* your provider's IChatClient */).UseFunctionInvocation();

app.MapBitAiSse("/api/ai/chat", (request, http, cancellationToken) =>
        http.RequestServices.GetRequiredService<IChatClient>()
            .StreamBitAiEventsAsync(request, new ChatOptions { ModelId = "my-model" }, cancellationToken))
   .RequireAuthorization();
```

In the browser:

```csharp
builder.Services.AddBitBlazorUIAIServices(ai =>
    ai.UseSseTransport(sp => sp.GetRequiredService<HttpClient>(), new() { Endpoint = "api/ai/chat" }));
```

- **Wire format**: each `BitAiEvent` is one JSON object told apart by its `type` (`"text.delta"`,
  `"tool_call.started"`, ...), and a request is the transcript as the message snapshots a saved conversation holds.
  `BitAiWireSerializer` reads and writes both, through source-generated metadata, so trimmed and native AOT apps work
  on both ends. The format is versioned (`BitAiWireSerializer.Version`) and only grows: a reader skips a property it
  does not know, and reads an event of an unknown `type` as `null`, which the transports drop.
- **Microsoft.Extensions.AI** (`Bit.BlazorUI.AI.ChatClient`): `IChatClient.StreamBitAiEventsAsync` and
  `ToBitAiEventsAsync` turn a reply into events (text, reasoning, function calls and results, images, files, citations,
  usage, errors). `ToChatMessagesAsync` and `ToChatOptions` turn a request into chat messages and options. The server's
  own options always win over what the browser asks for. A tool registered in the browser is declared to the model, and
  its call comes back for the chat session to run. `UseChatClient()` uses an `IChatClient` right where the components
  run (Blazor Server, Hybrid).
- **WebSocket**: `MapBitAiWebSocket` maps the same handler on a WebSocket endpoint and `UseWebSocketTransport` opens a
  socket per reply to it. A decision on a tool call the server holds goes back up that socket, so it reaches the
  instance holding the call with no sticky session. An endpoint authenticated by cookie lists the origins it serves
  (`BitAiWebSocketEndpointOptions.WebSockets`).
- **SignalR** (`Bit.BlazorUI.AI.SignalR` in the browser): derive a hub from `BitAiHub`, override its `StreamAsync`, and
  register `UseSignalRTransport` with a connection to it.
- **Approvals the server holds**: every transport implements `IBitAiApprovalTransport`. The server streams a
  `BitAiToolApprovalRequestedEvent` and then awaits `BitAiApprovalBroker.WaitAsync`. The user's decision reaches it
  through `MapBitAiApprovals`, the WebSocket or the hub.
