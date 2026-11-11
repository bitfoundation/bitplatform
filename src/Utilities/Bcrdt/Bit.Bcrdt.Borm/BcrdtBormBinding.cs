using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bit.Bcrdt.Borm;

/// <summary>
/// Keeps a Borm form and a collaborative map in step, so several people fill in one form together.
/// </summary>
/// <remarks>
/// <para>
/// A form is a set of independent answers, and that is how it is stored: every field is its own key
/// in <see cref="Map"/>, a nested object is a nested map, and a change writes only the field that
/// changed. Two people answering different questions at once therefore both keep their answers - the
/// failure a form saved as one whole object has, where the second save reverts the first person's
/// field. Two people changing the <i>same</i> field is decided by the map, one of the two values
/// winning, except for the text fields <see cref="BcrdtBormOptions.MergeText"/> picks, which are
/// <see cref="BcrdtText"/> and merge what both typed.
/// </para>
/// <para>
/// A repeater's rows are not stored by position. Each row gets an identifier the first time the
/// binding sees it, and the repeater is stored as an <c>order</c> list of those identifiers beside a
/// <c>rows</c> map holding each row's fields under its identifier. A field written into "row 2" by one
/// person while another inserts a row above it therefore lands on the row it was typed into, not on
/// whatever moved into second place; and moving a row moves an identifier, so an edit made inside the
/// row at the same moment is kept rather than left behind on a deleted copy. A row's identity is the
/// row object itself - the model instance, or the dictionary of a form bound to one - which Borm keeps
/// for as long as the row exists, across moves, swaps and undo.
/// </para>
/// <para>
/// Changes made by anybody else - a collaborator, an undo manager, other code writing into the map -
/// reach the form through <see cref="BormState.SetValueQuietly"/> and Borm's own repeater operations,
/// so its conditions, computed values and change handlers run exactly as they would for the local user,
/// and nothing is marked touched. Validation stays Borm's: nothing here judges a value. Computed fields
/// are never stored - every replica computes them from the answers.
/// </para>
/// <para>
/// A remote change to the field the local user is editing (<see cref="BeginEditing"/>) is held back
/// until they leave it (<see cref="EndEditing"/>). Writing it into the form while they type would
/// replace the text in their input box - and with Borm's default update-on-change, everything typed
/// since they entered the field, which the form has not even seen yet. A text field held back this way
/// still merges: what the user commits is read as an edit of what they were shown, not of the newer
/// text, so the collaborator's change is kept.
/// </para>
/// <para>
/// Changes reach the binding through <see cref="BormState.FieldChanged"/>. Borm does not raise it for
/// <see cref="BormState.Reset()"/>, a draft put back with <c>BormDraft.Restore</c> or a model edited
/// behind the form's back; call <see cref="Publish"/> after those. Borm's own undo history cannot tell
/// a collaborator's change from the user's, so leave <c>HistoryLimit</c> at zero on a collaborative form
/// and undo with a <see cref="BcrdtUndoManager"/> tracking <see cref="Origin"/>, which takes back only
/// what this user did.
/// </para>
/// <para>
/// Like the document and the form, the binding is not thread-safe: use it on the context both are
/// changed on, which in Blazor is the renderer's.
/// </para>
/// </remarks>
public sealed class BcrdtBormBinding : IDisposable
{
    private const string OrderKey = "order";
    private const string RowsKey = "rows";

    private readonly BcrdtBormOptions _options;
    private readonly IDisposable _subscription;
    private readonly ConditionalWeakTable<object, string> _rowIds = new();
    private readonly Dictionary<BcrdtText, BcrdtTextInputBinding> _texts = new(ReferenceEqualityComparer.Instance);
    private readonly List<BcrdtText> _createdTexts = [];

    private string? _editing;
    private bool _held;
    private bool _publishing;
    private int _applying;
    private bool _disposed;

