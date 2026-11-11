# Bit.Bcrdt.Server

The ASP.NET Core relays for [bit Bcrdt](https://www.nuget.org/packages/Bit.Bcrdt): the SignalR hub the
`Bit.Bcrdt.SignalR` transport talks to, and the WebSocket endpoint the WebSocket transport built into `Bit.Bcrdt` talks
to. Both only relay - neither decodes a message, holds a replica or knows what a state vector is.

```bash
dotnet add package Bit.Bcrdt.Server
```

```csharp
using Bit.Bcrdt.Server;

builder.Services.AddBitBcrdtHub();   // AddSignalR, with a receive limit sized for sync messages
// ...
app.UseWebSockets();
app.MapHub<BcrdtHub>("/hub/bcrdt");
app.MapBitBcrdtWebSocket("/ws/bcrdt/{documentId}");
```

- Use `AddBitBcrdtHub` rather than a bare `AddSignalR`: SignalR's default 32 KB receive limit is smaller than the
  handshake reply for any document of real size.
- Decide who may work on a document by deriving from `BcrdtHub` and overriding `AuthorizeAsync`, or by passing
  `authorize` to `MapBitBcrdtWebSocket`. A refusal reaches the client as a final
  `BcrdtTransportAuthorizationException` rather than a failure to retry.
- End or narrow a grant while the connection is open through `BcrdtRelayConnections`, which both relays list their
  connections in - registered by `AddBitBcrdtHub`, or by `AddBitBcrdtRelayConnections` in an application that maps
  only the WebSocket endpoint. `RevokeAsync` refuses the matching connections, in a `BcrdtDocumentHost` too when there
  is one, and `MakeReadOnlyAsync` makes them read-only without disconnecting them. Change the authorization hook
  first, and revoke on every instance behind a backplane: the list is per process.
- The hub scales out with a SignalR backplane. The WebSocket relay's rooms are process-local: use sticky sessions or
  a broker behind a load balancer. Every WebSocket client gets its own bounded outbound queue, so one slow client
  cannot hold up the others.
- In Blazor Server, `builder.Services.AddBitBcrdtCircuitPresence()` withdraws a user from everybody's presence list
  as soon as their browser connection drops, rather than when their circuit expires minutes later.

The full guide is in the [Bcrdt README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bcrdt).
