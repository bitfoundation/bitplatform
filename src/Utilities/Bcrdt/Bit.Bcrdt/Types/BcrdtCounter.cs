using System.Globalization;

namespace Bit.Bcrdt;

/// <summary>
/// A collaborative integer counter: concurrent increments and decrements from any number of replicas
/// add up rather than overwrite one another.
/// </summary>
/// <remarks>
/// <para>
/// Storing a number in a <see cref="BcrdtMap"/> and writing <c>count + 1</c> back loses increments:
/// two clients that both read 5 and both write 6 resolve last-writer-wins, and the counter ends at 6
/// instead of 7. A counter is the type that gets this right. It is the standard positive-negative
/// counter construction laid over the engine's key index: every replica accumulates its own changes
/// under a key of its own - its <see cref="BcrdtDocument.ClientId"/> - which no other replica ever
/// writes, so no write can conflict with another, and the value is the sum over every key.
/// </para>
/// <para>
/// That rests on client identifiers being unique to one live session, as the whole engine does: two
/// sessions sharing one - an explicit <see cref="BcrdtDocumentOptions.ClientId"/> reused across devices,
/// say - corrupt each other's blocks long before they lose each other's increments.
/// </para>
/// <para>
/// Because it is built on the shared primitives it needs no merge code of its own and inherits
/// everything else: change events, <see cref="BcrdtUndoManager"/> (undoing an increment takes exactly
/// that increment back, leaving everyone else's alone), garbage collection, encoding, persistence and
/// synchronization.
/// </para>
/// <para>
/// A counter holds one key per session that ever changed it, because a client identifier is drawn per
/// session. Each key costs a few bytes; a counter changed by many thousands of short-lived sessions
/// costs accordingly. The total wraps around on <see cref="long"/> overflow - identically on every
/// replica - while a local change that would overflow this replica's own share throws.
/// </para>
/// <para>
/// The wire kind is <see cref="BcrdtTypeKind.Counter"/>; a replica that predates it rejects an update
/// carrying a counter as malformed rather than misreading it.
/// </para>
/// </remarks>
public sealed class BcrdtCounter : BcrdtAbstractType
{
    private long? _pending;

    /// <summary>Creates a counter at zero, not yet attached to a document.</summary>
    /// <remarks>
    /// A detached counter can be changed before it is inserted anywhere; its value becomes this
    /// replica's share when it is.
    /// </remarks>
    public BcrdtCounter() => _pending = 0;

    /// <summary>Creates a detached counter starting at <paramref name="initialValue"/>.</summary>
    public BcrdtCounter(long initialValue) => _pending = initialValue;

    /// <inheritdoc/>
    public override BcrdtTypeKind Kind => BcrdtTypeKind.Counter;

    /// <summary>The current total: the sum of every replica's share.</summary>
    public long Value
    {
        get
        {
            if (_pending is { } pending) return pending;

            long total = 0;
            foreach (BcrdtItem item in KeyIndex.Values)
            {
                if (!item.Deleted && item.Content.GetValue(item.Length - 1) is long share) total = unchecked(total + share);
            }
            return total;
        }
    }

    /// <summary>This replica's own share of the total.</summary>
    /// <remarks>What an undo of this replica's changes can take back, at most.</remarks>
    public long LocalShare
    {
        get
        {
            if (_pending is { } pending) return pending;
            return BcrdtTypeOperations.MapGet(this, OwnKey) as long? ?? 0;
        }
    }

    private string OwnKey => Doc.ClientId.ToString(CultureInfo.InvariantCulture);

    protected internal override BcrdtAbstractType CreateInstance() => new BcrdtCounter();

    protected internal override BcrdtEvent CreateEvent(BcrdtTransaction transaction, HashSet<string?> changedKeys) =>
        new BcrdtCounterEvent(this, transaction);

    protected internal override void Integrate(BcrdtDocument doc, BcrdtItem? item)
    {
        base.Integrate(doc, item);

        if (_pending is not { } buffered) return;
        _pending = null;
        if (buffered != 0) Add(buffered);
    }

    /// <summary>Adds <paramref name="amount"/> to the counter; a negative amount subtracts.</summary>
    /// <exception cref="OverflowException">This replica's share would overflow <see cref="long"/>.</exception>
    public void Add(long amount)
    {
        if (amount == 0) return;

        if (_pending is { } pending)
        {
            _pending = checked(pending + amount);
            return;
        }

        Doc.Transact(transaction =>
        {
            // Read inside the transaction: the client identifier, and so the key, is only stable
            // there - a transaction that integrated our own identifier from a peer moves us to a new
            // one when it settles.
            string key = OwnKey;
            long current = BcrdtTypeOperations.MapGet(this, key) as long? ?? 0;
            BcrdtTypeOperations.MapSet(transaction, this, key, checked(current + amount));
        });
    }

    /// <summary>Adds one.</summary>
    public void Increment() => Add(1);

    /// <summary>Subtracts one.</summary>
    public void Decrement() => Add(-1);

    /// <inheritdoc/>
    /// <remarks>The copy starts at this counter's current total, with no collaborative history.</remarks>
    public override BcrdtCounter Clone() => new(Value);

    /// <inheritdoc/>
    public override object ToJson() => Value;

    /// <inheritdoc/>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>What changed in a <see cref="BcrdtCounter"/> during one transaction.</summary>
public sealed class BcrdtCounterEvent(BcrdtCounter target, BcrdtTransaction transaction) : BcrdtEvent(target, transaction)
{
    /// <summary>The counter that changed.</summary>
    public new BcrdtCounter Target => (BcrdtCounter)base.Target;

    /// <summary>
    /// How much the total moved in this transaction, from every replica whose change it carried.
    /// </summary>
    public long Change
    {
        get
        {
            long change = 0;
            foreach (BcrdtKeyChange key in Keys.Values)
            {
                long before = key.OldValue as long? ?? 0;
                long after = key.NewValue as long? ?? 0;
                change = unchecked(change + (after - before));
            }
            return change;
        }
    }
}
