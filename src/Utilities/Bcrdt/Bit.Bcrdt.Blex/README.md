# Bit.Bcrdt.Blex

Collaborative state for [bit Blex](https://www.nuget.org/packages/Bit.Blex): a Blex store whose state is part of a
[bit Bcrdt](https://www.nuget.org/packages/Bit.Bcrdt) document, shared with everyone who has the document open.

```bash
dotnet add package Bit.Bcrdt.Blex
```

```csharp
using Bit.Bcrdt.Blex;

public sealed record Board(string Title, IReadOnlyList<string> Cards, long Votes)
{
    public static Board Read(BcrdtMap board) => new(
        board.Get<string>("title") ?? "",
        [.. (board.GetArray("cards")?.ToArray() ?? []).OfType<string>()],
        board.GetCounter("votes")?.Value ?? 0);
}

public sealed class BoardStore(BcrdtMap board) : BcrdtBlexStore<Board>("board", board, () => Board.Read(board))
{
    public void Rename(string title) => Act("Rename", () => board.Set("title", title), new BlexActionArg("title", title));
    public void AddCard(string card) => Act("Add card", () => board.EnsureArray("cards").Push(card));
    public void Vote() => Act("Vote", () => board.EnsureCounter("votes").Increment());
}
```

- **The document is the state.** `State` is what the projection reads out of the subtree - a map, an array or a
  text - read again after every change. Return an immutable value: a record compares by value, so a selector over one
  of its members fires only when that member changed.
- **An action is a transaction.** `Act` runs the change as one named Blex action and one document transaction tagged
  with `store.Origin`: one entry for middleware and DevTools, one update on the wire, one step for an undo manager. A
  filter that vetoes the action stops the change as well.
- **Everyone else's changes are actions too.** A collaborator's change, an undo, another store on the same document:
  each is dispatched as `"Remote change"` (`RemoteActionName`), so `Subscribe`, `<BlexSelect>`, `manager.Subscribe`,
  `CreateSelector`, middleware and DevTools all see it the way they see anything else.
- **No echo.** A transaction of the store's own origin is not dispatched again - the action that made it has already
  projected it - and a remote change writes nothing back. A subscriber that answers a remote change with an action of
  its own makes one transaction, which the other replicas see once.

There is no `[BlexStore]` and nothing for the generator to do: the store is a plain class deriving from
`BcrdtBlexStore<TState>`, or `new BcrdtBlexStore<string>("notes", text, text.ToString)` with no class at all. It has
no generated `Actions` descriptors; override `DescribeActions` to make its actions dispatchable by name.

## Registering it

A store over a document exists once the document is open, so create it then and register it for as long as the
document is open - `manager.CreateScope(new BoardStore(document.Crdt.GetMap("board")), key: documentId)`, or
`<BlexScope TStore="BoardStore" Key="@documentId" Factory="...">` - rather than with `AddBitBlexStore`.

Create the collections a store writes into once, where the document is first seeded: two replicas calling
`EnsureArray("cards")` on an empty document at the same moment each create one, and the document keeps only one of
the two, with what was pushed into it.

## Undo, time travel and persistence

A restore - `BlexHistory`, DevTools time travel, persistence, the prerender handoff - cannot rewrite a document other
people share, so it reads the document again instead of taking the snapshot. Snapshot undo would take back
collaborators' work along with the user's: keep the store out of `BlexHistory` and undo with a `BcrdtUndoManager`
over `Source` that tracks `Origin`, which takes back only this store's actions and is itself dispatched as a remote
change. Persist the document, not the store - `Persist` is `false`.

## Threading

Like the document and every Blex store, it is not thread-safe: use it on the context the document is changed on - in
Blazor the renderer's, which is where a document opened through the collaboration service applies network updates.

The full guide is in the [Bcrdt README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bcrdt).