    /// <summary>Binds a form to a collaborative map.</summary>
    /// <param name="state">The form.</param>
    /// <param name="map">
    /// Where the form is kept - a map of its own, best a root one such as
    /// <c>document.Crdt.GetMap("ticket")</c>: two replicas creating the same nested map at once keep only
    /// one of the two, and whatever was written into the other. It must belong to a document.
    /// </param>
    /// <param name="options">How the form is laid out and what is shared.</param>
    /// <remarks>
    /// What the map already holds is written into the form, and whatever the map does not hold yet is
    /// written into the map from the form - so the first person to open a form seeds it, and everyone
    /// after them gets what it holds. Two people opening an empty form at the same moment each seed it,
    /// and the document keeps one of the two seeds; to avoid that race for content that matters, seed
    /// the document in one place (a server-side document host) before anybody opens it.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="map"/> does not belong to a document.</exception>
    public BcrdtBormBinding(BormState state, BcrdtMap map, BcrdtBormOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        if (!map.IsAttached) throw new ArgumentException("The map must belong to a document.", nameof(map));

        State = state;
        Map = map;
        _options = options ?? new BcrdtBormOptions();

        Apply(() => ApplyFields(map, state.Definition.Fields, string.Empty, keys: null));
        Write(() => WriteFields(map, state.Definition.Fields, string.Empty, onlyMissing: true));

        state.FieldChanged += OnFieldChanged;
        _subscription = map.ObserveDeep(OnDocumentChanged);
        if (_options.Presence is { } presence) presence.Changed += OnPresenceChanged;
    }

    /// <summary>The form.</summary>
    public BormState State { get; }

    /// <summary>The map the form is kept in.</summary>
    public BcrdtMap Map { get; }

    /// <summary>
    /// The origin of every transaction the binding makes - the binding itself. Add it to a
    /// <see cref="BcrdtUndoManagerOptions.TrackedOrigins"/> to undo what the local user did in the form.
    /// </summary>
    public object Origin => this;

    /// <summary>
    /// The path of the field the local user is editing, following its row when rows move; or
    /// <see langword="null"/>.
    /// </summary>
    public string? EditingPath => _editing is null ? null : FromStable(_editing);

    /// <summary>
    /// Every other collaborator who is editing a field of this form, and which. Needs
    /// <see cref="BcrdtBormOptions.Presence"/>.
    /// </summary>
    public IReadOnlyList<BcrdtBormEditor> Editors
    {
        get
        {
            if (_options.Presence is not { } presence) return [];

            List<BcrdtBormEditor> editors = [];
            foreach (BcrdtCollaborator collaborator in presence.Others)
            {
                if (collaborator.Custom.GetValueOrDefault(_options.PresenceField) is string stable &&
                    FromStable(stable) is { } path)
                {
                    editors.Add(new BcrdtBormEditor(collaborator, path));
                }
            }
            return editors;
        }
    }

    /// <summary>
    /// Raised after a change made elsewhere reached the form, after a change held back for the field
    /// being edited was let through, and when another collaborator starts or stops editing a field.
    /// Borm raises <see cref="BormState.StateChanged"/> for the values; this is for a page that also
    /// shows who is editing what.
    /// </summary>
    public event Action? Changed;

    /// <summary>The collaborators editing the field at <paramref name="path"/>.</summary>
    public IReadOnlyList<BcrdtCollaborator> EditorsOf(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return [.. Editors.Where(editor => string.Equals(editor.Path, path, StringComparison.OrdinalIgnoreCase))
                          .Select(editor => editor.Collaborator)];
    }

    /// <summary>
    /// The collaborative text behind a merging text field, for a control that edits it directly - a
    /// <c>BcrdtTextArea</c> in a custom renderer, which shows collaborators' typing as it happens rather
    /// than when the user leaves the field. <see langword="null"/> when the field is not stored as text.
    /// </summary>
    public BcrdtText? TextOf(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return TryLocate(path, out Location location) && location.Row is null
            ? location.Container.Get(location.Field.Name) as BcrdtText
            : null;
    }

    /// <summary>
    /// Records that the local user is editing the field at <paramref name="path"/>: changes others make
    /// to it are held back until <see cref="EndEditing"/>, and collaborators see who is in it.
    /// </summary>
    /// <remarks>
    /// Borm reports no focus, so call this where focus is known - <c>@onfocusin</c> on an element
    /// around a <c>BormField</c>, or a custom renderer. Editing one field ends editing the last.
    /// </remarks>
    public void BeginEditing(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        ObjectDisposedException.ThrowIf(_disposed, this);

        string stable = ToStable(path);
        if (string.Equals(stable, _editing, StringComparison.OrdinalIgnoreCase)) return;

        EndEditing();
        _editing = stable;
        _options.Presence?.SetCustomField(_options.PresenceField, stable);
    }

