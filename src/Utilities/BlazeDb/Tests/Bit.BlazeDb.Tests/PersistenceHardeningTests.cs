using System.Diagnostics.Metrics;
using Bit.BlazeDb;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

/// <summary>
/// An in-memory backend that reports a full origin the way the browser backends now do - as
/// <see cref="BlazeDbStorageQuotaExceededException"/> from the write itself - while its estimate, like a
/// stale or missing browser estimate, never warns the engine beforehand.
/// </summary>
internal sealed class RefusingStorage : IBlazeDbQuotaAwareStorage, IBlazeDbEnumerableStorage
{
    public BlazeDbInMemoryStorage Inner { get; } = new();

    /// <summary>When set, every append is refused for lack of space.</summary>
    public bool RefuseAppends { get; set; }

    /// <summary>When set, every atomic write - a checkpoint's snapshot and manifest - is refused too.</summary>
    public bool RefuseWrites { get; set; }

    public ValueTask<BlazeDbStorageQuota?> GetQuotaAsync(CancellationToken cancellationToken = default) => default;

    public ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default) => Inner.ReadAsync(name, cancellationToken);

    public ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        if (RefuseWrites)
        {
            throw Refusal(data.Length);
        }
        return Inner.WriteAtomicAsync(name, data, cancellationToken);
    }

    public ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        if (RefuseAppends)
        {
            throw Refusal(data.Length);
        }
        return Inner.AppendAsync(name, data, cancellationToken);
    }

    public ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default) => Inner.DeleteAsync(name, cancellationToken);

    public ValueTask<IReadOnlyCollection<string>> ListAsync(CancellationToken cancellationToken = default) => Inner.ListAsync(cancellationToken);

    private static BlazeDbStorageQuotaExceededException Refusal(int bytes) =>
        new(new BlazeDbStorageQuota(1000, 1000), bytes, new IOException("QuotaExceededError"));
}

[TestClass]
public class PersistenceHardeningTests
{
    private static BlazeDbDatabaseOptions Options(IBlazeDbStorage storage, bool readOnly = false) =>
        new BlazeDbDatabaseOptions { Storage = storage, FlushInterval = TimeSpan.FromHours(1), ReadOnly = readOnly }
            .AddTable(PersonTable.Descriptor);

