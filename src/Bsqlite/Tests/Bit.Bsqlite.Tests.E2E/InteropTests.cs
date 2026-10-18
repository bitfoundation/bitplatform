using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Bsqlite.Tests.E2E.Infrastructure;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>
/// The exact conversation the .NET transport has with the host: create a host, then drive every operation
/// through <c>call</c> so a failure is data rather than a thrown error. These tests pin the envelope shape
/// <c>BsqliteWireFormat.DecodeCall</c> reads; a change here without a matching change there is a broken
/// release.
/// </summary>
[TestClass]
public sealed class InteropTests : BsqliteTest
{
    [TestMethod]
    public async Task CreateHost_plus_call_opens_a_database_without_throwing()
    {
        await GotoHostPageAsync(Page);

        var envelope = await Page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const host = globalThis.BitBsqlite.host.createHost();
              globalThis.host = host;
              return await host.call({
                kind: 'open',
                databaseName: 'interop.db',
                requiredBuild: 'synchronous',
              });
            }
            """);

        AssertJson.AreEqual(
            """
            {
              "ok": true,
              "result": { "build": "synchronous", "vfsName": null, "reused": false }
            }
            """, envelope);
    }

    [TestMethod]
    public async Task A_successful_execute_carries_tagged_rows_not_raw_JavaScript_values()
    {
        await GotoHostPageAsync(Page);

        var envelope = await Page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const { createHost, encodeValue } = globalThis.BitBsqlite.host;
              const host = createHost();
              await host.call({ kind: 'open', databaseName: 'wire.db', requiredBuild: 'synchronous' });

              await host.call({
                kind: 'execute',
                batch: [{
                  commandText: 'CREATE TABLE t (i INTEGER, b BLOB)',
                  resultKind: 'nonQuery',
                  parameters: [],
                }],
              });

              await host.call({
                kind: 'execute',
                batch: [{
                  commandText: 'INSERT INTO t (i, b) VALUES (@i, @b)',
                  resultKind: 'nonQuery',
                  parameters: [
                    { name: '@i', ...encodeValue(9007199254740993n) },
                    { name: '@b', ...encodeValue(new Uint8Array([1, 2, 250])) },
                  ],
                }],
              });

              return await host.call({
                kind: 'execute',
                batch: [{ commandText: 'SELECT i, b FROM t', resultKind: 'reader', parameters: [] }],
              });
            }
            """);

        Assert.IsTrue(envelope.GetProperty("ok").GetBoolean());
        Assert.AreEqual(1, envelope.GetProperty("result").GetArrayLength());

        var row = envelope.GetProperty("result")[0].GetProperty("rows")[0];
        AssertJson.AreEqual("[1, 4]", row.GetProperty("t"));
        // A number would have already lost the low bits; a string is the whole point of the format.
        Assert.AreEqual("9007199254740993", row.GetProperty("v")[0].GetString());
        Assert.AreEqual(Convert.ToBase64String([1, 2, 250]), row.GetProperty("v")[1].GetString());
    }

    [TestMethod]
    public async Task A_failing_execute_is_an_envelope_with_the_SQLite_result_code_not_a_thrown_error()
    {
        await GotoHostPageAsync(Page);

        var envelope = await Page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const host = globalThis.BitBsqlite.host.createHost();
              await host.call({ kind: 'open', databaseName: 'fail.db', requiredBuild: 'synchronous' });
              await host.call({
                kind: 'execute',
                batch: [{
                  commandText: 'CREATE TABLE t (id INTEGER PRIMARY KEY)',
                  resultKind: 'nonQuery',
                  parameters: [],
                }],
              });
              await host.call({
                kind: 'execute',
                batch: [{
                  commandText: 'INSERT INTO t (id) VALUES (1)',
                  resultKind: 'nonQuery',
                  parameters: [],
                }],
              });

              return await host.call({
                kind: 'execute',
                batch: [{
                  commandText: 'INSERT INTO t (id) VALUES (1)',
                  resultKind: 'nonQuery',
                  parameters: [],
                }],
              });
            }
            """);

        Assert.IsFalse(envelope.GetProperty("ok").GetBoolean());
        Assert.AreEqual(19, envelope.GetProperty("error").GetProperty("sqliteCode").GetInt32());
        Assert.MatchesRegex(new Regex("UNIQUE|constraint", RegexOptions.IgnoreCase), envelope.GetProperty("error").GetProperty("message").GetString()!);
    }
}
