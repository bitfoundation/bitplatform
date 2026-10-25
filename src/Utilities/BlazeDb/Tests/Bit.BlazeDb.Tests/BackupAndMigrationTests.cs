using Bit.BlazeDb;
using Bit.BlazeDb.Serialization;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

/// <summary>Single-file export and import, the user version, and the migrations that move it.</summary>
[TestClass]
public class BackupAndMigrationTests
{
    private static BlazeDbDatabaseOptions Options(IBlazeDbStorage? storage = null) => new BlazeDbDatabaseOptions
    {
        Storage = storage,
        FlushInterval = TimeSpan.FromHours(1),
    }.AddTable(PersonTable.Descriptor).AddTable(Account.Table);

    private static async Task<byte[]> ExportAsync(BlazeDbDatabase db)
    {
        using var stream = new MemoryStream();
        await db.ExportAsync(stream);
        return stream.ToArray();
    }

    private static async Task ImportAsync(byte[] file, IBlazeDbStorage destination)
    {
        using var stream = new MemoryStream(file);
        await BlazeDbDatabase.ImportAsync(stream, destination);
    }

    [TestMethod]
    public async Task An_Export_Imports_As_The_Same_Database()
    {
        byte[] file;
        await using (var source = await BlazeDbDatabase.OpenAsync(Options()))
        {
            source.GetTable(PersonTable.Descriptor).InsertMany([new Person(1, "Ada", 36), new Person(2, "Grace", 45)]);
            source.GetTable(Account.Table).Insert(new Account { Id = 7, Username = "ada", TenantId = 3, Email = "a@x", Slot = 2 });
            source.SetUserVersion(4);
            file = await ExportAsync(source);
        }

        var storage = new BlazeDbInMemoryStorage();
        await ImportAsync(file, storage);

        await using var restored = await BlazeDbDatabase.OpenAsync(Options(storage));
        CollectionAssert.AreEquivalent(new[] { new Person(1, "Ada", 36), new Person(2, "Grace", 45) }, restored.GetTable(PersonTable.Descriptor).Scan().ToList());
        Assert.AreEqual("ada", restored.GetTable(Account.Table).Lookup(Account.Indexes.TenantId, 3).Single().Username);
        Assert.AreEqual(4, restored.UserVersion);
        Assert.AreEqual(0, restored.VerifyIntegrity().Count);

        // The imported database is an ordinary one: it takes writes and survives a reopen.
        restored.GetTable(PersonTable.Descriptor).Insert(new Person(3, "Alan", 41));
        await restored.FlushAsync();
        await using var reopened = await BlazeDbDatabase.OpenAsync(Options(storage.Clone()));
        Assert.AreEqual(3, reopened.GetTable(PersonTable.Descriptor).Count);
    }

    [TestMethod]
    public async Task An_Export_Carries_Tables_The_Exporter_Did_Not_Register()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using (var full = await BlazeDbDatabase.OpenAsync(Options(storage)))
        {
            full.GetTable(Account.Table).Insert(new Account { Id = 1, Username = "kept" });
        }

