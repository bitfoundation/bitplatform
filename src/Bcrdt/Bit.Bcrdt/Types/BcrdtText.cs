using System.Text;

namespace Bit.Bcrdt;

/// <summary>
/// Collaborative text, with optional rich-text formatting.
/// </summary>
/// <remarks>
/// <para>
/// This is the type the whole engine is shaped around. Characters are stored in run-length-encoded
/// blocks, so a paragraph typed in one sitting costs one block rather than one per keystroke, and
/// concurrent edits split blocks only where they actually overlap.
/// </para>
/// <para>
/// Formatting is stored as zero-width marks anchored between characters rather than as ranges over
/// indices. That distinction is what makes rich text survive collaboration: a range anchored to
/// "characters 10 through 20" is wrong the moment somebody else inserts at character 5, whereas a
/// mark anchored between two specific characters stays where the author put it no matter what
/// happens around it.
/// </para>
/// <para>
/// All indices and lengths are in UTF-16 code units, matching <see cref="string"/> and browser
/// selection APIs.
/// </para>
/// </remarks>
public sealed class BcrdtText : BcrdtAbstractType
{
    // A detached text records what it is asked to do and replays it on insertion. Each action takes
    // the text to apply itself to, so a detached copy can share the recording; everything an action
    // captures has already been validated and copied, so a replay cannot fail halfway through.
    private List<Action<BcrdtText>>? _pending;
    private int _pendingLength;
    private bool _pendingHasNestedTypes;

    /// <summary>Creates empty text, not yet attached to a document.</summary>
    public BcrdtText() => _pending = [];

    /// <summary>Creates text with initial content, not yet attached to a document.</summary>
    public BcrdtText(string initialText) : this()
    {
        if (!string.IsNullOrEmpty(initialText)) Insert(0, initialText);
    }

    /// <inheritdoc/>
    public override BcrdtTypeKind Kind => BcrdtTypeKind.Text;

    /// <summary>The number of characters, in UTF-16 code units, with each embed counting as one.</summary>
    /// <remarks>
    /// For detached text this is the length the text will have once it is inserted, so indices can
    /// be validated - and mistakes reported - at the call that makes them rather than at insertion.
    /// </remarks>
    public int Count => _pending is not null ? _pendingLength : Length;

    protected internal override BcrdtAbstractType CreateInstance() => new BcrdtText();

    protected internal override BcrdtEvent CreateEvent(BcrdtTransaction transaction, HashSet<string?> changedKeys) =>
        new BcrdtTextEvent(this, transaction);

    protected internal override void Integrate(BcrdtDocument doc, BcrdtItem? item)
    {
        base.Integrate(doc, item);

        if (_pending is null) return;
        List<Action<BcrdtText>> buffered = _pending;
        _pending = null;
        foreach (Action<BcrdtText> action in buffered) action(this);
    }

    /// <summary>Inserts text at a character index.</summary>
    /// <param name="index">Where to insert, between zero and <see cref="Count"/> inclusive.</param>
    /// <param name="text">The text to insert.</param>
    /// <param name="attributes">
    /// Formatting for the inserted text. When omitted, the text inherits whatever formatting is in
    /// force at the insertion point.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the text.</exception>
    /// <exception cref="NotSupportedException">An attribute value is outside the value model.</exception>
    public void Insert(int index, string text, IReadOnlyDictionary<string, object?>? attributes = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateIndex(index);
        if (text.Length == 0) return;

        InsertPrepared(index, text, attributes is null ? null : BcrdtTextOperations.PrepareAttributes(attributes));
    }

    /// <summary>Appends text.</summary>
    public void Append(string text, IReadOnlyDictionary<string, object?>? attributes = null) =>
        Insert(Count, text, attributes);

    /// <summary>Inserts a non-text value that occupies a single character position.</summary>
    /// <remarks>
    /// Used for images, mentions, formulas and anything else an editor renders as an atomic unit. A
    /// cursor can sit either side of it and a selection can include it, but its interior is opaque
    /// to the text algorithms. A string passed here is embedded as one opaque value, not inserted as
    /// characters.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the text.</exception>
    /// <exception cref="NotSupportedException">The value is outside the collaborative value model.</exception>
    /// <exception cref="InvalidOperationException">A nested type already belongs somewhere else.</exception>
    public void InsertEmbed(int index, object value, IReadOnlyDictionary<string, object?>? attributes = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        ValidateIndex(index);

        Dictionary<string, object?>? preparedAttributes =
            attributes is null ? null : BcrdtTextOperations.PrepareAttributes(attributes);
        object content = BcrdtTextOperations.PrepareContent(value, asEmbed: true, owner: this);

        InsertPrepared(index, content, preparedAttributes);
    }

