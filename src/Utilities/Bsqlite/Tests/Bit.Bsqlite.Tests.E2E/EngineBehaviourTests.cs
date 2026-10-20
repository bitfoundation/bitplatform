using System.Text.Json;
using Bit.Bsqlite.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>
/// Behaviours of the worker's engine loop that the .NET layer relies on: the statement cache, the
/// engine-reported change notifications, accurate affected-row counts, open-time configuration, extension
/// modules, and in-place snapshots.
/// </summary>
[TestClass]
public sealed class EngineBehaviourTests : BsqliteTest
{
    // ---------------------------------------------------------------------------------------------
    // statement cache
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Statement_cache_a_repeated_statement_runs_with_fresh_parameters_each_time()
    {
        await OpenForStatementCacheAsync();

        foreach (var name in new[] { "A", "B", "C" })
        {
            await ExecAsync(Page, "INSERT INTO product (name) VALUES (@name)", new WireParameter("@name", name));
        }

        var rows = (await QueryAsync(Page, "SELECT name FROM product ORDER BY id")).Rows;
        AssertJson.AreEqual("""[["A"], ["B"], ["C"]]""", rows);

        // The same SELECT text, now with a parameter that changed and then a parameter that was cleared.
        var one = await QueryAsync(Page, "SELECT name FROM product WHERE name = @n", new WireParameter("@n", "B"));
        AssertJson.AreEqual("""[["B"]]""", one.Rows);
        var none = await QueryAsync(Page, "SELECT name FROM product WHERE name = @n", new WireParameter("@n", "Z"));
        AssertJson.AreEqual("[]", none.Rows);
    }

    [TestMethod]
    public async Task Statement_cache_a_cached_statement_survives_a_schema_change()
    {
        await OpenForStatementCacheAsync();

        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('A')");
        CollectionAssert.AreEqual(new[] { "id", "name", "price" }, (await QueryAsync(Page, "SELECT * FROM product")).ColumnNames);

        await ExecAsync(Page, "ALTER TABLE product ADD COLUMN sku TEXT");

        CollectionAssert.AreEqual(new[] { "id", "name", "price", "sku" }, (await QueryAsync(Page, "SELECT * FROM product")).ColumnNames);
    }

    [TestMethod]
    public async Task Statement_cache_a_failing_statement_does_not_poison_its_cached_handle()
    {
        await OpenForStatementCacheAsync();

        await ExecAsync(Page, "CREATE UNIQUE INDEX ux ON product (name)");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('dup')");

        var error = await ExecuteExpectingFailureAsync(Page,
            new WireRequest("INSERT INTO product (name) VALUES (@n)", [new WireParameter("@n", "dup")]));
        Assert.IsNotNull(error);
        Assert.AreEqual(19, error.SqliteCode);

        // Same text again, now with a value that works: the reset in between must have cleared the error.
        var ok = await ExecAsync(Page, "INSERT INTO product (name) VALUES (@n)", new WireParameter("@n", "fresh"));
        Assert.AreEqual(1, ok.RecordsAffected);
    }

    [TestMethod]
    public async Task Statement_cache_many_distinct_statements_do_not_exhaust_the_cache()
    {
        await OpenForStatementCacheAsync();

        for (var i = 0; i < 200; i++)
        {
            await ExecAsync(Page, $"INSERT INTO product (name) VALUES ('n{i}') -- {i}");
        }

        AssertJson.AreEqual("[[200]]", (await QueryAsync(Page, "SELECT COUNT(*) FROM product")).Rows);
    }

    // ---------------------------------------------------------------------------------------------
    // engine-reported changes
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Engine_reported_changes_a_write_names_the_tables_it_changed_including_through_a_cascade()
    {
        await OpenForChangesAsync();

        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('A')");
        await ExecAsync(Page, "INSERT INTO line (product_id) VALUES (1)");
        await NotificationsAsync(Page);

        await ExecAsync(Page, "DELETE FROM product WHERE id = 1");

        AssertJson.AreEqual("""[["line", "product"]]""", await NotifiedAsync(Page));
    }

