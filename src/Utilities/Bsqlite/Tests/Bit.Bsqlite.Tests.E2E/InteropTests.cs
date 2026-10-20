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

    /// <summary>
    /// wa-sqlite binds a JavaScript number outside int32 with <c>bind_double</c>. A value tagged as
    /// an integer has to reach the engine as one, or <c>Date.now()</c> is stored as REAL - and in a
    /// TEXT column as '1700000000000.0'.
    /// </summary>
    [TestMethod]
    public async Task An_integer_parameter_outside_int32_is_bound_as_an_integer()
    {
        await GotoHostPageAsync(Page);

        var envelope = await Page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const { createHost, encodeValue } = globalThis.BitBsqlite.host;
              const host = createHost();
              await host.call({ kind: 'open', databaseName: 'int64.db', requiredBuild: 'synchronous' });

              await host.call({
                kind: 'execute',
                batch: [{ commandText: 'CREATE TABLE t (s TEXT)', resultKind: 'nonQuery', parameters: [] }],
              });

              await host.call({
                kind: 'execute',
                batch: [{
                  commandText: 'INSERT INTO t (s) VALUES (@v)',
                  resultKind: 'nonQuery',
                  parameters: [{ name: '@v', ...encodeValue(1700000000000) }],
                }],
              });

              const result = await host.call({
                kind: 'execute',
                batch: [{
                  commandText: 'SELECT typeof(@a), typeof(@b), typeof(@c), (SELECT s FROM t)',
                  resultKind: 'reader',
                  parameters: [
                    { name: '@a', ...encodeValue(3000000000) },
                    { name: '@b', ...encodeValue(-3000000000) },
                    { name: '@c', ...encodeValue(42) },
                  ],
                }],
              });

              host.dispose();
              return result;
            }
            """);

        Assert.IsTrue(envelope.GetProperty("ok").GetBoolean(), envelope.GetRawText());
        var row = envelope.GetProperty("result")[0].GetProperty("rows")[0];
        AssertJson.AreEqual("""{ "t": [3, 3, 3, 3], "v": ["integer", "integer", "integer", "1700000000000"] }""", row);
    }

    /// <summary>
    /// The lock EF migrations run under: shared by every host on the origin - every tab's worker - and
    /// let go of when its holder releases it, withdraws a request it gave up on, or is disposed.
    /// </summary>
    [TestMethod]
    public async Task Hosts_share_named_locks_across_the_origin()
    {
        await GotoHostPageAsync(Page);

        var outcome = await Page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const { createHost } = globalThis.BitBsqlite.host;
              const name = 'bit-bsqlite:test:' + crypto.randomUUID();
              const settle = () => new Promise(resolve => setTimeout(resolve, 100));
              const first = createHost();
              const second = createHost();
              const third = createHost();

              await first.acquireLock(name, 1);
              let secondHeld = false;
              const secondWaits = second.acquireLock(name, 1).then(() => { secondHeld = true; });
              await settle();
              const heldOff = !secondHeld;

              first.releaseLock(1);
              await secondWaits;

              // A request given up on while still queued is withdrawn, not granted later to nobody.
              const withdrawn = first.acquireLock(name, 2).then(() => 'granted', error => error.name);
              first.releaseLock(2);

              // Disposing the holder releases what it held.
              second.dispose();
              await third.acquireLock(name, 1);

              third.releaseLock(1);
              first.dispose();
              third.dispose();
              return { heldOff, secondHeld, withdrawn: await withdrawn };
            }
            """);

        AssertJson.AreEqual("""{ "heldOff": true, "secondHeld": true, "withdrawn": "AbortError" }""", outcome);
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
