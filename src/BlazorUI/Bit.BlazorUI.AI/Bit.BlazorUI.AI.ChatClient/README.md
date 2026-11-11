### bit BlazorUI AI ChatClient: the Microsoft.Extensions.AI adapter of bit BlazorUI AI
[![NuGet Version](https://img.shields.io/nuget/v/Bit.BlazorUI.AI.ChatClient.svg?style=flat)](https://www.nuget.org/packages/Bit.BlazorUI.AI.ChatClient/) ![Nuget](https://img.shields.io/nuget/dt/Bit.BlazorUI.AI.ChatClient.svg)

Connects any `Microsoft.Extensions.AI` `IChatClient` to [bit BlazorUI AI](https://www.nuget.org/packages/Bit.BlazorUI.AI/):

- `StreamBitAiEventsAsync` / `ToBitAiEventsAsync` turn a chat client's reply into `BitAiEvent`s - text, reasoning,
  function calls and results, images, files, citations, usage, errors - for the endpoints of `Bit.BlazorUI.AI.Server`.
- `ToChatMessagesAsync` and `ToChatOptions` turn a `BitAiRequest` into chat messages and options; the server's own
  options always win over what the browser asks for.
- `UseChatClient()` runs an `IChatClient` as the transport right where the components run (Blazor Server, Hybrid).

```
dotnet add package Bit.BlazorUI.AI.ChatClient
```

```csharp
// On a server, with Bit.BlazorUI.AI.Server:
app.MapBitAiSse("/api/ai/chat", (request, http, ct) =>
    http.RequestServices.GetRequiredService<IChatClient>().StreamBitAiEventsAsync(request, chatOptions, ct));

// In a Blazor Server or Hybrid app, with Bit.BlazorUI.AI:
builder.Services.AddBitBlazorUIAIServices(ai => ai.UseChatClient());
```

It needs no UI and no ASP.NET Core, so the server and the app share one package.
