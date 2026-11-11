# Bit.Bcrdt.Blow

[bit Bcrdt](https://www.nuget.org/packages/Bit.Bcrdt) as the collaboration backend for
[bit Blow](https://www.nuget.org/packages/Bit.Blow): several people editing one flow - nodes, edges, positions,
sizes, labels, data - with offline editing, reconnection, persistence, presence and selective undo coming from Bcrdt.

```bash
dotnet add package Bit.Bcrdt.Blow
```

```razor
<BlowCanvas @ref="_canvas" Nodes="_nodes" Edges="_edges"
            OnGraphChanged="change => _binding!.OnGraphChangedAsync(change)"
            OnSelectionChange="_ => _binding!.PublishSelection()" />

@code {
    [CascadingParameter] public BcrdtCollaborativeDocument Document { get; set; } = default!;

    private readonly List<BlowNode> _nodes = [];
    private readonly List<BlowEdge> _edges = [];
    private BlowCanvas? _canvas;
    private BcrdtBlowBinding? _binding;

    protected override void OnInitialized() =>
        _binding = new BcrdtBlowBinding(Document.Crdt.GetMap("flow"), _nodes, _edges, new BcrdtBlowBindingOptions
        {
            RemoteEditScope = edit => _canvas!.EditAsync(edit, "remote", BlowChangeOrigin.Remote, recordHistory: false),
            Presence = Document.Presence,   // optional: cursors and selections
        });

    public void Dispose() => _binding?.Dispose();
}
```

## How the graph is shared

Every node and every edge is an entry of its own in the bound map, keyed by its id - `n:{id}` and `e:{id}`, each a
nested map - holding the element's properties in Blow's own file format, one key per property.

- **Different elements never conflict**, and a deletion is by id.
- **Different properties of one node all survive.** One person dragging a node, another renaming it and a third
  editing its data keeps all three, because position, label and data are different keys. Two changes to the same
  property resolve to one of them, the same one on every replica.
- **List order is a field.** Each entry carries `$order`, a fractional order key (`BcrdtOrderKey`) its neighbours are
  sorted by (ties broken by id), so `BringToFrontAsync` and `SendToBackAsync` are one field write per moved element,
  concurrent reorders converge, and nothing is ever renumbered.
- **Selection, measured sizes, handle anchors and drag state are never shared.** They are each participant's own;
  selection travels through presence.
- **An edge is shown only while both of its nodes are.** One drawn to a node a collaborator deleted at the same time
  is kept in the shared state, out of every replica's lists, and comes back if the node does.
- **Data objects come back with their keys in ordinal order**, as the collaborative value model keeps them; data of a
  type of your own needs the same `MapNodeData` / `MapEdgeData` mappings on every participant
  (`BcrdtBlowBindingOptions.SerializerOptions`), as it would to load a file.

## Commits, echoes and origins

Blow's model is plain lists the canvas edits in place, and the canvas reports each finished edit - not each pointer
move - through `OnGraphChanged`. `OnGraphChangedAsync` commits it: `Commit` compares the lists with the shared state
and writes only the properties that differ, as one transaction tagged with `Origin`. Call `Commit` yourself after
changing the lists from your own code.

Everything else that changes the map - a collaborator, an undo, your own code - is applied to the same objects the
canvas holds, inside `RemoteEditScope`. With the scope above the canvas renders once, raises `OnGraphChanged` with
`BlowChangeOrigin.Remote` (which `OnGraphChangedAsync` ignores) and writes the edit into its history instead of
recording it. A remote change to one property applies to that property alone, so a node being dragged here keeps
following the pointer when somebody renames it. Without a scope, handle `RemoteChanged` and refresh the canvas.

## Joining and starting

Binding to a map that already holds a graph replaces the lists' contents with it; bind in `OnInitialized`, before the
canvas first renders. Binding non-empty lists to an empty map writes them there, in one transaction.

## Undo

`CreateUndoManager()` returns a `BcrdtUndoManager` scoped to the bound map and tracking the binding's origin: it undoes
this participant's commits and nobody else's, property by property. The canvas's own history (`HistoryEnabled`) keeps
working beside it, and its undo is committed like any other edit; it restores whole elements, with the remote edits it
was told about written into its steps. Use one or the other for a graph.

## Presence

With `Presence` set, `PublishCursor` and `PublishSelection` publish under the `blow` presence field, and
`Participants` reads everybody else's cursor and selected nodes and edges.

## Limits

- `Commit` reads the whole graph through Blow's serializer, so it costs in proportion to the graph: call it at the end
  of an edit, not on every pointer move.
- An element that breaks Blow's import limits, or cannot be read, is left out of this replica's lists and reported
  through `RemoteChangeRejected`.
- A repeated id in a list is shared once, as the first element with it.

The full guide is in the [Bcrdt README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bcrdt).
