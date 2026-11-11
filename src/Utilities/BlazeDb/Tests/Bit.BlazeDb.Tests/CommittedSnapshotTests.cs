using Bit.BlazeDb.EntityFrameworkCore.Infrastructure;
using Bit.BlazeDb.Storage;
using Bit.BlazeDb.Tests.@event;
using Bit.BlazeDb.Tests.EntityFrameworkCore;
using EfPerson = Bit.BlazeDb.Tests.EntityFrameworkCore.Person;
using Microsoft.EntityFrameworkCore;

namespace Bit.BlazeDb.Tests;

/// <summary>
/// Checkpoints and exports hold what the log committed - never an edit made in place and not saved - and keys the
/// table treats as equal are written as the same bytes.
/// </summary>
[TestClass]
public class CommittedSnapshotTests
{
    [TestMethod]
    public async Task An_Unsaved_EF_Core_Edit_Reaches_Neither_A_Checkpoint_Nor_An_Export()
    {
        var storage = new BlazeDbInMemoryStorage();
        var options = new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(storage).Options;
        using var context = new PeopleContext(options);
        context.People.Add(new EfPerson { Id = 1, Name = "Ada", City = "London", Age = 36 });
        context.SaveChanges();

        // A query hands back the table's own row, so this edit is in memory at once - and never saved.
        var ada = context.People.Single();
        ada.Name = "Unsaved";
        var engine = context.Database.GetBlazeDb();
        await engine.CheckpointAsync();
        using var export = new MemoryStream();
        await engine.ExportAsync(export);

        // What storage holds now, opened as the next session would.
        await using (var reopened = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage.Clone() }.AddTable(EfPerson.Table)))
        {
            Assert.AreEqual("Ada", reopened.GetTable(EfPerson.Table).Get(1)!.Name);
        }
        var imported = new BlazeDbInMemoryStorage();
        export.Position = 0;
        await BlazeDbDatabase.ImportAsync(export, imported);
        await using (var restored = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = imported }.AddTable(EfPerson.Table)))
        {
            Assert.AreEqual("Ada", restored.GetTable(EfPerson.Table).Get(1)!.Name);
        }

        // Saved, it is committed and checkpointed like any write.
        context.SaveChanges();
        await engine.CheckpointAsync();
        await using (var reopened = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage.Clone() }.AddTable(EfPerson.Table)))
        {
            Assert.AreEqual("Unsaved", reopened.GetTable(EfPerson.Table).Get(1)!.Name);
        }
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task A_Checkpoint_Keeps_Committed_Writes_Made_While_It_Runs()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(PersonTable.Descriptor));
        var people = db.GetTable(PersonTable.Descriptor);
        for (var i = 0; i < 200; i++)
        {
            people.Insert(new Person(i, "p" + i, i));
        }
        await db.FlushAsync();
        people.Delete(3);
        people.Upsert(new Person(4, "four", 4));

        var checkpoint = db.CheckpointAsync().AsTask();
        people.Insert(new Person(1000, "during", 1));
        await checkpoint;
        people.Delete(5);
        await db.DisposeAsync();

        await using var reopened = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage }.AddTable(PersonTable.Descriptor));
        var table = reopened.GetTable(PersonTable.Descriptor);
        Assert.AreEqual(199, table.Count);
        Assert.IsFalse(table.Contains(3));
        Assert.IsFalse(table.Contains(5));
        Assert.AreEqual("four", table.Get(4)!.Name);
        Assert.AreEqual("during", table.Get(1000)!.Name);
    }

    [TestMethod]
    public async Task A_Delete_Written_With_Another_Spelling_Of_The_Key_Still_Removes_The_Row_From_A_Table_Carried_Without_Its_Descriptor()
    {
        var storage = new BlazeDbInMemoryStorage();
        var withTable = new BlazeDbDatabaseOptions { Storage = storage, FlushInterval = TimeSpan.FromHours(1) }.AddTable(DecimalKeyed.Table);
        await using (var db = await BlazeDbDatabase.OpenAsync(withTable))
        {
            var rows = db.GetTable(DecimalKeyed.Table);
            rows.Insert(new DecimalKeyed { Id = 1.0m, Note = "one" });
            rows.Insert(new DecimalKeyed { Id = 2m, Note = "two" });
            await db.CheckpointAsync();
            // 1.00m is the same key to the table, but not the same bytes as written.
            Assert.IsTrue(rows.Delete(1.00m));
        }

        // Opened without the table: its rows are carried as bytes, and the delete is replayed and checkpointed there.
        await using (var narrow = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage }.AddTable(PersonTable.Descriptor)))
        {
            CollectionAssert.AreEqual(new[] { "decimal_keys" }, narrow.UnregisteredTables.ToArray());
            await narrow.CheckpointAsync();
        }

        await using var reopened = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage }.AddTable(DecimalKeyed.Table));
        var table = reopened.GetTable(DecimalKeyed.Table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("two", table.Get(2.00m)!.Note);
    }

    [TestMethod]
    public async Task A_Database_Whose_Snapshot_Predates_Keyed_Rows_Is_Checkpointed_Into_The_Current_Format()
    {
        // The first snapshot format stored rows without their keys; a checkpoint derives them from the rows.
        var storage = new BlazeDbInMemoryStorage();
        var snapshot = new Bit.BlazeDb.Serialization.BlazeDbBufferWriter();
        snapshot.WriteRaw("BLZS"u8);
        snapshot.WriteByte(1);
        snapshot.WriteVarUInt(0);
        snapshot.WriteVarUInt(1);
        snapshot.WriteString(PersonTable.Descriptor.Name);
        snapshot.WriteVarUInt(3);
        var row = new Bit.BlazeDb.Serialization.BlazeDbBufferWriter();
        foreach (var person in new[] { new Person(1, "Ada", 36), new Person(2, "Grace", 45), new Person(3, "Edsger", 72) })
        {
            row.Reset();
            PersonTable.Descriptor.RowWriter(row, person);
            snapshot.WriteBytes(row.WrittenSpan);
        }
        snapshot.WriteFixed32(Bit.BlazeDb.Wal.BlazeDbCrc32.Compute(snapshot.WrittenSpan));
        await storage.WriteAtomicAsync("snapshot-1.blz", snapshot.ToArray());
        var manifest = new Bit.BlazeDb.Serialization.BlazeDbBufferWriter();
        manifest.WriteRaw("BLZM"u8);
        manifest.WriteByte(1);
        manifest.WriteVarUInt(1);
        manifest.WriteString("snapshot-1.blz");
        manifest.WriteString("wal-1.blz");
        manifest.WriteVarUInt(0);
        manifest.WriteFixed32(Bit.BlazeDb.Wal.BlazeDbCrc32.Compute(manifest.WrittenSpan));
        await storage.WriteAtomicAsync("manifest.blz", manifest.ToArray());

        var options = new BlazeDbDatabaseOptions { Storage = storage, FlushInterval = TimeSpan.FromHours(1) }.AddTable(PersonTable.Descriptor);
        await using (var db = await BlazeDbDatabase.OpenAsync(options))
        {
            var people = db.GetTable(PersonTable.Descriptor);
            Assert.AreEqual(3, people.Count);
            people.Delete(2);
            people.Upsert(new Person(3, "Dijkstra", 72));
            await db.CheckpointAsync();
        }

        await using var reopened = await BlazeDbDatabase.OpenAsync(
            new BlazeDbDatabaseOptions { Storage = storage }.AddTable(PersonTable.Descriptor));
        var table = reopened.GetTable(PersonTable.Descriptor);
        CollectionAssert.AreEquivalent(new[] { "Ada", "Dijkstra" }, table.Scan().Select(p => p.Name).ToArray());
        CollectionAssert.AreEqual("BLZS"u8.ToArray().Append((byte)2).ToArray(), storage.GetFile(storage.Snapshot(2))![..5]);
    }

    [TestMethod]
    public async Task A_Replica_Exports_The_Generation_It_Loaded_While_The_Writer_Moves_On()
    {
        var storage = new BlazeDbInMemoryStorage();
        var writerOptions = new BlazeDbDatabaseOptions { Storage = storage, FlushInterval = TimeSpan.FromHours(1) }.AddTable(PersonTable.Descriptor);
        await using var writer = await BlazeDbDatabase.OpenAsync(writerOptions);
        writer.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));
        await writer.FlushAsync();

        await using var replica = await BlazeDbDatabase.OpenAsync(
            new BlazeDbDatabaseOptions { Storage = storage, ReadOnly = true }.AddTable(PersonTable.Descriptor));
        // The writer commits, flushes and checkpoints past what the replica loaded.
        writer.GetTable(PersonTable.Descriptor).Insert(new Person(2, "Grace", 45));
        await writer.FlushAsync();
        await writer.CheckpointAsync();

        using var export = new MemoryStream();
        await replica.ExportAsync(export);
        export.Position = 0;
        var imported = new BlazeDbInMemoryStorage();
        await BlazeDbDatabase.ImportAsync(export, imported);
        await using var restored = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = imported }.AddTable(PersonTable.Descriptor));
        CollectionAssert.AreEqual(new[] { 1 }, restored.GetTable(PersonTable.Descriptor).Scan().Select(p => p.Id).ToArray());
    }

    [TestMethod]
    public async Task A_Log_Damaged_After_The_Open_Is_Replaced_By_A_Checkpoint_Of_Memory()
    {
        var storage = new BlazeDbInMemoryStorage();
        var options = new BlazeDbDatabaseOptions { Storage = storage, FlushInterval = TimeSpan.FromHours(1) }.AddTable(PersonTable.Descriptor);
        await using (var db = await BlazeDbDatabase.OpenAsync(options))
        {
            var people = db.GetTable(PersonTable.Descriptor);
            for (var i = 0; i < 5; i++)
            {
                people.Insert(new Person(i, "p" + i, i));
                await db.FlushAsync();
            }
            // Damage in the middle of the log, with intact records after it: a scan reports corruption.
            var wal = storage.Wal();
            var bytes = storage.GetFile(wal)!;
            bytes[bytes.Length / 2] ^= 0xFF;
            storage.SetFile(wal, bytes);

            await db.CheckpointAsync();
        }

        await using var reopened = await BlazeDbDatabase.OpenAsync(options);
        Assert.AreEqual(5, reopened.GetTable(PersonTable.Descriptor).Count);
    }

    [TestMethod]
    public void Keys_The_Table_Treats_As_Equal_Have_One_Canonical_Form()
    {
        Assert.AreEqual(BlazeDbKeyCanonical.Of(1.0m).Scale, BlazeDbKeyCanonical.Of(1.000m).Scale);
        Assert.AreEqual(0, BlazeDbKeyCanonical.Of(-0.00m).Scale);
        Assert.AreEqual(2, BlazeDbKeyCanonical.Of(1.25m).Scale);
        Assert.AreEqual(1.5m, BlazeDbKeyCanonical.Of(1.50m));

        var utc = new DateTime(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);
        Assert.AreEqual(DateTimeKind.Unspecified, BlazeDbKeyCanonical.Of(utc).Kind);
        Assert.AreEqual(utc.Ticks, BlazeDbKeyCanonical.Of(utc).Ticks);

        var offset = new DateTimeOffset(2026, 11, 11, 13, 30, 0, TimeSpan.FromHours(3.5));
        Assert.AreEqual(TimeSpan.Zero, BlazeDbKeyCanonical.Of(offset).Offset);
        Assert.AreEqual(offset, BlazeDbKeyCanonical.Of(offset));

        Assert.IsFalse(double.IsNegative(BlazeDbKeyCanonical.Of(-0.0)));
        Assert.AreEqual(BitConverter.DoubleToInt64Bits(double.NaN), BitConverter.DoubleToInt64Bits(BlazeDbKeyCanonical.Of(BitConverter.Int64BitsToDouble(unchecked((long)0xFFF8000000000001)))));
        Assert.AreEqual("same", BlazeDbKeyCanonical.Of("same"));
        Assert.AreEqual(42, BlazeDbKeyCanonical.Of(42));
    }
}
