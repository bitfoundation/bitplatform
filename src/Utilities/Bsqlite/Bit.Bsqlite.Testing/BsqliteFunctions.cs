using Bit.Bsqlite.Interop;
using Microsoft.Data.Sqlite;

namespace Bit.Bsqlite.Testing;

/// <summary>
/// Registers the scalar functions, aggregates, and collation that EF Core's SQLite provider
/// normally installs itself.
/// </summary>
/// <remarks>
/// <para>
/// EF's <c>SqliteRelationalConnection</c> only installs these when the connection is literally a
/// <see cref="SqliteConnection"/>; against any other <c>DbConnection</c> it logs
/// <c>UnexpectedConnectionTypeWarning</c> and moves on. Since Bsqlite supplies its own
/// connection, it inherits the obligation - miss it and every <see cref="decimal"/> comparison,
/// aggregate, and ordering silently produces wrong answers.
/// </para>
/// <para>
/// The same set <see cref="BsqliteNativeTransport"/> installs on every connection it opens, and the
/// reference for the worker-side UDF host: same names, same semantics.
/// </para>
/// </remarks>
public static class BsqliteFunctions
{
    /// <summary>Installs every function EF Core's SQLite provider expects to exist.</summary>
    public static void Register(SqliteConnection connection) => BsqliteNativeFunctions.Register(connection);
}
