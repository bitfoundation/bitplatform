# Bit.Bcrdt.Blazor

Blazor integration for [bit Bcrdt](https://www.nuget.org/packages/Bit.Bcrdt), real-time collaborative editing written
natively in C#.

## Quick start

```bash
dotnet add package Bit.Bcrdt.Blazor
```

```csharp
builder.Services.AddBitBcrdtServices(collaboration => collaboration.UseInMemoryStore());
```

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

That is a working collaborative editor with live presence, automatic reconnection and offline support. Add a
transport - `Bit.Bcrdt.SignalR`, or the WebSocket transport built into `Bit.Bcrdt` - to reach other users.

## What is in it

- `BcrdtCollaborationScope` opens a document for its content, cascades it, and closes it again.
- `BcrdtTextArea` edits a `BcrdtText`: what the user types becomes the minimal CRDT edit, remote edits arrive without
  moving the caret or overwriting keystrokes still in flight, and the caret is published through presence.
- `BcrdtPresenceView` lists the people on a document.
- `BcrdtComponentBase` marshals change notifications onto the renderer and coalesces them onto a frame budget, and
  disposes its subscriptions with the component - the base for your own collaborative components.

No stylesheet to reference and no script to add: the one small module `BcrdtTextArea` needs is imported on demand.
Works in Blazor WebAssembly, Server, Web App (including prerendering) and Hybrid on .NET 8, 9 and 10.

## Documentation

The full guide is in the [Bcrdt README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bcrdt). See it
running at [bcrdt.bitplatform.dev](https://bcrdt.bitplatform.dev).
