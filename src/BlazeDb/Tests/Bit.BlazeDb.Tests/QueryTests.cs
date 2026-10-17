using Bit.BlazeDb;
using Bit.BlazeDb.Querying;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

[TestClass]
public class QueryTests
{
    private static async Task<(BlazeDbDatabase Db, BlazeDbTable<Guid, TodoItem> Todos)> OpenAsync(IBlazeDbStorage? storage = null)
    {
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(TodoItem.Table));
        return (db, db.GetTable(TodoItem.Table));
    }

    private static TodoItem Make(string title, bool done = false, string? category = null, int day = 1) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Done = done,
        Category = category,
        CreatedAt = new DateTime(2026, 1, day, 0, 0, 0, DateTimeKind.Utc),
    };

    [TestMethod]
    public async Task Hash_Index_Lookup_Returns_Matching_Rows()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        todos.Insert(Make("a", done: true));
        todos.Insert(Make("b", done: false));
        todos.Insert(Make("c", done: true));

        var doneTitles = todos.Lookup(TodoItem.Indexes.Done, true).Select(t => t.Title).Order().ToList();

        Assert.AreSequenceEqual(["a", "c"], doneTitles);
        Assert.AreEqual(2, todos.CountBy(TodoItem.Indexes.Done, true));
        Assert.AreEqual(1, todos.CountBy(TodoItem.Indexes.Done, false));
    }

    [TestMethod]
    public async Task Hash_Index_Follows_Updates_And_Deletes()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        var item = Make("a", done: false);
        todos.Insert(item);

        todos.Update(new TodoItem { Id = item.Id, Title = "a", Done = true, CreatedAt = item.CreatedAt });
        Assert.IsEmpty(todos.Lookup(TodoItem.Indexes.Done, false));
        Assert.ContainsSingle(todos.Lookup(TodoItem.Indexes.Done, true));

        todos.Delete(item.Id);
        Assert.IsEmpty(todos.Lookup(TodoItem.Indexes.Done, true));
    }

    [TestMethod]
    public async Task Null_Index_Values_Are_Looked_Up_Like_Any_Other()
    {
        // A null is recorded in the index as a value of its own: a lookup for null finds those rows,
        // and the entry is there to be retracted when the row changes or goes away.
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        todos.Insert(Make("categorized", category: "work"));
        var uncategorized = Make("uncategorized", category: null);
        todos.Insert(uncategorized);

        Assert.ContainsSingle(todos.Lookup(TodoItem.Indexes.Category, "work"));
        Assert.AreSame(uncategorized, todos.Lookup(TodoItem.Indexes.Category, null).Single());
        Assert.AreEqual(1, todos.CountBy(TodoItem.Indexes.Category, null));

        todos.Delete(uncategorized.Id);
        Assert.IsEmpty(todos.Lookup(TodoItem.Indexes.Category, null));
    }

    [TestMethod]
    public async Task Ordering_By_An_Index_Includes_Rows_Whose_Value_Is_Null()
    {
        // Sorting by a value that is sometimes null still returns every row, nulls first - the same
        // as LINQ's OrderBy - so walking the index answers the ordering exactly as sorting would.
        // A bounded range, on the other hand, excludes them: null is inside no interval.
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(Account.Table));
        await using var _ = db;
        var accounts = db.GetTable(Account.Table);

        Account Make(int id, int? slot) => new()
        {
            Id = id, Username = "u" + id, Email = $"u{id}@x", TenantId = 1, Slot = slot,
        };
        accounts.Insert(Make(1, 5));
        accounts.Insert(Make(2, null));
        accounts.Insert(Make(3, 2));

        Assert.AreSequenceEqual([2, 3, 1], accounts.OrderBy(Account.Indexes.Slot).Select(a => a.Id));
        Assert.AreSequenceEqual([1, 3, 2], accounts.OrderBy(Account.Indexes.Slot, descending: true).Select(a => a.Id));
        Assert.AreSequenceEqual(
            [3, 1],
            accounts.Range(Account.Indexes.Slot, BlazeDbBound<int?>.Unbounded, BlazeDbBound<int?>.At(10)).Select(a => a.Id));
    }

    [TestMethod]
    public async Task Rollback_Restores_Index_State()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        var item = Make("a", done: false);
        todos.Insert(item);

        using (db.BeginTransaction())
        {
            todos.Update(new TodoItem { Id = item.Id, Title = "a", Done = true, CreatedAt = item.CreatedAt });
            todos.Insert(Make("b", done: true));
            // Rollback via dispose.
        }

        Assert.IsEmpty(todos.Lookup(TodoItem.Indexes.Done, true));
        Assert.ContainsSingle(todos.Lookup(TodoItem.Indexes.Done, false));
    }

    [TestMethod]
    public async Task Ordered_Index_Scans_In_Both_Directions()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        todos.Insert(Make("third", day: 3));
        todos.Insert(Make("first", day: 1));
        todos.Insert(Make("second", day: 2));

        Assert.AreSequenceEqual(
            ["first", "second", "third"],
            todos.OrderBy(TodoItem.Indexes.CreatedAt).Select(t => t.Title).ToList());
        Assert.AreSequenceEqual(
            ["third", "second", "first"],
            todos.OrderBy(TodoItem.Indexes.CreatedAt, descending: true).Select(t => t.Title).ToList());
    }

    [TestMethod]
    public async Task Ordered_Index_Range_Is_Inclusive_And_Supports_Open_Ends()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        for (var day = 1; day <= 9; day++)
        {
            todos.Insert(Make($"day{day}", day: day));
        }

        DateTime Day(int d) => new(2026, 1, d, 0, 0, 0, DateTimeKind.Utc);

        var middle = todos.Range(TodoItem.Indexes.CreatedAt, Day(3), Day(5)).Select(t => t.Title).ToList();
        Assert.AreSequenceEqual(["day3", "day4", "day5"], middle);

        var fromOnly = todos.Range(TodoItem.Indexes.CreatedAt, Day(8), BlazeDbBound<DateTime>.Unbounded)
            .Select(t => t.Title).ToList();
        Assert.AreSequenceEqual(["day8", "day9"], fromOnly);

        var toOnly = todos.Range(TodoItem.Indexes.CreatedAt, BlazeDbBound<DateTime>.Unbounded, Day(2))
            .Select(t => t.Title).ToList();
        Assert.AreSequenceEqual(["day1", "day2"], toOnly);

        var descending = todos.Range(TodoItem.Indexes.CreatedAt, Day(4), Day(6), descending: true)
            .Select(t => t.Title).ToList();
        Assert.AreSequenceEqual(["day6", "day5", "day4"], descending);

        Assert.IsEmpty(todos.Range(TodoItem.Indexes.CreatedAt, Day(6), Day(3)));
    }

    [TestMethod]
    public async Task Ordered_Index_Handles_Duplicate_Values()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        todos.Insert(Make("a", day: 1));
        todos.Insert(Make("b", day: 1));
        todos.Insert(Make("c", day: 2));

        var sameDay = todos.Range(
                TodoItem.Indexes.CreatedAt,
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .Select(t => t.Title).Order().ToList();

        Assert.AreSequenceEqual(["a", "b"], sameDay);
    }

    [TestMethod]
    public async Task Indexes_Are_Rebuilt_After_Recovery()
    {
        var storage = new BlazeDbInMemoryStorage();
        Guid keepId;

        var (db, todos) = await OpenAsync(storage);
        await using (db)
        {
            var keep = Make("keep", done: true, category: "work", day: 2);
            keepId = keep.Id;
            todos.Insert(keep);
            todos.Insert(Make("other", done: false, day: 5));
            await db.CheckpointAsync();
            todos.Insert(Make("later", done: true, day: 7));
        }

        var (reopened, recovered) = await OpenAsync(storage);
        await using var _ = reopened;
        Assert.AreEqual(2, recovered.CountBy(TodoItem.Indexes.Done, true));
        Assert.AreEqual(keepId, recovered.Lookup(TodoItem.Indexes.Category, "work").Single().Id);
        Assert.AreSequenceEqual(
            ["keep", "other", "later"],
            recovered.OrderBy(TodoItem.Indexes.CreatedAt).Select(t => t.Title).ToList());
    }

    [TestMethod]
    public async Task Query_Combines_Index_Source_Residual_Filter_And_Paging()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        for (var day = 1; day <= 10; day++)
        {
            todos.Insert(Make($"day{day}", done: day % 2 == 0, day: day));
        }

        DateTime Day(int d) => new(2026, 1, d, 0, 0, 0, DateTimeKind.Utc);

        var query = BlazeDbQuery<Guid, TodoItem>.From(todos)
            .UseIndex(TodoItem.Indexes.CreatedAt, Day(2), Day(9))
            .Where(t => t.Done)
            .Skip(1)
            .Take(2);

        Assert.IsTrue(query.IsIndexOrdered);
        Assert.AreSequenceEqual(["day4", "day6"], query.Execute(t => t.Title).ToList());
    }

    [TestMethod]
    public async Task Query_With_Hash_Source_And_Post_Sort()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        todos.Insert(Make("zeta", done: true, day: 1));
        todos.Insert(Make("alpha", done: true, day: 2));
        todos.Insert(Make("mid", done: false, day: 3));

        var titles = BlazeDbQuery<Guid, TodoItem>.From(todos)
            .UseIndex(TodoItem.Indexes.Done, true)
            .OrderBy(t => t.Title)
            .Execute(t => t.Title)
            .ToList();

        Assert.AreSequenceEqual(["alpha", "zeta"], titles);
    }

    [TestMethod]
    public async Task Query_Full_Scan_Fallback_And_Count()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        todos.Insert(Make("a", day: 1));
        todos.Insert(Make("bb", day: 2));
        todos.Insert(Make("ccc", day: 3));

        var count = BlazeDbQuery<Guid, TodoItem>.From(todos)
            .Where(t => t.Title.Length >= 2)
            .Count();

        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public async Task Query_Rejects_Two_Index_Sources()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;

        var query = BlazeDbQuery<Guid, TodoItem>.From(todos).UseIndex(TodoItem.Indexes.Done, true);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            query.UseIndex(TodoItem.Indexes.CreatedAt, BlazeDbBound<DateTime>.Unbounded, BlazeDbBound<DateTime>.Unbounded));
    }

    [TestMethod]
    public async Task Query_Can_Source_A_Single_Row_By_Primary_Key()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        var a = Make("a", done: true);
        var b = Make("b");
        todos.Insert(a);
        todos.Insert(b);

        Assert.AreSame(a, BlazeDbQuery<Guid, TodoItem>.From(todos).UseKey(a.Id).FirstOrDefault());
        Assert.IsEmpty(BlazeDbQuery<Guid, TodoItem>.From(todos).UseKey(Guid.NewGuid()).ToList());
        // Residual filters still apply on top of the key source.
        Assert.IsEmpty(BlazeDbQuery<Guid, TodoItem>.From(todos).UseKey(a.Id).Where(t => !t.Done).ToList());
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            BlazeDbQuery<Guid, TodoItem>.From(todos).UseKey(a.Id).UseIndex(TodoItem.Indexes.Done, true));
    }

    [TestMethod]
    public async Task Query_Can_Source_Several_Rows_By_Primary_Key()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;
        var a = Make("a");
        var b = Make("b");
        var c = Make("c");
        todos.Insert(a);
        todos.Insert(b);
        todos.Insert(c);

        // Order follows the keys; duplicates and misses are skipped.
        var rows = BlazeDbQuery<Guid, TodoItem>.From(todos).UseKeys([c.Id, Guid.NewGuid(), a.Id, c.Id]).ToList();
        Assert.AreSequenceEqual(["c", "a"], rows.Select(t => t.Title));
        Assert.IsEmpty(BlazeDbQuery<Guid, TodoItem>.From(todos).UseKeys([]).ToList());
    }

    [TestMethod]
    public async Task Query_Rejects_Negative_Paging()
    {
        var (db, todos) = await OpenAsync();
        await using var _ = db;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BlazeDbQuery<Guid, TodoItem>.From(todos).Skip(-1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BlazeDbQuery<Guid, TodoItem>.From(todos).Take(-1));
    }


    [TestMethod]
    public async Task A_Boxed_Null_Lookup_Finds_The_Null_Rows_On_Either_Index_Kind()
    {
        // The EF Core provider reaches indexes through the boxed API with a key it only knows at
        // runtime; a null there has to find the rows whose value is null on an ordered index as it
        // does on a hash index, not an empty range between two nulls.
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(Account.Table));
        await using var _ = db;
        var accounts = db.GetTable(Account.Table);
        accounts.Insert(new Account { Id = 1, Username = "u1", Email = "u1@x", TenantId = 1, Slot = 5 });
        accounts.Insert(new Account { Id = 2, Username = "u2", Email = "u2@x", TenantId = 1, Slot = null });

        Assert.AreSequenceEqual([2], BlazeDbQuery<int, Account>.From(accounts).UseIndexBoxed(Account.Indexes.Slot, (object?)null).Execute(a => a.Id));
        Assert.AreSequenceEqual([1], BlazeDbQuery<int, Account>.From(accounts).UseIndexBoxed(Account.Indexes.Slot, 5).Execute(a => a.Id));
        Assert.AreSequenceEqual(
            new[] { 1, 2 },
            BlazeDbQuery<int, Account>.From(accounts).UseIndexBoxed(Account.Indexes.Slot, new object?[] { 5, null }).Execute(a => a.Id).Order());
    }
}
