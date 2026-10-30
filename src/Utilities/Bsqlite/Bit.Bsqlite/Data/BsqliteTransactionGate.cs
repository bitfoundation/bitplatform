using System.Runtime.CompilerServices;

namespace Bit.Bsqlite.Data;

/// <summary>
/// Whose turn it is on a transport: a transaction holds it from <c>BEGIN</c> to its end, and a
/// command outside any transaction holds it for its one round trip.
/// </summary>
/// <remarks>
/// <para>
/// The engine behind a transport is a single SQLite connection, so a transaction on it takes in
/// every statement that arrives while it is open. Every context of a session shares that
/// connection, and every statement is a real round trip, so without turns one context's
/// <c>SaveChangesAsync</c> could begin while another's was mid-transaction - refused as a nested
/// transaction - or a plain write could land inside someone else's transaction and roll back with
/// it. Separate SQLite connections settle this with the file lock and a busy timeout; this is the
/// same rule for statements sharing one: wait for the transaction to end, for as long as the
/// command's timeout allows.
/// </para>
/// <para>
/// Keyed by transport rather than held by a connection, so several connections over one transport
/// take turns too.
/// </para>
/// </remarks>
internal sealed class BsqliteTransactionGate
{
    private static readonly ConditionalWeakTable<IBsqliteTransport, BsqliteTransactionGate> Gates = new();

    private readonly SemaphoreSlim _turn = new(1, 1);

    public static BsqliteTransactionGate For(IBsqliteTransport transport) => Gates.GetValue(transport, _ => new());

    /// <summary>
    /// Waits for the turn, at most <paramref name="timeoutSeconds"/> - 0 waits as long as it takes,
    /// as <c>CommandTimeout</c> does - and returns it, to be disposed to pass it on.
    /// </summary>
    /// <exception cref="BsqliteConcurrencyException">The turn did not come in time.</exception>
    public async Task<IDisposable> EnterAsync(int timeoutSeconds, string databaseName, CancellationToken cancellationToken)
    {
        var timeout = timeoutSeconds <= 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(timeoutSeconds);

        if (!await _turn.WaitAsync(timeout, cancellationToken).ConfigureAwait(false))
        {
            throw new BsqliteConcurrencyException(
                $"The database '{databaseName}' is locked: a transaction another context began on it has not "
                + $"finished in {timeoutSeconds} s. Contexts of a session take turns, so a transaction keeps every "
                + "other context waiting until it commits or rolls back - for good when the code holding it is "
                + "itself waiting for one of them. Finish the transaction before using another context, or raise "
                + "CommandTimeout (BsqliteConnection.DefaultTimeout for BeginTransactionAsync).");
        }

        return new Turn(_turn);
    }

    private sealed class Turn(SemaphoreSlim turn) : IDisposable
    {
        private SemaphoreSlim? _turn = turn;

        public void Dispose() => Interlocked.Exchange(ref _turn, null)?.Release();
    }
}