    private void InsertPrepared(int index, object content, Dictionary<string, object?>? attributes)
    {
        if (_pending is not null)
        {
            ClaimPending(content);
            _pendingLength = checked(_pendingLength + BcrdtTextOperations.PreparedLength(content));
            _pending.Add(target => target.InsertPrepared(index, content, attributes));
            return;
        }

        Doc.Transact(transaction =>
        {
            // The index cache is only consulted for text that has never carried formatting, where
            // there are no marks for it to skip; see BcrdtSearchMarkerCache.Find.
            BcrdtTextCursor cursor = BcrdtTextOperations.FindPosition(transaction, this, index, useSearchMarker: true);
            var effective = new Dictionary<string, object?>(attributes ?? cursor.Attributes, StringComparer.Ordinal);
            BcrdtTextOperations.InsertText(transaction, this, cursor, BcrdtTextOperations.CreatePayload(content), effective);
        });
    }

    /// <summary>Deletes a run of characters.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The run extends outside the text.</exception>
    public void Delete(int index, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ValidateRange(index, length);
        if (length == 0) return;

        if (_pending is not null)
        {
            _pendingLength -= length;
            _pending.Add(target => target.Delete(index, length));
            return;
        }

        Doc.Transact(transaction =>
        {
            BcrdtTextCursor cursor = BcrdtTextOperations.FindPosition(transaction, this, index, useSearchMarker: true);
            BcrdtTextOperations.DeleteText(transaction, cursor, length);
        });
    }

    /// <summary>Applies formatting to a run of characters.</summary>
    /// <param name="index">Where the run starts.</param>
    /// <param name="length">How many characters to format.</param>
    /// <param name="attributes">
    /// The attributes to apply. A <see langword="null"/> value removes an attribute.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">The run extends outside the text.</exception>
    /// <exception cref="NotSupportedException">An attribute value is outside the value model.</exception>
    public void Format(int index, int length, IReadOnlyDictionary<string, object?> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ValidateRange(index, length);
        if (length == 0 || attributes.Count == 0) return;

        Dictionary<string, object?> prepared = BcrdtTextOperations.PrepareAttributes(attributes);

        if (_pending is not null)
        {
            _pending.Add(target => target.Format(index, length, prepared));
            return;
        }

        Doc.Transact(transaction =>
        {
            BcrdtTextCursor cursor = BcrdtTextOperations.FindPosition(transaction, this, index, useSearchMarker: false);
            if (cursor.Right is null) return;
            BcrdtTextOperations.FormatText(transaction, this, cursor, length, prepared);
        });
    }

    /// <summary>Replaces the entire contents, as a single transaction.</summary>
    /// <remarks>
    /// This is a blunt instrument: it discards concurrent edits to the replaced text, because that
    /// text no longer exists. Prefer targeted <see cref="Insert(int, string, IReadOnlyDictionary{string, object?})"/>
    /// and <see cref="Delete"/> calls, or <see cref="ApplyDelta"/>, whenever the change can be
    /// expressed as an edit rather than a wholesale replacement.
    /// </remarks>
    public void SetContents(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (_pending is not null)
        {
            _pendingLength = text.Length;
            _pending.Add(target => target.SetContents(text));
            return;
        }

        Doc.Transact(_ =>
        {
            if (Count > 0) Delete(0, Count);
            if (text.Length > 0) Insert(0, text);
        });
    }

    /// <summary>The plain-text contents, with formatting and embeds omitted.</summary>
    /// <remarks>
    /// Embeds occupy an index position but contribute no characters here, so when a text holds
    /// embeds this string is shorter than <see cref="Count"/>. Detached text renders as empty until
    /// it is inserted.
    /// </remarks>
    public override string ToString()
    {
        var builder = new StringBuilder(Length);
        for (BcrdtItem? item = Start; item is not null; item = item.Right)
        {
            if (item.Deleted) continue;
            if (item.Content is BcrdtContentString text) builder.Append(text.Text);
        }
        return builder.ToString();
    }

    /// <summary>
    /// The contents as a rich-text delta: a sequence of insert steps carrying their formatting.
    /// </summary>
    /// <remarks>
    /// This is the shape most rich-text editors consume directly, so rendering a collaborative
    /// document is usually a matter of handing this to the editor rather than translating anything.
    /// </remarks>
    public IReadOnlyList<BcrdtDeltaOperation> ToDelta()
    {
        List<BcrdtDeltaOperation> operations = [];
        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal);
        var buffer = new StringBuilder();

        void FlushText()
        {
            if (buffer.Length == 0) return;
            operations.Add(BcrdtDeltaOperation.InsertText(buffer.ToString(), BcrdtSnapshot(attributes)));
            buffer.Clear();
        }

