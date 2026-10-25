using BenchmarkDotNet.Attributes;
using Bit.BlazeDb;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Benchmarks;

/// <summary>
/// What persistence costs as the database grows: writing a checkpoint (the snapshot every
/// <c>CheckpointWalSize</c> bytes of log, taken while writes wait), and opening a database - recovery
/// decodes the snapshot and rebuilds every index, which is what a user waits for when the app starts.
/// Storage is in memory, so the numbers are the engine's share, not the disk's.
/// </summary>
[MemoryDiagnoser]
public class PersistenceBenchmarks
{
    private BlazeDbInMemoryStorage _storage = null!;
    private BlazeDbInMemoryStorage _openStorage = null!;
    private BlazeDbDatabase _db = null!;

    [Params(10_000, 100_000)]
    public int Rows { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _storage = new BlazeDbInMemoryStorage();
        _db = await BlazeDbDatabase.OpenAsync(Options(_storage));
        var people = _db.GetTable(BenchPerson.Table);
        using (var txn = _db.BeginTransaction())
        {
            for (var i = 0; i < Rows; i++)
            {
                people.Insert(new BenchPerson { Id = i, Name = $"Person {i}", Flag = i % 2 == 0, Age = i % 100 });
            }
            txn.Commit();
        }
        await _db.CheckpointAsync();
        // A copy no other instance writes to, so the open cannot race the one that owns the original's
        // background loop. Opening a clean checkpoint and disposing with nothing committed writes nothing,
        // so every iteration opens the same bytes.
        _openStorage = _storage.Clone();
    }

    private static BlazeDbDatabaseOptions Options(IBlazeDbStorage storage) => new BlazeDbDatabaseOptions
    {
        Storage = storage,
        FlushInterval = TimeSpan.FromHours(1),
        CheckpointWalSize = long.MaxValue,
    }.AddTable(BenchPerson.Table);

    [Benchmark]
    public ValueTask Checkpoint() => _db.CheckpointAsync();

    [Benchmark]
    public async Task<int> Open()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(Options(_openStorage));
        return db.GetTable(BenchPerson.Table).Count;
    }

    [GlobalCleanup]
    public async Task Cleanup() => await _db.DisposeAsync();
}
