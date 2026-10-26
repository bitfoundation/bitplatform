using Bit.BlazeDb;
using Bit.BlazeDb.Querying;

namespace Bit.BlazeDb.Tests;

/// <summary>Exclusive bounds and keyset paging, prefix scans, per-value counts, and query plans.</summary>
[TestClass]
public class IndexQueryTests
{
    private static async Task<(BlazeDbDatabase Db, BlazeDbTable<int, Account> Accounts)> OpenAsync(int rows)
    {
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(Account.Table));
        var accounts = db.GetTable(Account.Table);
        accounts.InsertMany(Enumerable.Range(1, rows).Select(i => new Account
        {
            Id = i,
            Username = $"user{i:D3}",
            TenantId = i % 3,
            Email = $"{(char)('a' + i % 5)}{i}@example.com",
            Slot = i % 7 == 0 ? null : i,
            CreatedAt = new DateTime(2026, 1, 1).AddHours(i / 2), // pairs of rows share a timestamp
        }));
        return (db, accounts);
    }

    [TestMethod]
    [DataRow(false, false, new[] { 3, 4, 5, 6 })]
    [DataRow(true, false, new[] { 4, 5, 6 })]
    [DataRow(false, true, new[] { 3, 4, 5 })]
    [DataRow(true, true, new[] { 4, 5 })]
    public async Task Bounds_Can_Be_Exclusive_At_Either_End(bool fromExclusive, bool toExclusive, int[] expected)
    {
        var (db, accounts) = await OpenAsync(10);
        await using var _ = db;
        var from = fromExclusive ? BlazeDbBound<int?>.Exclusive(3) : BlazeDbBound<int?>.At(3);
        var to = toExclusive ? BlazeDbBound<int?>.Exclusive(6) : BlazeDbBound<int?>.At(6);

        CollectionAssert.AreEqual(expected, accounts.Range(Account.Indexes.Slot, from, to).Select(a => a.Id).ToArray());
        CollectionAssert.AreEqual(Enumerable.Reverse(expected).ToArray(), accounts.Range(Account.Indexes.Slot, from, to, descending: true).Select(a => a.Id).ToArray());
    }

    [TestMethod]
    public async Task An_Exclusive_Bound_Skips_Every_Row_Sharing_The_Value()
    {
        var (db, accounts) = await OpenAsync(10);
        await using var _ = db;
        var oneOClock = new DateTime(2026, 1, 1, 1, 0, 0); // rows 2 and 3

        var createdIndex = new BlazeDbOrderedIndexDefinition<Account, DateTime>("Created", static a => a.CreatedAt);
        var descriptor = new BlazeDbTableDescriptor<int, Account>("created", static a => a.Id,
            Account.Table.RowWriter, Account.Table.RowReader, Account.Table.KeyWriter, Account.Table.KeyReader, [createdIndex]);
        await using var db2 = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(descriptor));
        var table = db2.GetTable(descriptor);
        table.InsertMany(accounts.Scan().Select(a => new Account { Id = a.Id, Username = a.Username, CreatedAt = a.CreatedAt }));

        CollectionAssert.AreEquivalent(new[] { 4, 5, 6, 7, 8, 9, 10 }, table.Range(createdIndex, BlazeDbBound<DateTime>.Exclusive(oneOClock), BlazeDbBound<DateTime>.Unbounded).Select(a => a.Id).ToArray());
        CollectionAssert.AreEquivalent(new[] { 1 }, table.Range(createdIndex, BlazeDbBound<DateTime>.Unbounded, BlazeDbBound<DateTime>.Exclusive(oneOClock)).Select(a => a.Id).ToArray());
        Assert.AreEqual(0, table.Range(createdIndex, BlazeDbBound<DateTime>.Exclusive(oneOClock), BlazeDbBound<DateTime>.Exclusive(oneOClock)).Count());
        Assert.AreEqual(0, table.Range(createdIndex, BlazeDbBound<DateTime>.Exclusive(oneOClock), oneOClock).Count());
        Assert.AreEqual(2, table.Range(createdIndex, oneOClock, oneOClock).Count());
    }

    [TestMethod]
    public async Task Keyset_Paging_Over_A_Compound_Index_Visits_Every_Row_Once()
    {
        // (TenantId, CreatedAt) has ties - two rows per timestamp - so the page boundary has to be a
        // unique position: the compound (TenantId, Username) index provides one.
        var (db, accounts) = await OpenAsync(97);
        await using var _ = db;
        var seen = new List<int>();
        BlazeDbBound<(int, string)> after = BlazeDbBound<(int, string)>.Unbounded;
        while (true)
        {
            var page = accounts.Range(Account.Indexes.TenantUsername, after, BlazeDbBound<(int, string)>.Unbounded).Take(10).ToList();
            if (page.Count == 0)
            {
                break;
            }
            seen.AddRange(page.Select(a => a.Id));
            var last = page[^1];
            after = BlazeDbBound<(int, string)>.Exclusive((last.TenantId, last.Username));
        }

        var expected = accounts.Scan().OrderBy(a => a.TenantId).ThenBy(a => a.Username, StringComparer.Ordinal).Select(a => a.Id).ToList();
        CollectionAssert.AreEqual(expected, seen);
    }

    [TestMethod]
    public async Task StartsWith_Finds_Exactly_The_Strings_With_The_Prefix_Ordinally()
    {
        var (db, accounts) = await OpenAsync(30);
        await using var _ = db;
        accounts.Insert(new Account { Id = 100, Username = "x100", Email = "A1@example.com" });
        accounts.Insert(new Account { Id = 101, Username = "x101", Email = "a￿@example.com" });
        accounts.Insert(new Account { Id = 102, Username = "x102", Email = "b" });

        foreach (var prefix in new[] { "a", "a1", "A", "a￿", "b", "", "zz", "￿" })
        {
            var expected = accounts.Scan().Where(a => a.Email.StartsWith(prefix, StringComparison.Ordinal)).Select(a => a.Email).Order(StringComparer.Ordinal).ToList();
            CollectionAssert.AreEqual(expected, accounts.StartsWith(Account.Indexes.Email, prefix).Select(a => a.Email).ToList(), $"prefix '{prefix}'");
            CollectionAssert.AreEqual(Enumerable.Reverse(expected).ToList(), accounts.StartsWith(Account.Indexes.Email, prefix, descending: true).Select(a => a.Email).ToList(), $"prefix '{prefix}' descending");
        }
    }

    [TestMethod]
    [DataRow("abc", "abd")]
    [DataRow("a￿", "b")]
    [DataRow("a￿￿", "b")]
    [DataRow("￿", null)]
    [DataRow("", null)]
    public void The_Prefix_Upper_Bound_Is_The_Next_String_Without_The_Prefix(string prefix, string? expected) =>
        Assert.AreEqual(expected, BlazeDbTable<int, Account>.PrefixUpperBound(prefix));

    [TestMethod]
    public async Task StartsWith_Refuses_An_Index_With_Its_Own_Comparer()
    {
        var ignoreCase = new BlazeDbOrderedIndexDefinition<Account, string>("EmailIgnoreCase", static a => a.Email, StringComparer.OrdinalIgnoreCase);
        var descriptor = new BlazeDbTableDescriptor<int, Account>("ci", static a => a.Id,
            Account.Table.RowWriter, Account.Table.RowReader, Account.Table.KeyWriter, Account.Table.KeyReader, [ignoreCase]);
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(descriptor));

        Assert.ThrowsExactly<NotSupportedException>(() => db.GetTable(descriptor).StartsWith(ignoreCase, "a").ToList());
    }

    [TestMethod]
    public async Task CountBy_Lists_Every_Value_With_Its_Row_Count_Including_Null()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(TodoItem.Table));
        var todos = db.GetTable(TodoItem.Table);
        string?[] categories = ["work", "home", null, "work", "work", null, "errand"];
        todos.InsertMany(categories.Select(c => new TodoItem { Id = Guid.NewGuid(), Category = c }));

        var counts = todos.CountBy(TodoItem.Indexes.Category).ToDictionary(p => p.Key ?? "<null>", p => p.Value);

        var expected = categories.GroupBy(c => c ?? "<null>").ToDictionary(g => g.Key, g => g.Count());
        CollectionAssert.AreEquivalent(expected, counts);

        todos.DeleteWhere(t => t.Category == "errand");
        Assert.IsFalse(todos.CountBy(TodoItem.Indexes.Category).Any(p => p.Key == "errand"), "An emptied value disappears.");
    }

    [TestMethod]
    public async Task ThenBy_Breaks_Ties_And_Needs_An_OrderBy()
    {
        var (db, accounts) = await OpenAsync(20);
        await using var _ = db;

        var rows = BlazeDbQuery<int, Account>.From(accounts)
            .OrderBy(a => a.TenantId)
            .ThenBy(a => a.Id, descending: true)
            .ToList();

        CollectionAssert.AreEqual(accounts.Scan().OrderBy(a => a.TenantId).ThenByDescending(a => a.Id).Select(a => a.Id).ToList(), rows.Select(a => a.Id).ToList());
        Assert.ThrowsExactly<InvalidOperationException>(() => BlazeDbQuery<int, Account>.From(accounts).ThenBy(a => a.Id));
    }

    [TestMethod]
    public async Task UseIndexPrefix_Streams_The_Prefix_In_Index_Order()
    {
        var (db, accounts) = await OpenAsync(30);
        await using var _ = db;

        var query = BlazeDbQuery<int, Account>.From(accounts).UseIndexPrefix(Account.Indexes.Email, "c").Take(3);

        Assert.IsTrue(query.IsIndexOrdered);
        CollectionAssert.AreEqual(
            accounts.Scan().Select(a => a.Email).Where(e => e.StartsWith('c')).Order(StringComparer.Ordinal).Take(3).ToList(),
            query.ToList().Select(a => a.Email).ToList());
    }

    [TestMethod]
    public async Task Explain_Describes_Source_Filter_Order_And_Paging_Without_Values()
    {
        var (db, accounts) = await OpenAsync(5);
        await using var _ = db;

        var plan = BlazeDbQuery<int, Account>.From(accounts)
            .UseIndex(Account.Indexes.Slot, BlazeDbBound<int?>.Exclusive(1234), BlazeDbBound<int?>.Unbounded, descending: true)
            .Where(a => a.TenantId == 1)
            .Skip(20)
            .Take(10)
            .Explain();

        Assert.AreEqual(string.Join(Environment.NewLine,
            "source: ordered index 'Slot' range (from, unbounded) descending",
            "filter: 1 predicate, evaluated on each source row",
            "order: index order, no sort",
            "skip: 20",
            "take: 10"), plan);
        Assert.IsFalse(plan.Contains("1234"));

        var scan = BlazeDbQuery<int, Account>.From(accounts).OrderBy(a => a.Email).ThenBy(a => a.Id).Explain();
        StringAssert.StartsWith(scan, "source: full scan");
        StringAssert.Contains(scan, "sorted in memory by 2 keys");
        StringAssert.Contains(BlazeDbQuery<int, Account>.From(accounts).UseIndex(Account.Indexes.TenantId, 1).Explain(), "hash index 'TenantId' lookup");
    }
}