    [TestMethod]
    public async Task Engine_reported_changes_a_read_and_a_write_that_changed_nothing_report_no_tables()
    {
        await OpenForChangesAsync();

        await QueryAsync(Page, "SELECT * FROM product");
        await ExecAsync(Page, "DELETE FROM product WHERE id = 42");

        AssertJson.AreEqual("[]", await NotifiedAsync(Page));
    }

    [TestMethod]
    public async Task Engine_reported_changes_a_truncating_DELETE_which_the_update_hook_does_not_see_is_still_reported()
    {
        await OpenForChangesAsync();

        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('A')");
        await NotificationsAsync(Page);

        await ExecAsync(Page, "DELETE FROM line");
        await ExecAsync(Page, "DELETE FROM product");

        AssertJson.AreEqual("""[["product"]]""", await NotifiedAsync(Page));
    }

    [TestMethod]
    public async Task Engine_reported_changes_DDL_names_the_table_it_touched()
    {
        await OpenForChangesAsync();

        await ExecAsync(Page, "CREATE TABLE extra (id INTEGER PRIMARY KEY)");

        AssertJson.AreEqual("""[["extra"]]""", await NotifiedAsync(Page));
    }

    [TestMethod]
    public async Task Engine_reported_changes_nothing_is_reported_until_the_transaction_commits_and_nothing_at_all_on_rollback()
    {
        await OpenForChangesAsync();

        await ExecAsync(Page, "BEGIN");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('A')");
        AssertJson.AreEqual("[]", await NotifiedAsync(Page));
        await ExecAsync(Page, "COMMIT");
        AssertJson.AreEqual("""[["product"]]""", await NotifiedAsync(Page));

        await ExecAsync(Page, "BEGIN");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('B')");
        await ExecAsync(Page, "ROLLBACK");
        AssertJson.AreEqual("[]", await NotifiedAsync(Page));
    }

    [TestMethod]
    public async Task Engine_reported_changes_the_cascade_also_reaches_the_notification_channel()
    {
        await OpenForChangesAsync();

        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('A')");
        await ExecAsync(Page, "INSERT INTO line (product_id) VALUES (1)");

        var tables = await Page.EvaluateAsync<string[]>(
            """
            async () => {
              const seen = new Promise(resolve => {
                globalThis.host.onNotify(payload => resolve(payload.tables));
              });
              await globalThis.host.execute([{ commandText: 'DELETE FROM product WHERE id = 1', resultKind: 'nonQuery' }]);
              return await seen;
            }
            """);

        Array.Sort(tables, StringComparer.Ordinal);
        CollectionAssert.AreEqual(new[] { "line", "product" }, tables);
    }

    // ---------------------------------------------------------------------------------------------
    // counters
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Counters_a_SELECT_after_an_INSERT_does_not_count_the_insert_twice()
    {
        await OpenForCountersAsync();

        var result = await QueryAsync(
            Page,
            "INSERT INTO product (name) VALUES ('x'); SELECT id FROM product WHERE rowid = last_insert_rowid();");

        Assert.AreEqual(1, result.RecordsAffected);
        AssertJson.AreEqual("[[1]]", result.Rows);
    }

    [TestMethod]
    public async Task Counters_a_read_only_batch_reports_zero_affected_rows()
    {
        await OpenForCountersAsync();

        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('x')");

        Assert.AreEqual(0, (await QueryAsync(Page, "SELECT * FROM product")).RecordsAffected);
    }

    [TestMethod]
    public async Task Counters_last_insert_rowid_is_carried_on_the_result()
    {
        await OpenForCountersAsync();

        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('a')");
        var second = await ExecAsync(Page, "INSERT INTO product (name) VALUES ('b')");

        Assert.AreEqual(2L, second.LastInsertRowId);
    }

    [TestMethod]
    public async Task Counters_a_row_limit_stops_the_reader_early()
    {
        await OpenForCountersAsync();

        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('a'), ('b'), ('c')");

        var limited = (await ExecuteAsync(Page, new WireRequest("SELECT name FROM product ORDER BY id", ResultKind: "reader", MaxRows: 2)))[0];
        AssertJson.AreEqual("""[["a"], ["b"]]""", limited.Rows);

        var schemaOnly = (await ExecuteAsync(Page, new WireRequest("SELECT id, name FROM product", ResultKind: "reader", MaxRows: 0)))[0];
        CollectionAssert.AreEqual(new[] { "id", "name" }, schemaOnly.ColumnNames);
        AssertJson.AreEqual("[]", schemaOnly.Rows);
    }