    [TestMethod]
    public async Task Opening_For_Writing_Removes_Generations_The_Manifest_No_Longer_Names()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using (var db = await BlazeDbDatabase.OpenAsync(Options(storage), TestContext.CancellationToken))
        {
            db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));
            await db.CheckpointAsync(TestContext.CancellationToken);
        }
        var current = storage.FileNames.ToHashSet();

        // What a tab closed between a checkpoint's new manifest and the deletion of the old generation leaves.
        await storage.WriteAtomicAsync("snapshot-0123abcd-1.blz", new byte[] { 1, 2, 3 }, TestContext.CancellationToken);
        await storage.WriteAtomicAsync("wal-0123abcd-1.blz", new byte[] { 4, 5, 6 }, TestContext.CancellationToken);
        await storage.WriteAtomicAsync("wal-7.blz", new byte[] { 7 }, TestContext.CancellationToken);
        // Not a file the engine writes: left alone.
        await storage.WriteAtomicAsync("notes.txt", new byte[] { 8 }, TestContext.CancellationToken);

        // A replica never deletes anything.
        await using (var replica = await BlazeDbDatabase.OpenAsync(Options(storage, readOnly: true), TestContext.CancellationToken))
        {
            Assert.Contains("wal-7.blz", storage.FileNames);
        }

        await using (var db = await BlazeDbDatabase.OpenAsync(Options(storage), TestContext.CancellationToken))
        {
            Assert.AreEqual("Ada", db.GetTable(PersonTable.Descriptor).Get(1)!.Name);
        }

        Assert.AreSequenceEqual(current.Append("notes.txt").ToList(), storage.FileNames.ToList(), Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
    }

    [TestMethod]
    public async Task A_Storage_That_Runs_Out_Of_Room_Is_Compacted_And_The_Flush_Succeeds()
    {
        var storage = new RefusingStorage();
        await using var db = await BlazeDbDatabase.OpenAsync(Options(storage), TestContext.CancellationToken);
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "Ada", 36));

        // The append is refused, but the checkpoint that follows writes the commit into a snapshot.
        storage.RefuseAppends = true;
        await db.FlushAsync(TestContext.CancellationToken);
        storage.RefuseAppends = false;

        Assert.IsNull(db.LastBackgroundError);
        await db.DisposeAsync();
        await using var reopened = await BlazeDbDatabase.OpenAsync(Options(storage), TestContext.CancellationToken);
        Assert.AreEqual("Ada", reopened.GetTable(PersonTable.Descriptor).Get(1)!.Name);
    }

    [TestMethod]
    public async Task A_Storage_Refusal_Compaction_Cannot_Fix_Reports_Quota_Pressure_Once()
    {
        var storage = new RefusingStorage();
        var pressure = 0;
        var options = Options(storage);
        options.OnQuotaPressure = _ => pressure++;
        await using var db = await BlazeDbDatabase.OpenAsync(options, TestContext.CancellationToken);
        db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));

        storage.RefuseAppends = storage.RefuseWrites = true;
        await Assert.ThrowsExactlyAsync<BlazeDbStorageQuotaExceededException>(async () => await db.FlushAsync(TestContext.CancellationToken));
        await Assert.ThrowsExactlyAsync<BlazeDbStorageQuotaExceededException>(async () => await db.FlushAsync(TestContext.CancellationToken));
        Assert.AreEqual(1, pressure);

        // Nothing was lost: once there is room, the buffered commit reaches storage.
        storage.RefuseAppends = storage.RefuseWrites = false;
        await db.FlushAsync(TestContext.CancellationToken);
        await db.DisposeAsync();
        await using var reopened = await BlazeDbDatabase.OpenAsync(Options(storage), TestContext.CancellationToken);
        Assert.AreEqual("Ada", reopened.GetTable(PersonTable.Descriptor).Get(1)!.Name);
    }

    [TestMethod]
    public async Task A_Replica_Can_Tell_That_The_Writer_Checkpointed_Since_It_Loaded()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using var writer = await BlazeDbDatabase.OpenAsync(Options(storage), TestContext.CancellationToken);
        writer.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));
        await writer.CheckpointAsync(TestContext.CancellationToken);

        await using var replica = await BlazeDbDatabase.OpenAsync(Options(storage, readOnly: true), TestContext.CancellationToken);
        Assert.IsFalse(await replica.HasNewerGenerationAsync(TestContext.CancellationToken));

        writer.GetTable(PersonTable.Descriptor).Insert(new Person(2, "Grace", 45));
        await writer.CheckpointAsync(TestContext.CancellationToken);
        Assert.IsTrue(await replica.HasNewerGenerationAsync(TestContext.CancellationToken));

        await replica.ReloadAsync(TestContext.CancellationToken);
        Assert.IsFalse(await replica.HasNewerGenerationAsync(TestContext.CancellationToken));
        Assert.AreEqual(2, replica.GetTable(PersonTable.Descriptor).Count);
    }

    [TestMethod]
    public void Flushing_A_Directory_Never_Fails_A_Write()
    {
        var directory = Directory.CreateTempSubdirectory("blazedb-dirsync-").FullName;
        try
        {
            BlazeDbDirectorySync.Flush(directory);
            BlazeDbDirectorySync.Flush(Path.Combine(directory, "missing"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task The_File_Storage_Writes_And_Appends_Durably_Named_Files()
    {
        var directory = Directory.CreateTempSubdirectory("blazedb-files-").FullName;
        try
        {
            using (var storage = new BlazeDbFileStorage(directory))
            await using (var db = await BlazeDbDatabase.OpenAsync(Options(storage), TestContext.CancellationToken))
            {
                db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));
                await db.FlushAsync(TestContext.CancellationToken);
                await db.CheckpointAsync(TestContext.CancellationToken);
                db.GetTable(PersonTable.Descriptor).Insert(new Person(2, "Grace", 45));
            }
            using var reopenedStorage = new BlazeDbFileStorage(directory);
            await using var reopened = await BlazeDbDatabase.OpenAsync(Options(reopenedStorage), TestContext.CancellationToken);
            Assert.AreEqual(2, reopened.GetTable(PersonTable.Descriptor).Count);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}

/// <summary>
/// The metrics on <see cref="BlazeDbDiagnostics.MeterName"/>. A <see cref="MeterListener"/> hears every database
/// in the process, so these run on their own, after the parallel tests.
/// </summary>
[TestClass]
[DoNotParallelize]
public class DiagnosticsTests
{
    private sealed class Recorder : IDisposable
    {
        private readonly MeterListener _listener = new();
        private readonly Dictionary<string, long> _counts = [];
        private readonly List<KeyValuePair<string, object?>[]> _failureTags = [];

        public Recorder()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == BlazeDbDiagnostics.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(instrument, value, tags));
            _listener.SetMeasurementEventCallback<double>((instrument, _, tags, _) => Record(instrument, 1, tags));
            _listener.Start();
        }

        private void Record(Instrument instrument, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            lock (_counts)
            {
                _counts[instrument.Name] = _counts.GetValueOrDefault(instrument.Name) + (instrument is Counter<long> ? value : 1);
                if (instrument.Name == "blazedb.storage.failures")
                {
                    _failureTags.Add(tags.ToArray());
                }
            }
        }

        public long this[string name]
        {
            get
            {
                lock (_counts)
                {
                    return _counts.GetValueOrDefault(name);
                }
            }
        }

        public IReadOnlyList<KeyValuePair<string, object?>[]> FailureTags => _failureTags;

        public void Dispose() => _listener.Dispose();
    }

    [TestMethod]
    public async Task Commits_Rollbacks_Flushes_Checkpoints_And_Recovery_Are_Measured()
    {
        using var recorder = new Recorder();
        var storage = new BlazeDbInMemoryStorage();
        await using (var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(PersonTable.Descriptor), TestContext.CancellationToken))
        {
            var people = db.GetTable(PersonTable.Descriptor);
            people.Insert(new Person(1, "Ada", 36));
            using (var tx = db.BeginTransaction())
            {
                people.Insert(new Person(2, "Grace", 45));
                people.Insert(new Person(3, "Edsger", 72));
                tx.Commit();
            }
            using (db.BeginTransaction())
            {
                people.Insert(new Person(4, "Rolled", 1));
            }
            // A transaction that wrote nothing commits nothing.
            db.BeginTransaction().Commit();

            Assert.AreEqual(2, recorder["blazedb.commits"]);
            Assert.AreEqual(1, recorder["blazedb.rollbacks"]);

            await db.FlushAsync(TestContext.CancellationToken);
            Assert.AreEqual(1, recorder["blazedb.wal.flushes"]);
            Assert.IsGreaterThan(0, recorder["blazedb.wal.flushed_bytes"]);
            Assert.AreEqual(1, recorder["blazedb.wal.flush.duration"]);

            await db.CheckpointAsync(TestContext.CancellationToken);
            Assert.AreEqual(1, recorder["blazedb.checkpoints"]);
            Assert.AreEqual(1, recorder["blazedb.checkpoint.duration"]);
            Assert.AreEqual(1, recorder["blazedb.checkpoint.snapshot_bytes"]);

            people.Insert(new Person(5, "Barbara", 60));
        }

        var recoveriesBefore = recorder["blazedb.recovery.duration"];
        await using (await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(PersonTable.Descriptor), TestContext.CancellationToken))
        {
        }
        Assert.AreEqual(recoveriesBefore + 1, recorder["blazedb.recovery.duration"]);
        // The insert after the checkpoint was flushed on dispose and replayed on the reopen.
        Assert.AreEqual(1, recorder["blazedb.recovery.replayed_commits"]);
    }

    [TestMethod]
    public async Task Storage_Failures_Are_Tagged_With_The_Operation_And_Error_Type_Only()
    {
        using var recorder = new Recorder();
        var storage = new RefusingStorage();
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(PersonTable.Descriptor), TestContext.CancellationToken);
        db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Secret name", 36));

        storage.RefuseAppends = storage.RefuseWrites = true;
        await Assert.ThrowsExactlyAsync<BlazeDbStorageQuotaExceededException>(async () => await db.FlushAsync(TestContext.CancellationToken));

        Assert.AreEqual(1, recorder["blazedb.quota.refusals"]);
        var operations = recorder.FailureTags
            .Select(tags => (string?)tags.Single(t => t.Key == "blazedb.operation").Value)
            .ToList();
        Assert.AreSequenceEqual(new[] { "flush", "checkpoint" }, operations, Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
        foreach (var tags in recorder.FailureTags)
        {
            Assert.AreSequenceEqual(new[] { "blazedb.operation", "error.type" }, tags.Select(t => t.Key).ToList(), Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
            Assert.AreEqual(typeof(BlazeDbStorageQuotaExceededException).FullName, tags.Single(t => t.Key == "error.type").Value);
        }

        storage.RefuseAppends = storage.RefuseWrites = false;
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
