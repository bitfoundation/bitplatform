
namespace Bit.BlazeDb.Tests;

/// <summary>
/// Covers the write path for rows that were mutated through the instance the table holds, rather
/// than replaced with a new one. A change tracker works that way, so the table has to be told what
/// the row used to look like or its index entries would be left pointing at values that no longer
/// exist.
/// </summary>
[TestClass]
public class InPlaceWriteTests
{
    private static async Task<(BlazeDbDatabase Db, BlazeDbTable<int, Account> Accounts)> OpenAsync()
    {
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(Account.Table));
        return (db, db.GetTable(Account.Table));
    }

    private static Account Make(int id, string username = "u", int tenant = 1) => new()
    {
        Id = id,
        Username = username + id,
        TenantId = tenant,
        Email = $"user{id}@example.com",
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    };

    /// <summary>
    /// The values a row has now, as a separate instance - what a change tracker's originals are. It
    /// has to carry every indexed property: the table checks that these are the values its indexes
    /// were built from before it retracts anything.
    /// </summary>
    private static Account Snapshot(Account row) => new()
    {
        Id = row.Id,
        Username = row.Username,
        TenantId = row.TenantId,
        Email = row.Email,
        CreatedAt = row.CreatedAt,
        Slot = row.Slot,
    };

    [TestMethod]
    public async Task An_In_Place_Update_Moves_The_Index_Entry()
    {
        var (db, accounts) = await OpenAsync();
        await using var owned = db;
        accounts.Insert(Make(1, tenant: 1));

        var row = accounts.Get(1)!;
        var before = Snapshot(row);
        row.TenantId = 2;
        accounts.UpdateInPlace(row, before);

        Assert.IsEmpty(accounts.Lookup(Account.Indexes.TenantId, 1));
        Assert.ContainsSingle(accounts.Lookup(Account.Indexes.TenantId, 2));
    }

    [TestMethod]
    public async Task A_Plain_Update_Of_A_Mutated_Instance_Is_Refused_Instead_Of_Stranding_The_Old_Entry()
    {
        var (db, accounts) = await OpenAsync();
        await using var owned = db;
        accounts.Insert(Make(1, tenant: 1));

        // Mutating the stored instance and then calling the ordinary Update would leave the table
        // with no record of the old value, so the tenant 1 entry would survive. Rather than let the
        // index quietly go wrong, the write is refused and points at UpdateInPlace.
        var row = accounts.Get(1)!;
        row.TenantId = 2;
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => accounts.Update(row));
        Assert.Contains("UpdateInPlace", ex.Message);
        Assert.Contains("TenantId", ex.Message);

        // Nothing changed: the table and its index still describe the row as it was written.
        Assert.ContainsSingle(accounts.Lookup(Account.Indexes.TenantId, 1));
        Assert.IsEmpty(accounts.Lookup(Account.Indexes.TenantId, 2));

        // A delete after the same in-place mutation is refused for the same reason...
        Assert.Contains("DeleteInPlace", Assert.ThrowsExactly<InvalidOperationException>(() => accounts.Delete(1)).Message);

        // ...but the same instance with its indexed values restored is fine to hand back.
        row.TenantId = 1;
        accounts.Update(row);
        Assert.AreSame(row, accounts.Get(1));
        Assert.IsTrue(accounts.Delete(1));
    }

    [TestMethod]
    public async Task An_In_Place_Update_Still_Enforces_Unique_Constraints()
    {
        var (db, accounts) = await OpenAsync();
        await using var owned = db;
        accounts.Insert(Make(1));
        accounts.Insert(Make(2));

        var row = accounts.Get(2)!;
        var before = Snapshot(row);
        row.Username = accounts.Get(1)!.Username;

        Assert.ThrowsExactly<BlazeDbUniqueConstraintViolationException>(() => accounts.UpdateInPlace(row, before));
    }

    [TestMethod]
    public async Task A_Rejected_In_Place_Update_Keeps_The_Instance_And_Allows_A_Corrected_Retry()
    {
        var (db, accounts) = await OpenAsync();
        await using var owned = db;
        accounts.Insert(Make(1, tenant: 1));
        accounts.Insert(Make(2, tenant: 2));

        var row = accounts.Get(2)!;
        var before = Snapshot(row);
        row.Username = accounts.Get(1)!.Username; // will be rejected
        row.TenantId = 1;

        Assert.ThrowsExactly<BlazeDbUniqueConstraintViolationException>(() => accounts.UpdateInPlace(row, before));

        // Nothing was written: the caller's instance is still the row (a change tracker keeps its
        // identity), and the indexes still describe the values it had before the mutation.
        Assert.AreSame(row, accounts.Get(2));
        Assert.ContainsSingle(accounts.Lookup(Account.Indexes.TenantId, 1));
        Assert.AreSame(row, accounts.Lookup(Account.Indexes.TenantId, 2).Single());

        // A corrected retry with the same previousValues finds those entries and moves them.
        row.Username = "fresh";
        row.TenantId = 3;
        accounts.UpdateInPlace(row, before);
        Assert.AreSame(row, accounts.Get(2));
        Assert.ContainsSingle(accounts.Lookup(Account.Indexes.TenantId, 1));
        Assert.IsEmpty(accounts.Lookup(Account.Indexes.TenantId, 2));
        Assert.ContainsSingle(accounts.Lookup(Account.Indexes.TenantId, 3));
    }

    [TestMethod]
    public async Task An_In_Place_Update_Cannot_Change_The_Primary_Key()
    {
        var (db, accounts) = await OpenAsync();
        await using var owned = db;
        accounts.Insert(Make(1));

        var row = accounts.Get(1)!;
        var before = Snapshot(row);
        row.Id = 99;

        Assert.ThrowsExactly<KeyNotFoundException>(() => accounts.UpdateInPlace(row, before));
    }

    [TestMethod]
    public async Task An_In_Place_Delete_Retracts_The_Old_Index_Entry()
    {
        var (db, accounts) = await OpenAsync();
        await using var owned = db;
        accounts.Insert(Make(1, tenant: 1));

        var row = accounts.Get(1)!;
        var before = Snapshot(row);
        row.TenantId = 2;
        Assert.IsTrue(accounts.DeleteInPlace(1, before));

        Assert.IsNull(accounts.Get(1));
        Assert.IsEmpty(accounts.Lookup(Account.Indexes.TenantId, 1));
        Assert.IsEmpty(accounts.Lookup(Account.Indexes.TenantId, 2));
    }

    [TestMethod]
    public async Task An_In_Place_Update_Rolls_Back_With_Its_Transaction()
    {
        var (db, accounts) = await OpenAsync();
        await using var owned = db;
        accounts.Insert(Make(1, tenant: 1));

        var row = accounts.Get(1)!;
        var before = Snapshot(row);
        using (var transaction = db.BeginTransaction())
        {
            row.TenantId = 2;
            accounts.UpdateInPlace(row, before);
            transaction.Rollback();
        }

        // The rollback restores the values the table was given, so the index agrees with the row.
        Assert.AreEqual(1, accounts.Get(1)!.TenantId);
        Assert.ContainsSingle(accounts.Lookup(Account.Indexes.TenantId, 1));
        Assert.IsEmpty(accounts.Lookup(Account.Indexes.TenantId, 2));
    }

    [TestMethod]
    public async Task Changing_An_Indexed_Value_To_Null_In_Place_Is_Caught_Like_Any_Other_Change()
    {
        // Nulls are recorded in the index like any other value, so the guard that refuses a plain
        // Update or Delete of a mutated instance sees this change too - previously a value set to
        // null slipped past it and left the old entry behind.
        var (db, accounts) = await OpenAsync();
        await using var owned = db;
        var row = Make(1);
        row.Slot = 7;
        accounts.Insert(row);

        row.Slot = null;
        Assert.ThrowsExactly<InvalidOperationException>(() => accounts.Update(row));
        Assert.ThrowsExactly<InvalidOperationException>(() => accounts.Delete(1));

        // Told what the row used to hold, the table moves the entry properly: slot 7 is free again.
        var before = Snapshot(row);
        before.Slot = 7;
        accounts.UpdateInPlace(row, before);
        Assert.IsEmpty(accounts.Range(Account.Indexes.Slot, BlazeDbBound<int?>.At(7), BlazeDbBound<int?>.At(7)));

        var other = Make(2);
        other.Slot = 7;
        accounts.Insert(other);
    }

    [TestMethod]
    public async Task Previous_Values_That_Do_Not_Match_The_Index_Are_Refused()
    {
        // Two change trackers sharing one live row each keep their own originals. Once the first
        // has saved, the second's originals describe a state the index has moved past; applying
        // them would retract nothing and add a second entry, so the row would come back twice.
        var (db, accounts) = await OpenAsync();
        await using var owned = db;
        accounts.Insert(Make(1, tenant: 1));
        var row = accounts.Get(1)!;

        var firstOriginals = Snapshot(row);
        var secondOriginals = Snapshot(row);

        row.TenantId = 2;
        accounts.UpdateInPlace(row, firstOriginals);

        // The second tracker still believes TenantId was 1.
        row.TenantId = 3;
        var ex = Assert.ThrowsExactly<BlazeDbStaleRowException>(() => accounts.UpdateInPlace(row, secondOriginals));
        Assert.AreEqual("accounts", ex.TableName);

        Assert.ThrowsExactly<BlazeDbStaleRowException>(() => accounts.DeleteInPlace(1, secondOriginals));

        // Nothing was disturbed: the index still describes the state the first save left.
        Assert.AreSame(row, accounts.Lookup(Account.Indexes.TenantId, 2).Single());
        Assert.IsEmpty(accounts.Lookup(Account.Indexes.TenantId, 1));
        Assert.IsEmpty(accounts.Lookup(Account.Indexes.TenantId, 3));
    }

    [TestMethod]
    public void Index_Definitions_Name_The_Members_They_Cover()
    {
        Assert.AreSequenceEqual(["TenantId"], Account.Indexes.TenantId.Members);
        Assert.AreSequenceEqual(["TenantId", "Email"], Account.Indexes.TenantEmail.Members);
        Assert.AreSequenceEqual(["TenantId", "CreatedAt"], Account.Indexes.TenantCreated.Members);
    }
}
