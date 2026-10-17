using Bit.BlazeDb.Serialization;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

/// <summary>
/// A write the log cannot record must not stay in memory: on the next open it would be gone, so
/// leaving it visible now would be showing a state the database will never have again.
/// </summary>
[TestClass]
public class CommitFailureTests
{
    /// <summary>A row serializer that refuses one particular row, standing in for any encoding failure.</summary>
    private static readonly BlazeDbTableDescriptor<int, Person> Fragile = new(
        "fragile",
        static p => p.Id,
        static (w, p) =>
        {
            if (p.Name == "boom")
            {
                throw new InvalidOperationException("This row cannot be encoded.");
            }
            w.WriteTag(1, BlazeDbWireType.VarInt);
            w.WriteVarInt(p.Id);
            w.WriteTag(2, BlazeDbWireType.LengthDelimited);
            w.WriteString(p.Name);
        },
        static (ref BlazeDbBufferReader r) =>
        {
            var id = 0;
            var name = "";
            while (r.Remaining > 0)
            {
                var (field, wire) = r.ReadTag();
                if (field == 1) id = (int)r.ReadVarInt();
                else if (field == 2) name = r.ReadString();
                else r.SkipField(wire);
            }
            return new Person(id, name, 0);
        },
        static (w, k) => w.WriteVarInt(k),
        static (ref BlazeDbBufferReader r) => (int)r.ReadVarInt());

    private static Task<BlazeDbDatabase> OpenAsync(IBlazeDbStorage storage) =>
        BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(Fragile)).AsTask();

    [TestMethod]
    public async Task A_Transaction_Whose_Commit_Cannot_Be_Journaled_Is_Undone_And_Over()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using var db = await OpenAsync(storage);
        var table = db.GetTable(Fragile);
        table.Insert(new Person(1, "Ada", 0));

        using (var txn = db.BeginTransaction())
        {
            table.Insert(new Person(2, "Grace", 0));
            table.Update(new Person(1, "Ada Lovelace", 0));
            table.Insert(new Person(3, "boom", 0));
            Assert.AreEqual(3, table.Count); // Applied to memory, as inside any transaction.

            Assert.ThrowsExactly<InvalidOperationException>(txn.Commit);

            // Everything the batch did is gone, not just the row that failed, and the transaction is
            // finished: a second Commit is refused and disposing it does not try to roll back again.
            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(new Person(1, "Ada", 0), table.Get(1));
            Assert.ThrowsExactly<InvalidOperationException>(txn.Commit);
        }

        // The database is usable straight away, and what did commit is all the log holds.
        table.Insert(new Person(4, "Alan", 0));
        await db.FlushAsync();
        await using var reopened = await OpenAsync(storage.Clone());
        Assert.AreSequenceEqual([1, 4], reopened.GetTable(Fragile).Scan().Select(p => p.Id).Order().ToList());
    }

    [TestMethod]
    public async Task An_Autocommit_Write_That_Cannot_Be_Journaled_Is_Undone()
    {
        await using var db = await OpenAsync(new BlazeDbInMemoryStorage());
        var table = db.GetTable(Fragile);
        table.Insert(new Person(1, "Ada", 0));

        Assert.ThrowsExactly<InvalidOperationException>(() => table.Update(new Person(1, "boom", 0)));
        Assert.AreEqual(new Person(1, "Ada", 0), table.Get(1));
        Assert.ThrowsExactly<InvalidOperationException>(() => table.Insert(new Person(2, "boom", 0)));
        Assert.AreEqual(1, table.Count);
    }
}