    // ---------------------------------------------------------------------------------------------
    // open-time configuration
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Open_time_configuration_foreign_keys_are_enforced_by_default()
    {
        await OpenHostAsync(Page, new HostOptions { DatabaseName = $"fk-{Now()}" });
        await ExecAsync(Page, "CREATE TABLE parent (id INTEGER PRIMARY KEY)");
        await ExecAsync(Page, "CREATE TABLE child (id INTEGER PRIMARY KEY, parent_id INTEGER REFERENCES parent(id))");

        var error = await ExecuteExpectingFailureAsync(Page, new WireRequest("INSERT INTO child (parent_id) VALUES (99)"));

        Assert.IsNotNull(error);
        Assert.AreEqual(19, error.SqliteCode);
        AssertJson.AreEqual("[[1]]", (await QueryAsync(Page, "PRAGMA foreign_keys")).Rows);
    }

    [TestMethod]
    public async Task Open_time_configuration_foreign_keys_stay_off_when_the_caller_opts_out()
    {
        await OpenHostAsync(Page, new HostOptions { DatabaseName = $"nofk-{Now()}", ForeignKeys = false });

        AssertJson.AreEqual("[[0]]", (await QueryAsync(Page, "PRAGMA foreign_keys")).Rows);
    }

    [TestMethod]
    public async Task Open_time_configuration_open_time_statements_run_after_the_defaults()
    {
        await OpenHostAsync(Page, new HostOptions { DatabaseName = $"onopen-{Now()}", OnOpenSql = ["PRAGMA foreign_keys = OFF"] });

        AssertJson.AreEqual("[[0]]", (await QueryAsync(Page, "PRAGMA foreign_keys")).Rows);
    }

    [TestMethod]
    public async Task Open_time_configuration_an_extension_module_can_add_a_function()
    {
        await OpenHostAsync(Page, new HostOptions
        {
            DatabaseName = $"ext-{Now()}",
            Extensions = [new JsModule("/extension.js", "register")],
        });

        AssertJson.AreEqual("""[["HI!"]]""", (await QueryAsync(Page, "SELECT shout('hi')")).Rows);
    }

    // ---------------------------------------------------------------------------------------------
    // snapshots
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Snapshots_export_returns_a_SQLite_image_of_the_open_database()
    {
        await OpenHostAsync(Page, new HostOptions { DatabaseName = $"export-{Now()}" });
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('kept')");

        var header = await Page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const image = await globalThis.host.export();
              return { length: image.byteLength, magic: new TextDecoder().decode(image.subarray(0, 15)) };
            }
            """);

        Assert.AreEqual("SQLite format 3", header.GetProperty("magic").GetString());
        Assert.IsGreaterThanOrEqualTo(4096, header.GetProperty("length").GetInt32());
    }

    // ---------------------------------------------------------------------------------------------
    // The describe blocks' beforeEach hooks
    // ---------------------------------------------------------------------------------------------

    private async Task OpenForStatementCacheAsync()
    {
        await OpenHostAsync(Page, new HostOptions { DatabaseName = $"cache-{Now()}" });
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT, price REAL)");
    }

    private async Task OpenForChangesAsync()
    {
        await OpenHostAsync(Page, new HostOptions { DatabaseName = $"changes-{Now()}" });
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecAsync(Page, "CREATE TABLE line (id INTEGER PRIMARY KEY, product_id INTEGER REFERENCES product(id) ON DELETE CASCADE)");
        await RecordNotificationsAsync(Page);
    }

    private async Task OpenForCountersAsync()
    {
        await OpenHostAsync(Page, new HostOptions { DatabaseName = $"counters-{Now()}" });
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
    }

    /// <summary>The notifications recorded since the last call, as JSON for <see cref="AssertJson"/>.</summary>
    private static async Task<JsonElement> NotifiedAsync(IPage page) => JsonSerializer.SerializeToElement(await NotificationsAsync(page));

    /// <summary><c>Date.now()</c>, which the spec put in its database names.</summary>
    private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
