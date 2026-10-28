using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.Bcrdt;

/// <summary>Who a participant is, for display purposes.</summary>
/// <param name="Id">The application's identifier for the person. Distinct from a client session id.</param>
/// <param name="Name">The name to show.</param>
/// <param name="Color">
/// A CSS colour used for their cursor and selection highlight. For participants read through
/// <see cref="BcrdtPresenceService"/> this is always a hex colour (<c>#rgb</c>, <c>#rgba</c>,
/// <c>#rrggbb</c> or <c>#rrggbbaa</c>): anything else a peer publishes is replaced with a suggested
/// colour, so the value is safe to place in a style attribute.
/// </param>
/// <param name="AvatarUrl">An optional avatar image.</param>
public sealed record BcrdtCollaboratorInfo(string Id, string Name, string Color, string? AvatarUrl = null);

/// <summary>
/// Where a participant's cursor and selection are, anchored to content rather than to indices.
/// </summary>
/// <param name="Field">The root type the selection lies in.</param>
/// <param name="Anchor">Where the selection was started.</param>
/// <param name="Head">Where the selection currently ends. Equal to the anchor for a plain caret.</param>
public sealed record BcrdtCollaboratorSelection(string Field, BcrdtRelativePosition Anchor, BcrdtRelativePosition Head)
{
    /// <summary>Whether this is a caret rather than a range.</summary>
    public bool IsCaret => ReferenceEquals(Anchor, Head) || Anchor.Encode().AsSpan().SequenceEqual(Head.Encode());

    /// <summary>Resolves the selection to indices in the document as it stands now.</summary>
    /// <returns>
    /// The resolved range, or <see langword="null"/> if the anchored content has not arrived yet.
    /// </returns>
    public (int Start, int End)? Resolve(BcrdtDocument doc)
    {
        BcrdtAbsolutePosition? anchor = Anchor.Resolve(doc);
        BcrdtAbsolutePosition? head = Head.Resolve(doc);
        if (anchor is null || head is null) return null;

        int start = Math.Min(anchor.Value.Index, head.Value.Index);
        int end = Math.Max(anchor.Value.Index, head.Value.Index);
        return (start, end);
    }
}

/// <summary>Everything currently known about one participant.</summary>
/// <param name="ClientId">Their session identity.</param>
/// <param name="User">Who they are.</param>
/// <param name="Selection">Where their cursor is, if they have published one.</param>
/// <param name="Activity">What they are doing, for example <c>typing</c>.</param>
/// <param name="IsLocal">Whether this is the local participant.</param>
/// <param name="LastSeen">When we last heard from them.</param>
/// <param name="Custom">Any application-specific fields they published.</param>
public sealed record BcrdtCollaborator(
    ulong ClientId,
    BcrdtCollaboratorInfo User,
    BcrdtCollaboratorSelection? Selection,
    string? Activity,
    bool IsLocal,
    DateTimeOffset LastSeen,
    IReadOnlyDictionary<string, object?> Custom);

/// <summary>
/// A typed view over awareness for the things collaborative applications always need.
/// </summary>
/// <remarks>
/// Awareness itself is an untyped bag of published state, which is the right primitive but the wrong
/// API to write an application against. This puts names on the fields every collaborative app ends
/// up inventing - who you are, where your cursor is, whether you are typing - so those things
/// interoperate between components and libraries instead of each picking its own key names.
/// Anything beyond them goes through <see cref="SetCustomField"/> and stays available.
/// </remarks>
public sealed partial class BcrdtPresenceService : IDisposable
{
    private const string UserKey = "user";
    private const string SelectionKey = "selection";
    private const string ActivityKey = "activity";

    private static readonly string[] Palette =
    [
        "#e5484d", "#0090ff", "#30a46c", "#f76b15", "#8e4ec6",
        "#e93d82", "#12a594", "#ffb224", "#3e63dd", "#bd4b00",
    ];

    private readonly BcrdtAwarenessStore _awareness;
    private readonly BcrdtDocument _doc;
    private IReadOnlyDictionary<string, object?>? _suspendedState;
    private bool _disposed;

    /// <summary>Creates a presence facade over an awareness store.</summary>
    public BcrdtPresenceService(BcrdtAwarenessStore awareness, BcrdtDocument doc)
    {
        ArgumentNullException.ThrowIfNull(awareness);
        ArgumentNullException.ThrowIfNull(doc);

        _awareness = awareness;
        _doc = doc;
        _awareness.Changed += OnAwarenessChanged;
    }

    /// <summary>Raised when any participant appears, changes or leaves.</summary>
    public event Action? Changed;

