namespace Bit.Bsqlite.Data;

/// <summary>
/// Executes SQLite work on behalf of the ADO.NET layer. Implementations decide where the engine
/// actually lives - a web worker, an in-process engine, or a test double.
/// </summary>
/// <remarks>
/// The contract is deliberately coarse: one round trip per command batch, because in the browser
/// the dominant cost is crossing the interop boundary, not executing SQL.
/// </remarks>
public interface IBsqliteTransport : IAsyncDisposable
{
    /// <summary>Opens <paramref name="databaseName"/>, creating it when absent.</summary>
    Task OpenAsync(string databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases the database handle. Called by whoever owns the transport, not by
    /// <see cref="BsqliteConnection"/> - see its <c>Close</c> documentation for why.
    /// </summary>
    Task CloseAsync(CancellationToken cancellationToken = default);

    /// <summary>Executes a batch and returns one result per request, in order.</summary>
    Task<IReadOnlyList<BsqliteCommandResult>> ExecuteAsync(
        IReadOnlyList<BsqliteCommandRequest> batch,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Raised when committed writes touched tables in the open database: another tab's writes
    /// always, and this transport's own writes when <see cref="ReportsLocalWrites"/> is true.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="BsqliteConnection"/> subscribes to this and re-raises it as
    /// <see cref="BsqliteConnection.TablesChanged"/>, which is what makes a live query re-run.
    /// A transport that reports its own writes must do so only once they are committed - a live
    /// query in another tab that re-ran on an uncommitted write would read the old data and then
    /// never hear about the commit.
    /// </para>
    /// <para>
    /// Implemented as a no-op by default: a transport with no way to hear about other writers - a
    /// test double, say - is still a complete transport, and the connection covers its local
    /// writes from the SQL it sends.
    /// </para>
    /// </remarks>
    event EventHandler<BsqliteTablesChangedEventArgs>? TablesChanged
    {
        add { }
        remove { }
    }

    /// <summary>
    /// Whether this transport raises <see cref="TablesChanged"/> for the writes it performs itself,
    /// so the connection must not.
    /// </summary>
    /// <remarks>
    /// A transport that sits next to the engine can be exact about what changed - SQLite's update
    /// hook names every table a row landed in, including through triggers and cascades - and about
    /// when, since it sees the commit. One that cannot leaves this <see langword="false"/>, and
    /// <see cref="BsqliteConnection"/> derives the tables from the SQL it sends instead.
    /// Reporting on both sides would re-run every live query twice per write.
    /// </remarks>
    bool ReportsLocalWrites => false;
}
