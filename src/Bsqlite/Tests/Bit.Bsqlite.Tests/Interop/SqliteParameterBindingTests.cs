using Bit.Bsqlite.Data;
using Bit.Bsqlite.Interop;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// The wire format refuses dates and GUIDs. The ADO.NET binder must convert them first, using the
/// same TEXT forms Microsoft.Data.Sqlite binds, or a browser SaveChanges of a DateTime column fails.
/// </summary>
[TestClass]
public sealed class SqliteParameterBindingTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public void DateTime_MatchesMicrosoftDataSqliteText()
    {
        var value = BsqliteParameterBinding.ToTransportValue(
            new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

        Assert.AreEqual("2026-03-01 12:00:00", value);
    }

    [TestMethod]
    public void Guid_MatchesMicrosoftDataSqliteText()
    {
        var guid = Guid.Parse("d1e2f3a4-b5c6-4789-abcd-ef0123456789");

        Assert.AreEqual("D1E2F3A4-B5C6-4789-ABCD-EF0123456789", BsqliteParameterBinding.ToTransportValue(guid));
    }

    [TestMethod]
    public void ConvertedValues_AreAcceptedByTheWireFormat()
    {
        var date = BsqliteParameterBinding.ToTransportValue(new DateTime(2026, 1, 1));
        var guid = BsqliteParameterBinding.ToTransportValue(Guid.Empty);

        var (dateType, dateValue) = BsqliteWireFormat.EncodeValue(date, "when");
        var (guidType, guidValue) = BsqliteWireFormat.EncodeValue(guid, "id");

        Assert.AreEqual(BsqliteWireFormat.TypeCode.Text, dateType);
        Assert.AreEqual("2026-01-01 00:00:00", dateValue);
        Assert.AreEqual(BsqliteWireFormat.TypeCode.Text, guidType);
        Assert.AreEqual("00000000-0000-0000-0000-000000000000", guidValue);
    }

    [TestMethod]
    public void FromTransportValue_ParsesTheTextToTransportValueWrote()
    {
        var lead = TimeSpan.FromDays(2);
        var born = new DateOnly(1978, 4, 12);
        var ordered = new DateTimeOffset(2026, 6, 2, 10, 15, 0, TimeSpan.Zero);

        Assert.AreEqual(lead, BsqliteParameterBinding.FromTransportValue<TimeSpan>(
            BsqliteParameterBinding.ToTransportValue(lead)));
        Assert.AreEqual(born, BsqliteParameterBinding.FromTransportValue<DateOnly>(
            BsqliteParameterBinding.ToTransportValue(born)));
        Assert.AreEqual(ordered, BsqliteParameterBinding.FromTransportValue<DateTimeOffset>(
            BsqliteParameterBinding.ToTransportValue(ordered)));
    }

    [TestMethod]
    public async Task Reader_GetFieldValue_ParsesTimeSpanText()
    {
        var transport = new ResultTransport(new BsqliteCommandResult
        {
            ColumnNames = ["lead"],
            ColumnTypes = ["TEXT"],
            Rows = [["2.00:00:00"]],
        });
        await using var connection = new BsqliteConnection(transport, "read.db");
        await connection.OpenAsync(Ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT lead FROM t";
        await using var reader = await command.ExecuteReaderAsync(Ct);

        Assert.IsTrue(await reader.ReadAsync(Ct));
        Assert.AreEqual(TimeSpan.FromDays(2), reader.GetFieldValue<TimeSpan>(0));
    }

    [TestMethod]
    public async Task Command_ConvertsDateTimeBeforeTheTransportSeesIt()
    {
        var transport = new CapturingTransport();
        await using var connection = new BsqliteConnection(transport, "bind.db");
        await connection.OpenAsync(Ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO t (d) VALUES (@p1)";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@p1";
        parameter.Value = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        command.Parameters.Add(parameter);

        await command.ExecuteNonQueryAsync(Ct);

        var batch = Assert.ContainsSingle(transport.LastBatch!);
        Assert.AreEqual("2026-03-01 12:00:00", Assert.ContainsSingle(batch.Parameters).Value);
        _ = BsqliteWireFormat.EncodeBatch(transport.LastBatch!);
    }

    private sealed class CapturingTransport : IBsqliteTransport
    {
        public IReadOnlyList<BsqliteCommandRequest>? LastBatch { get; private set; }

        public Task OpenAsync(string databaseName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task CloseAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<BsqliteCommandResult>> ExecuteAsync(
            IReadOnlyList<BsqliteCommandRequest> batch,
            CancellationToken cancellationToken = default)
        {
            LastBatch = batch;
            return Task.FromResult<IReadOnlyList<BsqliteCommandResult>>([new BsqliteCommandResult()]);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class ResultTransport(BsqliteCommandResult result) : IBsqliteTransport
    {
        public Task OpenAsync(string databaseName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task CloseAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<BsqliteCommandResult>> ExecuteAsync(
            IReadOnlyList<BsqliteCommandRequest> batch,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BsqliteCommandResult>>([result]);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
