using Bit.Bsqlite.Data;
using Bit.Bsqlite.Interop;

namespace Bit.Bsqlite.Testing;

/// <summary>
/// An <see cref="IBsqliteTransport"/> that runs SQLite in-process via Microsoft.Data.Sqlite.
/// </summary>
/// <remarks>
/// Stands in for the web-worker transport so provider behaviour can be tested on desktop .NET. It is
/// the <see cref="BsqliteNativeTransport"/> a Blazor Hybrid app runs on, so it mirrors the worker's
/// responsibilities exactly - installing the EF function set (see <see cref="BsqliteFunctions"/>),
/// reporting committed writes through the same update-hook and commit-hook mechanism, and
/// snapshotting through <c>sqlite3_serialize</c> - so a test that passes here is a meaningful signal.
/// It adds a record of the statements it ran, and defaults to a private in-memory database.
/// </remarks>
public sealed class BsqliteInProcessTransport : BsqliteNativeTransport
{
    /// <param name="connectionString">
    /// Defaults to a private in-memory database that lives as long as the connection.
    /// </param>
    /// <param name="registerEfFunctions">
    /// When <see langword="false"/>, the EF function set is deliberately omitted so tests can
    /// demonstrate what breaks without it.
    /// </param>
    public BsqliteInProcessTransport(
        string? connectionString = null,
        bool registerEfFunctions = true)
        : base(connectionString ?? "Data Source=:memory:", onOpenSql: null, registerEfFunctions)
    {
    }

    /// <summary>Statements executed so far, in order. Useful for asserting generated SQL.</summary>
    public List<string> ExecutedCommands { get; } = [];

    /// <inheritdoc />
    protected override void OnExecuting(BsqliteCommandRequest request)
        => ExecutedCommands.Add(request.CommandText);
}
