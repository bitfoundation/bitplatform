# Bit.Bcrdt

Real-time collaborative editing for .NET and Blazor, written natively in C#: a CRDT engine, a synchronization protocol,
presence, offline-first persistence, selective undo and Blazor components. There is no JavaScript CRDT library
underneath and no port of one - the engine, the wire format and the merge algorithm are all C#, and none of it needs
JavaScript interop, so it runs the same in Blazor WebAssembly, Server and Hybrid, in a worker or in a console app.

## Quick start

```bash
dotnet add package Bit.Bcrdt
```

```csharp
using Bit.Bcrdt;

builder.Services.AddBitBcrdtServices(collaboration => collaboration
    .UseTransport(new BcrdtWebSocketTransportFactory("https://example.com/ws/bcrdt/{documentId}"))
    .UseStore(new BcrdtFileSystemDocumentStore("./documents")));
```

```csharp
BcrdtCollaborativeDocument document = await collaboration.OpenAsync("design-review");

using (var transaction = document.BeginTransaction())
{
    document.Map.Set("title", "Q3 planning");
    document.Array("attendees").Push("Ada", "Grace");
    document.Text("notes").Insert(0, "Agenda:\n");
}
```

Two people running that against the same document identifier both end up with the same document, whether they were
online at the time or not. For the SignalR transport add `Bit.Bcrdt.SignalR`; for the relay a server maps, add
`Bit.Bcrdt.Server`.

## Blazor

```razor
@using Bit.Bcrdt
@using Bit.Bcrdt.Blazor

<BcrdtCollaborationScope DocumentId="notes-42" User="_me" Context="document">
    <BcrdtPresenceView Presence="document.Presence" />
    <BcrdtTextArea Text="@document.Text("body")" Presence="document.Presence" />
</BcrdtCollaborationScope>

@code {
    private readonly BcrdtCollaboratorInfo _me = new("u-1", "Ada", BcrdtPresenceService.SuggestColor("u-1"));
}
```

That is a working collaborative editor with live presence, automatic reconnection and offline support.

- `BcrdtCollaborationScope` opens a document for its content, cascades it, and closes it again.
- `BcrdtTextArea` edits a `BcrdtText`: what the user types becomes the minimal CRDT edit, remote edits arrive without
  moving the caret or overwriting keystrokes still in flight, and the caret is published through presence.
- `BcrdtPresenceView` lists the people on a document.
- `BcrdtComponentBase` marshals change notifications onto the renderer and coalesces them onto a frame budget, and
  disposes its subscriptions with the component - the base for your own collaborative components.

No stylesheet to reference and no script to add: the one small module `BcrdtTextArea` needs is imported on demand.
Works in Blazor WebAssembly, Server, Web App (including prerendering) and Hybrid.

## Features

- A YATA-family sequence CRDT with run-length-encoded blocks: `BcrdtText` (with rich-text marks), `BcrdtArray`,
  `BcrdtMap` and `BcrdtSet`, nestable inside each other.
- Incremental binary updates that merge without a document, and a symmetric, loss-, duplication- and
  reorder-tolerant sync protocol with jittered reconnection - offline work flows both ways on reconnect, deletions
  included.
- Presence: who is here, colours, and carets and selections anchored to content rather than to indices.
- Append-only, checksummed persistence with compaction; in-memory and file-system stores.
- Selective undo that reverses your own edits and leaves everyone else's alone.
- Snapshots that restore any point in a document's history.
- `BcrdtTextSynchronizer` and `BcrdtTextInputBinding`, which turn an input's whole new value into the minimal edit.
- In-memory and WebSocket client transports; every seam - transport, store, client identity - is an interface.
- .NET 8, 9 and 10; trim- and AOT-safe for the collaborative value model.

## For AI agents

The demo site hosts an MCP server at `https://bcrdt.bitplatform.dev/mcp` (also served through
`https://bitplatform.dev/mcp`) that answers from the shipped library: its API, its guide, the demo's working pages,
and a merge inspector that runs the real engine.

## Documentation

The full guide - choosing a data type, documents and transactions, input bindings, presence, offline and
reconnection, persistence, undo, hosting models, transports and relays, extending the library and the architecture -
is in the [Bcrdt README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bcrdt). See it running at
[bcrdt.bitplatform.dev](https://bcrdt.bitplatform.dev).
