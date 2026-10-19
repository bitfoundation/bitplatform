using Bit.BlazeDb;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

/// <summary>An in-memory backend with a settable allowance, standing in for the browser's estimate.</summary>
internal sealed class QuotaLimitedStorage : IBlazeDbQuotaAwareStorage
{
    private readonly BlazeDbInMemoryStorage _inner = new();
    private readonly Dictionary<string, int> _sizes = [];

    public long QuotaBytes { get; set; } = long.MaxValue;

    public int EstimateCalls { get; private set; }

    /// <summary>
    /// When set, an append needs room for the whole file plus the new bytes, the way OPFS's swap
    /// copy and IndexedDB's put-the-record-back do, and the backend tells the engine so.
    /// </summary>
    public bool RewritesOnAppend { get; set; }

    public bool AppendRewritesWholeFile => RewritesOnAppend;

    public long UsageBytes
    {
        get
        {
            lock (_sizes)
            {
                return _sizes.Values.Sum(static size => (long)size);
            }
        }
    }

    public ValueTask<BlazeDbStorageQuota?> GetQuotaAsync(CancellationToken cancellationToken = default)
    {
        EstimateCalls++;
        return new ValueTask<BlazeDbStorageQuota?>(new BlazeDbStorageQuota(UsageBytes, QuotaBytes));
    }

    public ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default) =>
        _inner.ReadAsync(name, cancellationToken);

    public async ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        lock (_sizes)
        {
            EnsureRoom(data.Length - _sizes.GetValueOrDefault(name));
        }
        await _inner.WriteAtomicAsync(name, data, cancellationToken);
        lock (_sizes)
        {
            _sizes[name] = data.Length;
        }
    }

    public async ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        lock (_sizes)
        {
            EnsureRoom(RewritesOnAppend ? _sizes.GetValueOrDefault(name) + data.Length : data.Length);
        }
        await _inner.AppendAsync(name, data, cancellationToken);
        lock (_sizes)
        {
            _sizes[name] = _sizes.GetValueOrDefault(name) + data.Length;
        }
    }

    /// <summary>Mimics the browser rejecting a write with QuotaExceededError.</summary>
    private void EnsureRoom(long extraBytes)
    {
        if (UsageBytes + extraBytes > QuotaBytes)
        {
            throw new IOException("QuotaExceededError");
        }
    }

    public async ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        await _inner.DeleteAsync(name, cancellationToken);
        lock (_sizes)
        {
            _sizes.Remove(name);
        }
    }
}

