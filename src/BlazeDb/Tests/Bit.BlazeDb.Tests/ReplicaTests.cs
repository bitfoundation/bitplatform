using Bit.BlazeDb;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

/// <summary>
/// Read-replica semantics. In the browser these instances live in different tabs and share the
/// origin's storage; here two BlazeDbDatabase instances share one IBlazeDbStorage, which exercises the same
/// paths - the tabs only ever communicate through storage anyway.
/// </summary>
[TestClass]
public class ReplicaTests
{
    private static Task<BlazeDbDatabase> OpenWriterAsync(IBlazeDbStorage storage, Action<BlazeDbDatabaseOptions>? configure = null)
    {
        var options = new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(TodoItem.Table);
        configure?.Invoke(options);
        return BlazeDbDatabase.OpenAsync(options).AsTask();
    }

    private static Task<BlazeDbDatabase> OpenReplicaAsync(IBlazeDbStorage storage) =>
        BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = storage,
            ReadOnly = true,
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(TodoItem.Table)).AsTask();

    private static TodoItem Make(string title) => new() { Id = Guid.NewGuid(), Title = title };

    [TestMethod]
    public async Task A_Replica_Refuses_Writes()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using var writer = await OpenWriterAsync(storage);
        await using var replica = await OpenReplicaAsync(storage);

        var todos = replica.GetTable(TodoItem.Table);

        Assert.IsTrue(replica.IsReadOnly);
        Assert.ThrowsExactly<InvalidOperationException>(() => todos.Insert(Make("nope")));
        Assert.ThrowsExactly<InvalidOperationException>(() => replica.BeginTransaction());
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await replica.CheckpointAsync());
    }

    [TestMethod]
    public async Task A_Replica_Sees_Flushed_Writes_After_Reloading()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using var writer = await OpenWriterAsync(storage);
        await using var replica = await OpenReplicaAsync(storage);

        writer.GetTable(TodoItem.Table).Insert(Make("from the writer"));
        await writer.FlushAsync();

        // Nothing arrives until the replica goes back to storage.
        Assert.AreEqual(0, replica.GetTable(TodoItem.Table).Count);

        await replica.ReloadAsync();

        Assert.AreEqual("from the writer", replica.GetTable(TodoItem.Table).Scan().Single().Title);
    }

    [TestMethod]
    public async Task A_Replica_Never_Sees_Unflushed_Writes()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using var writer = await OpenWriterAsync(storage);
        await using var replica = await OpenReplicaAsync(storage);

        writer.GetTable(TodoItem.Table).Insert(Make("committed but not yet durable"));
        await replica.ReloadAsync();

        Assert.AreEqual(0, replica.GetTable(TodoItem.Table).Count);
    }

    [TestMethod]
    public async Task Reloading_Picks_Up_Deletes_And_Updates()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using var writer = await OpenWriterAsync(storage);
        var writerTodos = writer.GetTable(TodoItem.Table);
        var keep = Make("keep");
        var drop = Make("drop");
        writerTodos.Insert(keep);
        writerTodos.Insert(drop);
        await writer.FlushAsync();

        await using var replica = await OpenReplicaAsync(storage);
        Assert.AreEqual(2, replica.GetTable(TodoItem.Table).Count);

        writerTodos.Delete(drop.Id);
        writerTodos.Upsert(new TodoItem { Id = keep.Id, Title = "renamed", Done = true });
        await writer.FlushAsync();
        await replica.ReloadAsync();

        var rows = replica.GetTable(TodoItem.Table);
        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("renamed", rows.Get(keep.Id)!.Title);
        Assert.IsFalse(rows.TryGet(drop.Id, out _));
    }

    [TestMethod]
    public async Task Reloading_Rebuilds_Secondary_Indexes()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using var writer = await OpenWriterAsync(storage);
        var writerTodos = writer.GetTable(TodoItem.Table);
        writerTodos.Insert(new TodoItem { Id = Guid.NewGuid(), Title = "a", Done = true, Category = "work" });
        await writer.FlushAsync();

        await using var replica = await OpenReplicaAsync(storage);
        Assert.AreEqual(1, replica.GetTable(TodoItem.Table).CountBy(TodoItem.Indexes.Done, true));

        writerTodos.Insert(new TodoItem { Id = Guid.NewGuid(), Title = "b", Done = true, Category = "work" });
        await writer.FlushAsync();
        await replica.ReloadAsync();

        var rows = replica.GetTable(TodoItem.Table);
        Assert.AreEqual(2, rows.CountBy(TodoItem.Indexes.Done, true));
        Assert.AreEqual(2, rows.Lookup(TodoItem.Indexes.Category, "work").Count());
    }

    [TestMethod]
    public async Task Reloading_Twice_Does_Not_Duplicate_Rows()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using var writer = await OpenWriterAsync(storage);
        writer.GetTable(TodoItem.Table).Insert(Make("only once"));
        await writer.FlushAsync();

        await using var replica = await OpenReplicaAsync(storage);
        await replica.ReloadAsync();
        await replica.ReloadAsync();

        Assert.AreEqual(1, replica.GetTable(TodoItem.Table).Count);
        Assert.AreEqual(1, replica.GetTable(TodoItem.Table).CountBy(TodoItem.Indexes.Done, false));
    }

    [TestMethod]
    public async Task A_Replica_Follows_The_Writer_Across_A_Checkpoint()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using var writer = await OpenWriterAsync(storage);
        var writerTodos = writer.GetTable(TodoItem.Table);
        for (var i = 0; i < 20; i++)
        {
            writerTodos.Insert(Make("row " + i));
        }
        await writer.FlushAsync();

        await using var replica = await OpenReplicaAsync(storage);
        await writer.CheckpointAsync();
        writerTodos.Insert(Make("after the checkpoint"));
        await writer.FlushAsync();

        await replica.ReloadAsync();

        // The replica now reads a different snapshot and WAL generation than it opened with.
        Assert.AreEqual(21, replica.GetTable(TodoItem.Table).Count);
    }

    [TestMethod]
    public async Task Checkpoints_Are_Announced_Once_They_Are_Durable()
    {
        var storage = new BlazeDbInMemoryStorage();
        var announced = new List<ulong>();
        await using var writer = await OpenWriterAsync(storage, o => o.OnCheckpoint = g => announced.Add(g));

        writer.GetTable(TodoItem.Table).Insert(Make("row"));
        await writer.FlushAsync();
        await writer.CheckpointAsync();
        await writer.CheckpointAsync();

        // Generations increase, which is what a replica uses to tell announcements apart.
        Assert.AreEqual(2, announced.Count);
        Assert.IsTrue(announced[1] > announced[0]);
    }

    [TestMethod]
    public async Task A_Replica_Leaves_Storage_Untouched()
    {
        var storage = new RecordingStorage();
        await using var writer = await OpenWriterAsync(storage);
        writer.GetTable(TodoItem.Table).Insert(Make("row"));
        await writer.FlushAsync();

        storage.Writes.Clear();
        await using (var replica = await OpenReplicaAsync(storage))
        {
            await replica.ReloadAsync();
        }

        Assert.IsEmpty(storage.Writes);
    }

    [TestMethod]
    public async Task A_Replica_Opens_On_An_Empty_Database_Without_Creating_One()
    {
        var storage = new RecordingStorage();
        await using var replica = await OpenReplicaAsync(storage);

        Assert.AreEqual(0, replica.GetTable(TodoItem.Table).Count);
        Assert.IsEmpty(storage.Writes);
    }

    [TestMethod]
    public async Task A_Reload_That_Fails_Keeps_The_Rows_The_Replica_Had()
    {
        // Nothing is cleared until the newer generation has been read and checksummed, so a replica
        // that cannot get at it goes on serving what it has instead of emptying itself.
        var storage = new BlazeDbInMemoryStorage();
        await using var writer = await OpenWriterAsync(storage);
        writer.GetTable(TodoItem.Table).Insert(Make("kept"));
        await writer.FlushAsync();
        await using var replica = await OpenReplicaAsync(storage);
        Assert.AreEqual(1, replica.GetTable(TodoItem.Table).Count);

        await writer.CheckpointAsync();
        var snapshot = storage.GetFile("snapshot-2.blz")!;
        snapshot[^1] ^= 0xFF;
        storage.SetFile("snapshot-2.blz", snapshot);

        await Assert.ThrowsExactlyAsync<BlazeDbCorruptDatabaseException>(async () => await replica.ReloadAsync());

        Assert.AreEqual("kept", replica.GetTable(TodoItem.Table).Scan().Single().Title);
    }

    [TestMethod]
    public async Task A_Reload_Overlapping_A_Checkpoint_Reads_One_Whole_Generation()
    {
        // A checkpoint deletes the log the replica's manifest named while the replica is still
        // reading. Finding it gone must not be mistaken for a log with nothing in it, which would
        // silently drop every commit the older snapshot does not cover.
        var gate = new TaskCompletionSource();
        var storage = new GatedStorage();
        await using var writer = await OpenWriterAsync(storage);
        var todos = writer.GetTable(TodoItem.Table);

        todos.Insert(Make("in the snapshot"));
        await writer.FlushAsync();
        await writer.CheckpointAsync();
        todos.Insert(Make("only in the log"));
        await writer.FlushAsync();

        await using var replica = await OpenReplicaAsync(storage);
        storage.BeforeRead = name => name.StartsWith("wal-") ? gate.Task : Task.CompletedTask;

        // The replica reads the generation-2 manifest and snapshot, then stalls on wal-2 ...
        var reload = replica.ReloadAsync().AsTask();
        await Task.Delay(50);
        Assert.IsFalse(reload.IsCompleted);

        // ... while the writer moves everything into generation 3 and deletes wal-2.
        todos.Insert(Make("after the checkpoint"));
        await writer.FlushAsync();
        await writer.CheckpointAsync();
        gate.SetResult();
        await reload;

        Assert.AreEqual(3, replica.GetTable(TodoItem.Table).Count);
    }

    /// <summary>Lets a test hold a read open while the writer changes the files underneath it.</summary>
    private sealed class GatedStorage : IBlazeDbStorage
    {
        private readonly BlazeDbInMemoryStorage _inner = new();

        public Func<string, Task>? BeforeRead { get; set; }

        public async ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default)
        {
            if (BeforeRead is { } wait)
            {
                await wait(name);
            }
            return await _inner.ReadAsync(name, cancellationToken);
        }

        public ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            _inner.WriteAtomicAsync(name, data, cancellationToken);

        public ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            _inner.AppendAsync(name, data, cancellationToken);

        public ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default) =>
            _inner.DeleteAsync(name, cancellationToken);
    }

    /// <summary>Records every mutating call so a test can assert a replica stays passive.</summary>
    private sealed class RecordingStorage : IBlazeDbStorage
    {
        private readonly BlazeDbInMemoryStorage _inner = new();

        public List<string> Writes { get; } = [];

        public ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default) =>
            _inner.ReadAsync(name, cancellationToken);

        public ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            Writes.Add("write:" + name);
            return _inner.WriteAtomicAsync(name, data, cancellationToken);
        }

        public ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            Writes.Add("append:" + name);
            return _inner.AppendAsync(name, data, cancellationToken);
        }

        public ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default)
        {
            Writes.Add("delete:" + name);
            return _inner.DeleteAsync(name, cancellationToken);
        }
    }
}
