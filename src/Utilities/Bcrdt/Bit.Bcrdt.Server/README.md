# Bit.Bcrdt.Server

The ASP.NET Core relays for [bit Bcrdt](https://www.nuget.org/packages/Bit.Bcrdt): the SignalR hub the
`Bit.Bcrdt.SignalR` transport talks to, and the WebSocket endpoint the WebSocket transport built into `Bit.Bcrdt` talks
to. Both only relay - neither decodes a message, holds a replica or knows what a state vector is.

```bash
dotnet add package Bit.Bcrdt.Server
```

```csharp
using Bit.Bcrdt;

builder.Services.AddBitBcrdtHub();   // AddSignalR, with a receive limit sized for sync messages
// ...
app.UseWebSockets();
app.MapHub<BcrdtHub>("/hub/bcrdt");
app.MapBitBcrdtWebSocket("/ws/bcrdt/{documentId}");
```

- Use `AddBitBcrdtHub` rather than a bare `AddSignalR`: SignalR's default 32 KB receive limit is smaller than the
  handshake reply for any document of real size.
- Decide who may work on a document by deriving from `BcrdtHub` and overriding `AuthorizeAsync`, or by passing
  `authorize` to `MapBitBcrdtWebSocket`. A refusal reaches the client as a final refusal
  (`AuthorizationDenied`, then `Faulted`) rather than a failure to retry: both relays send it as an `Auth` message
  granting nothing, which a browser's WebSocket hears as well as any other client.
- One hub connection may join at most 16 documents, with identifiers of at most 256 characters; register a
  `BcrdtHubOptions` of your own to change either. The WebSocket endpoint takes its own limit on identifiers in
  `BcrdtWebSocketEndpointOptions.MaxDocumentIdLength`, and pings its clients (`KeepAliveInterval`,
  `KeepAliveTimeout`) so a connection that died without a word is dropped within 45 seconds.
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