[TestClass]
public class QuotaTests
{
    private static async Task<(BlazeDbDatabase Db, BlazeDbTable<Guid, TodoItem> Todos)> OpenAsync(
        QuotaLimitedStorage storage, Action<BlazeDbDatabaseOptions>? configure = null)
    {
        var options = new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = TimeSpan.FromHours(1),
            QuotaReserveBytes = 0,
        }.AddTable(TodoItem.Table);
        configure?.Invoke(options);
        var db = await BlazeDbDatabase.OpenAsync(options);
        return (db, db.GetTable(TodoItem.Table));
    }

    private static void Fill(BlazeDbTable<Guid, TodoItem> todos, int rows)
    {
        for (var i = 0; i < rows; i++)
        {
            todos.Insert(new TodoItem { Id = Guid.NewGuid(), Title = new string('x', 200) });
        }
    }

    [TestMethod]
    public async Task Quota_Is_Surfaced_Through_The_Database()
    {
        var storage = new QuotaLimitedStorage { QuotaBytes = 10_000 };
        var (db, todos) = await OpenAsync(storage);
        await using var owned = db;
        Fill(todos, 5);
        await db.FlushAsync();

        var quota = await db.GetStorageQuotaAsync();

        Assert.IsNotNull(quota);
        Assert.AreEqual(10_000, quota!.Value.QuotaBytes);
        Assert.IsTrue(quota.Value.UsageBytes > 0);
        Assert.IsTrue(quota.Value.UsedFraction is > 0 and < 1);
    }

    [TestMethod]
    public async Task Quota_Is_Null_For_Backends_That_Cannot_Report_It()
    {
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = new BlazeDbInMemoryStorage(),
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(TodoItem.Table));
        await using var owned = db;

        Assert.IsNull(await db.GetStorageQuotaAsync());
    }

    [TestMethod]
    public async Task Flush_Fails_Deliberately_Instead_Of_Overrunning_The_Quota()
    {
        var storage = new QuotaLimitedStorage { QuotaBytes = 4_000 };
        var (db, todos) = await OpenAsync(storage);
        await using var owned = db;

        Fill(todos, 100);

        var ex = await Assert.ThrowsExactlyAsync<BlazeDbStorageQuotaExceededException>(async () => await db.FlushAsync());
        Assert.IsTrue(ex.RequiredBytes > 0);
        Assert.AreEqual(4_000, ex.Quota.QuotaBytes);

        // Disposal flushes one last time; give it room so the test ends on the assertion above
        // rather than on the (correct) failure to persist.
        storage.QuotaBytes = long.MaxValue;
    }

    [TestMethod]
    public async Task Data_Survives_In_Memory_After_A_Refused_Flush()
    {
        var storage = new QuotaLimitedStorage { QuotaBytes = 4_000 };
        var (db, todos) = await OpenAsync(storage);
        await using var owned = db;
        Fill(todos, 100);

        await Assert.ThrowsExactlyAsync<BlazeDbStorageQuotaExceededException>(async () => await db.FlushAsync());

        // The engine degraded rather than failing the application: reads and writes still work.
        Assert.AreEqual(100, todos.Count);
        todos.Insert(new TodoItem { Id = Guid.NewGuid(), Title = "still writable" });
        Assert.AreEqual(101, todos.Count);

        storage.QuotaBytes = long.MaxValue;
    }

    [TestMethod]
    public async Task A_Refused_Flush_Succeeds_Once_Space_Is_Freed()
    {
        var storage = new QuotaLimitedStorage { QuotaBytes = 4_000 };
        var (db, todos) = await OpenAsync(storage);
        await using var owned = db;
        Fill(todos, 100);
        await Assert.ThrowsExactlyAsync<BlazeDbStorageQuotaExceededException>(async () => await db.FlushAsync());

        storage.QuotaBytes = 10_000_000;
        await db.FlushAsync();

        // Nothing was dropped on the way: a reopen sees every row.
        await db.DisposeAsync();
        var reopened = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(TodoItem.Table));
        await using var owned2 = reopened;
        Assert.AreEqual(100, reopened.GetTable(TodoItem.Table).Count);
    }

    [TestMethod]
    public async Task Pressure_Callback_Reports_The_Offending_Quota()
    {
        BlazeDbStorageQuota? reported = null;
        var storage = new QuotaLimitedStorage { QuotaBytes = 4_000 };
        var (db, todos) = await OpenAsync(storage, o => o.OnQuotaPressure = q => reported = q);
        await using var owned = db;
        Fill(todos, 100);

        await Assert.ThrowsExactlyAsync<BlazeDbStorageQuotaExceededException>(async () => await db.FlushAsync());

        Assert.IsNotNull(reported);
        Assert.AreEqual(4_000, reported!.Value.QuotaBytes);

        storage.QuotaBytes = long.MaxValue;
    }

    [TestMethod]
    public async Task Pressure_Is_Reported_Once_Per_Episode_Not_Once_Per_Refusal()
    {
        var reports = 0;
        var storage = new QuotaLimitedStorage { QuotaBytes = 4_000 };
        var (db, todos) = await OpenAsync(storage, o => o.OnQuotaPressure = _ => reports++);
        await using var owned = db;
        Fill(todos, 100);

        // The background flusher would retry every tick; the application hears about it once.
        await Assert.ThrowsExactlyAsync<BlazeDbStorageQuotaExceededException>(async () => await db.FlushAsync());
        await Assert.ThrowsExactlyAsync<BlazeDbStorageQuotaExceededException>(async () => await db.FlushAsync());
        Assert.AreEqual(1, reports);

        // Space is freed, the flush succeeds, and a later refusal is a new episode. (The limit is
        // lowered again here, which real quotas do not do; refreshing the estimate through the
        // database is what makes the engine see it.)
        storage.QuotaBytes = long.MaxValue;
        await db.FlushAsync();
        storage.QuotaBytes = storage.UsageBytes + 100;
        await db.GetStorageQuotaAsync();
        Fill(todos, 100);
        await Assert.ThrowsExactlyAsync<BlazeDbStorageQuotaExceededException>(async () => await db.FlushAsync());
        Assert.AreEqual(2, reports);

        storage.QuotaBytes = long.MaxValue;
    }

    [TestMethod]
    public async Task The_Reserve_Keeps_Headroom_Free()
    {
        var storage = new QuotaLimitedStorage { QuotaBytes = 1_000_000 };
        // Unrelated origin data, which counts toward the browser's estimate just the same.
        await storage.WriteAtomicAsync("other-origin-data", new byte[970_000]);

        var (db, todos) = await OpenAsync(storage, o => o.QuotaReserveBytes = 25_000);
        await using var owned = db;
        Fill(todos, 50);

        // An open transaction rules out compaction, leaving the reserve as the only thing standing
        // between the flush and the limit. The bytes would physically fit; they would just leave
        // less than the reserve behind.
        using var tx = db.BeginTransaction();
        await Assert.ThrowsExactlyAsync<BlazeDbStorageQuotaExceededException>(async () => await db.FlushAsync());

        tx.Rollback();
        storage.QuotaBytes = long.MaxValue;
    }

    [TestMethod]
    public async Task Compaction_Runs_Before_Giving_Up()
    {
        var storage = new QuotaLimitedStorage { QuotaBytes = 100_000 };
        // The reserve is what leaves room to write the compacted snapshot before the old WAL goes.
        var (db, todos) = await OpenAsync(storage, o => o.QuotaReserveBytes = 20_000);
        await using var owned = db;

        // Repeatedly rewriting one row makes the WAL far bigger than the data it represents,
        // so checkpointing reclaims almost all of it.
        var id = Guid.NewGuid();
        for (var i = 0; i < 400; i++)
        {
            todos.Upsert(new TodoItem { Id = id, Title = new string('y', 200) });
            await db.FlushAsync();
        }

        Assert.AreEqual(1, todos.Count);
        Assert.IsTrue(storage.UsageBytes < 100_000, $"usage was {storage.UsageBytes}");

        // Everything is durable: reopening replays what compaction left behind.
        await db.FlushAsync();
        await db.DisposeAsync();
        var reopened = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(TodoItem.Table));
        await using var owned2 = reopened;
        Assert.AreEqual(1, reopened.GetTable(TodoItem.Table).Count);
    }

    [TestMethod]
    public async Task A_Backend_That_Rewrites_On_Append_Is_Charged_For_The_Whole_Log()
    {
        // The engine's preflight counted only the bytes being added, but OPFS and IndexedDB briefly
        // need the log twice over. A log a little under the limit then passed the check and failed
        // in the browser as a bare I/O error, never reaching the deliberate refusal or compaction.
        var storage = new QuotaLimitedStorage { RewritesOnAppend = true };
        var (db, todos) = await OpenAsync(storage, o => o.CheckpointWalSize = long.MaxValue);
        await using var owned = db;

        // One row rewritten many times: a log far larger than the snapshot it compacts to.
        var id = Guid.NewGuid();
        for (var i = 0; i < 150; i++)
        {
            todos.Upsert(new TodoItem { Id = id, Title = new string('y', 200) });
            await db.FlushAsync();
        }
        var logSize = storage.UsageBytes;
        Assert.IsTrue(logSize > 30_000, $"log was {logSize} bytes");

        // Room for the log one and a half times over: a snapshot fits, a copy of the log does not.
        storage.QuotaBytes = logSize + logSize / 2;
        await db.GetStorageQuotaAsync();

        // Appending one more record needs the log twice. With an open transaction compaction cannot
        // run, so the only correct answer is a deliberate refusal - never a QuotaExceededError
        // from the backend, which is what counting only the new bytes produced.
        todos.Upsert(new TodoItem { Id = id, Title = "once more" });
        using (var tx = db.BeginTransaction())
        {
            await Assert.ThrowsExactlyAsync<BlazeDbStorageQuotaExceededException>(async () => await db.FlushAsync());
            tx.Rollback();
        }

        // Without the transaction the engine compacts the log into a snapshot and the flush lands.
        await db.FlushAsync();
        Assert.IsTrue(storage.UsageBytes < logSize / 2, $"usage after compaction was {storage.UsageBytes}");
        Assert.AreEqual("once more", todos.Get(id)!.Title);
        storage.QuotaBytes = long.MaxValue;
    }

    [TestMethod]
    public async Task Estimates_Are_Not_Re_Read_On_Every_Flush()
    {
        var storage = new QuotaLimitedStorage { QuotaBytes = 100_000_000 };
        var (db, todos) = await OpenAsync(storage);
        await using var owned = db;

        for (var i = 0; i < 20; i++)
        {
            todos.Insert(new TodoItem { Id = Guid.NewGuid(), Title = "row" });
            await db.FlushAsync();
        }

        // The estimate is cached and only refreshed after enough bytes have gone by.
        Assert.IsTrue(storage.EstimateCalls < 20, $"expected caching, saw {storage.EstimateCalls} estimate calls");
    }
}