    /// <summary>
    /// Records that the local user left the field they were editing, and lets through whatever was held
    /// back for it.
    /// </summary>
    /// <remarks>
    /// Call it after the field has committed its value - a browser raises <c>change</c> before
    /// <c>focusout</c>, so <c>@onfocusout</c> is the place - or the value the user typed is read as
    /// older than the change held back, and loses to it.
    /// </remarks>
    public void EndEditing()
    {
        if (_editing is not { } stable) return;

        _editing = null;
        _options.Presence?.SetCustomField(_options.PresenceField, null);

        if (_disposed || !_held) return;
        _held = false;

        if (FromStable(stable) is { } path && TryLocate(path, out Location location) && location.Row is null)
        {
            Apply(() => ApplyField(location.Container, location.Field, location.Path));
        }
        Changed?.Invoke();
    }

    /// <summary>
    /// Writes into the map every answer that differs from it - for a change Borm did not report:
    /// <see cref="BormState.Reset()"/>, a restored draft, a model edited behind the form's back.
    /// </summary>
    /// <remarks>
    /// One transaction for every value and row that differs, however many - and one more for each text
    /// field that does, written by its own binding; nothing at all when nothing differs. A change from
    /// elsewhere still held back for the field being edited is not overwritten.
    /// </remarks>
    public void Publish()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _publishing = true;
        try
        {
            Write(() => WriteFields(Map, State.Definition.Fields, string.Empty, onlyMissing: false));
        }
        finally
        {
            _publishing = false;
        }
    }

    // ------------------------------------------------------------------ form to document

    private void OnFieldChanged(string path)
    {
        if (_applying > 0 || _disposed) return;

        // A computed value, a password, a heading: nothing the document holds.
        if (State.Definition.FindByPath(path) is { } changed && KindOf(changed) == FieldKind.Ignored) return;

        if (!TryLocate(path, out Location location))
        {
            // A path the document has no place for yet - a row it has not been told about, an object
            // it does not hold. Comparing the whole form puts it where it belongs.
            Publish();
            return;
        }

        if (location.Row is { } row)
        {
            Write(() => WriteFields(row, location.Field.ItemTemplate!.Children, location.Path, onlyMissing: false));
            return;
        }

        // Text typed into a field that merges is applied by its own binding, which reads it as an edit
        // of what the form was showing rather than of the document, and makes its own transaction.
        if (location.Container.Get(location.Field.Name) is BcrdtText text)
        {
            WriteText(text, location.Path);
            return;
        }

        Write(() => WriteField(location.Container, location.Field, location.Path, onlyMissing: false));
    }

    /// <summary>Runs <paramref name="write"/> as one transaction tagged with this binding.</summary>
    private void Write(Action write)
    {
        List<(BcrdtText Text, string Path)> texts = [];
        _pendingTextWrites = texts;
        try
        {
            Map.Doc.Transact(_ => write(), Origin);
        }
        finally
        {
            _pendingTextWrites = null;
        }

        // Texts created in that transaction are part of the document now, as the instances that were
        // written; each gets the binding that will carry the form's later edits into it.
        foreach (BcrdtText created in _createdTexts) EnsureTextBinding(created);
        _createdTexts.Clear();

        // Existing texts are written by their bindings, each in a transaction of its own: an edit is
        // placed against the positions the binding tracked as of the last settled change.
        foreach ((BcrdtText text, string path) in texts) WriteText(text, path);
    }

    private List<(BcrdtText Text, string Path)>? _pendingTextWrites;

    private void WriteFields(BcrdtMap container, IEnumerable<BormFieldDefinition> fields, string prefix, bool onlyMissing)
    {
        foreach (BormFieldDefinition field in fields)
        {
            if (KindOf(field) == FieldKind.Ignored) continue;
            WriteField(container, field, BormPath.Combine(prefix, field.Name), onlyMissing);
        }
    }

    private void WriteField(BcrdtMap container, BormFieldDefinition field, string path, bool onlyMissing)
    {
        string key = field.Name;
        bool present = container.TryGetValue(key, out object? stored);

        switch (KindOf(field))
        {
            case FieldKind.Object:
                if (stored is BcrdtMap child)
                {
                    WriteFields(child, field.Children, path, onlyMissing);
                }
                else
                {
                    var created = new BcrdtMap();
                    WriteFields(created, field.Children, path, onlyMissing: false);
                    container.Set(key, created);
                }
                return;

            case FieldKind.Rows:
                WriteRows(container, field, path, onlyMissing);
                return;
        }

        if (onlyMissing && present) return;

        // A change made elsewhere and held back for the field being edited is not reverted by comparing
        // the whole form, which still shows the older value. What the user commits there is newer than
        // that change, and is written.
        if (_publishing && _held && IsEditing(path)) return;

        object? value = State.GetValue(path);
        if (stored is BcrdtText text)
        {
            _pendingTextWrites?.Add((text, path));
            return;
        }

        if (!present && KindOf(field) == FieldKind.Text)
        {
            var created = new BcrdtText(Normalize(value as string));
            container.Set(key, created);
            _createdTexts.Add(created);
            return;
        }

        object? projected = BcrdtBormValues.ToDocument(value);
        if (!present || !BcrdtAnyCodec.DeepEquals(stored, projected)) container.Set(key, projected);
    }

    private void WriteText(BcrdtText text, string path)
    {
        BcrdtTextInputBinding binding = EnsureTextBinding(text);
        string value = Normalize(State.GetValue(path) as string);
        binding.ApplyInput(value, value.Length, value.Length);

        // Changes held back from the form, when the user is no longer in the field, are shown now.
        if (binding.HasPendingChanges && !IsEditing(path)) Apply(() => PushText(binding, path));
    }

    /// <summary>
    /// Makes the repeater's <c>order</c> list and <c>rows</c> map hold exactly the form's rows, in the
    /// form's order, with the fewest changes to <c>order</c>.
    /// </summary>
    /// <remarks>
    /// The identifiers that keep their relative order - the longest run of them in the form's order -
    /// stay where they are, and every other one is taken out and put back where it now belongs. A moved
    /// row is therefore one identifier deleted and inserted, not every row between its old and new
    /// places. Duplicates two concurrent moves of one row left behind, and identifiers whose row a
    /// concurrent removal took, go at the same time.
    /// </remarks>
    private void WriteRows(BcrdtMap container, BormFieldDefinition field, string path, bool onlyMissing)
    {
        BormFieldDefinition template = field.ItemTemplate!;
        IList? list = State.GetValue(path) as IList;
        int count = list?.Count ?? 0;

        if (container.Get(field.Name) is not BcrdtMap holder)
        {
            holder = new BcrdtMap();
            var newOrder = new BcrdtArray();
            var newRows = new BcrdtMap();
            for (int i = 0; i < count; i++)
            {
                if (list![i] is not { } item) continue;
                string id = IdFor(item);
                newRows.Set(id, CreateRow(template, BormPath.Combine(path, i)));
                newOrder.Push(id);
            }
            holder.Set(OrderKey, newOrder);
            holder.Set(RowsKey, newRows);
            container.Set(field.Name, holder);
            return;
        }

        if (onlyMissing) return;

        if (holder.Get(OrderKey) is not BcrdtArray order)
        {
            order = new BcrdtArray();
            holder.Set(OrderKey, order);
        }
        if (holder.Get(RowsKey) is not BcrdtMap rows)
        {
            rows = new BcrdtMap();
            holder.Set(RowsKey, rows);
        }

        List<string> target = new(count);
        var targetIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < count; i++)
        {
            if (list![i] is not { } item) continue;
            string id = IdFor(item);
            if (!targetIndex.TryAdd(id, target.Count)) continue;
            target.Add(id);
            if (rows.Get(id) is not BcrdtMap) rows.Set(id, CreateRow(template, BormPath.Combine(path, i)));
        }

        // The first occurrence of each identifier the form still has, by position in `order`; the rest go.
        List<object?> current = order.ToArray();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        List<(int Position, int Target)> candidates = [];
        for (int position = 0; position < current.Count; position++)
        {
            if (current[position] is string id && targetIndex.TryGetValue(id, out int at) && seen.Add(id))
            {
                candidates.Add((position, at));
            }
        }

        HashSet<int> kept = LongestIncreasingRun(candidates);
        for (int position = current.Count - 1; position >= 0;)
        {
            if (kept.Contains(position))
            {
                position--;
                continue;
            }

            int end = position;
            while (position >= 0 && !kept.Contains(position)) position--;
            order.Delete(position + 1, end - position);
        }

        List<string> remaining = [.. candidates.Where(c => kept.Contains(c.Position)).Select(c => (string)current[c.Position]!)];
        int next = 0;
        int insertAt = 0;
        for (int i = 0; i < target.Count;)
        {
            if (next < remaining.Count && string.Equals(remaining[next], target[i], StringComparison.Ordinal))
            {
                next++;
                insertAt++;
                i++;
                continue;
            }

            // A run of identifiers that belong here goes in as one insertion.
            int start = i;
            while (i < target.Count && !(next < remaining.Count && string.Equals(remaining[next], target[i], StringComparison.Ordinal))) i++;
            order.Insert(insertAt, [.. target.Skip(start).Take(i - start).Cast<object?>()]);
            insertAt += i - start;
        }

        foreach (string id in rows.Keys.ToList())
        {
            if (!targetIndex.ContainsKey(id)) rows.Delete(id);
        }
    }

    /// <summary>The positions of the longest run of <paramref name="candidates"/> whose targets increase.</summary>
    private static HashSet<int> LongestIncreasingRun(List<(int Position, int Target)> candidates)
    {
        // Patience sorting: tails[k] is the index of the smallest target ending a run of length k + 1.
        var tails = new List<int>();
        var previous = new int[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            int low = 0, high = tails.Count;
            while (low < high)
            {
                int middle = (low + high) / 2;
                if (candidates[tails[middle]].Target < candidates[i].Target) low = middle + 1;
                else high = middle;
            }

            previous[i] = low > 0 ? tails[low - 1] : -1;
            if (low == tails.Count) tails.Add(i);
            else tails[low] = i;
        }

        var kept = new HashSet<int>();
        for (int i = tails.Count > 0 ? tails[^1] : -1; i >= 0; i = previous[i]) kept.Add(candidates[i].Position);
        return kept;
    }

    private BcrdtMap CreateRow(BormFieldDefinition template, string rowPath)
    {
        var row = new BcrdtMap();
        WriteFields(row, template.Children, rowPath, onlyMissing: false);
        return row;
    }

    private string IdFor(object row)
    {
        if (_rowIds.TryGetValue(row, out string? id)) return id;
        id = Guid.NewGuid().ToString("N");
        _rowIds.Add(row, id);
        return id;
    }

    private string? IdOf(object? row) => row is not null && _rowIds.TryGetValue(row, out string? id) ? id : null;

    // ------------------------------------------------------------------ document to form

    private void OnDocumentChanged(IReadOnlyList<BcrdtEvent> events, BcrdtTransaction transaction)
    {
        if (_disposed || ReferenceEquals(transaction.Origin, Origin)) return;

        Apply(() =>
        {
            foreach (BcrdtEvent change in events)
            {
                // Typing in a text field reaches the form through that text's binding.
                if (change.Target is BcrdtText) continue;

                switch (Resolve(change.Path))
                {
                    case { Rows: { } rowsField } target:
                        if (target.Container.Get(rowsField.Name) is BcrdtMap holder) ApplyRows(holder, rowsField, target.Path);
                        break;
                    case { Fields: { } fields } target:
                        ApplyFields(target.Container, fields, target.Path, change.Keys);
                        break;
                }
            }
        });

        PruneTextBindings();
        Changed?.Invoke();
    }

    /// <summary>Writes into the form with its notifications held back to one, and without echoing them.</summary>
    private void Apply(Action apply)
    {
        _applying++;
        try
        {
            State.Batch(apply);
        }
        finally
        {
            _applying--;
        }
    }

    private void ApplyFields(BcrdtMap container, IEnumerable<BormFieldDefinition> fields, string prefix, IReadOnlyDictionary<string, BcrdtKeyChange>? keys)
    {
        foreach (BormFieldDefinition field in fields)
        {
            if (KindOf(field) == FieldKind.Ignored) continue;
            if (keys is not null && !keys.ContainsKey(field.Name)) continue;
            ApplyField(container, field, BormPath.Combine(prefix, field.Name));
        }
    }

    private void ApplyField(BcrdtMap container, BormFieldDefinition field, string path)
    {
        if (!container.TryGetValue(field.Name, out object? stored)) return;

        switch (KindOf(field))
        {
            case FieldKind.Object:
                if (stored is BcrdtMap child) ApplyFields(child, field.Children, path, keys: null);
                return;
            case FieldKind.Rows:
                if (stored is BcrdtMap holder) ApplyRows(holder, field, path);
                return;
        }

        if (stored is BcrdtText text)
        {
            bool known = _texts.ContainsKey(text);
            BcrdtTextInputBinding binding = EnsureTextBinding(text);

            // A text the form has not been showing - the document's, as the form opened, or one that
            // replaced it - is shown at once, so the binding knows what the form holds from here on.
            if (!known || !IsEditing(path)) PushText(binding, path);
            else if (binding.HasPendingChanges) _held = true;
            return;
        }

        if (BcrdtBormValues.Same(State.GetValue(path), stored)) return;

        if (IsEditing(path))
        {
            _held = true;
            return;
        }

        State.SetValueQuietly(path, BcrdtBormValues.FromDocument(stored, field));
    }

    /// <summary>
    /// Makes the form's rows match the document's: the same rows, by identifier, in the same order.
    /// Rows are inserted, moved and removed through Borm, so each keeps its messages, touched state and
    /// identity, and a new row is filled from the document.
    /// </summary>
    private void ApplyRows(BcrdtMap holder, BormFieldDefinition field, string path)
    {
        if (holder.Get(OrderKey) is not BcrdtArray order || holder.Get(RowsKey) is not BcrdtMap rows) return;

        List<string> target = [];
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (object? entry in order.ToArray())
        {
            if (entry is string id && seen.Add(id) && rows.Get(id) is BcrdtMap) target.Add(id);
        }

        BormFieldDefinition template = field.ItemTemplate!;
        for (int i = 0; i < target.Count; i++)
        {
            IList? list = State.GetValue(path) as IList;
            int count = list?.Count ?? 0;
            string id = target[i];
            if (i < count && string.Equals(IdOf(list![i]), id, StringComparison.Ordinal)) continue;

            int from = -1;
            for (int j = i + 1; j < count; j++)
            {
                if (string.Equals(IdOf(list![j]), id, StringComparison.Ordinal))
                {
                    from = j;
                    break;
                }
            }

            if (from >= 0)
            {
                State.MoveArrayItem(path, from, i);
                continue;
            }

            int at = State.InsertArrayItem(field, path, i);
            if (at < 0) return;   // a collection the model will not grow: nothing more can be shown

            if ((State.GetValue(path) as IList)?[at] is { } created) _rowIds.AddOrUpdate(created, id);
            ApplyFields((BcrdtMap)rows.Get(id)!, template.Children, BormPath.Combine(path, at), keys: null);
        }

        for (int count = State.ArrayCount(path); count > target.Count; count--)
        {
            State.RemoveArrayItem(path, count - 1);
        }
    }

    private BcrdtTextInputBinding EnsureTextBinding(BcrdtText text)
    {
        if (_texts.TryGetValue(text, out BcrdtTextInputBinding? binding)) return binding;

        binding = new BcrdtTextInputBinding(text, origin: Origin);
        binding.Changed += () => OnTextChanged(text);
        _texts[text] = binding;
        return binding;
    }

    private void OnTextChanged(BcrdtText text)
    {
        if (_disposed || !_texts.TryGetValue(text, out BcrdtTextInputBinding? binding)) return;
        if (PathOf(text) is not { } path) return;

        if (IsEditing(path))
        {
            _held = true;
            return;
        }

        Apply(() => PushText(binding, path));
        Changed?.Invoke();
    }

    /// <summary>Shows the form what its text binding has that the form does not.</summary>
    private void PushText(BcrdtTextInputBinding binding, string path)
    {
        if (binding.HasOutstandingUpdate) return;

        BcrdtTextControlUpdate update = binding.CreateUpdate();
        string? shown = State.GetValue(path) as string;
        if (!string.Equals(update.Value, Normalize(shown), StringComparison.Ordinal)) State.SetValueQuietly(path, update.Value);
        binding.Acknowledge(update);
    }

    private void PruneTextBindings()
    {
        foreach (BcrdtText text in _texts.Keys.Where(text => text.IsDeleted || PathOf(text) is null).ToList())
        {
            _texts.Remove(text, out BcrdtTextInputBinding? binding);
            binding?.Dispose();
        }
    }

    private static string Normalize(string? text) => (text ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal);

    // ------------------------------------------------------------------ paths

    private enum FieldKind { Ignored, Value, Text, Object, Rows }

    private FieldKind KindOf(BormFieldDefinition field)
    {
        if (field.Name.Length == 0 || field.Type is BormFieldType.Static or BormFieldType.File) return FieldKind.Ignored;
        if (field.Computed is not null) return FieldKind.Ignored;
        if (field.IsSensitive && !_options.IncludeSensitive) return FieldKind.Ignored;
        if (field.Type == BormFieldType.Object) return FieldKind.Object;
        if (field.Type == BormFieldType.Array && field.ItemTemplate is { Type: BormFieldType.Object }) return FieldKind.Rows;

        bool holdsText = field.ValueType is null || field.ValueType == typeof(string);
        bool merges = _options.MergeText?.Invoke(field) ?? field.Type is BormFieldType.TextArea or BormFieldType.Markdown;
        return holdsText && merges && field.Type != BormFieldType.Array ? FieldKind.Text : FieldKind.Value;
    }

    /// <summary>Where a form path lives in the document.</summary>
    /// <param name="Container">The map holding the field - or, for a whole row, the row's own map.</param>
    /// <param name="Field">The field; for a whole row, the repeater.</param>
    /// <param name="Path">The field's path in the form.</param>
    /// <param name="Row">The row's map when the path names a whole row.</param>
    private readonly record struct Location(BcrdtMap Container, BormFieldDefinition Field, string Path, BcrdtMap? Row);

    /// <summary>
    /// Follows a form path through the schema into the document: names into maps, a repeater's index
    /// to its row's identifier and from there to the row's map. A field whose name contains a dot - the
    /// typed builder's <c>x => x.Address.City</c> declares one - is matched whole before the path is
    /// split, as Borm itself matches it.
    /// </summary>
    private bool TryLocate(string path, out Location location)
    {
        location = default;
        IEnumerable<BormFieldDefinition> fields = State.Definition.Fields;
        BcrdtMap container = Map;
        string prefix = string.Empty;
        string rest = path;

        while (true)
        {
            BormFieldDefinition? field = Match(fields, rest);
            if (field is null || KindOf(field) == FieldKind.Ignored) return false;

            string fieldPath = BormPath.Combine(prefix, field.Name);
            string after = rest[field.Name.Length..];
            if (after.Length == 0)
            {
                location = new Location(container, field, fieldPath, null);
                return true;
            }

            switch (KindOf(field))
            {
                case FieldKind.Object when after[0] == '.':
                    if (container.Get(field.Name) is not BcrdtMap child) return false;
                    container = child;
                    fields = field.Children;
                    prefix = fieldPath;
                    rest = after[1..];
                    continue;

                case FieldKind.Rows when after[0] == '[':
                    int close = after.IndexOf(']', StringComparison.Ordinal);
                    if (close < 0 || !int.TryParse(after.AsSpan(1, close - 1), NumberStyles.None, CultureInfo.InvariantCulture, out int index)) return false;
                    if (State.GetValue(fieldPath) is not IList list || index >= list.Count) return false;
                    if (IdOf(list[index]) is not { } id) return false;
                    if ((container.Get(field.Name) as BcrdtMap)?.Get(RowsKey) is not BcrdtMap rows || rows.Get(id) is not BcrdtMap row) return false;

                    string rowPath = BormPath.Combine(fieldPath, index);
                    string remainder = after[(close + 1)..];
                    if (remainder.Length == 0)
                    {
                        location = new Location(container, field, rowPath, row);
                        return true;
                    }
                    if (remainder[0] != '.') return false;

                    container = row;
                    fields = field.ItemTemplate!.Children;
                    prefix = rowPath;
                    rest = remainder[1..];
                    continue;

                default:
                    // Deeper than a value goes - an element of a tag list, say: the field is written whole.
                    location = new Location(container, field, fieldPath, null);
                    return true;
            }
        }

        static BormFieldDefinition? Match(IEnumerable<BormFieldDefinition> fields, string rest)
        {
            BormFieldDefinition? best = null;
            foreach (BormFieldDefinition field in fields)
            {
                string name = field.Name;
                if (name.Length == 0 || name.Length > rest.Length || (best is not null && name.Length <= best.Name.Length)) continue;
                if (!rest.StartsWith(name, StringComparison.OrdinalIgnoreCase)) continue;
                if (name.Length < rest.Length && rest[name.Length] is not ('.' or '[')) continue;
                best = field;
            }
            return best;
        }
    }

    /// <summary>What a change in the document touches in the form.</summary>
    /// <param name="Container">The map the change is in.</param>
    /// <param name="Path">The form path of that map, or of the repeater.</param>
    /// <param name="Fields">The fields of a changed map.</param>
    /// <param name="Rows">The repeater, when the change is to its rows.</param>
    private readonly record struct Target(BcrdtMap Container, string Path, IEnumerable<BormFieldDefinition>? Fields, BormFieldDefinition? Rows);

    /// <summary>
    /// Follows a route from <see cref="Map"/> - as <see cref="BcrdtEvent.Path"/> gives it - back to the
    /// form: keys to fields, a row's identifier to its current index in the form.
    /// </summary>
    private Target? Resolve(IReadOnlyList<object> route)
    {
        IEnumerable<BormFieldDefinition> fields = State.Definition.Fields;
        BcrdtMap container = Map;
        string prefix = string.Empty;

        for (int i = 0; i < route.Count; i++)
        {
            if (route[i] is not string key) return null;
            BormFieldDefinition? field = fields.FirstOrDefault(f => string.Equals(f.Name, key, StringComparison.Ordinal));
            if (field is null) return null;
            string fieldPath = BormPath.Combine(prefix, field.Name);

            switch (KindOf(field))
            {
                case FieldKind.Object:
                    if (container.Get(key) is not BcrdtMap child) return null;
                    container = child;
                    fields = field.Children;
                    prefix = fieldPath;
                    continue;

                case FieldKind.Rows:
                    // The holder, its order or its row map changed: the rows themselves.
                    if (i + 2 >= route.Count || !Equals(route[i + 1], RowsKey)) return new Target(container, fieldPath, null, field);

                    if (route[i + 2] is not string id || IndexOfRow(fieldPath, id) is not int index ||
                        (container.Get(key) as BcrdtMap)?.Get(RowsKey) is not BcrdtMap rows || rows.Get(id) is not BcrdtMap row)
                    {
                        return new Target(container, fieldPath, null, field);
                    }

                    container = row;
                    fields = field.ItemTemplate!.Children;
                    prefix = BormPath.Combine(fieldPath, index);
                    i += 2;
                    continue;

                default:
                    return null;
            }
        }

        return new Target(container, prefix, fields, null);
    }

    /// <summary>The form path of a text in the document, or <see langword="null"/> if the form has no place for it.</summary>
    private string? PathOf(BcrdtText text)
    {
        // A text detached from the map - deleted with its row - is no longer the form's.
        BcrdtAbstractType? ancestor = text;
        while (ancestor is not null && !ReferenceEquals(ancestor, Map)) ancestor = ancestor.ContainingItem?.Parent as BcrdtAbstractType;
        if (ancestor is null) return null;

        IReadOnlyList<object> route = BcrdtAbstractType.ComputePath(Map, text);
        if (route.Count == 0 || route[^1] is not string key) return null;
        if (Resolve(route.Take(route.Count - 1).ToList()) is not { Fields: { } fields } target) return null;

        return fields.Any(field => string.Equals(field.Name, key, StringComparison.Ordinal))
            ? BormPath.Combine(target.Path, key)
            : null;
    }

    private int? IndexOfRow(string arrayPath, string id)
    {
        if (State.GetValue(arrayPath) is not IList list) return null;
        for (int i = 0; i < list.Count; i++)
        {
            if (string.Equals(IdOf(list[i]), id, StringComparison.Ordinal)) return i;
        }
        return null;
    }

    private bool IsEditing(string path) =>
        _editing is not null && string.Equals(ToStable(path), _editing, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// A path that names rows by identifier rather than by position - <c>Lines[#3f2a…].Notes</c> - so it
    /// names the same field on every replica, and after rows have moved.
    /// </summary>
    private string ToStable(string path)
    {
        var result = new StringBuilder(path.Length + 16);
        int start = 0;
        while (start < path.Length)
        {
            int open = path.IndexOf('[', start);
            if (open < 0) break;
            int close = path.IndexOf(']', open);
            if (close < 0) break;

            result.Append(path, start, open - start);
            string id = string.Empty;
            if (int.TryParse(path.AsSpan(open + 1, close - open - 1), NumberStyles.None, CultureInfo.InvariantCulture, out int index) &&
                State.GetValue(FromStable(result.ToString()) ?? string.Empty) is IList list && index < list.Count && IdOf(list[index]) is { } rowId)
            {
                id = "#" + rowId;
            }
            result.Append('[').Append(id.Length > 0 ? id : path.AsSpan(open + 1, close - open - 1)).Append(']');
            start = close + 1;
        }
        result.Append(path, start, path.Length - start);
        return result.ToString();
    }

    /// <summary>The form path a stable path names now, or <see langword="null"/> if its row is gone.</summary>
    private string? FromStable(string stable)
    {
        var result = new StringBuilder(stable.Length);
        int start = 0;
        while (start < stable.Length)
        {
            int open = stable.IndexOf("[#", start, StringComparison.Ordinal);
            if (open < 0) break;
            int close = stable.IndexOf(']', open);
            if (close < 0) break;

            result.Append(stable, start, open - start);
            if (IndexOfRow(result.ToString(), stable[(open + 2)..close]) is not int index) return null;
            result.Append('[').Append(index.ToString(CultureInfo.InvariantCulture)).Append(']');
            start = close + 1;
        }
        result.Append(stable, start, stable.Length - start);
        return result.ToString();
    }

    private void OnPresenceChanged() => Changed?.Invoke();

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;

        EndEditing();
        _disposed = true;

        State.FieldChanged -= OnFieldChanged;
        _subscription.Dispose();
        if (_options.Presence is { } presence) presence.Changed -= OnPresenceChanged;
        foreach (BcrdtTextInputBinding binding in _texts.Values) binding.Dispose();
        _texts.Clear();
        Changed = null;
    }
}
