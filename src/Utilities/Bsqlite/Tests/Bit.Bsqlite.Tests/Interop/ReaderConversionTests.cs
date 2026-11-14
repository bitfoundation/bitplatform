using System.Data;
using Bit.Bsqlite.Data;
using Bit.Bsqlite.Testing;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// The values the reader's getters return are the ones Microsoft.Data.Sqlite returns for the same cell,
/// so code moved between a server and the browser reads the same numbers, text and dates.
/// </summary>
[TestClass]
public sealed class ReaderConversionTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    private BsqliteInProcessTransport _transport = null!;
    private BsqliteConnection _connection = null!;

    [TestInitialize]
    public async Task OpenAsync()
    {
        _transport = new BsqliteInProcessTransport();
        _connection = new BsqliteConnection(_transport, "conversions.db");
        await _connection.OpenAsync(Ct);
    }

    [TestCleanup]
    public async Task CloseAsync()
    {
        await _connection.DisposeAsync();
        await _transport.DisposeAsync();
    }

    private async Task<BsqliteDataReader> ReadAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.Add(new BsqliteParameter(name, value));
        }

        var reader = (BsqliteDataReader)await command.ExecuteReaderAsync(Ct);
        Assert.IsTrue(await reader.ReadAsync(Ct));
        return reader;
    }

    private async Task<object?> ScalarAsync(string sql)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(Ct);
    }

    [TestMethod]
    public async Task IntegerGetters_TruncateAReal_AsSqliteCastsOne()
    {
        await using var reader = await ReadAsync("SELECT 7 / 2.0, -2.7, 2.5");

        Assert.AreEqual(3, reader.GetInt32(0));
        Assert.AreEqual(-2L, reader.GetInt64(1));
        Assert.AreEqual((short)2, reader.GetInt16(2));
        Assert.AreEqual(3, reader.GetFieldValue<int>(0));
        Assert.AreEqual(2L, reader.GetFieldValue<long>(2));
    }

    [TestMethod]
    public async Task IntegerGetters_RefuseAValueTheTypeCannotHold()
    {
        await using var reader = await ReadAsync("SELECT 3000000000, 300");

        Assert.ThrowsExactly<OverflowException>(() => reader.GetInt32(0));
        Assert.ThrowsExactly<OverflowException>(() => reader.GetByte(1));
    }

    [TestMethod]
    public async Task GetString_ReadsABlobAsUtf8_AndARealAsSqliteWritesIt()
    {
        await using var reader = await ReadAsync("SELECT CAST('héllo' AS BLOB), 1.0, 0.1, 1e20, 100000.0");

        Assert.AreEqual("héllo", reader.GetString(0));
        Assert.AreEqual("1.0", reader.GetString(1));
        Assert.AreEqual("0.1", reader.GetString(2));
        Assert.AreEqual("1.0e+20", reader.GetString(3));
        Assert.AreEqual("100000.0", reader.GetString(4));
        Assert.AreEqual("1.0", reader.GetFieldValue<string>(1));
    }

    /// <summary>The text the reader gives for a REAL is the text SQLite itself gives for it.</summary>
    [TestMethod]
    public async Task GetString_OfARealMatchesSqlitesOwnTextConversion()
    {
        await using var reader = await ReadAsync(
            "SELECT 1.0, CAST(1.0 AS TEXT), 0.1 + 0.2, CAST(0.1 + 0.2 AS TEXT), 1e-5, CAST(1e-5 AS TEXT), -12.5, CAST(-12.5 AS TEXT)");

        for (var i = 0; i < reader.FieldCount; i += 2)
        {
            Assert.AreEqual(reader.GetString(i + 1), reader.GetString(i), $"Column {i}");
        }
    }

    [TestMethod]
    public async Task Dates_AreReadFromAJulianDayNumber()
    {
        await using var reader = await ReadAsync("SELECT julianday('2024-01-02 03:04:05.678'), CAST(julianday('2024-03-04') AS INTEGER)");

        Assert.AreEqual(new DateTime(2024, 1, 2, 3, 4, 5, 678), reader.GetDateTime(0));
        Assert.AreEqual(new DateOnly(2024, 1, 2), reader.GetFieldValue<DateOnly>(0));
        Assert.AreEqual(new DateTimeOffset(2024, 1, 2, 3, 4, 5, 678, TimeSpan.Zero), reader.GetFieldValue<DateTimeOffset>(0));
        Assert.AreEqual(new TimeOnly(3, 4, 5, 678), reader.GetFieldValue<TimeOnly>(0));
        Assert.AreEqual(new DateTime(2024, 3, 3, 12, 0, 0), reader.GetDateTime(1), "An INTEGER Julian day starts at noon.");
    }

    [TestMethod]
    public async Task GetDecimal_ReadsTextWithAnExponent()
    {
        await using var reader = await ReadAsync("SELECT '1.5e-07', '2E3'");

        Assert.AreEqual(0.00000015m, reader.GetDecimal(0));
        Assert.AreEqual(2000m, reader.GetFieldValue<decimal>(1));
    }

    [TestMethod]
    public async Task GetFieldValue_OnNull_GivesWhatTheTypeCanHold_AndRefusesTheRest()
    {
        await using var reader = await ReadAsync("SELECT NULL");

        Assert.IsNull(reader.GetFieldValue<int?>(0));
        Assert.IsNull(reader.GetFieldValue<string>(0));
        Assert.AreSame(DBNull.Value, reader.GetFieldValue<object>(0));
        Assert.ThrowsExactly<InvalidOperationException>(() => reader.GetFieldValue<int>(0));
        Assert.ThrowsExactly<InvalidOperationException>(() => reader.GetInt64(0));
    }

    [TestMethod]
    public async Task GetFieldValue_ReadsAStreamAndATextReader()
    {
        await using var reader = await ReadAsync("SELECT x'010203', 'text'");

        using var stream = reader.GetFieldValue<Stream>(0);
        Assert.AreEqual(3, stream.Length);
        using var text = reader.GetFieldValue<TextReader>(1);
        Assert.AreEqual("text", await text.ReadToEndAsync(Ct));
        CollectionAssert.AreEqual("text"u8.ToArray(), reader.GetFieldValue<byte[]>(1));
    }

    [TestMethod]
    public async Task GetOrdinal_PrefersTheColumnWhoseNameMatchesExactly()
    {
        await using var reader = await ReadAsync("SELECT 1 AS Id, 2 AS id");

        Assert.AreEqual(0, reader.GetOrdinal("Id"));
        Assert.AreEqual(1, reader.GetOrdinal("id"));
        Assert.AreEqual(0, reader.GetOrdinal("ID"));
    }

    [TestMethod]
    public async Task AClosedReader_RefusesToRead()
    {
        var reader = await ReadAsync("SELECT 1 UNION ALL SELECT 2");
        await reader.CloseAsync();

        Assert.ThrowsExactly<InvalidOperationException>(() => reader.Read());
        Assert.ThrowsExactly<InvalidOperationException>(() => reader.GetValue(0));
    }

    [TestMethod]
    public async Task ExecuteScalar_TellsANullCellFromNoRow()
    {
        Assert.AreSame(DBNull.Value, await ScalarAsync("SELECT NULL"));
        Assert.IsNull(await ScalarAsync("SELECT 1 WHERE 0"));
    }

    [TestMethod]
    public async Task ExecuteNonQuery_OfAQuery_ReportsNoRecordsAffected()
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT 1";

        Assert.AreEqual(-1, await command.ExecuteNonQueryAsync(Ct));
    }

    private enum Status : short
    {
        Draft = 1,
        Shipped = 4,
    }

    [TestMethod]
    public async Task AnEnumParameter_IsBoundAsItsUnderlyingInteger()
    {
        await using var reader = await ReadAsync("SELECT @status, typeof(@status)", ("@status", Status.Shipped));

        Assert.AreEqual(4L, reader.GetInt64(0));
        Assert.AreEqual("integer", reader.GetString(1));
        Assert.AreEqual(Status.Shipped, reader.GetFieldValue<Status>(0));
    }

    [TestMethod]
    public async Task Opening_AndClosing_RaiseStateChange()
    {
        await using var connection = new BsqliteConnection(_transport, "conversions.db");
        var changes = new List<(ConnectionState From, ConnectionState To)>();
        connection.StateChange += (_, e) => changes.Add((e.OriginalState, e.CurrentState));

        await connection.OpenAsync(Ct);
        await connection.CloseAsync();

        CollectionAssert.AreEqual(
            new[] { (ConnectionState.Closed, ConnectionState.Open), (ConnectionState.Open, ConnectionState.Closed) },
            changes);
    }

    /// <summary>
    /// A transaction left open on a connection that is disposed - begun without <c>using</c> - would keep
    /// the turn every other connection over the transport waits for. It is rolled back, as
    /// Microsoft.Data.Sqlite rolls one back.
    /// </summary>
    [TestMethod]
    public async Task DisposingAConnection_RollsBackItsTransaction_AndLetsTheNextOneIn()
    {
        await using (var setup = _connection.CreateCommand())
        {
            setup.CommandText = "CREATE TABLE note (body TEXT)";
            await setup.ExecuteNonQueryAsync(Ct);
        }

        var abandoned = new BsqliteConnection(_transport, "conversions.db");
        await abandoned.OpenAsync(Ct);
        _ = await abandoned.BeginTransactionAsync(Ct);
        await using (var insert = abandoned.CreateCommand())
        {
            insert.CommandText = "INSERT INTO note VALUES ('never committed')";
            await insert.ExecuteNonQueryAsync(Ct);
        }

        await abandoned.DisposeAsync();

        _connection.DefaultTimeout = 5;
        await using var next = await _connection.BeginTransactionAsync(Ct);
        Assert.AreEqual(0L, await ScalarInTransactionAsync("SELECT COUNT(*) FROM note"));
        Assert.AreEqual(ConnectionState.Closed, abandoned.State);
    }

    private async Task<object?> ScalarInTransactionAsync(string sql)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(Ct);
    }
}
