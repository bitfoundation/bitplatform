using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Bsqlite.Tests.E2E.Infrastructure;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>The worker: SQL execution, batching, value round-tripping and request serialization.</summary>
[TestClass]
public sealed class WorkerTests : BsqliteTest
{
    // ---------------------------------------------------------------------------------------------
    // foreign keys
    //
    // SQLite defaults foreign keys off; Microsoft.Data.Sqlite turns them on for every connection. The
    // worker has to do the same, or the same model enforces its relationships on the server and silently
    // does not in the browser - which is the one difference this library exists to not have.
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Foreign_keys_are_on_the_way_Microsoft_Data_Sqlite_leaves_them()
    {
        await OpenWithParentAndChildAsync();

        var result = await QueryAsync(Page, "PRAGMA foreign_keys");
        AssertJson.AreEqual("[[1]]", result.Rows);
    }

    [TestMethod]
    public async Task Foreign_keys_reject_an_orphaned_row_rather_than_storing_it()
    {
        await OpenWithParentAndChildAsync();

        var error = await ExecuteExpectingFailureAsync(Page,
            new WireRequest("INSERT INTO child (id, parent_id) VALUES (1, 999)"));

        Assert.IsNotNull(error);
        Assert.Contains("FOREIGN KEY", error.Message);
        Assert.AreEqual(19, error.SqliteCode);
    }

    [TestMethod]
    public async Task Foreign_keys_cascade_a_delete_to_the_child_rows()
    {
        await OpenWithParentAndChildAsync();

        await ExecAsync(Page, "INSERT INTO parent (id) VALUES (1)");
        await ExecAsync(Page, "INSERT INTO child (id, parent_id) VALUES (1, 1)");

        await ExecAsync(Page, "DELETE FROM parent WHERE id = 1");

        var remaining = await QueryAsync(Page, "SELECT COUNT(*) FROM child");
        AssertJson.AreEqual("[[0]]", remaining.Rows);
    }

    // EF's SQLite migrations rebuild tables with the pragma toggled off and back on, so it has to stay
    // something the application can change.
    [TestMethod]
    public async Task Foreign_keys_can_still_be_turned_off_by_the_application()
    {
        await OpenWithParentAndChildAsync();

        await ExecAsync(Page, "PRAGMA foreign_keys=OFF");

        AssertJson.AreEqual("[[0]]", (await QueryAsync(Page, "PRAGMA foreign_keys")).Rows);
        await ExecAsync(Page, "INSERT INTO child (id, parent_id) VALUES (1, 999)");
    }

    // ---------------------------------------------------------------------------------------------
    // executing SQL
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Executing_SQL_round_trips_a_row_through_parameters()
    {
        await OpenAsync();

        var insert = await ExecAsync(
            Page,
            "INSERT INTO product (name, price) VALUES (@name, @price)",
            new WireParameter("@name", "Widget"), new WireParameter("@price", 9.5));

        Assert.AreEqual(1, insert.RecordsAffected);

        var selected = await QueryAsync(Page, "SELECT id, name, price FROM product");

        CollectionAssert.AreEqual(new[] { "id", "name", "price" }, selected.ColumnNames);
        AssertJson.AreEqual("""[[1, "Widget", 9.5]]""", selected.Rows);
    }

    [TestMethod]
    public async Task Executing_SQL_matches_parameter_names_with_or_without_their_sigil()
    {
        await OpenAsync();

        // ADO.NET callers write both, and Microsoft.Data.Sqlite accepts both.
        await ExecAsync(Page, "INSERT INTO product (name) VALUES (@name)", new WireParameter("name", "Bare"));

        var selected = await QueryAsync(Page, "SELECT name FROM product");

        AssertJson.AreEqual("""[["Bare"]]""", selected.Rows);
    }

    [TestMethod]
    public async Task Executing_SQL_binds_positional_markers_by_position()
    {
        await OpenAsync();

        await ExecAsync(
            Page,
            "INSERT INTO product (name, price) VALUES (?, ?)",
            new WireParameter("?1", "Positional"), new WireParameter("?2", 1.25));

        AssertJson.AreEqual("""[["Positional", 1.25]]""", (await QueryAsync(Page, "SELECT name, price FROM product")).Rows);
    }

