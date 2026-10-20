using Bit.BlazeDb;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

[TestClass]
public class DurabilityTests
{
    private static readonly TimeSpan Never = TimeSpan.FromHours(1);

    private static Task<BlazeDbDatabase> OpenAsync(IBlazeDbStorage storage, TimeSpan? flushInterval = null) =>
        BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = flushInterval ?? Never,
        }.AddTable(PersonTable.Descriptor)).AsTask();

    [TestMethod]
    public async Task Flushed_Data_Survives_Reopen_After_Clean_Dispose()
    {
        var storage = new BlazeDbInMemoryStorage();

        await using (var db = await OpenAsync(storage))
        {
            var people = db.GetTable(PersonTable.Descriptor);
            people.Insert(new Person(1, "Ada", 36));
            people.Insert(new Person(2, "Grace", 45));
            people.Update(new Person(2, "Grace Hopper", 46));
            people.Insert(new Person(3, "Alan", 41));
            people.Delete(3);
        }

        await using var reopened = await OpenAsync(storage);
        var recovered = reopened.GetTable(PersonTable.Descriptor);
        Assert.AreEqual(2, recovered.Count);
        Assert.AreEqual(new Person(1, "Ada", 36), recovered.Get(1));
        Assert.AreEqual(new Person(2, "Grace Hopper", 46), recovered.Get(2));
        Assert.IsFalse(recovered.Contains(3));
    }

    [TestMethod]
    public async Task Flushed_Data_Survives_Crash_Without_Dispose()
    {
        var storage = new BlazeDbInMemoryStorage();

        var db = await OpenAsync(storage);
        db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));
        await db.FlushAsync();
        // Simulated crash: the instance is abandoned without DisposeAsync.

        await using var reopened = await OpenAsync(storage);
        Assert.AreEqual(new Person(1, "Ada", 36), reopened.GetTable(PersonTable.Descriptor).Get(1));
    }

    [TestMethod]
    public async Task Unflushed_Data_Is_Lost_On_Crash_But_Flushed_Data_Survives()
    {
        var storage = new BlazeDbInMemoryStorage();

        var db = await OpenAsync(storage);
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "Durable", 1));
        await db.FlushAsync();
        people.Insert(new Person(2, "Volatile", 2));
        // Crash before the group-commit interval elapses.

        await using var reopened = await OpenAsync(storage);
        var recovered = reopened.GetTable(PersonTable.Descriptor);
        Assert.IsTrue(recovered.Contains(1));
        Assert.IsFalse(recovered.Contains(2));
    }

    [TestMethod]
    public async Task Background_Flusher_Persists_Without_Explicit_Flush()
    {
        var storage = new BlazeDbInMemoryStorage();

        var db = await OpenAsync(storage, flushInterval: TimeSpan.FromMilliseconds(20));
        db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));

        // Wait for at least one flusher tick.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
            await using var probe = await OpenAsync(storage);
            if (probe.GetTable(PersonTable.Descriptor).Contains(1))
            {
                return;
            }
        }
        Assert.Fail("Background flusher did not persist the write within 5 seconds.");
    }

    [TestMethod]
    public async Task A_Transient_Background_Failure_Is_Reported_Then_Retried_And_Cleared()
    {
        var storage = new FlakyStorage { FailAppends = true };

        var db = await OpenAsync(storage, flushInterval: TimeSpan.FromMilliseconds(20));
        db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));

        await WaitUntilAsync(() => db.LastBackgroundError is not null, "the flusher never reported its failure");
        Assert.IsExactInstanceOfType<IOException>(db.LastBackgroundError);

        // Storage recovers: the flusher retries on its own, the record lands, and the error clears.
        storage.FailAppends = false;
        await WaitUntilAsync(() => db.LastBackgroundError is null, "the flusher never recovered");

        // A clean shutdown after a recovered failure must not throw a stale error.
        await db.DisposeAsync();

        await using var reopened = await OpenAsync(storage);
        Assert.IsTrue(reopened.GetTable(PersonTable.Descriptor).Contains(1));
    }

    [TestMethod]
    public async Task Disposing_Surfaces_A_Failure_That_Is_Still_Standing()
    {
        var storage = new FlakyStorage { FailAppends = true };

        var db = await OpenAsync(storage);
        db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));

        // The final flush cannot get the record onto storage, and says so.
        await Assert.ThrowsExactlyAsync<IOException>(async () => await db.DisposeAsync());
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string failure)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }
            await Task.Delay(20);
        }
        Assert.Fail($"Timed out: {failure}.");
    }

    /// <summary>An in-memory backend whose appends can be made to fail on demand.</summary>
    private sealed class FlakyStorage : IBlazeDbStorage
    {
        private readonly BlazeDbInMemoryStorage _inner = new();

        public bool FailAppends { get; set; }

        public ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default) =>
            _inner.ReadAsync(name, cancellationToken);

        public ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            _inner.WriteAtomicAsync(name, data, cancellationToken);

        public ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            FailAppends
                ? throw new IOException("Simulated storage failure.")
                : _inner.AppendAsync(name, data, cancellationToken);

        public ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default) =>
            _inner.DeleteAsync(name, cancellationToken);
    }

    [TestMethod]
    public async Task A_Partial_Append_Is_Cut_Off_Before_The_Retry_Lands_Behind_It()
    {
        // An append that fails part-way leaves a fragment on storage. The retry writes the whole
        // record again; if it landed behind the fragment, replay would stop at the fragment and the
        // next open would truncate the retry - and everything after it - away.
        var storage = new PartialWriteStorage { FailAfterBytes = 10 };

        var db = await OpenAsync(storage);
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "Ada", 36));
        await Assert.ThrowsExactlyAsync<IOException>(async () => await db.FlushAsync());
        Assert.AreEqual(10, storage.Inner.GetFile("wal-1.blz")!.Length);

        storage.FailAfterBytes = null;
        people.Insert(new Person(2, "Grace", 45));
        await db.FlushAsync();
        await db.DisposeAsync();

        await using var reopened = await OpenAsync(storage);
        Assert.AreSequenceEqual([1, 2], reopened.GetTable(PersonTable.Descriptor).Scan().Select(p => p.Id).Order());
    }

    /// <summary>An in-memory backend whose appends can be made to write a prefix and then fail.</summary>
    private sealed class PartialWriteStorage : IBlazeDbStorage
    {
        public BlazeDbInMemoryStorage Inner { get; } = new();

        public int? FailAfterBytes { get; set; }

        public ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default) =>
            Inner.ReadAsync(name, cancellationToken);

        public ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            Inner.WriteAtomicAsync(name, data, cancellationToken);

        public async ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            if (FailAfterBytes is { } prefix && prefix < data.Length)
            {
                await Inner.AppendAsync(name, data[..prefix], cancellationToken);
                throw new IOException("Simulated disk-full part-way through the write.");
            }
            await Inner.AppendAsync(name, data, cancellationToken);
        }

        public ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default) =>
            Inner.DeleteAsync(name, cancellationToken);
    }

    [TestMethod]
    public async Task A_Corrupted_Lsn_Stops_Replay_Rather_Than_Skipping_The_Commit()
    {
        // The LSN sits outside the payload; if it were outside the checksum too, an LSN damaged into
        // something below the snapshot's would make replay skip that one commit and carry on - a
        // hole in the middle of history with nothing to say so.
        var storage = new BlazeDbInMemoryStorage();
        await using (var db = await OpenAsync(storage))
        {
            db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));
            await db.FlushAsync();
            db.GetTable(PersonTable.Descriptor).Insert(new Person(2, "Grace", 45));
        }

        // Record layout: magic(4) length(4) lsn(8) crc(4) payload. Zero the first record's LSN.
        var wal = storage.GetFile("wal-1.blz")!;
        Array.Clear(wal, 8, 8);
        storage.SetFile("wal-1.blz", wal);

        await using var reopened = await OpenAsync(storage);
        var recovered = reopened.GetTable(PersonTable.Descriptor);
        Assert.IsFalse(recovered.Contains(1));
        Assert.IsFalse(recovered.Contains(2), "replay continued past a record whose header was damaged");
    }

    [TestMethod]
    public async Task Torn_Tail_Is_Discarded_And_Wal_Keeps_Working()
    {
        var storage = new BlazeDbInMemoryStorage();

        await using (var db = await OpenAsync(storage))
        {
            var people = db.GetTable(PersonTable.Descriptor);
            people.Insert(new Person(1, "Ada", 36));
            people.Insert(new Person(2, "Grace", 45));
        }

        // Simulate a crash mid-append: garbage / half a record at the end of the WAL.
        var wal = storage.GetFile("wal-1.blz")!;
        var torn = wal.Concat(new byte[] { 0x10, 0x00, 0x00, 0x00, 0xAA, 0xBB }).ToArray();
        storage.SetFile("wal-1.blz", torn);

        await using (var reopened = await OpenAsync(storage))
        {
            var recovered = reopened.GetTable(PersonTable.Descriptor);
            Assert.AreEqual(2, recovered.Count);
            recovered.Insert(new Person(3, "Alan", 41));
        }

        await using var final = await OpenAsync(storage);
        Assert.AreEqual(3, final.GetTable(PersonTable.Descriptor).Count);
    }

    [TestMethod]
    public async Task Corrupted_Record_Payload_Stops_Replay_At_Last_Valid_Record()
    {
        var storage = new BlazeDbInMemoryStorage();

        await using (var db = await OpenAsync(storage))
        {
            db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));
            await db.FlushAsync();
            db.GetTable(PersonTable.Descriptor).Insert(new Person(2, "Grace", 45));
        }

        // Flip a byte in the last record's payload; its CRC no longer matches.
        var wal = storage.GetFile("wal-1.blz")!;
        wal[^1] ^= 0xFF;
        storage.SetFile("wal-1.blz", wal);

        await using var reopened = await OpenAsync(storage);
        var recovered = reopened.GetTable(PersonTable.Descriptor);
        Assert.IsTrue(recovered.Contains(1));
        Assert.IsFalse(recovered.Contains(2));
    }

    [TestMethod]
    public async Task Checkpoint_Compacts_Wal_Into_Snapshot_And_Recovers()
    {
        var storage = new BlazeDbInMemoryStorage();

        await using (var db = await OpenAsync(storage))
        {
            var people = db.GetTable(PersonTable.Descriptor);
            for (var i = 1; i <= 50; i++)
            {
                people.Upsert(new Person(i, $"Person {i}", i));
            }
            people.Delete(50);
            await db.CheckpointAsync();

            Assert.Contains("snapshot-2.blz", storage.FileNames);
            Assert.DoesNotContain("wal-1.blz", storage.FileNames);

            // Writes after the checkpoint land in the new WAL.
            people.Insert(new Person(100, "After Checkpoint", 1));
        }

        await using var reopened = await OpenAsync(storage);
        var recovered = reopened.GetTable(PersonTable.Descriptor);
        Assert.AreEqual(50, recovered.Count);
        Assert.AreEqual(new Person(49, "Person 49", 49), recovered.Get(49));
        Assert.IsFalse(recovered.Contains(50));
        Assert.AreEqual(new Person(100, "After Checkpoint", 1), recovered.Get(100));
    }

    [TestMethod]
    public async Task Automatic_Checkpoint_Triggers_When_Wal_Grows()
    {
        var storage = new BlazeDbInMemoryStorage();

        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = TimeSpan.FromMilliseconds(20),
            CheckpointWalSize = 2 * 1024,
        }.AddTable(PersonTable.Descriptor));
        await using (db)
        {
            var people = db.GetTable(PersonTable.Descriptor);
            for (var i = 1; i <= 500; i++)
            {
                people.Upsert(new Person(i, $"Person number {i} with a reasonably long name", i));
            }

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < deadline && !storage.FileNames.Any(f => f.StartsWith("snapshot-")))
            {
                await Task.Delay(50);
            }
            Assert.Contains(f => f.StartsWith("snapshot-"), storage.FileNames);
        }

        await using var reopened = await OpenAsync(storage);
        Assert.AreEqual(500, reopened.GetTable(PersonTable.Descriptor).Count);
    }

    [TestMethod]
    public async Task Committed_Transaction_Is_Atomic_Across_Recovery()
    {
        var storage = new BlazeDbInMemoryStorage();

        await using (var db = await OpenAsync(storage))
        {
            var people = db.GetTable(PersonTable.Descriptor);
            using var txn = db.BeginTransaction();
            people.Insert(new Person(1, "Ada", 36));
            people.Insert(new Person(2, "Grace", 45));
            txn.Commit();
        }

        await using var reopened = await OpenAsync(storage);
        Assert.AreEqual(2, reopened.GetTable(PersonTable.Descriptor).Count);
    }

    [TestMethod]
    public async Task RolledBack_Transaction_Leaves_No_Trace_After_Recovery()
    {
        var storage = new BlazeDbInMemoryStorage();

        await using (var db = await OpenAsync(storage))
        {
            var people = db.GetTable(PersonTable.Descriptor);
            people.Insert(new Person(1, "Keep", 1));
            using (db.BeginTransaction())
            {
                people.Insert(new Person(2, "Discard", 2));
                // Disposed without commit -> rollback.
            }
            await db.FlushAsync();
        }

        await using var reopened = await OpenAsync(storage);
        var recovered = reopened.GetTable(PersonTable.Descriptor);
        Assert.IsTrue(recovered.Contains(1));
        Assert.IsFalse(recovered.Contains(2));
    }

    [TestMethod]
    public async Task Corrupt_Manifest_Throws_CorruptDatabaseException()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using (var db = await OpenAsync(storage))
        {
            db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));
        }

        var manifest = storage.GetFile("manifest.blz")!;
        manifest[^1] ^= 0xFF;
        storage.SetFile("manifest.blz", manifest);

        await Assert.ThrowsExactlyAsync<BlazeDbCorruptDatabaseException>(async () => await OpenAsync(storage));
    }

    [TestMethod]
    public async Task Corrupt_Snapshot_Throws_CorruptDatabaseException()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using (var db = await OpenAsync(storage))
        {
            db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));
            await db.CheckpointAsync();
        }

        var snapshot = storage.GetFile("snapshot-2.blz")!;
        snapshot[^1] ^= 0xFF;
        storage.SetFile("snapshot-2.blz", snapshot);

        await Assert.ThrowsExactlyAsync<BlazeDbCorruptDatabaseException>(async () => await OpenAsync(storage));
    }

    [TestMethod]
    public async Task Checkpoint_Inside_Transaction_Is_Rejected()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using var db = await OpenAsync(storage);

        using var txn = db.BeginTransaction();
        db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await db.CheckpointAsync());
        txn.Commit();
    }

    [TestMethod]
    public async Task FileStorage_Full_Cycle_Persists_Across_Reopen()
    {
        var directory = Path.Combine(Path.GetTempPath(), "blazedb-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (var storage = new BlazeDbFileStorage(directory))
            {
                await using var db = await OpenAsync(storage);
                var people = db.GetTable(PersonTable.Descriptor);
                for (var i = 1; i <= 20; i++)
                {
                    people.Insert(new Person(i, $"Person {i}", i));
                }
                await db.CheckpointAsync();
                people.Insert(new Person(21, "After", 21));
            }

            using var again = new BlazeDbFileStorage(directory);
            await using var reopened = await OpenAsync(again);
            var recovered = reopened.GetTable(PersonTable.Descriptor);
            Assert.AreEqual(21, recovered.Count);
            Assert.AreEqual(new Person(21, "After", 21), recovered.Get(21));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task FileStorage_Allows_One_Writer_And_Any_Number_Of_ReadOnly_Followers()
    {
        var directory = Path.Combine(Path.GetTempPath(), "blazedb-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            // Nothing to follow yet: a replica waits for the writer rather than inventing a database.
            Assert.IsNull(BlazeDbFileStorage.OpenReadOnly(directory));

            using var writer = new BlazeDbFileStorage(directory);
            Assert.ThrowsExactly<BlazeDbDatabaseLockedException>(() => new BlazeDbFileStorage(directory));

            await using var db = await OpenAsync(writer);
            db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));
            await db.FlushAsync();

            using var follower = BlazeDbFileStorage.OpenReadOnly(directory)!;
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await follower.AppendAsync("wal-1.blz", new byte[1]));
            await using var replica = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
            {
                Storage = follower,
                ReadOnly = true,
                FlushInterval = Never,
            }.AddTable(PersonTable.Descriptor));
            Assert.AreEqual("Ada", replica.GetTable(PersonTable.Descriptor).Get(1)!.Name);

            // Disposing the writer's storage frees the directory for the next writer.
            await db.DisposeAsync();
            writer.Dispose();
            using var next = new BlazeDbFileStorage(directory);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task Deleting_A_Database_Leaves_Nothing_For_The_Next_Open_To_Replay()
    {
        // Removing only the manifest is not a delete: recovery treats a missing manifest as a fresh
        // database pointing at wal-1.blz and does not truncate that file, so the next open would
        // replay the rows that were supposedly gone.
        foreach (var enumerable in new[] { true, false })
        {
            var inner = new BlazeDbInMemoryStorage();
            IBlazeDbStorage storage = enumerable ? inner : new OpaqueStorage(inner);

            await using (var db = await OpenAsync(storage))
            {
                db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));
                await db.CheckpointAsync();
                db.GetTable(PersonTable.Descriptor).Insert(new Person(2, "Grace", 45));
            }

            await BlazeDbDatabase.DeleteAsync(storage);
            Assert.IsEmpty(inner.FileNames);

            await using (var fresh = await OpenAsync(storage))
            {
                Assert.AreEqual(0, fresh.GetTable(PersonTable.Descriptor).Count);
                fresh.GetTable(PersonTable.Descriptor).Insert(new Person(3, "Alan", 41));
            }

            await using var reopened = await OpenAsync(storage);
            Assert.AreSequenceEqual([3], reopened.GetTable(PersonTable.Descriptor).Scan().Select(p => p.Id));
        }
    }

    [TestMethod]
    public async Task A_Leftover_Log_From_An_Earlier_Database_Is_Never_Replayed_Into_A_New_One()
    {
        // An earlier database's logs, of the first and second generations, as a delete that could not
        // remove them all - a backend that cannot list its files - leaves them.
        var earlier = new BlazeDbInMemoryStorage();
        await using (var db = await OpenAsync(earlier))
        {
            db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Old one", 1));
            await db.FlushAsync();
            var firstLog = earlier.GetFile("wal-1.blz")!;
            await db.CheckpointAsync();
            db.GetTable(PersonTable.Descriptor).Insert(new Person(2, "Old two", 2));
            await db.FlushAsync();
            earlier.SetFile("wal-1.blz", firstLog);
        }

        var storage = new BlazeDbInMemoryStorage();
        storage.SetFile("wal-1.blz", earlier.GetFile("wal-1.blz")!);
        storage.SetFile("wal-2.blz", earlier.GetFile("wal-2.blz")!);

        await using (var db = await OpenAsync(storage))
        {
            var people = db.GetTable(PersonTable.Descriptor);
            people.Insert(new Person(10, "New one", 10));
            await db.FlushAsync();
            await db.CheckpointAsync();
            people.Insert(new Person(20, "New two", 20));
        }

        await using var reopened = await OpenAsync(storage);
        Assert.AreSequenceEqual([10, 20], reopened.GetTable(PersonTable.Descriptor).Scan().Select(p => p.Id).Order());
    }

    [TestMethod]
    public async Task A_Checkpoint_Whose_Cleanup_Fails_Still_Succeeds_And_Is_Announced()
    {
        var storage = new UndeletableStorage();
        var announced = new List<ulong>();
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = Never,
            OnCheckpoint = announced.Add,
        }.AddTable(PersonTable.Descriptor));

        db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));
        await db.FlushAsync();
        storage.Undeletable = "wal-1.blz";

        // The new generation is durable; the old log merely could not be removed.
        await db.CheckpointAsync();
        Assert.AreSequenceEqual([2UL], announced);
        Assert.IsNull(db.LastBackgroundError);
        Assert.IsNotNull(storage.Inner.GetFile("wal-1.blz"));

        // The removal is tried again at the next checkpoint.
        storage.Undeletable = null;
        await db.CheckpointAsync();
        Assert.AreSequenceEqual([2UL, 3UL], announced);
        Assert.IsNull(storage.Inner.GetFile("wal-1.blz"));
        Assert.IsNull(storage.Inner.GetFile("wal-2.blz"));
    }

    /// <summary>A backend that refuses to delete one file.</summary>
    private sealed class UndeletableStorage : IBlazeDbStorage
    {
        public BlazeDbInMemoryStorage Inner { get; } = new();

        public string? Undeletable { get; set; }

        public ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default) =>
            Inner.ReadAsync(name, cancellationToken);

        public ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            Inner.WriteAtomicAsync(name, data, cancellationToken);

        public ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            Inner.AppendAsync(name, data, cancellationToken);

        public ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default) =>
            name == Undeletable
                ? throw new IOException($"'{name}' is in use.")
                : Inner.DeleteAsync(name, cancellationToken);
    }

    /// <summary>A backend that cannot list its files, so a delete has to go by what the manifest names.</summary>
    private sealed class OpaqueStorage(BlazeDbInMemoryStorage inner) : IBlazeDbStorage
    {
        public ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default) =>
            inner.ReadAsync(name, cancellationToken);

        public ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            inner.WriteAtomicAsync(name, data, cancellationToken);

        public ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            inner.AppendAsync(name, data, cancellationToken);

        public ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default) =>
            inner.DeleteAsync(name, cancellationToken);
    }

    [TestMethod]
    public async Task Generated_Model_Persists_Through_Wal_And_Snapshot()
    {
        var storage = new BlazeDbInMemoryStorage();
        var options = () => new BlazeDbDatabaseOptions { Storage = storage, FlushInterval = Never }
            .AddTable(TodoItem.Table);

        var id = Guid.NewGuid();
        await using (var db = await BlazeDbDatabase.OpenAsync(options()))
        {
            var todos = db.GetTable(TodoItem.Table);
            todos.Insert(new TodoItem
            {
                Id = id,
                Title = "persist me",
                Done = true,
                CreatedAt = DateTime.UtcNow,
                Priority = Priority.High,
                Tags = ["a", "b"],
                Scores = [9, 8],
            });
            await db.CheckpointAsync();
        }

        await using (var reopened = await BlazeDbDatabase.OpenAsync(options()))
        {
            var todo = reopened.GetTable(TodoItem.Table).Get(id);
            Assert.IsNotNull(todo);
            Assert.AreEqual("persist me", todo.Title);
            Assert.IsTrue(todo.Done);
            Assert.AreEqual(Priority.High, todo.Priority);
            Assert.AreSequenceEqual(["a", "b"], todo.Tags);
            Assert.AreSequenceEqual([9, 8], todo.Scores);
        }
    }
}
