# Bit.Bcrdt.SignalR

The SignalR client transport for [bit Bcrdt](https://www.nuget.org/packages/Bit.Bcrdt). It carries no ASP.NET Core
server dependency, so it runs in Blazor WebAssembly, Server and Hybrid alike.

```bash
dotnet add package Bit.Bcrdt.SignalR
```

```csharp
using Bit.Bcrdt.SignalR;

// The .NET SignalR client needs an absolute hub URL; in WebAssembly resolve it against the host page.
string hubUrl = new Uri(new Uri(builder.HostEnvironment.BaseAddress), "hub/bcrdt").ToString();

builder.Services.AddBitBcrdtSignalRTransport(hubUrl);
builder.Services.AddBitBcrdtServices();
```

The server maps the matching hub from [Bit.Bcrdt.Server](https://www.nuget.org/packages/Bit.Bcrdt.Server):

```csharp
builder.Services.AddBitBcrdtHub();
app.MapHub<BcrdtHub>("/hub/bcrdt");
```

The optional `configure` argument is applied to each connection's builder, for access tokens or the MessagePack
protocol. Leave SignalR's own automatic reconnect off: bit Bcrdt's sync provider owns reconnection, and needs to see
the drop to re-run the handshake.

The full guide is in the [Bcrdt README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Bcrdt).
