### bit BlazorUI AI SignalR: the SignalR transport of bit BlazorUI AI
[![NuGet Version](https://img.shields.io/nuget/v/Bit.BlazorUI.AI.SignalR.svg?style=flat)](https://www.nuget.org/packages/Bit.BlazorUI.AI.SignalR/) ![Nuget](https://img.shields.io/nuget/dt/Bit.BlazorUI.AI.SignalR.svg)

Streams each reply of [bit BlazorUI AI](https://www.nuget.org/packages/Bit.BlazorUI.AI/)'s chat from a `BitAiHub`
(`Bit.BlazorUI.AI.Server`) over a SignalR connection the app already has. It carries no ASP.NET Core server dependency,
so it runs in Blazor WebAssembly, Server and Hybrid alike.

```
dotnet add package Bit.BlazorUI.AI.SignalR
```

```csharp
builder.Services.AddBitBlazorUIAIServices(ai =>
    ai.UseSignalRTransport(sp => sp.GetRequiredService<HubConnection>()));
```

A request waits for a connection that is starting or reconnecting, and starts a disconnected one unless
`BitAiSignalRTransportOptions.StartConnection` is off. Stopping a reply cancels the hub's stream; a decision on a tool
call the server holds goes to the hub over the same connection.
