# Bit.Bcrdt.Bink

[bit Bcrdt](https://www.nuget.org/packages/Bit.Bcrdt) as the collaboration backend for
[bit Bink](https://www.nuget.org/packages/Bit.Bink): several people drawing on one `BinkDocument`, with offline
editing, reconnection, persistence, presence and selective undo coming from Bcrdt instead of Bink's own
`BinkCollaborationSession`.

```bash
dotnet add package Bit.Bcrdt.Bink
```

```razor
<BinkCanvas @ref="_canvas" Document="_drawing" />

@code {
    [CascadingParameter] public BcrdtCollaborativeDocument Document { get; set; } = default!;

    private readonly BinkDocument _drawing = new();
    private BinkCanvas? _canvas;
    private BcrdtBinkBinding? _binding;

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender) return;
        _binding = new BcrdtBinkBinding(Document.Crdt.GetMap("drawing"), _drawing, new BcrdtBinkBindingOptions
        {
            History = _canvas!.History,       // one transaction per gesture
            Presence = Document.Presence,      // optional: cursors and selections
            Selection = _canvas.Selection,
        });
    }

    public void Dispose() => _binding?.Dispose();
}
```

## How the drawing is shared

Every layer and every element is an entry of its own in the bound map, keyed by its identifier - `l:{id}` and
`e:{id}`, each a nested map - holding the object's properties in Bink's own file format, one key per property. Page
settings are `s:width`, `s:height` and `s:background`; document metadata is `m:{key}`.

- **Different elements never conflict.** Two people drawing, moving or deleting different elements touch different
  entries, and a deletion is by identifier, never by position.
- **Different properties of one element both survive.** One person moving a shape while another recolours it keeps
  both, because the transform and the style are different keys. Two changes to the same property resolve to one of
  them, the same one on every replica.
- **A finished stroke's points are one value.** They never change after the pen lifts, so there is nothing to merge.
- **Stacking order and layer membership are fields.** Each entry carries `$order`, a number its neighbours are sorted
  by (ties broken by identifier), and an element carries `$layer`. Bringing an element to the front or moving it to
  another layer is one field write, so concurrent reorders converge on one order instead of Bink's index-based
  operations leaving replicas stacked differently. Repeatedly inserting at the same place halves the gap each time;
  when it runs out the layer is renumbered, which writes every element in it.
- **A deleted layer takes its elements with it**, including any a collaborator drew into it concurrently: such an
  element stays in the shared state but no replica shows it.

## Gestures, echoes and origins

The binding listens to `BinkDocument.Changed`. With `History` set, changes are held until the history records a step,
so a drag of sixty frames is one transaction, one update on the wire and one undo step; without it every change is
written as it is reported. Each transaction is tagged with `Origin` (the binding itself unless you give one).

Everything else that changes the map - a collaborator, an undo, your own code - is applied to the drawing as one
batch with `BinkChangeOrigin.Remote`, which Bink's history does not record and the binding does not write back.
Changes with `BinkChangeOrigin.Load` are not written either, as Bink's own collaboration session does not send them.

An element this participant is still changing keeps its unwritten properties when a collaborator's change to the
same element arrives; only the properties that change touched are taken from it.

## Joining and starting

Binding to a map that already holds a drawing replaces the document's content with it. Binding a document with
content - elements, a second layer, metadata - to an empty map writes it there, in one transaction. A new, empty
document writes nothing and shows the shared default layer, `BcrdtBinkBinding.DefaultLayerId`, so two people opening a
new drawing draw into the same layer rather than each into their own.

## Undo

`CreateUndoManager()` returns a `BcrdtUndoManager` scoped to the bound map and tracking the binding's origin: it undoes
this participant's changes and nobody else's, property by property. Bink's own history keeps working beside it, and
its undo is written like any other change - but it restores whole elements, so undoing a move of a shape somebody has
since recoloured restores the old colour too. Use one or the other for a drawing.

## Presence

With `Presence` set, `PublishCursor` and `PublishSelection` (automatic when `Selection` is given) publish under the
`bink` presence field, and `Participants` reads everybody else's cursor and selection.

## Limits

- Properties a newer Bink writes and an older one cannot read are dropped from an element when the older one edits it,
  as with Bink's own collaboration.
- An element that breaks this replica's `BinkDocumentLimits`, or cannot be read, is left out of its drawing and
  reported through `RemoteChangeRejected`; it stays in the shared state for the replicas that can show it.
- A stroke in progress is not shared - only finished ones are. Live ink belongs in presence.

The full guide is in the [Bcrdt README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bcrdt).
