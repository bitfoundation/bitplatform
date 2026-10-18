using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Bsqlite.Tests.E2E.Infrastructure;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>
/// The binary wire format is what the .NET transport speaks. These tests pin the layout from the JavaScript
/// side; <c>SqliteWireFormatTests</c> pins it from the .NET side with a byte-for-byte builder.
/// </summary>
[TestClass]
public sealed class BinaryWireTests : BsqliteTest
{
    [TestMethod]
    public async Task A_batch_encodes_to_the_documented_header()
    {
        await OpenAsync();

        var head = await Page.EvaluateAsync<int[]>(
            """
            () => {
              const { encodeBatchBinary } = globalThis.BitBsqlite.host;
              const bytes = encodeBatchBinary([{ commandText: 'SELECT 1', resultKind: 'scalar', parameters: [] }]);
              return Array.from(bytes.subarray(0, 6));
            }
            """);

        // magic, version, request count (u32 LE)
        CollectionAssert.AreEqual(new[] { 0xb5, 1, 1, 0, 0, 0 }, head);
    }

    [TestMethod]
    public async Task Every_storage_class_survives_the_round_trip()
    {
        await OpenAsync();

        var row = await Page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const { encodeBatchBinary, decodeReplyBinary, encodeValue } = globalThis.BitBsqlite.host;
              const run = async batch => decodeReplyBinary(await globalThis.host.executeBinary(encodeBatchBinary(batch)));

              await run([{ commandText: 'CREATE TABLE v (i INTEGER, r REAL, t TEXT, b BLOB, n INTEGER)', parameters: [] }]);
              await run([{
                commandText: 'INSERT INTO v VALUES (@i, @r, @t, @b, @n)',
                parameters: [
                  { name: '@i', ...encodeValue(9223372036854775807n) },
                  { name: '@r', ...encodeValue(-2.5) },
                  { name: '@t', ...encodeValue('héllo 🌍') },
                  { name: '@b', ...encodeValue(new Uint8Array([0, 128, 255])) },
                  { name: '@n', ...encodeValue(null) },
                ],
              }]);

              const [result] = await run([{ commandText: 'SELECT i, r, t, b, n FROM v', resultKind: 'reader', parameters: [] }]);
              return { types: result.columnTypes, t: result.rows[0].t, v: result.rows[0].v };
            }
            """);

        AssertJson.AreEqual("""["INTEGER", "REAL", "TEXT", "BLOB", null]""", row.GetProperty("types"));
        AssertJson.AreEqual("[1, 2, 3, 4, 5]", row.GetProperty("t"));

        var v = row.GetProperty("v");
        Assert.AreEqual("9223372036854775807", v[0].GetString());
        Assert.AreEqual(-2.5, v[1].GetDouble());
        Assert.AreEqual("héllo 🌍", v[2].GetString());
        Assert.AreEqual(Convert.ToBase64String([0, 128, 255]), v[3].GetString());
        Assert.AreEqual(JsonValueKind.Null, v[4].ValueKind);
    }

    [TestMethod]
    public async Task A_failure_is_a_reply_not_a_rejection()
    {
        await OpenAsync();

        var outcome = await Page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const { encodeBatchBinary, decodeReplyBinary } = globalThis.BitBsqlite.host;
              const reply = await globalThis.host.executeBinary(encodeBatchBinary([
                { commandText: 'SELECT * FROM nope', resultKind: 'reader', parameters: [] },
              ]));

              try {
                decodeReplyBinary(reply);
                return { threw: false };
              } catch (error) {
                return { threw: true, message: error.message, sqliteCode: error.sqliteCode };
              }
            }
            """);

        Assert.IsTrue(outcome.GetProperty("threw").GetBoolean());
        Assert.MatchesRegex(new Regex("no such table"), outcome.GetProperty("message").GetString()!);
        Assert.AreEqual(1, outcome.GetProperty("sqliteCode").GetInt32());
    }

    [TestMethod]
    public async Task A_corrupt_batch_is_reported_not_thrown()
    {
        await OpenAsync();

        var outcome = await Page.EvaluateAsync<string?>(
            """
            async () => {
              const { decodeReplyBinary } = globalThis.BitBsqlite.host;
              const reply = await globalThis.host.executeBinary(new Uint8Array([1, 2, 3]));
              try {
                decodeReplyBinary(reply);
                return null;
              } catch (error) {
                return error.message;
              }
            }
            """);

        Assert.IsNotNull(outcome);
        Assert.MatchesRegex(new Regex("Not a Bsqlite wire message"), outcome);
    }

    [TestMethod]
    public async Task A_large_result_set_round_trips_intact()
    {
        await OpenAsync();

        var summary = await Page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const { encodeBatchBinary, decodeReplyBinary } = globalThis.BitBsqlite.host;
              const run = async batch => decodeReplyBinary(await globalThis.host.executeBinary(encodeBatchBinary(batch)));

              await run([{ commandText: 'CREATE TABLE big (id INTEGER PRIMARY KEY, payload TEXT)', parameters: [] }]);
              await run([{
                commandText: "WITH RECURSIVE n(i) AS (SELECT 1 UNION ALL SELECT i + 1 FROM n WHERE i < 5000) INSERT INTO big (payload) SELECT 'row-' || i FROM n",
                parameters: [],
              }]);

              const [result] = await run([{ commandText: 'SELECT id, payload FROM big ORDER BY id', resultKind: 'reader', parameters: [] }]);
              return { count: result.rows.length, last: result.rows[4999].v };
            }
            """);

        Assert.AreEqual(5000, summary.GetProperty("count").GetInt32());
        AssertJson.AreEqual("""[5000, "row-5000"]""", summary.GetProperty("last"));
    }

    // Every test starts from a database of its own (the describe block's beforeEach).
    private Task<HostVersion> OpenAsync()
        => OpenHostAsync(Page, new HostOptions { DatabaseName = $"wire-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" });
}