        for (BcrdtItem? item = Start; item is not null; item = item.Right)
        {
            if (item.Deleted) continue;

            switch (item.Content)
            {
                case BcrdtContentString text:
                    buffer.Append(text.Text);
                    break;

                case BcrdtContentFormat format:
                    FlushText();
                    BcrdtAttributeComparer.Apply(attributes, format);
                    break;

                case BcrdtContentType or BcrdtContentEmbed or BcrdtContentBinary:
                    FlushText();
                    operations.Add(BcrdtDeltaOperation.InsertEmbed(item.Content.GetContent()[0]!, BcrdtSnapshot(attributes)));
                    break;
            }
        }

        FlushText();
        return operations;
    }

    /// <summary>
    /// Applies a rich-text delta, translating it into collaborative operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the inbound half of the editor bridge: an editor reports what the user did as a
    /// delta, and this turns that description into real CRDT edits that merge with everyone else's.
    /// Crucially it is <i>not</i> a replacement - a retain leaves the underlying blocks alone, so
    /// concurrent edits inside a retained span survive.
    /// </para>
    /// <para>
    /// The whole delta is validated before any of it is applied. An insert of a string is text unless
    /// the step is marked <see cref="BcrdtDeltaOperation.IsEmbed"/>; an insert of anything else is an embed. A
    /// retain that carries attributes and runs past the end
    /// pads the text with newlines, which is what editors that always end a document in a newline
    /// expect.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">A step has a negative length.</exception>
    /// <exception cref="NotSupportedException">An inserted or attribute value is outside the value model.</exception>
    /// <exception cref="InvalidOperationException">A nested type already belongs somewhere else.</exception>
    public void ApplyDelta(IReadOnlyList<BcrdtDeltaOperation> delta)
    {
        ArgumentNullException.ThrowIfNull(delta);

        List<PreparedStep> steps = PrepareDelta(delta);

        if (_pending is not null)
        {
            // Track the length the replay will produce, exactly as the attached path would change it.
            long position = 0;
            long length = _pendingLength;
            foreach (PreparedStep step in steps)
            {
                switch (step.Kind)
                {
                    case BcrdtDeltaKind.Insert:
                        ClaimPending(step.Content!);
                        int inserted = BcrdtTextOperations.PreparedLength(step.Content!);
                        length += inserted;
                        position += inserted;
                        break;
                    case BcrdtDeltaKind.Retain when step.Attributes is not null:
                        position += step.Length;
                        length = Math.Max(length, position);
                        break;
                    case BcrdtDeltaKind.Retain:
                        position = Math.Min(position + step.Length, length);
                        break;
                    case BcrdtDeltaKind.Delete:
                        length -= Math.Min(step.Length, length - position);
                        break;
                }
            }

            _pendingLength = checked((int)length);
            _pending.Add(target => target.ApplyPrepared(steps));
            return;
        }

        ApplyPrepared(steps);
    }

    private void ApplyPrepared(List<PreparedStep> steps)
    {
        Doc.Transact(transaction =>
        {
            var cursor = new BcrdtTextCursor(null, Start, 0, new Dictionary<string, object?>(StringComparer.Ordinal));

            foreach (PreparedStep step in steps)
            {
                switch (step.Kind)
                {
                    case BcrdtDeltaKind.Insert:
                    {
                        var attributes = new Dictionary<string, object?>(
                            step.Attributes ?? (IReadOnlyDictionary<string, object?>)EmptyAttributes, StringComparer.Ordinal);
                        BcrdtTextOperations.InsertText(transaction, this, cursor, BcrdtTextOperations.CreatePayload(step.Content!), attributes);
                        break;
                    }

                    case BcrdtDeltaKind.Retain when step.Attributes is not null:
                        BcrdtTextOperations.FormatText(transaction, this, cursor, step.Length, step.Attributes);
                        break;

                    case BcrdtDeltaKind.Retain:
                        BcrdtTextOperations.FindNextPosition(transaction, cursor, step.Length);
                        break;

                    case BcrdtDeltaKind.Delete:
                        BcrdtTextOperations.DeleteText(transaction, cursor, step.Length);
                        break;
                }
            }
        });
    }

    /// <summary>Validates and copies a delta, so applying it cannot fail partway.</summary>
    private List<PreparedStep> PrepareDelta(IReadOnlyList<BcrdtDeltaOperation> delta)
    {
        List<PreparedStep> steps = new(delta.Count);
        HashSet<BcrdtAbstractType>? nestedTypes = null;

        foreach (BcrdtDeltaOperation operation in delta)
        {
            ArgumentNullException.ThrowIfNull(operation, nameof(delta));
            Dictionary<string, object?>? attributes = operation.Attributes is { Count: > 0 } given
                ? BcrdtTextOperations.PrepareAttributes(given)
                : null;

            switch (operation.Kind)
            {
                case BcrdtDeltaKind.Insert:
                    if (operation.Content is null || operation.Content is string { Length: 0 }) break;
                    object content = BcrdtTextOperations.PrepareContent(operation.Content, asEmbed: operation.IsEmbed, owner: this);
                    if (content is BcrdtAbstractType nested &&
                        !(nestedTypes ??= new HashSet<BcrdtAbstractType>(ReferenceEqualityComparer.Instance)).Add(nested))
                    {
                        throw new InvalidOperationException(
                            "The same collaborative type instance appears more than once. Each insertion needs its own instance.");
                    }
                    steps.Add(new PreparedStep(BcrdtDeltaKind.Insert, 0, content, attributes));
                    break;

                case BcrdtDeltaKind.Retain:
                case BcrdtDeltaKind.Delete:
                    if (operation.Length < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(delta), $"A {operation.Kind} step has a negative length.");
                    }
                    if (operation.Length > 0) steps.Add(new PreparedStep(operation.Kind, operation.Length, null, attributes));
                    break;
            }
        }

        return steps;
    }

    /// <summary>Records that a nested type now waits inside this detached text.</summary>
    private void ClaimPending(object content)
    {
        if (content is not BcrdtAbstractType nested) return;
        if (ReferenceEquals(nested.PendingOwner, this))
        {
            throw new InvalidOperationException(
                "The same collaborative type instance appears more than once. Each insertion needs its own instance.");
        }
        nested.PendingOwner = this;
        _pendingHasNestedTypes = true;
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">
    /// The text is detached and holds a nested type, which cannot be in two places at once. Insert the
    /// text first, then clone it.
    /// </exception>
    public override BcrdtText Clone()
    {
        var clone = new BcrdtText();

        if (_pending is not null)
        {
            if (_pendingHasNestedTypes)
            {
                throw new NotSupportedException(
                    "Detached text that holds a nested type cannot be cloned. Insert it first, then clone it.");
            }
            clone._pending!.AddRange(_pending);
            clone._pendingLength = _pendingLength;
            return clone;
        }

        // Built from the blocks rather than from ToDelta, because a delta cannot tell an embedded
        // string apart from text.
        List<PreparedStep> steps = [];
        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal);
        int length = 0;

        for (BcrdtItem? item = Start; item is not null; item = item.Right)
        {
            if (item.Deleted) continue;

            object? content = item.Content switch
            {
                BcrdtContentString text => text.Text,
                BcrdtContentEmbed embed => BcrdtTextOperations.PrepareContent(embed.Value!, asEmbed: true, owner: clone),
                BcrdtContentType nested => nested.Type.Clone(),
                BcrdtContentBinary binary => binary.Data,
                _ => null,
            };

            if (item.Content is BcrdtContentFormat format)
            {
                BcrdtAttributeComparer.Apply(attributes, format);
                continue;
            }
            if (content is null) continue;

            if (content is BcrdtAbstractType nestedClone) nestedClone.PendingOwner = clone;
            Dictionary<string, object?>? snapshot = attributes.Count > 0
                ? new Dictionary<string, object?>(attributes, StringComparer.Ordinal)
                : null;
            steps.Add(new PreparedStep(BcrdtDeltaKind.Insert, 0, content, snapshot));
            length += BcrdtTextOperations.PreparedLength(content);
        }

        if (steps.Count > 0)
        {
            clone._pending!.Add(target => target.ApplyPrepared(steps));
            clone._pendingLength = length;
            clone._pendingHasNestedTypes = steps.Any(step => step.Content is BcrdtAbstractType);
        }
        return clone;
    }

    /// <inheritdoc/>
    public override object ToJson() => ToString();

    private void ValidateIndex(int index)
    {
        if (index < 0 || index > Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index), $"Index {index} is outside text of length {Count}.");
        }
    }

    private void ValidateRange(int index, int length)
    {
        ValidateIndex(index);
        if (length > Count - index)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length), $"A run of {length} character(s) at index {index} extends past the end of text of length {Count}.");
        }
    }

    private static readonly Dictionary<string, object?> EmptyAttributes = new(StringComparer.Ordinal);

    private sealed record PreparedStep(BcrdtDeltaKind Kind, int Length, object? Content, Dictionary<string, object?>? Attributes);

    private static IReadOnlyDictionary<string, object?>? BcrdtSnapshot(Dictionary<string, object?> attributes)
    {
        Dictionary<string, object?>? result = null;
        foreach ((string key, object? value) in attributes)
        {
            if (value is null) continue;
            result ??= new Dictionary<string, object?>(StringComparer.Ordinal);
            result[key] = value;
        }
        return result;
    }
}
