# bit Bcrdt

Real-time collaborative editing for .NET and Blazor, implemented natively in C#.

A CRDT engine, a synchronization protocol, presence and awareness, offline-first persistence, selective undo and
Blazor integration - with **no JavaScript CRDT library, no Yjs, and no interop in the engine**. The only JavaScript
in bit Bcrdt is a small module that reads and sets the caret position of a `<textarea>`, because the browser does not
expose one to .NET, and nothing in the engine calls it.

```csharp
var document = await collaboration.OpenAsync("design-review");

using (var transaction = document.BeginTransaction())
{
    document.Map.Set("title", "Q3 planning");
    document.Array("attendees").Push("Ada", "Grace");
    document.Text("notes").Insert(0, "Agenda:\n");
}
```

Two people running that against the same document identifier both end up with the same document, whether they were
online at the time or not.

bit Bcrdt's demo lives in [`Demo`](Demo/) - run the [Server](Demo/Server/) project (`dotnet run`, then
`http://localhost:5560`). Every panel on every page is a genuinely separate replica with its own client identity,
presence entry, local store and connection, so the merges you watch are real merges; the *Live* page goes one step
further and puts one replica in each browser tab, meeting through the server's SignalR hub or WebSocket endpoint. It
runs at [bcrdt.bitplatform.dev](https://bcrdt.bitplatform.dev).

That same server hosts an **MCP server** at `/mcp`, so an AI agent works against bit Bcrdt's real API instead of
guessing at it - see [MCP server](#mcp-server).

[`Samples`](Samples/) is the hosting-model harness rather than the docs: one shared [Core](Samples/Core/) project of
collaborative pages - notes, a to-do list and a ticket form - hosted unchanged under [Server](Samples/Server/),
[WASM](Samples/Wasm/) and [Auto](Samples/Auto/), each with its own `BcrdtHub` and document host, so two tabs edit
one document whichever render mode they run in - and under [MAUI](Samples/Maui/) (Android, iOS, Mac Catalyst and
Windows), where a `BlazorWebView` runs them as a local-first Blazor Hybrid app over a file-system store.

---

## Contents

- [What it is for](#what-it-is-for)
- [Why a CRDT](#why-a-crdt)
- [Packages](#packages)
- [Getting started](#getting-started)
- [The five-minute version](#the-five-minute-version)
- [Choosing a data type](#choosing-a-data-type)
- [Working with documents](#working-with-documents)
- [Binding to inputs](#binding-to-inputs)
- [Presence](#presence)
- [Offline and reconnection](#offline-and-reconnection)
- [Persistence](#persistence)
- [Undo](#undo)
- [Snapshots and history](#snapshots-and-history)
- [Hosting models](#hosting-models)
- [Transports and relays](#transports-and-relays)
- [Keeping documents on the server](#keeping-documents-on-the-server)
- [Access control and security](#access-control-and-security)
- [Diagnostics](#diagnostics)
- [Extending it](#extending-it)
- [Trimming and AOT](#trimming-and-aot)
- [Architecture](#architecture)
- [Correctness](#correctness)
- [Limitations](#limitations)
- [Upgrading](#upgrading)
- [MCP server](#mcp-server)
- [Projects](#projects)
- [Build and test](#build-and-test)

---

## What it is for

Anything where two people can touch the same data at once and neither should lose their work: a shared document, a
whiteboard, a kanban board, a form several people fill in together, a settings panel open in two tabs, an app that
has to keep working on a train.

The hard part of those features is never the UI. It is what happens when two edits cross in flight, when somebody's
connection drops mid-sentence, when a client comes back after a week offline with a pile of changes, or when the same
message gets delivered twice. This library's job is to make those situations uneventful.

## Why a CRDT

The obvious approach - last write wins on the whole document - loses data the moment two people type at once. The
next approach - operational transformation - needs a central server to order every keystroke, cannot work offline,
and is notoriously difficult to get right.

A CRDT (conflict-free replicated data type) takes a different route: operations are designed so that applying them
in *any* order gives the same answer. There is no authority, no ordering service and no transformation matrix. Two
replicas that have seen the same edits hold the same document, and that holds regardless of the network's behaviour.
Being offline is not a special case; it is just a replica that has not heard anything for a while.

The cost is that the merge is decided by the data structure rather than by your application, so it is worth
understanding what it will decide. See [Choosing a data type](#choosing-a-data-type).

---

## Packages

| Package | Contents |
|---|---|
| `Bit.Bcrdt` | The engine: identity, clocks and state vectors, the YATA engine with `BcrdtText`, `BcrdtArray`, `BcrdtMap` and `BcrdtSet`, updates, snapshots and selective undo; presence and awareness; the store abstraction with in-memory, file-system and browser (IndexedDB) stores; the sync protocol and `BcrdtSyncProvider`; `IBcrdtCollaborationService` and `AddBitBcrdtServices`; the text-input and object bindings; the in-memory and WebSocket client transports; and metrics. And the Blazor components, in the `Bit.Bcrdt.Blazor` namespace: `BcrdtComponentBase`, `BcrdtCollaborationScope`, `BcrdtTextArea` and `BcrdtPresenceView`. |
| `Bit.Bcrdt.SignalR` | The SignalR client transport. No ASP.NET Core server dependency, so it runs in WebAssembly and Hybrid apps. |
| `Bit.Bcrdt.Server` | The ASP.NET Core relays - `BcrdtHub` for the SignalR transport and `MapBitBcrdtWebSocket` for the WebSocket transport - with per-document read-only or read-write access; `BcrdtDocumentHost`, which keeps a persisted, always-present replica of every document the relays carry; and `BcrdtPresenceCircuitHandler`, which withdraws a Blazor Server user's presence while their browser connection is down. |
| `Bit.Bcrdt.Testing` | A deterministic, seeded network simulator (loss, duplication, reordering, partitions, offline peers), random operation generators, a convergence harness and an update corrupter for fuzzing. |

Dependencies point strictly downward. The engine knows nothing about transports, storage or Blazor - nothing in it
touches the components that ship beside it; the components know nothing about transports; the relays never decode a
message.

All four target .NET 8, 9 and 10.

---

## Getting started

### Install

```bash
dotnet add package Bit.Bcrdt          # the engine and the Blazor components
dotnet add package Bit.Bcrdt.SignalR   # or use the WebSocket transport built into Bit.Bcrdt, or none
```

### Register the services

Register it once, in every project that renders collaborative components - in a Blazor Web App, that is both the
server and the client project. The .NET SignalR client needs an **absolute** hub URL - a relative one such as
`"/hub/bcrdt"` is rejected when the connection is built - so resolve it against the host. In Blazor WebAssembly that
is the host page's base address:

```csharp
using Bit.Bcrdt;
using Bit.Bcrdt.SignalR;

string hubUrl = new Uri(new Uri(builder.HostEnvironment.BaseAddress), "hub/bcrdt").ToString();

builder.Services.AddBitBcrdtServices(collaboration => collaboration
    .UseTransport(new BcrdtSignalRTransportFactory(hubUrl))
    .UseInMemoryStore());
```

`AddBitBcrdtSignalRTransport(hubUrl)` is the same registration as a one-liner. Each of the three seams - transport,
store and options - is optional: an application that configures none of them still gets a working, purely local
collaborative document, which is the right starting point. The model is the same offline and online, so a feature
can be built and tested without a server and then connected to one without changing how it reads or writes state.

### Map the relay

The hub lives in a separate server-side package, so that the client transport carries no ASP.NET Core server
dependency:

```bash
dotnet add package Bit.Bcrdt.Server    # in the server project
```

```csharp
using Bit.Bcrdt.Server;

builder.Services.AddBitBcrdtHub();   // AddSignalR, with a receive limit sized for sync messages
// ...
app.MapHub<BcrdtHub>("/hub/bcrdt");
```

Use `AddBitBcrdtHub` rather than a bare `AddSignalR`: SignalR's default 32 KB receive limit is smaller than the
handshake reply for any document of real size, and a client that exceeds it is disconnected.

The raw WebSocket transport has its relay in the same package: call `app.UseWebSockets()` and map it with
`app.MapBitBcrdtWebSocket("/ws/bcrdt/{documentId}")`.

A relay only relays: a document exists while somebody has it open. To keep every document on the server as well -
so the first person into an empty room still gets everything ever written into it - add a document host. See
[Keeping documents on the server](#keeping-documents-on-the-server).

```csharp
builder.Services.AddBitBcrdtDocumentHost(_ => new BcrdtDocumentHostOptions
{
    Store = new BcrdtFileSystemDocumentStore(Path.Combine(builder.Environment.ContentRootPath, "documents")),
});
```

Everything after that is `IBcrdtCollaborationService`.

---

## The five-minute version

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

That is a working collaborative editor with live presence, automatic reconnection and offline support. The textarea
also publishes its caret through presence, anchored to content rather than to an index, so an application can draw
collaborators' cursors; nothing in the library draws them for you. There is no stylesheet to reference and no script
tag to add: the one small module `BcrdtTextArea` needs (`_content/Bit.Bcrdt/bit-bcrdt.js`) is imported on demand.

- `BcrdtCollaborationScope` opens the document, renders its content with the document as context (and cascades it),
  shows `LoadingContent` while it opens, follows `DocumentId` when it changes, and releases the document when it goes
  away.
- `BcrdtComponentBase` - the base of all three components, and the base to derive your own collaborative components
  from - marshals change notifications onto the renderer and coalesces them onto a frame budget
  (`RenderInterval`, 16 ms by default), so a fast typist and five collaborators do not produce five hundred renders
  a second. Subscriptions made through its `Track` overloads are disposed with the component.

```razor
@inherits BcrdtComponentBase

<p>@_title.Count characters</p>

@code {
    [CascadingParameter] public BcrdtCollaborativeDocument Document { get; set; } = default!;

    private BcrdtText _title = default!;

    protected override void OnParametersSet()
    {
        _title = Document.Text("title");
        Track(_title);   // re-render, coalesced, whenever the text changes - from anyone
    }
}
```

Where a page is prerendered and then becomes interactive, a document must be opened only once the component is
interactive (`RendererInfo.IsInteractive`) - a document opened while prerendering belongs to a request that is
already over, and a browser store cannot be reached at all. `BcrdtCollaborationScope` does this by itself and shows
`LoadingContent` while prerendering - on .NET 8, where there is no `RendererInfo`, by waiting for its first render in
the browser. Give it `ErrorContent` to show a document that cannot be opened instead of failing the page.

`BcrdtTextArea` turns read-only - still showing everyone else's changes - when its `ReadOnly` parameter says so, or
when the server granted the cascaded document's connection read-only access. If its small browser module cannot be
loaded it is disabled rather than left accepting keystrokes that would reach nobody, and `OnError` says why.

---

## Choosing a data type

This is the decision that matters most, and it is worth two minutes.

| Type | Concurrent edits resolve as | Reach for it when |
|---|---|---|
| `BcrdtText` | Both edits survive, each author's run stays contiguous | Anything a person types into at length |
| `BcrdtArray` | Both insertions survive, in a stable order | Lists, ordered collections |
| `BcrdtMap` | **One value wins**, deterministically but arbitrarily | Fields where a later value legitimately replaces an earlier one |
| `BcrdtSet` | An add wins over a concurrent remove - if it added something (re-adding a present element is a no-op) | Tags, membership, flags |

The trap is `BcrdtMap` with a long string in it. Two people editing the same field concurrently means one of them
loses everything they typed - not because of a bug, but because that is what "last writer wins" means. If a field is
something a human types prose into, make it a nested `BcrdtText` and both edits merge:

```csharp
document.Map.EnsureText("description").Insert(0, "...");   // merges
document.Map.Set("description", "...");                    // one side wins
```

The same applies to `BcrdtArray.Replace` and `BcrdtArray.Move`: both are delete-then-insert. `Move` inserts a copy of
the element at its new position, so a concurrent edit to the moved element is discarded - it lands on the element the
move removed - and two clients moving the same element concurrently end up with two copies of it. Mutate a nested type
in place instead when that matters.

The values a document stores are the **collaborative value model**: `null`, `bool`, `string`, integers (stored as
`long`), `double`, `float`, `decimal`, `byte[]`, `DateTime`/`DateTimeOffset`, `Guid`, lists and string-keyed
dictionaries of those, and nested collaborative types. Anything else is rejected up front with
`NotSupportedException`, rather than becoming part of the document and making every later encode throw. Application
types are flattened onto the value model through JSON with `BcrdtMap.SetObject` and read back with `Get<T>`.

---

## Working with documents

`IBcrdtCollaborationService.OpenAsync` does everything in the right order - load from storage first, so the document
is usable immediately and offline, then connect and let the handshake reconcile whatever changed while it was away.
Opening the same identifier twice returns the same instance, reference counted: pair every `OpenAsync` with a
`CloseAsync`, and the document closes when the last reference goes.

```csharp
BcrdtCollaborativeDocument document = await collaboration.OpenAsync("board-7",
    new BcrdtOpenDocumentOptions { User = me });

BcrdtArray columns = document.Array("columns");
BcrdtMap column = new();
column.Set("title", "To do");
column.Set("cards", new BcrdtArray());
columns.Push(column);

document.Map.Set("updatedAt", DateTimeOffset.UtcNow);

await collaboration.CloseAsync("board-7");
```

- **Transactions.** Every mutation runs in a transaction; consecutive calls each get their own. Group related changes
  with `BeginTransaction()` (dispose it to commit, or `await CommitAsync()`), or `Transact(transaction => ...)`: a
  transaction produces exactly one update message and one round of events, so a paste of 500 items is one packet.
  An `origin` argument tags it - for undo scoping and for telling your own changes from remote ones.
- **Events.** `document.Changed` reports every committed update with its origin; `Observe` on a type reports changes to
  that type - `Delta` for text and arrays, `Keys` for maps, `Added` and `Removed` for sets - and `ObserveDeep` includes
  nested types. Read the delta inside the handler: it is computed lazily from the blocks the
  transaction touched, which are compacted once it settles.
- **Rich text.** `BcrdtText.Format(index, length, attributes)` applies marks that occupy no index space, so formatting
  never moves a character or a cursor; `ToDelta()` reads the text as a Quill-style delta.
- **The engine directly.** `BcrdtDocument` (`document.Crdt`) is the replica itself and runs anywhere - a console app, a
  worker, a test - with no service at all: `EncodeStateAsUpdate`, `ApplyUpdate`, `GetStateVector` and the `Updated`
  event are the whole sync surface.
- **Thread affinity.** A document is not thread-safe. A document opened through the service is built on the calling
  synchronization context and applies every network update there - in Blazor, the renderer's - so remote changes and
  the user's own edits never race.

---

## Binding to inputs

An HTML input reports its **entire new value**, not the change the user made. Writing that value straight into a
`BcrdtText` would delete every character and reinsert it - discarding every concurrent edit, moving every remote
cursor to the start, and sending an update the size of the document for one keystroke.

`BcrdtTextSynchronizer` recovers the real edit:

```csharp
BcrdtTextSynchronizer.Apply(document.Text("body"), newValueFromTheInput);
```

`BcrdtTextArea` does this for you, and more: it maps the local caret across incoming remote edits so somebody typing
earlier in the document does not shift your cursor, refuses to overwrite keystrokes still on their way (an input
method composition included), and publishes the caret through presence. `BcrdtTextInputBinding` is the same machinery
without a component, for an input you render yourself.

Between the browser and .NET, `BcrdtTextArea` exchanges *changes*, never the whole text: a keystroke is a few bytes in
each direction however long the document is. That matters in Blazor Server, where a message from the browser larger
than the circuit's limit (32 KB by default) drops the connection. Every change the browser reports is numbered, and an
update built in .NET quotes the number it was built after; the browser refuses one built before something the user has
typed since, so a remote change can never land on top of a keystroke it did not know about - even when the text has
come back to exactly what it was. The numbers also show when a report went missing - lost while the connection
dropped - and when a change will not fit one message - a big paste. Either way .NET reads the control's whole value
back as a stream, which Blazor delivers in chunks under the limit, and edits reported meanwhile are replayed on top of
it. `BcrdtTextInputBinding.TryApplyInputChange` is the .NET half, for a control of your own.

`BcrdtListBinding<T>` projects a `BcrdtArray` as an `IReadOnlyList<T>` with `INotifyCollectionChanged`, and
`BcrdtObjectBinding<T>` binds the properties of a plain object to the keys of a `BcrdtMap` with
`INotifyPropertyChanged` - writing back only the properties the caller actually changed, so saving a form never
reverts a field somebody else edited meanwhile.

---

## Presence

Presence is **ephemeral, non-persistent, non-CRDT state** - who is here, where their cursor is, whether they are
typing - deliberately separate from the document, with different guarantees. `BcrdtPresenceService`
(`document.Presence`) is a typed façade over the awareness store:

```csharp
document.Presence.SetUser(new BcrdtCollaboratorInfo("ada", "Ada", BcrdtPresenceService.SuggestColor("ada")));
document.Presence.SetCaret("body", document.Text("body"), 42);
document.Presence.SetActivity("typing");

foreach (BcrdtCollaborator other in document.Presence.Others)
{
    (int Start, int End)? selection = other.Selection?.Resolve(document.Crdt);   // anchored to content, not to an index
}
```

Each entry has a time-to-live (30 seconds by default, `BcrdtCollaborationOptions.PresenceTimeout`) refreshed by a
heartbeat, so a crashed tab disappears from everyone's list without any server bookkeeping. `SuggestColor` hashes a
user id into a fixed palette, so every participant agrees on everyone's colour with no coordination.

In Blazor Server a circuit outlives its browser tab by the circuit retention period, and its documents keep publishing
presence until then. To withdraw a user as soon as the connection drops, call
`builder.Services.AddBitBcrdtCircuitPresence()` from `Bit.Bcrdt.Server`: it registers `BcrdtPresenceCircuitHandler`, which
calls `BcrdtPresenceService.Suspend` on every document the circuit has open when the connection goes down, and `Resume`
when it comes back.

---

## Offline and reconnection

Nothing is required to make this work. Local edits accumulate in the document while disconnected; `BcrdtSyncProvider`
reconnects with jittered exponential backoff and re-runs the handshake. Each peer answers a returning replica with
what it is missing *and* asks for what the replica holds, so work done offline flows both ways - deletions included,
even though a deletion leaves no trace in a state vector.

Note that there is no outbound message queue, and that is deliberate. The document *is* the queue, and it compacts
itself: an eight-hour offline session reconnects with one delta describing the net result, not eight hours of
replayed packets.

`document.ConnectAsync()` and `DisconnectAsync()` take a document online and offline on purpose;
`document.ConnectionStatus` (`Disconnected → Connecting → Handshaking → Synced → Reconnecting`, or `Faulted`) and
`IsSynced` report where it stands, and `Sync.LastSyncedAt` when it last caught up. A transport that is refused - an
HTTP 401 or 403, from the WebSocket upgrade or the SignalR negotiation - throws `BcrdtTransportAuthorizationException`,
and the provider stops rather than backing off forever; a fresh `ConnectAsync`, once the user has signed in again,
tries afresh. Over SignalR a 403 is final at once, but a 401 is first retried with the backoff, each attempt asking the
token provider for a fresh token, because an access token that expired while the device slept is renewed on the next
negotiation. Only the third 401 in a row counts as a refusal.

The backoff only resets once a connection has completed its handshake, so a server that accepts a connection and drops
it straight away is retried with growing delays rather than hammered. A message the transport failed to send on a
connection that stayed up is repaired by a handshake shortly afterwards. And no message is larger than
`BcrdtSyncProviderOptions.MaxMessageSize` (1 MB by default) where the content can be divided: a document's whole state
for a newcomer, or one enormous paste, goes out as several ordinary updates that the receiver applies as they come
(`BcrdtUpdateOperations.Split`) - so it never trips a relay's receive limit and gets its sender disconnected.

---

## Persistence

Add a store and the document also survives the app being closed:

```csharp
builder.Services.AddBitBcrdtServices(collaboration => collaboration
    .UseStore(new BcrdtFileSystemDocumentStore("./documents")));
```

Storage is append-only with periodic compaction (`BcrdtDocumentPersistenceOptions.CompactionThreshold`, 500 updates by
default). A crash mid-append costs at most the last update, which a peer still holds and resends; a torn trailing
record is detected by its checksum and discarded on load. Document identifiers routinely come from URLs, so the file
store hashes them into file names rather than trusting them as paths.

Three stores ship:

| Store | Where | Notes |
|---|---|---|
| `BcrdtIndexedDbDocumentStore` (`UseIndexedDbStore()`) | The browser's IndexedDB | For Blazor WebAssembly: a document - and what was typed into it offline - survives a reload, a closed tab and a restarted browser. Every write commits with strict durability, a few milliseconds after the change - a tab closed in that moment loses the change from this browser (not from a peer or server it reached); `FlushAsync` waits for it. Several tabs may share a document; each is its own replica and what any of them loads is the union of what all of them stored. Reached through interop, so open documents only once interactive (`BcrdtCollaborationScope` does). A full storage quota is reported as an `IOException` saying so, and the write is retried. |
| `BcrdtFileSystemDocumentStore` | A directory of append-only log files | For servers, desktop and MAUI. One lock per file across every instance in the process. Damage that an interrupted append cannot explain - intact records after a bad one - is never truncated away: the log is copied aside untouched (`*.corrupt`), repaired around the damage, and `CorruptionDetected` reports it. One directory per process. |
| `BcrdtInMemoryDocumentStore` | Process memory | Tests, and server-side hosts whose documents need not outlive the process. `new BcrdtInMemoryDocumentStore(maxDocuments)` forgets the least recently used document beyond a bound, for anything the public can create documents through - never one still attached to a `BcrdtDocumentPersistence` (open in a document host or a collaboration service), and never one used in the last minute. |

`FlushAsync` (and `CommitAsync`) wait for the changes made before the call, not for the queue to empty, so they complete
even while collaborators keep the document busy. A compaction that fails is not retried after every write but after
another threshold's worth of records. Any other backend is an `IBcrdtDocumentStore` of five methods that needs no CRDT
knowledge: see [Writing a store](#writing-a-store).

---

## Undo

`BcrdtUndoManager` implements *selective* undo: it reverses **your** edits and leaves everyone else's alone. Rolling
the document back to a previous state - what a single-user editor does - would discard your collaborators' concurrent
work.

```csharp
using var undo = new BcrdtUndoManager(document.Text("body"));
undo.Undo();   // removes what I did, not what they did
undo.Redo();
```

Consecutive edits within a capture window (`CaptureTimeout`, 500 ms) collapse into one step, so typing a word is one
undo rather than five. An undo is itself an ordinary edit: it synchronizes, it merges, and it can be undone. Remote
updates carry their sync provider as origin, so they are never captured; code that tags its own edits with an origin
adds it to `BcrdtUndoManagerOptions.TrackedOrigins`. Every document opened through the service also has a default
stack over its root map, `document.Undo`.

A step only ever removes content this replica created - even if a peer's update was applied inside the same
transaction. Each stack keeps `MaxStackItems` steps (100 by default); the oldest makes way, and the deleted content it
was keeping for an undo is released and reclaimed, as it is when the stack is cleared or the manager disposed.

---

## Snapshots and history

A `BcrdtSnapshot` is a `(state vector, delete set)` pair - a few hundred bytes - that names a point in history.
`BcrdtSnapshot.RestoreDocument(document, snapshot)` reconstructs the document as it was, as a new document, which is
what a version history needs without storing full copies. Restoring needs every tombstone in the document's history,
so a document whose history will be restored is created with `GarbageCollect = false`
(`BcrdtCollaborationOptions.GarbageCollect` or `BcrdtDocumentOptions.GarbageCollect`); restoring from a collecting
document is refused rather than answered wrongly.

---

## Hosting models

The DI registration is identical in every hosting model, and `IBcrdtCollaborationService` is registered **scoped** for
a specific reason:

- **WebAssembly** - one scope per application, so scoped behaves as a singleton. Correct for a single-user client.
- **Server** - one scope per circuit, so every connected user gets their own replica and their own client identity.
  Registering it as a singleton would make every visitor share one replica, and the engine could not tell their
  edits apart.
- **Blazor Web App** - register it in both projects; whichever side renders interactively owns the replica.
- **Hybrid (MAUI, WPF, WinForms)** - as WebAssembly, with the local process as the replica.

The [samples](Samples/) wire bit Bcrdt into each of these - their `Program.cs` (and the MAUI sample's `MauiProgram.cs`)
is the registration to copy.

The engine requires no JavaScript interop, so it runs unchanged in all of them, in a background service, in a console
application, or on a server that hosts documents without rendering them.

---

## Transports and relays

A transport moves opaque messages between peers. bit Bcrdt ships three:

| Transport | Package | Relay |
|---|---|---|
| `BcrdtSignalRTransport` (`BcrdtSignalRTransportFactory`) | `Bit.Bcrdt.SignalR` | `BcrdtHub` in `Bit.Bcrdt.Server`, mapped with `MapHub<BcrdtHub>`. Scales out with a SignalR backplane. |
| `BcrdtWebSocketTransport` (`BcrdtWebSocketTransportFactory`) | `Bit.Bcrdt` | `MapBitBcrdtWebSocket` in `Bit.Bcrdt.Server`. Rooms are process-local: use sticky sessions or a broker behind a load balancer. |
| `BcrdtInMemoryTransport` (`BcrdtInMemoryNetwork`) | `Bit.Bcrdt` | None - replicas in one process, for tests, demos and side-by-side views. |

On their own, both relays only relay: neither decodes a message, holds a replica or knows what a state vector is. Both
take an authorization hook - override `BcrdtHub.AuthorizeAccessAsync` (or `AuthorizeAsync` for a plain yes or no), or
set `BcrdtWebSocketEndpointOptions.AuthorizeAccess` (or pass `authorize` to `MapBitBcrdtWebSocket`) - consulted before
a client joins a document; a refusal reaches the client as a final `BcrdtTransportAuthorizationException`. Every client
of the WebSocket relay gets its own bounded outbound queue, so one client on a bad connection cannot hold up the
others. Neither relays an `Auth` verdict, a `Ping` or a `Pong` from a client: the first would be a forgery, the others
would be answered by every member of the room to every member.

---

## Keeping documents on the server

A relay holds nothing, so on its own a document exists only while somebody has it open: the first person into an
empty room sees an empty document unless their own device kept a copy, and an edit made by someone who then closes the
tab before anyone else connects is gone. `BcrdtDocumentHost` closes that gap.

```csharp
builder.Services.AddBitBcrdtHub();
builder.Services.AddBitBcrdtDocumentHost(_ => new BcrdtDocumentHostOptions
{
    Store = new BcrdtFileSystemDocumentStore("./documents"),   // or any IBcrdtDocumentStore
});

app.MapHub<BcrdtHub>("/hub/bcrdt");                            // picks the host up from the container
app.MapBitBcrdtWebSocket("/ws/bcrdt/{documentId}", options: new()
{
    DocumentHost = app.Services.GetRequiredService<BcrdtDocumentHost>(),
});
```

The host joins every room as one more peer, running the same `BcrdtSyncExchange` decisions as every client:

- It **loads** a document from its store when the first connection arrives, and unloads it `UnloadDelay` (30 s) after
  the last one leaves, flushing it to the store.
- It **persists** every change any client makes. A change that builds on something it has not seen makes it ask the
  sender for the rest at once.
- It **answers every handshake on its own**, with the whole document, so a join costs one answer rather than one from
  every peer in the room. Behind a SignalR backplane, where one instance does not see every connection to a document,
  set `ScaleOut = true` and the handshake is relayed to the others too.
- It **drops** what nobody should receive: malformed messages, changes from read-only connections, forged `Auth`
  verdicts, and presence published under somebody else's identity. It announces a connection's departure from presence
  the moment the connection goes.
- It **validates** changes against server-side rules, if you give it `ValidateUpdate`, before anyone else receives them.
- Server code can **read and change** a hosted document: `ReadAsync`, and `EditAsync`, whose changes reach every
  connected client and the store like a client's.

```csharp
await host.EditAsync("board-7", doc => doc.GetMap("").Set("status", "approved"));
string title = await host.ReadAsync("board-7", doc => doc.GetMap("").Get("title") as string ?? "");
```

## Access control and security

A CRDT guarantees that replicas converge. It says nothing about who may change what, and a modified client can produce
perfectly well-formed updates that break every rule your application has. The trust boundary is the server.

- **Who may join, and what they may do.** `AuthorizeAccessAsync` (hub) or `AuthorizeAccess` (WebSocket endpoint)
  returns `None`, `ReadOnly` or `ReadWrite` per connection and document, with the caller's authenticated identity at
  hand. A refused client is told and stops reconnecting.
- **Read-only.** The relay drops every `Update` and `SyncStep2` a read-only connection sends - that is the enforcement.
  The client is told what it was granted when it joins (`BcrdtSyncProvider.Access`, `BcrdtCollaborativeDocument.IsReadOnly`)
  so it can stop offering edits; `BcrdtTextArea` turns read-only by itself, and a local change made anyway is reported
  through the provider's `Error` event instead of vanishing silently. A read-only client answers no handshake: what it
  holds beyond what it was sent can never reach anyone, and promising it would leave others waiting for it. A client
  that ignores its grant gains nothing.
- **Business rules.** `BcrdtDocumentHostOptions.ValidateUpdate` sees each change before it is applied or relayed, with
  the document as it stands; apply the update to a scratch copy to see what it would do. A rejected change refuses its
  sender for good: the sender already holds the change and builds on it, so silently dropping it would leave that
  client diverged forever. Validate on the client too, so people are told before they type something the server will
  refuse.
- **Untrusted input.** Every update is decoded completely before anything is applied, and a malformed one leaves the
  document exactly as it was. Declared counts are bounded by the bytes that follow and never pre-allocate more than a
  small fixed amount; values nest at most 64 levels and collaborative types at most `BcrdtAbstractType.MaxNestingDepth`
  (128) - deeper ones from a peer are dropped identically on every replica, so they can neither exhaust the stack nor
  make replicas disagree. Block lengths that would overflow are never merged. Text splits are constant-time, so no
  update can make a replica do work in the square of a document's size.
- **Presence.** Awareness is not authenticated on a bare relay: any member can publish an entry under another's client
  identifier, briefly. Clocks are capped so a forged entry cannot take an identity over for good, and with a document
  host each identity belongs to the connection that first published it - until that connection has been silent for
  `PresenceTakeoverAfter` (15 s), or at once for a new connection of the same signed-in user, which is how a client
  that reconnected after a network blip gets its presence back.
- **Isolation.** Documents are isolated by identifier: one SignalR group, one WebSocket room per identifier and per
  mapping, one hosted replica per identifier and host. Two endpoints given the same host share its documents, and the
  host passes what a client of one sends on to the clients of the other, so they edit one document together.
- **Resource limits.** Both relays cap message size (16 MB) and the WebSocket relay caps each client's outbound queue by
  count and bytes, disconnecting a client that falls too far behind. Providers and hosts split their own messages below
  1 MB.
- **What is not provided.** End-to-end encryption, per-field permissions inside one document, signed or attributable
  changes (any writer can create blocks under any client identifier), and rate limiting - put that in front of the
  relay with ASP.NET Core's rate limiter. Grants are checked when a client joins; revoking access takes effect on its
  next connection.

## Diagnostics

Every metric is published on one meter, `BcrdtDiagnostics.MeterName` (`"Bit.Bcrdt"`), through
`System.Diagnostics.Metrics`, and costs a single check while nobody listens:

```csharp
builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(BcrdtDiagnostics.MeterName));
```

or `dotnet-counters monitor --counters Bit.Bcrdt`. In Blazor WebAssembly the SDK turns `System.Diagnostics.Metrics` off
by default, leaving every meter silent; set `<MetricsSupport>true</MetricsSupport>` in the project to collect them in the
browser. Messages and bytes sent and received by kind, handshake durations,
reconnections, provider errors by exception type, store writes, failures and compactions, and - on a server - loaded
documents, connections and the messages a host dropped, by reason. No measurement carries a document identifier, a
client identifier or any content. `BcrdtSyncProvider.Error`, `BcrdtDocumentPersistence.WriteFailed` and
`BcrdtFileSystemDocumentStore.CorruptionDetected` carry the details; `BcrdtDocument.HasPendingUpdates` says a replica is
waiting for something it was never sent.

---

## Extending it

Every seam is an interface with a shipped default:

- `IBcrdtTransport` (with `IBcrdtTransportFactory`) - anything that can carry bytes both ways.
- `IBcrdtDocumentStore` - five methods; needs no CRDT knowledge, because it only stores opaque update records and the
  persistence layer does the merging.
- `IBcrdtClientIdProvider` - where session identities come from.
- `IBcrdtCollaborationService` - document lifecycle and composition.

### Writing a transport

A transport moves opaque messages between peers. It does **not** need to be reliable, ordered, or even particularly
well behaved.

```csharp
public sealed class MyTransport : IBcrdtTransport
{
    public BcrdtTransportState State { get; private set; }
    public string Description => "my-transport";

    public event Action<ReadOnlyMemory<byte>>? MessageReceived;
    public event Action<BcrdtTransportStateChange>? StateChanged;

    public ValueTask ConnectAsync(CancellationToken ct) { /* ... */ }
    public ValueTask SendAsync(ReadOnlyMemory<byte> message, CancellationToken ct) { /* ... */ }
    public ValueTask DisconnectAsync(CancellationToken ct) { /* ... */ }
    public ValueTask DisposeAsync() { /* ... */ }
}
```

Messages may be **lost**, **duplicated**, **reordered** or **delayed arbitrarily**. The synchronization layer is built
for all four: duplicate detection is a single state-vector comparison, out-of-order blocks wait in a pending queue
until their dependencies arrive, and a lost message is repaired by the next handshake. You do not need to build
reliability underneath. What it does not tolerate:

1. **Partial messages.** A message that arrives must be exactly the bytes that were sent. Over a framed protocol like
   SignalR this is free; over raw WebSockets you must accumulate fragments until `EndOfMessage` before raising
   `MessageReceived`. Surfacing a partial frame means handing the decoder a truncated update, which it will correctly
   reject - losing the message for good.
2. **Lying about state.** `State` must be `Connected` only while messages can actually be delivered. The provider uses
   it to decide whether to send, and a transport that claims to be connected while dropping everything produces a
   client that appears synced and is not.
3. **Throwing from `SendAsync` when disconnected.** Drop the message and return. Disconnection is an ordinary event that
   happens mid-keystroke; treating it as an exception turns normal operation into an error path.
4. **Reporting one failure twice.** A `ConnectAsync` that fails reports it *only* by throwing - it must not also raise
   `Disconnected` or `Faulted` for the same attempt - and a connection that was announced as `Connected` and is then
   lost raises exactly one terminal transition. Either kind of double report would start two reconnection loops.
5. **Retrying a refusal.** A peer that refuses the connection - an HTTP 401 or 403, say - should be reported by throwing
   `BcrdtTransportAuthorizationException`, so the provider stops instead of backing off forever against an answer
   that will not change.
6. **Reusing a received buffer.** The memory handed to `MessageReceived` must stay valid after the handler returns,
   because the provider may process it later on another thread.

Events may be raised on any thread. `BcrdtSyncProvider` marshals them onto the `SynchronizationContext` it was created
on before it touches the document, so a transport needs no locking on the document's behalf.

If your underlying library has its own automatic reconnect - SignalR's `WithAutomaticReconnect`, for instance - leave
it **off**. `BcrdtSyncProvider` owns the connection state machine, and it needs to see the drop so that it can re-run
the handshake when the connection returns. A transport that silently reconnects underneath produces a client that is
connected but out of date, with no event to tell anyone.

### Writing a store

```csharp
public interface IBcrdtDocumentStore
{
    ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken ct = default);
    ValueTask AppendUpdateAsync(string documentId, ReadOnlyMemory<byte> update, CancellationToken ct = default);
    ValueTask CompactAsync(string documentId, CancellationToken ct = default);
    ValueTask DeleteAsync(string documentId, CancellationToken ct = default);
    ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken ct = default);
}
```

A store needs **no CRDT knowledge whatsoever**. Updates merge - applying `a` then `b` is the same as applying
`BcrdtUpdateOperations.Merge(a, b)` - so compaction is `BcrdtUpdateOperations.Merge(everything)` and correctness never
depends on the store preserving order, deduplicating, or understanding anything about what it holds. Three things are
worth getting right:

- **Append must stay cheap.** It runs on every burst of edits. Append-only is what makes that affordable and
  crash-safe; read-modify-write of the whole document is neither.
- **A torn trailing record must be detectable.** A crash mid-append leaves a partial record. Frame records with a
  length and a checksum and stop reading at the first one that does not verify. The cost is at most the last update,
  which the peer still holds and will resend on the next handshake - so a store that loses its final record is still
  correct.
- **Document identifiers are untrusted.** They routinely come from URLs. `../../etc/passwd` is a valid `string`.
  `BcrdtFileSystemDocumentStore` hashes the identifier to derive a filename and keeps the original in a sidecar; do
  something equivalent rather than concatenating it into a path.

Losing data a store never acknowledged is acceptable. Corrupting data it did acknowledge is not.

### Writing a collaborative type

The four built-in types are projections over one shared structure - a linked list of blocks plus a key index - so a new
type is written against the same operations rather than re-deriving positioning logic that would then need its own
convergence proof.

```csharp
using System.Globalization;

public sealed class CrdtCounter : BcrdtAbstractType
{
    public override BcrdtTypeKind Kind => BcrdtTypeKind.Map;   // reuse an existing wire kind

    protected override BcrdtAbstractType CreateInstance() => new CrdtCounter();

    protected override BcrdtEvent CreateEvent(BcrdtTransaction transaction, HashSet<string?> changedKeys) =>
        new BcrdtGenericEvent(this, transaction);

    public override object ToJson() => Value;

    public long Value
    {
        get
        {
            long total = 0;
            foreach ((string _, object? value) in BcrdtTypeOperations.MapEntries(this))
            {
                if (value is long amount) total += amount;
            }
            return total;
        }
    }

    // Each client accumulates into its own key, so concurrent increments add up
    // instead of overwriting one another.
    public void Add(long amount) => Doc.Transact(transaction =>
    {
        string key = Doc.ClientId.ToString(CultureInfo.InvariantCulture);
        long current = BcrdtTypeOperations.MapGet(this, key) as long? ?? 0;
        BcrdtTypeOperations.MapSet(transaction, this, key, current + amount);
    });
}
```

That counter is the standard grow-only-per-replica construction, and the pattern generalizes: the merge semantics you
want are expressed in *how you lay data out over the primitives*, not in custom merge code. There is no merge function
to write, and that is the point - a hand-written merge is exactly where convergence bugs live. Because it is built on
the shared primitives, it also inherits everything else for free: change events, undo and redo, garbage collection,
encoding, and synchronization. The same type is compiled and tested in
[`Tests/Bit.Bcrdt.Tests/ExtensibilityTests.cs`](Tests/Bit.Bcrdt.Tests/ExtensibilityTests.cs), including a run against
the adversarial simulator.

`CreateInstance` and `CreateEvent` are declared `protected internal` on `BcrdtAbstractType`. From your own assembly you
override them as **`protected`**, as above; writing `protected internal` there is error CS0507. The test copy is the
one exception: the test assembly is a friend of the engine (`InternalsVisibleTo`), and a friend assembly must keep
`protected internal` - which is also why that test cannot catch the access modifier in this sample going wrong.

Introducing a genuinely new `BcrdtTypeKind` or `BcrdtContentRef` value means taking the next free number. **Never
renumber an existing one**: those values are persisted in stored documents, and a reader that reinterprets an old
number silently misreads real data. Readers reject numbers they do not recognise, so an old client meeting a new
document fails with a clear message rather than corrupting it.

### Testing it against the simulator

`Bit.Bcrdt.Testing` is framework-neutral, so it works from MSTest, xUnit or NUnit:

```csharp
using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions
{
    Seed = 1,
    DropProbability = 0.25,
    DuplicateProbability = 0.25,
    MaxLatencySteps = 5,
});

BcrdtSimulatedPeer[] peers = network.AddPeers(4);
// ... exercise your type on each peer ...
network.Flush();
network.SynchronizeAll();
BcrdtConvergenceHarness.AssertConverged(network);
```

Anything that merges correctly on a reliable network and fails here has a real bug. Run it across a range of seeds,
and print the seed on failure - a randomized test you cannot replay is an anecdote, not a test.

---

## Trimming and AOT

The engine, the stores, the sync protocol, the Blazor components and both client transports are trim- and AOT-safe
for every value of the collaborative value model. Two things are not, by nature:

- **Projecting application types** - `BcrdtMap.SetObject`, `Get<T>` and `BcrdtArray.ToArray<T>` for a type outside the
  value model, `BcrdtAnyCodec.FromObject`/`ToObject`, `BcrdtObjectBinding<T>` and `BcrdtListBinding<T>` - goes through
  System.Text.Json. `FromObject` and `ToObject` take `JsonSerializerOptions`: a trimmed or native AOT app passes options
  whose `TypeInfoResolver` is a source-generated `JsonSerializerContext` covering its types, or stores value-model data
  (dictionaries and lists) instead.
- **The SignalR hub.** ASP.NET Core SignalR does not support trimming or native AOT on the server, so
  `AddBitBcrdtHub` says so (`RequiresUnreferencedCode`). The WebSocket relay has no such limit.

`Bit.Bcrdt.Testing` runs in test hosts only and is not trim-annotated.

---

## Architecture

This section is the design contract for the implementation. It explains what was chosen, what was rejected, and why.
Read it before changing anything in [`Bit.Bcrdt`](Bit.Bcrdt/) - the invariants described here are load-bearing.

### The CRDT model

The core is an **operation-based sequence CRDT of the YATA family** (*Yet Another Transformation Approach*),
implemented as a **doubly-linked list of run-length-encoded blocks** with a **causal-length delete set** layered on
top, and with **map, array, text and set semantics projected onto that single shared structure**.

Every collaborative value in a document - a map entry, an array element, a character run in a text field, a nested
collaborative type - is a `BcrdtItem`: a block in one linked list, owned by one client, addressed by one globally
unique `(client, clock)` identifier.

| Model | Why it was rejected |
|---|---|
| **Operational Transformation (OT)** | Requires a central transformation authority and `O(n²)`–`O(n³)` transformation matrices to prove correct. Cannot work peer-to-peer or offline-first without a server ordering every operation. Historically the source of a long tail of convergence bugs in production editors. Directly conflicts with the "offline-first, pluggable transport" requirement. |
| **State-based CvRDT (merge whole states)** | Merge is a join over the full state. Sending the whole document on every sync makes incremental updates impossible, and memory scales badly for text. |
| **LSEQ / Logoot / Treedoc (dense position identifiers)** | Position identifiers grow without bound under adversarial interleaving (repeated insert-at-same-spot). Identifier length becomes `O(n)` in the worst case, so a 1 MB document can carry many MB of position metadata. No run-length encoding is possible because every character needs its own path. |
| **RGA (Replicated Growable Array)** | Genuinely good, and YATA is a close relative. RGA resolves concurrent inserts at the same position purely by timestamp descending, which produces *interleaving* of concurrently typed runs ("abc" + "xyz" → "axbycz" is permitted). YATA's dual-origin rule keeps concurrently inserted runs contiguous, which is what users actually expect from a text editor. |
| **Peritext / Fugue (interleaving-optimal)** | Excellent research; Fugue's guarantees are strictly stronger than YATA's on interleaving. But both are substantially newer, have less production mileage, and (for Peritext) target rich-text marks specifically rather than a general-purpose document model. bit Bcrdt adopts Peritext-*style* mark semantics for rich text (`BcrdtContentFormat`) while keeping YATA for sequencing. |

YATA gives the properties the library needs, together:

- **Deterministic, commutative, idempotent integration** - replicas that have seen the same set of operations are
  byte-identical in structure, regardless of arrival order.
- **Block-level run-length encoding** - typing "hello world" is *one* `BcrdtItem` of length 11, not 11 items. This is
  what makes large text documents affordable; it is impossible in dense-identifier schemes.
- **A single uniform structure** for every data type, so map, array, text and set share one integration algorithm,
  one encoder, one garbage collector and one undo manager.
- **Causal, incremental deltas** - a replica describes what it has with a state vector and receives exactly the blocks
  it is missing.

The trade-offs accepted:

1. **Tombstones are retained until garbage collection.** A deleted block keeps its identifier so that concurrent
   operations referencing it still resolve. Mitigated by garbage collection (content is dropped, blocks merge, delete
   sets compact).
2. **Causal delivery is required within a client's own sequence.** Blocks referencing unknown origins are held as
   pending until their dependencies arrive. This is the standard requirement for op-based CRDTs and is why the sync
   protocol is state-vector driven rather than fire-and-forget.
3. **No total order over concurrent map writes.** A map key written concurrently by two clients resolves
   last-writer-wins by the *integration order* of the underlying items, which is deterministic but arbitrary.
   Applications that need merge semantics for a value should model it as a nested type, not a scalar.
4. **Not interleaving-optimal.** YATA prevents the common interleaving cases; adversarial concurrent insert patterns at
   the exact same position can still interleave at block granularity. Accepted in exchange for maturity and
   run-length encoding.

### Data model

```
BcrdtDocument
 ├── ClientId              ulong, random per session, never reused
 ├── BcrdtStructStore      client -> ordered, contiguous list of blocks
 │    └── BcrdtAbstractStruct   BcrdtItem | BcrdtGarbageCollected | BcrdtSkip
 ├── BcrdtDeleteSet        client -> sorted, merged ranges of deleted clocks
 │                         (derived from the store by GetDeleteSet, cached per version)
 ├── RootTypes             root-type name -> BcrdtAbstractType
 └── Types                 BcrdtAbstractType graph (map/array/text/set), nestable
```

Everything in the store is a struct occupying a **half-open clock range** `[id.Clock, id.Clock + Length)` for its
owning client. The store is *contiguous per client*: struct `n+1` starts exactly where struct `n` ends. This makes
lookup a binary search and makes "what am I missing" a single integer comparison per client.

| Struct | Purpose |
|---|---|
| `BcrdtItem` | A real block of content. |
| `BcrdtGarbageCollected` | A range whose content was dropped by GC. Occupies clock space, has no content. |
| `BcrdtSkip` | A gap placeholder written into an update when the sender deliberately omits a range. Never persisted in a document. |

```
BcrdtItem
Id           (client, clock)     globally unique, immutable
Length       int                 number of logical units (chars for text, 1 for a value)
Origin       BcrdtItemId?        id of the block immediately to the LEFT at insertion time
RightOrigin  BcrdtItemId?        id of the block immediately to the RIGHT at insertion time
Left / Right BcrdtItem?          live links, mutated during integration
Parent       BcrdtAbstractType   the containing type
ParentKey    string?             non-null => this item is a MAP entry under that key
Content      IBcrdtContent       the payload
Info         flags               Deleted, Keep, Countable
```

`Origin` **and** `RightOrigin` together are what make this YATA rather than RGA. They define the *interval* into which
the block was inserted, so a concurrent block can be placed relative to the interval instead of relative to a single
neighbour.

Content is a strategy object (`IBcrdtContent`), so new payload kinds can be added without touching integration,
encoding or GC:

| Content | Ref | Length | Notes |
|---|---|---|---|
| `BcrdtContentDeleted` | 1 | n | Placeholder for a deleted, GC'd range. |
| `BcrdtContentBinary` | 2 | 1 | Opaque `byte[]`. |
| `BcrdtContentString` | 3 | UTF-16 units | Run-length text. Splittable. |
| `BcrdtContentEmbed` | 4 | 1 | Non-text object embedded in text (image, mention). |
| `BcrdtContentFormat` | 5 | 1 | A rich-text mark boundary. Occupies one unit of *clock* space, because it needs an identifier like anything else, but **zero index space** - applying formatting must never shift a character position or a cursor. |
| `BcrdtContentType` | 6 | 1 | A nested `BcrdtAbstractType`. |
| `BcrdtContentAny` | 7 | n | One or more primitive values, run-length packed. |
| *(reserved)* | 8 | - | Reserved for nested sub-documents (`BcrdtContentRef.Doc`). Not implemented: no content type uses this number yet, and a reader rejects it. |

Every content type implements `Splice(offset)` and `TryMergeWith(next)`, which is how run-length encoding survives
edits: inserting into the middle of a 4000-character block splits it into two, and adjacent blocks from the same
client with adjacent clocks merge back together during transaction cleanup.

**Splitting must happen exactly at the requested offset, and must be lossless.** The offset is fixed because an
identifier addresses a logical unit, so a block of length *n* must split into *offset* and *n − offset* or every later
clock changes meaning. Losslessness matters because a split can land between the two halves of a surrogate pair, and
if the halves were "repaired" locally - replaced with U+FFFD, say - the visible text would depend on *whether a given
replica happened to split that block*, which differs between replicas and breaks convergence outright. Instead the
halves keep their lone surrogates and the wire format encodes them faithfully (WTF-8), so the character reassembles
intact.

The four public types are *views* over the same item list:

- **`BcrdtArray`** - items with `ParentKey == null`, read in list order, deleted items skipped.
- **`BcrdtText`** - same as array, but `BcrdtContentString` lengths are character counts and `BcrdtContentFormat`
  items contribute marks rather than characters.
- **`BcrdtMap`** - items with `ParentKey == "k"`. The type keeps a `key -> newest item` index; older items under the
  same key are deleted on write, so reading a key is `O(1)`. Writes to a key chain left to right in integration order,
  and the LWW winner is the item that integration placed right-most - the one with no right neighbour under that key.
- **`BcrdtSet`** - an add-wins observed-remove set projected onto a map keyed by the canonical encoding of each
  element. Concurrent add/remove resolves to *add wins*, because the remove deletes a specific item id while the
  concurrent add creates a new one - provided it does create one: adding an element the replica already holds writes
  nothing, so a concurrent remove of that element stands. A set holds values only; a nested type has no value-based identity and is
  refused.

**Values are stored canonically.** Before anything is integrated, a value is validated against the value model and
converted to exactly what a peer will decode - integers widened to `Int64`, lists and maps copied into
`List<object?>` and `Dictionary<string, object?>` (`BcrdtAnyCodec.Canonicalize`). An unsupported value is therefore
rejected up front, and mutating a list after storing it cannot change one replica's view without producing an update.
Byte arrays are treated as immutable and are not copied.

**A nested type lives in exactly one place.** Inserting a type instance that already belongs to a document, or that is
already waiting inside another detached container, is refused, as is a type that would end up containing itself.
Putting the same contents somewhere else means inserting `Clone()` - a detached deep copy. `BcrdtArray.Move` does
exactly that for nested elements, because removing the original removes everything inside it.

### Operations and the wire format

There is no separate "operation" type. **The block is the operation.** An update is literally a serialized set of
blocks plus a delete set. This is deliberate:

- No operation log to replay separately from state - the state *is* the log, compacted.
- Applying an update and loading a document from disk are the same code path.
- Idempotency is structural: a block whose `(client, clock)` is already covered by the local state vector is skipped
  without inspecting its content.

```
Update        := ClientBlocks DeleteSet
ClientBlocks  := varuint numClients
                 { varuint numBlocks, varuint client, varuint firstClock, Block+ }*
Block         := byte info, [Origin], [RightOrigin], [Parent], [ParentKey], Content
info          := bit7 hasOrigin | bit6 hasRightOrigin | bit5 hasParentKey | bits0-4 contentRef
Origin        := varuint client, varuint clock
Parent        := written only when the block has neither origin
                 (byte isRootName, then string | ItemId)
ParentKey     := string, written only together with Parent (and only when hasParentKey is set);
                 otherwise the receiver derives it from the origin
DeleteSet     := varuint numClients
                 { varuint client, varuint numRanges, { varuint clockDelta, varuint lenMinusOne }* }*
```

In the delete set, `clockDelta` is the range's start clock minus the end of the previous range in the same client run
(zero-based for the first range), and the length is written as `len - 1` because an empty range is never encoded.

- Clocks inside a client run are **delta-encoded** against the run's first clock, so a 10 000-keystroke session encodes
  its clocks in ~1 byte each.
- Integers are LEB128; signed integers are zigzag-then-LEB128.
- Strings are **WTF-8**, not strict UTF-8. Identical to UTF-8 for every well-formed string, with the single addition
  that an unpaired surrogate encodes as its own three-byte sequence rather than being destroyed. This is what makes
  block splitting lossless; strict UTF-8 would silently replace a split surrogate with U+FFFD and diverge the replica
  that performed the split.
- The parent is omitted whenever it can be derived from an origin, which is the common case.
- The format is versioned by a leading byte in the *message* envelope, not per update. Updates are not
  byte-concatenable - each carries its own count prefixes - but they merge: any set of updates can be rewritten as one
  equivalent update without a document.
- **Decoding is all-or-nothing.** `ApplyUpdate` decodes the entire payload - every block and the delete set - before it
  integrates anything, and only then binds root names to the document. A malformed update therefore raises
  `BcrdtFormatException` and leaves the document exactly as it was. Structural checks run at decode time too: a block
  may only refer to its own client at an earlier clock, clocks and delete ranges must stay inside the `Int64` clock
  space, and string payloads must be well-formed WTF-8.

**Merging is a first-class property.** `BcrdtUpdateOperations.Merge(params ReadOnlyMemory<byte>[])` exists and is used
by persistence - to batch appends and to compact a log into one record - and by the decoder to fold together blocks
that are waiting for their dependencies. The shipped relays forward messages without decoding them, so they do not
merge; a relay that wanted to coalesce a burst could.

### Identity and clocks

`ClientId` is a `ulong` drawn from a cryptographic RNG **per document session**, not per user and not per device.
Collision probability across a document's lifetime is negligible (birthday bound at 2⁶⁴), and the value is never
reused. A client id is **not** a user id: the user lives in the awareness layer, because identity is a presentation
concern and the CRDT must not depend on it.

Each client keeps a **monotonic Lamport-style counter** that increments by the *length* of every block it creates. So
`(client, clock)` is not just unique, it addresses a specific logical unit - `(c, 105)` is the 6th character of a
block created at `(c, 100)` with length 10. That property is what allows a block to be split without invalidating
references to its interior.

The document's `BcrdtStateVector` maps `client -> next expected clock`. It is the **complete causal summary** of what a
replica knows, `O(number of clients)` in size (typically tens of bytes), and the only thing a client needs to send to
request exactly the delta it is missing.

Causality is enforced structurally rather than with vector-clock comparison per operation: a block is integrable iff
its origin, right-origin and parent are all already present. Blocks that are not yet integrable are parked in the
store as a pending update, alongside a state vector describing the dependencies they are waiting for, and deletions
that target blocks not yet received are parked as a pending delete set. Both are retried automatically whenever new
blocks arrive.

### The synchronization protocol

Transport-agnostic, message-oriented, and **symmetric** - the same state machine runs on a peer, a browser tab and a
server. A server that holds a replica is just a peer that happens to be well-connected.

```
Message := byte version(=1), byte kind, payload
kind: 0 SyncStep1 | 1 SyncStep2 | 2 Update | 3 AwarenessUpdate
      4 AwarenessQuery | 5 Auth | 6 Ping | 7 Pong | 8 SyncStep1Reply
Auth  := byte grant (0 refused | 1 read-write | 2 read-only), string reason
```

```
A                                              B
|<-- Auth(grant) ------------------------------|      from the relay, on every join
|-- SyncStep1(stateVector(A)) ---------------->|      announcement, on every (re)connect
|<-- SyncStep1Reply(stateVector(B), digest(B))-|      always: B states what it holds - small, so first
|<-- SyncStep2(diff B\A) ----------------------|      possibly as several messages, each an ordinary update
|-- SyncStep2(diff A\B) ---------------------->|      unless B provably holds everything A does
|                                              |
|== both synced; steady state =================|
|-- Update(delta) ---------------------------->|      broadcast on every local commit
|<-- Update(delta) ----------------------------|
```

A grant of `2` reads as "permitted" to a client that predates read-only access, which is why the values are not in
privilege order; an unknown non-zero grant reads as read-only. A delta larger than the sender's `MaxMessageSize` is cut
into several updates (`BcrdtUpdateOperations.Split`), the delete set travelling with the first, so a receiver that
holds every block holds every deletion; a receiver applies each as it comes and needs nothing new to do so.

The digest is a 64-bit, order-independent hash of the replier's complete delete set. It exists because a deletion
does not advance a state vector: a replica that has only *deleted* things while offline looks, by state vector alone,
as if it held nothing new. A `SyncStep1Reply` is answered with a `SyncStep2` - never with another reply, which is what
makes the exchange terminate - and that answer is skipped only when the replier's state vector covers the answerer's
and the two digests match. Over a broadcast relay this keeps replicas that are already in sync quiet instead of
costing a cubic number of deliveries per join. A provider weighs a reply's digest only once the replier's delta has
arrived, so a replica returning to a room that deleted text in its absence does not send the room's own deletions back
to everyone.

A replica counts as **Synced** once it has applied a delta and its own state vector covers every state vector it has
been shown in a reply *so far*. Over a relay that can be the first peer to answer: a better-informed peer's reply that
arrives afterwards is still applied in full, it just does not hold the status back. If nobody answers within
`BcrdtSyncProviderOptions.HandshakeTimeout` - the first replica in an empty room - it counts as synced with nobody; if
somebody answered but their data has not all arrived, it announces again, waiting twice as long each time (up to a
minute) so a large document on a slow link is not asked for over and over. The reply goes ahead of the delta precisely
so that a slow delta does not look like an empty room. With a [document host](#keeping-documents-on-the-server) the
host is always there to answer.

Blocks a replica received but cannot place yet - they build on something it has not been sent - are still part of what
it knows: `EncodeStateAsUpdate` includes them (and deletions waiting for their targets), as Yjs does, so a replica that
relays between two others never holds back what it could pass on.

- **Incremental by construction.** `SyncStep2` carries `EncodeStateAsUpdate(remoteStateVector)` - only blocks the peer
  lacks.
- **Idempotent.** Re-delivering any message is a no-op; duplicate detection is the state vector check.
- **Order-insensitive.** Updates may arrive in any order; unintegrable blocks wait in the pending queue.
- **Loss-tolerant.** A lost `Update` is repaired by the next `SyncStep1`, which the client re-issues on every reconnect
  and, if `ResyncInterval` is set, periodically.
- **Symmetric under reconnection.** Receiving a `SyncStep1` always prompts a `SyncStep2` *and* a `SyncStep1Reply`, so a
  client returning from an offline session is both brought up to date and asked for the work it did offline -
  deletions included.
- **Offline-first.** While disconnected, local commits accumulate in the document and, if a store is attached, in
  storage. On reconnect one exchange in each direction reconciles an arbitrarily long divergence.
- **Thread-affine.** A document is not thread-safe, but transports raise their events on whatever thread their I/O
  completes on. `BcrdtSyncProvider` captures the `SynchronizationContext` it was created on - the renderer's, in Blazor -
  and runs every transport callback, heartbeat and reconnect step there.

`BcrdtSyncProvider` owns the connection state machine
(`Disconnected → Connecting → Handshaking → Synced → Reconnecting`, or `Faulted` when it gives up or is refused) with
**full-jitter exponential backoff** (`delay = rand(0, min(cap, base·2ⁿ))`) and a configurable cap. Detecting a
connection that died without closing is the transport's job: the WebSocket transport uses WebSocket keep-alives and
SignalR uses its server timeout. The heartbeat only refreshes presence; it does not broadcast pings, which over a relay
would cost every peer an answer from every other peer.

### Awareness

- **Storage**: `clientId -> (clock, JSON-ish state)`. Last-write-wins per client, ordered by a per-client awareness
  clock. A client may only author its *own* entry.
- **Liveness**: each entry has a TTL (default 30 s) refreshed by a heartbeat (default 10 s). Entries that time out are
  removed and reported as removed. This is what makes a hard-crashed tab disappear from the presence list without any
  server bookkeeping.
- **Never persisted.** Awareness state is not part of the document, is not in the update stream, and is not written by
  stores. A document loaded from disk has an empty awareness set, which is correct.
- **Wire format**: its own message kind, encoding only changed clients.

### Persistence

```
IBcrdtDocumentStore
 ├── AppendUpdateAsync(docId, update)        hot path: O(1) append
 ├── LoadAsync(docId)                        returns merged update + metadata
 ├── CompactAsync(docId)                     collapse N updates into 1 snapshot
 ├── DeleteAsync(docId)
 └── ListAsync()                             the document identifiers the store holds
```

Persistence is **append-only with periodic compaction**, not read-modify-write. Appending is cheap and crash-safe;
compaction runs after a threshold and replaces the log with a single merged update. Because updates merge, compaction
is `Merge(all)` - no CRDT knowledge required in a store implementation. A **partially written trailing record is
detected and discarded** on load (length-prefixed records with a checksum), so a crash mid-append costs at most the
last update, and the document still converges once a peer re-syncs.

### Undo and redo

Naïve undo (revert to a previous document state) is wrong in a collaborative system: it would discard other users'
concurrent work. `BcrdtUndoManager` implements **selective, per-origin undo**:

- It tracks only changes whose transaction `Origin` is in `BcrdtUndoManagerOptions.TrackedOrigins` (an origin object,
  or its type). By default that is untagged local edits - origin `null` - plus the manager's own undo and redo
  transactions. Remote updates carry their sync provider as origin and are therefore excluded.
- It is scoped to a set of types, so a form field's undo stack does not swallow a sibling's edits.
- A stack item records two delete sets: **insertions** made by the action and **deletions** made by the action.
- **Undo** = delete the recorded insertions + re-insert the recorded deletions (as *new* blocks, with `Redone`
  back-references so redo can find them).
- Adjacent actions within `CaptureTimeout` (default 500 ms) coalesce into one stack item, so typing a word is one undo
  step. Remote changes that arrive between two local edits break the capture window, which is what makes concurrent
  undo behave sanely.

### Garbage collection and compaction

1. **Block merging** (every transaction, cheap). Adjacent blocks from the same client with adjacent clocks, identical
   deleted state and mergeable content collapse into one. This is what keeps a long typing session at a handful of
   blocks.
2. **Delete-set compaction** (every transaction). Ranges are sorted and coalesced; `[0,5) ∪ [5,9)` becomes `[0,9)`.
3. **Content GC** (on by default through `BcrdtDocumentOptions.GarbageCollect`, run at the end of every transaction over
   what that transaction deleted). A deleted block that no live block depends on has its content replaced by
   `BcrdtContentDeleted`, freeing the payload while preserving the identifier. Blocks marked `Keep` are exempt.
4. **Snapshot compaction** (persistence layer). Replace an update log with one merged update.

A `BcrdtUndoManager` marks the deleted content its stack could still restore as `Keep`, so undo works on a collecting
document; the protection is enforced by the flag rather than by convention, and is released when the step is dropped,
together with the containers it pinned above that content, unless another step still needs them.

### Performance

Budgets are measured by benchmarks, not taken on hope: every row below has a benchmark in
[`Tests/Bit.Bcrdt.Benchmarks`](Tests/Bit.Bcrdt.Benchmarks/) (its README maps each budget to the benchmark that measures
it). Targets on a 2020-class desktop CPU:

| Scenario | Budget |
|---|---|
| Insert 100 000 characters sequentially | < 150 ms |
| Apply a 100 000-char update from a peer | < 60 ms |
| Encode 100 000-char document | < 25 ms, < 1.2× raw UTF-8 size |
| Merge two 50 000-op divergent replicas | < 200 ms |
| State vector encode/decode (100 clients) | < 10 µs |
| Steady-state keystroke (local, 1 MB doc) | < 50 µs, 0 gen-2 allocations |
| Awareness update fan-out (100 clients) | < 1 ms |

- **Constant-time splits.** A text block holds a slice of a string shared with the blocks it was split from, plus
  whatever was typed onto its end since, so inserting into the middle of a long block, or deleting from it, copies
  nothing - and no update can make a replica do work in the square of a block's length.
- **Run-length blocks** - the single biggest win. 100 000 characters typed sequentially is one block, not 100 000
  objects.
- **`ArrayPool<byte>` throughout encoding**, `Span<byte>` arithmetic, no intermediate `MemoryStream`, no LINQ on hot
  paths.
- **Binary search** over the per-client contiguous struct list - `O(log n)` addressing.
- **Cached index → item search markers** per type, so sequential access (the dominant editor pattern) is amortized
  `O(1)` instead of `O(n)` list walking. Text that has ever carried formatting does not use them: a marker skips the
  marks before it, so a lookup starting there would not know which attributes are in force.
- **Struct-based ids and readers/writers** to keep the hot path off the heap.
- **Transaction batching** - N mutations produce exactly one update message and one event flush.
- **Delta events** rather than "something changed" - a Blazor component re-renders from a delta, not a full re-read of
  the document.
- **A cached document-level delete set.** Every sync response carries the complete delete set, and deriving it means
  scanning every block. It is derived once per change and reused, invalidated by *any* change rather than by reasoning
  about which changes could affect it.

The delete set is deliberately sent in full rather than diffed. A deletion does not advance a state vector, so a peer
cannot detect that it is missing one; re-sending the set is what repairs a lost deletion. It is small relative to
content and idempotent to apply, so the trade is worth it.

---

## Correctness

The suite is not a set of examples; it is an attempt to break the convergence property.

- **Randomized property tests** over 25 seeds: several replicas, random operations, random delivery schedules,
  asserting that replicas which received the same updates hold identical state.
- **Adversarial networks**: 25% message loss, 25% duplication, reordering, partitions, and replicas editing while
  offline for extended periods.
- **Oracle tests** comparing sequence behaviour against a plain `List<T>` and `StringBuilder`.
- **Fuzz tests**: every truncation and every single-byte mutation of a valid update, plus hostile payloads that declare
  enormous collection sizes. A hostile payload must be rejected with `BcrdtFormatException`, and a rejected update must
  leave the document exactly as it was. A truncation or mutation that happens to still be a well-formed update may be
  applied, but must never crash, hang, or leave the document unreadable.
- **Persistence recovery**: torn records, broken checksums, path traversal in document identifiers.
- **Concurrent undo**, snapshot restoration, presence expiry.
- **Allocation budgets** (`PerformanceBudgetTests`) for the algorithmic properties behind the performance budgets -
  incremental sync size, block merging, keystroke update size, cached delete sets.

Every randomized test prints its seed, and every seed reproduces exactly. The guiding rule: **a test that cannot fail
deterministically is not a test.**

Four real defects were found by this suite during development, all of the kind example-based tests do not find:

1. **Stale index caches after a remote update.** Sequential editing is kept cheap by caching "index *n* is at this
   block". A local edit updates those caches as it goes, because it knows where it is editing. A *remote* update
   rearranges blocks arbitrarily and cannot, so every cached position silently starts describing the wrong place. Fix:
   drop the cache whenever a transaction that integrated an update touches a type - including a transaction that was
   opened as local and had an update applied inside it, whose own edits still count as local.
2. **Surrogate pairs destroyed by local block splits.** Splitting a text block between the halves of a surrogate pair
   leaves two lone surrogates, which strict UTF-8 cannot represent and silently replaces with U+FFFD. The damage
   depends on *whether a given replica happened to split that block*, which differs between replicas, so two replicas
   that had received identical operations displayed different text and could never reconcile. Fix: lossless splits
   plus WTF-8 on the wire.
3. **An asymmetric reconnection handshake.** A returning client sent `SyncStep1` and the peer answered with
   `SyncStep2`, so the client caught up - and the work it had done offline was never collected, because nobody ever
   asked for it. A first fix - asking back only when the returning client's state vector showed blocks the peer
   lacked - still lost every offline session that had only *deleted* things. Fix: a `SyncStep1` is always answered
   with a `SyncStep1Reply` carrying the replier's state vector and a digest of its deletions.
4. **Sync responses that scaled with the document.** Answering "anything new?" rebuilt the entire delete set by
   scanning every block, so a peer that was already up to date cost time and allocation proportional to the whole
   document. A fifty-replica benchmark surfaced it as tens of gigabytes of allocation. Fix: cache the derived delete
   set per document version. Measured effect on a document with 80 000 characters: 69 KB per response down to 3.3 KB,
   and flat with respect to content thereafter.

The first two were found by randomized multi-replica tests, the third by an integration test that took a client
offline, edited on both sides, and reconnected, and the fourth by a benchmark - it is now guarded by an
allocation-budget test, because it is the kind of regression that reappears silently.

An adversarial review of the whole library found more, each now fixed and pinned by a test that failed before
(`HardeningTests`, `AccessAndHostingTests`, `BrowserBridgeTests`, and additions to the persistence, undo and sync suites):

- **Length overflow.** Two adjacent collected ranges near the 2³¹ block-length limit - one well-formed 12-byte update -
  merged into a negative length, after which the replica's state vector was unreadable to every peer. Blocks that
  would overflow are no longer merged, and a gap wider than one block becomes several skips.
- **Unbounded nesting.** A chain of nested types a few bytes per level, from a peer, overflowed the stack the moment
  its top was deleted - an exception nothing can catch. Nesting is capped, identically on every replica.
- **Allocation amplification.** Sixty-odd nested collections, each claiming as many entries as the payload had bytes,
  made the decoder reserve gigabytes for a megabyte. Reservations are bounded.
- **Stale index caches.** A local edit after an update in the same transaction landed at the wrong index. Caches are now
  dropped the moment remote content lands.
- **Quadratic splits.** Splitting a text block copied it, so one update deleting every other character of a long block
  cost time in the square of its length. Splits are constant-time slices.
- **Oversized messages.** A document larger than a relay's message limit got every peer that answered a newcomer
  disconnected, in a loop. Deltas are split below a size limit.
- **Premature sync and runaway reconnection.** A slow delta made a joiner conclude the room was empty; a server that
  dropped every connection straight away was retried at the shortest delay for ever.
- **Duplicated text in the editor.** An update the browser applied late, against text that had changed and changed
  back, was ignored by .NET and then re-applied by the next keystroke. Updates are now tied to the number of changes the
  browser has reported, and the browser and .NET exchange changes instead of whole values.
- **Destroyed records.** One damaged record in the middle of a file log truncated every intact record after it.
- **Presence takeover.** A forged awareness clock at the top of the range wrapped the victim's clock and froze their
  entry for the rest of the session.

---

## Limitations

What bit Bcrdt does not do, deliberately or not yet:

- **Its own protocol.** The wire format is YATA-family like Yjs's, but it is not Yjs's: a Bcrdt replica cannot sync
  with a Yjs one. Versioning is by the leading byte of every message; a peer rejects a version or kind it does not know
  rather than guessing.
- **Plain text and Quill-style rich text.** `BcrdtText` carries formatting marks and embeds (`Format`, `ToDelta`,
  `ApplyDelta`), which is the Quill delta model; there is no XML or tree type for ProseMirror-style editors, and no
  ready-made rich-text editor component - only `BcrdtTextArea`, for plain text.
- **No conflict-free move.** `BcrdtArray.Move` is delete-then-insert: concurrent moves of one element duplicate it.
- **Interleaving.** YATA keeps concurrently typed runs together in the common cases, but adversarial patterns at the
  exact same position can interleave at block granularity; Fugue-class algorithms avoid that.
- **No authenticated authorship.** Any writer can create blocks under any client identifier. Validate on the server
  when that matters.
- **History needs `GarbageCollect = false`.** Snapshots restore only from documents that keep their tombstones.
- **Scale-out hosting.** A document host keeps one replica per document per process; behind a backplane set
  `ScaleOut`, give every instance the same store - one built for several processes writing at once, which
  `BcrdtFileSystemDocumentStore` is not: each of its directories belongs to one process - and prefer routing a
  document's connections to one instance.
- **Indivisible values.** One byte array or embedded object larger than a relay's message limit cannot be split.
- **One process per file-store directory.**
- **SignalR fan-out waits for the slowest member.** The hub sends to a document's group through SignalR, which completes
  a send once every member's connection has taken it; a client that stops reading slows the senders in its group until
  SignalR's own timeouts drop it. The WebSocket relay queues per member and evicts one that falls behind. A document
  host's answer to a client's own handshake is not counted against that queue: it is queued as the client drains it,
  and the client's next message waits until it is.
- **Pending updates are held without a bound.** Changes that wait for something they build on are kept until it
  arrives, as in Yjs; a peer that sends many of them on purpose grows the receiver's memory. Rate-limit untrusted
  writers at the server.

## Upgrading

Changes since the previous release that an application may notice:

- **Relays send an `Auth` grant on every join**, read-write included, before anything else. Clients of every version
  read a non-zero grant as permission. A test that reads a raw socket must read past it.
- **Relays no longer pass on `Ping` or `Pong`.** Nothing in the library sends them; a custom peer that used them through
  a relay should use its transport's keep-alive instead.
- **A `SyncStep1` is answered with `SyncStep1Reply` first, then `SyncStep2`** - possibly several. Handling them with
  `BcrdtSyncExchange` needs no change; code that relied on the order should not.
- **`BcrdtHub.OnDisconnectedAsync` is overridden.** A subclass overriding it must call the base implementation.
- **`BcrdtUndoManagerOptions.MaxStackItems` defaults to 100.** Set it to zero for the previous unbounded stacks.
- **Collaborative types nest at most 128 levels.** Deeper nesting throws locally and is dropped when it arrives.
- **`BcrdtCollaborationScope` does not open documents while prerendering.** On .NET 8 it opens them after its first
  interactive render.
- **`EncodeStateAsUpdate` includes updates a replica is still waiting to place**, as Yjs does.
- **The browser module's exports changed.** It is internal to the components; the unused `getSelection`,
  `setSelection`, `setValuePreservingSelection`, `focus` and `measureCaret` are gone.

---

## MCP server

The demo hosts a [Model Context Protocol](https://modelcontextprotocol.io) server at `https://bcrdt.bitplatform.dev/mcp`
(`http://localhost:5560/mcp` locally), so an AI agent writing collaborative features reads the library itself - its
API, this guide and the demo's working pages - instead of guessing at an API it half remembers.

```json
{
  "mcpServers": {
    "bcrdt": { "type": "http", "url": "https://bcrdt.bitplatform.dev/mcp" }
  }
}
```

Start with `SearchBcrdt`, which searches this guide, every public type and member, the setup guides and the demo's
sources at once, and points each hit at the exact follow-up call. `GetBcrdtSetupGuide` gives the wiring for one hosting
model - a Blazor Web App, standalone WebAssembly, Blazor Server, Hybrid, the WebSocket relay or a non-UI peer -
`GetBcrdtGuideSection` one section of this guide, `GetBcrdtApi` one public type with its members, real defaults and
documentation, and `GetBcrdtSourceFile` one of the demo's working pages; each returns its index when called without a
key. `InspectBcrdtMerge` runs the real engine: it applies concurrent edits to separate replicas, exchanges their
updates in every delivery order and reports what each replica held before the merge, what they all converge to, how
large the updates were and what was decided for each contested key, text, list or set - the quickest way to see what a
merge will decide before building on it. `InspectBcrdtUpdate` decodes an update or a protocol message - blocks per
client, delete set, implied state vector, sizes - and reports why a malformed one is rejected. Both run a sample when
called without input. Every tool is read-only, and every one is also a plain HTTP GET under `/api/mcp/...`.

---

## Projects

| Project | Description |
|---|---|
| `Bit.Bcrdt` | The engine, presence, persistence, sync, collaboration service, bindings and the in-memory and WebSocket client transports; the Blazor components and `BcrdtComponentBase` in `Blazor/` (TypeScript in `Scripts/`, compiled into `wwwroot/` on build). |
| `Bit.Bcrdt.SignalR` | The SignalR client transport. |
| `Bit.Bcrdt.Server` | The ASP.NET Core relays: the SignalR hub and the WebSocket endpoint, the document host behind them, and the Blazor Server presence circuit handler. |
| `Bit.Bcrdt.Testing` | The network simulator, operation generators, convergence harness and update corrupter. |
| `Demo` | The demo (`Client` + prerendering `Server` host): side-by-side replicas over the in-memory transport, a live page across tabs through the server's relays, and the MCP server. |
| `Samples` | The hosting samples: the `Core` pages hosted under `Server`, `Wasm` and `Auto` (each with its own relay) and `Maui` (local-first, file-system store). |
| `Tests/Bit.Bcrdt.Tests` | MSTest tests - convergence, adversarial networks, fuzzing, persistence recovery, undo, presence, the transports and relays, the demo's own plumbing - on every target framework. |
| `Tests/Bit.Bcrdt.Tests.Mcp` | MCP server tests, over the real protocol against the demo server's own `Program.cs`. |
| `Tests/Bit.Bcrdt.Tests.E2E` | Playwright tests of the demo in a real browser, including collaboration between tabs. See its [README](Tests/Bit.Bcrdt.Tests.E2E/README.md). |
| `Tests/Bit.Bcrdt.Benchmarks` | BenchmarkDotNet suites for the performance budgets. See its [README](Tests/Bit.Bcrdt.Benchmarks/README.md). |

## Build and test

```bash
dotnet build src/Utilities/Bcrdt/Bit.Bcrdt.slnx
cd src/Utilities/Bcrdt/Tests/Bit.Bcrdt.Tests && dotnet test
cd src/Utilities/Bcrdt/Tests/Bit.Bcrdt.Tests.Mcp && dotnet test
cd src/Utilities/Bcrdt/Tests/Bit.Bcrdt.Tests.E2E && dotnet test       # needs a Playwright browser; BCRDT_E2E_BROWSER=firefox/webkit for the others, see its README
dotnet run --project src/Utilities/Bcrdt/Demo/Server                    # the demo, at http://localhost:5560
dotnet run -c Release --project src/Utilities/Bcrdt/Tests/Bit.Bcrdt.Benchmarks -- --filter "*Text*"
```

Run `dotnet test` from inside `src` so `src/global.json`'s Microsoft.Testing.Platform runner setting applies.