    /// <summary>
    /// The failure this prevents is the quiet one: binding a name the statement never asked for would
    /// otherwise leave the real parameter NULL and return wrong rows instead of an error.
    /// </summary>
    [TestMethod]
    public async Task Executing_SQL_refuses_to_run_a_statement_with_a_parameter_left_unbound()
    {
        await OpenAsync();

        var error = await ExecuteExpectingFailureAsync(Page, new WireRequest(
            "INSERT INTO product (name) VALUES (@name)",
            [new WireParameter("@misspelled", "Widget")],
            "nonQuery"));

        Assert.IsNotNull(error);
        Assert.Contains("@name", error.Message);
        Assert.Contains("@misspelled", error.Message);
    }

    [TestMethod]
    public async Task Executing_SQL_reports_storage_classes_per_column()
    {
        await OpenAsync();

        await ExecAsync(
            Page,
            "INSERT INTO product (name, price) VALUES ('Mixed', 3)");

        // The alias avoids NOTHING, which SQLite reserves for `ON CONFLICT DO NOTHING`.
        var selected = await QueryAsync(Page, "SELECT id, name, price, NULL AS absent FROM product");

        // 'price' is declared REAL but was given an integer, and this reports what SQLite stored rather than
        // what the column was declared as - the engine is built without declared types at all.
        CollectionAssert.AreEqual(new[] { "INTEGER", "TEXT", "REAL", null }, selected.ColumnTypes);
    }

    // ---------------------------------------------------------------------------------------------
    // batching
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Batching_runs_a_batch_in_order_and_returns_one_result_per_request()
    {
        await OpenAsync();

        var results = await ExecuteAsync(Page,
            new WireRequest(
                "INSERT INTO product (name) VALUES (@n)",
                [new WireParameter("@n", "First")],
                "nonQuery"),
            new WireRequest(
                "INSERT INTO product (name) VALUES (@n)",
                [new WireParameter("@n", "Second")],
                "nonQuery"),
            new WireRequest("SELECT name FROM product ORDER BY id", ResultKind: "reader"));

        Assert.HasCount(3, results);
        Assert.AreEqual(1, results[0].RecordsAffected);
        Assert.AreEqual(1, results[1].RecordsAffected);
        AssertJson.AreEqual("""[["First"], ["Second"]]""", results[2].Rows);
    }

    /// <summary>
    /// EF Core's insert pattern: the write and the identity read arrive as one command, and the reader has to
    /// see the SELECT's rows while the INSERT's effect is still counted.
    /// </summary>
    [TestMethod]
    public async Task Batching_takes_rows_from_the_first_row_producing_statement_and_still_runs_the_rest()
    {
        await OpenAsync();

        var result = await QueryAsync(
            Page,
            """
            INSERT INTO product (name) VALUES (@n);
                   SELECT id FROM product WHERE rowid = last_insert_rowid();
            """,
            new WireParameter("@n", "Identity"));

        CollectionAssert.AreEqual(new[] { "id" }, result.ColumnNames);
        AssertJson.AreEqual("[[1]]", result.Rows);

        // Exactly one: sqlite3_changes still says 1 after the SELECT, and a count that asked it after every
        // statement billed the row twice.
        Assert.AreEqual(1, result.RecordsAffected);
    }

    [TestMethod]
    public async Task Batching_a_scalar_request_stops_after_the_first_row()
    {
        await OpenAsync();

        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('A'), ('B'), ('C')");

        var result = (await ExecuteAsync(Page, new WireRequest("SELECT name FROM product ORDER BY id", ResultKind: "scalar")))[0];

        AssertJson.AreEqual("""[["A"]]""", result.Rows);
    }

    [TestMethod]
    public async Task Batching_a_failing_request_does_not_leave_the_connection_unusable()
    {
        await OpenAsync();

        var error = await ExecuteExpectingFailureAsync(Page,
            new WireRequest("SELECT * FROM nonexistent", ResultKind: "reader"));

        Assert.IsNotNull(error);
        Assert.IsNotNull(error.SqliteCode);

        // The queue must survive a rejected request; otherwise one bad query poisons the worker.
        AssertJson.AreEqual("[[0]]", (await QueryAsync(Page, "SELECT COUNT(*) FROM product")).Rows);
    }

