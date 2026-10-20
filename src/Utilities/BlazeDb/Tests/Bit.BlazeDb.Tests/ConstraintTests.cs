using Bit.BlazeDb;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

[TestClass]
public class ConstraintTests
{
    private static async Task<(BlazeDbDatabase Db, BlazeDbTable<int, Account> Accounts)> OpenAsync()
    {
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(Account.Table));
        return (db, db.GetTable(Account.Table));
    }

    // Email defaults to something per-row unique so tests that aren't about the compound
    // TenantEmail constraint don't trip over it.
    private static Account Make(int id, string username, int tenant = 1, string? email = null, int? slot = null) =>
        new()
        {
            Id = id,
            Username = username,
            TenantId = tenant,
            Email = email ?? $"user{id}@b.c",
            Slot = slot,
            CreatedAt = new DateTime(2026, 1, id, 0, 0, 0, DateTimeKind.Utc),
        };

    [TestMethod]
    public async Task Unique_Hash_Index_Rejects_Duplicate()
    {
        var (db, accounts) = await OpenAsync();
        await using var _ = db;
        accounts.Insert(Make(1, "ada"));

        var ex = Assert.ThrowsExactly<BlazeDbUniqueConstraintViolationException>(() => accounts.Insert(Make(2, "ada")));

        Assert.AreEqual("Username", ex.IndexName);
        Assert.AreEqual("accounts", ex.TableName);
    }

    [TestMethod]
    public async Task Rejected_Write_Leaves_Table_Untouched()
    {
        var (db, accounts) = await OpenAsync();
        await using var owned = db;
        accounts.Insert(Make(1, "ada"));

        Assert.ThrowsExactly<BlazeDbUniqueConstraintViolationException>(() => accounts.Insert(Make(2, "ada")));

        Assert.AreEqual(1, accounts.Count);
        Assert.IsFalse(accounts.TryGet(2, out _));
        Assert.ContainsSingle(accounts.Lookup(Account.Indexes.Username, "ada"));
    }

    [TestMethod]
    public async Task Updating_A_Row_Does_Not_Conflict_With_Itself()
    {
        var (db, accounts) = await OpenAsync();
        await using var _ = db;
        accounts.Insert(Make(1, "ada"));

        accounts.Update(Make(1, "ada", email: "new@b.c"));

        Assert.AreEqual("new@b.c", accounts.Get(1)!.Email);
    }

    [TestMethod]
    public async Task Unique_Constraint_Frees_Value_After_Delete()
    {
        var (db, accounts) = await OpenAsync();
        await using var _ = db;
        accounts.Insert(Make(1, "ada"));
        accounts.Delete(1);

        accounts.Insert(Make(2, "ada"));

        Assert.AreEqual(2, accounts.Lookup(Account.Indexes.Username, "ada").Single().Id);
    }

    [TestMethod]
    public async Task Unique_Ordered_Index_Rejects_Duplicates_But_Allows_Nulls()
    {
        var (db, accounts) = await OpenAsync();
        await using var _ = db;
        accounts.Insert(Make(1, "ada", slot: 7));
        accounts.Insert(Make(2, "bob"));
        accounts.Insert(Make(3, "cleo"));

        Assert.ThrowsExactly<BlazeDbUniqueConstraintViolationException>(() => accounts.Insert(Make(4, "dan", slot: 7)));
        Assert.AreEqual(3, accounts.Count);
    }

    [TestMethod]
    public async Task Unique_Violation_Inside_Transaction_Can_Be_Rolled_Back()
    {
        var (db, accounts) = await OpenAsync();
        await using var _ = db;
        accounts.Insert(Make(1, "ada"));

        using (var tx = db.BeginTransaction())
        {
            accounts.Insert(Make(2, "bob"));
            Assert.ThrowsExactly<BlazeDbUniqueConstraintViolationException>(() => accounts.Insert(Make(3, "ada")));
            tx.Rollback();
        }

        Assert.AreEqual(1, accounts.Count);
    }

    [TestMethod]
    public async Task Compound_Hash_Index_Looks_Up_By_Tuple()
    {
        var (db, accounts) = await OpenAsync();
        await using var _ = db;
        accounts.Insert(Make(1, "ada", tenant: 1, email: "x@b.c"));
        accounts.Insert(Make(2, "bob", tenant: 2, email: "x@b.c"));
        accounts.Insert(Make(3, "cleo", tenant: 1, email: "y@b.c"));

        var hit = accounts.Lookup(Account.Indexes.TenantEmail, (1, "x@b.c")).Single();

        Assert.AreEqual(1, hit.Id);
        Assert.AreEqual(1, accounts.CountBy(Account.Indexes.TenantEmail, (2, "x@b.c")));
        Assert.AreEqual(0, accounts.CountBy(Account.Indexes.TenantEmail, (2, "y@b.c")));
    }

    [TestMethod]
    public async Task Compound_Unique_Index_Rejects_Only_Duplicate_Tuples()
    {
        var (db, accounts) = await OpenAsync();
        await using var _ = db;
        accounts.Insert(Make(1, "ada", tenant: 1, email: "x@b.c"));

        // The same email under a different tenant is fine: the constraint spans both members.
        accounts.Insert(Make(2, "bob", tenant: 2, email: "x@b.c"));

        Assert.ThrowsExactly<BlazeDbUniqueConstraintViolationException>(
            () => accounts.Insert(Make(3, "cleo", tenant: 1, email: "x@b.c")));
    }

    [TestMethod]
    public async Task Compound_Ordered_Index_Scans_A_Leading_Key_Prefix()
    {
        var (db, accounts) = await OpenAsync();
        await using var _ = db;
        for (var i = 1; i <= 6; i++)
        {
            accounts.Insert(Make(i, "user" + i, tenant: i % 2 == 0 ? 2 : 1, email: "e" + i));
        }

        var tenant1 = accounts.Range(
            Account.Indexes.TenantCreated,
            (1, DateTime.MinValue),
            (1, DateTime.MaxValue)).ToList();

        Assert.AreSequenceEqual([1, 3, 5], tenant1.Select(a => a.Id));
        Assert.IsTrue(tenant1.SequenceEqual(tenant1.OrderBy(a => a.CreatedAt)));
    }

    [TestMethod]
    public async Task Ordered_String_Index_Sorts_Ordinally_Not_By_Culture()
    {
        // The current culture would put "apple" before "Banana"; ordinal goes by code unit, which
        // is what makes a range answer the same question on every machine that opens the database
        // and is how the hash index and the primary-key dictionary already compare strings.
        var (db, accounts) = await OpenAsync();
        await using var _ = db;
        accounts.Insert(Make(1, "a", email: "apple"));
        accounts.Insert(Make(2, "b", email: "Banana"));
        accounts.Insert(Make(3, "c", email: "Cherry"));

        Assert.AreSequenceEqual(
            ["Banana", "Cherry", "apple"],
            accounts.OrderBy(Account.Indexes.Email).Select(a => a.Email));
        Assert.AreSequenceEqual(
            ["Banana", "Cherry"],
            accounts.Range(Account.Indexes.Email, "B", "D").Select(a => a.Email));
    }

    [TestMethod]
    public async Task Compound_Ordered_Index_Sorts_Its_String_Member_Ordinally()
    {
        // A value tuple compares its members with Comparer<T>.Default, so the ordinal default has
        // to reach inside the tuple too, or the two index kinds would disagree on string order.
        var (db, accounts) = await OpenAsync();
        await using var _ = db;
        accounts.Insert(Make(1, "apple", tenant: 1));
        accounts.Insert(Make(2, "Banana", tenant: 1));
        accounts.Insert(Make(3, "zebra", tenant: 2));

        Assert.AreSequenceEqual(
            ["Banana", "apple"],
            accounts.Range(Account.Indexes.TenantUsername, (1, ""), (1, "\uFFFF")).Select(a => a.Username));
    }

    [TestMethod]
    public async Task Compound_Indexes_Track_Updates_And_Deletes()
    {
        var (db, accounts) = await OpenAsync();
        await using var _ = db;
        accounts.Insert(Make(1, "ada", tenant: 1, email: "x@b.c"));
        accounts.Upsert(Make(1, "ada", tenant: 9, email: "x@b.c"));

        Assert.AreEqual(0, accounts.CountBy(Account.Indexes.TenantEmail, (1, "x@b.c")));
        Assert.AreEqual(1, accounts.CountBy(Account.Indexes.TenantEmail, (9, "x@b.c")));

        accounts.Delete(1);
        Assert.AreEqual(0, accounts.CountBy(Account.Indexes.TenantEmail, (9, "x@b.c")));
    }

    [TestMethod]
    public async Task Unique_Constraints_Are_Reenforced_After_Reload()
    {
        var storage = new BlazeDbInMemoryStorage();
        var options = new BlazeDbDatabaseOptions { FlushInterval = TimeSpan.FromHours(1), Storage = storage }
            .AddTable(Account.Table);

        var db = await BlazeDbDatabase.OpenAsync(options);
        db.GetTable(Account.Table).Insert(Make(1, "ada"));
        await db.FlushAsync();
        await db.DisposeAsync();

        var reopened = await BlazeDbDatabase.OpenAsync(
            new BlazeDbDatabaseOptions { FlushInterval = TimeSpan.FromHours(1), Storage = storage }.AddTable(Account.Table));
        await using var _ = reopened;
        var accounts = reopened.GetTable(Account.Table);

        Assert.AreEqual(1, accounts.Count);
        Assert.ThrowsExactly<BlazeDbUniqueConstraintViolationException>(() => accounts.Insert(Make(2, "ada")));
    }
}
