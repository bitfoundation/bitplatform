# Bit.Bcrdt.Borm

Collaborative forms: several people filling in one [bit Borm](https://www.nuget.org/packages/Bit.Borm) form at
once, through a [bit Bcrdt](https://www.nuget.org/packages/Bit.Bcrdt) document.

```bash
dotnet add package Bit.Bcrdt.Borm
```

```razor
@* TicketForm.razor, rendered inside <BcrdtCollaborationScope DocumentId="..." User="..."> *@
@using Bit.Bcrdt.Borm
@implements IDisposable

<BormView State="_state" OnValidSubmit="Save" />

@code {
    [CascadingParameter] public BcrdtCollaborativeDocument Document { get; set; } = default!;

    private readonly BormState _state = new(BormSchemaGenerator.Generate<Ticket>(), new BormModelDataAccessor(new Ticket()));
    private BcrdtBormBinding? _binding;

    protected override void OnParametersSet() =>
        _binding ??= new BcrdtBormBinding(_state, Document.Crdt.GetMap("ticket"),
            new BcrdtBormOptions { Presence = Document.Presence });

    private Task Save(BormState state) => Task.CompletedTask;   // every collaborator's answers are in the model

    public void Dispose()
    {
        _binding?.Dispose();
        _state.Dispose();
    }
}
```

Everything Borm does stays Borm's: the schema, the controls, conditions, computed values, change handlers and
validation. The binding only moves answers between the form and the document. Give the form a root map of its own
(`Document.Crdt.GetMap(...)`): two replicas creating the same nested map at once keep only one of them.

## What is stored, and how it merges

- **Every field is its own key.** A change writes the field that changed and nothing else, so two people answering
  different questions at once both keep their answers. Two people changing the same field converge on one of the two
  values.
- **Text areas merge.** `TextArea` and `Markdown` fields are a nested `BcrdtText`, so two people typing in one field
  both keep what they typed. `BcrdtBormOptions.MergeText` picks other fields; a short field - a name, an email address -
  is better left as a value, because two concurrent edits of an email address merged character by character are an
  address neither person typed.
- **Nested objects** are nested maps.
- **Repeater rows are kept by identity, not position.** Each row gets an identifier, and the repeater is stored as a
  `rows` map of each row's fields under its identifier, the row's position a fractional order key (`BcrdtOrderKey`)
  in its `$order` field. An edit made in a row while somebody inserts a row above it lands on the row it was typed
  into; moving a row writes that one field, so an edit made inside the row at the same moment is kept, and two people
  moving one row at once leave it once, in one place. A row's identity is its object - the model instance, or a dictionary form's dictionary -
  so rows must be reference types. A repeater inside a row is laid out the same way.
- **Not stored:** computed fields (every replica computes them), files, headings, and passwords and fields marked
  `Sensitive()` unless `IncludeSensitive` says so - a document is replicated, persisted and relayed, which is the wrong
  place for a secret.

Dates, times and enum members are stored as invariant text, so a `DateOnly`, a `TimeOnly` or an enum comes back as
itself, and an enum survives being reordered.

The first form to open a document seeds it; every form after that opens with what the document holds. Two forms
seeding an empty document at the same moment keep one of the two seeds - seed it in one place, such as a server-side
document host, when the starting content matters.

## The field being edited

A change somebody else makes to the field the local user is in is held back until they leave it. Written straight
into the form, it would replace the text in their input box - and, with Borm's default update-on-change, everything
they typed since entering the field, which the form has not seen yet. A held-back text field still merges: what the
user commits is read as an edit of what they were shown.

Borm reports no focus, so tell the binding where the user is - around a `BormField` in a layout of your own, or in a
custom renderer:

```razor
<div @onfocusin="() => _binding.BeginEditing(nameof(Ticket.Title))" @onfocusout="_binding.EndEditing">
    <BormField State="_state" Name="@nameof(Ticket.Title)" />
    @foreach (BcrdtCollaborator other in _binding.EditorsOf(nameof(Ticket.Title)))
    {
        <span style="color: @other.User.Color">@other.User.Name is editing</span>
    }
</div>
```

`BeginEditing` publishes the field through presence (`BcrdtBormOptions.Presence`, under `PresenceField`), and
`Editors` reads every other collaborator's, with each path following its row when rows move. `Changed` is raised when
a remote change reached the form or a collaborator moved between fields; Borm's own `StateChanged` covers the values.

For live co-typing inside one box rather than on leaving it, render that field with a `BcrdtTextArea` bound to
`_binding.TextOf(path)` in a custom renderer.

## Undo, resets and drafts

Borm's undo history cannot tell a collaborator's change from the user's, so leave `HistoryLimit` at zero on a
collaborative form and undo with a `BcrdtUndoManager` that tracks the binding - it takes back only what this user did,
and the form follows:

```csharp
var undo = new BcrdtUndoManager(map, new BcrdtUndoManagerOptions { TrackedOrigins = [binding.Origin] });
```

Borm raises no `FieldChanged` for `Reset()`, a draft put back with `BormDraft.Restore` or a model edited behind the
form's back. Call `binding.Publish()` after those: it writes every answer that differs from the document, in one
transaction.

## Threading

Like the document and the form, the binding is not thread-safe: use it on the context both are changed on - in Blazor
the renderer's, which is where a document opened through the collaboration service applies network updates.

The full guide is in the [Bcrdt README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bcrdt).