    // ---------------------------------------------------------------------------------------------
    // value round-tripping
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Value_round_tripping_carries_every_storage_class()
    {
        await OpenAsync();

        await ExecAsync(Page, "CREATE TABLE values_ (i INTEGER, r REAL, t TEXT, b BLOB, n INTEGER)");
        await ExecAsync(Page, "INSERT INTO values_ VALUES (@i, @r, @t, @b, @n)",
            new WireParameter("@i", 42),
            new WireParameter("@r", 1.5),
            new WireParameter("@t", "text"),
            WireParameter.Blob("@b", 1, 2, 250),
            new WireParameter("@n", null));

        var result = await QueryAsync(Page, "SELECT i, r, t, b, n FROM values_");

        var row = result.Rows[0];
        Assert.AreEqual(42, row[0].GetInt32());
        Assert.AreEqual(1.5, row[1].GetDouble());
        Assert.AreEqual("text", row[2].GetString());
        AssertJson.AreEqual("""{ "blob": [1, 2, 250] }""", row[3]);
        Assert.AreEqual(JsonValueKind.Null, row[4].ValueKind);
    }

    /// <summary>
    /// JSON has no spelling for Infinity - JSON.stringify writes null - and the .NET transport reads the rows
    /// through JSON. So it travels as a string, and NaN, which SQLite stores as NULL, comes back as one.
    /// </summary>
    [TestMethod]
    public async Task Value_round_tripping_carries_an_infinite_REAL_and_NaN_as_the_NULL_SQLite_makes_of_it()
    {
        await OpenAsync();

        await ExecAsync(Page, "CREATE TABLE reals (r REAL)");
        await ExecAsync(Page, "INSERT INTO reals VALUES (@a), (@b), (@c)",
            new WireParameter("@a", double.PositiveInfinity),
            new WireParameter("@b", double.NegativeInfinity),
            new WireParameter("@c", double.NaN));

        var result = await QueryAsync(Page, "SELECT r FROM reals");

        // The decoded values are ±Infinity, which ExecuteAsync re-tags for the trip out of the page.
        AssertJson.AreEqual("""[[{ "real": "Infinity" }], [{ "real": "-Infinity" }], [null]]""", result.Rows);

        // What actually crossed the wire: a string, not the null JSON would have made of it.
        var raw = await Page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const [first] = await globalThis.host.execute([{ commandText: 'SELECT r FROM reals', resultKind: 'reader' }]);
              return first.rows.map(row => row.v[0]);
            }
            """);
        AssertJson.AreEqual("""["Infinity", "-Infinity", null]""", raw);
    }

    /// <summary>
    /// Integers beyond 2^53 cannot be represented exactly as a JavaScript number, and SQLite's INTEGER is
    /// 64-bit. The engine hands these back as BigInt, which the .NET transport must be prepared for - JSON
    /// interop cannot carry it.
    /// </summary>
    [TestMethod]
    public async Task Value_round_tripping_returns_a_big_integer_as_a_BigInt_rather_than_losing_precision()
    {
        await OpenAsync();

        await ExecAsync(Page, "CREATE TABLE big (v INTEGER)");
        await ExecAsync(Page, "INSERT INTO big (v) VALUES (9007199254740993)");

        var result = await QueryAsync(Page, "SELECT v FROM big");

        AssertJson.AreEqual("""[[{ "bigint": "9007199254740993" }]]""", result.Rows);
    }

    /// <summary>The same boundary on the way in: a 64-bit parameter must bind as int64, not as a rounded double.</summary>
    [TestMethod]
    public async Task Value_round_tripping_accepts_a_big_integer_as_a_parameter_without_rounding_it()
    {
        await OpenAsync();

        await ExecAsync(Page, "CREATE TABLE big (v INTEGER)");
        await ExecAsync(Page, "INSERT INTO big (v) VALUES (@v)", WireParameter.BigInt("@v", "9223372036854775807"));

        var result = await QueryAsync(Page, "SELECT v FROM big");

        AssertJson.AreEqual("""[[{ "bigint": "9223372036854775807" }]]""", result.Rows);
    }

    [TestMethod]
    public async Task Value_round_tripping_round_trips_a_blob_too_large_for_a_single_fromCharCode_call()
    {
        await OpenAsync();

        await ExecAsync(Page, "CREATE TABLE blobs (b BLOB)");

        // Past the 0x8000 chunk boundary in the base64 encoder, where an unchunked implementation throws.
        var bytes = Enumerable.Range(0, 70_000).Select(i => (byte)(i % 251)).ToArray();
        await ExecAsync(Page, "INSERT INTO blobs (b) VALUES (@b)", WireParameter.Blob("@b", bytes));

        var result = await QueryAsync(Page, "SELECT b, length(b) AS size FROM blobs");

        Assert.AreEqual(70_000, result.Rows[0][1].GetInt32());
        CollectionAssert.AreEqual(bytes, result.Rows[0][0].GetProperty("blob").EnumerateArray().Select(b => b.GetByte()).ToArray());
    }

    // ---------------------------------------------------------------------------------------------
    // request serialization
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// One SQLite connection cannot execute two statements at once. Requests issued without awaiting must
    /// therefore queue, and the observable proof is that a counter incremented by overlapping batches lands
    /// on the exact total.
    /// </summary>
    [TestMethod]
    public async Task Request_serialization_overlapping_batches_queue_instead_of_interleaving()
    {
        await OpenAsync();

        await ExecAsync(Page, "CREATE TABLE counter (n INTEGER)");
        await ExecAsync(Page, "INSERT INTO counter (n) VALUES (0)");

        await Page.EvaluateAsync(
            """
            async () => {
              const bump = () => globalThis.host.execute([{
                commandText: 'UPDATE counter SET n = n + 1',
                resultKind: 'nonQuery',
              }]);

              await Promise.all(Array.from({ length: 25 }, bump));
            }
            """);

        AssertJson.AreEqual("[[25]]", (await QueryAsync(Page, "SELECT n FROM counter")).Rows);
    }

    /// <summary>
    /// A worker whose script never loaded has nobody listening. The first request fails through the error
    /// event; the ones after it must fail the same way rather than wait for an answer that is never coming.
    /// </summary>
    [TestMethod]
    public async Task Request_serialization_a_worker_that_failed_to_load_rejects_every_request_not_just_the_first()
    {
        await OpenAsync();

        await GotoHostPageAsync(Page);

        var outcomes = await Page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const host = globalThis.BitBsqlite.host.createHost('/_content/Bit.Bsqlite/no-such-worker.js');
              const attempt = async () => {
                try {
                  await host.version();
                  return 'resolved';
                } catch (error) {
                  return error.message;
                }
              };

              const first = await attempt();
              const second = await attempt();
              host.dispose();
              return { first, second };
            }
            """);

        Assert.MatchesRegex(new Regex("SQLite worker failed"), outcomes.GetProperty("first").GetString()!);
        Assert.MatchesRegex(new Regex("SQLite worker failed"), outcomes.GetProperty("second").GetString()!);
    }

    [TestMethod]
    public async Task Request_serialization_disposing_rejects_requests_still_in_flight()
    {
        await OpenAsync();

        var outcome = await Page.EvaluateAsync<string>(
            """
            async () => {
              const pending = globalThis.host.execute([
                { commandText: 'SELECT 1', resultKind: 'scalar' },
              ]);

              globalThis.host.dispose();

              try {
                await pending;
                return 'resolved';
              } catch (error) {
                return error.message;
              }
            }
            """);

        Assert.Contains("disposed", outcome);
    }

    // The file's beforeEach: every test starts from an open database with a product table.
    private async Task OpenAsync()
    {
        await OpenHostAsync(Page);
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT, price REAL)");
    }

    // The foreign keys describe block's beforeEach, after the file's.
    private async Task OpenWithParentAndChildAsync()
    {
        await OpenAsync();
        await ExecAsync(Page, "CREATE TABLE parent (id INTEGER PRIMARY KEY)");
        await ExecAsync(Page,
            "CREATE TABLE child (id INTEGER PRIMARY KEY, parent_id INTEGER REFERENCES parent(id) ON DELETE CASCADE)");
    }
}
