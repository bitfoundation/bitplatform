using System.Globalization;
using System.Text.Json.Nodes;

namespace Bit.Bcrdt.Blex;

/// <summary>
/// A Blex store whose state is a projection of part of a collaborative document.
/// </summary>
/// <typeparam name="TState">
/// The store's state: an immutable view of <see cref="Source"/> - a record, a string, an immutable list.
/// </typeparam>
/// <remarks>
/// <para>
/// The document is the state, and <see cref="State"/> is what <c>project</c> reads out of it. Nothing
/// changes it but the document, so there are two ways in, and both are Blex actions:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <see cref="Act(string, Action, BlexActionArg[])"/> runs the store's own change as one named action
/// and one document transaction, tagged with <see cref="Origin"/> - one entry for middleware and
/// DevTools, one update on the wire, one step for a <see cref="BcrdtUndoManager"/> that tracks the origin.
/// </description></item>
/// <item><description>
/// Every other change to the subtree - a collaborator's, an undo manager's, another store's on the same
/// document - is dispatched as <see cref="RemoteActionName"/>, so selectors, <c>Subscribe</c>,
/// <c>BlexSelect</c>, manager subscriptions and DevTools all hear about it as they hear about anything
/// else. A transaction of the store's own origin is never dispatched again: it has been projected by
/// the action that made it, so a change does not come back as an echo.
/// </description></item>
/// </list>
/// <para>
/// The generator is not involved, and does not need to be: derive from this class and give the store
/// its actions as methods that call <see cref="Act(string, Action, BlexActionArg[])"/>. A store built
/// this way has no generated <c>Actions</c> descriptors; override <c>DescribeActions</c> for dispatch
/// by name.
/// </para>
/// <para>
/// A restore - Blex's undo history, DevTools time travel, persistence, the prerender handoff - cannot
/// rewrite a shared document, so it projects the document again instead. Snapshot undo would take back
/// collaborators' work along with the user's; keep this store out of <c>BlexHistory</c> and undo with a
/// <see cref="BcrdtUndoManager"/> over <see cref="Source"/> that tracks <see cref="Origin"/>. Persist the
/// document, not the store: <see cref="IBlexStore.Persist"/> is <see langword="false"/>.
/// </para>
/// <para>
/// Like the document and every Blex store, it is not thread-safe: use it on the context the document
/// is changed on - in Blazor, the renderer's, which is where a document opened through the
/// collaboration service applies network updates.
/// </para>
/// </remarks>
public class BcrdtBlexStore<TState> : BlexStoreBase
{
    private readonly string _name;
    private readonly Func<TState> _project;
    private readonly IDisposable _subscription;
    private TState _state;
    private int _acting;

    /// <summary>Creates a store over part of a document.</summary>
    /// <param name="name">The store's name, unique in its manager - the slice key in DevTools.</param>
    /// <param name="source">The map, array or text the store projects.</param>
    /// <param name="project">
    /// Reads <see cref="State"/> out of <paramref name="source"/>. Called once now and once after each
    /// change, so return a fresh value: a record compares by value, and a selector over one of its
    /// members only fires when that member changed. <c>() =&gt; BcrdtAnyCodec.ToObject&lt;T&gt;(map.ToJson())</c>
    /// reads a map through JSON, for an application whose types survive trimming.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="source"/> does not belong to a document.</exception>
    public BcrdtBlexStore(string name, BcrdtAbstractType source, Func<TState> project)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(project);
        if (!source.IsAttached) throw new ArgumentException("The source must belong to a document.", nameof(source));

