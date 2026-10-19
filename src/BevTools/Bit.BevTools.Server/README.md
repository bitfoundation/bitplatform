# Bit.BevTools.Server

Blazor Server (interactive server / circuits) instrumentation for
[bit BevTools](https://github.com/bitfoundation/bitplatform/tree/develop/src/BevTools): circuit lifecycle, connection
state, inbound message processing, reconnect attempts, an active circuit overview, and the diagnostic rules that read
them (an unstable or blocked circuit).

```csharp
// Program.cs of the server project
using Bit.BevTools;

builder.Services.AddBitBevToolsServerServices();
```

It registers everything `Bit.BevTools` does plus a `CircuitHandler`, and adds a Circuit panel to `<BevToolsPanel />`.
In an Interactive Auto solution, also call `AddBitBevToolsServices()` in the `.Client` project, so BevTools works
whichever render mode the page ends up in. Each circuit gets its own BevTools session; other circuits are visible only
as aggregates (truncated id, state, counts).