        byte[] file;
        await using (var partial = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage }.AddTable(PersonTable.Descriptor)))
        {
            CollectionAssert.AreEqual(new[] { "accounts" }, partial.UnregisteredTables.ToArray());
            file = await ExportAsync(partial);
        }

        var copy = new BlazeDbInMemoryStorage();
        await ImportAsync(file, copy);
        await using var restored = await BlazeDbDatabase.OpenAsync(Options(copy));
        Assert.AreEqual("kept", restored.GetTable(Account.Table).Get(1)!.Username);
    }

    [TestMethod]
    public async Task Exporting_During_A_Transaction_Is_Refused()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(Options());
        using var txn = db.BeginTransaction();
        db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Uncommitted", 1));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await ExportAsync(db));
    }

    [TestMethod]
    [DataRow("flip")]
    [DataRow("truncate")]
    [DataRow("garbage")]
    [DataRow("empty")]
    public async Task A_Damaged_Export_Is_Refused_And_Writes_Nothing(string damage)
    {
        byte[] file;
        await using (var db = await BlazeDbDatabase.OpenAsync(Options()))
        {
            db.GetTable(PersonTable.Descriptor).InsertMany(Enumerable.Range(1, 20).Select(i => new Person(i, $"P{i}", i)));
            file = await ExportAsync(db);
        }
        file = damage switch
        {
            "flip" => Flip(file, file.Length / 2),
            "truncate" => file[..^7],
            "garbage" => "not a BlazeDb export"u8.ToArray(),
            _ => [],
        };

        var storage = new BlazeDbInMemoryStorage();
        await Assert.ThrowsExactlyAsync<BlazeDbCorruptDatabaseException>(() => ImportAsync(file, storage));
        Assert.AreEqual(0, storage.FileNames.Count);
    }

    private static byte[] Flip(byte[] bytes, int index)
    {
        var copy = bytes.ToArray();
        copy[index] ^= 0x40;
        return copy;
    }

    [TestMethod]
    public async Task Importing_Over_A_Database_Is_Refused()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using (var existing = await BlazeDbDatabase.OpenAsync(Options(storage)))
        {
            existing.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Existing", 1));
        }
        byte[] file;
        await using (var other = await BlazeDbDatabase.OpenAsync(Options()))
        {
            file = await ExportAsync(other);
        }

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => ImportAsync(file, storage));

        await using var reopened = await BlazeDbDatabase.OpenAsync(Options(storage));
        Assert.AreEqual("Existing", reopened.GetTable(PersonTable.Descriptor).Get(1)!.Name);
    }

    [TestMethod]
    public async Task An_Import_Ignores_A_Log_Left_Behind_By_A_Partly_Deleted_Database()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using (var old = await BlazeDbDatabase.OpenAsync(Options(storage)))
        {
            old.GetTable(PersonTable.Descriptor).Insert(new Person(99, "Stale", 1));
        }
        storage.SetFile("manifest.blz", []);
        await storage.DeleteAsync("manifest.blz"); // gone, but wal-1.blz with the stale row remains

        byte[] file;
        await using (var source = await BlazeDbDatabase.OpenAsync(Options()))
        {
            source.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Fresh", 1));
            file = await ExportAsync(source);
        }
        await ImportAsync(file, storage);

        await using var restored = await BlazeDbDatabase.OpenAsync(Options(storage));
        CollectionAssert.AreEqual(new[] { 1 }, restored.GetTable(PersonTable.Descriptor).Scan().Select(p => p.Id).ToArray());
    }

    [TestMethod]
    public async Task UserVersion_Is_Zero_Until_Set_And_Survives_A_Reopen()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using (var db = await BlazeDbDatabase.OpenAsync(Options(storage)))
        {
            Assert.AreEqual(0, db.UserVersion);
            db.SetUserVersion(3);
            await db.CheckpointAsync();
            db.SetUserVersion(5);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => db.SetUserVersion(-1));
        }

        await using var reopened = await BlazeDbDatabase.OpenAsync(Options(storage));
        Assert.AreEqual(5, reopened.UserVersion);
        Assert.AreEqual(0, reopened.UnregisteredTables.Count);
    }

    [TestMethod]
    public async Task UserVersion_Rolls_Back_With_Its_Transaction()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(Options());
        db.SetUserVersion(1);

        using (var txn = db.BeginTransaction())
        {
            db.SetUserVersion(2);
            Assert.AreEqual(2, db.UserVersion);
            txn.Rollback();
        }

        Assert.AreEqual(1, db.UserVersion);
    }

    [TestMethod]
    public async Task A_Database_That_Never_Sets_A_Version_Writes_No_Engine_Table()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using (var db = await BlazeDbDatabase.OpenAsync(Options(storage)))
        {
            db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));
            await db.CheckpointAsync();
        }

        var snapshot = storage.GetFile("snapshot-2.blz")!;
        Assert.AreEqual(-1, snapshot.AsSpan().IndexOf("__blazedb"u8), "An earlier engine version would see an extra table.");
    }

    [TestMethod]
    public async Task Migrations_Run_In_Order_Once_Each()
    {
        var storage = new BlazeDbInMemoryStorage();
        var ran = new List<long>();
        BlazeDbDatabaseOptions WithMigrations(int upTo)
        {
            var options = Options(storage);
            options.AddMigration(2, db => { ran.Add(2); db.GetTable(PersonTable.Descriptor).Insert(new Person(2, "From v2", 2)); });
            options.AddMigration(1, db => { ran.Add(1); db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "From v1", 1)); });
            if (upTo >= 3)
            {
                options.AddMigration(3, db =>
                {
                    ran.Add(3);
                    var people = db.GetTable(PersonTable.Descriptor);
                    people.UpsertMany(people.Scan().Select(p => p with { Age = p.Age * 10 }).ToList());
                });
            }
            return options;
        }

        await using (var db = await BlazeDbDatabase.OpenAsync(WithMigrations(2)))
        {
            Assert.AreEqual(2, db.UserVersion);
        }
        CollectionAssert.AreEqual(new long[] { 1, 2 }, ran);

        await using (var db = await BlazeDbDatabase.OpenAsync(WithMigrations(3)))
        {
            Assert.AreEqual(3, db.UserVersion);
            CollectionAssert.AreEquivalent(new[] { 10, 20 }, db.GetTable(PersonTable.Descriptor).Scan().Select(p => p.Age).ToArray());
        }
        CollectionAssert.AreEqual(new long[] { 1, 2, 3 }, ran);

        await using (var db = await BlazeDbDatabase.OpenAsync(WithMigrations(3)))
        {
            Assert.AreEqual(3, db.UserVersion);
        }
        CollectionAssert.AreEqual(new long[] { 1, 2, 3 }, ran, "Nothing runs twice.");
    }

    [TestMethod]
    public async Task A_Failing_Migration_Fails_The_Open_And_Is_Retried_Next_Time()
    {
        var storage = new BlazeDbInMemoryStorage();
        var fail = true;
        BlazeDbDatabaseOptions WithMigrations() => Options(storage)
            .AddMigration(1, db => db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "v1", 1)))
            .AddMigration(2, db =>
            {
                db.GetTable(PersonTable.Descriptor).Insert(new Person(2, "v2", 2));
                if (fail)
                {
                    throw new InvalidOperationException("migration 2 failed");
                }
            });

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await BlazeDbDatabase.OpenAsync(WithMigrations()));

        await using (var inspect = await BlazeDbDatabase.OpenAsync(Options(storage)))
        {
            Assert.AreEqual(1, inspect.UserVersion, "The first migration committed; the failed one left nothing.");
            CollectionAssert.AreEqual(new[] { 1 }, inspect.GetTable(PersonTable.Descriptor).Scan().Select(p => p.Id).ToArray());
        }

        fail = false;
        await using var retried = await BlazeDbDatabase.OpenAsync(WithMigrations());
        Assert.AreEqual(2, retried.UserVersion);
        Assert.AreEqual(2, retried.GetTable(PersonTable.Descriptor).Count);
    }

    [TestMethod]
    public async Task A_Replica_Does_Not_Run_Migrations_But_Sees_The_Version()
    {
        var storage = new BlazeDbInMemoryStorage();
        await (await BlazeDbDatabase.OpenAsync(Options(storage).AddMigration(1, _ => { }))).DisposeAsync();

        var ran = false;
        var replicaOptions = Options(storage).AddMigration(2, _ => ran = true);
        replicaOptions.ReadOnly = true;
        await using var replica = await BlazeDbDatabase.OpenAsync(replicaOptions);

        Assert.IsFalse(ran);
        Assert.AreEqual(1, replica.UserVersion);
        Assert.ThrowsExactly<InvalidOperationException>(() => replica.SetUserVersion(2));
    }

    [TestMethod]
    public void Invalid_Migrations_And_Reserved_Table_Names_Are_Refused()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BlazeDbDatabaseOptions().AddMigration(0, _ => { }));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BlazeDbDatabaseOptions().AddMigration(1, null!));

        var duplicate = new BlazeDbDatabaseOptions().AddMigration(1, _ => { }).AddMigration(1, _ => { });
        Assert.ThrowsExactlyAsync<ArgumentException>(async () => await BlazeDbDatabase.OpenAsync(duplicate)).GetAwaiter().GetResult();

        var reserved = new BlazeDbTableDescriptor<int, Person>("__blazedb_mine", static p => p.Id,
            PersonTable.Descriptor.RowWriter, PersonTable.Descriptor.RowReader, PersonTable.Descriptor.KeyWriter, PersonTable.Descriptor.KeyReader);
        Assert.ThrowsExactlyAsync<ArgumentException>(async () => await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(reserved))).GetAwaiter().GetResult();
    }

    [TestMethod]
    public async Task The_Export_Is_A_Checksummed_Snapshot()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(Options());
        db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));

        var file = await ExportAsync(db);

        CollectionAssert.AreEqual("BLZS"u8.ToArray(), file[..4]);
        var body = file.AsSpan(0, file.Length - 4);
        var stored = new BlazeDbBufferReader(file.AsSpan(file.Length - 4)).ReadFixed32();
        Assert.AreEqual(Bit.BlazeDb.Wal.BlazeDbCrc32.Compute(body), stored);
    }
}