        _name = name;
        Source = source;
        _project = project;
        _state = project();
        _subscription = source.ObserveDeep(OnDocumentChanged);
    }

    /// <inheritdoc/>
    /// <remarks>A scoped instance (<c>manager.CreateScope</c>, <c>BlexScope</c>) is named <c>"{name}:{key}"</c>.</remarks>
    public override string Name => WithInstanceKey(_name);

    /// <summary>The part of the document the store projects.</summary>
    public BcrdtAbstractType Source { get; }

    /// <summary>
    /// The origin of the store's transactions - the store itself. Add it to a
    /// <see cref="BcrdtUndoManagerOptions.TrackedOrigins"/> to undo what the store's actions did.
    /// </summary>
    public object Origin => this;

    /// <summary>What the store's part of the document holds, as of the last change.</summary>
    public TState State => _state;

    /// <summary>The name changes made elsewhere are dispatched under. <c>"Remote change"</c> by default.</summary>
    public string RemoteActionName { get; init; } = "Remote change";

    /// <summary>
    /// Changes the document as one named action: <paramref name="change"/> runs in a single document
    /// transaction tagged with <see cref="Origin"/>, and the state is projected once it has run.
    /// </summary>
    /// <remarks>
    /// A filter that vetoes the action stops the change too - <paramref name="change"/> does not run. A
    /// change that throws part-way leaves what it already did in the document, as any CRDT operation
    /// does (there is no rollback, only new operations); the state still shows it, and the exception
    /// reaches the caller and <c>OnActionError</c>.
    /// </remarks>
    /// <param name="actionName">The action's name, as middleware, subscribers and DevTools see it.</param>
    /// <param name="change">Changes <see cref="Source"/>.</param>
    /// <param name="args">The action's payload.</param>
    public void Act(string actionName, Action change, params BlexActionArg[] args)
    {
        ArgumentException.ThrowIfNullOrEmpty(actionName);
        ArgumentNullException.ThrowIfNull(change);
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        Dispatch(actionName, () =>
        {
            _acting++;
            try
            {
                Source.Doc.Transact(_ => change(), Origin);
            }
            finally
            {
                _acting--;
                Project();
            }
        }, args.Length == 0 ? null : args);
    }

    private void Project() => SetState(ref _state, _project(), nameof(State));

    private void OnDocumentChanged(IReadOnlyList<BcrdtEvent> events, BcrdtTransaction transaction)
    {
        if (IsDisposed) return;

        // The store's own change was projected by the action that made it. One of its own origin that
        // committed after that action ended - made from inside another transaction, which it joined -
        // is still projected, or the state would be left behind the document.
        if (ReferenceEquals(transaction.Origin, Origin) && _acting > 0) return;
        if (ReferenceEquals(transaction.Origin, Origin) && EqualityComparer<TState>.Default.Equals(_state, _project())) return;

        Dispatch(RemoteActionName, Project, null);
    }

    /// <inheritdoc/>
    /// <remarks>The document's own contents, under <c>State</c>, for DevTools and snapshots.</remarks>
    public override JsonObject SerializeState() => new() { ["State"] = ToNode(Source.ToJson()) };

    /// <inheritdoc/>
    /// <remarks>
    /// The document is the state: a snapshot cannot rewrite what collaborators share, so the store reads
    /// the document again instead of taking the snapshot's values.
    /// </remarks>
    public override void DeserializeState(JsonObject state) => _state = _project();

    private static JsonNode? ToNode(object? value) => value switch
    {
        null => null,
        string text => JsonValue.Create(text),
        bool flag => JsonValue.Create(flag),
        long number => JsonValue.Create(number),
        double number => JsonValue.Create(number),
        float number => JsonValue.Create(number),
        decimal number => JsonValue.Create(number),
        DateTimeOffset moment => JsonValue.Create(moment.ToString("o", CultureInfo.InvariantCulture)),
        DateTime moment => JsonValue.Create(moment.ToString("o", CultureInfo.InvariantCulture)),
        Guid id => JsonValue.Create(id.ToString()),
        byte[] bytes => JsonValue.Create(Convert.ToBase64String(bytes)),
        IEnumerable<KeyValuePair<string, object?>> map => ToObject(map),
        System.Collections.IEnumerable items => ToArray(items),
        _ => JsonValue.Create(Convert.ToString(value, CultureInfo.InvariantCulture)),
    };

    private static JsonObject ToObject(IEnumerable<KeyValuePair<string, object?>> map)
    {
        var result = new JsonObject();
        foreach ((string key, object? value) in map) result[key] = ToNode(value);
        return result;
    }

    private static JsonArray ToArray(System.Collections.IEnumerable items)
    {
        var result = new JsonArray();
        foreach (object? item in items) result.Add(ToNode(item));
        return result;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing) _subscription.Dispose();
        base.Dispose(disposing);
    }
}