    /// <summary>The local participant's published identity, if one has been set.</summary>
    public BcrdtCollaboratorInfo? LocalUser { get; private set; }

    /// <summary>Whether the local participant's presence is currently withdrawn by <see cref="Suspend"/>.</summary>
    public bool IsSuspended => _suspendedState is not null;

    /// <summary>Every participant, including the local one.</summary>
    public IReadOnlyList<BcrdtCollaborator> Participants => Read(includeLocal: true);

    /// <summary>Every participant except the local one - the people to draw cursors for.</summary>
    public IReadOnlyList<BcrdtCollaborator> Others => Read(includeLocal: false);

    /// <summary>Publishes who the local participant is.</summary>
    public void SetUser(BcrdtCollaboratorInfo user)
    {
        ArgumentNullException.ThrowIfNull(user);
        LocalUser = user;

        _awareness.SetLocalField(UserKey, new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = user.Id,
            ["name"] = user.Name,
            ["color"] = user.Color,
            ["avatar"] = user.AvatarUrl,
        });
    }

    /// <summary>Publishes a caret position in a named field.</summary>
    public void SetCaret(string field, BcrdtAbstractType type, int index)
    {
        BcrdtRelativePosition position = BcrdtRelativePosition.FromIndex(type, index);
        Publish(field, position, position);
    }

    /// <summary>Publishes a selection range in a named field.</summary>
    /// <param name="field">The root type the selection lies in.</param>
    /// <param name="type">The sequence being selected in.</param>
    /// <param name="anchorIndex">Where the selection started.</param>
    /// <param name="headIndex">Where the selection currently ends.</param>
    /// <remarks>
    /// Both ends bind to the selected characters themselves - the start to the first selected
    /// character, the end to the last - whichever direction the selection was made in. The highlight
    /// therefore keeps covering exactly the text that was selected: text typed by anyone at either
    /// edge lands outside it rather than silently growing it. An empty range is published as a caret.
    /// </remarks>
    public void SetSelection(string field, BcrdtAbstractType type, int anchorIndex, int headIndex)
    {
        if (anchorIndex == headIndex)
        {
            SetCaret(field, type, anchorIndex);
            return;
        }

        int start = Math.Min(anchorIndex, headIndex);
        int end = Math.Max(anchorIndex, headIndex);
        BcrdtRelativePosition startPosition = BcrdtRelativePosition.FromIndex(type, start);
        BcrdtRelativePosition endPosition = BcrdtRelativePosition.FromIndex(type, end, association: -1);

        bool forward = anchorIndex <= headIndex;
        Publish(field, forward ? startPosition : endPosition, forward ? endPosition : startPosition);
    }

    /// <summary>Clears the local participant's published cursor.</summary>
    public void ClearSelection() => _awareness.SetLocalField(SelectionKey, null);

    /// <summary>Publishes what the local participant is doing, for example <c>typing</c>.</summary>
    public void SetActivity(string? activity) => _awareness.SetLocalField(ActivityKey, activity);

    /// <summary>Publishes an application-specific field.</summary>
    /// <remarks>
    /// Values outside the collaborative value model are projected onto it, the same way a document
    /// stores application types.
    /// </remarks>
    public void SetCustomField(string key, object? value)
    {
        if (key is UserKey or SelectionKey or ActivityKey)
        {
            throw new ArgumentException($"'{key}' is reserved; use the dedicated method instead.", nameof(key));
        }
        _awareness.SetLocalField(key, value);
    }

    /// <summary>
    /// Withdraws the local participant from everybody's participant list, remembering what was
    /// published so <see cref="Resume"/> can restore it.
    /// </summary>
    /// <remarks>
    /// For when the person has gone but the session has not. In Blazor Server a closed or crashed
    /// browser tab leaves its circuit - and so its document and heartbeat - alive for the circuit
    /// retention period, which would keep the person listed as present for minutes. Call this from a
    /// <c>CircuitHandler</c>'s <c>OnConnectionDownAsync</c>, and <see cref="Resume"/> from
    /// <c>OnConnectionUpAsync</c> - which is what <c>AddBitBcrdtCircuitPresence</c> from
    /// <c>Bit.Bcrdt.Server</c> registers.
    /// </remarks>
    public void Suspend()
    {
        if (_suspendedState is not null) return;

        IReadOnlyDictionary<string, object?>? state = _awareness.LocalState;
        if (state is null) return;

        _suspendedState = state;
        _awareness.SetLocalState(null);
    }

    /// <summary>Restores the presence withdrawn by <see cref="Suspend"/>.</summary>
    public void Resume()
    {
        IReadOnlyDictionary<string, object?>? state = _suspendedState;
        if (state is null) return;

        _suspendedState = null;
        if (_awareness.LocalState is null) _awareness.SetLocalState(state);
    }

    /// <summary>
    /// Picks a stable colour for a user identifier.
    /// </summary>
    /// <remarks>
    /// Derived from the identifier rather than assigned in join order, so a person keeps the same
    /// colour across sessions and every participant independently agrees on it without coordination.
    /// </remarks>
    public static string SuggestColor(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        uint hash = 2166136261;
        foreach (char c in userId)
        {
            hash = (hash ^ c) * 16777619;
        }
        return Palette[hash % (uint)Palette.Length];
    }

    /// <summary>Whether a colour string is a plain hex colour, safe to embed in CSS.</summary>
    public static bool IsSafeColor(string? color) => color is not null && SafeColorPattern().IsMatch(color);

    [GeneratedRegex("^#(?:[0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeColorPattern();

    private void Publish(string field, BcrdtRelativePosition anchor, BcrdtRelativePosition head) =>
        _awareness.SetLocalField(SelectionKey, new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["field"] = field,
            ["anchor"] = anchor.Encode(),
            ["head"] = head.Encode(),
        });

    private IReadOnlyList<BcrdtCollaborator> Read(bool includeLocal)
    {
        List<BcrdtCollaborator> result = [];

        // A snapshot, so the list is consistent even while updates arrive on another thread.
        foreach ((ulong clientId, BcrdtAwarenessEntry entry) in _awareness.Entries)
        {
            if (entry.State is null) continue;
            bool isLocal = clientId == _awareness.LocalClientId;
            if (isLocal && !includeLocal) continue;

            result.Add(new BcrdtCollaborator(
                clientId,
                ReadUser(entry, clientId),
                ReadSelection(entry),
                entry.State.GetValueOrDefault(ActivityKey) as string,
                isLocal,
                entry.LastSeen,
                ReadCustom(entry.State)));
        }

        result.Sort(static (left, right) => left.ClientId.CompareTo(right.ClientId));
        return result;
    }

    private static BcrdtCollaboratorInfo ReadUser(BcrdtAwarenessEntry entry, ulong clientId)
    {
        if (entry.State?.GetValueOrDefault(UserKey) is not IReadOnlyDictionary<string, object?> user)
        {
            // A participant who has not published an identity still needs to be drawable.
            string fallback = clientId.ToString(CultureInfo.InvariantCulture);
            return new BcrdtCollaboratorInfo(fallback, "Anonymous", SuggestColor(fallback));
        }

        string id = user.GetValueOrDefault("id") as string ?? clientId.ToString(CultureInfo.InvariantCulture);

        // The colour is peer-controlled and ends up in style attributes. Anything but a plain hex
        // colour could inject arbitrary CSS into every other participant's page.
        string? color = user.GetValueOrDefault("color") as string;
        if (!IsSafeColor(color)) color = SuggestColor(id);

        return new BcrdtCollaboratorInfo(
            id,
            user.GetValueOrDefault("name") as string ?? "Anonymous",
            color!,
            user.GetValueOrDefault("avatar") as string);
    }

    private static BcrdtCollaboratorSelection? ReadSelection(BcrdtAwarenessEntry entry)
    {
        if (entry.State?.GetValueOrDefault(SelectionKey) is not IReadOnlyDictionary<string, object?> selection)
        {
            return null;
        }

        if (selection.GetValueOrDefault("field") is not string field ||
            selection.GetValueOrDefault("anchor") is not byte[] anchor ||
            selection.GetValueOrDefault("head") is not byte[] head)
        {
            return null;
        }

        try
        {
            return new BcrdtCollaboratorSelection(field, BcrdtRelativePosition.Decode(anchor), BcrdtRelativePosition.Decode(head));
        }
        catch (BcrdtFormatException)
        {
            // A peer published something we cannot read. Their cursor is not worth failing over.
            return null;
        }
    }

    private static IReadOnlyDictionary<string, object?> ReadCustom(IReadOnlyDictionary<string, object?> state)
    {
        Dictionary<string, object?>? custom = null;
        foreach ((string key, object? value) in state)
        {
            if (key is UserKey or SelectionKey or ActivityKey) continue;
            custom ??= new Dictionary<string, object?>(StringComparer.Ordinal);
            custom[key] = value;
        }
        return custom ?? (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    private void OnAwarenessChanged(BcrdtAwarenessChangedEventArgs args) => Changed?.Invoke();

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _awareness.Changed -= OnAwarenessChanged;
        Changed = null;
    }
}
