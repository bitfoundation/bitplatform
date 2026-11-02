using Bit.BlazeDb;
using Bit.BlazeDb.Querying;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

/// <summary>The README's quick-start table, as the guide declares it.</summary>
[BlazeDbTable("readme-todos")]
[BlazeDbCompoundIndex("CategoryDone", nameof(Category), nameof(Done))]
public partial class ReadmeTodo
{
    [BlazeDbKey] public Guid Id { get; set; }
    [BlazeDbIndex(Unique = true)] public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Category { get; set; }
    [BlazeDbIndex] public bool Done { get; set; }
    [BlazeDbOrderedIndex] public DateTime CreatedAt { get; set; }
}

/// <summary>
/// The code samples of the README, run as written against the README's own model, so the guide cannot
/// describe an API that does not exist or behaves otherwise. Each test names the section it checks.
/// </summary>
[TestClass]
public class ReadmeExampleTests
{
    private static async Task<(BlazeDbDatabase Db, BlazeDbTable<Guid, ReadmeTodo> Todos)> OpenAsync(IBlazeDbStorage? storage = null)
    {
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage }.AddTable(ReadmeTodo.Table));
        return (db, db.GetTable(ReadmeTodo.Table));
    }

    private static ReadmeTodo Todo(string slug, DateTime createdAt, bool done = false, string? category = null) =>
        new() { Id = Guid.NewGuid(), Slug = slug, Title = slug, CreatedAt = createdAt, Done = done, Category = category };

    [TestMethod]
    public async Task Writing_rows_optimistic_concurrency()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        var id = Guid.NewGuid();
        todos.Insert(new ReadmeTodo { Id = id, Slug = "a", Title = "first" });
        var newTitle = "second";

        var read = todos.Get(id)!;
        var edited = new ReadmeTodo { Id = id, Slug = read.Slug, Title = newTitle, Category = read.Category, Done = read.Done, CreatedAt = read.CreatedAt };
        todos.Update(new ReadmeTodo { Id = id, Slug = "a", Title = "changed meanwhile" });
        Assert.IsFalse(todos.TryUpdate(edited, comparisonValue: read));

        read = todos.Get(id)!;
        edited = new ReadmeTodo { Id = id, Slug = read.Slug, Title = newTitle, Category = read.Category, Done = read.Done, CreatedAt = read.CreatedAt };
        Assert.IsTrue(todos.TryUpdate(edited, comparisonValue: read));
    }

    [TestMethod]
    public async Task Writing_rows_retention_sweep()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        var cutoff = new DateTime(2026, 6, 1);
        todos.InsertMany([Todo("old", cutoff.AddDays(-1)), Todo("edge", cutoff), Todo("new", cutoff.AddDays(1))]);

        todos.DeleteMany(todos.Range(ReadmeTodo.Indexes.CreatedAt, BlazeDbBound<DateTime>.Unbounded, cutoff).Select(t => t.Id));

        CollectionAssert.AreEqual(new[] { "new" }, todos.Scan().Select(t => t.Slug).ToArray());
    }

    [TestMethod]
    public async Task Transactions_run_in_transaction_and_savepoints()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        var first = Todo("first", DateTime.UtcNow);
        var second = Todo("second", DateTime.UtcNow);

        using (var txn = db.BeginTransaction())
        {
            todos.Insert(first);
            var savepoint = txn.CreateSavepoint();
            todos.Insert(second);
            txn.RollbackTo(savepoint);
            txn.Commit();
        }

        CollectionAssert.AreEqual(new[] { "first" }, todos.Scan().Select(t => t.Slug).ToArray());
    }

    [TestMethod]
    public async Task Querying_explain_prints_what_the_guide_shows()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        var from = DateTime.UtcNow;

        var query = BlazeDbQuery<Guid, ReadmeTodo>.From(todos)
            .UseIndex(ReadmeTodo.Indexes.CreatedAt, from, BlazeDbBound<DateTime>.Unbounded, descending: true)
            .Where(t => !t.Done)
            .Take(20);

        Assert.AreEqual(string.Join(Environment.NewLine,
            "source: ordered index 'CreatedAt' range [from, unbounded) descending",
            "filter: 1 predicate, evaluated on each source row",
            "order: index order, no sort",
            "take: 20"), query.Explain());
    }

    [TestMethod]
    public async Task Reacting_to_changes_live_query()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        todos.Insert(Todo("a", DateTime.UtcNow));

        using var open = todos.Watch(t => t.Lookup(ReadmeTodo.Indexes.Done, false).ToList(), relevantTo: t => !t.Done);
        var renders = 0;
        open.Changed += (_, _) => renders++;

        todos.Insert(Todo("b", DateTime.UtcNow));
        todos.Insert(Todo("c", DateTime.UtcNow, done: true));

        Assert.AreEqual(2, open.Result.Count);
        Assert.AreEqual(1, renders, "The done row does not concern a list of open ones.");
    }

    [TestMethod]
    public async Task Backups_export_import_and_migration()
    {
        var storage = new BlazeDbInMemoryStorage();
        var (db, todos) = await OpenAsync(storage);
        var legacy = new ReadmeTodo { Id = Guid.NewGuid(), Title = "written before slugs", CreatedAt = DateTime.UtcNow };
        await using (db)
        {
            todos.Insert(legacy);
        }

        var options = new BlazeDbDatabaseOptions { Storage = storage }
            .AddTable(ReadmeTodo.Table)
            .AddMigration(1, db =>
            {
                // Version 1 gave every todo a slug; rows written before it have none.
                var todos = db.GetTable(ReadmeTodo.Table);
                todos.UpsertMany(todos.Scan().Where(t => t.Slug == "").Select(t => new ReadmeTodo
                {
                    Id = t.Id, Slug = $"todo-{t.Id:N}", Title = t.Title, Category = t.Category, Done = t.Done, CreatedAt = t.CreatedAt,
                }).ToList());
            });

        using var file = new MemoryStream();
        await using (var migrated = await BlazeDbDatabase.OpenAsync(options))
        {
            Assert.AreEqual(1, migrated.UserVersion);
            Assert.AreEqual($"todo-{legacy.Id:N}", migrated.GetTable(ReadmeTodo.Table).Get(legacy.Id)!.Slug);
            await migrated.ExportAsync(file);
        }

        file.Position = 0;
        var restored = new BlazeDbInMemoryStorage();
        await BlazeDbDatabase.ImportAsync(file, restored);
        await using var reopened = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = restored }.AddTable(ReadmeTodo.Table));
        Assert.AreEqual(1, reopened.UserVersion);
        Assert.AreEqual(1, reopened.GetTable(ReadmeTodo.Table).Count);
        foreach (var issue in reopened.VerifyIntegrity())
        {
            Assert.Fail($"{issue.Table} / {issue.Index ?? "primary key"}: {issue.Description}");
        }
    }
}
